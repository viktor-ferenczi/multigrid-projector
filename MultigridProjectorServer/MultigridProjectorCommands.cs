using Torch.Commands;
using Torch.Commands.Permissions;
using VRage.Game.ModAPI;

namespace MultigridProjectorServer
{
    // [Category("multigrid")]
    // public class MultigridProjectorCommands : CommandModule
    // {
    //     [Command("info", "Prints the current settings")]
    //     [Permission(MyPromoteLevel.None)]
    //     public void Info()
    //     {
    //         if (!MultigridProjectorConfig.Instance.EnablePlugin)
    //         {
    //             Context.Respond("Multigrid Projector plugin is disabled");
    //             return;
    //         }
    //
    //         var identityId = Context.Player.IdentityId;
    //         if (identityId == 0L)
    //         {
    //             Context.Respond("This command can only be used in game");
    //             return;
    //         }
    //
    //         RespondWithInfo();
    //     }
    //
    //     [Command("block limit", "Enables or disables enforcing player block limits")]
    //     [Permission(MyPromoteLevel.Admin)]
    //     public void BlockLimit(bool enable)
    //     {
    //         MultigridProjectorConfig.Instance.BlockLimit = enable;
    //         RespondWithInfo();
    //     }
    //
    //     [Command("block limit", "Enables or disables enforcing player block limits")]
    //     [Permission(MyPromoteLevel.Admin)]
    //     public void PcuLimit(bool enable)
    //     {
    //         MultigridProjectorConfig.Instance.PcuLimit = enable;
    //         RespondWithInfo();
    //     }
    //
    //     private void RespondWithInfo()
    //     {
    //         var config = MultigridProjectorConfig.Instance;
    //         Context.Respond("Multigrid Projector:\r\n" +
    //                         $"Block limit: {FormatBool(config.BlockLimit)}\r\n" +
    //                         $"PCU limit: {FormatBool(config.PcuLimit)}");
    //     }
    //
    //     private static string FormatBool(bool value)
    //     {
    //         return value ? "Yes" : "No";
    //     }
    // }
}