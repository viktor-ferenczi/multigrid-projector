using System.Reflection;
using HarmonyLib;
using Sandbox.Game.Entities.Cube;

namespace MultigridProjector.Extensions
{
    public static class MyMotorStatorExtensions
    {
        private static readonly MethodInfo SetAngleToPhysicsMethodInfo = AccessTools.DeclaredMethod(typeof(MyMotorStator), "SetAngleToPhysics");
        public static void SetAngleToPhysics(this MyMotorStator entity)
        {
            SetAngleToPhysicsMethodInfo.Invoke(entity, new object[]{});
        }
    }
}