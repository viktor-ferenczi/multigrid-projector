using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using HarmonyLib;
using MultigridProjector.Utilities;


namespace MultigridProjectorServer
{
    [AttributeUsage(AttributeTargets.Class)]
    public class EnsureOriginalTorch : Attribute
    {
        private readonly string[] _allowedHexDigests;
        private readonly Type _declaringType;
        private readonly string _methodName;
        private readonly Type[] _methodArgs;
        private string AllowedHexDigestsAsText => string.Join(", ", _allowedHexDigests);

        public EnsureOriginalTorch(Type declaringType, string methodName, Type[] methodArgs, params string[] allowedHexDigests)
        {
            _allowedHexDigests = allowedHexDigests;
            _declaringType = declaringType;
            _methodName = methodName;
            _methodArgs = methodArgs;
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
            var ensureOriginal = patchType.GetCustomAttributes<EnsureOriginalTorch>().FirstOrDefault();
            if (ensureOriginal == null)
                return "";

            var declaringType = ensureOriginal._declaringType;
            var methodName = ensureOriginal._methodName;
            var methodArgs = ensureOriginal._methodArgs;
            var methodInfo = AccessTools.DeclaredMethod(declaringType, methodName, methodArgs);
            if (methodInfo == null)
                return $"Could not get method information for the {declaringType.Name}.{methodName} method patched in class {patchType.Name}";

            try
            {
                var actualDigest = HashMethodBody(methodInfo).ToString("x8");
                if (!ensureOriginal._allowedHexDigests.Contains(actualDigest))
                    return $"Body of patched method {declaringType.Name}.{methodName} has changed: actual {actualDigest}, expected one of {ensureOriginal.AllowedHexDigestsAsText}\"";
            }
            catch (TargetInvocationException e)
            {
                // Silencing rare exception
                PluginLog.Error(e,$"Failed to verify body of method {declaringType.Name}.{methodName}");
            }

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