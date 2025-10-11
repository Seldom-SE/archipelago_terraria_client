using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.Serialization;
using SeldomArchipelago.Systems;
using Terraria.ModLoader.Config;
using static SeldomArchipelago.Systems.ArchipelagoSystem;

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
        public List<string> manualFlags = ["Hardmode", "Post-Moon Lord"];

        [OnDeserialized]
        internal void CheckItems(StreamingContext _)
        {
            string[] flags = ArchipelagoSystem.flags;
            if (manualFlags is null) return;
            int counter = 0;
            HashSet<string> registeredFlags = new();
            string[] lowercaseFlags = (from x in flags select x.ToLower()).ToArray();

            while (counter < manualFlags.Count)
            {
                string item = manualFlags[counter];

                bool itemFound = FindFlag(item, out string fuzzy);

                if (!itemFound)
                {
                    manualFlags[counter] = "???";
                    counter++;
                    continue;
                }

                if (registeredFlags.Contains(item))
                {
                    manualFlags.RemoveAt(counter);
                    continue;
                }

                item = fuzzy ?? item;
                manualFlags[counter] = item;
                registeredFlags.Add(item);
                counter++;
            }
        }
    }
}