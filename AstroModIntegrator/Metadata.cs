using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace AstroModIntegrator
{
    // modified so that no error is thrown on failure
    // adapted from Json.NET: https://github.com/JamesNK/Newtonsoft.Json/blob/master/Src/Newtonsoft.Json/Converters/VersionConverter.cs
    // see NOTICE.md
    public class VersionConverter2 : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
            }
            else if (value is Version)
            {
                writer.WriteValue(value.ToString());
            }
            else
            {
                writer.WriteNull();
            }
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }
            else
            {
                if (reader.TokenType == JsonToken.String)
                {
                    string val = reader.Value! as string;
                    if (string.IsNullOrEmpty(val)) return null;

                    try
                    {
                        return new Version(val);
                    }
                    catch
                    {
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Version);
        }
    }

    // modified to allow unknown enum values
    public class StringEnumConverter2 : StringEnumConverter
    {
        public static readonly List<string> ValidDownloadModes = new List<string>() { "index_file", "thunderstore" };

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.String)
            {
                string enumText = reader.Value.ToString();
                if (string.IsNullOrEmpty(enumText) || !ValidDownloadModes.Contains(enumText))
                {
                    return Enum.Parse(objectType, "Unknown");
                }
            }
            return base.ReadJson(reader, objectType, existingValue, serializer);
        }
    }

    public enum DownloadMode
    {
        [EnumMember(Value = "index_file")]
        IndexFile,
        [EnumMember(Value = "thunderstore")]
        Thunderstore,
        [EnumMember(Value = "unknown")]
        Unknown
    }

    public class DownloadInfo
    {
        [JsonProperty("type")]
        [JsonConverter(typeof(StringEnumConverter2))]
        public DownloadMode Type;

        [JsonProperty("url")]
        [DefaultValue("")]
        public string URL;

        [JsonProperty("namespace")]
        [DefaultValue("")]
        public string ThunderstoreNamespace;

        [JsonProperty("name")]
        [DefaultValue("")]
        public string ThunderstoreName;
    }

    public enum SyncMode
    {
        [EnumMember(Value = "serverclient")]
        ServerAndClient,
        [EnumMember(Value = "server")]
        ServerOnly,
        [EnumMember(Value = "client")]
        ClientOnly,
        [EnumMember(Value = "none")]
        None
    }

    public class MetadataSchema1 : ICloneable
    {
        [JsonProperty("schema_version")]
        [DefaultValue(1)]
        public int SchemaVersion;

        [JsonProperty("name")]
        public string Name;

        [JsonProperty("mod_id")]
        [DefaultValue("")]
        public string ModID;

        [JsonProperty("author")]
        [DefaultValue("")]
        public string Author;

        [JsonProperty("description")]
        [DefaultValue("")]
        public string Description;

        [JsonProperty("version")]
        [JsonConverter(typeof(VersionConverter2))]
        public Version ModVersion;

        [JsonProperty("astro_build")]
        [JsonConverter(typeof(VersionConverter2))]
        public Version AstroBuild;

        [JsonProperty("sync")]
        [JsonConverter(typeof(StringEnumConverter))]
        public SyncMode Sync;

        [JsonProperty("homepage")]
        [DefaultValue("")]
        public string Homepage;

        [JsonProperty("download")]
        public DownloadInfo Download;

        [JsonProperty("linked_actor_components")]
        public Dictionary<string, List<string>> LinkedActorComponents;

        [JsonProperty("item_list_entries")]
        public Dictionary<string, Dictionary<string, List<string>>> ItemListEntries;

        [JsonProperty("persistent_actors")]
        public List<string> PersistentActors;

        [JsonProperty("mission_trailheads")]
        public List<string> MissionTrailheads;

        public object Clone()
        {
            return this.MemberwiseClone();
        }

        public Metadata Convert()
        {
            var res = new Metadata();

            res.Name = Name;
            res.ModID = ModID;
            res.Author = Author;
            res.Description = Description;
            res.ModVersion = ModVersion;
            res.GameBuild = AstroBuild;
            res.Sync = Sync;
            res.Homepage = Homepage;
            res.Download = Download;
            res.IntegratorEntries = new IntegratorEntries();
            res.IntegratorEntries.LinkedActorComponents = LinkedActorComponents;
            res.IntegratorEntries.ItemListEntries = ItemListEntries;
            res.IntegratorEntries.PersistentActors = PersistentActors;
            res.IntegratorEntries.PersistentActorMaps = null;
            res.IntegratorEntries.MissionTrailheads = MissionTrailheads;
            res.IntegratorEntries.BiomePlacementModifiers = null;
            res.Dependencies = null;

            return res;
        }
    }

    public struct IntegratorEntries : ICloneable
    {
        [JsonProperty("linked_actor_components")]
        public Dictionary<string, List<string>> LinkedActorComponents;

        [JsonProperty("item_list_entries")]
        public Dictionary<string, Dictionary<string, List<string>>> ItemListEntries;

        [JsonProperty("persistent_actors")]
        public List<string> PersistentActors;

        // undocumented, but we implement anyways; list of maps to integrate PersistentActors with; NOT /Game/ paths, but raw paths in the .pak file
        // defaults to ["Astro/Content/Maps/Staging_T2.umap", "Astro/Content/Maps/Staging_T2_PackedPlanets_Switch.umap", "Astro/Content/U32_Expansion/U32_Expansion.umap"]
        [JsonProperty("persistent_actors_maps")]
        public List<string> PersistentActorMaps;

        [JsonProperty("mission_trailheads")]
        public List<string> MissionTrailheads;

        [JsonProperty("biome_placement_modifiers")]
        public List<PlacementModifier> BiomePlacementModifiers;

        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }

    public class Metadata : ICloneable
    {
        [JsonProperty("schema_version")]
        [DefaultValue(2)]
        public int SchemaVersion;

        [JsonProperty("name")]
        public string Name;

        [JsonProperty("mod_id")]
        [DefaultValue("")]
        public string ModID;

        [JsonProperty("author")]
        [DefaultValue("")]
        public string Author;

        [JsonProperty("description")]
        [DefaultValue("")]
        public string Description;

        [JsonProperty("version")]
        [JsonConverter(typeof(VersionConverter2))]
        public Version ModVersion;

        [JsonProperty("game_build")]
        [JsonConverter(typeof(VersionConverter2))]
        public Version GameBuild;

        [JsonProperty("sync")]
        [JsonConverter(typeof(StringEnumConverter))]
        public SyncMode Sync;

        [JsonProperty("homepage")]
        [DefaultValue("")]
        public string Homepage;

        [JsonProperty("enable_ue4ss")]
        [DefaultValue(false)]
        public bool EnableUE4SS;

        [JsonProperty("download")]
        public DownloadInfo Download;

        [JsonProperty("integrator")]
        public IntegratorEntries IntegratorEntries;

        // for now, unimplemented
        // standard is ambiguous as to whether or not this field is optional, but it is de facto
        [JsonProperty("dependencies")]
        public Dictionary<string, object> Dependencies;

        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }
}
