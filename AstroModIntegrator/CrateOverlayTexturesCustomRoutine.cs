using System.Collections.Generic;
using System.IO;
using System.Linq;
using UAssetAPI;
using UAssetAPI.ExportTypes;
using UAssetAPI.PropertyTypes.Objects;
using UAssetAPI.UnrealTypes;

namespace AstroModIntegrator
{
    public class CrateOverlayTexturesCustomRoutine : CustomRoutine
    {
        public override string RoutineID => "CrateOverlayTexturesCustomRoutine";
        public override bool Enabled => true;

        public static readonly string TemplatePath = "/Game/Materials/modules/CrateMaterialInstances/CrateMaterialLogo_Ball";
        public static readonly string TemplateName = "CrateMaterialLogo_Ball";
        public static readonly string TemplateTexturePath = "/Game/UI/Textures/Icons/Packages/ui_icon_package_ball_generic";
        public static readonly string TemplateTextureName = "ui_icon_package_ball_generic";
        public override void Execute(ICustomRoutineAPI api)
        {
            UAsset crateMaterialTemplate = api.FindFile(TemplatePath);
            var crateMaterialTemplateNameMap = crateMaterialTemplate.GetNameMapIndexList();

            // locate relevant name map entries
            int miPath = -1;
            int miName = -1;
            int texturePath = -1;
            int textureName = -1;

            for (int i = 0; i < crateMaterialTemplateNameMap.Count; i++)
            {
                string nameEntry = crateMaterialTemplateNameMap[i].ToString();
                if (nameEntry == TemplatePath)
                {
                    miPath = i;
                }
                else if (nameEntry == TemplateName)
                {
                    miName = i;
                }
                else if (nameEntry == TemplateTexturePath)
                {
                    texturePath = i;
                }
                else if (nameEntry == TemplateTextureName)
                {
                    textureName = i;
                }
            }

            if (miPath == -1 || miName == -1 || texturePath == -1 || textureName == -1) throw new IOException("Failed to find all the required name map entries in template asset " + TemplatePath);

            // get all entries to add from metadata
            IReadOnlyList<Metadata> allMods = api.GetAllMods();
            List<string> texturePathsToAdd = new List<string>();
            foreach (Metadata mod in allMods)
            {
                if (api.ShouldExitNow()) return;
                if (mod?.IntegratorEntries.CrateOverlayTextures == null) continue;
                texturePathsToAdd.AddRange(mod.IntegratorEntries.CrateOverlayTextures);
            }

            // add entries to AstroGameSingletonInstance
            UAsset singletonInstance = api.FindFile("/Game/Globals/AstroGameSingletonInstance");
            NormalExport cdo = singletonInstance?.GetClassExport()?.ClassDefaultObject?.ToExport(singletonInstance) as NormalExport;
            if (cdo == null) throw new IOException("Failed to find CDO of AstroGameSingletonInstance");

            MapPropertyData crateLogoMaterialInstances = cdo["CrateLogoMaterialInstances"] as MapPropertyData;
            int desiredTextureIdx = 0;
            foreach (string desiredTexturePath in texturePathsToAdd)
            {
                // modify asset
                string newMIName = "CrateMaterialLogo_Modded" + desiredTextureIdx;
                string newMIPath = "/Game/Materials/modules/CrateMaterialInstances/" + newMIName;
                string desiredTextureName = desiredTexturePath.Split("/").Last();

                crateMaterialTemplate.SetNameReference(miPath, FString.FromString(newMIPath));
                crateMaterialTemplate.SetNameReference(miName, FString.FromString(newMIName));
                crateMaterialTemplate.SetNameReference(texturePath, FString.FromString(desiredTexturePath));
                crateMaterialTemplate.SetNameReference(textureName, FString.FromString(desiredTextureName));

                api.AddFile(newMIPath, crateMaterialTemplate); // AddFile immediately serializes the asset, so we are OK to continue modifying it afterwards

                // add new imports to singletonInstance
                FPackageIndex imp1 = singletonInstance.AddImport(new Import("/Script/CoreUObject", "Package", FPackageIndex.FromRawIndex(0), newMIPath, false, singletonInstance));
                FPackageIndex imp2 = singletonInstance.AddImport(new Import("/Script/Engine", "MaterialInstanceConstant", imp1, newMIName, false, singletonInstance));

                // add to crateLogoMaterialInstances
                crateLogoMaterialInstances.Value[new StrPropertyData() { Value = FString.FromString(desiredTextureName) }] = new ObjectPropertyData() { Value = imp2 };

                desiredTextureIdx++;
            }

            api.AddFile("/Game/Globals/AstroGameSingletonInstance", singletonInstance);

            api.LogToDisk("Completed " + RoutineID, false);
        }
    }
}
