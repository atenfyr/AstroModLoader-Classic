using AstroModIntegrator;
using Newtonsoft.Json.Linq;
using UAssetAPI;
using UAssetAPI.ExportTypes;
using UAssetAPI.PropertyTypes.Objects;
using UAssetAPI.PropertyTypes.Structs;

namespace AMLCustomRoutine
{
    // This routine modifies the Floodlight to require organic instead of tungsten
    public class ExampleCustomRoutine1 : CustomRoutine
    {
        public override string RoutineID => "ExampleCustomRoutine1";

        public override void Execute(ICustomRoutineAPI api)
        {
            UAsset floodlightAsset = api.FindFile("/Game/Items/ItemTypes/FloodLight_IT");

            NormalExport exp = (NormalExport)(floodlightAsset.GetClassExport().ClassDefaultObject.ToExport(floodlightAsset));
            StructPropertyData constructionRecipe = exp["ConstructionRecipe"] as StructPropertyData;
            ArrayPropertyData ingredients = constructionRecipe["Ingredients"] as ArrayPropertyData;
            StructPropertyData ingredient0 = ingredients.Value[0] as StructPropertyData;
            ObjectPropertyData ingredient0type = ingredient0["ItemType"] as ObjectPropertyData;
            ingredient0type.Value = floodlightAsset.AddItemTypeImport("/Game/Items/ItemTypes/Minables/Organic");

            api.AddFile("/Game/Items/ItemTypes/FloodLight_IT", floodlightAsset);

            api.LogToDisk("Completed ExampleCustomRoutine1");
        }
    }

    // This routine logs the name of every mod with the "example" field in the "integrator" object of their metadata
    public class ExampleCustomRoutine2 : CustomRoutine
    {
        public override string RoutineID => "ExampleCustomRoutine2";

        public override void Execute(ICustomRoutineAPI api)
        {
            IReadOnlyList<Metadata> allMods = api.GetAllMods();
            foreach (Metadata mod in allMods)
            {
                api.LogToDisk("Parsing " + mod?.ModID ?? "null");
                if (mod?.IntegratorEntries.ExtraFields != null && mod.IntegratorEntries.ExtraFields.TryGetValue("example", out JToken val))
                {
                    api.LogToDisk(mod.ModID + ": example = " + val.Value<string>() ?? "???");
                }
            }

            api.LogToDisk("Completed ExampleCustomRoutine2");
        }
    }
}
