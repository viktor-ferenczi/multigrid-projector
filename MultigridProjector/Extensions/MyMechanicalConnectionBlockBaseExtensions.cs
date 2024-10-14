using System.Reflection;
using HarmonyLib;
using MultigridProjector.Utilities;
using Sandbox.Definitions;
using Sandbox.Game.Entities.Blocks;

namespace MultigridProjector.Extensions
{
    public static class MyMechanicalConnectionBlockBaseExtensions
    {
        private static readonly MethodInfo CallAttachMethodInfo = Validation.EnsureInfo(AccessTools.DeclaredMethod(typeof(MyMechanicalConnectionBlockBase), "CallAttach"));
        public static void CallAttach(this MyMechanicalConnectionBlockBase obj)
        {
            CallAttachMethodInfo.Invoke(obj, new object[] {});
        }

        private static readonly MethodInfo AttachMethodInfo = Validation.EnsureInfo(AccessTools.DeclaredMethod(typeof(MyMechanicalConnectionBlockBase), "Attach", new []{typeof(MyAttachableTopBlockBase), typeof(bool)}));
        public static void Attach(this MyMechanicalConnectionBlockBase obj, MyAttachableTopBlockBase topBlock, bool updateGroup = true)
        {
            AttachMethodInfo.Invoke(obj, new object[] {topBlock, updateGroup});
        }

        private static readonly MethodInfo CreateTopPartMethodInfo = Validation.EnsureInfo(AccessTools.DeclaredMethod(typeof(MyMechanicalConnectionBlockBase), "CreateTopPart"));
        public static MyAttachableTopBlockBase CreateTopPart(this MyMechanicalConnectionBlockBase baseBlock, MyCubeBlockDefinitionGroup definitionGroup, MyMechanicalConnectionBlockBase.MyTopBlockSize topSize, bool instantBuild)
        {
            var args = new object[] {null, baseBlock.BuiltBy, definitionGroup, topSize, instantBuild};
            CreateTopPartMethodInfo.Invoke(baseBlock, args);
            var topBlock = (MyAttachableTopBlockBase) args[0];
            return topBlock;
        }
    }
}