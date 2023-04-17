using SeldomArchipelago.Systems;
using Terraria.ModLoader;

namespace SeldomArchipelago.Command
{
    public class ApCommand : ModCommand
    {
        public override string Command => "ap";
        public override CommandType Type => CommandType.World;
        public override string Description => "Sends a command to Archipelago";

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            if (args.Length == 0)
            {
                caller.Reply("Error: No arguments");
                return;
            }

            var command = args[0];
            for (var i = 1; i < args.Length; i++) command += " " + args[i];

            var archipelagoSystem = ModContent.GetInstance<ArchipelagoSystem>();

            if (!archipelagoSystem.SendCommand(command)) caller.Reply("Error: Could not send command");
        }
    }
}