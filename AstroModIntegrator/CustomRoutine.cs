namespace AstroModIntegrator
{
    public abstract class CustomRoutine
    {
        /// <summary>
        /// ID of the routine.
        /// </summary>
        public virtual string RoutineID { get { return "None"; } }
        /// <summary>
        /// Whether or not this routine should actually be enabled.
        /// </summary>
        public virtual bool Enabled { get { return false; } }
        /// <summary>
        /// The API version of this routine. Incremented when there are backwards-incompatible changes.
        /// <para/>
        /// 1: the initial version. Custom routine is implemented in Execute(ICustomRoutineAPI api)
        /// <para/>
        /// The latest version is 1. The default version (if APIVersion is unspecified) is 1.
        /// Custom routines for future versions would be implemented in Execute(ICustomRoutineAPI_V2 api), Execute(ICustomRoutineAPI_V3 api), etc. as needed
        /// </summary>
        public virtual int APIVersion { get { return 1; } }
        /// <summary>
        /// Whether or not to request that the sandbox be disabled. This option is only functional on the Debug_CustomRoutineTest configuration.
        /// <para/>
        /// This may be useful for some developers who wish to add breakpoints to their code, but will also allow code to function that may not function with the sandbox enabled.
        /// </summary>
        public virtual bool RequestNoSandbox { get { return false; } }

        public virtual void Execute(ICustomRoutineAPI api)
        {

        }

        public CustomRoutine()
        {

        }
    }
}
