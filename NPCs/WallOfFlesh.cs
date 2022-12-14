using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SeldomArchipelago.NPCs
{
    public class Boss : GlobalNPC
    {
        public override bool AppliesToEntity(NPC npc, bool lateInstantiation) => npc.type == NPCID.WallofFlesh;
        public override void OnKill(NPC npc)
        {
            System.Archipelago.CompleteLocation("Wall of Flesh");
        }
    }
}
