using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using MultigridProjector.Logic;
using MultigridProjector.Tools;
using MultigridProjector.Utilities;
using Sandbox.Game.Entities.Blocks;

namespace MultigridProjector.Patches
{
    // Disable the automatic building of top parts if the base block is built from a projection
    // and the projection has a subgrid defined for the top block

    // ReSharper disable once UnusedType.Global
    [HarmonyPatch(typeof(MyMechanicalConnectionBlockBase))]
    [HarmonyPatch(nameof(MyMechanicalConnectionBlockBase.OnBuildSuccess))]
    [EnsureOriginal("9d1cc43c")]
    // ReSharper disable once InconsistentNaming
    public static class MyMechanicalConnectionBlockBase_OnBuildSuccess
    {
        [ServerOnly]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var il = instructions.ToList();
            il.RecordOriginalCode();

            // Jump to ret if the mechanical base is built from projection (disables building the default top part)
            var j = il.FindIndex(i => i.opcode == OpCodes.Brfalse_S);
            Debug.Assert(j > 0);
            var k = j + 1;
            il.Insert(k++, new CodeInstruction(OpCodes.Ldarg_0));
            il.Insert(k++, new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(MultigridProjection), nameof(MultigridProjection.ShouldAllowBuildingDefaultTopBlock))));
            il.Insert(k, new CodeInstruction(OpCodes.Brfalse, il[j].operand));

            il.RecordPatchedCode();
            return il.AsEnumerable();
        }
    }
}