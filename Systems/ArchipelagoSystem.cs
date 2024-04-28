using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Packets;
using Microsoft.Xna.Framework;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Terraria;
using Terraria.Chat;
using Terraria.DataStructures;
using Terraria.GameContent.Events;
using Terraria.GameContent.Generation;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.Social;
using Terraria.WorldBuilding;

namespace SeldomArchipelago.Systems
{
    // TODO Use a separate class for data and logic
    public class ArchipelagoSystem : ModSystem
    {
        // Achievements can be completed while loading into the world, but those complete before
        // `ArchipelagoPlayer::OnEnterWorld`, where achievements are reset, is run. So, I keep track
        // of which achievements have been completed since `OnWorldLoad` was run, so
        // `ArchipelagoPlayer` knows not to clear them.
        List<string> achieved = new List<string>();
        // I store locations that were collected before `/apstart` is run so they can be queued once
        // it's run
        List<string> locationBacklog = new List<string>();
        List<Task<LocationInfoPacket>> locationQueue;
        ArchipelagoSession session;
        DeathLinkService deathlink;
        bool enabled;
        int collectedItems;
        int currentItem;
        List<string> collectedLocations = new List<string>();
        List<string> goals = new List<string>();
        bool victory = false;
        int slot = 0;

        public override void LoadWorldData(TagCompound tag)
        {
            collectedItems = tag.ContainsKey("ApCollectedItems") ? tag.Get<int>("ApCollectedItems") : 0;
        }

        public override void OnWorldLoad()
        {
            typeof(SocialAPI).GetField("_mode", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, SocialMode.None);

            locationQueue = new List<Task<LocationInfoPacket>>();

            if (Main.netMode == NetmodeID.MultiplayerClient) return;

            var config = ModContent.GetInstance<Config.Config>();

            LoginResult result;
            try
            {
                session = ArchipelagoSessionFactory.CreateSession(config.address, config.port);

                result = session.TryConnectAndLogin("Terraria", config.name, ItemsHandlingFlags.AllItems, null, null, null, config.password == "" ? null : config.password);
                if (result is LoginFailure)
                {
                    session = null;
                    return;
                }
            }
            catch
            {
                session = null;
                return;
            }

            var locations = session.DataStorage[Scope.Slot, "CollectedLocations"].To<String[]>();
            if (locations != null)
            {
                collectedLocations = new List<string>(locations);
            }

            var success = (LoginSuccessful)result;
            goals = new List<string>(((JArray)success.SlotData["goal"]).ToObject<string[]>());

            victory = false;

            session.MessageLog.OnMessageReceived += (message) =>
            {
                var text = "";
                foreach (var part in message.Parts)
                {
                    text += part.Text;
                }
                Chat(text);
            };

            if ((bool)success.SlotData["deathlink"])
            {
                deathlink = session.CreateDeathLinkService();
                deathlink.EnableDeathLink();

                deathlink.OnDeathLinkReceived += ReceiveDeathlink;
            }

            slot = success.Slot;
        }

        public string[] flags = { "Post-King Slime", "Post-Desert Scourge", "Post-Giant Clam", "Post-Eye of Cthulhu", "Post-Acid Rain Tier 1", "Post-Crabulon", "Post-Evil Boss", "Post-Old One's Army Tier 1", "Post-Goblin Army", "Post-Queen Bee", "Post-The Hive Mind", "Post-The Perforators", "Post-Skeletron", "Post-Deerclops", "Post-The Slime God", "Hardmode", "Post-Dreadnautilus", "Post-Hardmode Giant Clam", "Post-Pirate Invasion", "Post-Queen Slime", "Post-Aquatic Scourge", "Post-Cragmaw Mire", "Post-Acid Rain Tier 2", "Post-The Twins", "Post-Old One's Army Tier 2", "Post-Brimstone Elemental", "Post-The Destroyer", "Post-Cryogen", "Post-Skeletron Prime", "Post-Calamitas Clone", "Post-Plantera", "Post-Great Sand Shark", "Post-Leviathan and Anahita", "Post-Astrum Aureus", "Post-Golem", "Post-Old One's Army Tier 3", "Post-Martian Madness", "Post-The Plaguebringer Goliath", "Post-Duke Fishron", "Post-Mourning Wood", "Post-Pumpking", "Post-Everscream", "Post-Santa-NK1", "Post-Ice Queen", "Post-Frost Legion", "Post-Ravager", "Post-Empress of Light", "Post-Lunatic Cultist", "Post-Astrum Deus", "Post-Lunar Events", "Post-Moon Lord", "Post-Profaned Guardians", "Post-The Dragonfolly", "Post-Providence, the Profaned Goddess", "Post-Storm Weaver", "Post-Ceaseless Void", "Post-Signus, Envoy of the Devourer", "Post-Polterghast", "Post-Mauler", "Post-Nuclear Terror", "Post-The Old Duke", "Post-The Devourer of Gods", "Post-Yharon, Dragon of Rebirth", "Post-Exo Mechs", "Post-Supreme Witch, Calamitas", "Post-Primordial Wyrm", "Post-Boss Rush" };

