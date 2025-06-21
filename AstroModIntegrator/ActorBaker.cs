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
    public class ActorBaker
    {
        private readonly Export refData1B; // ObjectProperty
        private readonly Export refData2B; // Template category
        private readonly Export refData3B; // SCS_Node

        public ActorBaker()
        {
            UAsset y = new UAsset(EngineVersion.VER_UE4_18);
            y.Read(new AssetBinaryReader(new MemoryStream(Properties.Resources.ActorTemplate), y));
            refData1B = y.Exports[6];
            refData2B = y.Exports[5];
            refData3B = y.Exports[10];
        }

        public UAsset Bake(string[] newComponents, byte[] superRawData)
        {
            UAsset y = new UAsset(IntegratorUtils.EngineVersion);
            y.UseSeparateBulkDataFiles = true;
            y.CustomSerializationFlags = CustomSerializationFlags.SkipParsingBytecode | CustomSerializationFlags.SkipPreloadDependencyLoading | CustomSerializationFlags.SkipParsingExports;
            var reader = new AssetBinaryReader(new MemoryStream(superRawData), y);
            y.Read(reader);

            newComponents = newComponents.Distinct().ToArray();

            int scsLocation = -1;
            int bgcLocation = -1;
            int cdoLocation = -1;
            int nodeOffset = 0;
            for (int i = 0; i < y.Exports.Count; i++)
            {
                Export baseUs = y.Exports[i];
                switch (baseUs.ClassIndex.IsImport() ? baseUs.ClassIndex.ToImport(y).ObjectName.Value.Value : string.Empty)
                {
                    case "SimpleConstructionScript":
                        scsLocation = i;
                        y.ParseExport(reader, i);
                        break;
                    case "BlueprintGeneratedClass":
                        bgcLocation = i;
                        y.ParseExport(reader, i);
                        break;
                    case "SCS_Node":
                        nodeOffset = Math.Max(nodeOffset, baseUs.ObjectName.Number);
                        break;
                }
                if (baseUs.ObjectFlags.HasFlag(EObjectFlags.RF_ClassDefaultObject))
                {
                    cdoLocation = i;
                    y.ParseExport(reader, i);
                }
            }
            if (scsLocation < 0) throw new FormatException("Unable to find SimpleConstructionScript");
            if (bgcLocation < 0) throw new FormatException("Unable to find BlueprintGeneratedClass");
            if (cdoLocation < 0) throw new FormatException("Unable to find CDO");
            int objectPropertyLink = y.SearchForImport(new FName(y, "/Script/CoreUObject"), new FName(y, "Class"), new FName(y, "ObjectProperty"));
            int objectPropertyLink2 = y.SearchForImport(new FName(y, "/Script/CoreUObject"), new FName(y, "ObjectProperty"), new FName(y, "Default__ObjectProperty"));
            int scsNodeLink = y.SearchForImport(new FName(y, "/Script/CoreUObject"), new FName(y, "Class"), new FName(y, "SCS_Node"));
            int scsNodeLink2 = y.SearchForImport(new FName(y, "/Script/Engine"), new FName(y, "SCS_Node"), new FName(y, "Default__SCS_Node"));
            byte[] noneRef = BitConverter.GetBytes((long)y.SearchNameReference(FString.FromString("None")));

            foreach (string componentPathRaw in newComponents)
            {
                Export refData1 = (Export)refData1B.Clone();
                Export refData2 = (Export)refData2B.Clone();
                Export refData3 = (Export)refData3B.Clone();

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

                refData2.ClassIndex = bigNewLink;
                refData2.ObjectName = new FName(y, component + "_GEN_VARIABLE");
                refData2.TemplateIndex = bigNewLink2;

                refData1.ClassIndex = FPackageIndex.FromRawIndex(objectPropertyLink);
                refData1.ObjectName = new FName(y, component);
                refData1.TemplateIndex = FPackageIndex.FromRawIndex(objectPropertyLink2);

                refData3.ClassIndex = FPackageIndex.FromRawIndex(scsNodeLink);
                refData3.ObjectName = new FName(y, "SCS_Node");
                refData3.TemplateIndex = FPackageIndex.FromRawIndex(scsNodeLink2);

                List<byte> rawData = new List<byte>();

                // Here we specify the raw data for our ObjectProperty category, including necessary flags and such
                rawData.AddRange(noneRef);
                rawData.AddRange(new byte[] {
                    0x00,
                    0x00,
                    0x00,
                    0x00,
                    0x01,
                    0x00,
                    0x00,
                    0x00,

                    0x04,
                    0x00,
                    0x00,
                    0x00,
                    0x04,
                    0x00,
                    0x00,
                    0x00,
                });
                rawData.AddRange(noneRef);
                rawData.Add((byte)0);
                rawData.AddRange(BitConverter.GetBytes(bigNewLink.Index));

                refData1.OuterIndex = FPackageIndex.FromRawIndex(bgcLocation + 1); // BlueprintGeneratedClass category
                refData2.OuterIndex = FPackageIndex.FromRawIndex(bgcLocation + 1); // BlueprintGeneratedClass category
                refData3.OuterIndex = FPackageIndex.FromRawIndex(scsLocation + 1);

                // Note that category links are set to one more than you'd think since categories in the category list index from 1 instead of 0

                // First we add the template category
                NormalExport templateCat = refData2.ConvertToChildExport<NormalExport>();
                templateCat.SerializationBeforeSerializationDependencies.Add(FPackageIndex.FromRawIndex(bgcLocation + 1));
                templateCat.SerializationBeforeCreateDependencies.Add(bigNewLink);
                templateCat.SerializationBeforeCreateDependencies.Add(bigNewLink2);
                templateCat.CreateBeforeCreateDependencies.Add(FPackageIndex.FromRawIndex(bgcLocation + 1));
                templateCat.Extras = Array.Empty<byte>();
                templateCat.Data = new List<PropertyData>
                {
                    new BoolPropertyData(new FName(y, "bAutoActivate"))
                    {
                        Value = true
                    }
                };
                y.Exports.Add(templateCat);

                NormalExport cdoCategory = (NormalExport)y.Exports[cdoLocation];
                cdoCategory.SerializationBeforeSerializationDependencies.Add(FPackageIndex.FromRawIndex(y.Exports.Count));

                // Then the ObjectProperty category
                RawExport objectCat = refData1.ConvertToChildExport<RawExport>();
                objectCat.CreateBeforeSerializationDependencies.Add(bigNewLink);
                objectCat.CreateBeforeCreateDependencies.Add(FPackageIndex.FromRawIndex(bgcLocation + 1));
                objectCat.Extras = Array.Empty<byte>();
                objectCat.Data = rawData.ToArray();
                y.Exports.Add(objectCat);

                // Then the SCS_Node
                NormalExport scsCat = refData3.ConvertToChildExport<NormalExport>();
                scsCat.ObjectName = new FName(y, "SCS_Node", ++nodeOffset);
                scsCat.Extras = Array.Empty<byte>();
                scsCat.CreateBeforeSerializationDependencies.Add(bigNewLink);
                scsCat.CreateBeforeSerializationDependencies.Add(FPackageIndex.FromRawIndex(y.Exports.Count - 1));
                scsCat.SerializationBeforeCreateDependencies.Add(FPackageIndex.FromRawIndex(scsNodeLink));
                scsCat.SerializationBeforeCreateDependencies.Add(FPackageIndex.FromRawIndex(scsNodeLink2));
                scsCat.CreateBeforeCreateDependencies.Add(FPackageIndex.FromRawIndex(scsLocation + 1));
                scsCat.Data = new List<PropertyData>
                {
                    new ObjectPropertyData(new FName(y, "ComponentClass"))
                    {
                        Value = bigNewLink
                    },
                    new ObjectPropertyData(new FName(y, "ComponentTemplate"))
                    {
                        Value = FPackageIndex.FromRawIndex(y.Exports.Count - 1) // the first NormalCategory
                    },
                    new StructPropertyData(new FName(y, "VariableGuid"), new FName(y, "Guid"))
                    {
                        Value = new List<PropertyData>
                        {
                            new GuidPropertyData(new FName(y, "VariableGuid"))
                            {
                                Value = Guid.NewGuid()
                            }
                        }
                    },
                    new NamePropertyData(new FName(y, "InternalVariableName"))
                    {
                        Value = new FName(y, component)
                    }
                };
                y.Exports.Add(scsCat);

                // We update the BlueprintGeneratedClass data to include our new ActorComponent
                FPackageIndex[] oldData = ((StructExport)y.Exports[bgcLocation]).Children;
                FPackageIndex[] newData = new FPackageIndex[oldData.Length + 1];
                Array.Copy(oldData, 0, newData, 0, oldData.Length);
                newData[oldData.Length] = FPackageIndex.FromRawIndex(y.Exports.Count - 1); // the RawCategory
                ((StructExport)y.Exports[bgcLocation]).Children = newData;

                // Here we update the SimpleConstructionScript so that the parser constructs our new ActorComponent
                NormalExport scsCategory = (NormalExport)y.Exports[scsLocation];
                scsCategory.CreateBeforeSerializationDependencies.Add(FPackageIndex.FromRawIndex(y.Exports.Count));
                cdoCategory.SerializationBeforeSerializationDependencies.Add(FPackageIndex.FromRawIndex(y.Exports.Count));
                for (int j = 0; j < scsCategory.Data.Count; j++)
                {
                    PropertyData bit = scsCategory.Data[j];
                    if (bit is ArrayPropertyData)
                    {
                        switch (bit.Name.Value.Value)
                        {
                            case "AllNodes":
                            case "RootNodes":
                                PropertyData[] ourArr = ((ArrayPropertyData)bit).Value;
                                int oldSize = ourArr.Length;
                                Array.Resize(ref ourArr, oldSize + 1);
                                refData3.ObjectName = new FName(y, refData3.ObjectName.Value, oldSize + 2);
                                ourArr[oldSize] = new ObjectPropertyData(bit.Name)
                                {
                                    Value = FPackageIndex.FromRawIndex(y.Exports.Count) // the SCS_Node
                                };
                                ((ArrayPropertyData)bit).Value = ourArr;
                                break;
                        }
                    }
                }
            }

            return y;
        }
    }
}
