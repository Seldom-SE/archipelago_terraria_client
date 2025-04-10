using SeldomArchipelago.Systems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using static SeldomArchipelago.Systems.ArchipelagoSystem;

namespace SeldomArchipelago.HardmodeItem
{
    public class HardmodeStarter : ModItem
    {
        public override void SetDefaults()
        {
            Item.CloneDefaults(ItemID.DemonHeart);
        }

        public override bool CanUseItem(Player player) => !Main.hardMode;

        public override bool? UseItem(Player player)
        {
            ActivateHardmode();
            return true;
        }
    }
}
