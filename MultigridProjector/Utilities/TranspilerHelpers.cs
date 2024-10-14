using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using HarmonyLib;

namespace MultigridProjector.Tools
{
    public static class TranspilerHelpers
    {
        public delegate bool OpcodePredicate(OpCode opcode);

        public delegate bool CodeInstructionPredicate(CodeInstruction ci);

        public delegate bool FieldInfoPredicate(FieldInfo fi);

        public delegate bool PropertyInfoPredicatePredicate(PropertyInfo pi);

        public static List<int> FindAllIndex(this IEnumerable<CodeInstruction> il, CodeInstructionPredicate predicate)
        {
            return il.Select((instruction, index) => new { Instruction = instruction, Index = index })
                .Where(pair => predicate(pair.Instruction))
                .Select(pair => pair.Index)
                .ToList();
        }

        public static FieldInfo GetField(this List<CodeInstruction> il, FieldInfoPredicate predicate)
        {
            var ci = il.Find(i => (i.opcode == OpCodes.Ldfld || i.opcode == OpCodes.Stfld) && i.operand is FieldInfo fi && predicate(fi));
            if (ci == null)
                throw new CodeInstructionNotFound("No code instruction found loading or storing a field matching the given predicate");

            return (FieldInfo)ci.operand;
        }

        public static MethodInfo FindPropertyGetter(this List<CodeInstruction> il, string name)
        {
            var ci = il.Find(i => i.opcode == OpCodes.Call && i.operand is MethodInfo fi && fi.Name == $"get_{name}");
            if (ci == null)
                throw new CodeInstructionNotFound("No code instruction found getting or setting a property matching the given predicate");

            return (MethodInfo)ci.operand;
        }

        public static MethodInfo FindPropertySetter(this List<CodeInstruction> il, string name)
        {
            var ci = il.Find(i => i.opcode == OpCodes.Call && i.operand is MethodInfo fi && fi.Name == $"set_{name}");
            if (ci == null)
                throw new CodeInstructionNotFound("No code instruction found getting or setting a property matching the given predicate");

            return (MethodInfo)ci.operand;
        }

        public static Label GetLabel(this List<CodeInstruction> il, OpcodePredicate predicate)
        {
            var ci = il.Find(i => i.operand is Label && predicate(i.opcode));
            if (ci == null)
                throw new CodeInstructionNotFound("No label found matching the opcode predicate");

            return (Label)ci.operand;
        }

        public static void RemoveFieldInitialization(this List<CodeInstruction> il, string name)
        {
            var i = il.FindIndex(ci => ci.opcode == OpCodes.Stfld && ci.operand is FieldInfo fi && fi.Name.Contains(name));
            if (i < 2)
                throw new CodeInstructionNotFound($"No code instruction found initializing field: {name}");

            Debug.Assert(il[i - 2].opcode == OpCodes.Ldarg_0);
            Debug.Assert(il[i - 1].opcode == OpCodes.Newobj);

            il.RemoveRange(i - 2, 3);
        }

        private static string FormatCode(this List<CodeInstruction> il)
        {
            var sb = new StringBuilder();

            var hash = il.HashInstructions().CombineHashCodes().ToString("x8");
            sb.Append($"// {hash}\r\n");

            foreach (var ci in il)
            {
                sb.Append(ci.ToCodeLine());
                sb.Append("\r\n");
            }

            return sb.ToString();
        }

        private static string ToCodeLine(this CodeInstruction ci)
        {
            var sb = new StringBuilder();

            foreach (var label in ci.labels)
                sb.Append($"L{label.GetHashCode()}:\r\n");

            if (ci.blocks.Count > 0)
            {
                var formattedBlocks = string.Join(", ", ci.blocks.Select(b => $"EX_{b.blockType}"));
                sb.Append("[");
                sb.Append(formattedBlocks.Replace("Block", ""));
                sb.Append("]\r\n");
            }

            sb.Append(ci.opcode);

            var arg = FormatArgument(ci.operand);
            if (arg.Length > 0)
            {
                sb.Append(' ');
                sb.Append(arg);
            }

            return sb.ToString();
        }

