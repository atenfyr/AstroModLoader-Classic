using DouglasDwyer.CasCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using UAssetAPI;
using UAssetAPI.UnrealTypes;

namespace AstroModIntegrator
{
    public class CustomRoutineAPIWrapper : ICustomRoutineAPI
    {
        private bool Enabled = true;
        private bool bShouldExitNow = false;
        private ModIntegrator Integrator;

        public UAsset FindFile(string target)
        {
            if (!Enabled) throw new InvalidOperationException("API is disabled because of thread timeout");
            return Integrator.FindFile(target);
        }
        public byte[] FindFileRaw(string target)
        {
            if (!Enabled) throw new InvalidOperationException("API is disabled because of thread timeout");
            return Integrator.FindFileRaw(target);
        }
        public byte[] FindFileRaw(string target, out EngineVersion engVer)
        {
            if (!Enabled) throw new InvalidOperationException("API is disabled because of thread timeout");
            return Integrator.FindFileRaw(target, out engVer);
        }
        public void AddFile(string outPath, UAsset outAsset)
        {
            if (!Enabled) throw new InvalidOperationException("API is disabled because of thread timeout");
            Integrator.AddFile(outPath, outAsset);
        }
        public void AddFileRaw(string outPath, byte[] rawData)
        {
            if (!Enabled) throw new InvalidOperationException("API is disabled because of thread timeout");
            Integrator.AddFileRaw(outPath, rawData);
        }
        public Metadata GetCurrentMod()
        {
            if (!Enabled) throw new InvalidOperationException("API is disabled because of thread timeout");
            return Integrator.GetCurrentMod();
        }
        public IReadOnlyList<Metadata> GetAllMods()
        {
            if (!Enabled) throw new InvalidOperationException("API is disabled because of thread timeout");
            return Integrator.GetAllMods();
        }
        public Metadata GetModFromRoutine(CustomRoutine routine)
        {
            if (!Enabled) throw new InvalidOperationException("API is disabled because of thread timeout");
            return Integrator.GetModFromRoutine(routine);
        }
        public CustomRoutine GetCustomRoutineFromID(string routineID)
        {
            if (!Enabled) throw new InvalidOperationException("API is disabled because of thread timeout");
            return Integrator.GetCustomRoutineFromID(routineID);
        }
        public bool LogToDisk(string text, bool prefixWithMod = true)
        {
            if (!Enabled) throw new InvalidOperationException("API is disabled because of thread timeout");
            return Integrator.LogToDisk(text, prefixWithMod);
        }
        public bool ShouldExitNow()
        {
            return bShouldExitNow;
        }
        internal void SetEnabled(bool newVal)
        {
            Enabled = newVal;
        }
        internal void SetShouldExitNow(bool newVal)
        {
            bShouldExitNow = newVal;
        }

        internal CustomRoutineAPIWrapper(ModIntegrator integrator)
        {
            Enabled = true;
            bShouldExitNow = false;
            Integrator = integrator;
        }
    }

    public class ModIntegrator
    {
        // Settings //
        public bool RefuseMismatchedConnections = true;
        public bool EnableCustomRoutines = false;
        public List<string> OptionalModIDs;
        public bool Verbose = false;
        public string PakToNamedPipe = null;
        public string CallingExePath = null; // for debugging
        public bool IsModIntegratorCMD = false;
        // End Settings //

        // Exposed Fields //
        public Version DetectedAstroBuild;
        // End Exposed Fields //

        private static string[] DefaultMapPaths = [
            "Astro/Content/Maps/Staging_T2.umap",
            "Astro/Content/Maps/Staging_T2_PackedPlanets_Switch.umap",
            "Astro/Content/U32_Expansion/U32_Expansion.umap" // DLC map
        ];

        private Dictionary<string, byte[]> CreatedPakData;
        private volatile Dictionary<string, byte[]> CreatedPakDataTemp;
        private volatile PakExtractor pakExtractorForCustomRoutines;
        private volatile string currentMod; // RoutineID
        private volatile List<Metadata> allMods;
        private volatile Dictionary<string, CustomRoutine> customRoutinesMap;
        private volatile Dictionary<string, Metadata> customRoutinesMap2;
        private volatile Dictionary<Guid, Metadata> customRoutineAssemblyToMetadata = null;
        private volatile bool isCurrentlyIntegrating = false;
        private volatile bool hasLoggedOnceAlready = false;

        // policy initialization fields
        internal volatile static bool EnableGlobalSandbox = true; // can add a few extra hundred milliseconds unless no custom routines are loaded; always enabled
        internal volatile static CasPolicy policy = null;
        internal volatile static CasAssemblyLoader loadContext = null;
        internal volatile static Thread policyInitThread = null;

        static ModIntegrator()
        {
            /*// always enable sandbox if not on Windows
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                EnableGlobalSandbox = true;
            }
            else if (!IntegrityLevelChecker.IsCurrentProcessLowIntegrity())
            {
                // on Windows, enable sandbox only if not low-integrity
                EnableGlobalSandbox = true;
            }

//#if DEBUG || DEBUG_CUSTOMROUTINETEST
            // always enable sandbox in debug
            EnableGlobalSandbox = true;
//#endif*/

            // load sandbox policy
            // extremely slow (reflection) so we cache it
            if (policy == null && EnableGlobalSandbox)
            {
                policyInitThread = new Thread(() =>
                {
                    policy = new CasPolicyBuilder().WithDefaultSandbox()
                        .Allow(new TypeBinding(typeof(ICustomRoutineAPI), Accessibility.Public))
                        .Allow(new TypeBinding(typeof(CustomRoutineAPIWrapper), Accessibility.Public))
                        .Allow(new TypeBinding(typeof(CustomRoutine), Accessibility.Private))
                        .Allow(new TypeBinding(typeof(Dependency), Accessibility.Private))
                        .Allow(new TypeBinding(typeof(DownloadMode), Accessibility.Private))
                        .Allow(new TypeBinding(typeof(DownloadInfo), Accessibility.Private))
                        .Allow(new TypeBinding(typeof(SyncMode), Accessibility.Private))
                        .Allow(new TypeBinding(typeof(IntegratorEntries), Accessibility.Private))
                        .Allow(new TypeBinding(typeof(Metadata), Accessibility.Private))
                        .Allow(new TypeBinding(typeof(IntegratorUtils), Accessibility.Public))
                        .Allow(new AssemblyBinding(typeof(UAssetAPI.UAsset).Assembly, Accessibility.Public))
                        .Allow(new AssemblyBinding(typeof(Newtonsoft.Json.Linq.JObject).Assembly, Accessibility.Private))
                        .Deny(typeof(UAsset).GetMethods().Where(p => p.Name == "PathToStream" || p.Name == "Write" || p.Name == "SerializeJson" || p.Name == "SerializeJsonObject" || p.Name == "DeserializeJson" || p.Name == "DeserializeJsonObject" || p.Name == "PullSchemasFromAnotherAsset" || p.Name == "VerifyBinaryEquality"))
                        .Deny(typeof(UAsset).GetConstructors())
                        //.Deny(new TypeBinding(typeof(UAssetAPI.Unversioned.Usmap), Accessibility.Private))
                        .Deny(new TypeBinding(typeof(UAssetAPI.Unversioned.SaveGame), Accessibility.Private))
                    .Build();
                    loadContext = new CasAssemblyLoader(policy, true);
                });
                policyInitThread.Start();
            }
        }

