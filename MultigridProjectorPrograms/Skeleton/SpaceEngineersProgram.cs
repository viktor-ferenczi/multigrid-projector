using System;
using Sandbox.ModAPI.Ingame;
using IMyGridTerminalSystem = Sandbox.ModAPI.Ingame.IMyGridTerminalSystem;
using IMyProgrammableBlock = Sandbox.ModAPI.Ingame.IMyProgrammableBlock;
using IMyGridProgram = Sandbox.ModAPI.IMyGridProgram;

namespace MultigridProjectorPrograms.Skeleton
{
    public class SpaceEngineersProgram : IMyGridProgram
    {
        public SpaceEngineersProgram()
        {
            // The constructor, called only once every session and
            // always before any other method is called. Use it to
            // initialize your script.
            //
            // The constructor is optional and can be removed if not
            // needed.
            //
            // It's recommended to set RuntimeInfo.UpdateFrequency
            // here, which will allow your script to run itself without a
            // timer block.
        }

        public void Save()
        {
            // Called when the program needs to save its state. Use
            // this method to save your state to the Storage field
            // or some other means.
            //
            // This method is optional and can be removed if not
            // needed.
        }

        public Func<IMyIntergridCommunicationSystem> IGC_ContextGetter { get; set; }
        public IMyGridTerminalSystem GridTerminalSystem { get; set; }
        public IMyProgrammableBlock Me { get; set; }
        public TimeSpan ElapsedTime { get; set; }
        public string Storage { get; set; }
        public IMyGridProgramRuntimeInfo Runtime { get; set; }
        public Action<string> Echo { get; set; }
        public bool HasMainMethod { get; }
        public bool HasSaveMethod { get; }

        public void Main(string argument)
        {
            throw new NotImplementedException();
        }

        public void Main(string argument, UpdateType updateSource)
        {
            // The main entry point of the script, invoked every time
            // one of the programmable block's Run actions are invoked,
            // or the script updates itself. The updateSource argument
            // describes where the update came from.
            //
            // The method itself is required, but the arguments above
            // can be removed if not needed.
        }
    }
}