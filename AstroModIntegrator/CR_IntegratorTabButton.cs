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
    public class CR_IntegratorTabButton : CustomRoutine
    {
        public override string RoutineID => "CR_IntegratorTabButton";
        public override bool Enabled => true;
        public override int APIVersion => 1;

        public static readonly HashSet<string> MenuBarsToAddTo = ["HostMidGameMenu", "ClientMidGameMenu", "HostTitleScreenMenu", "HostTitleScreenMenuConsole", "ClientTitleScreenMenu", "ClientTitleScreenMenuConsole"];
        public static readonly string NewTabBarButtonPath = "/Game/Integrator/UI/IntegratorTabBarButton";
        public override void Execute(ICustomRoutineAPI api)
        {
            api.LogToDisk("Starting built-in routine " + RoutineID, false);

            string targetPath = "/Game/Globals/AstroUIStylingDatabase";
            UAsset asset = api.FindFile(targetPath);
            bool success = false;

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
                                    string bpClass = Path.GetFileNameWithoutExtension(NewTabBarButtonPath) + "_C";
                                    FPackageIndex newIdx = asset.AddImport(new Import(FName.FromString(asset, "/Script/CoreUObject"), FName.FromString(asset, "Package"), FPackageIndex.FromRawIndex(0), FName.FromString(asset, NewTabBarButtonPath), false));
                                    FPackageIndex newImp = asset.AddImport(new Import(FName.FromString(asset, "/Script/Engine"), FName.FromString(asset, "WidgetBlueprintGeneratedClass"), newIdx, FName.FromString(asset, bpClass), false));
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

            api.LogToDisk("Completed built-in routine " + RoutineID, false);
        }
    }
}
