﻿using AstroModIntegrator;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace AstroModLoader
{
    public class Mod : ICloneable
    {
        private bool _enabled;
        [JsonIgnore]
        public bool Dirty;
        [JsonProperty("enabled")]
        [DefaultValue(true)]
        [DisplayName(" ")]
        public bool Enabled
        {
            get
            {
                return _enabled;
            }
            set
            {
                _enabled = value;
                Dirty = true;
            }
        }

        [JsonProperty("optional")]
        [DefaultValue(false)]
        public bool IsOptional;

        public bool ShouldSerializeIsOptional()
        {
            return IsOptional || TableHandler.ShouldContainOptionalColumn();
        }

        [JsonProperty("force_latest")]
        [DefaultValue(false)]
        public bool ForceLatest;

        public bool ShouldSerializeForceLatest()
        {
            return ForceLatest;
        }

        [JsonConverter(typeof(Newtonsoft.Json.Converters.VersionConverter))]
        [DisplayName("Version")]
        [JsonProperty("version")]
        public Version InstalledVersion { get; set; }

        [JsonIgnore]
        public List<Version> AvailableVersions { get; set; }

        [JsonProperty("priority")]
        public int Priority = 0;

        [JsonIgnore]
        public Dictionary<Version, Metadata> AllModData;

        [JsonIgnore]
        public Metadata CurrentModData {
            get
            {
                if (!AllModData.ContainsKey(InstalledVersion)) throw new KeyNotFoundException("This mod does not have the following version: " + InstalledVersion);
                return AllModData[InstalledVersion];
            }
        }

        [JsonIgnore]
        public string NameOnDisk;

        [JsonIgnore]
        internal bool CannotCurrentlyUpdate = false;

        public Mod(Metadata modData, string nameOnDisk)
        {
            if (modData == null && nameOnDisk == null) return;
            if (AllModData == null) AllModData = new Dictionary<Version, Metadata>();
            NameOnDisk = nameOnDisk;

            PerformNameAnalysis();

            Priority = newPriority;
            InstalledVersion = newModVersion;

            if (modData != null)
            {
                if (modData.ModVersion != null) InstalledVersion = modData.ModVersion;
            }

            AllModData[InstalledVersion] = modData;
            if (modData == null)
            {
                AllModData[InstalledVersion] = JsonConvert.DeserializeObject<Metadata>("{}");
                AllModData[InstalledVersion].Sync = SyncMode.None;
            }

            if (!string.IsNullOrEmpty(newModID) && string.IsNullOrEmpty(CurrentModData.ModID))
            {
                CurrentModData.ModID = newModID;
            }
            if (string.IsNullOrEmpty(CurrentModData.Name)) CurrentModData.Name = CurrentModData.ModID;

            NameOnDisk = nameOnDisk;
            AvailableVersions = new List<Version>();
            if (InstalledVersion != null && !AvailableVersions.Contains(InstalledVersion)) AvailableVersions.Add(InstalledVersion);

            if (AllModData[InstalledVersion].Name.Length > 32) AllModData[InstalledVersion].Name = AllModData[InstalledVersion].Name.Substring(0, 32);
        }

        // . is informally allowed in mod IDs these days, to include author name
        // this violates the specification, but it is a de facto standard
        private static readonly Regex ModIDFilterRegex = new Regex(@"[^A-Za-z0-9\.]", RegexOptions.Compiled);
        public string ConstructName(int forcePriority = -1)
        {
            return AMLUtils.GeneratePriorityFromPositionInList(forcePriority >= 0 ? forcePriority : Priority) + "-" + ModIDFilterRegex.Replace(CurrentModData.ModID, "") + "-" + InstalledVersion + "_P.pak";
        }

        private int newPriority;
        private string newModID;
        private Version newModVersion;
        private void PerformNameAnalysis()
        {
            if (NameOnDisk == null) NameOnDisk = "";
            List<string> nameData = NameOnDisk.Split('_')[0].Split('-').ToList();
            int origCount = nameData.Count;

            if (origCount >= 1)
            {
                try
                {
                    newPriority = int.Parse(nameData[0]);
                }
                catch (FormatException)
                {
                    newPriority = 1;
                }
                nameData.RemoveAt(0);
            }
            else
            {
                newPriority = 1;
            }

            if (origCount >= 2)
            {
                if (!string.IsNullOrEmpty(nameData[0])) newModID = nameData[0];
                nameData.RemoveAt(0);
            }
            else
            {
                newModID = NameOnDisk.Replace(".pak", "");
            }

            newModVersion = new Version(0, 1, 0);
            if (origCount >= 3)
            {
                if (!string.IsNullOrEmpty(nameData[0])) newModVersion = new Version(nameData[0]);
                nameData.RemoveAt(0);
            }
        }

        public static volatile string ThunderstoreFetched = null;

        public IndexFile GetIndexFile(List<string> duplicateURLs)
        {
            DownloadInfo di = CurrentModData.Download;
            if (di == null) return null;

            try
            {
                if (di.Type == DownloadMode.IndexFile && !string.IsNullOrEmpty(di.URL))
                {
                    if (duplicateURLs != null && duplicateURLs.Contains(di.URL)) return null;
                    string rawIndexFileData = "";
                    using (var wb = new WebClient())
                    {
                        wb.Headers[HttpRequestHeader.UserAgent] = AMLUtils.UserAgent;
                        rawIndexFileData = wb.DownloadString(di.URL);
                    }
                    if (string.IsNullOrEmpty(rawIndexFileData)) return null;

                    IndexFile indexFile = JsonConvert.DeserializeObject<IndexFile>(rawIndexFileData);
                    indexFile.OriginalURL = di.URL;
                    return indexFile;
                }
                else if (di.Type == DownloadMode.Thunderstore && !string.IsNullOrEmpty(di.ThunderstoreNamespace) && !string.IsNullOrEmpty(di.ThunderstoreName))
                {
                    string origUrl = "https://thunderstore.io/c/astroneer/api/v1/package/";
                    if (ThunderstoreFetched == null)
                    {
                        using (var wb = new WebClient())
                        {
                            wb.Headers[HttpRequestHeader.UserAgent] = AMLUtils.UserAgent;
                            ThunderstoreFetched = wb.DownloadString(origUrl);
                        }
                    }

                    if (string.IsNullOrEmpty(ThunderstoreFetched)) return null;

                    IndexFile indexFile = new IndexFile();
                    indexFile.OriginalURL = origUrl;
                    indexFile.Mods = new Dictionary<string, IndexMod>();

                    dynamic tStorePackages = JsonConvert.DeserializeObject(ThunderstoreFetched); // array
                    dynamic tStorePackage = null;
                    string fullName = di.ThunderstoreNamespace + "-" + di.ThunderstoreName;
                    foreach (var package in tStorePackages)
                    {
                        if (package["full_name"].Value == fullName)
                        {
                            tStorePackage = package;
                            break;
                        }
                    }
                    if (tStorePackage == null) return null;

                    var idxMod = new IndexMod();
                    idxMod.AllVersions = new Dictionary<Version, IndexVersionData>();
                    idxMod.LatestVersion = null;
                    foreach (var version in tStorePackage["versions"])
                    {
                        Version ver = Version.Parse(version["version_number"].Value);
                        if (idxMod.LatestVersion == null || ver > idxMod.LatestVersion) idxMod.LatestVersion = ver;
                        idxMod.AllVersions[ver] = new IndexVersionData();
                        idxMod.AllVersions[ver].URL = version["download_url"].Value;
                        idxMod.AllVersions[ver].Filename = CurrentModData.ModID + "-" + ver.ToString() + ".zip";
                    }

                    indexFile.Mods[CurrentModData.ModID] = idxMod;

                    return indexFile;
                }
            }
            catch (Exception ex)
            {
                if (ex is WebException || ex is JsonException) return null;
                throw;
            }

            return null;
        }

        public override bool Equals(object obj)
        {
            string comparer;
            if (obj is Mod mobj)
            {
                comparer = mobj.NameOnDisk;
            }
            else if (obj is string sobj)
            {
                comparer = sobj;
            }
            else
            {
                return false;
            }
            return NameOnDisk.Equals(comparer);
        }

        public override int GetHashCode()
        {
            return NameOnDisk.GetHashCode();
        }

        public override string ToString()
        {
            return Enabled.ToString();
        }

        public object Clone()
        {
            var modClone = new Mod(null, this.NameOnDisk);
            modClone.AvailableVersions = this.AvailableVersions.ToList();
            modClone.AvailableVersions.ForEach(x => x.Clone());
            modClone.AllModData = this.AllModData.ToDictionary(entry => (Version)entry.Key.Clone(), entry => (Metadata)entry.Value.Clone());
            modClone.InstalledVersion = (Version)this.InstalledVersion.Clone();
            modClone.ForceLatest = this.ForceLatest;
            modClone.Enabled = this.Enabled;
            modClone.IsOptional = this.IsOptional;
            modClone.Priority = this.Priority;
            return modClone;
        }
    }
}
