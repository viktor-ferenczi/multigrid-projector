using System;
using System.Reflection;
using MultigridProjector.Logic;
using MultigridProjector.Utilities;
using Sandbox.Game.Entities.Blocks;
using Torch.Managers.PatchManager;
using VRageMath;

namespace MultigridProjectorServer
{
    [PatchShim]
    // ReSharper disable once InconsistentNaming
    public static class MyProjectorBase_BuildInternal
    {
        // // ReSharper disable once UnusedMember.Global
        // public static IEnumerable<MsilInstruction> PostTranspiler(IEnumerable<MsilInstruction> instructions)
        // {
        //     yield return new MsilInstruction(OpCodes.Ldarg_0);
        //     yield return new MsilInstruction(OpCodes.Ldarg_1);
        //     yield return new MsilInstruction(OpCodes.Ldarg_2);
        //     yield return new MsilInstruction(OpCodes.Ldarg_3);
        //     yield return Ldargs_S(4);
        //     yield return Ldargs_S(5);
        //     yield return CallMethod(typeof (MyProjectorBase_BuildInternal).GetMethod(nameof(Replacement), BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic));
        //     yield return new MsilInstruction(OpCodes.Ret);
        // }
        //
        // private static MsilInstruction Ldargs_S(int n)
        // {
        //     var instruction = new MsilInstruction(OpCodes.Ldarg_S);
        //     instruction.InlineValue(n);
        //     return instruction;
        // }
        //
        // private static MsilInstruction CallMethod(MethodInfo methodInfo)
        // {
        //     var instruction = new MsilInstruction(OpCodes.Call);
        //     instruction.InlineValue(methodInfo);
        //     return instruction;
        // }

        // IMPORTANT: Do NOT use a Prefix method! Although that would be simpler it is prone to random crashes with null dereference when a
        // multiplayer event handler is patched. It happens even if everything is prefect with the patch. It crashes before even executing the
        // Prefix method, so no way to fix that without changing Harmony itself. Just use a transpiler and redirect to a static method replacement.
        public static void Patch(PatchContext ctx) => ctx.GetPattern(typeof (MyProjectorBase).GetMethod("BuildInternal", BindingFlags.Instance | BindingFlags.NonPublic)).Prefixes.Add(typeof (MyProjectorBase_BuildInternal).GetMethod(nameof(Prefix), BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic));

        // [Server]

        private static bool Prefix(
            // ReSharper disable once InconsistentNaming
            MyProjectorBase __instance,
            Vector3I cubeBlockPosition,
            long owner,
            long builder,
            bool requestInstant,
            long builtBy)
        {
            var projector = __instance;

            try
            {
                // Find the multigrid projection, fall back to the default implementation if this projector is not handled by the plugin
                if (!MultigridProjection.TryFindProjectionByProjector(projector, out var projection))
                    return true;

                // We use the builtBy field to pass the subgrid index
                projection.BuildInternal(cubeBlockPosition, owner, builder, requestInstant, (int) builtBy);
            }
            catch (Exception e)
            {
                PluginLog.Error(e);
            }

            return false;
        }
    }
}