        private static string FormatArgument(object argument, string extra = null)
        {
            switch (argument)
            {
                case null:
                    return "";

                case MethodBase member when extra == null:
                    return $"{member.FullDescription()}";

                case MethodBase member:
                    return $"{member.FullDescription()} {extra}";
            }

            var fieldInfo = argument as FieldInfo;
            if (fieldInfo != null)
                return fieldInfo.FieldType.FullDescription() + " " + fieldInfo.DeclaringType.FullDescription() + "::" + fieldInfo.Name;

            switch (argument)
            {
                case Label label:
                    return $"L{label.GetHashCode()}";

                case Label[] labels:
                    return string.Join(", ", labels.Select(l => $"L{l.GetHashCode()}").ToArray());

                case LocalBuilder localBuilder:
                    return $"{localBuilder.LocalIndex} ({localBuilder.LocalType})";

                case string s:
                    return s.ToLiteral();

                default:
                    return argument.ToString().Trim();
            }
        }

        public static void RecordOriginalCode(this List<CodeInstruction> il, [CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMemberName = "")
        {
#if DEBUG
            RecordCode(il, callerFilePath, callerMemberName, "original");
#endif
        }

        public static void RecordPatchedCode(this List<CodeInstruction> il, [CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMemberName = "")
        {
#if DEBUG
            RecordCode(il, callerFilePath, callerMemberName, "patched");
#endif
        }

        private static void RecordCode(List<CodeInstruction> il, string callerFilePath, string callerMemberName, string suffix)
        {
            if (!File.Exists(callerFilePath))
                return;

            var text = il.FormatCode();

            Debug.Assert(callerFilePath.Length > 0);
            var dir = Path.GetDirectoryName(callerFilePath);
            Debug.Assert(dir != null);

            const string expectedMemberNameSuffix = "Transpiler";
            Debug.Assert(callerMemberName.Length > expectedMemberNameSuffix.Length);
            Debug.Assert(callerMemberName.EndsWith(expectedMemberNameSuffix));
            var name = callerMemberName.Substring(0, callerMemberName.Length - expectedMemberNameSuffix.Length);

            var path = Path.Combine(dir, $"{name}.{suffix}.il");

            File.WriteAllText(path, text);
        }

        public static CodeInstruction DeepClone(this CodeInstruction ci)
        {
            var clone = ci.Clone();
            clone.labels = ci.labels.ToList();
            clone.blocks = ci.blocks.Select(b => new ExceptionBlock(b.blockType, b.catchType)).ToList();
            return clone;
        }

        public static List<CodeInstruction> DeepClone(this IEnumerable<CodeInstruction> il)
        {
            return il.Select(ci => ci.DeepClone()).ToList();
        }

#if NEEDS_MORE_TESTING_LINQ_DOES_NOT_WORK
        public static int InsertCodeFromMethod(this List<CodeInstruction> il, ILGenerator gen, MethodInfo targetMethod, int beforeIndex, MethodInfo sourceMethod, LocalVariableInfo[] argMap, Dictionary<string, Type> typeMap)
        {
            var code = PatchProcessor.GetCurrentInstructions(sourceMethod).DeepClone();

            Debug.Assert(code.Last().opcode == OpCodes.Ret);
            code.Last().opcode = OpCodes.Nop;

            var labelMap = new Dictionary<Label, Label>();
            var localMap = new Dictionary<int, LocalBuilder>();

            foreach (var ci in code)
            {
                ci.labels = ci.labels.Select(original => labelMap.TryGetValue(original, out var mapped) ? mapped : labelMap[original] = gen.DefineLabel()).ToList();

                if (ci.operand is Type typeOperand)
                {
                    // If the type's name is in our map, replace the operand with the new type.
                    if (typeMap.TryGetValue(typeOperand.Name, out Type newType))
                    {
                        ci.operand = newType;
                    }
                }

                var sourceVariables = sourceMethod.GetMethodBody()?.LocalVariables;
                Debug.Assert(sourceVariables != null);

                var localVarIndex = ci.GetLocalVarIndex();
                if (localVarIndex >= 0)
                {
                    if (!localMap.TryGetValue(localVarIndex, out var localVariable))
                    {
                        var type = sourceVariables.First(v => v.LocalIndex == localVarIndex).LocalType
                                   ?? throw new Exception($"Cannot find local variable {localVarIndex} in the source method");
                        type = typeMap.GetValueOrDefault(type.Name, type);
                        localVariable = gen.DeclareLocal(type);
                        localMap[localVarIndex] = localVariable;
                    }

                    ReplaceWithLocalVar(ci, localVariable);
                }

                var argIndex = ci.GetArgumentIndex();
                if (argIndex >= 0)
                {
                    var targetVariable = argMap[argIndex];
                    ReplaceWithLocalVar(ci, targetVariable);
                }
            }

            foreach (var ci in code)
            {
                if (ci.operand is Label original)
                {
                    if (!labelMap.TryGetValue(original, out var mapped))
                    {
                        throw new Exception($"Could not map label: {original}");
                    }

                    ci.operand = mapped;
                }
            }

            il.InsertRange(beforeIndex, code);
            return beforeIndex + code.Count;
        }

        private static void ReplaceWithLocalVar(CodeInstruction ci, LocalVariableInfo varInfo)
        {
            var i = varInfo.LocalIndex;
            object box = i < 256 ? (object)(byte)i : (object)(short)i;

            switch (ci.opcode.Name)
            {
                case "ldloc.0":
                case "ldarg.0":
                case "ldloc.1":
                case "ldarg.1":
                case "ldloc.2":
                case "ldarg.2":
                case "ldloc.3":
                case "ldarg.3":
                case "ldloc":
                case "ldarg":
                case "ldloc.s":
                case "ldarg.s":
                    switch (i)
                    {
                        case 0:
                            ci.opcode = OpCodes.Ldloc_0;
                            ci.operand = null;
                            break;
                        case 1:
                            ci.opcode = OpCodes.Ldloc_1;
                            ci.operand = null;
                            break;
                        case 2:
                            ci.opcode = OpCodes.Ldloc_2;
                            ci.operand = null;
                            break;
                        case 3:
                            ci.opcode = OpCodes.Ldloc_3;
                            ci.operand = null;
                            break;
                        default:
                            ci.opcode = i < 256 ? OpCodes.Ldloc_S : OpCodes.Ldloc;
                            ci.operand = box;
                            break;
                    }

                    break;

                case "ldloca":
                case "ldarga":
                case "ldloca.s":
                case "ldarga.s":
                    ci.opcode = i < 256 ? OpCodes.Ldloca_S : OpCodes.Ldloca;
                    ci.operand = box;
                    break;

                case "stloc.0":
                case "starg.0":
                case "stloc.1":
                case "starg.1":
                case "stloc.2":
                case "starg.2":
                case "stloc.3":
                case "starg.3":
                case "stloc":
                case "starg":
                case "stloc.s":
                case "starg.s":
                    switch (i)
                    {
                        case 0:
                            ci.opcode = OpCodes.Stloc_0;
                            ci.operand = null;
                            break;
                        case 1:
                            ci.opcode = OpCodes.Stloc_1;
                            ci.operand = null;
                            break;
                        case 2:
                            ci.opcode = OpCodes.Stloc_2;
                            ci.operand = null;
                            break;
                        case 3:
                            ci.opcode = OpCodes.Stloc_3;
                            ci.operand = null;
                            break;
                        default:
                            ci.opcode = i < 256 ? OpCodes.Stloc_S : OpCodes.Stloc;
                            ci.operand = box;
                            break;
                    }

                    break;
            }
        }

        public static int GetLocalVarIndex(this CodeInstruction ci)
        {
            switch (ci.opcode.Name)
            {
                case "ldloc.0":
                case "stloc.0":
                    return 0;
                case "ldloc.1":
                case "stloc.1":
                    return 1;
                case "ldloc.2":
                case "stloc.2":
                    return 2;
                case "ldloc.3":
                case "stloc.3":
                    return 3;
                case "ldloc.s":
                case "stloc.s":
                case "ldloca.s":
                case "ldloc":
                case "stloc":
                case "ldloca":
                    if (ci.operand is LocalBuilder lb)
                        return lb.LocalIndex;
                    if (ci.operand is byte b)
                        return b;
                    if (ci.operand is short s)
                        return s;
                    break;
            }

            return -1;
        }

        public static int GetArgumentIndex(this CodeInstruction ci)
        {
            switch (ci.opcode.Name)
            {
                case "ldarg.0":
                case "starg.0":
                    return 0;
                case "ldarg.1":
                case "starg.1":
                    return 1;
                case "ldarg.2":
                case "starg.2":
                    return 2;
                case "ldarg.3":
                case "starg.3":
                    return 3;
                case "ldarg.s":
                case "starg.s":
                case "ldarga.s":
                case "ldarg":
                case "starg":
                case "ldarga":
                    if (ci.operand is LocalBuilder lb)
                        return lb.LocalIndex;
                    if (ci.operand is byte b)
                        return b;
                    if (ci.operand is short s)
                        return s;
                    break;
            }

            return -1;
        }
#endif

#if DOES_NOT_WORK
        public static int VerifyCallStack(this IEnumerable<CodeInstruction> il)
        {
            var depth = 0;

            foreach (var ci in il)
            {
                // Handle the push behavior
                switch (ci.opcode.StackBehaviourPush)
                {
                    case StackBehaviour.Push0:
                        break;
                    case StackBehaviour.Push1:
                    case StackBehaviour.Pushi:
                    case StackBehaviour.Pushr8:
                    case StackBehaviour.Pushi8:
                    case StackBehaviour.Pushr4:
                    case StackBehaviour.Pushref:
                        depth++;
                        break;
                    case StackBehaviour.Push1_push1:
                        depth += 2;
                        break;
                    case StackBehaviour.Varpush:
                        // FIXME: Varpush would require more intricate handling based on the specific method call
                        // This is a simplification
                        // depth += 2;
                        break;
                    default:
                        throw new InvalidOperationException($"Unexpected push behavior: {ci.opcode.StackBehaviourPush}");
                }

                // Handle the pop behavior
                switch (ci.opcode.StackBehaviourPop)
                {
                    case StackBehaviour.Pop0:
                        break;
                    case StackBehaviour.Pop1:
                    case StackBehaviour.Popi:
                    case StackBehaviour.Popref:
                        depth--;
                        break;
                    case StackBehaviour.Pop1_pop1:
                    case StackBehaviour.Popi_pop1:
                    case StackBehaviour.Popi_popi:
                    case StackBehaviour.Popi_popi8:
                    case StackBehaviour.Popi_popr4:
                    case StackBehaviour.Popi_popr8:
                    case StackBehaviour.Popref_pop1:
                    case StackBehaviour.Popref_popi:
                        depth -= 2;
                        break;
                    case StackBehaviour.Popi_popi_popi:
                    case StackBehaviour.Popref_popi_pop1:
                    case StackBehaviour.Popref_popi_popi:
                    case StackBehaviour.Popref_popi_popi8:
                    case StackBehaviour.Popref_popi_popr4:
                    case StackBehaviour.Popref_popi_popr8:
                    case StackBehaviour.Popref_popi_popref:
                        depth -= 3;
                        break;
                    case StackBehaviour.Varpop:
                        // FIXME: Varpop would require more intricate handling based on the specific method call
                        // This is a simplification
                        // depth -= 2;
                        break;
                    default:
                        throw new InvalidOperationException($"Unexpected pop behavior: {ci.opcode.StackBehaviourPop}");
                }

                if (depth < 0)
                {
                    // The stack went negative at some point, which is an error
                    break;
                }
            }

            // The stack should be balanced (back to 0) at the end of a method
            return depth;
        }
#endif
    }

    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public class CodeInstructionNotFound : Exception
    {
        public CodeInstructionNotFound()
        {
        }

        public CodeInstructionNotFound(string message)
            : base(message)
        {
        }

        public CodeInstructionNotFound(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}