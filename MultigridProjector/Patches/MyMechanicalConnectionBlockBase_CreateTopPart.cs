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
     
     This works with the topSize parameter introduced in 1.205.023

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

     Additional code is inserted before the above code segment. 
     The new code is handling out of range enum values 10 and 11 in topSize. 
     10 means that the top block has the same size as the base, 11 means it has the opposite side.
     The vanilla game code will never pass these, therefore the original functionality is preserved.
     MGP passes 10 and 11 according to the base and top block sizes.

        if (topSize == 10) {
            goto L3;
        }
        
        if (topSize == 11) {
            myCubeSize ^= 1;
            goto L3;
        }

        ldarg.s 4
        ldc.i4.s 10
        beq.s L2
        
        ldarg.s 4
        ldc.i4.s 11
        bne.s L15
        
        ldloc.0			// myCubeSize
        ldc.i4.1
        xor				// Invert the top's block size
        stloc.0			// myCubeSize
        
        br L2
        
        L15:
        
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

            var j = il.FindIndex(i => i.opcode == OpCodes.Ldarg_S);
            var k = il.FindIndex(j, i => i.opcode == OpCodes.Ldarg_0);

            var l2 = il[k].labels.First();
            var l15 = generator.DefineLabel();
            
            il.Insert(j++, new CodeInstruction(OpCodes.Ldarg_S, 4));
            il.Insert(j++, new CodeInstruction(OpCodes.Ldc_I4_S, 10));
            il.Insert(j++, new CodeInstruction(OpCodes.Beq_S, l2));

            il.Insert(j++, new CodeInstruction(OpCodes.Ldarg_S, 4));
            il.Insert(j++, new CodeInstruction(OpCodes.Ldc_I4_S, 11));
            il.Insert(j++, new CodeInstruction(OpCodes.Bne_Un_S, l15));
            
            il.Insert(j++, new CodeInstruction(OpCodes.Ldloc_0));
            il.Insert(j++, new CodeInstruction(OpCodes.Ldc_I4_1));
            il.Insert(j++, new CodeInstruction(OpCodes.Xor));
            il.Insert(j++, new CodeInstruction(OpCodes.Stloc_0));
            
            il.Insert(j++, new CodeInstruction(OpCodes.Br_S, l15));
            
            il[j].labels.Add(l15);

            il.RecordPatchedCode();
            return il.AsEnumerable();
        }
    }
}