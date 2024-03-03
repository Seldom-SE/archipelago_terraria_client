using SeldomArchipelago.Systems;
using Terraria.ModLoader;

namespace SeldomArchipelago.Command
{
    public class ApConnectCommand : ModCommand
    {
        public override string Command => "apconnect";
        public override CommandType Type => CommandType.World;
        public override string Description => "Attempts to connect to Archipelago";

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            if (args.Length > 0)
            {
                caller.Reply("Error: Unexpected arguments");
                return;
            }

            var archipelagoSystem = ModContent.GetInstance<ArchipelagoSystem>();
            archipelagoSystem.Reset();
            archipelagoSystem.OnWorldLoad();

            archipelagoSystem.Chat(archipelagoSystem.Status());
        }
    }
}