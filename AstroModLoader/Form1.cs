using AstroModIntegrator;
using Newtonsoft.Json;
using Semver;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AstroModLoader
{
    public partial class Form1 : Form
    {
        private CoolDataGridView _dgv1;

        public ModHandler ModManager;
        public TableHandler TableManager;
        public CoolDataGridView dataGridView1
        {
            get
            {
                if (AMLUtils.InvokeRequired()) throw new InvalidOperationException("Attempt to get Form1.dataGridView1 outside of UI thread");
                return _dgv1;
            }
            set
            {
                if (AMLUtils.InvokeRequired()) throw new InvalidOperationException("Attempt to set Form1.dataGridView1 outside of UI thread");
                _dgv1 = value;
            }
        }
        public Panel footerPanel;

        public string InformationalVersion;

        public Form1()
        {
            InitializeComponent();
            modInfo.Text = "";
            integratingLabel.Text = "";
            AMLUtils.InitializeInvoke(this);

            InformationalVersion = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
            this.Text = "AstroModLoader Classic v" + InformationalVersion;

            // Enable double buffering to look nicer
            if (!SystemInformation.TerminalServerSession)
            {
                Type ourGridType = dataGridView1.GetType();
                PropertyInfo pi = ourGridType.GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic);
                pi.SetValue(dataGridView1, true, null);
                this.DoubleBuffered = true;
            }
            dataGridView1.Select();

            if (Program.CommandLineOptions.ServerMode) syncButton.Hide();

            ModManager = new ModHandler(this);
            TableManager = new TableHandler(dataGridView1, ModManager);

            dataGridView1.CellValueChanged += DataGridView1_CellValueChanged;
            dataGridView1.CellContentClick += DataGridView1_CellContentClick;
            dataGridView1.DataBindingComplete += DataGridView1_DataBindingComplete;
            dataGridView1.CellEndEdit += DataGridView1_CellEndEdit;
            dataGridView1.SelectionChanged += new EventHandler(DataGridView1_SelectionChanged);
            footerPanel.Paint += Footer_Paint;
            AMLPalette.RefreshTheme(this);

            AllowDrop = true;
            DragEnter += new DragEventHandler(Form1_DragEnter);
            DragDrop += new DragEventHandler(Form1_DragDrop);
            dataGridView1.DragEnter += new DragEventHandler(Form1_DragEnter);
            dataGridView1.DragDrop += new DragEventHandler(Form1_DragDrop);

            PeriodicCheckTimer.Enabled = true;
            CheckAllDirty.Enabled = true;
            ForceAutoUpdateRefresh.Enabled = true;

            autoUpdater = new BackgroundWorker();
            autoUpdater.DoWork += new DoWorkEventHandler(AutoUpdater_DoWork);
            autoUpdater.RunWorkerCompleted += new RunWorkerCompletedEventHandler(Simple_Refresh_RunWorkerCompleted);
            autoUpdater.RunWorkerAsync();
        }

        private BackgroundWorker autoUpdater;

        private void AutoUpdater_DoWork(object sender, DoWorkEventArgs e)
        {
            if (ModManager != null) ModManager.AggregateIndexFiles();
        }

        private void Simple_Refresh_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (!AMLUtils.InvokeRequired())
            {
                TableManager.Refresh();
                ModManager.FullUpdate();
            }
        }

        public bool DownloadVersionSync(Mod thisMod, Version newVersion, IndexVersionData desiredVersionData = null, bool enableByDefault = false)
        {
            try
            {
                if (desiredVersionData == null)
                {
                    if (!ModManager.GlobalIndexFile.ContainsKey(thisMod.CurrentModData.ModID)) throw new IndexFileException("Can't find index file entry for mod: " + thisMod.CurrentModData.ModID);
                    Dictionary<Version, IndexVersionData> allVerData = ModManager.GlobalIndexFile[thisMod.CurrentModData.ModID].AllVersions;
                    if (!allVerData.ContainsKey(newVersion)) throw new IndexFileException("Failed to find the requested version in the mod's index file: " + thisMod.CurrentModData.ModID + " v" + newVersion);
                    desiredVersionData = allVerData[newVersion];
                }

                using (var wb = new WebClient())
                {
                    wb.Headers[HttpRequestHeader.UserAgent] = AMLUtils.UserAgent;

                    string kosherFileName = AMLUtils.SanitizeFilename(desiredVersionData.Filename);

                    string tempDownloadFolder = Path.Combine(Path.GetTempPath(), "AstroModLoader", "Downloads");
                    Directory.CreateDirectory(tempDownloadFolder);
                    wb.DownloadFile(desiredVersionData.URL, Path.Combine(tempDownloadFolder, kosherFileName));

                    var installedMods = InstallModFromPath(Path.Combine(tempDownloadFolder, kosherFileName), out _, out int numMalformatted, out _, out _);
                    if (enableByDefault)
                    {
                        foreach (var mod in installedMods)
                        {
                            mod.Enabled = true;
                            mod.Dirty = true;
                        }
                    }
                    if (numMalformatted > 0) throw new FormatException(numMalformatted + " mods were malformatted");

                    ModManager.SortVersions();
                    ModManager.SortMods();
                    Directory.Delete(tempDownloadFolder, true);
                }
            }
            catch (Exception ex)
            {
                if (ex is WebException || ex is IOException || ex is IndexFileException)
                {
                    Debug.WriteLine(ex.ToString());
                    return false;
                }
                throw;
            }
            return true;
        }

        public void SwitchVersionSync(Mod thisMod, Version newVersion)
        {
            if (!thisMod.AllModData.ContainsKey(newVersion))
            {
                thisMod.CannotCurrentlyUpdate = true;
                bool outcome = DownloadVersionSync(thisMod, newVersion);
                thisMod.CannotCurrentlyUpdate = false;

                if (!outcome) return;
            }

            thisMod.InstalledVersion = newVersion;
            if (!thisMod.AllModData.ContainsKey(newVersion))
            {
                if (!thisMod.ForceLatest) return;
                thisMod.InstalledVersion = thisMod.AvailableVersions[0];
            }
            thisMod.Dirty = true;
        }

        private Semaphore updateCellsSemaphore = new Semaphore(1, 1);
        private async Task ForceUpdateCells()
        {
            await Task.Run(() =>
            {
                if (ModManager.IsReadOnly) return;

                if (!updateCellsSemaphore.WaitOne(5000)) return;

                List<Mod> mods = new List<Mod>();
                List<string> strVals = new List<string>();
                AMLUtils.InvokeUI(() =>
                {
                    foreach (DataGridViewRow row in this.dataGridView1.Rows)
                    {
                        if (row.Tag is Mod taggedMod)
                        {
                            if (taggedMod.CannotCurrentlyUpdate) continue;
                            taggedMod.Enabled = (bool)row.Cells[0].Value;
                            if (TableHandler.ShouldContainOptionalColumn()) taggedMod.IsOptional = (bool)row.Cells[5].Value;
                            mods.Add(taggedMod);
                            strVals.Add(row.Cells[2].Value as string);
                        }
                    }
                });

                for (int i = 0; i < mods.Count; i++)
                {
                    Mod taggedMod = mods[i];
                    if (strVals[i] != null)
                    {
                        string strVal = strVals[i];
                        Version changingVer = null;
                        if (strVal.Contains("Latest"))
                        {
                            taggedMod.ForceLatest = true;
                            changingVer = taggedMod.AvailableVersions[0];
                        }
                        else
                        {
                            taggedMod.ForceLatest = false;
                            changingVer = new Version(strVal);
                        }

                        SwitchVersionSync(taggedMod, changingVer);
                    }
                }

                ModManager.FullUpdate();
                updateCellsSemaphore.Release();
            }).ContinueWith(res =>
            {
                AMLUtils.InvokeUI(TableManager.Refresh);
            });
        }

        private volatile bool IsAllDirty = false;
        private void DataGridView1_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            IsAllDirty = true;
        }

        private async void CheckAllDirty_Tick(object sender, EventArgs e)
        {
            if (IsAllDirty)
            {
                IsAllDirty = false;
                await ForceUpdateCells();
            }
        }

        private void PeriodicCheckTimer_Tick(object sender, EventArgs e)
        {
            ModManager.UpdateReadOnlyStatus();
        }

        // Normally this wouldn't be necessary because of the fact that all index files are refreshed when the loader boots up, but for the folks that leave the mod loader open for days on end, we should refresh the global index file every once in a while
        private void ForceAutoUpdateRefresh_Tick(object sender, EventArgs e)
        {
            ModManager.ResetGlobalIndexFile();
            if (!autoUpdater.IsBusy) autoUpdater.RunWorkerAsync();
        }

        public void AdjustModInfoText(string txt, string linkText = "")
        {
            AMLUtils.InvokeUI(() =>
            {
                if (txt == "")
                {
                    AdjustModInfoText("Drop a .pak or .zip file onto this window to install a mod.\n\nOnce you're ready to use your enabled mods, press the \"Play\" button below to apply your mods and start playing.");
                    return;
                }

                string newTextFull = txt + linkText;
                var newLinkArea = new LinkArea(txt.Length, linkText.Length);
                if (this.modInfo.Text == newTextFull && this.modInfo.LinkArea.Start == newLinkArea.Start && this.modInfo.LinkArea.Length == newLinkArea.Length) return; // Partial fix for winforms rendering issue

                this.modInfo.Text = newTextFull;
                this.modInfo.LinkArea = newLinkArea;
            });
        }

        public void UpdateVersionLabel()
        {
            AMLUtils.InvokeUI(() =>
            {
                if (ModManager == null) return;
                headerLabel.Text = "Mods (" + (ModManager.InstalledAstroBuild?.ToString() ?? "Unknown") + (ModManager.MismatchedSteamworksDLL ? " Pirated?" : "") + "):";
                headerLabel.ForeColor = ModManager.MismatchedSteamworksDLL ? AMLPalette.WarningColor : AMLPalette.ForeColor;
            });
        }

        public void SwitchPlatform(PlatformType newPlatform)
        {
            AMLUtils.InvokeUI(() =>
            {
                if (!ModManager.ValidPlatformTypesToPaths.ContainsKey(newPlatform)) return;
                ModManager.GamePath = null;
                ModManager.Platform = newPlatform;
                ModManager.DeterminePaths();
                ModManager.GamePath = ModManager.ValidPlatformTypesToPaths[newPlatform];
                ModManager.VerifyGamePath();
                ModManager.ApplyGamePathDerivatives();
                ModManager.VerifyIntegrity();
                ModManager.SyncIndependentConfigToDisk();
                ModManager.SyncModsFromDisk();
                ModManager.SyncDependentConfigFromDisk(false);
                ModManager.SyncDependentConfigToDisk();

                FullRefresh();
                UpdateVersionLabel();
            });
        }

        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
        }

        private string[] AllowedModExtensions = new string[]
        {
            ".pak",
            ".zip"
        };

        /*private string AdjustNewPathToBeValid(string newPath, Metadata originalModData)
        {
            Mod testMod = new Mod(originalModData, Path.GetFileName(newPath));
            if (testMod.Priority >= 800) return null;
            return Path.Combine(Path.GetDirectoryName(newPath), testMod.ConstructName());
        }*/

        private bool NewPathIsValid(string newPath, Metadata originalModData)
        {
            if (string.IsNullOrEmpty(newPath) || originalModData == null || string.IsNullOrEmpty(originalModData.Name) || string.IsNullOrEmpty(originalModData.ModID) || originalModData.ModVersion == null) return false;

            string normalFilePath = Path.GetFileName(newPath);
            if (normalFilePath.Length < 6) return false;

            Mod testMod = new Mod(originalModData, normalFilePath);
            if (testMod.Priority >= 800) return false;

            string desiredName = testMod.ConstructName(0);
            string realName = "000-" + normalFilePath.Substring(4);
            return desiredName.Equals(realName);
        }

        private string AddModFromPakPath(string newInstallingMod, out bool wasMalformatted)
        {
            wasMalformatted = false;
            try
            {
                string newPath = Path.Combine(ModManager.DownloadPath, Path.GetFileName(newInstallingMod));

                if (!NewPathIsValid(newPath, ModManager.ExtractMetadataFromPath(newInstallingMod)))
                {
                    wasMalformatted = true;
                    return null;
                }

                if (!string.IsNullOrEmpty(newPath))
                {
                    File.Copy(newInstallingMod, newPath, true);
                    return newPath;
                }
            }
            catch (IOException)
            {
                return null;
            }

            return null;
        }

        private bool PullDependency(string modID, Dependency dependency)
        {
            IndexFile idxFile = Mod.GetIndexFileFromDownloadInfo(dependency.Download, modID, null);
            if (idxFile == null) return false;
            if (!idxFile.Mods.ContainsKey(modID)) return false;
            IndexMod idxMod = idxFile.Mods[modID];

            // we use NPM syntax altered so that e.g. "1.0.0" = "^1.0.0", hopefully this approximates Cargo semver syntax well enough?
            string alteredVersion = dependency.Version;
            if ("0123456789".Contains(alteredVersion[0]))
            {
                alteredVersion = "^" + alteredVersion;
            }

            List<SemVersion> allVersions = idxMod.AllVersions.Keys.Select(e => SemVersion.Parse(e.ToString())).OrderBy(e => e, SemVersion.PrecedenceComparer).Reverse().ToList();
            var satisfiesRange = SemVersionRange.ParseNpm(alteredVersion);

            SemVersion desiredVersion = null;
            foreach (SemVersion version in allVersions)
            {
                if (satisfiesRange.Contains(version))
                {
                    desiredVersion = version;
                    break;
                }
            }

            if (desiredVersion == null) return false;

            // got desired version, now download
            Version desiredVersionRaw = desiredVersion.ToVersion();
            if (!idxMod.AllVersions.ContainsKey(desiredVersionRaw)) return false;
            IndexVersionData desiredVersionData = idxMod.AllVersions[desiredVersionRaw];
            if (desiredVersionData == null) return false;

            bool success = DownloadVersionSync(null, desiredVersionRaw, desiredVersionData, true);
            //if (success) ModManager.FullUpdate();
            return success;
        }

        private List<Mod> InstallModFromPath(string newInstallingMod, out int numClientOnly, out int numMalformatted, out int numNewProfiles, out List<string> newDeps)
        {
            numClientOnly = 0;
            numMalformatted = 0;
            numNewProfiles = 0;
            newDeps = null;
            string ext = Path.GetExtension(newInstallingMod);
            if (!AllowedModExtensions.Contains(ext)) return null;

            List<string> newPaths = new List<string>();

            if (ext == ".zip") // If the mod we are trying to install is a zip, we go through and copy each pak file inside that zip
            {
                string targetFolderPath = Path.Combine(Path.GetTempPath(), "AstroModLoader", Path.GetFileNameWithoutExtension(newInstallingMod)); // Extract the zip file to the temporary data folder
                ZipFile.ExtractToDirectory(newInstallingMod, targetFolderPath);

                string[] allAccessiblePaks = Directory.GetFiles(targetFolderPath, "*.pak", SearchOption.AllDirectories); // Get all pak files that exist in the zip file
                foreach (string zippedPakPath in allAccessiblePaks)
                {
                    string newPath = AddModFromPakPath(zippedPakPath, out bool wasMalformatted);
                    if (wasMalformatted) numMalformatted++;
                    if (newPath != null) newPaths.Add(newPath);
                }

                // Any .json files included will be treated as mod profiles to add to our current list
                string[] allAccessibleJsonFiles = Directory.GetFiles(targetFolderPath, "*.json", SearchOption.AllDirectories);
                foreach (string jsonFilePath in allAccessibleJsonFiles)
                {
                    ModProfile parsingProfile = null;
                    try
                    {
                        parsingProfile = JsonConvert.DeserializeObject<ModProfile>(File.ReadAllText(jsonFilePath));
                    }
                    catch
                    {
                        continue;
                    }

                    if (parsingProfile?.ProfileData == null) continue;

                    parsingProfile.Name = string.IsNullOrWhiteSpace(parsingProfile.Name) ? "Unknown" : parsingProfile.Name;
                    while (ModManager.ProfileList.ContainsKey(parsingProfile.Name)) parsingProfile.Name = parsingProfile.Name + "*";

                    List<KeyValuePair<string, Mod>> plannedOrdering = new List<KeyValuePair<string, Mod>>();
                    foreach (KeyValuePair<string, Mod> entry in parsingProfile.ProfileData)
                    {
                        plannedOrdering.Add(entry);
                    }
                    plannedOrdering = new List<KeyValuePair<string, Mod>>(plannedOrdering.OrderBy(o => o.Value.Priority).ToList());

                    ModProfile currentProf = ModManager.GenerateProfile();
                    List<KeyValuePair<string, Mod>> plannedOrderingCurrent = new List<KeyValuePair<string, Mod>>();
                    string[] parsingProfileAllIDs = parsingProfile.ProfileData.Keys.ToArray();
                    foreach (KeyValuePair<string, Mod> entry in currentProf.ProfileData)
                    {
                        if (parsingProfileAllIDs.Contains(entry.Key)) continue;
                        entry.Value.Enabled = false;
                        plannedOrderingCurrent.Add(entry);
                    }

                    plannedOrdering.AddRange(plannedOrderingCurrent.OrderBy(o => o.Value.Priority));

                    ModProfile creatingProfile = new ModProfile();
                    creatingProfile.ProfileData = new Dictionary<string, Mod>();
                    for (int i = 0; i < plannedOrdering.Count; i++)
                    {
                        plannedOrdering[i].Value.Priority = i + 1;
                        creatingProfile.ProfileData[plannedOrdering[i].Key] = plannedOrdering[i].Value;
                    }

                    if (ModManager.ProfileList == null) ModManager.ProfileList = new Dictionary<string, ModProfile>();
                    ModManager.ProfileList[parsingProfile.Name] = creatingProfile;
                    numNewProfiles++;
                }

                Directory.Delete(targetFolderPath, true); // Clean up the temporary data folder
            }
            else // Otherwise just copy the file itself
            {
                string newPath = AddModFromPakPath(newInstallingMod, out bool wasMalformatted);
                if (wasMalformatted) numMalformatted++;
                if (newPath != null) newPaths.Add(newPath);
            }

            List<Mod> outputs = new List<Mod>();
            newDeps = new List<string>();

            foreach (string newPath in newPaths)
            {
                try
                {
                    if (!string.IsNullOrEmpty(newPath))
                    {
                        Mod nextMod = ModManager.SyncSingleModFromDisk(newPath, out bool wasClientOnly, false);
                        if (nextMod != null)
                        {
                            outputs.Add(nextMod);
                            // check for dependencies
                            if (nextMod.CurrentModData.Dependencies != null && nextMod.CurrentModData.Dependencies.Count > 0)
                            {
                                var parsedDeps = nextMod.CurrentModData.ParseDependencies();
                                foreach (KeyValuePair<string, Dependency> entry in parsedDeps)
                                {
                                    bool pulledSuccessfully = PullDependency(entry.Key, entry.Value);
                                    if (pulledSuccessfully)
                                    {
                                        newDeps.Add(entry.Key);
                                    }
                                }
                            }
                        }
                        if (wasClientOnly)
                        {
                            numClientOnly++;
                            File.Delete(newPath);
                        }
                    }
                }
                catch (IOException) { }
            }
            
            return outputs;
        }

        private async void Form1_DragDrop(object sender, DragEventArgs e)
        {
            string[] installingModPaths = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (installingModPaths.Length > 0)
            {
                Dictionary<string, List<Version>> newMods = new Dictionary<string, List<Version>>();
                int clientOnlyCount = 0;
                int malformattedCount = 0;
                int newProfileCount = 0;
                int invalidExtensionCount = 0;
                int wasFolderCount = 0;
                List<string> newDeps = new List<string>();
                foreach (string newInstallingMod in installingModPaths)
                {
                    if (!File.Exists(newInstallingMod))
                    {
                        wasFolderCount++;
                        continue;
                    }
                    if (!AllowedModExtensions.Contains(Path.GetExtension(newInstallingMod)))
                    {
                        invalidExtensionCount++;
                        continue;
                    }

                    List<Mod> resMods = InstallModFromPath(newInstallingMod, out int thisClientOnlyCount, out int thisNumMalformatted, out int thisNumNewProfiles, out List<string> thisNewDeps);
                    if (resMods == null) continue;
                    foreach (Mod resMod in resMods)
                    {
                        if (resMod == null) continue;
                        if (!newMods.ContainsKey(resMod.CurrentModData.ModID)) newMods[resMod.CurrentModData.ModID] = new List<Version>();
                        newMods[resMod.CurrentModData.ModID].AddRange(resMod.AvailableVersions);
                    }
                    clientOnlyCount += thisClientOnlyCount;
                    malformattedCount += thisNumMalformatted;
                    newProfileCount += thisNumNewProfiles;
                    if (thisNewDeps != null) newDeps.AddRange(thisNewDeps);
                }

                //ModManager.SyncModsFromDisk(true);
                ModManager.SortMods();
                ModManager.SortVersions();
                ModManager.RefreshAllPriorites();
                if (!autoUpdater.IsBusy) autoUpdater.RunWorkerAsync();

                foreach (Mod mod in ModManager.Mods)
                {
                    if (mod == null) continue;
                    mod.Dirty = true;

                    if (newMods.ContainsKey(mod.CurrentModData.ModID))
                    {
                        // We switch the installed version to the newest version that has just been added
                        newMods[mod.CurrentModData.ModID].Sort();
                        newMods[mod.CurrentModData.ModID].Reverse();
                        mod.InstalledVersion = newMods[mod.CurrentModData.ModID][0];

                        // If this is a new mod, we enable it or disable it automatically, but if it's not new then we respect the user's pre-existing setting 
                        if (mod.AvailableVersions.Count == 1) mod.Enabled = ModManager.InstalledAstroBuild.AcceptablySimilar(mod.CurrentModData.GameBuild) && (!Program.CommandLineOptions.ServerMode || mod.CurrentModData.Sync != SyncMode.ClientOnly);
                    }
                }

                await ModManager.FullUpdate();

                AMLUtils.InvokeUI(() =>
                {
                    TableManager.Refresh();
                    if (wasFolderCount > 0)
                    {
                        this.ShowBasicButton("You cannot drag in a folder!", "OK", null, null);
                    }

                    if (invalidExtensionCount > 0)
                    {
                        this.ShowBasicButton(invalidExtensionCount + " file" + (invalidExtensionCount == 1 ? " had an invalid extension" : "s had invalid extensions") + " and " + (invalidExtensionCount == 1 ? "was" : "were") + " ignored.\nAcceptable mod extensions are: " + string.Join(", ", AllowedModExtensions), "OK", null, null);
                    }

                    if (clientOnlyCount > 0)
                    {
                        this.ShowBasicButton(clientOnlyCount + " mod" + (clientOnlyCount == 1 ? " is" : "s are") + " designated as \"Client only\" and " + (clientOnlyCount == 1 ? "was" : "were") + " ignored.", "OK", null, null);
                    }

                    if (malformattedCount > 0)
                    {
                        this.ShowBasicButton(malformattedCount + " mod" + (malformattedCount == 1 ? " was" : "s were") + " malformatted, and could not be installed.\nThe file name may be invalid, the metadata may be invalid, or both.\nPlease ensure that this mod meets the community-made standards.", "OK", null, null);
                    }

                    if (newProfileCount > 0)
                    {
                        this.ShowBasicButton(newProfileCount + " new profile" + (newProfileCount == 1 ? " was" : "s were") + " included with the file" + (installingModPaths.Length == 1 ? "" : "s") + " you installed.\n" + (newProfileCount == 1 ? "It has" : "They have") + " been added to your list of profiles.", "OK", null, null);
                    }

                    if (newDeps.Count > 0)
                    {
                        this.ShowBasicButton(newDeps.Count + " new " + (newDeps.Count == 1 ? "dependency was" : "dependencies were") + " installed alongside the mod" + (installingModPaths.Length == 1 ? "" : "s") + " you installed.\n" + (newDeps.Count == 1 ? "It has" : "They have") + " been enabled by default.\n\n" + (string.Join(", ", newDeps)), "OK", null, null);
                    }
                });
            }
        }

        private void DataGridView1_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            AMLUtils.InvokeUI(() =>
            {
                if (e.RowIndex == -1) return;
                if (AMLUtils.IsLinux) return;

                Type t = dataGridView1?.GetType()?.BaseType;
                FieldInfo viewSetter = t?.GetField("latestEditingControl", BindingFlags.Default | BindingFlags.NonPublic | BindingFlags.Instance);
                viewSetter?.SetValue(dataGridView1, null);
            });
        }

        private void DataGridView1_DataError(object sender, DataGridViewDataErrorEventArgs anError)
        {
            AMLUtils.InvokeUI(() => MessageBox.Show("DataError happened! Please report this! " + anError.Context.ToString()));
        }

        private void Footer_Paint(object sender, PaintEventArgs e)
        {
            using (Pen p = new Pen(AMLPalette.FooterLineColor, 1))
            {
                e.Graphics.DrawLine(p, new Point(0, 0), new Point(footerPanel.ClientSize.Width, 0));
            }
        }

        private void DataGridView1_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            AMLUtils.InvokeUI(() =>
            {
                foreach (DataGridViewColumn column in dataGridView1.Columns)
                {
                    column.SortMode = DataGridViewColumnSortMode.NotSortable;
                    column.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                    if (column is DataGridViewCheckBoxColumn)
                    {
                        column.ReadOnly = false;
                        column.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCellsExceptHeader;
                    }
                }

                ForceResize();
                ModManager.RefreshAllPriorites();
            });
        }

        private void DataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            AMLUtils.InvokeUI(() =>
            {
                dataGridView1.EndEdit();
                dataGridView1.CommitEdit(DataGridViewDataErrorContexts.Commit);
            });
        }

        private Mod previouslySelectedMod;
        private bool canAdjustOrder = true;
        private void DataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            AMLUtils.InvokeUI(() =>
            {
                Mod selectedMod = TableManager.GetCurrentlySelectedMod();
                if (dataGridView1.SelectedRows.Count == 1 && !ModManager.IsReadOnly)
                {
                    DataGridViewRow selectedRow = dataGridView1.SelectedRows[0];
                    int newModIndex = selectedRow.Index;

                    // If shift is held, that means we are changing the order
                    if (canAdjustOrder && ModifierKeys == Keys.Shift && selectedMod != null && previouslySelectedMod != null && previouslySelectedMod != selectedMod)
                    {
                        ModManager.SwapMod(previouslySelectedMod, newModIndex, false);
                        previouslySelectedMod = null;
                        canAdjustOrder = false;
                        TableManager.Refresh();
                        canAdjustOrder = true;

                        dataGridView1.ClearSelection();
                        dataGridView1.Rows[newModIndex].Selected = true;
                        dataGridView1.CurrentCell = dataGridView1.Rows[newModIndex].Cells[0];
                        selectedMod = ModManager.Mods[newModIndex];

                        foreach (Mod mod in ModManager.Mods) mod.Dirty = true; // Update all the priorities on disk to be safe
                        ModManager.FullUpdate();
                    }
                }

                previouslySelectedMod = selectedMod;

                RefreshModInfoLabel();
            });
        }

        private void dataGridView1_KeyDown(object sender, KeyEventArgs e)
        {
            AMLUtils.InvokeUI(() =>
            {
                Mod selectedMod = TableManager.GetCurrentlySelectedMod();

                switch (e.KeyCode)
                {
                    case Keys.Delete:
                        if (selectedMod != null && !ModManager.IsReadOnly)
                        {
                            if (e.Alt)
                            {
                                selectedMod.AvailableVersions.Sort();
                                selectedMod.AvailableVersions.Reverse();

                                List<Version> badVersions = new List<Version>();
                                foreach (Version testingVersion in selectedMod.AvailableVersions)
                                {
                                    if (testingVersion != selectedMod.AvailableVersions[0] && selectedMod.AllModData.ContainsKey(testingVersion)) badVersions.Add(testingVersion);
                                }
                                if (badVersions.Count == 0)
                                {
                                    this.ShowBasicButton("You have no old versions of this mod on disk to clean!", "OK", null, null);
                                }
                                else
                                {
                                    int dialogRes = this.ShowBasicButton("Are you sure you want to clean \"" + selectedMod.CurrentModData.Name + "\"?\nThese versions will be deleted from disk: " + string.Join(", ", badVersions.Select(v => v.ToString()).ToArray()), "Yes", "No", null);
                                    if (dialogRes == 0)
                                    {
                                        ModManager.EviscerateMod(selectedMod, badVersions);
                                        FullRefresh();
                                        selectedMod.InstalledVersion = selectedMod.AvailableVersions[0];
                                        this.ShowBasicButton("Successfully cleaned \"" + selectedMod.CurrentModData.Name + "\".", "OK", null, null);
                                    }
                                }
                            }
                            else
                            {
                                int dialogRes = this.ShowBasicButton("Are you sure you want to completely delete \"" + selectedMod.CurrentModData.Name + "\"?", "Yes", "No", null);
                                if (dialogRes == 0)
                                {
                                    ModManager.EviscerateMod(selectedMod);
                                    FullRefresh();
                                }
                            }
                        }
                        break;
                    case Keys.Escape:
                        dataGridView1.ClearSelection();
                        break;
                }
            });
        }

        private void RefreshModInfoLabel()
        {
            AMLUtils.InvokeUI(() =>
            {
                Mod selectedMod = TableManager?.GetCurrentlySelectedMod();
                if (selectedMod == null)
                {
                    AdjustModInfoText("");
                    return;
                }

                string kosherDescription = selectedMod.CurrentModData.Description;
                if (!string.IsNullOrEmpty(kosherDescription) && kosherDescription.Length > 200) kosherDescription = kosherDescription.Substring(0, 200) + "...";

                string kosherSync = "N/A";
                switch (selectedMod.CurrentModData.Sync)
                {
                    case SyncMode.None:
                        kosherSync = "None";
                        break;
                    case SyncMode.ClientOnly:
                        kosherSync = "Client only";
                        break;
                    case SyncMode.ServerOnly:
                        kosherSync = "Server only";
                        break;
                    case SyncMode.ServerAndClient:
                        kosherSync = "Server and client";
                        break;
                }

                long knownSize = -1;
                try
                {
                    knownSize = ModManager.GetSizeOnDisk(selectedMod);
                }
                catch (Exception ex)
                {
                    if (!(ex is IOException) && !(ex is FileNotFoundException)) throw;
                }

                string additionalData = "";
                if (knownSize >= 0) additionalData += "\nSize: " + AMLUtils.FormatFileSize(knownSize);

                bool hasHomepage = !string.IsNullOrEmpty(selectedMod.CurrentModData.Homepage) && AMLUtils.IsValidUri(selectedMod.CurrentModData.Homepage);

                string realText = "Name: " + selectedMod.CurrentModData.Name;
                if (!string.IsNullOrEmpty(kosherDescription)) realText += "\nDescription: " + kosherDescription;
                realText += "\nSync: " + kosherSync;
                realText += additionalData;
                realText += hasHomepage ? "\nWebsite: " : "";

                AdjustModInfoText(realText, hasHomepage ? selectedMod.CurrentModData.Homepage : "");
            });
        }

        private void modInfo_LinkClicked(object sender, EventArgs e)
        {
            Mod selectedMod = TableManager.GetCurrentlySelectedMod();
            if (selectedMod != null && !string.IsNullOrEmpty(selectedMod.CurrentModData.Homepage) && AMLUtils.IsValidUri(selectedMod.CurrentModData.Homepage)) AMLUtils.OpenURL(selectedMod.CurrentModData.Homepage);
        }

        public void ForceResize()
        {
            footerPanel.Width = this.Width;
            footerPanel.Location = new Point(0, this.ClientSize.Height - footerPanel.Height);
            modPanel.Width = this.ClientSize.Width - 15;
            dataGridView1.Width = modPanel.Width - dataGridView1.Location.X;

            int buttonHeight = footerPanel.Height / 2;
            this.SetHeightOfAllButtonsInControl(buttonHeight);
            syncButton.Width = (int)(buttonHeight * 4.5);
            exitButton.Width = buttonHeight * 3;

            modPanel.Height = (int)(this.ClientSize.Height - this.MinimumSize.Height * 0.45f);
            dataGridView1.Height = modPanel.Height - dataGridView1.Location.Y - (int)(buttonHeight * 1.5);
            modInfo.Location = new Point(modInfo.Location.X, modPanel.Location.Y + modPanel.Height + 15);
            modInfo.MaximumSize = new Size(this.ClientSize.Width - (modInfo.Location.X * 2), modInfo.MaximumSize.Height);

            refresh.Location = new Point(dataGridView1.Location.X, dataGridView1.Location.Y + dataGridView1.Height + 10);
            loadButton.Location = new Point(refresh.Location.X + refresh.Width + 5, dataGridView1.Location.Y + dataGridView1.Height + 10);
            syncButton.Location = new Point(modPanel.Width - syncButton.Width, dataGridView1.Location.Y + dataGridView1.Height + 10);
            exitButton.Location = new Point(syncButton.Location.X + syncButton.Width - exitButton.Width, (footerPanel.Height - exitButton.Height) / 2);

            integratingLabel.Location = new Point(exitButton.Location.X - 6 - integratingLabel.Width, exitButton.Location.Y);
            integratingLabel.Height = exitButton.Height;

            dataGridView1.Invalidate();
        }

        public void ForceTableToFit()
        {
            if (dataGridView1.PreferredSize.Width > dataGridView1.Size.Width) this.Size = new Size((int)((this.Width + (dataGridView1.PreferredSize.Width - dataGridView1.Size.Width)) * 1.1f), this.Height);
        }

        public void FullRefresh()
        {
            AMLUtils.InvokeUI(() =>
            {
                if (ModManager != null)
                {
                    Directory.CreateDirectory(ModManager.DownloadPath);
                    Directory.CreateDirectory(ModManager.InstallPath);

                    ModManager.SyncModsFromDisk();
                    ModManager.SyncConfigFromDisk();
                    ModManager.UpdateReadOnlyStatus();
                    ModManager.SortMods();
                    if (!autoUpdater.IsBusy) autoUpdater.RunWorkerAsync();
                }

                if (TableManager != null) TableManager.Refresh();
                AMLPalette.RefreshTheme(this);
                RefreshModInfoLabel();
            });
        }

        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            AMLUtils.InvokeUI(ForceResize);
        }

        private void refresh_Click(object sender, EventArgs e)
        {
            FullRefresh();
        }

        public static readonly string GitHubRepo = "atenfyr/AstroModLoader-Classic";
        private Version latestOnlineVersion = null;
        private void Form1_Load(object sender, EventArgs e)
        {
            AMLUtils.InvokeUI(dataGridView1.ClearSelection);

            if (!string.IsNullOrEmpty(Program.CommandLineOptions.NextLaunchPath))
            {
                ModManager.LaunchCommand = Program.CommandLineOptions.NextLaunchPath;
                Program.CommandLineOptions.NextLaunchPath = null;
                ModManager.SyncConfigToDisk();
            }

            // Fetch the latest version from github
            Task.Run(() =>
            {
                latestOnlineVersion = GitHubAPI.GetLatestVersionFromGitHub(GitHubRepo);
            }).ContinueWith(res =>
            {
                if (latestOnlineVersion != null && latestOnlineVersion.IsAMLVersionLower())
                {
                    BasicButtonPopup resultButton = this.GetBasicButton("A new version of AstroModLoader (v" + latestOnlineVersion + ") is available!", "OK", "Open in browser", null);
                    resultButton.PageToVisit = GitHubAPI.GetLatestVersionURL(GitHubRepo);
                    resultButton.ShowDialog();
                }
            }, TaskScheduler.FromCurrentSynchronizationContext());

            // Initial resize of the menu to fit the table if necessary
            AMLUtils.InvokeUI(ForceTableToFit);

            AMLUtils.InvokeUI(ForceResize);
            AMLUtils.InvokeUI(ForceResize);

            UpdateVersionLabel();
            RefreshModInfoLabel();

            Task.Run(() =>
            {
                if (!string.IsNullOrEmpty(Program.CommandLineOptions.InstallMod))
                {
                    InstallModFromPath(Program.CommandLineOptions.InstallMod, out _, out _, out _, out _);
                    FullRefresh();
                }
            });

            HandleThunderstoreCommandLineParameters(Program.CommandLineOptions.InstallThunderstore);
        }

        internal void ReceivePipe(string txt)
        {
            string[] parts = txt.Split(":");
            string cmd = parts[0];
            string data = string.Join(":", parts.Skip(1));
            
            switch(cmd)
            {
                case "InstallThunderstore":
                    AMLUtils.InvokeUI(() =>
                    {
                        this.Activate();
                        this.WindowState = FormWindowState.Normal;
                    });
                    HandleThunderstoreCommandLineParameters(data);
                    break;
                case "Focus":
                    AMLUtils.InvokeUI(() =>
                    {
                        this.Activate();
                        this.WindowState = FormWindowState.Normal;
                    });
                    break;
            }
        }

        private void HandleThunderstoreCommandLineParameters(string installParams)
        {
            Task.Run(() =>
            {
                if (!string.IsNullOrEmpty(installParams))
                {
                    string tstorePrefix = "ror2mm://v1/install/thunderstore.io/";
                    if (installParams.StartsWith(tstorePrefix))
                    {
                        var thing_split = installParams.TrimEnd().TrimEnd('/').Split("/");
                        string mod_id = thing_split[thing_split.Length - 2];

                        BasicButtonPopup basicButtonPrompt = null;
                        AMLUtils.InvokeUI(() =>
                        {
                            basicButtonPrompt = AMLUtils.GetBasicButton(this, "Installing " + mod_id + " from Thunderstore...", null, null, null);
                            basicButtonPrompt.ControlBox = false;
                            basicButtonPrompt.Show();
                        });

                        bool succeeded = true;
                        List<string> newDeps = null;
                        string tempDownloadFolder = Path.Combine(Path.GetTempPath(), "AstroModLoader", "ThunderstoreDownload");
                        try
                        {
                            try
                            {
                                Directory.CreateDirectory(tempDownloadFolder);
                                string zipPath = Path.Combine(tempDownloadFolder, "mod.zip");
                                using (var wb = new WebClient())
                                {
                                    wb.Headers[HttpRequestHeader.UserAgent] = AMLUtils.UserAgent;
                                    wb.DownloadFile(new Uri("https://thunderstore.io/package/download/" + installParams.Substring(tstorePrefix.Length)), zipPath);
                                }
                                List<Mod> installedMods = InstallModFromPath(zipPath, out _, out _, out _, out newDeps);
                                foreach (var mod in installedMods)
                                {
                                    mod.Enabled = true;
                                    mod.Dirty = true;
                                }
                                ModManager.FullUpdate();
                            }
                            catch
                            {
                                AMLUtils.InvokeUI(() =>
                                {
                                    basicButtonPrompt.Close();
                                    this.ShowBasicButton("Failed to install " + mod_id + " from Thunderstore!", "OK", null, null);
                                });
                                succeeded = false;
                            }
                        }
                        finally
                        {
                            Directory.Delete(tempDownloadFolder, true);
                            if (succeeded)
                            {
                                AMLUtils.InvokeUI(() =>
                                {
                                    basicButtonPrompt.Close();
                                    this.ShowBasicButton("Successfully installed " + mod_id + " from Thunderstore." + ((newDeps != null && newDeps.Count > 0) ? ("\n\n" + newDeps.Count + " new " + (newDeps.Count == 1 ? "dependency was" : " dependencies were") + " added as well:\n" + string.Join(", ", newDeps)) : ""), "OK", null, null);
                                });
                            }
                        }
                    }
                }
            }).ContinueWith(res =>
            {
                FullRefresh();
            });
        }

        private async void playButton_Click(object sender, EventArgs e)
        {
            // check, do we need UE4SS?
            bool needUE4SS = false;
            List<string> modsNeedUE4SS = new List<string>();
            foreach (var mod in ModManager.Mods)
            {
                if (mod.Enabled && mod.CurrentModData.EnableUE4SS)
                {
                    needUE4SS = true;
                    modsNeedUE4SS.Add(mod.CurrentModData.Name);
                }
            }

            if (!ModManager.UE4SSInstalled && needUE4SS)
            {
                int dialogRes = -1;
                AMLUtils.InvokeUI(() => dialogRes = this.ShowBasicButton("The following mods use UE4SS:\n\n" + string.Join(", ", modsNeedUE4SS) + "\n\nYou do not currently have UE4SS installed.\nThese mods may not operate as expected.\n\nWould you like to automatically install UE4SS now?", "Install", "Play anyways", "Cancel"));
                switch(dialogRes)
                {
                    case 0:
                        // install
                        if (!UE4SSManager.Install(ModManager.GetBinaryDir(), ModManager.InstallPathLua, this)) return;
                        AMLUtils.InvokeUI(() => this.ShowBasicButton("Successfully installed UE4SS.", "OK", null, null));
                        ModManager.CheckUE4SSInstalled();
                        break;
                    case 1:
                        // nothing
                        break;
                    case 2:
                        // cancel
                        return;
                }
            }

            ModManager.IsUpdatingAvailableVersionsFromIndexFilesWaitHandler.WaitOne(3000);

            await ModManager.FullUpdate();

            // ok, now check: do we meet all dependencies?
            // we only actually check presence of dependency, not version, for simplicity
            List<string> missingDependencies = new List<string>();
            HashSet<string> allModIDs = ModManager.Mods.Select(x => x.CurrentModData.ModID).ToHashSet();
            foreach (var mod in ModManager.Mods)
            {
                if (mod.Enabled && mod.CurrentModData.Dependencies != null && mod.CurrentModData.Dependencies.Count > 0)
                {
                    var parsedDeps = mod.CurrentModData.ParseDependencies();
                    foreach (KeyValuePair<string, Dependency> entry in parsedDeps)
                    {
                        if (!allModIDs.Contains(entry.Key))
                        {
                            missingDependencies.Add(entry.Key);
                            continue;
                        }
                    }
                }
            }

            if (missingDependencies.Count > 0)
            {
                int dialogRes = -1;
                AMLUtils.InvokeUI(() => dialogRes = this.ShowBasicButton("One or more of your mods require the following missing dependencies:\n\n" + string.Join(", ", missingDependencies) + "\n\nThese mods may not operate as expected without these dependencies.\nWould you like to continue anyways?", "Play", "Cancel", null));
                switch (dialogRes)
                {
                    case 0:
                        // nothing
                        break;
                    case -1:
                    case 1:
                    case 2:
                        // cancel
                        return;
                }
            }

            if (!Program.CommandLineOptions.ServerMode)
            {
                if (ModManager.Platform == PlatformType.Steam)
                {
                    AMLUtils.OpenURL(@"steam://run/361420");
                    return;
                }
                else if (ModManager.Platform == PlatformType.Win10)
                {
                    if (!string.IsNullOrEmpty(ModManager.MicrosoftRuntimeID)) AMLUtils.OpenURL(@"shell:appsFolder\" + ModManager.MicrosoftRuntimeID + "!AppSystemEraSoftworks29415440E1269Shipping");
                    return;
                }
            }

            if ((Program.CommandLineOptions.ServerMode || AMLUtils.IsLinux || string.IsNullOrEmpty(ModManager.BinaryFilePath)) && string.IsNullOrEmpty(ModManager.LaunchCommand))
            {
                TextPrompt initialPathPrompt = null;
                AMLUtils.InvokeUI(() =>
                {
                    initialPathPrompt = new TextPrompt
                    {
                        StartPosition = FormStartPosition.CenterScreen,
                        DisplayText = "Select a file to run:",
                        AllowBrowse = true,
                        BrowseMode = BrowseMode.File
                    };
                });

                if (initialPathPrompt?.ShowDialog(this) == DialogResult.OK)
                {
                    ModManager.LaunchCommand = initialPathPrompt.OutputText;
                    ModManager.SyncConfigToDisk();
                }
            }

            if (string.IsNullOrEmpty(ModManager.LaunchCommand) && !string.IsNullOrEmpty(ModManager.BinaryFilePath))
            {
                Process.Start(ModManager.BinaryFilePath, Program.CommandLineOptions.ServerMode ? "-log" : "");
            }
            else
            {
                if (string.IsNullOrEmpty(ModManager.LaunchCommand)) return;
                try
                {
                    Process.Start(new ProcessStartInfo()
                    {
                        WorkingDirectory = Path.GetDirectoryName(ModManager.LaunchCommand),
                        FileName = ModManager.LaunchCommand,
                        UseShellExecute = true
                    });
                }
                catch
                {
                    AMLUtils.InvokeUI(() => this.ShowBasicButton("Invalid path to file: \"" + ModManager.LaunchCommand + "\"", "OK", null, null));
                    ModManager.LaunchCommand = null;
                    ModManager.SyncConfigToDisk();
                }
            }
        }

        private void settingsButton_Click(object sender, EventArgs e)
        {
            SettingsForm settingsForm = new SettingsForm();
            settingsForm.StartPosition = FormStartPosition.Manual;
            settingsForm.Location = new Point((this.Location.X + this.Width / 2) - (settingsForm.Width / 2), (this.Location.Y + this.Height / 2) - (settingsForm.Height / 2));
            settingsForm.ShowDialog(this);
        }

        private void exitButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void loadButton_Click(object sender, EventArgs e)
        {
            if (ModManager.IsReadOnly)
            {
                this.ShowBasicButton("You cannot edit profiles while the game is open!", "OK", null, null);
                return;
            }

            ProfileSelector selectorForm = new ProfileSelector();
            selectorForm.StartPosition = FormStartPosition.Manual;
            selectorForm.Location = new Point((this.Location.X + this.Width / 2) - (selectorForm.Width / 2), (this.Location.Y + this.Height / 2) - (selectorForm.Height / 2));
            selectorForm.ShowDialog(this);
        }

        internal bool CurrentlySyncing = false;
        internal bool syncErrored;
        internal string syncErrorMessage;
        internal string[] syncFailedDownloadMods = null;
        internal string syncKosherProfileName;

        private void syncButton_Click(object sender, EventArgs e)
        {
            if (ModManager.IsReadOnly)
            {
                this.ShowBasicButton("You cannot sync mods while the game is open!", "OK", null, null);
                return;
            }

            if (CurrentlySyncing)
            {
                this.ShowBasicButton("Please wait, the mod loader is currently busy syncing.", "OK", null, null);
                return;
            }

            TextPrompt getIPPrompt = new TextPrompt();
            getIPPrompt.DisplayText = "Enter a server address to sync with:";
            getIPPrompt.Width -= 100;
            getIPPrompt.AllowBrowse = false;
            getIPPrompt.StartPosition = FormStartPosition.Manual;
            getIPPrompt.Location = new Point((this.Location.X + this.Width / 2) - (getIPPrompt.Width / 2), (this.Location.Y + this.Height / 2) - (getIPPrompt.Height / 2));

            if (getIPPrompt.ShowDialog(this) == DialogResult.OK)
            {
                ServerSyncPopup waiting = new ServerSyncPopup();
                waiting.StartPosition = FormStartPosition.Manual;
                waiting.Location = new Point((this.Location.X + this.Width / 2) - (waiting.Width / 2), (this.Location.Y + this.Height / 2) - (waiting.Height / 2));
                waiting.OurIP = getIPPrompt.OutputText.Trim();

                syncErrored = true;
                syncErrorMessage = "The syncing process halted prematurely!";
                syncFailedDownloadMods = null;
                syncKosherProfileName = "";

                waiting.ShowDialog(this);

                CurrentlySyncing = false;
                ModManager.SyncConfigToDisk();
                TableManager.Refresh();

                if (syncErrored)
                {
                    this.ShowBasicButton(syncErrorMessage, "OK", null, null);
                }
                else
                {
                    int syncFailedDownloadCount = syncFailedDownloadMods.Length;
                    if (syncFailedDownloadCount == 0)
                    {
                        this.ShowBasicButton("Added a new profile named \"" + syncKosherProfileName + "\".\nNo mods failed to sync.", "OK", null, null);
                    }
                    else
                    {
                        this.ShowBasicButton("Added a new profile named \"" + syncKosherProfileName + "\".\n\n" + syncFailedDownloadCount.ToString() + " mod" + (syncFailedDownloadCount == 1 ? "" : "s") + " failed to sync:\n" + string.Join("\n", syncFailedDownloadMods).Trim(), "OK", null, null);
                    }
                }
            }
        }

        private void dataGridView1_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (TableManager != null) AMLUtils.InvokeUI(() => TableManager.PaintCell(sender, e));
        }
    }
}
