using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using MultigridProjector.Tools;
using MultigridProjector.Utilities;
using Sandbox.Game.Entities.Blocks;

namespace MultigridProjector.Patches
{
    /* This patch is replacing the mechanism determining the size of the top block to allow
     for building any combination as determined by the preview blocks (blueprint).

     Original:

        if ((topSize == MyTopBlockSize.Small || topSize == MyTopBlockSize.Medium) && myCubeSize == MyCubeSize.Large)
        {
            myCubeSize = MyCubeSize.Small;
        }

        ldarg.s 4
        ldc.i4.2
        beq.s L1
        ldarg.s 4
        ldc.i4.1
        bne.un.s L2
        L1:
        ldloc.0
        brtrue.s L3
        ldc.i4.1
        stloc.0
        L2:
        L3:

     Modified code (written as IL) is inverting the cube size based on the topSize parameter introduced in 1.205.023:

        if (topSize != MyTopBlockSize.Normal) {
            myCubeSize ^= 1;
        }

        ldarg.s 4		// topSize
        ldc.i4.0		// MyTopBlockSize.Normal
        beq.s L1		// If topSize == MyTopBlockSize.Normal, then the top block has the same size as the base
        ldloc.0			// myCubeSize
        ldc.i4.1
        xor				// Invert the top's block size
        stloc.0			// myCubeSize
        L1:

     */

    // ReSharper disable once UnusedType.Global
    [HarmonyPatch(typeof(MyMechanicalConnectionBlockBase))]
    [HarmonyPatch("CreateTopPart")]
    [EnsureOriginal("439d944c")]
    // ReSharper disable once InconsistentNaming
    public static class MyMechanicalConnectionBlockBase_CreateTopPart
    {
        [ServerOnly]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var il = instructions.ToList();
            il.RecordOriginalCode();

            // Remove old code section
            var j = il.FindIndex(i => i.opcode == OpCodes.Ldarg_S);
            var k = il.FindIndex(j, i => i.opcode == OpCodes.Ldarg_0);
            il.RemoveRange(j, k - j);

            var l1 = generator.DefineLabel(); 
            
            il.Insert(j++, new CodeInstruction(OpCodes.Ldarg_S, 4));
            il.Insert(j++, new CodeInstruction(OpCodes.Ldc_I4_0));
            il.Insert(j++, new CodeInstruction(OpCodes.Beq_S, l1));
            il.Insert(j++, new CodeInstruction(OpCodes.Ldloc_0));
            il.Insert(j++, new CodeInstruction(OpCodes.Ldc_I4_1));
            il.Insert(j++, new CodeInstruction(OpCodes.Xor));
            il.Insert(j++, new CodeInstruction(OpCodes.Stloc_0));
            
            il[j].labels.Clear();
            il[j].labels.Add(l1);

            il.RecordPatchedCode();
            return il.AsEnumerable();
        }
    }
}