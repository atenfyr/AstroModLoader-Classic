using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AstroModLoader
{
    public static class UE4SSManager
    {
        public static bool Install(string binaryDir, string InstallPathLua, Form displayForm = null)
        {
            // clean first for good measure
            Uninstall(binaryDir, displayForm);

            string tempDownloadFolder = Path.Combine(Path.GetTempPath(), "AstroModLoader", "UE4SSDownload");
            string ue4ssZipPath = Path.Combine(tempDownloadFolder, "UE4SS.zip");

            try
            {
                try
                {
                    Directory.CreateDirectory(tempDownloadFolder);

                    using (var wb = new WebClient())
                    {
                        wb.Headers[HttpRequestHeader.UserAgent] = AMLUtils.UserAgent;
                        wb.DownloadFile(new Uri("https://github.com/UE4SS-RE/RE-UE4SS/releases/download/experimental/UE4SS_v3.0.1-240-g1244004.zip"), ue4ssZipPath);
                    }
                }
                catch
                {
                    if (displayForm != null) AMLUtils.InvokeUI(() => displayForm.ShowBasicButton("Failed to download UE4SS from the web!", "OK", null, null));
                    return false;
                }

                try
                {
                    System.IO.Compression.ZipFile.ExtractToDirectory(ue4ssZipPath, binaryDir, true);

                    // if the output has a directory called "ue4ss," move everything there to the root directory
                    try
                    {
                        string[] filesWithinue4ssFolder = Directory.GetFiles(Path.Combine(binaryDir, "ue4ss"));
                        foreach (string subPath in filesWithinue4ssFolder)
                        {
                            File.Move(subPath, Path.Combine(subPath, "..", "..", Path.GetFileName(subPath)), true);
                        }
                        Directory.Delete(Path.Combine(binaryDir, "ue4ss"), true);
                    }
                    catch { }

                    Directory.CreateDirectory(Path.Combine(binaryDir, "UE4SS_Signatures"));
                    File.WriteAllText(Path.Combine(binaryDir, "UE4SS_Signatures", "GUObjectArray.lua"), "function Register()\n    return \"8B 05 ?? ?? ?? ?? 3B 05 ?? ?? ?? ?? 75 ?? 48 8D 15 ?? ?? ?? ?? 48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 48 8D 05\"\nend\n\nfunction OnMatchFound(MatchAddress)\n    local JmpInstr = MatchAddress + 24\n    return JmpInstr + DerefToInt32(JmpInstr) + 4\nend");

                    string modifiedText = File.ReadAllText(Path.Combine(binaryDir, "UE4SS-settings.ini")).Replace("ModsFolderPath =", "ModsFolderPath = " + InstallPathLua);
                    File.WriteAllText(Path.Combine(binaryDir, "UE4SS-settings.ini"), modifiedText);
                }
                catch
                {
                    if (displayForm != null) AMLUtils.InvokeUI(() => displayForm.ShowBasicButton("Failed to unpack UE4SS!", "OK", null, null));
                    UE4SSManager.Uninstall(binaryDir);
                    return false;
                }

                try
                {
                    Directory.Delete(Path.Combine(binaryDir, "Mods"), true);
                }
                catch { }
            }
            finally
            {
                Directory.Delete(tempDownloadFolder, true);
            }
            return true;
        }

        public static bool Uninstall(string binaryDir, Form displayForm = null)
        {
            try
            {
                string[] allPaths = Directory.GetFileSystemEntries(binaryDir);
                foreach (string path in allPaths)
                {
                    string fileName = new FileInfo(path).Name.ToLower();
                    if (!fileName.StartsWith("astro"))
                    {
                        try
                        {
                            File.Delete(path);
                        }
                        catch { }

                        try
                        {
                            Directory.Delete(path, true);
                        }
                        catch { }
                    }
                }
            }
            catch
            {
                if (displayForm != null) AMLUtils.InvokeUI(() => displayForm.ShowBasicButton("Failed to uninstall UE4SS!", "OK", null, null));
                return false;
            }

            if (File.Exists(Path.Combine(binaryDir, "UE4SS.dll")))
            {
                if (displayForm != null) AMLUtils.InvokeUI(() => displayForm.ShowBasicButton("Failed to uninstall UE4SS!", "OK", null, null));
                return false;
            }

            return true;
        }
    }
}