        // static readonly Func<bool>[] flags = new[] { () => NPC.downedSlimeKing, () => NPC.downedBoss1, () => NPC.downedBoss2, () => DD2Event.DownedInvasionT1, () => NPC.downedGoblins, () => NPC.downedQueenBee, () => NPC.downedBoss3, () => NPC.downedDeerclops, () => Main.hardMode, () => NPC.downedPirates, () => NPC.downedQueenSlime, () => NPC.downedMechBoss1, () => NPC.downedMechBoss2, () => NPC.downedMechBoss3, () => NPC.downedPlantBoss, () => NPC.downedGolemBoss, () => DD2Event.DownedInvasionT2, () => NPC.downedMartians, () => NPC.downedFishron, () => NPC.downedHalloweenTree, () => NPC.downedHalloweenKing, () => NPC.downedChristmasTree, () => NPC.downedChristmasSantank, () => NPC.downedChristmasIceQueen, () => NPC.downedFrost, () => NPC.downedEmpressOfLight, () => NPC.downedAncientCultist, () => NPC.downedTowerNebula, () => NPC.downedMoonlord };

        public bool CheckFlag(string flag) => flag switch
        {
            "Post-King Slime" => NPC.downedSlimeKing,
            "Post-Eye of Cthulhu" => NPC.downedBoss1,
            "Post-Evil Boss" => NPC.downedBoss2,
            "Post-Old One's Army Tier 1" => DD2Event.DownedInvasionT1,
            "Post-Goblin Army" => NPC.downedGoblins,
            "Post-Queen Bee" => NPC.downedQueenBee,
            "Post-Skeletron" => NPC.downedBoss3,
            "Post-Deerclops" => NPC.downedDeerclops,
            "Hardmode" => Main.hardMode,
            "Post-Pirate Invasion" => NPC.downedPirates,
            "Post-Queen Slime" => NPC.downedQueenSlime,
            "Post-The Twins" => NPC.downedMechBoss2,
            "Post-Old One's Army Tier 2" => DD2Event.DownedInvasionT2,
            "Post-The Destroyer" => NPC.downedMechBoss1,
            "Post-Skeletron Prime" => NPC.downedMechBoss3,
            "Post-Plantera" => NPC.downedPlantBoss,
            "Post-Golem" => NPC.downedGolemBoss,
            "Post-Old One's Army Tier 3" => DD2Event.DownedInvasionT3,
            "Post-Martian Madness" => NPC.downedMartians,
            "Post-Duke Fishron" => NPC.downedFishron,
            "Post-Mourning Wood" => NPC.downedHalloweenTree,
            "Post-Pumpking" => NPC.downedHalloweenKing,
            "Post-Everscream" => NPC.downedChristmasTree,
            "Post-Santa-NK1" => NPC.downedChristmasSantank,
            "Post-Ice Queen" => NPC.downedChristmasIceQueen,
            "Post-Frost Legion" => NPC.downedFrost,
            "Post-Empress of Light" => NPC.downedEmpressOfLight,
            "Post-Lunatic Cultist" => NPC.downedAncientCultist,
            "Post-Lunar Events" => NPC.downedTowerNebula,
            "Post-Moon Lord" => NPC.downedMoonlord,
            _ => ModContent.GetInstance<CalamitySystem>()?.CheckCalamityFlag(flag) ?? false,
        };

