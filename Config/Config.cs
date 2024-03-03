using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace SeldomArchipelago.Config
{
    public class Config : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ServerSide;

        [Header("Common")]

        const string SADisplayName = "$Mods.SeldomArchipelago.Configs.ModConfig.DisplayName";
        [LabelKey(SADisplayName)]
        [DefaultValue("")]
        public string name;

        const string SAPort = "$Mods.SeldomArchipelago.Configs.ModConfig.Port";
        [LabelKey(SAPort)]
        [Range(0, 65535)]
        [DefaultValue(38281)]
        public int port;

        [Header("Advanced")]

        const string SAHost = "$Mods.SeldomArchipelago.Configs.ModConfig.Host";
        [LabelKey(SAHost)]
        [DefaultValue("archipelago.gg")]
        public string address;

        const string SAPassword = "$Mods.SeldomArchipelago.Configs.ModConfig.Password";
        [LabelKey(SAPassword)]
        [DefaultValue("")]
        public string password;
    }
}