using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using HarmonyLib;

namespace MultigridProjector.Utilities
{
    [AttributeUsage(AttributeTargets.Class)]
    public class EnsureOriginal : Attribute
    {
        public readonly string[] AllowedHexDigests;
        public string AllowedHexDigestsAsText => string.Join(", ", AllowedHexDigests);

        public EnsureOriginal(params string[] allowedHexDigests)
        {
            AllowedHexDigests = allowedHexDigests;
        }

        public static void VerifyAll()
        {
            VerifyAssembly(new StackTrace().GetFrame(1).GetMethod().ReflectedType.Assembly);
        }

        private static void VerifyAssembly(Assembly assembly)
        {
            var errors = new StringBuilder();
            foreach (var type in AccessTools.GetTypesFromAssembly(assembly))
            {
                var error = VerifyType(type);
                if (error.Length == 0) continue;

                errors.Append(error);
                errors.Append("\r\n");
            }

            if (errors.Length == 0)
                return;

            throw new NotSupportedException(
                "Refusing to load the plugin due to potentially incompatible code changes in the game:\r\n"
                + errors + "\r\nPlease reach out to the plugin author for an update.");
        }

        private static string VerifyType(Type patchType)
        {
            var declaringType = patchType.GetCustomAttributes<HarmonyPatch>().FirstOrDefault(a => a.info.declaringType != null)?.info.declaringType;
            if (declaringType == null)
                return "";

            var harmonyAttribute = patchType.GetCustomAttributes<HarmonyPatch>().FirstOrDefault(a => !string.IsNullOrEmpty(a.info.methodName));
            if (harmonyAttribute == null)
                return "";

            var ensureOriginal = patchType.GetCustomAttributes<EnsureOriginal>().FirstOrDefault();
            if (ensureOriginal == null)
                return "";

            var methodName = harmonyAttribute.info.methodName;
            var methodArgs = harmonyAttribute.info.argumentTypes;
            var methodInfo = AccessTools.DeclaredMethod(declaringType, methodName, methodArgs);
            if (methodInfo == null)
                return $"Could not get method information for the {declaringType.Name}.{methodName} method patched in class {patchType.Name}";

            var actualDigest = HashMethodBody(methodInfo).ToString("x8");
            if (!ensureOriginal.AllowedHexDigests.Contains(actualDigest))
                return $"Body of patched method {declaringType.Name}.{methodName} has changed: actual {actualDigest}, expected one of {ensureOriginal.AllowedHexDigestsAsText}\"";

            return "";
        }

        private static int HashMethodBody(MethodInfo methodInfo)
        {
            var code = PatchProcessor.GetCurrentInstructions(methodInfo);
            return Arithmetic.CombineHashCodes(HashInstructions(code));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static IEnumerable<int> HashInstructions(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var instruction in instructions)
            {
                yield return instruction.opcode.GetHashCode();

                if (instruction.operand?.GetType().IsValueType == true)
                    yield return instruction.operand.GetHashCode();

                if (instruction.operand is string)
                    yield return instruction.operand.GetHashCode();

                foreach (var label in instruction.labels)
                    yield return label.GetHashCode();
            }
        }
    }
}