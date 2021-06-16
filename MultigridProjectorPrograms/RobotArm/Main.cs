using System;
using System.Collections.Generic;
using System.Linq;
using VRageMath;
using Sandbox.ModAPI.Ingame;
using VRage.Game.GUI.TextPanel;

namespace MultigridProjectorPrograms.RobotArm
{
    class Program : MyGridProgram
    {
        private static IMyTextPanel lcdTimer;
        private static IMyTextPanel lcdDetails;
        private static IMyTextPanel lcdStatus;
        private static IMyTextPanel lcdLog;
        private readonly Shipyard shipyard;
        private readonly RotorReverser rotorReverser;

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;

            PrepareDisplay();
            FindTextPanels();

            Util.ClearLog();
            try
            {
                var mgp = new MultigridProjectorProgrammableBlockAgent(Me);
                var projector = GridTerminalSystem.GetBlockWithName(Cfg.ProjectorName) as IMyProjector;
                shipyard = new Shipyard(GridTerminalSystem, projector, mgp);

                var projectorRotor = GridTerminalSystem.GetBlockWithName(Cfg.ProjectorRotorName) as IMyMotorStator;
                rotorReverser = new RotorReverser(projectorRotor);
                rotorReverser.OnReverse += shipyard.RetractAll;
            }
            catch (Exception e)
            {
                Util.Log(e.ToString());
                throw;
            }
            finally
            {
                Util.ShowLog(lcdLog);
            }
        }

        private void PrepareDisplay()
        {
            var pbSurface = Me.GetSurface(0);
            pbSurface.ContentType = ContentType.TEXT_AND_IMAGE;
            pbSurface.Alignment = TextAlignment.CENTER;
            pbSurface.FontColor = Color.DarkGreen;
            pbSurface.Font = "DEBUG";
            pbSurface.FontSize = 3f;
            pbSurface.WriteText("Robotic Arm\r\nController");
        }

        private void FindTextPanels()
        {
            var lcdGroup = GridTerminalSystem.GetBlockGroupWithName(Cfg.TextPanelsGroupName);
            var textPanels = new List<IMyTextPanel>();

            lcdGroup.GetBlocksOfType(textPanels);

            foreach (var textPanel in textPanels)
            {
                textPanel.ContentType = ContentType.TEXT_AND_IMAGE;
                textPanel.Alignment = TextAlignment.LEFT;
                textPanel.FontColor = Color.Cyan;
                textPanel.Font = "DEBUG";
                textPanel.FontSize = 1.2f;
                textPanel.WriteText("");
            }

            lcdTimer = textPanels.FirstOrDefault(p => p.CustomName.Contains("Timer"));
            lcdDetails = textPanels.FirstOrDefault(p => p.CustomName.Contains("Details"));
            lcdStatus = textPanels.FirstOrDefault(p => p.CustomName.Contains("Status"));
            lcdLog = textPanels.FirstOrDefault(p => p.CustomName.Contains("Log"));

            if (lcdTimer != null)
            {
                lcdTimer.Font = "Monospace";
                lcdTimer.FontSize = 4f;
                lcdTimer.Alignment = TextAlignment.CENTER;
                lcdTimer.TextPadding = 10;
            }

            if (lcdStatus != null)
            {
                lcdStatus.Font = "Monospace";
                lcdStatus.FontSize = 0.8f;
                lcdStatus.TextPadding = 0;
            }
        }

        public void Main(string argument, UpdateType updateSource)
        {
            Util.ClearLog();
            try
            {
                try
                {
                    if (((int) updateSource & (int) UpdateType.Update10) > 0)
                    {
                        shipyard.Update(lcdDetails, lcdStatus, lcdTimer);
                        rotorReverser.Update();
                        // MechanicalConnectorTests();
                    }
                }
                catch (Exception e)
                {
                    Util.Log(e.ToString());
                    shipyard.Stop();
                    throw;
                }
            }
            finally
            {
                Util.ShowLog(lcdLog);
            }
        }

        public void Save()
        {
            shipyard.Stop();
        }
    }
}