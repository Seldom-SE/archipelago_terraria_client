using SeldomArchipelago.Systems;
using Terraria;
using Terraria.ModLoader;

namespace SeldomArchipelago.Players
{
    public class ArchipelagoPlayer : ModPlayer
    {
        public override void OnEnterWorld(Player player)
        {
            ArchipelagoSystem.Chat(ArchipelagoSystem.Status(), player);
        }
    }
}