        public void Collect(string item)
        {
            switch (item)
            {
                case "Reward: Torch God's Favor": GiveItem(ItemID.TorchGodsFavor); break;
                case "Post-King Slime": BossFlag(ref NPC.downedSlimeKing, NPCID.KingSlime); break;
                case "Post-Eye of Cthulhu": BossFlag(ref NPC.downedBoss1, NPCID.EyeofCthulhu); break;
                case "Post-Evil Boss": BossFlag(ref NPC.downedBoss2, NPCID.EaterofWorldsHead); break;
                case "Post-Old One's Army Tier 1": DD2Event.DownedInvasionT1 = true; break;
                case "Post-Goblin Army": NPC.downedGoblins = true; break;
                case "Post-Queen Bee": BossFlag(ref NPC.downedQueenBee, NPCID.QueenBee); break;
                case "Post-Skeletron": BossFlag(ref NPC.downedBoss3, NPCID.SkeletronHead); break;
                case "Post-Deerclops": BossFlag(ref NPC.downedDeerclops, NPCID.Deerclops); break;
                case "Hardmode":
                    BossFlag(NPCID.WallofFlesh);
                    WorldGen.StartHardmode();
                    break;
                case "Post-Pirate Invasion": NPC.downedPirates = true; break;
                case "Post-Queen Slime": BossFlag(ref NPC.downedQueenSlime, NPCID.QueenSlimeBoss); break;
                case "Post-The Twins":
                    Action set = () => NPC.downedMechBoss2 = NPC.downedMechBossAny = true;
                    if (NPC.AnyNPCs(NPCID.Retinazer))
                    {
                        if (NPC.AnyNPCs(NPCID.Spazmatism))
                        {
                            // If the player is fighting The Twins, it would mess with the `CalamityGlobalNPC.OnKill` logic, so we have a fallback
                            if (ModLoader.HasMod("CalamityMod")) ModContent.GetInstance<CalamitySystem>().SpawnMechOres();
                            NPC.downedMechBoss2 = NPC.downedMechBossAny = true;
                        }
                        else BossFlag(set, NPCID.Retinazer);
                    }
                    else BossFlag(set, NPCID.Spazmatism);
                    break;
                case "Post-Old One's Army Tier 2": DD2Event.DownedInvasionT2 = true; break;
                case "Post-The Destroyer": BossFlag(() => NPC.downedMechBoss1 = NPC.downedMechBossAny = true, NPCID.TheDestroyer); break;
                case "Post-Skeletron Prime": BossFlag(() => NPC.downedMechBoss3 = NPC.downedMechBossAny = true, NPCID.SkeletronPrime); break;
                case "Post-Plantera": BossFlag(ref NPC.downedPlantBoss, NPCID.Plantera); break;
                case "Post-Golem": BossFlag(ref NPC.downedGolemBoss, NPCID.Golem); break;
                case "Post-Old One's Army Tier 3": DD2Event.DownedInvasionT3 = true; break;
                case "Post-Martian Madness": NPC.downedMartians = true; break;
                case "Post-Duke Fishron": BossFlag(ref NPC.downedFishron, NPCID.DukeFishron); break;
                case "Post-Mourning Wood": BossFlag(ref NPC.downedHalloweenTree, NPCID.MourningWood); break;
                case "Post-Pumpking": BossFlag(ref NPC.downedHalloweenKing, NPCID.Pumpking); break;
                case "Post-Everscream": BossFlag(ref NPC.downedChristmasTree, NPCID.Everscream); break;
                case "Post-Santa-NK1": BossFlag(ref NPC.downedChristmasSantank, NPCID.SantaNK1); break;
                case "Post-Ice Queen": BossFlag(ref NPC.downedChristmasIceQueen, NPCID.IceQueen); break;
                case "Post-Frost Legion": NPC.downedFrost = true; break;
                case "Post-Empress of Light": BossFlag(ref NPC.downedEmpressOfLight, NPCID.HallowBoss); break;
                case "Post-Lunatic Cultist": BossFlag(ref NPC.downedAncientCultist, NPCID.CultistBoss); break;
                case "Post-Lunar Events": NPC.downedTowerNebula = NPC.downedTowerSolar = NPC.downedTowerStardust = NPC.downedTowerVortex = true; break;
                case "Post-Moon Lord": BossFlag(ref NPC.downedMoonlord, NPCID.MoonLordCore); break;
                case "Post-Desert Scourge": ModContent.GetInstance<CalamitySystem>().CalamityOnKillDesertScourge(); break;
                case "Post-Giant Clam": ModContent.GetInstance<CalamitySystem>().CalamityOnKillGiantClam(false); break;
                case "Post-Acid Rain Tier 1": ModContent.GetInstance<CalamitySystem>().CalamityAcidRainTier1Downed(); break;
                case "Post-Crabulon": ModContent.GetInstance<CalamitySystem>().CalamityOnKillCrabulon(); break;
                case "Post-The Hive Mind": ModContent.GetInstance<CalamitySystem>().CalamityOnKillTheHiveMind(); break;
                case "Post-The Perforators": ModContent.GetInstance<CalamitySystem>().CalamityOnKillThePerforators(); break;
                case "Post-The Slime God": ModContent.GetInstance<CalamitySystem>().CalamityOnKillTheSlimeGod(); break;
                case "Post-Dreadnautilus": ModContent.GetInstance<CalamitySystem>().CalamityDreadnautilusDowned(); break;
                case "Post-Hardmode Giant Clam": ModContent.GetInstance<CalamitySystem>().CalamityOnKillGiantClam(true); break;
                case "Post-Aquatic Scourge": ModContent.GetInstance<CalamitySystem>().CalamityOnKillAquaticScourge(); break;
                case "Post-Cragmaw Mire": ModContent.GetInstance<CalamitySystem>().CalamityOnKillCragmawMire(); break;
                case "Post-Acid Rain Tier 2": ModContent.GetInstance<CalamitySystem>().CalamityAcidRainTier2Downed(); break;
                case "Post-Brimstone Elemental": ModContent.GetInstance<CalamitySystem>().CalamityOnKillBrimstoneElemental(); break;
                case "Post-Cryogen": ModContent.GetInstance<CalamitySystem>().CalamityOnKillCryogen(); break;
                case "Post-Calamitas Clone": ModContent.GetInstance<CalamitySystem>().CalamityOnKillCalamitasClone(); break;
                case "Post-Great Sand Shark": ModContent.GetInstance<CalamitySystem>().CalamityOnKillGreatSandShark(); break;
                case "Post-Leviathan and Anahita": ModContent.GetInstance<CalamitySystem>().CalamityOnKillLeviathanAndAnahita(); break;
                case "Post-Astrum Aureus": ModContent.GetInstance<CalamitySystem>().CalamityOnKillAstrumAureus(); break;
                case "Post-The Plaguebringer Goliath": ModContent.GetInstance<CalamitySystem>().CalamityOnKillThePlaguebringerGoliath(); break;
                case "Post-Ravager": ModContent.GetInstance<CalamitySystem>().CalamityOnKillRavager(); break;
                case "Post-Astrum Deus": ModContent.GetInstance<CalamitySystem>().CalamityOnKillAstrumDeus(); break;
                case "Post-Profaned Guardians": ModContent.GetInstance<CalamitySystem>().CalamityOnKillProfanedGuardians(); break;
                case "Post-The Dragonfolly": ModContent.GetInstance<CalamitySystem>().CalamityOnKillTheDragonfolly(); break;
                case "Post-Providence, the Profaned Goddess": ModContent.GetInstance<CalamitySystem>().CalamityOnKillProvidenceTheProfanedGoddess(); break;
                case "Post-Storm Weaver": ModContent.GetInstance<CalamitySystem>().CalamityOnKillStormWeaver(); break;
                case "Post-Ceaseless Void": ModContent.GetInstance<CalamitySystem>().CalamityOnKillCeaselessVoid(); break;
                case "Post-Signus, Envoy of the Devourer": ModContent.GetInstance<CalamitySystem>().CalamityOnKillSignusEnvoyOfTheDevourer(); break;
                case "Post-Polterghast": ModContent.GetInstance<CalamitySystem>().CalamityOnKillPolterghast(); break;
                case "Post-Mauler": ModContent.GetInstance<CalamitySystem>().CalamityOnKillMauler(); break;
                case "Post-Nuclear Terror": ModContent.GetInstance<CalamitySystem>().CalamityOnKillNuclearTerror(); break;
                case "Post-The Old Duke": ModContent.GetInstance<CalamitySystem>().CalamityOnKillTheOldDuke(); break;
                case "Post-The Devourer of Gods": ModContent.GetInstance<CalamitySystem>().CalamityOnKillTheDevourerOfGods(); break;
                case "Post-Yharon, Dragon of Rebirth": ModContent.GetInstance<CalamitySystem>().CalamityOnKillYharonDragonOfRebirth(); break;
                case "Post-Exo Mechs": ModContent.GetInstance<CalamitySystem>().CalamityOnKillExoMechs(); break;
                case "Post-Supreme Witch, Calamitas": ModContent.GetInstance<CalamitySystem>().CalamityOnKillSupremeWitchCalamitas(); break;
                case "Post-Primordial Wyrm": ModContent.GetInstance<CalamitySystem>().CalamityPrimordialWyrmDowned(); break;
                case "Post-Boss Rush": ModContent.GetInstance<CalamitySystem>().CalamityBossRushDowned(); break;
                case "Reward: Hermes Boots": GiveItem(ItemID.HermesBoots); break;
                case "Reward: Magic Mirror": GiveItem(ItemID.MagicMirror); break;
                case "Reward: Demon Conch": GiveItem(ItemID.DemonConch); break;
                case "Reward: Magic Conch": GiveItem(ItemID.MagicConch); break;
                case "Reward: Grappling Hook": GiveItem(ItemID.GrapplingHook); break;
                case "Reward: Cloud in a Bottle": GiveItem(ItemID.CloudinaBottle); break;
                case "Reward: Climbing Claws": GiveItem(ItemID.ClimbingClaws); break;
                case "Reward: Ancient Chisel": GiveItem(ItemID.AncientChisel); break;
                case "Reward: Fledgling Wings": GiveItem(ItemID.CreativeWings); break;
                case "Reward: Rod of Discord": GiveItem(ItemID.RodofDiscord); break;
                case "Reward: Aglet": GiveItem(ItemID.Aglet); break;
                case "Reward: Anklet of the Wind": GiveItem(ItemID.AnkletoftheWind); break;
                case "Reward: Ice Skates": GiveItem(ItemID.IceSkates); break;
                case "Reward: Lava Charm": GiveItem(ItemID.LavaCharm); break;
                case "Reward: Water Walking Boots": GiveItem(ItemID.WaterWalkingBoots); break;
                case "Reward: Flipper": GiveItem(ItemID.Flipper); break;
                case "Reward: Frog Leg": GiveItem(ItemID.FrogLeg); break;
                case "Reward: Shoe Spikes": GiveItem(ItemID.ShoeSpikes); break;
                case "Reward: Tabi": GiveItem(ItemID.Tabi); break;
                case "Reward: Black Belt": GiveItem(ItemID.BlackBelt); break;
                case "Reward: Flying Carpet": GiveItem(ItemID.FlyingCarpet); break;
                case "Reward: Moon Charm": GiveItem(ItemID.MoonCharm); break;
                case "Reward: Neptune's Shell": GiveItem(ItemID.NeptunesShell); break;
                case "Reward: Compass": GiveItem(ItemID.Compass); break;
                case "Reward: Depth Meter": GiveItem(ItemID.DepthMeter); break;
                case "Reward: Radar": GiveItem(ItemID.Radar); break;
                case "Reward: Tally Counter": GiveItem(ItemID.TallyCounter); break;
                case "Reward: Lifeform Analyzer": GiveItem(ItemID.LifeformAnalyzer); break;
                case "Reward: DPS Meter": GiveItem(ItemID.DPSMeter); break;
                case "Reward: Stopwatch": GiveItem(ItemID.Stopwatch); break;
                case "Reward: Metal Detector": GiveItem(ItemID.MetalDetector); break;
                case "Reward: Fisherman's Pocket Guide": GiveItem(ItemID.FishermansGuide); break;
                case "Reward: Weather Radio": GiveItem(ItemID.WeatherRadio); break;
                case "Reward: Sextant": GiveItem(ItemID.Sextant); break;
                case "Reward: Band of Regeneration": GiveItem(ItemID.BandofRegeneration); break;
                case "Reward: Celestial Magnet": GiveItem(ItemID.CelestialMagnet); break;
                case "Reward: Nature's Gift": GiveItem(ItemID.NaturesGift); break;
                case "Reward: Philosopher's Stone": GiveItem(ItemID.PhilosophersStone); break;
                case "Reward: Cobalt Shield": GiveItem(ItemID.CobaltShield); break;
                case "Reward: Armor Polish": GiveItem(ItemID.ArmorPolish); break;
                case "Reward: Vitamins": GiveItem(ItemID.Vitamins); break;
                case "Reward: Bezoar": GiveItem(ItemID.Bezoar); break;
                case "Reward: Adhesive Bandage": GiveItem(ItemID.AdhesiveBandage); break;
                case "Reward: Megaphone": GiveItem(ItemID.Megaphone); break;
                case "Reward: Nazar": GiveItem(ItemID.Nazar); break;
                case "Reward: Fast Clock": GiveItem(ItemID.FastClock); break;
                case "Reward: Trifold Map": GiveItem(ItemID.TrifoldMap); break;
                case "Reward: Blindfold": GiveItem(ItemID.Blindfold); break;
                case "Reward: Pocket Mirror": GiveItem(ItemID.PocketMirror); break;
                case "Reward: Paladin's Shield": GiveItem(ItemID.PaladinsShield); break;
                case "Reward: Frozen Turtle Shell": GiveItem(ItemID.FrozenTurtleShell); break;
                case "Reward: Flesh Knuckles": GiveItem(ItemID.FleshKnuckles); break;
                case "Reward: Putrid Scent": GiveItem(ItemID.PutridScent); break;
                case "Reward: Feral Claws": GiveItem(ItemID.FeralClaws); break;
                case "Reward: Cross Necklace": GiveItem(ItemID.CrossNecklace); break;
                case "Reward: Star Cloak": GiveItem(ItemID.StarCloak); break;
                case "Reward: Titan Glove": GiveItem(ItemID.TitanGlove); break;
                case "Reward: Obsidian Rose": GiveItem(ItemID.ObsidianRose); break;
                case "Reward: Magma Stone": GiveItem(ItemID.MagmaStone); break;
                case "Reward: Shark Tooth Necklace": GiveItem(ItemID.SharkToothNecklace); break;
                case "Reward: Magic Quiver": GiveItem(ItemID.MagicQuiver); break;
                case "Reward: Rifle Scope": GiveItem(ItemID.RifleScope); break;
                case "Reward: Brick Layer": GiveItem(ItemID.BrickLayer); break;
                case "Reward: Extendo Grip": GiveItem(ItemID.ExtendoGrip); break;
                case "Reward: Paint Sprayer": GiveItem(ItemID.PaintSprayer); break;
                case "Reward: Portable Cement Mixer": GiveItem(ItemID.PortableCementMixer); break;
                case "Reward: Treasure Magnet": GiveItem(ItemID.TreasureMagnet); break;
                case "Reward: Step Stool": GiveItem(ItemID.PortableStool); break;
                case "Reward: Discount Card": GiveItem(ItemID.DiscountCard); break;
                case "Reward: Gold Ring": GiveItem(ItemID.GoldRing); break;
                case "Reward: Lucky Coin": GiveItem(ItemID.LuckyCoin); break;
                case "Reward: High Test Fishing Line": GiveItem(ItemID.HighTestFishingLine); break;
                case "Reward: Angler Earring": GiveItem(ItemID.AnglerEarring); break;
                case "Reward: Tackle Box": GiveItem(ItemID.TackleBox); break;
                case "Reward: Lavaproof Fishing Hook": GiveItem(ItemID.LavaFishingHook); break;
                case "Reward: Red Counterweight": GiveItem(ItemID.RedCounterweight); break;
                case "Reward: Yoyo Glove": GiveItem(ItemID.YoYoGlove); break;
                case "Reward: Coins": GiveCoins(); break;
                case "Reward: Cosmolight": ModContent.GetInstance<CalamitySystem>().GiveCosmolight(); break;
                case "Reward: Diving Helmet": GiveItem(ItemID.DivingHelmet); break;
                case "Reward: Jellyfish Necklace": GiveItem(ItemID.JellyfishNecklace); break;
                case "Reward: Corrupt Flask": ModContent.GetInstance<CalamitySystem>().GiveCorruptFlask(); break;
                case "Reward: Crimson Flask": ModContent.GetInstance<CalamitySystem>().GiveCrimsonFlask(); break;
                case "Reward: Craw Carapace": ModContent.GetInstance<CalamitySystem>().GiveCrawCarapace(); break;
                case "Reward: Giant Shell": ModContent.GetInstance<CalamitySystem>().GiveGiantShell(); break;
                case "Reward: Life Jelly": ModContent.GetInstance<CalamitySystem>().GiveLifeJelly(); break;
                case "Reward: Vital Jelly": ModContent.GetInstance<CalamitySystem>().GiveVitalJelly(); break;
                case "Reward: Cleansing Jelly": ModContent.GetInstance<CalamitySystem>().GiveCleansingJelly(); break;
                case "Reward: Giant Tortoise Shell": ModContent.GetInstance<CalamitySystem>().GiveGiantTortoiseShell(); break;
                case "Reward: Coin of Deceit": ModContent.GetInstance<CalamitySystem>().GiveCoinOfDeceit(); break;
                case "Reward: Ink Bomb": ModContent.GetInstance<CalamitySystem>().GiveInkBomb(); break;
                case "Reward: Voltaic Jelly": ModContent.GetInstance<CalamitySystem>().GiveVoltaicJelly(); break;
                case "Reward: Wulfrum Battery": ModContent.GetInstance<CalamitySystem>().GiveWulfrumBattery(); break;
                case "Reward: Luxor's Gift": ModContent.GetInstance<CalamitySystem>().GiveLuxorsGift(); break;
                case "Reward: Raider's Talisman": ModContent.GetInstance<CalamitySystem>().GiveRaidersTalisman(); break;
                case "Reward: Rotten Dogtooth": ModContent.GetInstance<CalamitySystem>().GiveRottenDogtooth(); break;
                case "Reward: Scuttler's Jewel": ModContent.GetInstance<CalamitySystem>().GiveScuttlersJewel(); break;
                case "Reward: Unstable Granite Core": ModContent.GetInstance<CalamitySystem>().GiveUnstableGraniteCore(); break;
                case "Reward: Amidias' Spark": ModContent.GetInstance<CalamitySystem>().GiveAmidiasSpark(); break;
                case "Reward: Ursa Sergeant": ModContent.GetInstance<CalamitySystem>().GiveUrsaSergeant(); break;
                case "Reward: Trinket of Chi": ModContent.GetInstance<CalamitySystem>().GiveTrinketOfChi(); break;
                case "Reward: The Transformer": ModContent.GetInstance<CalamitySystem>().GiveTheTransformer(); break;
                case "Reward: Rover Drive": ModContent.GetInstance<CalamitySystem>().GiveRoverDrive(); break;
                case "Reward: Marnite Repulsion Shield": ModContent.GetInstance<CalamitySystem>().GiveMarniteRepulsionShield(); break;
                case "Reward: Frost Barrier": ModContent.GetInstance<CalamitySystem>().GiveFrostBarrier(); break;
                case "Reward: Ancient Fossil": ModContent.GetInstance<CalamitySystem>().GiveAncientFossil(); break;
                case "Reward: Spelunker's Amulet": ModContent.GetInstance<CalamitySystem>().GiveSpelunkersAmulet(); break;
                case "Reward: Fungal Symbiote": ModContent.GetInstance<CalamitySystem>().GiveFungalSymbiote(); break;
                case "Reward: Gladiator's Locket": ModContent.GetInstance<CalamitySystem>().GiveGladiatorsLocket(); break;
                case "Reward: Wulfrum Acrobatics Pack": ModContent.GetInstance<CalamitySystem>().GiveWulfrumAcrobaticsPack(); break;
                case "Reward: Depths Charm": ModContent.GetInstance<CalamitySystem>().GiveDepthsCharm(); break;
                case "Reward: Anechoic Plating": ModContent.GetInstance<CalamitySystem>().GiveAnechoicPlating(); break;
                case "Reward: Iron Boots": ModContent.GetInstance<CalamitySystem>().GiveIronBoots(); break;
                case "Reward: Sprit Glyph": ModContent.GetInstance<CalamitySystem>().GiveSpritGlyph(); break;
                case "Reward: Abyssal Amulet": ModContent.GetInstance<CalamitySystem>().GiveAbyssalAmulet(); break;
                default: Chat($"Received unknown item: {item}"); break;
            }
        }

