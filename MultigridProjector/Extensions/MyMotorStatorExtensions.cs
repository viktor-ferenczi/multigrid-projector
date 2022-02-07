using System.Reflection;
using HarmonyLib;
using MultigridProjector.Utilities;
using Sandbox.Game.Entities.Cube;

namespace MultigridProjector.Extensions
{
    public static class MyMotorStatorExtensions
    {
        private static readonly MethodInfo SetAngleToPhysicsMethodInfo = Validation.EnsureInfo(AccessTools.DeclaredMethod(typeof(MyMotorStator), "SetAngleToPhysics"));

        public static void SetAngleToPhysics(this MyMotorStator entity)
        {
            SetAngleToPhysicsMethodInfo.Invoke(entity, new object[] { });
        }
    }
}