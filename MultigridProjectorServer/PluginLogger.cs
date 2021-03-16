using System.Runtime.CompilerServices;
using MultigridProjector.Utilities;
using NLog;

namespace MultigridProjectorServer
{
    internal class PluginLogger : IPluginLogger
    {
        private readonly Logger _log;
        public PluginLogger(string pluginName)
        {
            _log = LogManager.GetLogger(pluginName);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Info(string msg)
        {
            _log.Info(msg);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Debug(string msg)
        {
            _log.Debug(msg);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Warn(string msg)
        {
            _log.Warn(msg);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Error(string msg)
        {
            _log.Error(msg);
        }
    }
}