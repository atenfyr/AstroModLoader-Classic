using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel;

namespace AstroModLoader
{
    public class ModConfig
    {
        [JsonProperty("install_path")]
        [DefaultValue("")]
        public string GamePath;

        [JsonProperty("launch_path")]
        public string LaunchCommand;

        public bool ShouldSerializeLaunchCommand()
        {
            return !string.IsNullOrEmpty(LaunchCommand);
        }

        [JsonProperty("refuse_mismatched_connections", DefaultValueHandling = DefaultValueHandling.Populate)]
        [DefaultValue(true)]
        public bool RefuseMismatchedConnections;

        [JsonProperty("disable_lua_cleanup", DefaultValueHandling = DefaultValueHandling.Populate)]
        [DefaultValue(false)]
        public bool DisableLuaCleanup;

        [JsonProperty("current")]
        public ModProfile ModsOnDisk;

        [JsonProperty("profiles")]
        public Dictionary<string, ModProfile> Profiles;
    }
}
