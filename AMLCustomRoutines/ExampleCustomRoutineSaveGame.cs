using AstroModIntegrator;
using UAssetAPI;
using UAssetAPI.ExportTypes;
using UAssetAPI.PropertyTypes.Objects;
using UAssetAPI.PropertyTypes.Structs;

namespace AMLCustomRoutines
{
    // This routine reads a ModConfig save game file.
    public class ExampleCustomRoutineSaveGame : CustomRoutine
    {
        public override string RoutineID => "ExampleCustomRoutineSaveGame";
        public override bool Enabled => false;
        public override int APIVersion => 1;

        public override void Execute(ICustomRoutineAPI api)
        {
            UeSaveGame.SaveGame modConfig = api.ReadSave("ModConfigExample.savecfg");
            if (modConfig == null || modConfig.Properties == null)
            {
                api.LogToDisk("Save not found.");
            }
            else
            {
                foreach (UeSaveGame.FPropertyTag? prop in modConfig.Properties)
                {
                    if (prop == null) continue;
                    api.LogToDisk(prop.Name + ": " + prop.Property?.ToString());
                }
            }

            api.LogToDisk("Completed " + RoutineID);
        }
    }
}