        public override void PostUpdateWorld()
        {
            if (session == null) return;

            if (!session.Socket.Connected)
            {
                Chat("Disconnected from Archipelago. Reload the world to reconnect.");
                session = null;
                deathlink = null;
                enabled = false;
                return;
            }

            if (!enabled) return;

            var unqueue = new List<int>();
            for (var i = 0; i < locationQueue.Count; i++)
            {
                var status = locationQueue[i].Status;

                if (status switch
                {
                    TaskStatus.RanToCompletion or TaskStatus.Canceled or TaskStatus.Faulted => true,
                    _ => false,
                })
                {
                    if (status == TaskStatus.RanToCompletion) foreach (var item in locationQueue[i].Result.Locations) Chat($"Sent {session.Items.GetItemName(item.Item)} to {session.Players.GetPlayerAlias(item.Player)}!");
                    else Chat("Sent an item to a player...but failed to get info about it!");

                    unqueue.Add(i);
                }
            }

            unqueue.Reverse();
            foreach (var i in unqueue) locationQueue.RemoveAt(i);

            while (session.Items.Any())
            {
                var item = session.Items.DequeueItem();
                var itemName = session.Items.GetItemName(item.Item);

                if (currentItem++ < collectedItems) continue;

                Collect(itemName);

                collectedItems++;
            }

            if (ModLoader.HasMod("CalamityMod")) ModContent.GetInstance<CalamitySystem>().CalamityPostUpdateWorld();

            if (victory) return;

            foreach (var goal in goals) if (!collectedLocations.Contains(goal)) return;

            var victoryPacket = new StatusUpdatePacket()
            {
                Status = ArchipelagoClientState.ClientGoal,
            };
            session.Socket.SendPacket(victoryPacket);

            victory = true;
        }

