# AstroModIntegrator Classic
Integrates Astroneer mods based on their metadata.json files in order to avoid mod conflict.

## Features
This is a fork of the original C# AstroTechies AstroModIntegrator software. It has been updated for the newest versions of the game (as of December 2024). It includes:
- support for metadata schema versions 1 and 2
- integrator operations: persistent_actors, mission_trailheads, linked_actor_components, item_list_entries, biome_placement_modifiers
- mod mismatch verification with servers at runtime
- ~70% average integration time decrease compared to [astro_modloader](https://github.com/AstroTechies/astro_modloader)
- [custom Blueprint API](https://github.com/atenfyr/AstroModLoader-Classic/tree/master/AstroModIntegrator/BlueprintAssets)

## Licensing
AstroModIntegrator Classic is licensed under the MIT license, which can be found in [the LICENSE.md file.](https://github.com/atenfyr/AstroModIntegrator/blob/master/LICENSE.md) In addition, necessary licenses for the third-party material used in this software can be found in [the NOTICE.md file.](https://github.com/atenfyr/AstroModIntegrator/blob/master/NOTICE.md)

## Sample Code
### Sample Integration Code
```cs
Stopwatch stopWatch = new Stopwatch();
stopWatch.Start();

new ModIntegrator()
{
    RefuseVanillaConnections = true
}.IntegrateMods(Path.Combine(Environment.GetEnvironmentVariable("LocalAppData"), @"Astro\Saved\Paks"), @"C:\Program Files (x86)\Steam\steamapps\common\ASTRONEER\Astro\Content\Paks");

stopWatch.Stop();
Console.WriteLine("Finished integrating! Took " + ((double)stopWatch.Elapsed.Ticks / TimeSpan.TicksPerMillisecond) + " ms in total.");
```

### Sample .pak Parsing Code
```cs
Stopwatch extractingTimer = new Stopwatch();
extractingTimer.Start();

string pakPath = @"C:\Program Files (x86)\Steam\steamapps\common\ASTRONEER\Astro\Content\Paks\Astro-WindowsNoEditor.pak";

string extractingDir = Path.Combine(Path.GetDirectoryName(pakPath), Path.GetFileNameWithoutExtension(pakPath));
Directory.CreateDirectory(extractingDir);
using (FileStream f = new FileStream(pakPath, FileMode.Open, FileAccess.Read))
{
    PakExtractor mainExtractor = new PakExtractor(new BinaryReader(f));
    IReadOnlyList<string> allPaths = mainExtractor.GetAllPaths(); // Get a list of every path that is contained within this pak file. The provided mount point is ignored
    foreach (string path in allPaths)
    {
        Console.WriteLine("Extracting " + path);

        string writingPathName = Path.Combine(extractingDir, path);
        Directory.CreateDirectory(Path.GetDirectoryName(writingPathName));

        byte[] allPathData = mainExtractor.ReadRaw(path); // Read the bytes of this particular asset based off its path within the pak file
        File.WriteAllBytes(writingPathName, allPathData);
    }
}

extractingTimer.Stop();
Console.WriteLine("Finished extracting! Took " + ((double)extractingTimer.Elapsed.Ticks / TimeSpan.TicksPerSecond) + " seconds in total.");
```
