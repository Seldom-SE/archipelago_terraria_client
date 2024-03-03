using SeldomArchipelago.Systems;
using Terraria.ModLoader;

namespace SeldomArchipelago.Command
{
    public class ApSetFlagCommand : ModCommand
    {
        public override string Command => "apcollect";
        public override CommandType Type => CommandType.World;
        public override string Description => "Collects an item without telling the Archipelago server. This is a cheat.";

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            if (args.Length == 0)
            {
                caller.Reply("Error: No arguments");
                return;
            }

            var item = args[0];
            for (var i = 1; i < args.Length; i++) item += " " + args[i];

            ModContent.GetInstance<ArchipelagoSystem>().Collect(item);
        }
    }
}