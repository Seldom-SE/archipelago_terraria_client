using SeldomArchipelago.Systems;
using Terraria.ModLoader;

namespace SeldomArchipelago.Command
{
    public class ApFlagstartCommand : ModCommand
    {
        public override string Command => "apflagstart";
        public override CommandType Type => CommandType.World;
        public override string Description => "Trigger a manual flag (as set in config) or list available flags to trigger";

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            if (args.Length == 0)
            {
                caller.Reply("Error: No arguments");
                return;
            }

            var archipelagoSystem = ModContent.GetInstance<ArchipelagoSystem>();

            var command = args[0];
            if (command == "list")
            {
                string reply = "FLAGS AWAITING TRIGGER:";
                if (archipelagoSystem.world.suspendedFlags.Count == 0)
                {
                    reply = "There are no flags awaiting manual trigger";
                }
                else
                {
                    foreach (string flag in archipelagoSystem.world.suspendedFlags) reply += $"\n{flag}";
                }
                caller.Reply(reply);
                return;
            }
            for (var i = 1; i < args.Length; i++) command += " " + args[i];

            bool flagFound = ArchipelagoSystem.FindFlag(command, out string fuzzy);
            if (!flagFound)
            {
                caller.Reply($"Error: Flag {command} not found");
                return;
            }
            // Because commands automatically convert to lowercase, all non-failure finds will be fuzzy
            if (!fuzzy.ToLower().Equals(command))
            {
                caller.Reply($"Error: Flag {command} not found, did you mean to type {fuzzy}?");
                return;
            }
            command = fuzzy;
            if (archipelagoSystem.CheckFlag(command))
            {
                caller.Reply($"Error: Flag {command} is already active in this world");
                return;
            }
            if (!archipelagoSystem.world.suspendedFlags.Contains(command))
            {
                caller.Reply($"Error: Flag {command} has not been received yet");
                return;
            }

            archipelagoSystem.Collect(command, true);
            caller.Reply($"Successfully triggered flag {command}");
        }
    }
}