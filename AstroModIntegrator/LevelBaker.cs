using System;
using System.Collections.Generic;
using System.IO;
using UAssetAPI;
using UAssetAPI.ExportTypes;
using UAssetAPI.PropertyTypes.Objects;
using UAssetAPI.UnrealTypes;

namespace AstroModIntegrator
{
    public class SCS_Node
    {
        public string InternalVariableName; // string name
        public int TypeLink;
        public int AttachParent = -1; // parent category
        public int OriginalCategory; // original category in the source file
    }

    public class LevelBaker
    {
        private PakExtractor Extractor;
        private ModIntegrator ParentIntegrator;
        private readonly Export refData1B; // Actor template
        private readonly Export refData2B; // SceneComponent

        public LevelBaker(PakExtractor extractor, ModIntegrator integrator)
        {
            Extractor = extractor;
            ParentIntegrator = integrator;

            UAsset y = new UAsset(EngineVersion.VER_UE4_18);
            y.Read(new AssetBinaryReader(new MemoryStream(Properties.Resources.LevelTemplate), y));
            refData1B = y.Exports[2];
            refData2B = y.Exports[11];
        }

        public UAsset Bake(string[] newComponents, byte[] superRawData)
        {
            UAsset y = new UAsset(IntegratorUtils.EngineVersion);
            y.UseSeparateBulkDataFiles = true;
            y.CustomSerializationFlags = CustomSerializationFlags.SkipParsingBytecode | CustomSerializationFlags.SkipPreloadDependencyLoading | CustomSerializationFlags.SkipParsingExports;
            var reader = new AssetBinaryReader(new MemoryStream(superRawData), y);
            y.Read(reader);
            return this.Bake(newComponents, y, reader);
        }

        /*
            Game plan:
            1. Find the Level category
            2. Find the link with the Property SceneComponent
            3. Dig into the vanilla pak and the mod pak, try to find the connecting actor, add its nodes in the SimpleConstructionScript under BlueprintCreatedComponents (garbage1 = 0 no problem)
            4. Create the SceneComponent (garbage1 = 0), no RelativeLocation or UCSModifiedProperties, CreationMethod = EComponentCreationMethod::SimpleConstructionScript, bNetAddressable = 1
            5. Create the new Actor_C category, set its Linkage to the Level category, set the garbage1 to 0 (maybe random number idk), DefaultSceneRoot & RootComponent = the matching SceneComponent
        */

