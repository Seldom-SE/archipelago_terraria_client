using System.Collections.Generic;
using System.ComponentModel;
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

        [Label("Receive Hardmode As Item")]
        [DefaultValue(false)]
        public bool hardmodeAsItem;

        [Label("String Array Test")]
        [DefaultValue(null)]
        public List<string> arrayTest;

        [OnDeserialized]
        internal void CheckItems(StreamingContext _)
        {
            if (arrayTest.Contains("error"))
            {
                arrayTest.Remove("error");
            }
        }
    }
}