using AstroModIntegrator;

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
            Dictionary<string, object>? config = null;

            // parse config
            UeSaveGame.SaveGame modConfig = api.ReadSave("ModConfigExample.savecfg");
            if (modConfig == null || modConfig.Properties == null)
            {
                api.LogToDisk("Save not found.");
            }
            else
            {
                config = new Dictionary<string, object>();

                foreach (UeSaveGame.FPropertyTag? prop in modConfig.Properties)
                {
                    if (prop == null || prop.Property == null) continue;

                    // this accounts for most major primitives
                    // missing: array, struct (vector/transform/rotator), map, etc.
                    if (prop.Property is UeSaveGame.PropertyTypes.DoubleProperty)
                    {
                        config[prop.Name] = ((UeSaveGame.PropertyTypes.DoubleProperty)prop.Property).Value; // double
                    }
                    else if (prop.Property is UeSaveGame.PropertyTypes.FloatProperty)
                    {
                        config[prop.Name] = ((UeSaveGame.PropertyTypes.FloatProperty)prop.Property).Value; // float
                    }
                    else if (prop.Property is UeSaveGame.PropertyTypes.IntProperty)
                    {
                        config[prop.Name] = ((UeSaveGame.PropertyTypes.IntProperty)prop.Property).Value; // int
                    }
                    else if (prop.Property is UeSaveGame.PropertyTypes.Int64Property)
                    {
                        config[prop.Name] = ((UeSaveGame.PropertyTypes.Int64Property)prop.Property).Value; // long
                    }
                    else if (prop.Property is UeSaveGame.PropertyTypes.ByteProperty)
                    {
                        object? bytePropertyVal = ((UeSaveGame.PropertyTypes.ByteProperty)prop.Property).Value;
                        if (bytePropertyVal is UeSaveGame.FString)
                        {
                            config[prop.Name] = ((UeSaveGame.FString)bytePropertyVal).Value; // string
                        }
                        else if (bytePropertyVal != null)
                        {
                            config[prop.Name] = (byte)bytePropertyVal; // byte
                        }
                    }
                    else if (prop.Property is UeSaveGame.PropertyTypes.StrProperty)
                    {
                        config[prop.Name] = ((UeSaveGame.PropertyTypes.StrProperty)prop.Property).Value?.Value ?? "null"; // string
                    }
                    else if (prop.Property is UeSaveGame.PropertyTypes.NameProperty)
                    {
                        config[prop.Name] = ((UeSaveGame.PropertyTypes.NameProperty)prop.Property).Value?.Value ?? "null"; // string
                    }
                    else if (prop.Property is UeSaveGame.PropertyTypes.BoolProperty)
                    {
                        config[prop.Name] = ((UeSaveGame.PropertyTypes.BoolProperty)prop.Property).Value; // bool
                    }
                    else
                    {
                        api.LogToDisk("Unknown property type: " + prop.Property.GetType().Name);
                    }
                }
            }

            // print config to log
            // note that properties that are set to their default value will not be present in the save file. if a value is missing, it will not be printed here
            if (config != null)
            {
                foreach (KeyValuePair<string, object> kvp in config)
                {
                    api.LogToDisk(kvp.Key + " = " + kvp.Value.ToString());
                }
            }

            api.LogToDisk("Completed " + RoutineID);
        }
    }
}