        // api methods
        public UAsset FindFile(string target)
        {
            if (target.StartsWith("/Game/")) target = IntegratorUtils.ConvertGamePathToAbsolutePath(target, ".uasset");
            byte[] data1 = FindFile(target, pakExtractorForCustomRoutines, out EngineVersion engVer);
            byte[] data2 = FindFile(Path.ChangeExtension(target, ".uexp"), pakExtractorForCustomRoutines, out EngineVersion _) ?? Array.Empty<byte>();

            if (data1 == null || data2 == null || data1.Length == 0 || data2.Length == 0) throw new InvalidOperationException("Failed to find target file \"" + target + "\" (or .uexp counterpart)");

            UAsset y = new UAsset(engVer);
            y.UseSeparateBulkDataFiles = true;
            y.CustomSerializationFlags = CustomSerializationFlags.SkipPreloadDependencyLoading;
            var reader = new AssetBinaryReader(new MemoryStream(IntegratorUtils.Concatenate(data1, data2)), y);
            y.Read(reader);

            return y;
        }

        public byte[] FindFileRaw(string target, out EngineVersion engVer)
        {
            return FindFile(target, pakExtractorForCustomRoutines, out engVer);
        }

        public byte[] FindFileRaw(string target)
        {
            return FindFile(target, pakExtractorForCustomRoutines, out _);
        }

        public void AddFile(string outPath, UAsset outAsset)
        {
            if (outPath.StartsWith("/Game/")) outPath = IntegratorUtils.ConvertGamePathToAbsolutePath(outPath, ".uasset");
            FName.FromString(outAsset, "AMLC CR: " + (GetCurrentMod()?.ModID ?? "unknown mod")); // add watermark to name map for easily tracing what mods modify what files
            IntegratorUtils.SplitExportFiles(outAsset, outPath, CreatedPakDataTemp);
        }

        public void AddFileRaw(string outPath, byte[] rawData)
        {
            CreatedPakDataTemp[outPath] = rawData;
        }

        private string logCache = string.Empty;
        private bool ForceLogCacheFlush = false;

        public bool LogToDisk(string text, bool prefixWithMod = true)
        {
            try
            {
                bool usePipe = !string.IsNullOrEmpty(PakToNamedPipe);

                if (!hasLoggedOnceAlready && !usePipe)
                {
                    File.WriteAllText("ModIntegrator.log", "[" + DateTime.Now.ToString() + "] Begin ModIntegrator.log\n");
                }

                string desiredText = "[" + DateTime.Now.ToString() + "] " + (prefixWithMod ? ("[" + (GetCurrentMod()?.ModID ?? "unknown mod") + "] ") : "") + text + "\n";
                if (usePipe)
                {
                    logCache += desiredText;

                    int flushFrequency = 3000;
#if DEBUG_CUSTOMROUTINETEST
                    flushFrequency = 100;
#endif

                    if (ForceLogCacheFlush || logCache.Length > flushFrequency)
                    {
                        try
                        {
                            byte[] bytes = Encoding.UTF8.GetBytes(logCache);
                            client.WriteLine("WriteFile:Log");
                            client.WriteLine(bytes.Length.ToString());
                            client.Write(bytes);
                            client.Flush();
                        }
                        catch
                        {

                        }
                        finally
                        {
                            logCache = string.Empty;
                            ForceLogCacheFlush = false;
                        }
                    }
                }
                else
                {
                    File.AppendAllText("ModIntegrator.log", desiredText);
                }
                hasLoggedOnceAlready = true;
            }
            catch
            {
                return false;
            }
            return true;
        }

        public bool LogToDiskVerbose(string text)
        {
            if (Verbose)
            {
                return LogToDisk(text, false);
            }
            return false;
        }

        public Metadata GetCurrentMod()
        {
            return GetModFromRoutine(GetCustomRoutineFromID(currentMod));
        }

        public IReadOnlyList<Metadata> GetAllMods()
        {
            return allMods?.AsReadOnly<Metadata>();
        }

        public Metadata GetModFromRoutine(CustomRoutine routine)
        {
            if (customRoutinesMap2 == null || routine?.RoutineID == null || !customRoutinesMap2.ContainsKey(routine.RoutineID)) return null;
            return customRoutinesMap2[routine.RoutineID];
        }
        public CustomRoutine GetCustomRoutineFromID(string routineID)
        {
            if (customRoutinesMap == null || routineID == null || !customRoutinesMap.ContainsKey(routineID)) return null;
            return customRoutinesMap[routineID];
        }

        internal byte[] FindFile(string target, PakExtractor ourExtractor, out EngineVersion engVer)
        {
            engVer = IntegratorUtils.MainEngineVersion;
            if (CreatedPakData.ContainsKey(target)) return CreatedPakData[target];
            return SearchInAllPaksForPath(target, ourExtractor, false, out engVer);
        }

        internal Dictionary<string, string> SearchLookup; // file to path --> pak you can find it in
        internal void InitializeSearch(string[] realPakPaths)
        {
            SearchLookup = new Dictionary<string, string>();
            foreach (string realPakPath in realPakPaths)
            {
                using (FileStream f = new FileStream(realPakPath, FileMode.Open, FileAccess.Read))
                {
                    try
                    {
                        PakExtractor ourExtractor = new PakExtractor(f);

                        Metadata us = null;
                        try
                        {
                            us = ourExtractor.ReadMetadata();
                        }
                        catch { }

                        if (us == null || IntegratorUtils.IgnoredModIDs.Contains(us.ModID)) continue;

                        foreach (string entry in ourExtractor.GetAllPaths())
                        {
                            SearchLookup[entry] = realPakPath;
                        }

                        // no need to dispose ourExtractor
                    }
                    catch
                    {
                        continue;
                    }
                }
            }
        }

        internal EngineVersion GetEngineVersionFromMod(Metadata metadataForEngVer)
        {
            EngineVersion engVer = IntegratorUtils.MainEngineVersion;
            if (metadataForEngVer != null)
            {
                engVer = IntegratorUtils.GetEngineVersionFromAstroBuild(metadataForEngVer.GameBuild);
                if (metadataForEngVer.Download?.URL != null && metadataForEngVer.Download.URL.Contains("atenfyr.com/ams-archive"))
                {
                    // if from the ams-archive, set to 4.23
                    engVer = EngineVersion.VER_UE4_23;
                }
            }
            return engVer;
        }

