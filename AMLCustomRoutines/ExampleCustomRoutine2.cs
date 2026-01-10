using AstroModIntegrator;
using Newtonsoft.Json.Linq;

namespace AMLCustomRoutines
{
    // This routine logs the name of every mod with the "example" field in the "integrator" object of their metadata
    public class ExampleCustomRoutine2 : CustomRoutine
    {
        public override string RoutineID => "ExampleCustomRoutine2";
        public override bool Enabled => true;

        public override void Execute(ICustomRoutineAPI api)
        {
            IReadOnlyList<Metadata> allMods = api.GetAllMods();
            foreach (Metadata mod in allMods)
            {
                if (api.ShouldExitNow()) return;

                api.LogToDisk("Parsing " + mod?.ModID ?? "null");
                if (mod?.IntegratorEntries.ExtraFields != null && mod.IntegratorEntries.ExtraFields.TryGetValue("example", out JToken val))
                {
                    api.LogToDisk(mod.ModID + ": example = " + val.Value<string>() ?? "???");
                }
            }

            api.LogToDisk("Completed " + RoutineID);
        }
    }
}
