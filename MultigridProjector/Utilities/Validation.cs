using System;

namespace MultigridProjector.Utilities
{
    public static class Validation
    {
        public static T EnsureInfo<T>(T info) where T : class
        {
            if (info == null)
                throw new Exception("AccessTools did not find something via reflection. Run with  Harmony.DEBUG = true  for more information.");

            return info;
        }
    }
}