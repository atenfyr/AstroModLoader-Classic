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
#if DEBUG
            args = ["benchmark", Path.Combine(Environment.GetEnvironmentVariable("LocalAppData") ?? throw new InvalidOperationException(), @"Astro\Saved\Paks"), @"D:\Games\steamapps\common\ASTRONEER\Astro\Content\Paks\"];
#endif

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


            if (args.Length == 3 && args[0] == "benchmark")
            {
                ModIntegrator us = new ModIntegrator()
                {
                    RefuseMismatchedConnections = true,
                    //OptionalModIDs = new List<string> { "AstroChat" }
                };

                int TRIALS = 10;
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();
                for (int i = 0; i < TRIALS; i++)
                {
                    us.IntegrateMods(args[1], args[2]);
                }
                stopWatch.Stop();
                Console.WriteLine("Finished integrating! Took " + ((double)stopWatch.Elapsed.Ticks / TimeSpan.TicksPerMillisecond / TRIALS) + " ms per trial for " + TRIALS + " trials.");
                return;
            }

            // single-argument options
            if (args.Length >= 1 && args[0] == "version")
            {
                Console.WriteLine(IntegratorUtils.CurrentVersion.ToString());
                return;
            }
            if (args.Length >= 1 && args[0] == "license")
            {
                using (var resource = typeof(AstroModIntegrator.ModIntegrator).Assembly.GetManifestResourceStream("AstroModIntegrator.LICENSE.md"))
                {
                    if (resource != null)
                    {
                        using (StreamReader reader = new StreamReader(resource))
                        {
                            Console.WriteLine(reader.ReadToEnd());
                        }
                    }
                    else
                    {
                        Console.WriteLine("Failed to retrieve LICENSE.md. Please report this bug online on GitHub at https://github.com/atenfyr/AstroModLoader-Classic.\nYou may alternatively retrieve the LICENSE.md file online here: https://github.com/atenfyr/AstroModLoader-Classic/blob/master/AstroModIntegrator/LICENSE.md");
                    }
                }

                return;
            }
            if (args.Length >= 1 && args[0] == "notice")
            {
                using (var resource = typeof(AstroModIntegrator.ModIntegrator).Assembly.GetManifestResourceStream("AstroModIntegrator.NOTICE.md"))
                {
                    if (resource != null)
                    {
                        using (StreamReader reader = new StreamReader(resource))
                        {
                            Console.WriteLine(reader.ReadToEnd());
                        }
                    }
                    else
                    {
                        Console.WriteLine("Failed to retrieve NOTICE.md. Please report this bug online on GitHub at https://github.com/atenfyr/AstroModLoader-Classic.\nYou may alternatively retrieve the NOTICE.md file online here: https://github.com/atenfyr/AstroModLoader-Classic/blob/master/AstroModIntegrator/NOTICE.md");
                    }
                }

                return;
            }

            if (args.Length < 2)
            {
                string decidedPath = "C:\\Users\\YOU\\AppData\\Local\\Astro\\Saved\\Paks";
                try
                {
                    decidedPath = Path.Combine(Environment.GetEnvironmentVariable("LocalAppData") ?? throw new InvalidOperationException(), @"Astro\Saved\Paks");
                }
                catch {}

                Console.WriteLine("AstroModIntegrator Classic " + IntegratorUtils.CurrentVersion.ToString() + ": Automatically integrates Astroneer .pak mods based on their metadata\n");
                Console.WriteLine("Usage: modintegrator <active mod paks directory> <game installation paks directory> [folder to output to]\n");
                Console.WriteLine("Example: modintegrator \"" + decidedPath + "\" \"C:\\Program Files (x86)\\Steam\\steamapps\\common\\ASTRONEER\\Astro\\Content\\Paks\"");
                Console.WriteLine("\nExecute \"modintegrator license\" to receive a copy of the license agreement for this software.\nExecute \"modintegrator notice\" to receive a copy of the license agreements for the third-party material used in this software.");
                return;
            }

            {
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();

                int startOtherParams = 1;

                List<string> paksPaths = [args[0]];
                if (args[0] == "[")
                {
                    paksPaths.Clear();
                    while (args[startOtherParams] != "]")
                    {
                        paksPaths.Add(args[startOtherParams]);
                        startOtherParams++;
                    }
                    startOtherParams++;
                }

                if (args.Length <= startOtherParams)
                {
                    Console.WriteLine("Error: <game installation paks directory> not specified. Execute with no parameters to view help");
                    Console.WriteLine("All active mod paks directories: " + string.Join(", ", paksPaths));
                    return;
                }

                ModIntegrator us = new ModIntegrator()
                {
                    RefuseMismatchedConnections = true,
                    EnableCustomRoutines = false, // todo command line option
                    //OptionalModIDs = new List<string> { "AstroChat" }
                };
                us.IntegrateMods(paksPaths.ToArray(), args[startOtherParams], args.Length > startOtherParams+1 ? args[startOtherParams+1] : null, args.Length > (startOtherParams+2) ? args[startOtherParams+2] : null, args.Length > (startOtherParams+3) ? (args[startOtherParams+3].ToLowerInvariant() == "true") : false, args.Length > (startOtherParams + 4) ? (args[startOtherParams + 4].ToLowerInvariant() == "true") : false);
                stopWatch.Stop();

                Console.WriteLine("Finished integrating! Took " + ((double)stopWatch.Elapsed.Ticks / TimeSpan.TicksPerMillisecond) + " ms in total.");
                Environment.Exit(0);
            }
        }
    }
}
