using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using MultigridProjector.Logic;
using MultigridProjector.Tools;
using MultigridProjector.Utilities;
using Sandbox.Game.Entities.Blocks;
using VRageMath;

namespace MultigridProjectorClient.Patches
{
    // ReSharper disable once UnusedType.Global
    [HarmonyPatch(typeof(MyProjectorBase))]
    [HarmonyPatch("BuildInternal")]
    [EnsureOriginal("ee2506ad")]
    // ReSharper disable once InconsistentNaming
    public static class MyProjectorBase_BuildInternal
    {
        // ReSharper disable once UnusedMember.Global
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            instructions.ToList().RecordOriginalCode();

            var il = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldarg_1),
                new CodeInstruction(OpCodes.Ldarg_2),
                new CodeInstruction(OpCodes.Ldarg_3),
                new CodeInstruction(OpCodes.Ldarg_S, 4),
                new CodeInstruction(OpCodes.Ldarg_S, 5),
                new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(MyProjectorBase_BuildInternal), nameof(BuildInternal))),
                new CodeInstruction(OpCodes.Ret)
            };
            
            il.RecordPatchedCode();
            return il.AsEnumerable();
        }

        // IMPORTANT: Do NOT use a Prefix method! Although that would be simpler it is prone to random crashes with null dereference when a
        // multiplayer event handler is patched. It happens even if everything is prefect with the patch. It crashes before even executing the
        // Prefix method, so no way to fix that without changing Harmony itself. Just use a transpiler and redirect to a static method replacement.

        [ServerOnly]
        public static void BuildInternal(
            // ReSharper disable once InconsistentNaming
            MyProjectorBase __instance,
            Vector3I cubeBlockPosition,
            long owner,
            long builder,
            bool requestInstant,
            long builtBy)
        {
            var projector = __instance;
            
            // Find the multigrid projection, fall back to the default implementation if this projector is not handled by the plugin
            if (!MultigridProjection.TryFindProjectionByProjector(projector, out var projection))
                return;

            // We use the builtBy field to pass the subgrid index
#if DEBUG
            projection.BuildInternal(cubeBlockPosition, owner, builder, requestInstant, builtBy);
#else            
            try
            {
                projection.BuildInternal(cubeBlockPosition, owner, builder, requestInstant, builtBy);
            }
            catch (Exception e)
            {
                PluginLog.Error(e);
            }
#endif
        }
    }
}