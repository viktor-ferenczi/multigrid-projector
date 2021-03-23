using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Utils;

// ReSharper disable once CheckNamespace
namespace MultigridProjectorClient
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    // ReSharper disable once UnusedType.Global
    public class EmptyMod : MySessionComponentBase
    {
        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            base.Init(sessionComponent);

            var message = "Please do not add the Multigrid Projector Plugin to your world as a mod. It does not do anything, just makes your world loading slower.";
            MyAPIGateway.Utilities.ShowMessage("Multigrid Projector", message);
            MyLog.Default.WriteLineAndConsole($"Multigrid Projector: {message}");
        }
    }
}