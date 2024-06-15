using Terraria;
using Terraria.GameContent.Achievements;
using Terraria.ID;
using Terraria.ModLoader;

namespace SeldomArchipelago.Tile
{
    public class ArchipelagoGlobalTile : GlobalTile
    {
        public override void KillTile(int i, int j, int type, ref bool fail, ref bool effectOnly, ref bool noItem)
        {
            base.KillTile(i, j, type, ref fail, ref effectOnly, ref noItem);

            // Calamity's ore progression rework prevents the code that completes the "Begone, Evil!" achievement from running, so we complete it manually
            if (Main.LocalPlayer != null && type == TileID.DemonAltar && !fail) AchievementsHelper.NotifyProgressionEvent(6);
        }
    }
}