        public override void SaveWorldData(TagCompound tag)
        {
            tag["ApCollectedItems"] = collectedItems;
            if (enabled) session.DataStorage[Scope.Slot, "CollectedLocations"] = collectedLocations.ToArray();
        }

        public void Reset()
        {
            typeof(SocialAPI).GetField("_mode", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, SocialMode.Steam);

            achieved.Clear();
            locationBacklog.Clear();
            locationQueue = null;
            deathlink = null;
            enabled = false;
            currentItem = 0;
            collectedLocations = new List<string>();
            goals = new List<string>();
            victory = false;

            if (session != null) session.Socket.DisconnectAsync();
            session = null;
        }

        public override void OnWorldUnload()
        {
            collectedItems = 0;
            Reset();
        }

        public string[] Status() => Tuple.Create(session != null, enabled) switch
        {
            (false, _) => new[] {
                @"The world is not connected to Archipelago! Reload the world or run ""/apconnect"".",
                "If you are the host, check your config in the main menu at Workshop > Manage Mods > Config",
                "Or in-game at Settings > Mod Configuration",
            },
            (true, false) => new[] { @"Archipelago is connected but not enabled. Once everyone's joined, run ""/apstart"" to start it." },
            (true, true) => new[] { "Archipelago is active!" },
        };

