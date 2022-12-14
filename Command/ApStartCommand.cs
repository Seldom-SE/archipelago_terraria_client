using SeldomArchipelago.System;
using Terraria.ModLoader;

namespace SeldomArchipelago.Command
{
    public class ApStartCommand : ModCommand
    {
        public override string Command => "apstart";
        public override CommandType Type => CommandType.World;
        public override string Description => "Enables Archipelago for this world";

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            if (args.Length > 0)
            {
                caller.Reply("Error: Unexpected arguments");
                return;
            }

            if (System.Archipelago.Enable())
                caller.Reply("Enabled Archipelago");
            else
                caller.Reply("Could not enable Archipelago since you are not connected");
        }
    }
}