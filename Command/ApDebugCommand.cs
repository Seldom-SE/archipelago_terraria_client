using SeldomArchipelago.Systems;
using Terraria.ModLoader;

namespace SeldomArchipelago.Command
{
    public class ApDebugCommand : ModCommand
    {
        public override string Command => "apdebug";
        public override CommandType Type => CommandType.World;
        public override string Description => "Dumps debug information";

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            foreach (var line in ModContent.GetInstance<ArchipelagoSystem>().DebugInfo())
            {
                caller.Reply(line);
                Mod.Logger.Info(line);
            }

            caller.Reply("You can copy this message from the logs");
            caller.Reply("See client.log and server.log in Steam/steamapps/common/tModLoader/tModLoader-Logs");
        }
    }
}