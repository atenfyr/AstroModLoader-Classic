using AstroModIntegrator;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace ModIntegratorCMD
{
    public class Program
    {
        public static void Main(string[] args)
        {
            /*// @"C:\Program Files (x86)\Steam\steamapps\common\ASTRONEER\Astro\Content\Paks\Astro-WindowsNoEditor.pak"
            using (FileStream f = new FileStream(@"C:\Users\Alexandros\AppData\Local\Astro\Saved\Paks\000-Minimap-0.1.1_P.pak", FileMode.Open, FileAccess.Read))
            {
                var ext = new MetadataExtractor(new BinaryReader(f));
                foreach (string thing in ext.PathToOffset.Keys)
                {
                    Console.WriteLine(thing + ": " + ext.PathToOffset[thing]);
                }
                Console.WriteLine(BitConverter.ToString(ext.ReadRaw("metadata.json")).Replace("-", " "));
            }

            Console.ReadKey();*/

            /*{
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

                        byte[] allPathData = mainExtractor.ReadRaw(path, true); // Read the bytes of this particular asset based off its path within the pak file
                        File.WriteAllBytes(writingPathName, allPathData);
                    }
                }

                extractingTimer.Stop();
                Console.WriteLine("Finished extracting! Took " + ((double)extractingTimer.Elapsed.Ticks / TimeSpan.TicksPerSecond) + " seconds in total.");
                return;
            }*/

            /*{
                Stopwatch testTimer = new Stopwatch();
                string pakPath = @"C:\Program Files (x86)\Steam\steamapps\common\ASTRONEER\Astro\Content\Paks\Astro-WindowsNoEditor.pak";

                using (FileStream f = new FileStream(pakPath, FileMode.Open, FileAccess.Read))
                {
                    testTimer.Start();
                    PakExtractor mainExtractor = new PakExtractor(new BinaryReader(f));
                    testTimer.Stop();
                    Console.WriteLine("Parsed index in " + ((double)testTimer.Elapsed.Ticks / TimeSpan.TicksPerMillisecond) + " ms");

                    IReadOnlyList<string> allPaths = mainExtractor.GetAllPaths();
                    int numTests = 5000;
                    testTimer.Reset();
                    testTimer.Start();
                    for (int i = 0; i < numTests; i++)
                    {
                        byte[] allPathData = mainExtractor.ReadRaw(allPaths[i]);
                    }
                    testTimer.Stop();
                    Console.WriteLine("~" + ((double)testTimer.Elapsed.Ticks / TimeSpan.TicksPerMillisecond) / numTests + " ms per asset read");
                    //Console.ReadLine();
                }

                return;
            }*/

            if (args.Length != 2)
            {
                string decidedPath = "C:\\Users\\YOU\\AppData\\Local\\Astro\\Saved\\Paks";
                try
                {
                    decidedPath = Path.Combine(Environment.GetEnvironmentVariable("LocalAppData") ?? throw new InvalidOperationException(), @"Astro\Saved\Paks");
                }
                catch {}

                Console.WriteLine("AstroModIntegrator - Automatically integrates Astroneer .pak mods based on their metadata\n");
                Console.WriteLine("Usage: modintegrator <active mod paks directory> <game installation paks directory>\n");
                Console.WriteLine("Example: modintegrator \"" + decidedPath + "\" \"C:\\Program Files (x86)\\Steam\\steamapps\\common\\ASTRONEER\\Astro\\Content\\Paks\"");
                return;
            }

            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            ModIntegrator us = new ModIntegrator()
            {
                RefuseMismatchedConnections = true,
                //OptionalModIDs = new List<string> { "AstroChat" }
            };
            us.IntegrateMods(args[0], args[1]);
            stopWatch.Stop();

            Console.WriteLine("Finished integrating! Took " + ((double)stopWatch.Elapsed.Ticks / TimeSpan.TicksPerMillisecond) + " ms in total.");
        }
    }
}
