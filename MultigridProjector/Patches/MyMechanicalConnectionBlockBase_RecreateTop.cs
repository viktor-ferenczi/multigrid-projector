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

    // IL_006c: ldloc.1      // myCubeSize
    // IL_006d: brtrue.s     IL_0071
    // IL_006f: ldc.i4.1
    // IL_0070: stloc.1      // myCubeSize

    // With:
    // IL_006c: ldloc.1      // myCubeSize
    // IL_006d: ldind.i1     1
    // IL_006f: xor
    // IL_0070: stloc.1      // myCubeSize

    // ReSharper disable once UnusedType.Global
    [HarmonyPatch(typeof(MyMechanicalConnectionBlockBase))]
    [HarmonyPatch("RecreateTop")]
    [EnsureOriginal("322f8da0")]
    // ReSharper disable once InconsistentNaming
    public static class MyMechanicalConnectionBlockBase_RecreateTop
    {
        [ServerOnly]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var code = instructions.ToList();

            var index = code.FindIndex(i => i.opcode == OpCodes.Ldloc_1);
            code[index + 1] = new CodeInstruction(OpCodes.Ldc_I4_1);
            code[index + 2] = new CodeInstruction(OpCodes.Xor);

            foreach (var instruction in code)
                yield return instruction;
        }
    }
}