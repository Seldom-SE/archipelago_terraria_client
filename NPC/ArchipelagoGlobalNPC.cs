using SeldomArchipelago.Systems;
using Terraria;
using Terraria.ModLoader;

namespace SeldomArchipelago
{
    [ExtendsFromMod("CalamityMod")]
    public class ArchipelagoGlobalNPC : GlobalNPC
    {
        public override void OnKill(NPC npc)
        {
            if (ModLoader.HasMod("CalamityMod")) CalamityOnKill(npc.type);
        }

        void CalamityOnKill(int npc)
        {
            if (npc == ModContent.NPCType<CalamityMod.NPCs.AdultEidolonWyrm.AdultEidolonWyrmHead>()) ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Adult Eidolon Wyrm");
        }
    }
}