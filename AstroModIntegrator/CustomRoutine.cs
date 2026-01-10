namespace AstroModIntegrator
{
    public abstract class CustomRoutine
    {
        public virtual string RoutineID { get { return "None"; } }
        public virtual bool Enabled { get { return false; } }

        public virtual void Execute(ICustomRoutineAPI api)
        {

        }

        public CustomRoutine()
        {

        }
    }
}