        internal byte[] SearchInAllPaksForPath(string searchingPath, PakExtractor fullExtractor, bool checkMainPakFirst, out EngineVersion engVer)
        {
            engVer = IntegratorUtils.MainEngineVersion;
            if (checkMainPakFirst && fullExtractor.HasPath(searchingPath)) return fullExtractor.ReadRaw(searchingPath);
            if (SearchLookup.ContainsKey(searchingPath))
            {
                try
                {
                    using (FileStream f = new FileStream(SearchLookup[searchingPath], FileMode.Open, FileAccess.Read))
                    {
                        try
                        {
                            PakExtractor modPakExtractor = new PakExtractor(f);
                            if (modPakExtractor.HasPath(searchingPath))
                            {
                                engVer = GetEngineVersionFromMod(modPakExtractor.ReadMetadata());
                                return modPakExtractor.ReadRaw(searchingPath);
                            }
                        }
                        catch { }
                    }
                }
                catch (IOException)
                {
                    
                }
            }

            if (!checkMainPakFirst && fullExtractor.HasPath(searchingPath)) return fullExtractor.ReadRaw(searchingPath);
            return null;
        }

        /*internal bool AttemptCompatibilityMode(string pakPath, EngineVersion beforeVersion)
        {
            Dictionary<string, byte[]> outDat = new Dictionary<string, byte[]>();
            string mountPoint = null;
            using (FileStream f = new FileStream(pakPath, FileMode.Open, FileAccess.Read))
            {
                PakExtractor us = null;
                try
                {
                    us = new PakExtractor(f);
                }
                catch
                {
                    return false;
                }

                mountPoint = us.MountPoint;

                if (us.HasPath(".compatmode")) return true; // already converted

                IReadOnlyList<string> pathsInPak = us.GetAllPaths();
                foreach (string pathInPak in pathsInPak)
                {
                    byte[] dataInPathInPak = us.ReadRaw(pathInPak);

                    if (!pathInPak.EndsWith(".uasset") && !pathInPak.EndsWith(".uexp"))
                    {
                        outDat[pathInPak] = dataInPathInPak;
                        continue;
                    }

                    if (!pathInPak.EndsWith(".uasset")) continue;
                    byte[] uexpVersion = us.ReadRaw(Path.ChangeExtension(pathInPak, ".uexp"));

                    try
                    {
                        UAsset beforeAsset = new UAsset(beforeVersion);
                        beforeAsset.UseSeparateBulkDataFiles = true;
                        beforeAsset.CustomSerializationFlags = CustomSerializationFlags.SkipParsingBytecode | CustomSerializationFlags.SkipPreloadDependencyLoading;
                        beforeAsset.Read(new AssetBinaryReader(new MemoryStream(IntegratorUtils.Concatenate(dataInPathInPak, uexpVersion)), beforeAsset));

                        beforeAsset.IsUnversioned = false;
                        foreach (var cVer in beforeAsset.CustomVersionContainer) cVer.IsSerialized = true;

                        IntegratorUtils.SplitExportFiles(beforeAsset, pathInPak, outDat);
                    }
                    catch
                    {
                        outDat[pathInPak] = dataInPathInPak;
                        continue;
                    }
                }
            }

            outDat[".compatmode"] = Encoding.UTF8.GetBytes("This flag is used by AstroModLoader-Classic to avoid re-executing compatibility mode on a mod. Do not delete!");

            byte[] pakData = PakBaker.Bake(outDat, mountPoint);
            using (FileStream f = new FileStream(pakPath, FileMode.Create, FileAccess.Write))
            {
                f.Write(pakData, 0, pakData.Length);
            }

            return true;
        }

        internal void AttemptCompatibilityModeOnAll(string paksPath)
        {
            string[] files = Directory.GetFiles(paksPath, "*_P.pak", SearchOption.TopDirectoryOnly);

            foreach (string file in files)
            {
                try
                {
                    EngineVersion curVer = IntegratorUtils.MainEngineVersion;
                    using (FileStream f = new FileStream(file, FileMode.Open, FileAccess.Read))
                    {
                        Metadata us = null;
                        try
                        {
                            us = new PakExtractor(f).ReadMetadata();
                        }
                        catch
                        {
                            continue;
                        }

                        curVer = GetEngineVersionFromMod(us);
                    }

                    if (curVer < IntegratorUtils.MainEngineVersion)
                    {
                        AttemptCompatibilityMode(file, curVer);
                    }
                }
                catch
                {
                    continue;
                }
            }
        }*/

        public void IntegrateMods(string paksPath, string installPath, string outputFolder = null, string mountPoint = null, bool extractLua = false, bool cleanLua = true)
        {
            IntegrateMods([paksPath], installPath, outputFolder, mountPoint, extractLua, cleanLua);
        }

        private volatile NamedPipeClientStream client;
        public void IntegrateMods(string[] paksPaths, string installPath, string outputFolder = null, string mountPoint = null, bool extractLua = false, bool cleanLua = true)
        {
            if (isCurrentlyIntegrating) return;

            try
            {
                isCurrentlyIntegrating = true;
                IntegrateModsInternal(paksPaths, installPath, outputFolder, mountPoint, extractLua, cleanLua);
            }
            finally
            {
                if (client != null)
                {
                    client.WriteLine("WriteFile:Close");
                    client.WriteLine("Disconnect");
                    client.Flush();
                    client.Close();
                    client = null;
                }
                isCurrentlyIntegrating = false;
            }
        }