        public bool Enable()
        {
            if (session == null) return false;

            enabled = true;

            foreach (var location in locationBacklog) QueueLocation(location);
            locationBacklog.Clear();

            return true;
        }

        public bool SendCommand(string command)
        {
            if (session == null) return false;

            var packet = new SayPacket()
            {
                Text = command,
            };
            session.Socket.SendPacket(packet);

            return true;
        }

        public string[] DebugInfo()
        {
            var info = new List<string>();

            if (locationBacklog.Count > 0)
            {
                info.Add("You have locations in the backlog, which should only be the case if Archipelago is inactive");
                info.Add($"Location backlog: [{string.Join("; ", locationBacklog)}]");
            }
            else
            {
                info.Add("No locations in the backlog, which is usually normal");
            }

            if (locationQueue.Count > 0)
            {
                info.Add($"You have locations queued for sending. In normal circumstances, these locations will be sent ASAP.");

                var statuses = new List<string>();
                foreach (var location in locationQueue) statuses.Add(location.Status switch
                {
                    TaskStatus.Created => "Created",
                    TaskStatus.WaitingForActivation => "Waiting for activation",
                    TaskStatus.WaitingToRun => "Waiting to run",
                    TaskStatus.Running => "Running",
                    TaskStatus.WaitingForChildrenToComplete => "Waiting for children to complete",
                    TaskStatus.RanToCompletion => "Completed",
                    TaskStatus.Canceled => "Canceled",
                    TaskStatus.Faulted => "Faulted",
                    _ => "Has a status that was added to C# after this code was written",
                });

                info.Add($"Location queue statuses: [{string.Join("; ", statuses)}]");
            }
            else
            {
                info.Add("No locations in the queue, which is usually normal");
            }

            info.Add($"You're {(session == null ? "not " : "")}connected to Archipelago");
            if (session != null && !session.Socket.Connected)
            {
                info.Add("Actually, the socket is disconnected and mod is in a weird state");
            }

            info.Add($"DeathLink is {(deathlink == null ? "dis" : "en")}abled");
            info.Add($"Archipelago is {(enabled ? "en" : "dis")}abled");
            info.Add($"You've collected {collectedItems} items, of which {currentItem} have been applied");
            info.Add($"Collected locations: [{string.Join("; ", collectedLocations)}]");
            info.Add($"Goals: [{string.Join("; ", goals)}]");
            info.Add($"Victory has {(victory ? "been achieved! Hooray!" : "not been achieved. Alas.")}");
            info.Add($"You are slot {slot}");

            return info.ToArray();
        }

