using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using Sandbox.Game.Weapons;
using VRage.Network;

namespace MultigridProjector.Patches
{
    [SuppressMessage("ReSharper", "UnusedType.Global")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "UnusedMember.Local")]
    [HarmonyPatch(typeof(MyShipToolBase))]
    public class MyShipToolBase_UpdateAfterSimulation10
    {
        [Server]
        [HarmonyPrefix]
        [HarmonyPatch(nameof(MyShipToolBase.UpdateAfterSimulation10))]
        private static bool UpdateAfterSimulation10Prefix(MyShipToolBase __instance)
        {
            // Prevent projected welders and grinders from functioning
            return __instance?.CubeGrid?.Physics != null;
        }
    }
}