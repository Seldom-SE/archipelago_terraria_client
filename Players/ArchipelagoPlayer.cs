using SeldomArchipelago.Systems;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SeldomArchipelago.Players
{
    public class ArchipelagoPlayer : ModPlayer
    {
        public override void OnEnterWorld(Player player)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                var mod = ModContent.GetInstance<SeldomArchipelago>();

                if (mod == null) return;

                var packet = mod.GetPacket();
                packet.Write("");
                packet.Send();

                return;
            }

            ArchipelagoSystem.Chat(ArchipelagoSystem.Status(), player.whoAmI);
        }
    }
}
