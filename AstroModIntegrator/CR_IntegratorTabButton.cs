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
    public class CR_IntegratorTabButton : CustomRoutine
    {
        public override string RoutineID => "CR_IntegratorTabButton";
        public override bool Enabled => true;
        public override int APIVersion => 1;

        public static readonly HashSet<string> MenuBarsToAddTo = ["HostMidGameMenu", "ClientMidGameMenu", "HostTitleScreenMenu", "HostTitleScreenMenuConsole", "ClientTitleScreenMenu", "ClientTitleScreenMenuConsole"];
        public static readonly string NewTabBarButtonPath = "/Game/Integrator/ModConfig/UI/IntegratorTabBarButton";
        public static readonly string ID_to_ModConfig_Class_Path = "/Game/Integrator/ModConfig/UI/ID_to_ModConfig_Class";
        public override void Execute(ICustomRoutineAPI api)
        {
            api.LogToDisk("Starting built-in routine " + RoutineID, false);

            string targetPath = "/Game/Globals/AstroUIStylingDatabase";
            UAsset asset = api.FindFile(targetPath);
            bool success = false;

            FPackageIndex newImp = null;
            foreach (Export exp in asset.Exports)
            {
                if (exp is NormalExport nexp)
                {
                    foreach (PropertyData prop in nexp.Data)
                    {
                        if (prop is StructPropertyData structDat && MenuBarsToAddTo.Contains(prop.Name.ToString()))
                        {
                            StructPropertyData structDat2 = structDat["TabBarAuthoringData"] as StructPropertyData;
                            if (structDat2 == null) continue;

                            ArrayPropertyData arrDat = structDat2["LeftTabBarGroupButtons"] as ArrayPropertyData;
                            if (arrDat == null) continue;

                            for (int i = 0; i < arrDat.Value.Length; i++)
                            {
                                if (arrDat.Value[i] is ObjectPropertyData objProp && objProp.Value.IsImport() && objProp.Value.ToImport(asset).ObjectName.ToString() == "GameMenuTabBarButtonOptions_C")
                                {
                                    if (newImp == null)
                                    {
                                        string bpClass = Path.GetFileNameWithoutExtension(NewTabBarButtonPath) + "_C";
                                        FPackageIndex newIdx = asset.AddImport(new Import(FName.FromString(asset, "/Script/CoreUObject"), FName.FromString(asset, "Package"), FPackageIndex.FromRawIndex(0), FName.FromString(asset, NewTabBarButtonPath), false));
                                        newImp = asset.AddImport(new Import(FName.FromString(asset, "/Script/Engine"), FName.FromString(asset, "WidgetBlueprintGeneratedClass"), newIdx, FName.FromString(asset, bpClass), false));
                                    }
                                    ObjectPropertyData newObjProp = new ObjectPropertyData(FName.DefineDummy(asset, "")) { Value = newImp };

                                    List<PropertyData> newArrDatList = arrDat.Value.ToList();
                                    newArrDatList.Insert(i + 1, newObjProp);
                                    arrDat.Value = newArrDatList.ToArray();

                                    success = true;
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            if (success)
            {
                api.AddFile(targetPath, asset);
            }
            else
            {
                api.LogToDisk("Failed to modify " + targetPath, false);
            }

            // now populate ID_to_ModConfig_Class
            IReadOnlyList<Metadata> allMods = api.GetAllMods();
            Dictionary<string, string> modIdToModConfigPath = new Dictionary<string, string>();
            foreach (Metadata mod in allMods)
            {
                if (string.IsNullOrEmpty(mod?.IntegratorEntries.PathToModConfig)) continue;
                modIdToModConfigPath.Add(mod.ModID, mod.IntegratorEntries.PathToModConfig);
            }

            // allow accessing the example mod config by choosing the mod integrator
            // (temporary debug feature)
            modIdToModConfigPath.Add("AstroModIntegrator", "/Game/Integrator/ModConfig/ModConfigExample");

            if (modIdToModConfigPath.Count > 0)
            {
                bool success2 = false;
                UAsset asset2 = api.FindFile(ID_to_ModConfig_Class_Path);
                NormalExport cdo = asset2?.GetClassExport()?.ClassDefaultObject?.ToExport(asset2) as NormalExport;
                if (cdo != null)
                {
                    MapPropertyData mapProp = cdo["ID_to_ModConfig_Class"] as MapPropertyData;
                    if (mapProp != null)
                    {
                        foreach (KeyValuePair<string, string> entry in modIdToModConfigPath)
                        {
                            string bpClass = Path.GetFileNameWithoutExtension(entry.Value) + "_C";
                            FPackageIndex newIdx = asset2.AddImport(new Import(FName.FromString(asset2, "/Script/CoreUObject"), FName.FromString(asset2, "Package"), FPackageIndex.FromRawIndex(0), FName.FromString(asset2, entry.Value), false));
                            FPackageIndex newImp2 = asset2.AddImport(new Import(FName.FromString(asset2, "/Script/Engine"), FName.FromString(asset2, "WidgetBlueprintGeneratedClass"), newIdx, FName.FromString(asset2, bpClass), false));

                            ObjectPropertyData newObjProp = new ObjectPropertyData(FName.DefineDummy(asset2, "Value")) { Value = newImp2 };
                            mapProp.Value.Add(new StrPropertyData(FName.DefineDummy(asset2, "Key")) { Value = new FString(entry.Key) }, newObjProp);
                            success2 = true;
                        }
                    }
                }

                if (success2)
                {
                    api.AddFile(ID_to_ModConfig_Class_Path, asset2);
                }
                else
                {
                    api.LogToDisk("Failed to modify " + ID_to_ModConfig_Class_Path, false);
                }
            }
            else
            {
                api.LogToDisk("No mod configs to add to " + ID_to_ModConfig_Class_Path, false);
            }

            api.LogToDisk("Completed built-in routine " + RoutineID, false);
        }
    }
}
