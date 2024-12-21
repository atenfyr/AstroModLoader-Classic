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

        public void IntegrateMods(string paksPath, string installPath, string outputFolder = null, string mountPoint = null, bool extractLua = false) // @"C:\Users\<CLIENT USERNAME>\AppData\Local\Astro\Saved\Paks", @"C:\Program Files (x86)\Steam\steamapps\common\ASTRONEER\Astro\Content\Paks"
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

            outputFolder = outputFolder ?? paksPath;
            using (FileStream f = new FileStream(Path.Combine(outputFolder, @"999-AstroModIntegrator_P.pak"), FileMode.Create, FileAccess.Write))
            {
                f.Write(pakData, 0, pakData.Length);
            }

            if (extractLua)
            {
                string luaDir = Path.Combine(outputFolder, "Lua");
                try
                {
                    Directory.Delete(luaDir, true);
                }
                catch { }

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
                                string newPath = Path.Combine(outputFolder, "Lua", mtd.ModID, subPath.Substring(6));
                                Directory.CreateDirectory(Path.GetDirectoryName(newPath));
                                File.WriteAllBytes(newPath, us.ReadRaw(subPath));
                            }
                        }
                    }
                }

                if (Path.Exists(luaDir))
                {
                    StringBuilder modsTxt = new StringBuilder();
                    string[] luaDirPaths = Directory.GetDirectories(luaDir);
                    foreach (string luaDirPath in luaDirPaths)
                    {
                        modsTxt.AppendLine(Path.GetFileNameWithoutExtension(luaDirPath) + " : 1");
                    }
                    File.WriteAllText(Path.Combine(luaDir, "mods.txt"), modsTxt.ToString());

                    // also add UEHelpers, taken from UE4SS: https://github.com/UE4SS-RE/RE-UE4SS
                    /*
                     * MIT License

                    Copyright (c) 2022 Narknon

                    Permission is hereby granted, free of charge, to any person obtaining a copy
                    of this software and associated documentation files (the "Software"), to deal
                    in the Software without restriction, including without limitation the rights
                    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
                    copies of the Software, and to permit persons to whom the Software is
                    furnished to do so, subject to the following conditions:

                    The above copyright notice and this permission notice shall be included in all
                    copies or substantial portions of the Software.

                    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
                    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
                    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
                    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
                    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
                    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
                    SOFTWARE.
                    */
                    Directory.CreateDirectory(Path.Combine(luaDir, "shared", "UEHelpers"));
                    File.WriteAllText(Path.Combine(luaDir, "shared", "UEHelpers", "UEHelpers.lua"), "local UEHelpers = {}\n-- Uncomment the below require to use the Lua VM profiler on these functions\n-- local jsb = require(\"jsbProfiler.jsbProfi\")\n\n-- Version 1 does not exist, we start at version 2 because the original version didn't have a version at all.\nlocal Version = 3\n\n-- Functions and classes local to this module, do not attempt to use!\n\n---@param ObjectFullName string\n---@param VariableName string\n---@param ForceInvalidateCache boolean?\n---@return UObject\nlocal function CacheDefaultObject(ObjectFullName, VariableName, ForceInvalidateCache)\n    local DefaultObject = CreateInvalidObject()\n\n    if not ForceInvalidateCache then\n        DefaultObject = ModRef:GetSharedVariable(VariableName)\n        if DefaultObject and DefaultObject:IsValid() then return DefaultObject end\n    end\n\n    DefaultObject = StaticFindObject(ObjectFullName)\n    ModRef:SetSharedVariable(VariableName, DefaultObject)\n    if not DefaultObject:IsValid() then error(string.format(\"%s not found\", ObjectFullName)) end\n\n    return DefaultObject\nend\n\n-- Everything in this section can be used in any mod that requires this module.\n-- Exported functions -> START\n\nfunction UEHelpers.GetUEHelpersVersion()\n    return Version\nend\n\nlocal EngineCache = CreateInvalidObject() ---@cast EngineCache UEngine\n---Returns instance of UEngine\n---@return UEngine\nfunction UEHelpers.GetEngine()\n    if EngineCache:IsValid() then return EngineCache end\n\n    EngineCache = FindFirstOf(\"Engine\") ---@cast EngineCache UEngine\n    return EngineCache\nend\n\nlocal GameInstanceCache = CreateInvalidObject() ---@cast GameInstanceCache UGameInstance\n---Returns instance of UGameInstance\n---@return UGameInstance\nfunction UEHelpers.GetGameInstance()\n    if GameInstanceCache:IsValid() then return GameInstanceCache end\n\n    GameInstanceCache = FindFirstOf(\"GameInstance\") ---@cast GameInstanceCache UGameInstance\n    return GameInstanceCache\nend\n\n---Returns the main UGameViewportClient (doesn't exist on a server)\n---@return UGameViewportClient\nfunction UEHelpers.GetGameViewportClient()\n    local Engine = UEHelpers.GetEngine()\n    if Engine:IsValid() and Engine.GameViewport then\n        return Engine.GameViewport\n    end\n    return CreateInvalidObject() ---@type UGameViewportClient\nend\n\nlocal PlayerControllerCache = CreateInvalidObject() ---@cast PlayerControllerCache APlayerController\n---Returns first player controller.<br>\n---In most games, a valid player controller is available from the start.<br>\n---There are no player controllers on the server until a player joins the server.\n---@return APlayerController\nfunction UEHelpers.GetPlayerController()\n    if PlayerControllerCache:IsValid() then return PlayerControllerCache end\n\n    -- local Controllers = jsb.simpleBench(\"FindAllOf: PlayerController\", FindAllOf, \"PlayerController\")\n    -- Controllers = jsb.simpleBench(\"FindAllOf: Controller\", FindAllOf, \"Controller\")\n    local Controllers = FindAllOf(\"PlayerController\") or FindAllOf(\"Controller\") ---@type AController[]?\n    if Controllers then\n        for _, Controller in ipairs(Controllers) do\n            if Controller:IsValid() and (Controller.IsPlayerController and Controller:IsPlayerController() or Controller:IsLocalPlayerController()) then\n                PlayerControllerCache = Controller\n                break\n            end\n        end\n    end\n\n    return PlayerControllerCache\nend\n\n---Returns local player pawn\n---@return APawn\nfunction UEHelpers.GetPlayer()\n    local playerController = UEHelpers.GetPlayerController()\n    if playerController:IsValid() and playerController.Pawn then\n        return playerController.Pawn\n    end\n    return CreateInvalidObject() ---@type APawn\nend\n\nlocal WorldCache = CreateInvalidObject() ---@cast WorldCache UWorld\n---Returns the main UWorld\n---@return UWorld\nfunction UEHelpers.GetWorld()\n    if WorldCache:IsValid() then return WorldCache end\n\n    local PlayerController = UEHelpers.GetPlayerController()\n    if PlayerController:IsValid() then\n        WorldCache = PlayerController:GetWorld()\n    else\n        local GameInstance = UEHelpers.GetGameInstance()\n        if GameInstance:IsValid() then\n            WorldCache = GameInstance:GetWorld()\n        end\n    end\n    return WorldCache\nend\n\n---Returns UWorld->PersistentLevel\n---@return ULevel\nfunction UEHelpers.GetPersistentLevel()\n    local World = UEHelpers.GetWorld()\n    if World:IsValid() and World.PersistentLevel then\n        return World.PersistentLevel\n    end\n    return CreateInvalidObject() ---@type ULevel\nend\n\n---Returns UWorld->AuthorityGameMode<br>\n---The function doesn't guarantee it to be an AGameMode, as many games derive their own game modes directly from AGameModeBase!\n---@return AGameModeBase\nfunction UEHelpers.GetGameModeBase()\n    local World = UEHelpers.GetWorld()\n    if World:IsValid() and World.AuthorityGameMode then\n        return World.AuthorityGameMode\n    end\n    return CreateInvalidObject() ---@type AGameModeBase\nend\n\n---Returns UWorld->GameState<br>\n---The function doesn't guarantee it to be an AGameState, as many games derive their own game states directly from AGameStateBase!\n---@return AGameStateBase\nfunction UEHelpers.GetGameStateBase()\n    local World = UEHelpers.GetWorld()\n    if World:IsValid() and World.GameState then\n        return World.GameState\n    end\n    return CreateInvalidObject() ---@type AGameStateBase\nend\n\n---Returns PersistentLevel->WorldSettings\n---@return AWorldSettings\nfunction UEHelpers.GetWorldSettings()\n    local PersistentLevel = UEHelpers.GetPersistentLevel()\n    if PersistentLevel:IsValid() and PersistentLevel.WorldSettings then\n        return PersistentLevel.WorldSettings\n    end\n    return CreateInvalidObject() ---@type AWorldSettings\nend\n\n--- Returns an object that's useable with UFunctions that have a WorldContext parameter.<br>\n--- Prefer to use an actor that you already have access to whenever possible over this function.\n--- Any UObject that has a GetWorld() function can be used as WorldContext.\n---@return UObject\nfunction UEHelpers.GetWorldContextObject()\n    return UEHelpers.GetWorld()\nend\n\n---Returns an array of all players APlayerState\n---@return APlayerState[]\nfunction UEHelpers.GetAllPlayerStates()\n    local PlayerStates = {}\n    local GameState = UEHelpers.GetGameStateBase()\n    if GameState:IsValid() and GameState.PlayerArray then\n        for i = 1, #GameState.PlayerArray do\n            table.insert(PlayerStates, GameState.PlayerArray[i])\n        end\n    end\n    return PlayerStates\nend\n\n---Returns all players as APawn.<br>\n---You can use `IsA` function to check the type of APawn to make sure it's the player class of the game.\n---@return APawn[]\nfunction UEHelpers.GetAllPlayers()\n    local PlayerPawns = {}\n    local PlayerStates = UEHelpers.GetAllPlayerStates()\n    if PlayerStates then\n        for i = 1, #PlayerStates do\n            local Pawn = PlayerStates[i].PawnPrivate\n            if Pawn and Pawn:IsValid() then\n                table.insert(PlayerPawns, Pawn)\n            end\n        end\n    end\n    return PlayerPawns\nend\n\n---Returns hit actor from FHitResult.<br>\n---The function handles the struct differance between UE4 and UE5\n---@param HitResult FHitResult\n---@return AActor|UObject\nfunction UEHelpers.GetActorFromHitResult(HitResult)\n    if not HitResult or not HitResult:IsValid() then\n        return CreateInvalidObject() ---@type AActor\n    end\n\n    if UnrealVersion:IsBelow(5, 0) then\n        return HitResult.Actor:Get()\n    elseif UnrealVersion:IsBelow(5, 4) then\n        return HitResult.HitObjectHandle.Actor:Get()\n    end\n    return HitResult.HitObjectHandle.ReferenceObject:Get()\nend\n\n---@param ForceInvalidateCache boolean? # Force update the cache\n---@return UGameplayStatics\nfunction UEHelpers.GetGameplayStatics(ForceInvalidateCache)\n    ---@type UGameplayStatics\n    return CacheDefaultObject(\"/Script/Engine.Default__GameplayStatics\", \"UEHelpers_GameplayStatics\", ForceInvalidateCache)\nend\n\n---@param ForceInvalidateCache boolean? # Force update the cache\n---@return UKismetSystemLibrary\nfunction UEHelpers.GetKismetSystemLibrary(ForceInvalidateCache)\n    ---@type UKismetSystemLibrary\n    return CacheDefaultObject(\"/Script/Engine.Default__KismetSystemLibrary\", \"UEHelpers_KismetSystemLibrary\", ForceInvalidateCache)\nend\n\n---@param ForceInvalidateCache boolean? # Force update the cache\n---@return UKismetMathLibrary\nfunction UEHelpers.GetKismetMathLibrary(ForceInvalidateCache)\n    ---@type UKismetMathLibrary\n    return CacheDefaultObject(\"/Script/Engine.Default__KismetMathLibrary\", \"UEHelpers_KismetMathLibrary\", ForceInvalidateCache)\nend\n\n---@param ForceInvalidateCache boolean? # Force update the cache\n---@return UKismetStringLibrary\nfunction UEHelpers.GetKismetStringLibrary(ForceInvalidateCache)\n    ---@type UKismetStringLibrary\n    return CacheDefaultObject(\"/Script/Engine.Default__KismetStringLibrary\", \"UEHelpers_KismetStringLibrary\", ForceInvalidateCache)\nend\n\n---@param ForceInvalidateCache boolean? # Force update the cache\n---@return UKismetTextLibrary\nfunction UEHelpers.GetKismetTextLibrary(ForceInvalidateCache)\n    ---@type UKismetTextLibrary\n    return CacheDefaultObject(\"/Script/Engine.Default__KismetTextLibrary\", \"UEHelpers_KismetTextLibrary\", ForceInvalidateCache)\nend\n\n---@param ForceInvalidateCache boolean? # Force update the cache\n---@return UGameMapsSettings\nfunction UEHelpers.GetGameMapsSettings(ForceInvalidateCache)\n    ---@type UGameMapsSettings\n    return CacheDefaultObject(\"/Script/EngineSettings.Default__GameMapsSettings\", \"UEHelpers_GameMapsSettings\", ForceInvalidateCache)\nend\n\n---Returns found FName or \"None\" FName if the operation faled\n---@param Name string\n---@return FName\nfunction UEHelpers.FindFName(Name)\n    return FName(Name, EFindName.FNAME_Find)\nend\n\n---Returns added FName or \"None\" FName if the operation faled\n---@param Name string\n---@return FName\nfunction UEHelpers.AddFName(Name)\n    return FName(Name, EFindName.FNAME_Add)\nend\n\n---Tries to find existing FName, if it doesn't exist a new FName will be added to the pool\n---@param Name string\n---@return FName # Returns found or added FName, “None” FName if both operations fail\nfunction UEHelpers.FindOrAddFName(Name)\n    local NameFound = FName(Name, EFindName.FNAME_Find)\n    if NameFound == NAME_None then\n        NameFound = FName(Name, EFindName.FNAME_Add)\n    end\n    return NameFound\nend\n\n-- Exported functions -> END\n\nreturn UEHelpers\n");
                }
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
