using AstroModIntegrator;
using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Runtime.InteropServices;
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

        [Option('p', "proton", Required = false, Default = false, HelpText = "If specified, attempt to find default values for -i and -g assuming a Proton game environment.")]
        public bool ProtonFlag { get; set; }

        public Options()
        {

        }
    }

    public class Program
    {
        // exit immediately, ensures dangling threads are killed
        private static bool waitForInputAtEnd = false;
        private static void Exit(int exitCode)
        {
            if (waitForInputAtEnd)
            {
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey(true);
            }
            Environment.Exit(exitCode);
        }

        public static void Main(string[] args_raw)
        {
            string[] args = args_raw;
#if DEBUG
            // if in debug configuration and no parameters, attempt to connect to main AML pipe
            try
            {
                string gamePaksPath = "D:\\Games\\steamapps\\common\\ASTRONEER\\Astro\\Content\\Paks";
                if (args.Length == 0 && Directory.Exists(gamePaksPath))
                {
                    string decidedPath = Path.Combine(Environment.GetEnvironmentVariable("LocalAppData") ?? throw new InvalidOperationException(), "Astro", "Saved", "Paks");
                    args = ["-i", decidedPath, "-g", gamePaksPath, "-v", "--enable_custom_routines", "--benchmark", "10", "--pak_to_named_pipe", "AstroModLoader-Classic-192637418"];
                    Console.WriteLine("Entering named pipe benchmark mode; if this is a mistake, execute with the --help parameter or execute in the Release configuration");
                }
            }
            catch { }
#endif
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
            bool protonFlag = false;
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
                if (args[i] == "-p" || args[i] == "--proton")
                {
                    protonFlag = true;
                }
            }
            // set protonFlag = true if on Linux and no arguments specified
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && args.Length == 0)
            {
                protonFlag = true;
                waitForInputAtEnd = true;
            }

            if (printVersion)
            {
                Console.WriteLine(IntegratorUtils.CurrentVersion.ToString());
                return;
            }

            string copyrightNotice = "AstroModIntegrator Classic " + IntegratorUtils.CurrentVersion.ToString() + ": Automatically integrates Astroneer .pak mods based on their metadata";
            copyrightNotice += "\nCopyright (c) 2020 - " + DateTime.Now.Year.ToString() + " AstroTechies, atenfyr";

            if (protonFlag)
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Console.Error.WriteLine("--proton flag is not valid when executing on Windows");
                    return;
                }

                // attempt to find default paths for proton
                string gameID = "361420";
                string defaultI = "";
                string defaultG = "";

                string basePath = "";
                string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                string[] basePathOptions = [Path.Combine(homeDir, ".steam", "steam", "steamapps"), Path.Combine(homeDir, ".local", "share", "Steam", "steamapps")];
                foreach (string option in basePathOptions)
                {
                    if (Path.Exists(option))
                    {
                        basePath = option;
                        break;
                    }
                }

                if (!string.IsNullOrEmpty(basePath))
                {
                    defaultI = Path.Join(basePath, "compatdata", gameID, "pfx", "drive_c", "users", "steamuser", "AppData", "Local", "Astro");
                    if (Path.Exists(defaultI))
                    {
                        defaultI = Path.Join(defaultI, "Saved");
                        Directory.CreateDirectory(defaultI);
                        defaultI = Path.Join(defaultI, "Paks");
                        Directory.CreateDirectory(defaultI);
                    }

                    defaultG = Path.Join(basePath, "common", "ASTRONEER", "Astro", "Content", "Paks");
                }

                if (!string.IsNullOrEmpty(defaultI) && !string.IsNullOrEmpty(defaultG) && Path.Exists(defaultI) && Path.Exists(defaultG))
                {
                    Console.WriteLine(copyrightNotice);
                    Console.WriteLine("\nPlace your mods in the following directory:\n\n" + defaultI + "\n\nYou must execute this program every time that the game updates or you change your list of mods.\n");

                    try { File.Delete(Path.Combine(defaultI, "999-AstroModIntegrator_P.pak")); } catch {}
                    if (Directory.GetFiles(defaultI).Length == 0)
                    {
                        Console.WriteLine("No mods are currently installed, so integration will be skipped this time.");
                        Exit(0);
                    }

                    string[] newArgs = new string[args.Length + 5];
                    newArgs[0] = "-v";
                    newArgs[1] = "-i";
                    newArgs[2] = defaultI;
                    newArgs[3] = "-g";
                    newArgs[4] = defaultG;
                    Array.Copy(args, 0, newArgs, 5, args.Length);

                    args = newArgs;
                    argWithHyphenExists = true;
                }
                else
                {
                    Console.Error.WriteLine("Failed to find default paths for a Proton installation of Astroneer");
                    return;  
                }
            }

            if (args.Length < 2 || printHelp)
            {
                string decidedPath = "C:\\Users\\YOU\\AppData\\Local\\Astro\\Saved\\Paks";
                try
                {
                    decidedPath = Path.Combine(Environment.GetEnvironmentVariable("LocalAppData") ?? throw new InvalidOperationException(), "Astro", "Saved", "Paks");
                }
                catch { }

                Console.WriteLine(copyrightNotice);
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

            // start optimization now that we know that we'll actually integrate
            string optimizationDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AstroModLoader", "IntegratorProfileOptimization");
            try
            {
                Directory.CreateDirectory(optimizationDirectory);
            }
            catch { }
            ProfileOptimization.SetProfileRoot(optimizationDirectory);
            ProfileOptimization.StartProfile("Startup.Profile");

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
                        Exit(1);
                        break;
                    }
                    await Task.Delay(1000);
                }
            });

            if (argWithHyphenExists) // new
            {
                var parserResult = new Parser(with => with.HelpWriter = null).ParseArguments<Options>(args);

                parserResult.WithParsed(o =>
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
                            CallingExePath = o.CallingExePath,
                            IsModIntegratorCMD = true
                        };
                        us.IntegrateMods(o.ModPakDirectories?.ToArray(), o.GamePakDirectory, o.OutputFolder, o.MountPoint, o.ExtractLua, !o.DisableCleanLua);
                    }
                    stopWatch.Stop();

                    Console.WriteLine("Finished integrating! Took " + ((double)stopWatch.Elapsed.Ticks / TimeSpan.TicksPerMillisecond) + " ms in total.");
                    if (isBenchmark)
                    {
                        Console.WriteLine("(" + ((double)stopWatch.Elapsed.Ticks / TimeSpan.TicksPerMillisecond / (double)numBenchmarkTrials) + " ms per trial)");
                    }
                    Exit(0);
                }).WithNotParsed<Options>(errs =>
                {
                    var helpText = HelpText.AutoBuild(parserResult, h =>
                    {
                        h.Heading = "AstroModIntegrator Classic " + IntegratorUtils.CurrentVersion.ToString() + ": Automatically integrates Astroneer .pak mods based on their metadata";
                        h.Copyright = "Copyright (c) 2020 - " + DateTime.Now.Year.ToString() + " AstroTechies, atenfyr";
                        h.AdditionalNewLineAfterOption = false;
                        return HelpText.DefaultParsingErrorsHandler(parserResult, h);
                    });
                    Console.Error.WriteLine(helpText);
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
                    EnableCustomRoutines = enableCustomRoutines,
                    IsModIntegratorCMD = true
                };
                us.IntegrateMods(paksPaths.ToArray(), args[startOtherParams], outputFolder, mountPoint, extractLua, cleanLua);
                stopWatch.Stop();

                Console.WriteLine("Finished integrating! Took " + ((double)stopWatch.Elapsed.Ticks / TimeSpan.TicksPerMillisecond) + " ms in total.");
                Exit(0);
            }
        }
    }
}
