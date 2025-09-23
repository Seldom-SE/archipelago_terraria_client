using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.Serialization;
using Terraria.ModLoader.Config;

namespace SeldomArchipelago.Config
{
    public class Config : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ServerSide;

        [Header("Common")]

        [Label("Name")]
        [DefaultValue("")]
        public string name;

        [Label("Port")]
        [Range(0, 65535)]
        [DefaultValue(38281)]
        public int port;

        [Header("Advanced")]

        [Label("Server Address")]
        [DefaultValue("archipelago.gg")]
        public string address;

        [Label("Password")]
        [DefaultValue("")]
        public string password;

        [Header("Miscellaneous")]

        [Label("Receive Flag As Item")]
        [DefaultValue(null)]
        public List<string> manualFlags;

        [OnDeserialized]
        internal void CheckItems(StreamingContext _)
        {
            if (manualFlags is null) return;
            ImmutableArray<string> postItems = [
                "Post-King Slime",
                "Post-Eye of Cthulhu",
                "Post-Evil Boss",
                "Post-Old One's Army Tier 1",
                "Post-Goblin Army",
                "Post-Queen Bee",
                "Post-Skeletron",
                "Hardmode",
                "Post-Pirate Invasion",
                "Post-Queen Slime",
                "Post-The Twins",
                "Post-Old One's Army Tier 2",
                "Post-The Destroyer",
                "Post-Skeletron Prime",
                "Post-Plantera",
                "Post-Golem",
                "Post-Old One's Army Tier 3",
                "Post-Martian Madness",
                "Post-Duke Fishron",
                "Post-Mourning Wood",
                "Post-Pumpking",
                "Post-Everscream",
                "Post-Santa-NK1",
                "Post-Ice Queen",
                "Post-Frost Legion",
                "Post-Empress of Light",
                "Post-Lunatic Cultist",
                "Post-Lunar Events",
                "Post-Moon Lord",
                "Post-Desert Scourge",
                "Post-Giant Clam",
                "Post-Acid Rain Tier 1",
                "Post-Crabulon",
                "Post-The Hive Mind",
                "Post-The Perforators",
                "Post-The Slime God",
                "Post-Dreadnautilus",
                "Post-Hardmode Giant Clam",
                "Post-Aquatic Scourge",
                "Post-Cragmaw Mire",
                "Post-Acid Rain Tier 2",
                "Post-Brimstone Elemental",
                "Post-Cryogen",
                "Post-Calamitas Clone",
                "Post-Great Sand Shark",
                "Post-Leviathan and Anahita",
                "Post-Astrum Aureus",
                "Post-The Plaguebringer Goliath",
                "Post-Ravager",
                "Post-Astrum Deus",
                "Post-Profaned Guardians",
                "Post-The Dragonfolly",
                "Post-Providence, the Profaned Goddess",
                "Post-Storm Weaver",
                "Post-Ceaseless Void",
                "Post-Signus, Envoy of the Devourer",
                "Post-Polterghast",
                "Post-Mauler",
                "Post-Nuclear Terror",
                "Post-The Old Duke",
                "Post-The Devourer of Gods",
                "Post-Yharon, Dragon of Rebirth",
                "Post-Exo Mechs",
                "Post-Supreme Witch, Calamitas",
                "Post-Primordial Wyrm",
                "Post-Boss Rush",
            ];
            int counter = 0;
            HashSet<string> registeredFlags = new();
            string[] lowercasePostItems = (from x in postItems select x.ToLower()).ToArray();

            while (counter < manualFlags.Count)
            {
                string item = manualFlags[counter];

                if (!postItems.Contains(item))
                {
                    string lowerItem = item.ToLower();
                    int assumedItemIndex = System.Array.FindIndex(lowercasePostItems, x => x.Contains(lowerItem));
                    if (assumedItemIndex > -1)
                    {
                        item = postItems[assumedItemIndex];
                    }
                    else
                    {
                        manualFlags.RemoveAt(counter);
                        continue;
                    }
                       
                }

                if (registeredFlags.Contains(item))
                {
                    manualFlags.RemoveAt(counter);
                    continue;
                }

                manualFlags[counter] = item;
                registeredFlags.Add(item);
                counter++;
            }
        }
    }
}