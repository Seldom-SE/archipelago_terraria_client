using SeldomArchipelago.Systems;
using Terraria;
using Terraria.ModLoader;

namespace SeldomArchipelago.NPCs
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
            if (npc == ModContent.NPCType<CalamityMod.NPCs.PrimordialWyrm.PrimordialWyrmHead>()) ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Primordial Wyrm");
        }
    }
}