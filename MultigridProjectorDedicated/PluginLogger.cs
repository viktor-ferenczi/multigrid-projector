using System.Runtime.CompilerServices;
using MultigridProjector.Utilities;
using NLog;
using VRage.Utils;

namespace MultigridProjectorDedicated
{
    internal class PluginLogger : IPluginLogger
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Info(string msg)
        {
            MyLog.Default.Info(msg);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Debug(string msg)
        {
            MyLog.Default.Debug(msg);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Warn(string msg)
        {
            MyLog.Default.Warning(msg);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Error(string msg)
        {
            MyLog.Default.Error(msg);
        }
    }
}