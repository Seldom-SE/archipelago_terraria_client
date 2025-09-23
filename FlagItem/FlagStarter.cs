using SeldomArchipelago.Systems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

using Microsoft.Xna.Framework;

using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using static SeldomArchipelago.Systems.ArchipelagoSystem;

namespace SeldomArchipelago.FlagItem
{
    public class FlagStarter : ModItem
    {
        string? flagName = null;
        // Visually these flag items should be differentiated.
        // Instead of assigning colors to each item manually or completely randomizing them, we can base it off of the flagName's hash.
        byte[] colorHash = null;
        public string FlagName
        {
            get { return flagName; }
            set
            {
                flagName = value;
                if (value is null)
                {
                    Item.SetNameOverride($"Inert Starter");
                    colorHash = [0, 20, 20, 20];
                    return;
                }
                Item.SetNameOverride($"{value} Starter");
                using (SHA256 hash = SHA256.Create())
                {
                    colorHash = hash.ComputeHash(Encoding.UTF8.GetBytes(value));
                }
            }
        }
        public override Microsoft.Xna.Framework.Color? GetAlpha(Microsoft.Xna.Framework.Color lightColor)
        {
            lightColor.R = colorHash[1];
            lightColor.G = colorHash[2];
            lightColor.B = colorHash[3];
            return lightColor;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            if (flagName == null)
            {
                tooltips.Add(new TooltipLine(Mod, "Tooltip0", "\"Missed its window\""));
            }
        }
        public override void SetDefaults()
        {
            Item.CloneDefaults(ItemID.DemonHeart);
        }
        public override bool CanStack(Item source) => false;
        public override bool? UseItem(Player player)
        {
            if (flagName is null) return true;
            ModContent.GetInstance<ArchipelagoSystem>().Collect(flagName, true);
            return true;
        }
        public override void UpdateInventory(Player player)
        {
            if (flagName is not null && ModContent.GetInstance<ArchipelagoSystem>().CheckFlag(flagName))
            {
                FlagName = null;
            }
        }
        public override void LoadData(TagCompound tag) => FlagName = tag.GetString(nameof(flagName));
        public override void SaveData(TagCompound tag) => tag[nameof(flagName)] = flagName;
    }
}
