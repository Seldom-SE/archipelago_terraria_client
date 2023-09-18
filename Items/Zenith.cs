using SeldomArchipelago.Systems;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace SeldomArchipelago.Items
{
    public class Zenith : GlobalItem
    {
        public override bool AppliesToEntity(Item entity, bool lateInstantiation) => entity.type == ItemID.Zenith;

        public override void OnCreated(Item item, ItemCreationContext context)
        {
            ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Zenith");
        }
    }
}
