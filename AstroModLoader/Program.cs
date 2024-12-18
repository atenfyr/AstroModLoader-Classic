﻿using CommandLine;
using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AstroModLoader
{
    public class Options
    {
        [Option("server", Required = false, HelpText = "Specifies that AstroModLoader is being ran for a server.")]
        public bool ServerMode { get; set; }

        [Option("client", Required = false, HelpText = "Specifies that AstroModLoader is being ran for a client.")]
        public bool ForceClient { get; set; }

        [Option("data", Required = false, HelpText = "Specifies the %localappdata% folder or the local equivalent of it.")]
        public string LocalDataPath { get; set; }

        [Option("next_launch_path", Required = false, HelpText = "Specifies a path to a file to store as the launch script.")]
        public string NextLaunchPath { get; set; }
    }

    public static class Program
    {
        public static Options CommandLineOptions;

        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
            .WithParsed(o =>
            {
                CommandLineOptions = o;
                if (CommandLineOptions.ForceClient) CommandLineOptions.ServerMode = false;
                else if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "AstroServer.exe"))) CommandLineOptions.ServerMode = true;

                if (Environment.OSVersion.Version.Major >= 6) SetProcessDPIAware();
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.SetDefaultFont(new Font(new FontFamily("Microsoft Sans Serif"), 8.25f)); // default font changed in .NET Core 3.0

                Form1 f1 = new Form1();
                Application.Run(f1);
            });
        }
    }
}
