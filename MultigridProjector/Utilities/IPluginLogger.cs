namespace MultigridProjector.Utilities
{
    public interface IPluginLogger
    {
        void Info(string msg);
        void Debug(string msg);
        void Warn(string msg);
        void Error(string msg);
    }
}