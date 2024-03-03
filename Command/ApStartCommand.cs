using SeldomArchipelago.Systems;
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

            var archipelagoSystem = ModContent.GetInstance<ArchipelagoSystem>();
            if (archipelagoSystem.Enable())
                archipelagoSystem.Chat("Enabled Archipelago");
            else
                archipelagoSystem.Chat("Could not enable Archipelago since you are not connected");
        }
    }
}