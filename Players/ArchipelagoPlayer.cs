using SeldomArchipelago.Systems;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.Achievements;
using Terraria.DataStructures;
using Terraria.GameContent.Achievements;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace SeldomArchipelago.Players
{
    public class ArchipelagoPlayer : ModPlayer
    {
        TagCompound achievements = new();

        public override void OnEnterWorld(Player player)
        {
            Main.Achievements.ClearAll();
            var achievements = (Dictionary<string, Achievement>)typeof(AchievementManager).GetField("_achievements", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(Main.Achievements);

            foreach (var achievement in achievements)
            {
                var conditions = (Dictionary<string, AchievementCondition>)typeof(Achievement).GetField("_conditions", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(achievement.Value);
                var serConditions = this.achievements.Get<TagCompound>(achievement.Key);

                foreach (var condition in conditions)
                {
                    var serCondition = serConditions.Get<TagCompound>(condition.Key);
                    if (condition.Value is CustomIntCondition intCondition) intCondition.Value = serCondition.Get<int>("int");
                    if (condition.Value is CustomFloatCondition floatCondition) floatCondition.Value = serCondition.Get<float>("float");
                    if (serCondition.Get<bool>("completed")) condition.Value.Complete();
                }
            }

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                var mod = ModContent.GetInstance<SeldomArchipelago>();

                if (mod == null) return;

                var packet = mod.GetPacket();
                packet.Write("");
                packet.Send();

                return;
            }

            var archipelagoSystem = ModContent.GetInstance<ArchipelagoSystem>();
            archipelagoSystem.Chat(archipelagoSystem.Status(), player.whoAmI);
        }

        public override void SaveData(TagCompound tag)
        {
            if (Main.netMode == NetmodeID.Server) return;

            var achievements = (Dictionary<string, Achievement>)typeof(AchievementManager).GetField("_achievements", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(Main.Achievements);
            var serAchievements = new TagCompound();

            foreach (var achievement in achievements)
            {
                var conditions = (Dictionary<string, AchievementCondition>)typeof(Achievement).GetField("_conditions", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(achievement.Value);
                var serConditions = new TagCompound();

                foreach (var condition in conditions)
                {
                    var serCondition = new TagCompound();
                    serCondition["completed"] = condition.Value.IsCompleted;
                    if (condition.Value is CustomIntCondition intCondition) serCondition["int"] = intCondition.Value;
                    if (condition.Value is CustomFloatCondition floatCondition) serCondition["float"] = floatCondition.Value;

                    serConditions[condition.Key] = serCondition;
                }

                serAchievements[achievement.Key] = serConditions;
            }

            tag["apachievements"] = serAchievements;
        }

        public override void LoadData(TagCompound tag)
        {
            if (Main.netMode == NetmodeID.Server) return;

            achievements = new();
            if (!tag.ContainsKey("apachievements")) return;

            achievements = tag.Get<TagCompound>("apachievements");
        }

        public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource)
        {
            if (damageSource.SourceCustomReason != null && damageSource.SourceCustomReason.StartsWith("[DeathLink]")) return;
            if (Main.netMode == NetmodeID.SinglePlayer)
            {
                ModContent.GetInstance<ArchipelagoSystem>().TriggerDeathlink(damageSource.GetDeathText(Player.name).ToString(), Main.myPlayer);
                return;
            }
            else if (Main.netMode == NetmodeID.Server) return;

            var packet = ModContent.GetInstance<SeldomArchipelago>().GetPacket();
            packet.Write($"deathlink{damageSource.GetDeathText(Player.name)}");
            packet.Send();
        }
    }
}
