using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using MultigridProjector.Utilities;
using Sandbox.Game.Entities.Blocks;

namespace MultigridProjector.Patches
{
    // Replacing:
    // if (smallToLarge && myCubeSize == MyCubeSize.Large)
    //     myCubeSize = MyCubeSize.Small;
    //
    // IL_0017: ldloc.0      // myCubeSize
    // IL_0018: brtrue.s     IL_001c
    // IL_001a: ldc.i4.1
    // IL_001b: stloc.0      // myCubeSize
    //
    // With:
    // IL_0017: ldloc.0      // myCubeSize
    // IL_0018: ldc.i4.1
    // IL_001a: xor
    // IL_001b: stloc.0      // myCubeSize

    // ReSharper disable once UnusedType.Global
    [HarmonyPatch(typeof(MyMechanicalConnectionBlockBase))]
    [HarmonyPatch("CreateTopPart")]
    [EnsureOriginal("192978a5")]
    // ReSharper disable once InconsistentNaming
    public static class MyMechanicalConnectionBlockBase_CreateTopPart
    {
        [ServerOnly]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var code = instructions.ToList();

            var index = code.FindIndex(i => i.opcode == OpCodes.Ldloc_0);
            code[index + 1] = new CodeInstruction(OpCodes.Ldc_I4_1);
            code[index + 2] = new CodeInstruction(OpCodes.Xor);

            return code.AsEnumerable();
        }
    }
}