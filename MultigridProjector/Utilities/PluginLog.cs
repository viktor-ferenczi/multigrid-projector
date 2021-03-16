using System;
using System.Runtime.CompilerServices;

namespace MultigridProjector.Utilities
{
    public static class PluginLog
    {
        private const string PluginName = "Multigrid Projector";

        public static IPluginLogger Logger;
        public static string Prefix = $"{PluginName}: ";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void EnsureLogger()
        {
            if (Logger == null)
                throw new Exception($"{PluginName}: No logger registered");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Info(string msg)
        {
            EnsureLogger();
            Logger.Info($"{Prefix}{msg}");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Debug(string msg)
        {
            EnsureLogger();
            Logger.Debug($"{Prefix}{msg}");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Warn(string msg)
        {
            EnsureLogger();
            Logger.Warn($"{Prefix}{msg}");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Error(string msg)
        {
            EnsureLogger();
            Logger.Error($"{Prefix}{msg}");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Error(Exception e, string msg="")
        {
            EnsureLogger();
            var sep = msg == "" ? "" : "; ";
            Logger.Error($"{Prefix}{msg}{sep}{e}");
        }
    }
}