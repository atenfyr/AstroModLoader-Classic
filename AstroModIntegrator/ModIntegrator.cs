using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using UAssetAPI;

namespace AstroModIntegrator
{
    public class ModIntegrator
    {
        // Settings //
        public bool RefuseMismatchedConnections;
        public List<string> OptionalModIDs;
        // End Settings //

        // Exposed Fields //
        public Version DetectedAstroBuild;
        // End Exposed Fields //

        private static string[] DefaultMapPaths = [
            "Astro/Content/Maps/Staging_T2.umap",
            "Astro/Content/Maps/Staging_T2_PackedPlanets_Switch.umap",
            "Astro/Content/U32_Expansion/U32_Expansion.umap" // DLC map
        ];

        internal byte[] FindFile(string target, PakExtractor ourExtractor)
        {
            if (CreatedPakData.ContainsKey(target)) return CreatedPakData[target];
            return SearchInAllPaksForPath(target, ourExtractor, false);
        }

        internal Dictionary<string, string> SearchLookup; // file to path --> pak you can find it in
        internal void InitializeSearch(string installPath)
        {
            SearchLookup = new Dictionary<string, string>();
            string[] realPakPaths = Directory.GetFiles(installPath, "*_P.pak", SearchOption.TopDirectoryOnly);
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

        internal byte[] SearchInAllPaksForPath(string searchingPath, PakExtractor fullExtractor, bool checkMainPakFirst = true)
        {
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
                            if (modPakExtractor.HasPath(searchingPath)) return modPakExtractor.ReadRaw(searchingPath);
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

        private Dictionary<string, byte[]> CreatedPakData;

        public void IntegrateMods(string paksPath, string installPath, string outputFolder = null, string mountPoint = null) // @"C:\Users\<CLIENT USERNAME>\AppData\Local\Astro\Saved\Paks", @"C:\Program Files (x86)\Steam\steamapps\common\ASTRONEER\Astro\Content\Paks"
        {
            Directory.CreateDirectory(paksPath);
            string[] files = Directory.GetFiles(paksPath, "*_P.pak", SearchOption.TopDirectoryOnly);

            string[] realPakPaths = Directory.GetFiles(installPath, "*.pak", SearchOption.TopDirectoryOnly);
            if (realPakPaths.Length == 0) throw new FileNotFoundException("Failed to locate any game installation pak files");
            string realPakPath = Directory.GetFiles(installPath, "Astro-*.pak", SearchOption.TopDirectoryOnly)[0];

            InitializeSearch(paksPath);

            int modCount = 0;
            Dictionary<string, List<string>> newComponents = new Dictionary<string, List<string>>();
            Dictionary<string, Dictionary<string, List<string>>> newItems = new Dictionary<string, Dictionary<string, List<string>>>();
            Dictionary<string, List<string>> newPersistentActors = new Dictionary<string, List<string>>();
            List<string> newTrailheads = new List<string>();
            List<PlacementModifier> biomePlacementModifiers = new List<PlacementModifier>();
            List<Metadata> allMods = new List<Metadata>();
            foreach (string file in files)
            {
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
                        List<string> mapPaths = us.IntegratorEntries.PersistentActorMaps ?? DefaultMapPaths.ToList();
                        foreach (string mapPath in mapPaths)
                        {
                            if (!newPersistentActors.ContainsKey(mapPath)) newPersistentActors[mapPath] = new List<string>();
                            newPersistentActors[mapPath].AddRange(thesePersistentActors);
                        }
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

            CreatedPakData = new Dictionary<string, byte[]>
            {
                { "metadata.json", StarterPakData["metadata.json"] }
            };

            if (modCount > 0)
            {
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
                IntegratorUtils.SplitExportFiles(dtb.Bake(allMods.ToArray(), OptionalModIDs, IntegratorUtils.Concatenate(CreatedPakData["Astro/Content/Integrator/ListOfMods.uasset"], CreatedPakData["Astro/Content/Integrator/ListOfMods.uexp"])), "Astro/Content/Integrator/ListOfMods.uasset", CreatedPakData);
                IntegratorUtils.SplitExportFiles(dtb.Bake2(IntegratorUtils.Concatenate(CreatedPakData["Astro/Content/Integrator/IntegratorStatics_BP.uasset"], CreatedPakData["Astro/Content/Integrator/IntegratorStatics_BP.uexp"])), "Astro/Content/Integrator/IntegratorStatics_BP.uasset", CreatedPakData);
                IntegratorUtils.SplitExportFiles(dtb.Bake3(IntegratorUtils.Concatenate(CreatedPakData["Astro/Content/Integrator/IntegratorStatics.uasset"], CreatedPakData["Astro/Content/Integrator/IntegratorStatics.uexp"])), "Astro/Content/Integrator/IntegratorStatics.uasset", CreatedPakData);
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

                    var actorBaker = new ActorBaker();
                    var itemListBaker = new ItemListBaker();
                    var bpmBaker = new BiomePlacementModifiersBaker();
                    var levelBaker = new LevelBaker(ourExtractor, this);

                    // Patch levels for biome placement modifiers and missions, as well as persistent actors if we can just do that here to avoid reading twice
                    if (biomePlacementModifiers.Count > 0 || newTrailheads.Count > 0)
                    {
                        foreach (string path in DefaultMapPaths)
                        {
                            byte[] mapPathData1 = FindFile(path, ourExtractor);
                            byte[] mapPathData2 = FindFile(Path.ChangeExtension(path, ".uexp"), ourExtractor) ?? Array.Empty<byte>();
                            UAsset baked = bpmBaker.Bake(biomePlacementModifiers, newTrailheads.ToArray(), IntegratorUtils.Concatenate(mapPathData1, mapPathData2), out AssetBinaryReader mapReader);
                            if (newPersistentActors.ContainsKey(path))
                            {
                                baked = levelBaker.Bake(newPersistentActors[path].ToArray(), baked, mapReader);
                                newPersistentActors.Remove(path); // avoid re-visiting this map again later
                                mapReader.Dispose();
                            }
                            if (mapPathData1 != null) IntegratorUtils.SplitExportFiles(baked, path, CreatedPakData);
                        }
                    }

                    // Patch levels for remaining persistent actors
                    if (newPersistentActors.Count > 0)
                    {
                        foreach (KeyValuePair<string, List<string>> entry in newPersistentActors)
                        {
                            byte[] mapPathData1 = FindFile(entry.Key, ourExtractor);
                            byte[] mapPathData2 = FindFile(Path.ChangeExtension(entry.Key, ".uexp"), ourExtractor) ?? Array.Empty<byte>();
                            if (mapPathData1 != null) IntegratorUtils.SplitExportFiles(levelBaker.Bake(entry.Value.ToArray(), IntegratorUtils.Concatenate(mapPathData1, mapPathData2)), entry.Key, CreatedPakData);
                        }
                    }

                    // Add components
                    foreach (KeyValuePair<string, List<string>> entry in newComponents)
                    {
                        string establishedPath = entry.Key.ConvertGamePathToAbsolutePath();

                        byte[] actorData1 = FindFile(establishedPath, ourExtractor);
                        byte[] actorData2 = FindFile(Path.ChangeExtension(establishedPath, ".uexp"), ourExtractor) ?? Array.Empty<byte>();
                        if (actorData1 == null) continue;
                        try
                        {
                            IntegratorUtils.SplitExportFiles(actorBaker.Bake(entry.Value.ToArray(), IntegratorUtils.Concatenate(actorData1, actorData2)), establishedPath, CreatedPakData);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex.ToString());
                        }
                    }

                    // Add new item entries
                    foreach (KeyValuePair<string, Dictionary<string, List<string>>> entry in newItems)
                    {
                        string establishedPath = entry.Key.ConvertGamePathToAbsolutePath();

                        byte[] actorData1 = FindFile(establishedPath, ourExtractor);
                        byte[] actorData2 = FindFile(Path.ChangeExtension(establishedPath, ".uexp"), ourExtractor) ?? Array.Empty<byte>();
                        if (actorData1 == null) continue;
                        try
                        {
                            IntegratorUtils.SplitExportFiles(itemListBaker.Bake(entry.Value, IntegratorUtils.Concatenate(actorData1, actorData2)), establishedPath, CreatedPakData);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex.ToString());
                        }
                    }
                }
            }

            byte[] pakData = PakBaker.Bake(CreatedPakData, mountPoint);

            using (FileStream f = new FileStream(Path.Combine(outputFolder ?? paksPath, @"999-AstroModIntegrator_P.pak"), FileMode.Create, FileAccess.Write))
            {
                f.Write(pakData, 0, pakData.Length);
            }
        }

        private Dictionary<string, byte[]> StarterPakData = new Dictionary<string, byte[]>();
        public ModIntegrator()
        {
            OptionalModIDs = new List<string>();

            // Include static assets
            PakExtractor staticAssetsExtractor = new PakExtractor(new MemoryStream(Properties.Resources.IntegratorStaticAssets));
            foreach (string entry in staticAssetsExtractor.GetAllPaths())
            {
                StarterPakData[entry] = staticAssetsExtractor.ReadRaw(entry);
            }
        }
    }
}
