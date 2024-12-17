using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UAssetAPI;
using UAssetAPI.ExportTypes;
using UAssetAPI.PropertyTypes.Objects;
using UAssetAPI.PropertyTypes.Structs;
using UAssetAPI.UnrealTypes;

namespace AstroModIntegrator
{
    // The logic for this source code was obtained by referencing the astro_modloader source code
    // https://github.com/AstroTechies/astro_modloader/blob/87145c9d5e5ba2914e576012820d662558619372/astro_mod_integrator/src/handlers/biome_placement_modifiers.rs#L35
    // while astro_modloader is unlicensed, I consider there to be insufficient originality for the snippets referenced here to be protected under copyright law
    // (i.e. there is no other reasonable technique that could be used to conduct the operation, and nothing other than the overarching technique is derived)

    public enum BiomeType
    {
        Surface,
        Crust,
    }

    public struct PlacementModifier : ICloneable
    {
        [JsonProperty("planet_type")]
        public string PlanetType;

        [JsonProperty("biome_type")]
        public BiomeType BiomeType;

        [JsonProperty("biome_name")]
        public string BiomeName;

        [JsonProperty("layer_name")]
        public string LayerName;

        [JsonProperty("placements")]
        public List<string> Placements;

        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }

    public class BiomePlacementModifiersBaker
    {
        public UAsset Bake(List<PlacementModifier> modifiers, string[] newTrailheads, byte[] mapData, out AssetBinaryReader reader)
        {
            UAsset y = new UAsset(IntegratorUtils.EngineVersion);
            y.UseSeparateBulkDataFiles = true;
            y.CustomSerializationFlags = CustomSerializationFlags.SkipParsingBytecode | CustomSerializationFlags.SkipPreloadDependencyLoading | CustomSerializationFlags.SkipParsingExports;
            reader = new AssetBinaryReader(new MemoryStream(mapData), y);
            y.Read(reader);

            modifiers = modifiers.Distinct().ToList();
            newTrailheads = newTrailheads.Distinct().ToArray();

            // also do mission trailheads here

            // Missions
            if (newTrailheads.Length > 0)
            {
                for (int cat = 0; cat < y.Exports.Count; cat++)
                {
                    if ((y.Exports[cat].ClassIndex.IsImport() ? y.Exports[cat].ClassIndex.ToImport(y).ObjectName.Value.Value : string.Empty) != "AstroSettings") continue;
                    y.ParseExport(reader, cat);
                    NormalExport normalCat = y.Exports[cat] as NormalExport;
                    if (normalCat == null) continue;

                    for (int i = 0; i < normalCat.Data.Count; i++)
                    {
                        if (normalCat.Data[i].Name.Value.Value == "MissionData" && normalCat.Data[i] is ArrayPropertyData arrDat && arrDat.ArrayType.Value.Value == "ObjectProperty")
                        {
                            PropertyData[] usArrData = arrDat.Value;
                            int oldLen = usArrData.Length;
                            Array.Resize(ref usArrData, usArrData.Length + newTrailheads.Length);
                            for (int j = 0; j < newTrailheads.Length; j++)
                            {
                                string realName = newTrailheads[j];
                                string softClassName = Path.GetFileNameWithoutExtension(realName);

                                Import newLink = new Import("/Script/Astro", "AstroMissionDataAsset", y.AddImport(new Import("/Script/CoreUObject", "Package", FPackageIndex.FromRawIndex(0), realName, false, y)), softClassName, false, y);
                                FPackageIndex bigNewLink = y.AddImport(newLink);

                                usArrData[oldLen + j] = new ObjectPropertyData(arrDat.Name)
                                {
                                    Value = bigNewLink
                                };
                            }
                            arrDat.Value = usArrData;
                            break;
                        }
                    }
                    break;
                }
            }

            // biome placement modifiers

            Dictionary<string, NormalExport> voxelVolumeExports = new Dictionary<string, NormalExport>();
            for (int i = 0; i < y.Exports.Count; i++)
            {
                var exp = y.Exports[i];
                if (exp.GetExportClassType().ToString() == "VoxelVolumeComponent" && exp.ObjectName.ToString() != "Default Voxel Volume")
                {
                    y.ParseExport(reader, i);
                    if (y.Exports[i] is NormalExport nexp)
                    {
                        voxelVolumeExports[exp.ObjectName.ToString()] = nexp;
                    }
                }
            }

            foreach (var modifier in modifiers)
            {
                try
                {
                    List<FPackageIndex> modifierImports = new List<FPackageIndex>();

                    foreach (string path in modifier.Placements)
                    {
                        FPackageIndex packageImport = y.AddImport(new Import("/Script/CoreUObject", "Package", FPackageIndex.FromRawIndex(0), path, false, y));
                        FPackageIndex modifierImport = y.AddImport(new Import("/Script/Terrain2", "ProceduralModifier", packageImport, Path.GetFileNameWithoutExtension(path), false, y));
                        modifierImports.Add(modifierImport);
                    }

                    string voxelsName = modifier.PlanetType + "Voxels";
                    voxelVolumeExports.TryGetValue(voxelsName, out NormalExport voxelsExport);
                    if (voxelsExport == null) continue; // not an error, could just occur, e.g. with DLC map (needs PlanetType == "GlitchPlanet")

                    StructPropertyData biome = null;
                    switch (modifier.BiomeType)
                    {
                        case BiomeType.Surface:
                            var listOfBiomes = voxelsExport["SurfaceBiomes"] as ArrayPropertyData;
                            if (listOfBiomes == null) throw new FormatException("Unable to find SurfaceBiomes for planet " + modifier.PlanetType);
                            foreach (PropertyData testBiomeRaw in listOfBiomes.Value)
                            {
                                if (testBiomeRaw is StructPropertyData testBiome)
                                {
                                    if ((testBiome["Name"] as NamePropertyData)?.Value?.ToString() == modifier.BiomeName)
                                    {
                                        biome = testBiome;
                                        break;
                                    }
                                }
                            }

                            if (biome == null) throw new FormatException("Unable to find biome " + modifier.BiomeName + " on planet " + modifier.PlanetType);
                            break;
                        case BiomeType.Crust:
                            biome = voxelsExport["CrustBiome"] as StructPropertyData;
                            if (biome == null) throw new FormatException("Unable to find crust biome on planet " + modifier.PlanetType);
                            break;
                    }

                    ArrayPropertyData layers = biome["Layers"] as ArrayPropertyData;
                    if (layers == null) throw new FormatException("Unable to find layers for biome " + modifier.BiomeName + " on planet " + modifier.PlanetType);

                    StructPropertyData layer = null;
                    foreach (PropertyData testLayerRaw in layers.Value)
                    {
                        if (testLayerRaw is StructPropertyData testLayer)
                        {
                            if ((testLayer["Name"] as NamePropertyData)?.Value?.ToString() == modifier.LayerName)
                            {
                                layer = testLayer;
                                break;
                            }
                        }
                    }
                    if (layer == null) throw new FormatException("Unable to find layer " + modifier.LayerName + " in biome " + modifier.BiomeName + " on planet " + modifier.PlanetType);

                    ArrayPropertyData objectPlacementModifiers = layer["ObjectPlacementModifiers"] as ArrayPropertyData;
                    if (objectPlacementModifiers == null) throw new FormatException("Unable to find ObjectPlacementModifiers in layer " + modifier.LayerName + " in biome " + modifier.BiomeName + " on planet " + modifier.PlanetType);

                    PropertyData[] oldVal = objectPlacementModifiers.Value;
                    PropertyData[] newVal = new PropertyData[oldVal.Length + modifierImports.Count];
                    Array.Copy(oldVal, 0, newVal, 0, oldVal.Length);

                    int i = oldVal.Length;
                    foreach (FPackageIndex idx in modifierImports)
                    {
                        var newObject = new ObjectPropertyData(); // name not serialized because it's in an array
                        newObject.Value = idx;
                        newVal[i++] = newObject;
                    }

                    objectPlacementModifiers.Value = newVal;

                    // all done!
                }
                catch
                {
                    continue;
                }
            }

            return y;
        }
    }
}