        // do not execute outside of IntegrateMods
        private void IntegrateModsInternal(string[] paksPaths, string installPath, string outputFolder = null, string mountPoint = null, bool extractLua = false, bool cleanLua = true) // @"C:\Users\<CLIENT USERNAME>\AppData\Local\Astro\Saved\Paks", @"C:\Program Files (x86)\Steam\steamapps\common\ASTRONEER\Astro\Content\Paks"
        {
            CreatedPakData = null;
            CreatedPakDataTemp = null;
            currentMod = null;
            allMods = null;
            customRoutinesMap = null;
            customRoutinesMap2 = null;
            pakExtractorForCustomRoutines = null;
            customRoutineAssemblyToMetadata = null;
            hasLoggedOnceAlready = false;
            client = null;

            // reload context if policy has already been initialized
            if (EnableCustomRoutines && EnableGlobalSandbox && policy != null)
            {
                loadContext.Unload();
                loadContext = new CasAssemblyLoader(policy, true);
            }

            bool usePipe = !string.IsNullOrEmpty(PakToNamedPipe);
            if (usePipe)
            {
                client = new NamedPipeClientStream(".", PakToNamedPipe, PipeDirection.Out);
                client.Connect(5);
                try
                {
                    client.WriteLine("WriteFile:Open");
                }
                catch (IOException)
                {
                    throw new IOException("Failed to connect to named pipe server");
                }
                try
                {
                    // send twice because the second will throw the exception if the server disconnected from the first
                    client.WriteLine("WriteFile:DisconnectIfReject");
                    client.WriteLine("WriteFile:DisconnectIfReject");
                }
                catch (IOException)
                {
                    throw new IOException("Named pipe server rejected integrator client");
                }
            }

            LogToDiskVerbose("Currently executing AstroModIntegrator Classic " + IntegratorUtils.CurrentVersion.ToString());
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) LogToDiskVerbose("Running on Windows");
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) LogToDiskVerbose("Running on OSX");
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) LogToDiskVerbose("Running on Linux");
            LogToDiskVerbose("https://github.com/atenfyr/AstroModLoader-Classic/tree/master/AstroModIntegrator");
            LogToDiskVerbose(string.Empty);

            LogToDiskVerbose("paksPaths: " + string.Join(", ", paksPaths));
            LogToDiskVerbose("installPath: " + installPath);
            LogToDiskVerbose("outputFolder: " + outputFolder ?? "null");
            LogToDiskVerbose("mountPoint: " + mountPoint ?? "null");
            LogToDiskVerbose("extractLua: " + extractLua);
            LogToDiskVerbose("cleanLua: " + cleanLua);
            LogToDiskVerbose("RefuseMismatchedConnections: " + RefuseMismatchedConnections);
            LogToDiskVerbose("OptionalModIDs: " + string.Join(", ", OptionalModIDs ?? new List<string>()));
            LogToDiskVerbose("EnableCustomRoutines: " + EnableCustomRoutines);
            LogToDiskVerbose("CallingExePath: " + CallingExePath);
            LogToDiskVerbose("Verbose: " + Verbose);
            LogToDiskVerbose("EnableGlobalSandbox: " + EnableGlobalSandbox);
            LogToDiskVerbose(string.Empty);

            foreach (string paksPath in paksPaths) Directory.CreateDirectory(paksPath);

            /*if (IntegratorUtils.CompatibilityMode)
            {
                AttemptCompatibilityModeOnAll(paksPath);
            }*/

            List<string> filesList = new List<string>();
            foreach (string paksPath in paksPaths)
            {
                filesList.AddRange(Directory.GetFiles(paksPath, "*_P.pak", paksPath.Contains("LogicMods") ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly));
            }
            filesList.Sort();
            string[] files = filesList.ToArray();

            string[] realPakPaths = Directory.GetFiles(installPath, "*.pak", SearchOption.TopDirectoryOnly);
            if (realPakPaths.Length == 0) throw new FileNotFoundException("Failed to locate any game installation pak files");
            string realPakPath = Directory.GetFiles(installPath, "pakchunk0-*.pak", SearchOption.TopDirectoryOnly)[0];

            LogToDiskVerbose("Found game installation pak: " + realPakPath);

            InitializeSearch(files);

            int modCount = 0;
            Dictionary<string, List<string>> newComponents = new Dictionary<string, List<string>>();
            Dictionary<string, Dictionary<string, List<string>>> newItems = new Dictionary<string, Dictionary<string, List<string>>>();
            List<string> newPersistentActors = new List<string>();
            List<string> newTrailheads = new List<string>();
            List<PlacementModifier> biomePlacementModifiers = new List<PlacementModifier>();
            allMods = new List<Metadata>();
            List<string> allPersistentActorMaps = DefaultMapPaths.ToList();
            List<Assembly> customRoutineAssemblies = new List<Assembly>();
            Dictionary<Guid, byte[]> assemblyBytesForDebugging = new Dictionary<Guid, byte[]>();
            customRoutineAssemblyToMetadata = new Dictionary<Guid, Metadata>();
            foreach (string file in files)
            {
                LogToDiskVerbose("Parsing " + file);

                using (FileStream f = new FileStream(file, FileMode.Open, FileAccess.Read))
                {
                    Metadata us = null;
                    try
                    {
                        PakExtractor newPakExtractor = new PakExtractor(f);

                        // read metadata, could throw error
                        us = newPakExtractor.ReadMetadata();

                        // add assembly if exists
                        string dllPath = us.IntegratorEntries.PathToCustomRoutineDLL ?? "AMLCustomRoutines.dll";
                        if (EnableCustomRoutines && newPakExtractor.HasPath(dllPath))
                        {
                            byte[] assemblyData = newPakExtractor.ReadRaw(dllPath);
                            if (assemblyData != null && assemblyData.Length > 0)
                            {
                                // wait for load context if needed
                                if (EnableCustomRoutines && EnableGlobalSandbox && (policy == null || loadContext == null))
                                {
                                    policyInitThread?.Join();
                                    policyInitThread = null;
                                }

                                Assembly newAsm = EnableGlobalSandbox ? loadContext.LoadFromStream(new MemoryStream(assemblyData)) : Assembly.Load(assemblyData);
                                customRoutineAssemblies.Add(newAsm);
                                customRoutineAssemblyToMetadata[newAsm.ManifestModule.ModuleVersionId] = us;
                                LogToDiskVerbose("Found custom routines DLL for mod " + (us?.ModID ?? file) + ", adding to list of assemblies");

#if DEBUG_CUSTOMROUTINETEST
                                assemblyBytesForDebugging[newAsm.ManifestModule.ModuleVersionId] = assemblyData;
#endif
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogToDiskVerbose("Exception while examining mod: " + ex.Message + "\n\n" + ex.StackTrace);
                        continue;
                    }

                    if (us == null || IntegratorUtils.IgnoredModIDs.Contains(us.ModID)) continue;
                    modCount++;
                    allMods.Add(us);

                    Dictionary<string, List<string>> theseComponents = us.IntegratorEntries.LinkedActorComponents;
                    if (theseComponents != null)
                    {
                        foreach (KeyValuePair<string, List<string>> entry in theseComponents)
                        {
                            if (newComponents.ContainsKey(entry.Key))
                            {
                                newComponents[entry.Key].AddRange(entry.Value);
                            }
                            else
                            {
                                newComponents.Add(entry.Key, entry.Value);
                            }
                        }
                    }

                    Dictionary<string, Dictionary<string, List<string>>> theseItems = us.IntegratorEntries.ItemListEntries;
                    if (theseItems != null)
                    {
                        // we duplicate /Game/Items/ItemTypes/MasterItemList entries into /Game/Items/ItemTypes/BaseGameInitialKnownItemList, if the latter list is not specified
                        // this provides backwards compatibility for older mods
                        // this can just be suppressed by specifying an entry for /Game/Items/ItemTypes/BaseGameInitialKnownItemList in metadata
                        if (theseItems.ContainsKey("/Game/Items/ItemTypes/MasterItemList") && !theseItems.ContainsKey("/Game/Items/ItemTypes/BaseGameInitialKnownItemList"))
                        {
                            theseItems["/Game/Items/ItemTypes/BaseGameInitialKnownItemList"] = theseItems["/Game/Items/ItemTypes/MasterItemList"];
                        }
                        if (theseItems.ContainsKey("/Game/Items/ItemTypes/MasterItemList") && !theseItems.ContainsKey("/Game/U32_Expansion/Items/GW_InitialKnownItemList"))
                        {
                            theseItems["/Game/U32_Expansion/Items/GW_InitialKnownItemList"] = theseItems["/Game/Items/ItemTypes/MasterItemList"];
                        }
                        if (theseItems.ContainsKey("/Game/Items/ItemLists/BackpackPrinterItemList") && !theseItems.ContainsKey("/Game/Items/ItemLists/BackpackPrinterItemList_GW"))
                        {
                            theseItems["/Game/Items/ItemLists/BackpackPrinterItemList_GW"] = theseItems["/Game/Items/ItemLists/BackpackPrinterItemList"];
                        }
                        if (theseItems.ContainsKey("/Game/Items/ItemLists/T1PrinterItemList") && !theseItems.ContainsKey("/Game/Items/ItemLists/T1PrinterItemList_GW"))
                        {
                            theseItems["/Game/Items/ItemLists/T1PrinterItemList_GW"] = theseItems["/Game/Items/ItemLists/T1PrinterItemList"];
                        }
                        if (theseItems.ContainsKey("/Game/Items/ItemLists/T2PrinterItemList") && !theseItems.ContainsKey("/Game/Items/ItemLists/T2PrinterItemList_GW"))
                        {
                            theseItems["/Game/Items/ItemLists/T2PrinterItemList_GW"] = theseItems["/Game/Items/ItemLists/T2PrinterItemList"];
                        }

                        // parse as normal
                        foreach (KeyValuePair<string, Dictionary<string, List<string>>> entry in theseItems)
                        {
                            if (newItems.ContainsKey(entry.Key))
                            {
                                foreach (KeyValuePair<string, List<string>> entry2 in entry.Value)
                                {
                                    if (newItems[entry.Key].ContainsKey(entry2.Key))
                                    {
                                        newItems[entry.Key][entry2.Key].AddRange(entry2.Value);
                                    }
                                    else
                                    {
                                        newItems[entry.Key].Add(entry2.Key, entry2.Value);
                                    }
                                }
                            }
                            else
                            {
                                newItems.Add(entry.Key, entry.Value);
                            }
                        }
                    }

                    List<string> thesePersistentActors = us.IntegratorEntries.PersistentActors;
                    if (thesePersistentActors != null)
                    {
                        newPersistentActors.AddRange(thesePersistentActors);
                    }

                    List<string> thesePersistentActorMaps = us.IntegratorEntries.PersistentActorMaps;
                    if (thesePersistentActorMaps != null)
                    {
                        allPersistentActorMaps.AddRange(thesePersistentActorMaps);
                    }

                    List<string> theseTrailheads = us.IntegratorEntries.MissionTrailheads;
                    if (theseTrailheads != null)
                    {
                        newTrailheads.AddRange(theseTrailheads);
                    }

                    List<PlacementModifier> placementModifiers = us.IntegratorEntries.BiomePlacementModifiers;
                    if (placementModifiers != null)
                    {
                        biomePlacementModifiers.AddRange(placementModifiers);
                    }
                }
            }

            // correct any package names in allPersistentActorsMap
            for (int i = 0; i < allPersistentActorMaps.Count; i++)
            {
                string corrected = IntegratorUtils.ConvertGamePathToAbsolutePath(allPersistentActorMaps[i], ".umap");
                if (!string.IsNullOrEmpty(corrected)) allPersistentActorMaps[i] = corrected;
            }
            // remove duplicates
            allPersistentActorMaps = allPersistentActorMaps.Distinct().ToList();

            CreatedPakData = new Dictionary<string, byte[]>
            {
                { "metadata.json", StarterPakData["metadata.json"] }
            };

            if (modCount > 0)
            {
                LogToDiskVerbose("Adding static files");

                // Apply static files
                CreatedPakData = StarterPakData.ToDictionary(entry => entry.Key, entry => (byte[])entry.Value.Clone());

                // attach ServerModComponent to PlayControllerInstance
                if (!newComponents.ContainsKey("/Game/Globals/PlayControllerInstance")) newComponents.Add("/Game/Globals/PlayControllerInstance", new List<string>());
                newComponents["/Game/Globals/PlayControllerInstance"].Add("/Game/Integrator/ServerModComponent");

                // add NotificationActor to default maps
                /*foreach (string map in DefaultMapPaths)
                {
                    if (!newPersistentActors.ContainsKey(map)) newPersistentActors[map] = new List<string>();
                    newPersistentActors[map].Add("/Game/Integrator/NotificationActor");
                }*/

                // Generate mods data table
                var dtb = new DataTableBaker(this);
                IntegratorUtils.SplitExportFiles(dtb.Bake(allMods.ToArray(), OptionalModIDs, IntegratorUtils.Concatenate(CreatedPakData["Astro/Content/Integrator/ListOfMods.uasset"], CreatedPakData["Astro/Content/Integrator/ListOfMods.uexp"]), IntegratorUtils.MainEngineVersion), "Astro/Content/Integrator/ListOfMods.uasset", CreatedPakData);
                IntegratorUtils.SplitExportFiles(dtb.Bake2(IntegratorUtils.Concatenate(CreatedPakData["Astro/Content/Integrator/IntegratorStatics_BP.uasset"], CreatedPakData["Astro/Content/Integrator/IntegratorStatics_BP.uexp"]), IntegratorUtils.MainEngineVersion), "Astro/Content/Integrator/IntegratorStatics_BP.uasset", CreatedPakData);
                IntegratorUtils.SplitExportFiles(dtb.Bake3(IntegratorUtils.Concatenate(CreatedPakData["Astro/Content/Integrator/IntegratorStatics.uasset"], CreatedPakData["Astro/Content/Integrator/IntegratorStatics.uexp"]), IntegratorUtils.MainEngineVersion), "Astro/Content/Integrator/IntegratorStatics.uasset", CreatedPakData);
            }

            using (FileStream f = new FileStream(realPakPath, FileMode.Open, FileAccess.Read))
            {
                PakExtractor ourExtractor = null;
                try
                {
                    ourExtractor = new PakExtractor(f);
                }
                catch { }

                if (ourExtractor != null)
                {
                    // See if we can find the current version in this pak
                    byte[] defaultGameIni = ourExtractor.ReadRaw("Astro/Config/DefaultGame.ini");
                    if (defaultGameIni != null && defaultGameIni.Length > 0)
                    {
                        string iniIndicatedVersionStr = IniParser.FindLine(Encoding.UTF8.GetString(defaultGameIni), "/Script/EngineSettings.GeneralProjectSettings", "ProjectVersion");
                        if (iniIndicatedVersionStr != null) Version.TryParse(iniIndicatedVersionStr, out DetectedAstroBuild);
                    }
                    LogToDiskVerbose("Detected game version: " + (DetectedAstroBuild?.ToString() ?? "unknown"));

                    var actorBaker = new ActorBaker();
                    var itemListBaker = new ItemListBaker();
                    var bpmBaker = new BiomePlacementModifiersBaker();
                    var levelBaker = new LevelBaker(ourExtractor, this);

                    // add version to GUI
                    var gmdoBaker = new GameMenuDisplayOptionsBaker(this);
                    string gmdoPath = "Astro/Content/UI/PauseMenu/SubMenus/GameMenuOptionsSubmenu.uasset";
                    byte[] gmdoData1 = FindFile(gmdoPath, ourExtractor, out EngineVersion gmdoEngVer);
                    byte[] gmdoData2 = FindFile(Path.ChangeExtension(gmdoPath, ".uexp"), ourExtractor, out EngineVersion _) ?? Array.Empty<byte>();
                    if (gmdoData1 != null && gmdoData1.Length != 0)
                    {
                        IntegratorUtils.SplitExportFiles(gmdoBaker.Bake(IntegratorUtils.Concatenate(gmdoData1, gmdoData2), gmdoEngVer), gmdoPath, CreatedPakData);
                        LogToDiskVerbose("Baked GameMenuOptionsSubmenu");
                    }
                    else
                    {
                        LogToDiskVerbose("Failed to bake GameMenuOptionsSubmenu");
                    }

                    // Patch levels for biome placement modifiers and missions, as well as persistent actors if we can just do that here to avoid reading twice
                    if (biomePlacementModifiers.Count > 0 || newTrailheads.Count > 0)
                    {
                        foreach (string path in DefaultMapPaths)
                        {
                            byte[] mapPathData1 = FindFile(path, ourExtractor, out EngineVersion engVer);
                            byte[] mapPathData2 = FindFile(Path.ChangeExtension(path, ".uexp"), ourExtractor, out EngineVersion _) ?? Array.Empty<byte>();
                            UAsset baked = bpmBaker.Bake(biomePlacementModifiers, newTrailheads.ToArray(), IntegratorUtils.Concatenate(mapPathData1, mapPathData2), engVer, out AssetBinaryReader mapReader);
                            if (allPersistentActorMaps.Contains(path))
                            {
                                baked = levelBaker.Bake(newPersistentActors.ToArray(), baked, mapReader);
                                allPersistentActorMaps.Remove(path); // avoid re-visiting this map again later
                                mapReader.Dispose();
                            }
                            if (mapPathData1 != null)
                            {
                                IntegratorUtils.SplitExportFiles(baked, path, CreatedPakData);
                                LogToDiskVerbose("Baked BPM/missions/persistent actors for " + path);
                            }
                            else
                            {
                                LogToDiskVerbose("Failed to bake BPM/missions/persistent actors for " + path);
                            }
                        }
                    }

                    // Patch levels for remaining persistent actors
                    if (allPersistentActorMaps.Count > 0)
                    {
                        foreach (string mapPath in allPersistentActorMaps)
                        {
                            byte[] mapPathData1 = FindFile(mapPath, ourExtractor, out EngineVersion engVer);
                            byte[] mapPathData2 = FindFile(Path.ChangeExtension(mapPath, ".uexp"), ourExtractor, out EngineVersion _) ?? Array.Empty<byte>();
                            if (mapPathData1 != null)
                            {
                                IntegratorUtils.SplitExportFiles(levelBaker.Bake(newPersistentActors.ToArray(), IntegratorUtils.Concatenate(mapPathData1, mapPathData2), engVer), mapPath, CreatedPakData);
                                LogToDiskVerbose("Baked persistent actors for " + mapPath);
                            }
                            else
                            {
                                LogToDiskVerbose("Failed to bake persistent actors for " + mapPath);
                            }
                        }
                    }

                    // Add components
                    foreach (KeyValuePair<string, List<string>> entry in newComponents)
                    {
                        string establishedPath = entry.Key.ConvertGamePathToAbsolutePath();

                        byte[] actorData1 = FindFile(establishedPath, ourExtractor, out EngineVersion engVer);
                        byte[] actorData2 = FindFile(Path.ChangeExtension(establishedPath, ".uexp"), ourExtractor, out EngineVersion _) ?? Array.Empty<byte>();
                        if (actorData1 == null)
                        {
                            LogToDiskVerbose("Failed to find target actor " + establishedPath + " for baking components");
                            continue;
                        }
                        try
                        {
                            IntegratorUtils.SplitExportFiles(actorBaker.Bake(entry.Value.ToArray(), IntegratorUtils.Concatenate(actorData1, actorData2), engVer), establishedPath, CreatedPakData);
                            LogToDiskVerbose("Baked components for target actor " + establishedPath);
                        }
                        catch (Exception ex)
                        {
                            LogToDiskVerbose("Failed to bake components: " + ex.ToString());
                        }
                    }

                    // Add new item entries
                    foreach (KeyValuePair<string, Dictionary<string, List<string>>> entry in newItems)
                    {
                        string establishedPath = entry.Key.ConvertGamePathToAbsolutePath();

                        byte[] actorData1 = FindFile(establishedPath, ourExtractor, out EngineVersion engVer);
                        byte[] actorData2 = FindFile(Path.ChangeExtension(establishedPath, ".uexp"), ourExtractor, out EngineVersion _) ?? Array.Empty<byte>();
                        if (actorData1 == null)
                        {
                            LogToDiskVerbose("Failed to find target asset " + establishedPath + " for baking item entries");
                        }
                        try
                        {
                            IntegratorUtils.SplitExportFiles(itemListBaker.Bake(entry.Value, IntegratorUtils.Concatenate(actorData1, actorData2), engVer), establishedPath, CreatedPakData);
                            LogToDiskVerbose("Baked item entries for target asset " + establishedPath);
                        }
                        catch (Exception ex)
                        {
                            LogToDiskVerbose("Failed to bake item entries: " + ex.ToString());
                        }
                    }

#if DEBUG_CUSTOMROUTINETEST
                    // if Debug_CustomRoutineTest then also load AMLCustomRoutines.dll if we can find it
                    // repeatedly look in higher directories (up to 7) until we find AMLCustomRoutines.dll or *.sln
                    if (EnableCustomRoutines)
                    {
                        LogToDiskVerbose("Searching for AMLCustomRoutines.dll");

                        bool foundDll = false;
                        string currentTestPath = CallingExePath ?? Directory.GetCurrentDirectory();
                        for (int i = 0; i < 7; i++)
                        {
                            LogToDiskVerbose("Trying " + currentTestPath);

                            try
                            {
                                string[] allPossibleDlls = Directory.GetFiles(currentTestPath, "AMLCustomRoutines.dll", SearchOption.AllDirectories);
                                for (int j = 0; j < allPossibleDlls.Length; j++)
                                {
                                    if (i == 0 || (allPossibleDlls[j].Contains("Debug_CustomRoutineTest") && allPossibleDlls[j].Contains("bin")))
                                    {
                                        // wait for load context if needed
                                        if (EnableCustomRoutines && EnableGlobalSandbox && (policy == null || loadContext == null))
                                        {
                                            policyInitThread?.Join();
                                            policyInitThread = null;
                                        }

                                        string chosenDll = allPossibleDlls[j];
                                        byte[] byts = File.ReadAllBytes(chosenDll);
                                        Assembly newAsm = EnableGlobalSandbox ? loadContext.LoadFromStream(new MemoryStream(byts)) : Assembly.Load(byts);
                                        customRoutineAssemblies.Add(newAsm);
                                        customRoutineAssemblyToMetadata[newAsm.ManifestModule.ModuleVersionId] = new Metadata() { ModID = "AMLCustomRoutines" }; // dummy metadata

#if DEBUG_CUSTOMROUTINETEST
                                        assemblyBytesForDebugging[newAsm.ManifestModule.ModuleVersionId] = byts;
#endif

                                        foundDll = true;
                                        LogToDiskVerbose("Found AMLCustomRoutines.dll, adding to list of assemblies");
                                        break;
                                    }
                                }

                                if (Directory.GetFiles(currentTestPath, "*.sln", SearchOption.TopDirectoryOnly).Length > 0) break; // don't go any farther back if we have a sln file here
                            }
                            catch
                            {
                                // whatever
                            }

                            currentTestPath = Directory.GetParent(currentTestPath)?.FullName;
                            if (currentTestPath == null) break;
                        }

                        if (!foundDll)
                        {
                            LogToDiskVerbose("Failed to find AMLCustomRoutines.dll, skipping");
                        }
                    }
#endif

                    // custom routines
                    if (EnableCustomRoutines)
                    {
                        // add mod integrator assembly itself so we can make internal custom routines if desired
                        Assembly selfAsm = typeof(ModIntegrator).Assembly;
                        customRoutineAssemblies.Add(selfAsm);
                        customRoutineAssemblyToMetadata[selfAsm.ManifestModule.ModuleVersionId] = new Metadata() { ModID = "AstroModIntegrator" };

                        pakExtractorForCustomRoutines = ourExtractor;
                        currentMod = null;
                        customRoutinesMap = new Dictionary<string, CustomRoutine>();
                        customRoutinesMap2 = new Dictionary<string, Metadata>();
                        List<CustomRoutine> customRoutineInstances = new List<CustomRoutine>();
                        for (int i = 0; i < customRoutineAssemblies.Count; i++)
                        {
                            Type[] alCRTypes = customRoutineAssemblies[i].GetTypes().Where(t => t.IsSubclassOf(typeof(CustomRoutine))).ToArray();
                            for (int j = 0; j < alCRTypes.Length; j++)
                            {
                                try
                                {
                                    Type currentCRType = alCRTypes[j];
                                    if (currentCRType == null || currentCRType.ContainsGenericParameters) continue;

                                    CustomRoutine customRoutineInstance = Activator.CreateInstance(currentCRType) as CustomRoutine;
                                    if (customRoutineInstance == null) continue;

#if DEBUG_CUSTOMROUTINETEST
                                    if (EnableGlobalSandbox && customRoutineInstance.RequestNoSandbox)
                                    {
                                        Assembly asm2 = Assembly.Load(assemblyBytesForDebugging[customRoutineAssemblies[i].ManifestModule.ModuleVersionId]);
                                        Type[] crType2 = asm2.GetTypes().Where(t => t.Name == currentCRType.Name).ToArray();

                                        customRoutineInstance = null;
                                        if (crType2.Length > 0 && crType2[0] != null && !crType2[0].ContainsGenericParameters) customRoutineInstance = Activator.CreateInstance(crType2[0]) as CustomRoutine;
                                    }
                                    if (customRoutineInstance == null) continue;
#endif

                                    if (customRoutineInstance.RoutineID == null || customRoutineInstance.RoutineID == "None") continue;
                                    if (!customRoutineInstance.Enabled) continue;

                                    customRoutineInstances.Add(customRoutineInstance);
                                    customRoutinesMap[customRoutineInstance.RoutineID] = customRoutineInstance;

                                    Metadata mod = customRoutineAssemblyToMetadata[customRoutineAssemblies[i].ManifestModule.ModuleVersionId];
                                    customRoutinesMap2[customRoutineInstance.RoutineID] = mod;
                                }
                                catch (Exception ex)
                                {
                                    LogToDisk(ex.Message + "\n" + (ex.StackTrace ?? "null") + "\n" + (ex.InnerException?.Message ?? "null") + "\n" + (ex.InnerException?.StackTrace ?? "null"), true);
#if DEBUG_CUSTOMROUTINETEST
                                    throw;
#else
                                    continue;
#endif
                                }
                            }
                        }

                        assemblyBytesForDebugging.Clear();

                        LogToDiskVerbose("Executing " + customRoutineInstances.Count + " custom routines");
                        for (int i = 0; i < customRoutineInstances.Count; i++)
                        {
                            CustomRoutine customRoutineInstance = customRoutineInstances[i];
                            try
                            {
                                currentMod = customRoutineInstance.RoutineID;
                                Metadata currentModMetadata = GetCurrentMod();

                                string modId = currentModMetadata?.ModID ?? "unknown mod";
                                LogToDiskVerbose("Executing custom routine " + customRoutineInstance.RoutineID + " for mod " + modId);

                                CreatedPakDataTemp = new Dictionary<string, byte[]>();

                                CustomRoutineAPIWrapper apiWrapper = new CustomRoutineAPIWrapper(this);
                                Thread workerThread = new Thread(() =>
                                {
                                    try
                                    {
                                        customRoutineInstance.Execute(apiWrapper);
                                    }
                                    catch (Exception ex)
                                    {
                                        LogToDisk("[" + modId + "] " + ex.Message + "\n" + (ex.StackTrace ?? "null") + "\n" + (ex.InnerException?.Message ?? "null") + "\n" + (ex.InnerException?.StackTrace ?? "null"), false); // the method could get incorrect mod id so we prepend the mod id manually
#if DEBUG_CUSTOMROUTINETEST
                                            throw;
#endif
                                    }
                                });
                                workerThread.Start();

#if DEBUG_CUSTOMROUTINETEST
                                // in Debug_CustomRoutineTest, no time limit because of breakpoints
                                workerThread.Join();
                                bool terminated = true;
#else
                                bool terminated = workerThread.Join(TimeSpan.FromSeconds(5));
                                if (!terminated)
                                {
                                    // if the thread is still going, tell it that it should exit and give another 2 seconds to try and cleanly exit
                                    // no matter what terminated = false still so that we don't keep changes
                                    LogToDisk("[" + modId + "] Custom routine is taking too long; discarding changes and attempting to cancel safely", false);
                                    apiWrapper.SetShouldExitNow(true);
                                    workerThread.Join(TimeSpan.FromSeconds(2));
                                }
#endif
                                if (terminated)
                                {
                                    // copy saved assets
                                    foreach (KeyValuePair<string, byte[]> entry in CreatedPakDataTemp) CreatedPakData[entry.Key] = entry.Value;
                                }
                                else
                                {
                                    LogToDisk("[" + modId + "] Custom routine is unresponsive; discarding changes, disabling API, and moving on", false);
                                    if (IsModIntegratorCMD) LogToDisk("[" + modId + "] Thread will be killed after integration", false);
                                }
                                apiWrapper.SetEnabled(false); // disable all api methods
                            }
                            catch (Exception ex)
                            {
                                LogToDisk(ex.Message + "\n" + (ex.StackTrace ?? "null") + "\n" + (ex.InnerException?.Message ?? "null") + "\n" + (ex.InnerException?.StackTrace ?? "null"), true);
#if DEBUG_CUSTOMROUTINETEST
                                    throw;
#else
                                continue;
#endif
                            }
                        }
                    }
                }
            }

            // final save
            outputFolder = outputFolder ?? paksPaths[0];

            LogToDiskVerbose("Writing final integrator .pak file");
            byte[] pakData = PakBaker.Bake(CreatedPakData, mountPoint);

            if (usePipe)
            {
                client.WriteLine("WriteFile:ClientTransmitIntegratorPak");
                client.WriteLine(pakData.Length.ToString());
                client.Write(pakData, 0, pakData.Length);
                client.Flush();

                LogToDiskVerbose("Wrote to named pipe " + PakToNamedPipe);
            }
            else
            {
                using (FileStream f = new FileStream(Path.Combine(outputFolder, @"999-AstroModIntegrator_P.pak"), FileMode.Create, FileAccess.Write))
                {
                    f.Write(pakData, 0, pakData.Length);
                }
                LogToDiskVerbose("Wrote to disk");
            }

            if (extractLua)
            {
                LogToDiskVerbose("Extracting UE4SS mods");
                string luaDir = Path.Combine(outputFolder, "UE4SS");
                if (cleanLua)
                {
                    try
                    {
                        Directory.Delete(luaDir, true);
                    }
                    catch { }
                }

                if (usePipe)
                {
                    client.WriteLine("WriteFile:ClientTransmitUE4SSMods");
                }

                foreach (string file in files)
                {
                    using (FileStream f = new FileStream(file, FileMode.Open, FileAccess.Read))
                    {
                        PakExtractor us = null;
                        try
                        {
                            us = new PakExtractor(f);
                        }
                        catch
                        {
                            continue;
                        }

                        Metadata mtd = us.ReadMetadata();
                        if (mtd == null || IntegratorUtils.IgnoredModIDs.Contains(mtd.ModID) || !mtd.EnableUE4SS) continue;

                        // extract any files that exists in a root folder called UE4SS into the Lua\ModID folder
                        foreach (string subPath in us.GetAllPaths())
                        {
                            if (subPath.StartsWith("UE4SS/"))
                            {
                                if (usePipe)
                                {
                                    byte[] rawData = us.ReadRaw(subPath);
                                    client.WriteLine("!!!File");
                                    client.WriteLine(mtd.ModID);
                                    client.WriteLine(subPath.Substring(6));
                                    client.WriteLine(rawData.Length.ToString());
                                    client.Write(rawData, 0, rawData.Length);
                                }
                                else
                                {
                                    string newPath = Path.Combine(outputFolder, "UE4SS", mtd.ModID, subPath.Substring(6));
                                    Directory.CreateDirectory(Path.GetDirectoryName(newPath));
                                    File.WriteAllBytes(newPath, us.ReadRaw(subPath));
                                }
                            }
                        }
                    }
                }

                if (usePipe)
                {
                    client.WriteLine("!!!Stop");
                    client.WriteLine("!!!Stop");
                    client.WriteLine("!!!Stop");
                    client.WriteLine("Next");
                }

                if (Path.Exists(luaDir))
                {
                    StringBuilder modsTxt = new StringBuilder();
                    string[] luaDirPaths = Directory.GetDirectories(luaDir);
                    foreach (string luaDirPath in luaDirPaths)
                    {
                        modsTxt.AppendLine(Path.GetFileNameWithoutExtension(luaDirPath) + " : 1");
                    }
                    string modsTxtAsStr = modsTxt.ToString();

                    if (usePipe)
                    {
                        byte[] bytes = Encoding.ASCII.GetBytes(modsTxtAsStr);
                        client.WriteLine(bytes.Length.ToString());
                        client.Write(bytes, 0, bytes.Length);
                    }
                    else
                    {
                        File.WriteAllText(Path.Combine(luaDir, "mods.txt"), modsTxtAsStr);
                    }

                    // extra libraries
                    // UEHelpers taken from UE4SS, see NOTICE.md for more information
                    if (usePipe)
                    {
                        client.WriteLine(Properties.Resources.UEHelpers.Length.ToString());
                        client.Write(Properties.Resources.UEHelpers, 0, Properties.Resources.UEHelpers.Length);
                        client.WriteLine(Properties.Resources.AstroHelpers.Length.ToString());
                        client.Write(Properties.Resources.AstroHelpers, 0, Properties.Resources.AstroHelpers.Length);
                    }
                    else
                    {
                        Directory.CreateDirectory(Path.Combine(luaDir, "shared", "UEHelpers"));
                        Directory.CreateDirectory(Path.Combine(luaDir, "shared", "AstroHelpers"));
                        File.WriteAllBytes(Path.Combine(luaDir, "shared", "UEHelpers", "UEHelpers.lua"), Properties.Resources.UEHelpers);
                        File.WriteAllBytes(Path.Combine(luaDir, "shared", "AstroHelpers", "AstroHelpers.lua"), Properties.Resources.AstroHelpers);
                    }
                }
                else if (usePipe)
                {
                    client.WriteLine("0"); // mods.txt
                    client.WriteLine("0"); // UEHelpers.lua
                    client.WriteLine("0"); // AstroHelpers.lua
                }
            }

            LogToDiskVerbose("All done");

            if (usePipe && hasLoggedOnceAlready)
            {
                ForceLogCacheFlush = true;
                LogToDisk("Flushing logs", false);
            }
        }

        private Dictionary<string, byte[]> StarterPakData = new Dictionary<string, byte[]>();
        public ModIntegrator()
        {
            OptionalModIDs = new List<string>();

            // Include static assets
            StarterPakData.Clear();
            PakExtractor staticAssetsExtractor = new PakExtractor(new MemoryStream(Properties.Resources.IntegratorStaticAssets));
            foreach (string entry in staticAssetsExtractor.GetAllPaths())
            {
                StarterPakData[entry] = staticAssetsExtractor.ReadRaw(entry);
            }
        }
    }
}
