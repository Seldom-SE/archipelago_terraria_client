using Terraria;
using Terraria.GameContent.Events;
using Terraria.ModLoader;

namespace SeldomArchipelago.Command
{
    public class ApTriggerLunar : ModCommand
    {
        public override string Command => "aptriggerlunar";
        public override CommandType Type => CommandType.World;
        public override string Description => "Triggers the lunar events. This is a cheat.";

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            WorldGen.TriggerLunarApocalypse();
        }
    }
}