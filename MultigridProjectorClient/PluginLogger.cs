#if DEBUG
//#define USE_SHOW_MESSAGE_FOR_DEBUGGING
#endif

using System.Runtime.CompilerServices;
using MultigridProjector.Utilities;
using Sandbox.ModAPI;
using VRage.Utils;

namespace MultigridProjectorClient
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

#if USE_SHOW_MESSAGE_FOR_DEBUGGING
            if (MyAPIGateway.Session != null)
                MyAPIGateway.Utilities.ShowMessage("Multigrid Projector", $"{msg}");
#endif
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

            if (MyAPIGateway.Session != null)
                MyAPIGateway.Utilities.ShowMessage("Multigrid Projector", $"Please report this exception: {msg}");
        }
    }
}