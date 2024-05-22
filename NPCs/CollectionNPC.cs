using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SeldomArchipelago.NPCs
{
    public class CollectionNPC : ModNPC
    {
        public override void SetDefaults()
        {
            NPC.friendly = true;
            NPC.height = 1;
            NPC.width = 1;
            NPC.lifeMax = 1;
            NPC.trapImmune = true;
            NPC.lavaImmune = true;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.behindTiles = true;
            NPC.alpha = 0;
            NPC.hide = true;
            NPC.aiStyle = -1;
        }

        public override bool? CanBeHitByItem(Player player, Item item) => false;
        public override bool? CanBeHitByProjectile(Projectile projectile) => false;

        public override void AI()
        {
            NPC.position = Main.player[(int)NPC.ai[0]].position;

            if (NPC.ai[0] == Main.myPlayer && (Main.LocalPlayer.chest != (int)NPC.ai[1] || Main.LocalPlayer.talkNPC == -1 || Main.npc[Main.LocalPlayer.talkNPC] != NPC))
            {
                NPC.ai[0] = -1;
                var hit = new NPC.HitInfo();
                hit.InstantKill = true;
                NPC.StrikeNPC(hit, false, true);
            }
        }
    }
}