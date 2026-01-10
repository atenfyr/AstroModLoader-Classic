namespace AstroModIntegrator
{
    public abstract class CustomRoutine
    {
        public virtual string RoutineID { get { return "None"; } }
        public virtual void Execute(ICustomRoutineAPI api)
        {

        }

        public CustomRoutine()
        {

        }
    }
}
