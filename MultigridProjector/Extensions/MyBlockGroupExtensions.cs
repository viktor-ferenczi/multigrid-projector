using System.Collections.Generic;
using System.Reflection;
using System.Text;
using HarmonyLib;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems;

namespace MultigridProjector.Extensions
{
    public static class MyBlockGroupExtensions
    {
        public static HashSet<MyTerminalBlock> GetTerminalBlocks(this MyBlockGroup blockGroup)
        {
            var value = AccessTools.Field(typeof(MyBlockGroup), "Blocks").GetValue(blockGroup);
            return (HashSet<MyTerminalBlock>) value;
        }

        private static readonly ConstructorInfo MyBlockGroupConstructor = AccessTools.Constructor(typeof(MyBlockGroup));

        public static MyBlockGroup NewBlockGroup(string name)
        {
            var blockGroup = MyBlockGroupConstructor.Invoke(new object[] { }) as MyBlockGroup;
            // ReSharper disable once PossibleNullReferenceException
            blockGroup.Name = new StringBuilder(name);
            return blockGroup;
        }
    }
}