using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using MultigridProjector.Logic;
using MultigridProjector.Tools;
using MultigridProjector.Utilities;
using Sandbox.Game.Entities.Blocks;

namespace MultigridProjector.Patches
{
    /* Disable the automatic building of top parts if the base block is built from a projection

    // [668 7 - 668 49]
    IL_0000: ldarg.0      // this
    IL_0001: ldarg.1      // builtBy
    IL_0002: ldarg.2      // instantBuild
    IL_0003: call         instance void Sandbox.Game.Entities.MyCubeBlock::OnBuildSuccess(int64, bool)

    // [669 7 - 669 51]
    IL_0008: call         bool Sandbox.Game.Multiplayer.Sync::get_IsServer()
    IL_000d: brfalse.s    IL_0018

    <Inserts a call and jump here>

    // [671 7 - 671 64]
    IL_000f: ldarg.0      // this
    IL_0010: ldarg.1      // builtBy
    IL_0011: ldc.i4.0
    IL_0012: ldarg.2      // instantBuild
    IL_0013: call         instance void Sandbox.Game.Entities.Blocks.MyMechanicalConnectionBlockBase::CreateTopPartAndAttach(int64, bool, bool)

    IL_0018: ret

    */

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
            il.Insert(6, new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(MultigridProjection), nameof(MultigridProjection.IsBuildingProjection))));
            il.Insert(7, new CodeInstruction(OpCodes.Brtrue, il[5].operand));

            il.RecordPatchedCode();
            return il.AsEnumerable();
        }
    }
}