        public UAsset Bake(string[] newComponents, UAsset y, AssetBinaryReader reader)
        {
            if (newComponents.Length == 0) return y;

            LevelExport levelCategory = null;
            int levelLocation = -1;
            for (int i = 0; i < y.Exports.Count; i++)
            {
                Export baseUs = y.Exports[i];
                if (baseUs.GetExportClassType().ToString() == "Level")
                {
                    y.ParseExport(reader, i);
                    levelCategory = y.Exports[i] as LevelExport;
                    if (levelCategory != null)
                    {
                        levelLocation = i;
                        break;
                    }
                }
            }
            if (levelLocation < 0) throw new FormatException("Unable to find Level category");

            foreach (string componentPathRaw in newComponents)
            {
                Export refData1 = (Export)refData1B.Clone();
                string componentPath = componentPathRaw;
                string component = Path.GetFileNameWithoutExtension(componentPathRaw);
                if (componentPathRaw.Contains("."))
                {
                    string[] tData = componentPathRaw.Split(new char[] { '.' });
                    componentPath = tData[0];
                    component = tData[1].Remove(tData[1].Length - 2);
                }

                Import firstLink = new Import("/Script/CoreUObject", "Package", FPackageIndex.FromRawIndex(0), componentPath, false, y);
                FPackageIndex bigFirstLink = y.AddImport(firstLink);
                Import newLink = new Import("/Script/Engine", "BlueprintGeneratedClass", bigFirstLink, component + "_C", false, y);
                FPackageIndex bigNewLink = y.AddImport(newLink);
                Import newLink2 = new Import(componentPath, component + "_C", bigFirstLink, "Default__" + component + "_C", false, y);
                FPackageIndex bigNewLink2 = y.AddImport(newLink2);

                refData1.ClassIndex = bigNewLink;
                refData1.ObjectName = new FName(y, component);
                refData1.TemplateIndex = bigNewLink2;

                // Note that category links are set to one more than you'd think since categories in the category list index from 1 instead of 0

                refData1.OuterIndex = FPackageIndex.FromRawIndex(levelLocation + 1); // Level category

                // First we see if we can find the actual asset it's referring to
                List<SCS_Node> allBlueprintCreatedComponents = new List<SCS_Node>();
                byte[] foundData1 = ParentIntegrator.SearchInAllPaksForPath(componentPath.ConvertGamePathToAbsolutePath(), Extractor);
                byte[] foundData2 = ParentIntegrator.SearchInAllPaksForPath(Path.ChangeExtension(componentPath.ConvertGamePathToAbsolutePath(), ".uexp"), Extractor) ?? new byte[0];
                byte[] foundData = null; if (foundData1 != null) foundData = IntegratorUtils.Concatenate(foundData1, foundData2);
                if (foundData != null && foundData.Length > 0)
                {
                    // If we can find the asset, then we read the asset and hop straight to the SimpleConstructionScript
                    UAsset foundDataReader = new UAsset(IntegratorUtils.EngineVersion);
                    foundDataReader.CustomSerializationFlags = CustomSerializationFlags.SkipParsingBytecode | CustomSerializationFlags.SkipPreloadDependencyLoading | CustomSerializationFlags.SkipParsingExports;
                    var fbReader = new AssetBinaryReader(new MemoryStream(foundData), foundDataReader);
                    foundDataReader.Read(fbReader);

                    int scsLocation = -1;
                    for (int i = 0; i < foundDataReader.Exports.Count; i++)
                    {
                        Export foundCategory = foundDataReader.Exports[i];
                        string nm = foundCategory.ClassIndex.IsImport() ? foundCategory.ClassIndex.ToImport(y)?.ObjectName?.Value?.Value : string.Empty;
                        switch (nm)
                        {
                            case "SimpleConstructionScript":
                                y.ParseExport(reader, i);
                                scsLocation = i;
                                break;
                        }
                    }

                    if (scsLocation >= 0)
                    {
                        List<int> knownNodeCategories = new List<int>();
                        NormalExport scsCategory = (NormalExport)foundDataReader.Exports[scsLocation];
                        for (int j = 0; j < scsCategory.Data.Count; j++)
                        {
                            PropertyData bit = scsCategory.Data[j];
                            if (bit is ArrayPropertyData arrBit && arrBit.ArrayType.Value.Value == "ObjectProperty" && bit.Name.Value.Value == "AllNodes")
                            {
                                foreach (ObjectPropertyData objProp in arrBit.Value)
                                {
                                    if (objProp.Value.Index > 0) knownNodeCategories.Add(objProp.Value.Index);
                                }
                            }
                        }

                        Dictionary<int, int> knownParents = new Dictionary<int, int>();
                        foreach (int knownNodeCategory in knownNodeCategories)
                        {
                            Export knownCat = foundDataReader.Exports[knownNodeCategory - 1];
                            string nm = knownCat.ClassIndex.IsImport() ? knownCat.ClassIndex.ToImport(y).ObjectName.Value.Value : string.Empty;
                            if (nm != "SCS_Node") continue;
                            y.ParseExport(fbReader, knownNodeCategory - 1);
                            if (foundDataReader.Exports[knownNodeCategory - 1] is NormalExport knownNormalCat)
                            {
                                SCS_Node newSCS = new SCS_Node();
                                newSCS.InternalVariableName = "Unknown";
                                newSCS.OriginalCategory = knownNodeCategory;
                                Import knownTypeLink1 = null;
                                Import knownTypeLink2 = null;

                                foreach (PropertyData knownNormalCatProp in knownNormalCat.Data)
                                {
                                    switch (knownNormalCatProp.Name.Value.Value)
                                    {
                                        case "InternalVariableName":
                                            if (knownNormalCatProp is NamePropertyData) newSCS.InternalVariableName = ((NamePropertyData)knownNormalCatProp).Value.Value.Value;
                                            break;
                                        case "ComponentClass":
                                            if (knownNormalCatProp is ObjectPropertyData) knownTypeLink1 = ((ObjectPropertyData)knownNormalCatProp).ToImport(foundDataReader);
                                            knownTypeLink2 = knownTypeLink1.OuterIndex.ToImport(foundDataReader);
                                            break;
                                        case "ChildNodes":
                                            if (knownNormalCatProp is ArrayPropertyData arrData2 && arrData2.ArrayType.Value.Value == "ObjectProperty")
                                            {
                                                foreach (ObjectPropertyData knownNormalCatPropChildren in arrData2.Value)
                                                {
                                                    knownParents.Add(knownNormalCatPropChildren.Value.Index, knownNodeCategory);
                                                }
                                            }
                                            break;
                                    }
                                }

                                if (knownTypeLink1 != null && knownTypeLink2 != null)
                                {
                                    Import prospectiveLink2 = knownTypeLink2;
                                    int addedLink = y.SearchForImport(prospectiveLink2.ClassPackage, prospectiveLink2.ClassName, prospectiveLink2.OuterIndex, prospectiveLink2.ObjectName);
                                    if (addedLink >= 0) addedLink = y.AddImport(prospectiveLink2).Index;

                                    Import prospectiveLink1 = knownTypeLink1;
                                    int newTypeLink = y.SearchForImport(prospectiveLink1.ClassPackage, prospectiveLink1.ClassName, prospectiveLink1.OuterIndex, prospectiveLink1.ObjectName);
                                    if (newTypeLink >= 0) newTypeLink = y.AddImport(prospectiveLink1).Index;
                                    newSCS.TypeLink = newTypeLink;
                                }

                                allBlueprintCreatedComponents.Add(newSCS);
                            }
                        }

                        foreach (SCS_Node node in allBlueprintCreatedComponents)
                        {
                            if (knownParents.ContainsKey(node.OriginalCategory)) node.AttachParent = knownParents[node.OriginalCategory];
                        }
                    }
                }

                // Then we add all our child components
                int templateCategoryPointer = y.Exports.Count + allBlueprintCreatedComponents.Count + 1;

                List<ObjectPropertyData> BlueprintCreatedComponentsSerializedList = new List<ObjectPropertyData>();
                List<ObjectPropertyData> AttachParentDueForCorrection = new List<ObjectPropertyData>();
                Dictionary<string, int> NodeNameToCatIndex = new Dictionary<string, int>();
                Dictionary<int, int> OldCatToNewCat = new Dictionary<int, int>();
                foreach (SCS_Node blueprintCreatedComponent in allBlueprintCreatedComponents)
                {
                    Export refData2 = (Export)refData2B.Clone();

                    refData2.ClassIndex = FPackageIndex.FromRawIndex(blueprintCreatedComponent.TypeLink);
                    y.AddNameReference(new FString(blueprintCreatedComponent.InternalVariableName));
                    refData2.ObjectName = new FName(y, blueprintCreatedComponent.InternalVariableName);
                    refData2.OuterIndex = FPackageIndex.FromRawIndex(templateCategoryPointer); // Template category

                    var determinedPropData = new List<PropertyData>
                    {
                        new BoolPropertyData(new FName(y, "bNetAddressable"))
                        {
                            Value = true,
                        },
                        new EnumPropertyData(new FName(y, "CreationMethod"))
                        {
                            EnumType = new FName(y, "EComponentCreationMethod"),
                            Value = new FName(y, "EComponentCreationMethod::SimpleConstructionScript")
                        }
                    };

                    if (blueprintCreatedComponent.AttachParent >= 0)
                    {
                        var nextOPD = new ObjectPropertyData(new FName(y, "AttachParent"))
                        {
                            Value = FPackageIndex.FromRawIndex(blueprintCreatedComponent.AttachParent)
                        };
                        AttachParentDueForCorrection.Add(nextOPD);
                        determinedPropData.Add(nextOPD);
                    }

                    NormalExport sceneCat = refData2.ConvertToChildExport<NormalExport>();
                    sceneCat.Extras = new byte[4] { 0, 0, 0, 0 };
                    sceneCat.Data = determinedPropData;
                    y.Exports.Add(sceneCat);
                    BlueprintCreatedComponentsSerializedList.Add(new ObjectPropertyData(new FName(y, "BlueprintCreatedComponents"))
                    {
                        Value = FPackageIndex.FromRawIndex(y.Exports.Count)
                    });
                    NodeNameToCatIndex.Add(blueprintCreatedComponent.InternalVariableName, y.Exports.Count);
                    OldCatToNewCat.Add(blueprintCreatedComponent.OriginalCategory, y.Exports.Count);

                    y.AddImport(new Import("/Script/Engine", FPackageIndex.FromRawIndex(blueprintCreatedComponent.TypeLink).ToImport(y).ObjectName.Value.Value, refData1.ClassIndex, blueprintCreatedComponent.InternalVariableName + "_GEN_VARIABLE", false, y));
                }

                foreach (ObjectPropertyData attachParentCorrecting in AttachParentDueForCorrection)
                {
                    attachParentCorrecting.Value = FPackageIndex.FromRawIndex(OldCatToNewCat[attachParentCorrecting.Value.Index]);
                }

                // Then we add the template category
                var templateDeterminedPropData = new List<PropertyData>
                {
                    new BoolPropertyData(new FName(y, "bHidden"))
                    {
                        Value = true
                    },
                    new ArrayPropertyData(new FName(y, "BlueprintCreatedComponents"))
                    {
                        ArrayType = new FName(y, "ObjectProperty"),
                        Value = BlueprintCreatedComponentsSerializedList.ToArray()
                    }
                };

                foreach (KeyValuePair<string, int> entry in NodeNameToCatIndex)
                {
                    if (entry.Key == "DefaultSceneRoot")
                    {
                        templateDeterminedPropData.Add(new ObjectPropertyData(new FName(y, "RootComponent"))
                        {
                            Value = FPackageIndex.FromRawIndex(entry.Value)
                        });
                    }
                    templateDeterminedPropData.Add(new ObjectPropertyData(new FName(y, entry.Key))
                    {
                        Value = FPackageIndex.FromRawIndex(entry.Value)
                    });
                }

                NormalExport lastExport = refData1.ConvertToChildExport<NormalExport>();
                lastExport.SerializationBeforeCreateDependencies.Add(bigNewLink);
                lastExport.SerializationBeforeCreateDependencies.Add(bigNewLink2);
                lastExport.CreateBeforeCreateDependencies.Add(FPackageIndex.FromRawIndex(levelLocation + 1));
                lastExport.Extras = Array.Empty<byte>();
                lastExport.Data = templateDeterminedPropData;
                y.Exports.Add(lastExport);

                // Add the template category to the level category
                levelCategory.Actors.Add(FPackageIndex.FromRawIndex(y.Exports.Count));
                levelCategory.CreateBeforeSerializationDependencies.Add(FPackageIndex.FromRawIndex(y.Exports.Count));
            }

            return y;
        }
    }
}
