using AstroModIntegrator;
using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ModIntegratorCMD
{
    public class Options
    {
        [Option('i', "input", Required = true, HelpText = "List of active mod paks directories.")]
        public IEnumerable<string> ModPakDirectories { get; set; }

        [Option('g', "game_pak_folder", Required = true, HelpText = "Game installation paks folder.")]
        public string GamePakDirectory { get; set; }

        [Option('o', "output", Required = false, Default = null, HelpText = "The folder to output the integrator .pak file to. Defaults to the first directory passed into the input parameter.")]
        public string OutputFolder { get; set; }

        [Option('v', "verbose", Required = false, Default = false, HelpText = "Whether or not to enable verbose logging to disk.")]
        public bool Verbose { get; set; }

        [Option("mount_point", Required = false, Default = "../../../", HelpText = "The integrator .pak file's mount point. Almost always should be left unspecified.")]
        public string MountPoint { get; set; }

        [Option("extract_lua", Required = false, Default = false, HelpText = "Whether or not to extract UE4SS mods.")]
        public bool ExtractLua { get; set; }

        [Option("disable_clean_lua", Required = false, Default = false, HelpText = "Whether or not to disable clean-up of UE4SS mods before execution.")]
        public bool DisableCleanLua { get; set; }

        [Option("enable_custom_routines", Required = false, Default = false, HelpText = "Whether or not to execute custom routines.")]
        public bool EnableCustomRoutines { get; set; }

        [Option("disable_refuse_mismatched_connections", Required = false, Default = false, HelpText = "Whether or not to disable refusing mismatched connections.")]
        public bool DisableRefuseMismatchedConnections { get; set; }

        [Option("pak_to_named_pipe", Required = false, Default = null, HelpText = "If specified, outputs written files to the specified named pipe instead of to disk. Used internally by AstroModLoader Classic. See source code for format.")]
        public string PakToNamedPipe { get; set; }

        [Option("calling_exe_path", Required = false, Default = null, Hidden = true, HelpText = "Used only for Debug_CustomRoutineTest configuration")]
        public string CallingExePath { get; set; }

        [Option("benchmark", Required = false, Default = 1, HelpText = "If specified, repeat integration multiple times for benchmarking purposes.")]
        public int BenchmarkTrials { get; set; }

        [Option("optional_mod_ids", Required = false, Default = null, HelpText = "List of optional mod IDs. Defaults to an empty list (i.e., clients are required to install all server-client mods).")]
        public IEnumerable<string> OptionalModIDs { get; set; }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            // single-argument options
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

            // check if any argument exists starting with "-"; if so, use new system, else, use old system
            bool argWithHyphenExists = false;
            bool printHelp = false;
            bool printVersion = false;
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].Length > 0 && args[i][0] == '-')
                {
                    argWithHyphenExists = true;
                }
                if (args[i] == "help" || args[i] == "--help" || args[i] == "-h")
                {
                    printHelp = true;
                }
                if (args[i] == "version" || args[i] == "--version")
                {
                    printVersion = true;
                }
            }

            if (printVersion)
            {
                Console.WriteLine(IntegratorUtils.CurrentVersion.ToString());
                return;
            }

            if (args.Length < 2 || printHelp)
            {
                string decidedPath = "C:\\Users\\YOU\\AppData\\Local\\Astro\\Saved\\Paks";
                try
                {
                    decidedPath = Path.Combine(Environment.GetEnvironmentVariable("LocalAppData") ?? throw new InvalidOperationException(), @"Astro\Saved\Paks");
                }
                catch { }

                Console.WriteLine("AstroModIntegrator Classic " + IntegratorUtils.CurrentVersion.ToString() + ": Automatically integrates Astroneer .pak mods based on their metadata");
                Console.WriteLine("\nParameters:");

                var parser = new Parser(with =>
                {
                    with.HelpWriter = null;
                    with.AutoVersion = false;
                    with.AutoHelp = false;
                });
                HelpText helpText = new HelpText { AddDashesToOption = true, AddNewLineBetweenHelpSections = true }.AddOptions(parser.ParseArguments<Options>(args));
                Console.WriteLine(helpText.ToString().Trim(['\r', '\n']));

                Console.WriteLine("\nExample: modintegrator -i \"" + decidedPath + "\" -g \"D:\\Games\\steamapps\\common\\ASTRONEER\\Astro\\Content\\Paks\"");
                Console.WriteLine("\nExecute \"modintegrator license\" to receive a copy of the license agreement for this software.\nExecute \"modintegrator notice\" to receive a copy of the license agreements for the third-party material used in this software.");
                return;
            }

            Stopwatch stopWatch = new Stopwatch();
            int numBenchmarkTrials = 1;

            // start watchdog
            Task.Factory.StartNew(async () =>
            {
                // wait until stopwatch starts
                while (!stopWatch.IsRunning)
                {
                    await Task.Delay(1000);
                }

                // keep running until stopwatch stops
                while (stopWatch.IsRunning)
                {
                    if (stopWatch.ElapsedMilliseconds > (15000 * numBenchmarkTrials))
                    {
                        Console.Error.WriteLine("Watchdog timer activated after " + stopWatch.ElapsedMilliseconds.ToString() + " ms; prematurely terminating program");
                        Environment.Exit(1);
                        break;
                    }
                    await Task.Delay(1000);
                }
            });

            if (argWithHyphenExists) // new
            {
                Parser.Default.ParseArguments<Options>(args).WithParsed(o =>
                {
                    bool isBenchmark = o.BenchmarkTrials > 1;
                    numBenchmarkTrials = isBenchmark ? o.BenchmarkTrials : 1;

                    stopWatch.Start();
                    for (int i = 0; i < numBenchmarkTrials; i++)
                    {
                        ModIntegrator us = new ModIntegrator()
                        {
                            RefuseMismatchedConnections = !o.DisableRefuseMismatchedConnections,
                            EnableCustomRoutines = o.EnableCustomRoutines,
                            OptionalModIDs = o.OptionalModIDs?.ToList() ?? new List<string>(),
                            Verbose = o.Verbose,
                            PakToNamedPipe = o.PakToNamedPipe,
                            CallingExePath = o.CallingExePath
                        };
                        us.IntegrateMods(o.ModPakDirectories?.ToArray(), o.GamePakDirectory, o.OutputFolder, o.MountPoint, o.ExtractLua, !o.DisableCleanLua);
                    }
                    stopWatch.Stop();

                    Console.WriteLine("Finished integrating! Took " + ((double)stopWatch.Elapsed.Ticks / TimeSpan.TicksPerMillisecond) + " ms in total.");
                    if (isBenchmark)
                    {
                        Console.WriteLine("(" + ((double)stopWatch.Elapsed.Ticks / TimeSpan.TicksPerMillisecond / (double)numBenchmarkTrials) + " ms per trial)");
                    }
                    Environment.Exit(0);
                });
            }
            else // old
            {
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

                string outputFolder = args.Length > (startOtherParams + 1) ? ((args[startOtherParams + 1] == "null") ? null : args[startOtherParams + 1]) : null;
                string mountPoint = args.Length > (startOtherParams + 2) ? ((args[startOtherParams + 2] == "null") ? null : args[startOtherParams + 2]) : null;
                bool extractLua = args.Length > (startOtherParams + 3) ? (args[startOtherParams + 3].ToLowerInvariant() == "true") : false;
                bool cleanLua = args.Length > (startOtherParams + 4) ? (args[startOtherParams + 4].ToLowerInvariant() == "true") : false;
                bool enableCustomRoutines = args.Length > (startOtherParams + 5) ? (args[startOtherParams + 5].ToLowerInvariant() == "true") : false;

                stopWatch.Start();

                ModIntegrator us = new ModIntegrator()
                {
                    RefuseMismatchedConnections = true,
                    EnableCustomRoutines = enableCustomRoutines
                };
                us.IntegrateMods(paksPaths.ToArray(), args[startOtherParams], outputFolder, mountPoint, extractLua, cleanLua);
                stopWatch.Stop();

                Console.WriteLine("Finished integrating! Took " + ((double)stopWatch.Elapsed.Ticks / TimeSpan.TicksPerMillisecond) + " ms in total.");
                Environment.Exit(0);
            }
        }
    }
}
