using CommandLine;
using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AstroModLoader
{
    // BEGIN NON-MIT LICENSED SECTION //

    // THE FOLLOWING METHODS ARE ADAPTED FROM STACK OVERFLOW ANSWERS!
    // THEY ARE NOT LICENSED UNDER MIT. SEE COMMENTS FOR FURTHER INFORMATION

    // CC BY-SA 3.0 (https://creativecommons.org/licenses/by-sa/3.0/deed.en)
    // This code is copyrighted by StackOverflow user "Dzmitry Lahoda" https://stackoverflow.com/users/173073/dzmitry-lahoda
    // Minor changes were made to this source code from the original. No warranties are given. See the original license text for more information.
    // https://stackoverflow.com/a/14424623

    public static class NativeMethods
    {
        public const string LOW_INTEGRITY_SSL_SACL = "S:(ML;;NW;;;LW)";

        public static int ERROR_SUCCESS = 0x0;

        public const int LABEL_SECURITY_INFORMATION = 0x00000010;

        public enum SE_OBJECT_TYPE
        {
            SE_UNKNOWN_OBJECT_TYPE = 0,
            SE_FILE_OBJECT,
            SE_SERVICE,
            SE_PRINTER,
            SE_REGISTRY_KEY,
            SE_LMSHARE,
            SE_KERNEL_OBJECT,
            SE_WINDOW_OBJECT,
            SE_DS_OBJECT,
            SE_DS_OBJECT_ALL,
            SE_PROVIDER_DEFINED_OBJECT,
            SE_WMIGUID_OBJECT,
            SE_REGISTRY_WOW64_32KEY
        }



        [DllImport("advapi32.dll", EntryPoint = "ConvertStringSecurityDescriptorToSecurityDescriptorW")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern Boolean ConvertStringSecurityDescriptorToSecurityDescriptor(
            [MarshalAs(UnmanagedType.LPWStr)] String strSecurityDescriptor,
            UInt32 sDRevision,
            ref IntPtr securityDescriptor,
            ref UInt32 securityDescriptorSize);

        [DllImport("kernel32.dll", EntryPoint = "LocalFree")]
        public static extern UInt32 LocalFree(IntPtr hMem);

        [DllImport("Advapi32.dll", EntryPoint = "SetSecurityInfo")]
        public static extern int SetSecurityInfo(SafeHandle hFileMappingObject,
                                                    SE_OBJECT_TYPE objectType,
                                                    Int32 securityInfo,
                                                    IntPtr psidOwner,
                                                    IntPtr psidGroup,
                                                    IntPtr pDacl,
                                                    IntPtr pSacl);
        [DllImport("advapi32.dll", EntryPoint = "GetSecurityDescriptorSacl")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern Boolean GetSecurityDescriptorSacl(
            IntPtr pSecurityDescriptor,
            out IntPtr lpbSaclPresent,
            out IntPtr pSacl,
            out IntPtr lpbSaclDefaulted);
    }

    public class InterProcessSecurity
    {

        public static void SetLowIntegrityLevel(SafeHandle hObject)
        {
            IntPtr pSD = IntPtr.Zero;
            IntPtr pSacl;
            IntPtr lpbSaclPresent;
            IntPtr lpbSaclDefaulted;
            uint securityDescriptorSize = 0;

            if (NativeMethods.ConvertStringSecurityDescriptorToSecurityDescriptor(NativeMethods.LOW_INTEGRITY_SSL_SACL, 1, ref pSD, ref securityDescriptorSize))
            {
                if (NativeMethods.GetSecurityDescriptorSacl(pSD, out lpbSaclPresent, out pSacl, out lpbSaclDefaulted))
                {
                    var err = NativeMethods.SetSecurityInfo(hObject,
                                                  NativeMethods.SE_OBJECT_TYPE.SE_KERNEL_OBJECT,
                                                  NativeMethods.LABEL_SECURITY_INFORMATION,
                                                  IntPtr.Zero,
                                                  IntPtr.Zero,
                                                  IntPtr.Zero,
                                                  pSacl);
                    if (err != NativeMethods.ERROR_SUCCESS)
                    {
                        throw new Win32Exception(err);
                    }
                }
                NativeMethods.LocalFree(pSD);
            }
        }
    }
    // END NON-MIT LICENSED SECTION //

    // ALL CODE FROM THIS POINT ON IS MIT LICENSED BY ATENFYR
    // SEE THE "LICENSE" FILE FOR MORE INFORMATION

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

        [Option("install_mod", Required = false, HelpText = "Specifies a path to a mod to install.")]
        public string InstallMod { get; set; }

        [Option("install_thunderstore", Required = false, HelpText = "Used for the ror2mm URL protocol.")]
        public string InstallThunderstore { get; set; }
    }

    public static class Program
    {
        public static Options CommandLineOptions;
        public static volatile bool ExpectingPak = false;
        public static volatile bool GotPak = false;
        public static volatile string Cwd = null;
        public static readonly string PipeUniqID = "AstroModLoader-Classic-192637418";

        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AstroModLoader"));

                // dump ModIntegrator.exe
                using (var resource = typeof(Program).Assembly.GetManifestResourceStream("AstroModLoader.ModIntegrator.exe"))
                {
                    if (resource != null)
                    {
                        using (var file = new FileStream(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AstroModLoader", "ModIntegrator.exe"), FileMode.Create, FileAccess.Write))
                        {
                            resource.CopyTo(file);
                        }
                    }
                }

                // dump repak_bind ourselves because low-integrity uassetapi cannot
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    using (var resource = typeof(UAssetAPI.PropertyTypes.Objects.PropertyData).Assembly.GetManifestResourceStream("UAssetAPI.repak_bind.so"))
                    {
                        if (resource != null)
                        {
                            using (var file = new FileStream(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AstroModLoader", "repak_bind.so"), FileMode.Create, FileAccess.Write))
                            {
                                resource.CopyTo(file);
                            }
                        }
                    }
                }
                else
                {
                    using (var resource = typeof(UAssetAPI.PropertyTypes.Objects.PropertyData).Assembly.GetManifestResourceStream("UAssetAPI.repak_bind.dll"))
                    {
                        if (resource != null)
                        {
                            using (var file = new FileStream(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AstroModLoader", "repak_bind.dll"), FileMode.Create, FileAccess.Write))
                            {
                                resource.CopyTo(file);
                            }
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                // some error occurred we need to output...
                Clipboard.SetText(ex.ToString());
                return;
            }

            Parser.Default.ParseArguments<Options>(args)
            .WithParsed(o =>
            {
                try
                {
                    CommandLineOptions = o;
                    if (CommandLineOptions.ForceClient) CommandLineOptions.ServerMode = false;
                    else if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "AstroServer.exe"))) CommandLineOptions.ServerMode = true;

                    if (Environment.OSVersion.Version.Major >= 6) SetProcessDPIAware();
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    Application.SetDefaultFont(new Font(new FontFamily("Microsoft Sans Serif"), 8.25f)); // default font changed in .NET Core 3.0

                    // if available, we want to accept the ror2mm url protocol; but if other software is installed that accepts it, we want them to override us
                    try
                    {
                        string thunderstoreProtocol = "ror2mm";
                        RegistryKey key = Registry.CurrentUser.OpenSubKey("Software\\Classes\\" + thunderstoreProtocol);
                        bool canWeContinue = key == null;
                        if (key != null)
                        {
                            var key2 = key.OpenSubKey(@"shell\open\command");
                            if (key2.GetValue(string.Empty) is string blah && blah.Contains("AstroModLoader"))
                            {
                                canWeContinue = true;
                            }
                            key2.Close();
                            key.Close();
                        }
                        if (canWeContinue)
                        {
                            key = Registry.CurrentUser.CreateSubKey("Software\\Classes\\" + thunderstoreProtocol);
                            key.SetValue(string.Empty, "URL: " + thunderstoreProtocol);
                            key.SetValue("URL Protocol", string.Empty);

                            var key2 = key.CreateSubKey(@"shell\open\command");
                            key2.SetValue(string.Empty, Application.ExecutablePath + " --install_thunderstore=" + "%1");
                            key2.Close();
                            key.Close();
                        }
                    }
                    catch
                    {
                        // no big deal if it doesn't work
                    }

                    using (Mutex mutex = new Mutex(false, PipeUniqID))
                    {
                        if (!mutex.WaitOne(5000, false))
                        {
                            // program already running, let's communicate our InstallThunderstore to them if needed
                            if (!string.IsNullOrEmpty(Program.CommandLineOptions.InstallThunderstore))
                            {
                                try
                                {
                                    using (var client = new NamedPipeClientStream(".", PipeUniqID, PipeDirection.Out))
                                    {
                                        client.Connect(5);

                                        using (var writer = new StreamWriter(client))
                                        {
                                            writer.WriteLine("InstallThunderstore:" + Program.CommandLineOptions.InstallThunderstore);
                                            writer.WriteLine("Disconnect");
                                            writer.Flush();
                                        }
                                    }
                                }
                                catch { } // if failed to connect, that's OK, but we need to make sure the process ends
                            }
                            else
                            {
                                try
                                {
                                    using (var client = new NamedPipeClientStream(".", PipeUniqID, PipeDirection.Out))
                                    {
                                        client.Connect(5);

                                        using (var writer = new StreamWriter(client))
                                        {
                                            writer.WriteLine("Focus");
                                            writer.WriteLine("Disconnect");
                                            writer.Flush();
                                        }
                                    }
                                }
                                catch { } // if failed to connect, that's OK, but we need to make sure the process ends
                            }
                            return;
                        }

                        Form1 f1 = new Form1();

                        // open named pipe server
                        Task.Factory.StartNew(() =>
                        {
                            while (true)
                            {
                                using (var server = new NamedPipeServerStream(PipeUniqID))
                                {
                                    try
                                    {
                                        server.Disconnect();
                                    }
                                    catch { }

                                    InterProcessSecurity.SetLowIntegrityLevel(server.SafePipeHandle);

                                    server.WaitForConnection();

                                    // continually respond to messages until we receive Disconnect
                                    using (var reader = new StreamReader(server))
                                    {
                                        using (var writer = new StreamWriter(server))
                                        {
                                            bool keepResponding = true;
                                            while (keepResponding)
                                            {
                                                string myLine = reader.ReadLine();
                                                switch (myLine)
                                                {
                                                    case null:
                                                        keepResponding = true;
                                                        break;
                                                    case "Disconnect":
                                                        keepResponding = false;
                                                        break;
                                                    case "WriteFile:ClientTransmitIntegratorPak":
                                                        if (!ExpectingPak || f1?.ModManager == null) break;
                                                        if (f1.ModManager.GetReadOnly()) break;

                                                        GotPak = false;
                                                        if (int.TryParse(reader.ReadLine(), out int numBytes))
                                                        {
                                                            byte[] pakData = new byte[numBytes];
                                                            server.Read(pakData, 0, numBytes);
                                                            if (pakData != null && pakData.Length > 0)
                                                            {
                                                                File.WriteAllBytes(Path.Combine(f1.ModManager.InstallPath, "999-AstroModIntegrator_P.pak"), pakData);
                                                                GotPak = true;
                                                            }
                                                        }
                                                        break;
                                                    case "WriteFile:Log":
                                                        if (string.IsNullOrEmpty(Program.Cwd)) break;
                                                        {
                                                            int numBytes1 = int.Parse(reader.ReadLine());
                                                            if (numBytes1 == 0) continue;
                                                            byte[] data1 = new byte[numBytes1];
                                                            server.Read(data1, 0, numBytes1);
                                                            File.AppendAllText(Path.Combine(Program.Cwd, "ModIntegrator.log"), Encoding.UTF8.GetString(data1));
                                                        }
                                                        break;
                                                    case "WriteFile:ClientTransmitUE4SSMods":
                                                        if (!ExpectingPak || f1?.ModManager == null) break;
                                                        if (f1.ModManager.GetReadOnly()) break;

                                                        while (true)
                                                        {
                                                            string modId = reader.ReadLine().Replace("..", "");
                                                            if (modId == "Stop") break;
                                                            string modSubPath = reader.ReadLine().Replace("..", "");
                                                            int numBytes1 = int.Parse(reader.ReadLine());
                                                            if (numBytes1 == 0) continue;
                                                            byte[] data1 = new byte[numBytes1];
                                                            server.Read(data1, 0, numBytes1);

                                                            string newPath = Path.Combine(f1.ModManager.InstallPathLua, modId, modSubPath);
                                                            Directory.CreateDirectory(Path.GetDirectoryName(newPath));
                                                            File.WriteAllBytes(newPath, data1);
                                                        }

                                                        {
                                                            int numBytes1 = int.Parse(reader.ReadLine());
                                                            if (numBytes1 > 0)
                                                            {
                                                                byte[] data1 = new byte[numBytes1];
                                                                server.Read(data1, 0, numBytes1);

                                                                string newPath = Path.Combine(f1.ModManager.InstallPathLua, "mods.txt");
                                                                Directory.CreateDirectory(Path.GetDirectoryName(newPath));
                                                                File.WriteAllBytes(newPath, data1);
                                                            }
                                                        }
                                                        {
                                                            int numBytes1 = int.Parse(reader.ReadLine());
                                                            if (numBytes1 > 0)
                                                            {
                                                                byte[] data1 = new byte[numBytes1];
                                                                server.Read(data1, 0, numBytes1);

                                                                string newPath = Path.Combine(f1.ModManager.InstallPathLua, "shared", "UEHelpers", "UEHelpers.lua");
                                                                Directory.CreateDirectory(Path.GetDirectoryName(newPath));
                                                                File.WriteAllBytes(newPath, data1);

                                                                string[] binariesDir = Directory.GetDirectories(Path.Combine(f1.ModManager.GamePath, "Astro", "Binaries"), "*", SearchOption.TopDirectoryOnly);
                                                                if (binariesDir.Length > 0)
                                                                {
                                                                    try
                                                                    {
                                                                        string newPath2 = Path.Combine(binariesDir[0], "ue4ss", "Mods", "shared", "UEHelpers", "UEHelpers.lua");
                                                                        Directory.CreateDirectory(Path.GetDirectoryName(newPath2));
                                                                        File.WriteAllBytes(newPath2, data1);
                                                                    }
                                                                    catch { }
                                                                }
                                                            }
                                                        }
                                                        {
                                                            int numBytes1 = int.Parse(reader.ReadLine());
                                                            if (numBytes1 > 0)
                                                            {
                                                                byte[] data1 = new byte[numBytes1];
                                                                server.Read(data1, 0, numBytes1);

                                                                string newPath = Path.Combine(f1.ModManager.InstallPathLua, "shared", "AstroHelpers", "AstroHelpers.lua");
                                                                Directory.CreateDirectory(Path.GetDirectoryName(newPath));
                                                                File.WriteAllBytes(newPath, data1);

                                                                string[] binariesDir = Directory.GetDirectories(Path.Combine(f1.ModManager.GamePath, "Astro", "Binaries"), "*", SearchOption.TopDirectoryOnly);
                                                                if (binariesDir.Length > 0)
                                                                {
                                                                    try
                                                                    {
                                                                        string newPath2 = Path.Combine(binariesDir[0], "ue4ss", "Mods", "shared", "AstroHelpers", "AstroHelpers.lua");
                                                                        Directory.CreateDirectory(Path.GetDirectoryName(newPath2));
                                                                        File.WriteAllBytes(newPath2, data1);
                                                                    }
                                                                    catch { }
                                                                }
                                                            }
                                                        }
                                                        break;
                                                    default:
                                                        // pass to main program
                                                        f1.ReceivePipe(myLine);
                                                        break;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        });
                        Application.Run(f1);
                    }
                }
                catch (Exception ex)
                {
                    // some error occurred we need to output...
                    Clipboard.SetText(ex.ToString());
                }
            });
        }
    }
}