        public void Chat(string message, int player = -1)
        {
            if (player == -1)
            {
                if (Main.netMode == NetmodeID.Server)
                {
                    ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral(message), Color.White);
                    Console.WriteLine(message);
                }
                else Main.NewText(message);
            }
            else ChatHelper.SendChatMessageToClient(NetworkText.FromLiteral(message), Color.White, player);
        }

        public void Chat(string[] messages, int player = -1)
        {
            foreach (var message in messages) Chat(message, player);
        }

        public void QueueLocation(string locationName)
        {
            if (!enabled)
            {
                locationBacklog.Add(locationName);
                return;
            }

            var location = session.Locations.GetLocationIdFromName("Terraria", locationName);
            if (location == -1 || !session.Locations.AllMissingLocations.Contains(location)) return;

            if (!collectedLocations.Contains(locationName))
            {
                locationQueue.Add(session.Locations.ScoutLocationsAsync(new[] { location }));
                collectedLocations.Add(locationName);
            }

            session.Locations.CompleteLocationChecks(new[] { location });
        }

        public void QueueLocationClient(string locationName)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                QueueLocation(locationName);
                return;
            }

            var packet = ModContent.GetInstance<SeldomArchipelago>().GetPacket();
            packet.Write(locationName);
            packet.Send();
        }

        public void Achieved(string achievement)
        {
            achieved.Add(achievement);
        }

        public List<string> GetAchieved()
        {
            return achieved;
        }

        public void TriggerDeathlink(string message, int player)
        {
            if (deathlink == null) return;

            var death = new DeathLink(session.Players.GetPlayerAlias(slot), message);
            deathlink.SendDeathLink(death);
            ReceiveDeathlink(death);
        }

        public void ReceiveDeathlink(DeathLink death)
        {
            var message = $"[DeathLink] {(death.Source == null ? "" : $"{death.Source} died")}{(death.Source != null && death.Cause != null ? ": " : "")}{(death.Cause == null ? "" : $"{death.Cause}")}";

            for (var i = 0; i < Main.maxPlayers; i++)
            {
                var player = Main.player[i];
                if (player.active && !player.dead) player.Hurt(PlayerDeathReason.ByCustomReason(message), 999999, 1);
            }

            if (Main.netMode == NetmodeID.SinglePlayer) return;

            var packet = ModContent.GetInstance<SeldomArchipelago>().GetPacket();
            packet.Write(message);
            packet.Send();
        }

        void BossFlag(ref bool flag, int boss)
        {
            BossFlag(boss);
            flag = true;
        }

        void BossFlag(Action set, int boss)
        {
            BossFlag(boss);
            set();
        }

        void BossFlag(int boss)
        {
            if (ModLoader.HasMod("CalamityMod")) ModContent.GetInstance<CalamitySystem>().VanillaBossKilled(boss);
        }

        void GiveItem(Action<Player> giveItem)
        {
            for (var i = 0; i < Main.maxPlayers; i++)
            {
                var player = Main.player[i];
                if (player.active) giveItem(player);
            }
        }

        void GiveItem(int item) => GiveItem(player => player.QuickSpawnItem(player.GetSource_GiftOrReward(), item, 1));
        public void GiveItem<T>() where T : ModItem => GiveItem(ModContent.ItemType<T>());

        int[] baseCoins = { 15, 20, 25, 30, 40, 50, 70, 100 };

        void GiveCoins()
        {
            var flagCount = 0;
            foreach (var flag in flags) if (CheckFlag(flag)) flagCount++;
            var count = baseCoins[flagCount % 8] * (int)Math.Pow(10, flagCount / 8);

            var platinum = count / 10000;
            var gold = count % 10000 / 100;
            var silver = count % 100;
            GiveItem(player =>
            {
                if (platinum > 0) player.QuickSpawnItem(player.GetSource_GiftOrReward(), ItemID.PlatinumCoin, platinum);
                if (gold > 0) player.QuickSpawnItem(player.GetSource_GiftOrReward(), ItemID.GoldCoin, gold);
                if (silver > 0) player.QuickSpawnItem(player.GetSource_GiftOrReward(), ItemID.SilverCoin, silver);
            });
        }

        public override void ModifyHardmodeTasks(List<GenPass> list)
        {
            // If all mech boss flags are collected, but not Hardmode, there was no Hallow when hallowed ore was generated, so no ore was generated. So, we generate new ore if this is the case.
            list.Add(new PassLegacy("Hallowed Ore", (progress, config) =>
            {
                if (ModLoader.HasMod("CalamityMod")) ModContent.GetInstance<CalamitySystem>().CalamityStartHardmode();
            }));
        }
    }
}
