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
