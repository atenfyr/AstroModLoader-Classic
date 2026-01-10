using AstroModIntegrator;
using UAssetAPI;
using UAssetAPI.ExportTypes;
using UAssetAPI.PropertyTypes.Objects;
using UAssetAPI.PropertyTypes.Structs;

namespace AMLCustomRoutines
{
    // This routine modifies the Floodlight to require organic instead of tungsten
    public class ExampleCustomRoutine1 : CustomRoutine
    {
        public override string RoutineID => "ExampleCustomRoutine1";
        public override bool Enabled => true;

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

            api.LogToDisk("Completed " + RoutineID);
        }
    }
}
