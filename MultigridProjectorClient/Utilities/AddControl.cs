using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Gui;
using System;
using System.Collections.Generic;

namespace MultigridProjectorClient.Utilities
{
    internal static class AddControl
    {
        public static bool AddControlAfter<TBlock>(string id, MyTerminalControl<TBlock> control) where TBlock : MyTerminalBlock
        {
            int index = GetControlIndex(typeof(TBlock), id);

            if (index == -1)
                return false;

            MyTerminalControlFactory.AddControl(index+1, control);

            return true;
        }

        public static bool AddControlBefore<TBlock>(string id, MyTerminalControl<TBlock> control) where TBlock : MyTerminalBlock
        {
            int index = GetControlIndex(typeof(TBlock), id);

            if (index == -1)
                return false;

            MyTerminalControlFactory.AddControl(index, control);

            return true;
        }

        private static int GetControlIndex(Type blockType, string id)
        {
            List<ITerminalControl> controls = new List<ITerminalControl>();
            MyTerminalControlFactory.GetControls(blockType, controls);

            for (int i = 0; i < controls.Count; i++)
            {
                if (controls[i].Id == id)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
