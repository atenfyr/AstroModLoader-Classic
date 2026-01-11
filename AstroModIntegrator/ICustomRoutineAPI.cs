using System.Collections.Generic;
using UAssetAPI;
using UAssetAPI.UnrealTypes;

namespace AstroModIntegrator
{
    /// <summary>
    /// API for custom routines.
    /// </summary>
    public interface ICustomRoutineAPI
    {
        /// <summary>
        /// Find a specific asset.
        /// <para>This operation searches .pak files in the following order:</para>
        /// <para>1st. assets already added to the deployment pak</para>
        /// <para>2nd. mod assets</para>
        /// <para>3rd. base game assets</para>
        /// </summary>
        /// <param name="target">The package name or raw file path to fetch.</param>
        /// <returns>A UAsset containing the desired asset, or null.</returns>
        public UAsset FindFile(string target);

        /// <summary>
        /// Find a specific file.
        /// <para>This operation searches .pak files in the following order:</para>
        /// <para>1st. assets already added to the deployment pak</para>
        /// <para>2nd. mod assets</para>
        /// <para>3rd. base game assets</para>
        /// </summary>
        /// <param name="target">The raw file path to fetch.</param>
        /// <returns>A byte array containing the raw binary data of the file that was fetched.</returns>
        public byte[] FindFileRaw(string target);

        /// <summary>
        /// Find a specific file.
        /// <para>This operation searches .pak files in the following order:</para>
        /// <para>1st. assets already added to the deployment pak</para>
        /// <para>2nd. mod assets</para>
        /// <para>3rd. base game assets</para>
        /// </summary>
        /// <param name="target">The raw file path to fetch.</param>
        /// <param name="engVer">An output variable containing the engine version of the asset, if appropriate.</param>
        /// <returns>A byte array containing the raw binary data of the file that was fetched.</returns>
        public byte[] FindFileRaw(string target, out EngineVersion engVer);

        /// <summary>
        /// Add an asset to be deployed in the integrator pak. Overrides any asset at the same path that have already been added to the integrator pak.
        /// </summary>
        /// <param name="outPath">The desired output package name or raw file path of the asset.</param>
        /// <param name="outAsset">The asset to deploy.</param>
        public void AddFile(string outPath, UAsset outAsset);

        /// <summary>
        /// Add a raw file to be deployed in the integrator pak. Overrides any file at the same path that have already been added to the integrator pak.
        /// </summary>
        /// <param name="outPath">The desired output raw file path of the file.</param>
        /// <param name="rawData">The raw binary data of the file to deploy.</param>
        public void AddFileRaw(string outPath, byte[] rawData);

        /// <summary>
        /// Get the current mod being integrated. May return null.
        /// </summary>
        /// <returns>The Metadata class corresponding to the current mod being integrated.</returns>
        public Metadata GetCurrentMod();

        /// <summary>
        /// Get a list of all mods being integrated. Will never return null.
        /// </summary>
        /// <returns>A list of Metadata classes corresponding to every mod being integrated.</returns>
        public IReadOnlyList<Metadata> GetAllMods();

        /// <summary>
        /// Get the mod corresponding to a specific custom routine. May return null.
        /// </summary>
        /// <param name="routine">The routine for which the current mod should be obtained.</param>
        /// <returns>The Metadata class corresponding to the desired mod.</returns>
        public Metadata GetModFromRoutine(CustomRoutine routine);

        /// <summary>
        /// Fetch a specific custom routine from its ID. May return null.
        /// </summary>
        /// <param name="routineID">The ID of the custom routine to fetch</param>
        /// <returns>The requested CustomRoutine, or null if it could not be found.</returns>
        public CustomRoutine GetCustomRoutineFromID(string routineID);

        /// <summary>
        /// Log some text to disk.
        /// </summary>
        /// <param name="text">The text to log to disk. The text will automatically be suffixed with a newline character.</param>
        /// <param name="prefixWithMod">Whether or not to prefix the message with the current mod name. Defaults to true.</param>
        /// <returns>Whether or not the operation succeeded.</returns>
        public bool LogToDisk(string text, bool prefixWithMod = true);

        /// <summary>
        /// Whether or not the custom routine should exit immediately. It is a good idea to check this method occasionally when executing large tasks.
        /// </summary>
        /// <returns>Whether or not the custom routine should exit immediately.</returns>
        public bool ShouldExitNow();
    }

}
