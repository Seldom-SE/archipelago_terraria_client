using SeldomArchipelago.Systems;
using Terraria.ModLoader;

namespace SeldomArchipelago.Command
{
    public class ApFlagsCommand : ModCommand
    {
        public override string Command => "apflags";
        public override CommandType Type => CommandType.World;
        public override string Description => "Lists the set boss/event flags";

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            if (args.Length > 0)
            {
                caller.Reply("Error: Unexpected arguments");
                return;
            }

            var archipelagoSystem = ModContent.GetInstance<ArchipelagoSystem>();

            foreach (var flag in archipelagoSystem.flags)
            {
                if (archipelagoSystem.CheckFlag(flag)) caller.Reply(flag);
            }
        }
    }
}