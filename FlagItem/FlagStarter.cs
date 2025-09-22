using SeldomArchipelago.Systems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                if (flagName is null)
                {
                    flagName = value;
                    colorHash = BitConverter.GetBytes(flagName.GetHashCode());
                }
                else
                {
                    throw new Exception($"Tried to set pre-existing FlagStarter item of type {flagName} to {value}");
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
        public override void SetDefaults()
        {
            Item.CloneDefaults(ItemID.DemonHeart);
        }

        public override bool? UseItem(Player player)
        {
            ModContent.GetInstance<ArchipelagoSystem>().Collect(flagName, true);
            return true;
        }
        public override void LoadData(TagCompound tag) => FlagName = tag.GetString(nameof(flagName));
        public override void SaveData(TagCompound tag) => tag[nameof(flagName)] = flagName;
    }
}
