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
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.Social;

namespace SeldomArchipelago.Systems
{
    // TODO Use a separate class for data and logic
    public class ArchipelagoSystem : ModSystem
    {
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
            session = ArchipelagoSessionFactory.CreateSession(config.address, config.port);

            LoginResult result;
            try
            {
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
            this.goals = new List<string>(((JArray)success.SlotData["goal"]).ToObject<string[]>());

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

        public string[] flags = new[] { "Post-King Slime", "Post-Desert Scourge", "Post-Giant Clam", "Post-Eye of Cthulhu", "Post-Acid Rain Tier 1", "Post-Crabulon", "Post-Evil Boss", "Post-Old One's Army Tier 1", "Post-Goblin Army", "Post-Queen Bee", "Post-The Hive Mind", "Post-The Perforators", "Post-Skeletron", "Post-Deerclops", "Post-The Slime God", "Hardmode", "Post-Dreadnautilus", "Post-Hardmode Giant Clam", "Post-Pirate Invasion", "Post-Queen Slime", "Post-Aquatic Scourge", "Post-Cragmaw Mire", "Post-Acid Rain Tier 2", "Post-The Twins", "Post-Old One's Army Tier 2", "Post-Brimstone Elemental", "Post-The Destroyer", "Post-Cryogen", "Post-Skeletron Prime", "Post-Calamitas Clone", "Post-Plantera", "Post-Great Sand Shark", "Post-Leviathan and Anahita", "Post-Astrum Aureus", "Post-Golem", "Post-Old One's Army Tier 3", "Post-Martian Madness", "Post-The Plaguebringer Goliath", "Post-Duke Fishron", "Post-Mourning Wood", "Post-Pumpking", "Post-Everscream", "Post-Santa-NK1", "Post-Ice Queen", "Post-Frost Legion", "Post-Ravager", "Post-Empress of Light", "Post-Lunatic Cultist", "Post-Astrum Deus", "Post-Lunar Events", "Post-Moon Lord", "Post-Profaned Guardians", "Post-The Dragonfolly", "Post-Providence, the Profaned Goddess", "Post-Storm Weaver", "Post-Ceaseless Void", "Post-Signus, Envoy of the Devourer", "Post-Polterghast", "Post-Mauler", "Post-Nuclear Terror", "Post-The Old Duke", "Post-The Devourer of Gods", "Post-Yharon, Dragon of Rebirth", "Post-Exo Mechs", "Post-Supreme Witch, Calamitas", "Post-Adult Eidolon Wyrm", "Post-Boss Rush" };

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
            _ => CheckCalamityFlag(flag),
        };

        public bool CheckCalamityFlag(string flag) => flag switch
        {
            "Post-Desert Scourge" => CalamityMod.DownedBossSystem.downedDesertScourge,
            "Post-Giant Clam" => CalamityMod.DownedBossSystem.downedCLAM,
            "Post-Acid Rain Tier 1" => CalamityMod.DownedBossSystem.downedEoCAcidRain,
            "Post-Crabulon" => CalamityMod.DownedBossSystem.downedCrabulon,
            "Post-The Hive Mind" => CalamityMod.DownedBossSystem.downedHiveMind,
            "Post-The Perforators" => CalamityMod.DownedBossSystem.downedPerforator,
            "Post-The Slime God" => CalamityMod.DownedBossSystem.downedSlimeGod,
            "Post-Dreadnautilus" => CalamityMod.DownedBossSystem.downedDreadnautilus,
            "Post-Hardmode Giant Clam" => CalamityMod.DownedBossSystem.downedCLAMHardMode,
            "Post-Aquatic Scourge" => CalamityMod.DownedBossSystem.downedAquaticScourge,
            "Post-Cragmaw Mire" => CalamityMod.DownedBossSystem.downedCragmawMire,
            "Post-Acid Rain Tier 2" => CalamityMod.DownedBossSystem.downedAquaticScourgeAcidRain,
            "Post-Brimstone Elemental" => CalamityMod.DownedBossSystem.downedBrimstoneElemental,
            "Post-Cryogen" => CalamityMod.DownedBossSystem.downedCryogen,
            "Post-Calamitas Clone" => CalamityMod.DownedBossSystem.downedCalamitasClone,
            "Post-Great Sand Shark" => CalamityMod.DownedBossSystem.downedGSS,
            "Post-Leviathan and Anahita" => CalamityMod.DownedBossSystem.downedLeviathan,
            "Post-Astrum Aureus" => CalamityMod.DownedBossSystem.downedAstrumAureus,
            "Post-The Plaguebringer Goliath" => CalamityMod.DownedBossSystem.downedPlaguebringer,
            "Post-Ravager" => CalamityMod.DownedBossSystem.downedRavager,
            "Post-Astrum Deus" => CalamityMod.DownedBossSystem.downedAstrumDeus,
            "Post-Profaned Guardians" => CalamityMod.DownedBossSystem.downedGuardians,
            "Post-The Dragonfolly" => CalamityMod.DownedBossSystem.downedDragonfolly,
            "Post-Providence, the Profaned Goddess" => CalamityMod.DownedBossSystem.downedProvidence,
            "Post-Storm Weaver" => CalamityMod.DownedBossSystem.downedStormWeaver,
            "Post-Ceaseless Void" => CalamityMod.DownedBossSystem.downedCeaselessVoid,
            "Post-Signus, Envoy of the Devourer" => CalamityMod.DownedBossSystem.downedSignus,
            "Post-Polterghast" => CalamityMod.DownedBossSystem.downedPolterghast,
            "Post-Mauler" => CalamityMod.DownedBossSystem.downedMauler,
            "Post-Nuclear Terror" => CalamityMod.DownedBossSystem.downedNuclearTerror,
            "Post-The Old Duke" => CalamityMod.DownedBossSystem.downedBoomerDuke,
            "Post-The Devourer of Gods" => CalamityMod.DownedBossSystem.downedDoG,
            "Post-Yharon, Dragon of Rebirth" => CalamityMod.DownedBossSystem.downedYharon,
            "Post-Exo Mechs" => CalamityMod.DownedBossSystem.downedExoMechs,
            "Post-Supreme Witch, Calamitas" => CalamityMod.DownedBossSystem.downedCalamitas,
            "Post-Adult Eidolon Wyrm" => CalamityMod.DownedBossSystem.downedAdultEidolonWyrm,
            "Post-Boss Rush" => CalamityMod.DownedBossSystem.downedBossRush,
            _ => false,
        };

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

                switch (itemName)
                {
                    case "Reward: Torch God's Favor": GiveItem(ItemID.TorchGodsFavor); break;
                    case "Post-King Slime": NPC.downedSlimeKing = true; break;
                    case "Post-Eye of Cthulhu": NPC.downedBoss1 = true; break;
                    case "Post-Evil Boss": NPC.downedBoss2 = true; break;
                    case "Post-Old One's Army Tier 1": DD2Event.DownedInvasionT1 = true; break;
                    case "Post-Goblin Army": NPC.downedGoblins = true; break;
                    case "Post-Queen Bee": NPC.downedQueenBee = true; break;
                    case "Post-Skeletron": NPC.downedBoss3 = true; break;
                    case "Post-Deerclops": NPC.downedDeerclops = true; break;
                    case "Hardmode": StartHardmode(); break;
                    case "Post-Pirate Invasion": NPC.downedPirates = true; break;
                    case "Post-Queen Slime": NPC.downedQueenSlime = true; break;
                    case "Post-The Twins": NPC.downedMechBoss2 = NPC.downedMechBossAny = true; break;
                    case "Post-Old One's Army Tier 2": DD2Event.DownedInvasionT2 = true; break;
                    case "Post-The Destroyer": NPC.downedMechBoss1 = NPC.downedMechBossAny = true; break;
                    case "Post-Skeletron Prime": NPC.downedMechBoss3 = NPC.downedMechBossAny = true; break;
                    case "Post-Plantera": NPC.downedPlantBoss = true; break;
                    case "Post-Golem": NPC.downedGolemBoss = true; break;
                    case "Post-Old One's Army Tier 3": DD2Event.DownedInvasionT3 = true; break;
                    case "Post-Martian Madness": NPC.downedMartians = true; break;
                    case "Post-Duke Fishron": NPC.downedFishron = true; break;
                    case "Post-Mourning Wood": NPC.downedHalloweenTree = true; break;
                    case "Post-Pumpking": NPC.downedHalloweenKing = true; break;
                    case "Post-Everscream": NPC.downedChristmasTree = true; break;
                    case "Post-Santa-NK1": NPC.downedChristmasSantank = true; break;
                    case "Post-Ice Queen": NPC.downedChristmasIceQueen = true; break;
                    case "Post-Frost Legion": NPC.downedFrost = true; break;
                    case "Post-Empress of Light": NPC.downedEmpressOfLight = true; break;
                    case "Post-Lunatic Cultist": NPC.downedAncientCultist = true; break;
                    case "Post-Lunar Events": NPC.downedTowerNebula = NPC.downedTowerSolar = NPC.downedTowerStardust = NPC.downedTowerVortex = true; break;
                    case "Post-Moon Lord": NPC.downedMoonlord = true; break;
                    case "Post-Desert Scourge": CalamityOnKillDesertScourge(); break;
                    case "Post-Giant Clam": CalamityOnKillGiantClam(false); break;
                    case "Post-Acid Rain Tier 1": CalamityAcidRainTier1Downed(); break;
                    case "Post-Crabulon": CalamityOnKillCrabulon(); break;
                    case "Post-The Hive Mind": CalamityOnKillTheHiveMind(); break;
                    case "Post-The Perforators": CalamityOnKillThePerforators(); break;
                    case "Post-The Slime God": CalamityOnKillTheSlimeGod(); break;
                    case "Post-Dreadnautilus": CalamityDreadnautilusDowned(); break;
                    case "Post-Hardmode Giant Clam": CalamityOnKillGiantClam(true); break;
                    case "Post-Aquatic Scourge": CalamityOnKillAquaticScourge(); break;
                    case "Post-Cragmaw Mire": CalamityOnKillCragmawMire(); break;
                    case "Post-Acid Rain Tier 2": CalamityAcidRainTier2Downed(); break;
                    case "Post-Brimstone Elemental": CalamityOnKillBrimstoneElemental(); break;
                    case "Post-Cryogen": CalamityOnKillCryogen(); break;
                    case "Post-Calamitas Clone": CalamityOnKillCalamitasClone(); break;
                    case "Post-Great Sand Shark": CalamityOnKillGreatSandShark(); break;
                    case "Post-Leviathan and Anahita": CalamityOnKillLeviathanAndAnahita(); break;
                    case "Post-Astrum Aureus": CalamityOnKillAstrumAureus(); break;
                    case "Post-The Plaguebringer Goliath": CalamityOnKillThePlaguebringerGoliath(); break;
                    case "Post-Ravager": CalamityOnKillRavager(); break;
                    case "Post-Astrum Deus": CalamityOnKillAstrumDeus(); break;
                    case "Post-Profaned Guardians": CalamityOnKillProfanedGuardians(); break;
                    case "Post-The Dragonfolly": CalamityOnKillTheDragonfolly(); break;
                    case "Post-Providence, the Profaned Goddess": CalamityOnKillProvidenceTheProfanedGoddess(); break;
                    case "Post-Storm Weaver": CalamityOnKillStormWeaver(); break;
                    case "Post-Ceaseless Void": CalamityOnKillCeaselessVoid(); break;
                    case "Post-Signus, Envoy of the Devourer": CalamityOnKillSignusEnvoyOfTheDevourer(); break;
                    case "Post-Polterghast": CalamityOnKillPolterghast(); break;
                    case "Post-Mauler": CalamityOnKillMauler(); break;
                    case "Post-Nuclear Terror": CalamityOnKillNuclearTerror(); break;
                    case "Post-The Old Duke": CalamityOnKillTheOldDuke(); break;
                    case "Post-The Devourer of Gods": CalamityOnKillTheDevourerOfGods(); break;
                    case "Post-Yharon, Dragon of Rebirth": CalamityOnKillYharonDragonOfRebirth(); break;
                    case "Post-Exo Mechs": CalamityOnKillExoMechs(); break;
                    case "Post-Supreme Witch, Calamitas": CalamityOnKillSupremeWitchCalamitas(); break;
                    case "Post-Adult Eidolon Wyrm": CalamityAdultEidolonWyrmDowned(); break;
                    case "Post-Boss Rush": CalamityBossRushDowned(); break;
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
                    case "Reward: Cosmolight": GiveCosmolight(); break;
                    case "Reward: Diving Helmet": GiveItem(ItemID.DivingHelmet); break;
                    case "Reward: Jellyfish Necklace": GiveItem(ItemID.JellyfishNecklace); break;
                    case "Reward: Corrupt Flask": GiveCorruptFlask(); break;
                    case "Reward: Crimson Flask": GiveCrimsonFlask(); break;
                    case "Reward: Craw Carapace": GiveCrawCarapace(); break;
                    case "Reward: Giant Shell": GiveGiantShell(); break;
                    case "Reward: Fungal Carapace": GiveFungalCarapace(); break;
                    case "Reward: Life Jelly": GiveLifeJelly(); break;
                    case "Reward: Vital Jelly": GiveVitalJelly(); break;
                    case "Reward: Mana Jelly": GiveManaJelly(); break;
                    case "Reward: Giant Tortoise Shell": GiveGiantTortoiseShell(); break;
                    case "Reward: Coin of Deceit": GiveCoinOfDeceit(); break;
                    case "Reward: Ink Bomb": GiveInkBomb(); break;
                    case "Reward: Voltaic Jelly": GiveVoltaicJelly(); break;
                    case "Reward: Wulfrum Battery": GiveWulfrumBattery(); break;
                    case "Reward: Luxor's Gift": GiveLuxorsGift(); break;
                    case "Reward: Raider's Talisman": GiveRaidersTalisman(); break;
                    case "Reward: Rotten Dogtooth": GiveRottenDogtooth(); break;
                    case "Reward: Scuttler's Jewel": GiveScuttlersJewel(); break;
                    case "Reward: Unstable Granite Core": GiveUnstableGraniteCore(); break;
                    case "Reward: Amidias' Spark": GiveAmidiasSpark(); break;
                    case "Reward: Ursa Sergeant": GiveUrsaSergeant(); break;
                    case "Reward: Trinket of Chi": GiveTrinketOfChi(); break;
                    case "Reward: The Transformer": GiveTheTransformer(); break;
                    case "Reward: Rover Drive": GiveRoverDrive(); break;
                    case "Reward: Marnite Repulsion Shield": GiveMarniteRepulsionShield(); break;
                    case "Reward: Frost Barrier": GiveFrostBarrier(); break;
                    case "Reward: Ancient Fossil": GiveAncientFossil(); break;
                    case "Reward: Spelunker's Amulet": GiveSpelunkersAmulet(); break;
                    case "Reward: Fungal Symbiote": GiveFungalSymbiote(); break;
                    case "Reward: Gladiator's Locket": GiveGladiatorsLocket(); break;
                    case "Reward: Wulfrum Acrobatics Pack": GiveWulfrumAcrobaticsPack(); break;
                    case "Reward: Depths Charm": GiveDepthsCharm(); break;
                    case "Reward: Anechoic Plating": GiveAnechoicPlating(); break;
                    case "Reward: Iron Boots": GiveIronBoots(); break;
                    case "Reward: Sprit Glyph": GiveSpritGlyph(); break;
                    case "Reward: Abyssal Amulet": GiveAbyssalAmulet(); break;
                }

                collectedItems++;
            }

            if (ModLoader.GetMod("CalamityMod") != null) CalamityPostUpdateWorld();

            if (victory) return;

            foreach (var goal in goals) if (!collectedLocations.Contains(goal)) return;

            var victoryPacket = new StatusUpdatePacket();
            victoryPacket.Status = ArchipelagoClientState.ClientGoal;
            session.Socket.SendPacket(victoryPacket);

            victory = true;
        }

        void CalamityPostUpdateWorld()
        {
            if (CalamityMod.DownedBossSystem.downedBossRush) QueueLocation("Boss Rush");
        }

        public override void SaveWorldData(TagCompound tag)
        {
            tag["ApCollectedItems"] = collectedItems;
            if (enabled) session.DataStorage[Scope.Slot, "CollectedLocations"] = collectedLocations.ToArray();
        }

        public void Reset()
        {
            typeof(SocialAPI).GetField("_mode", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, SocialMode.Steam);

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
            (false, _) => new string[] {
                @"The world is not connected to Archipelago! Reload the world or run ""/apconnect"".",
                "If you are the host, check your config in the main menu at Workshop > Manage Mods > Config",
                "Or in-game at Settings > Mod Configuration",
            },
            (true, false) => new string[] { @"Archipelago is connected but not enabled. Once everyone's joined, run ""/apstart"" to start it." },
            (true, true) => new string[] { "Archipelago is active!" },
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

            var packet = new SayPacket();
            packet.Text = command;
            session.Socket.SendPacket(packet);

            return true;
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
                locationQueue.Add(session.Locations.ScoutLocationsAsync(new long[] { location }));
                collectedLocations.Add(locationName);
            }

            session.Locations.CompleteLocationChecks(new long[] { location });
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

        void GiveItem(Action<Player> giveItem)
        {
            for (var i = 0; i < Main.maxPlayers; i++)
            {
                var player = Main.player[i];
                if (player.active) giveItem(player);
            }
        }

        void GiveItem(int item) => GiveItem(player => player.QuickSpawnItem(player.GetSource_GiftOrReward(), item, 1));
        void GiveItem<T>() where T : ModItem => GiveItem(ModContent.ItemType<T>());

        int[] baseCoins = new int[] { 15, 20, 25, 30, 40, 50, 70, 100 };

        void GiveCoins()
        {
            var flagCount = 0;
            foreach (var flag in flags) if (CheckFlag(flag)) flagCount++;
            var count = baseCoins[flagCount % 8] * (int)Math.Pow(10, flagCount / 8);

            var platinum = count / 10000;
            var gold = (count % 10000) / 100;
            var silver = count % 100;
            GiveItem(player =>
            {
                if (platinum > 0) player.QuickSpawnItem(player.GetSource_GiftOrReward(), ItemID.PlatinumCoin, platinum);
                if (gold > 0) player.QuickSpawnItem(player.GetSource_GiftOrReward(), ItemID.GoldCoin, gold);
                if (silver > 0) player.QuickSpawnItem(player.GetSource_GiftOrReward(), ItemID.SilverCoin, silver);
            });
        }

        void StartHardmode()
        {
            WorldGen.StartHardmode();

            if (ModLoader.GetMod("CalamityMod") != null) CalamityStartHardmode();
        }

        void CalamityStartHardmode()
        {
            CalamityMod.NPCs.CalamityGlobalNPC.SetNewShopVariable(new int[]
            {
                17,
                19,
                20,
                227,
                228,
                353,
                207,
                38,
                208,
                54,
                453,
                633,
                0
            }, false);

            if (!CalamityMod.CalamityConfig.Instance.EarlyHardmodeProgressionRework) return;

            WorldGen.altarCount++;
            CalamityMod.CalamityUtils.SpawnOre(107, 0.00012, 0.45f, 0.7f, 3, 8, Array.Empty<int>());
            CalamityMod.CalamityUtils.SpawnOre(221, 0.00012, 0.45f, 0.7f, 3, 8, Array.Empty<int>());
        }

        void GiveCosmolight() => GiveItem<CalamityMod.Items.Tools.ClimateChange.Cosmolight>();

        void GiveCorruptFlask() => GiveItem<CalamityMod.Items.Accessories.CorruptFlask>();
        void GiveCrimsonFlask() => GiveItem<CalamityMod.Items.Accessories.CrimsonFlask>();
        void GiveCrawCarapace() => GiveItem<CalamityMod.Items.Accessories.CrawCarapace>();
        void GiveGiantShell() => GiveItem<CalamityMod.Items.Accessories.GiantShell>();
        void GiveFungalCarapace() => GiveItem<CalamityMod.Items.Accessories.FungalCarapace>();
        void GiveLifeJelly() => GiveItem<CalamityMod.Items.Accessories.LifeJelly>();
        void GiveVitalJelly() => GiveItem<CalamityMod.Items.Accessories.VitalJelly>();
        void GiveManaJelly() => GiveItem<CalamityMod.Items.Accessories.ManaJelly>();
        void GiveGiantTortoiseShell() => GiveItem<CalamityMod.Items.Accessories.GiantTortoiseShell>();
        void GiveCoinOfDeceit() => GiveItem<CalamityMod.Items.Accessories.CoinofDeceit>();
        void GiveInkBomb() => GiveItem<CalamityMod.Items.Accessories.InkBomb>();
        void GiveVoltaicJelly() => GiveItem<CalamityMod.Items.Accessories.VoltaicJelly>();
        void GiveWulfrumBattery() => GiveItem<CalamityMod.Items.Accessories.WulfrumBattery>();
        void GiveLuxorsGift() => GiveItem<CalamityMod.Items.Accessories.LuxorsGift>();
        void GiveRaidersTalisman() => GiveItem<CalamityMod.Items.Accessories.RaidersTalisman>();
        void GiveRottenDogtooth() => GiveItem<CalamityMod.Items.Accessories.RottenDogtooth>();
        void GiveScuttlersJewel() => GiveItem<CalamityMod.Items.Accessories.ScuttlersJewel>();
        void GiveUnstableGraniteCore() => GiveItem<CalamityMod.Items.Accessories.UnstableGraniteCore>();
        void GiveAmidiasSpark() => GiveItem<CalamityMod.Items.Accessories.AmidiasSpark>();
        void GiveUrsaSergeant() => GiveItem<CalamityMod.Items.Fishing.AstralCatches.UrsaSergeant>();
        void GiveTrinketOfChi() => GiveItem<CalamityMod.Items.Accessories.TrinketofChi>();
        void GiveTheTransformer() => GiveItem<CalamityMod.Items.Accessories.TheTransformer>();
        void GiveRoverDrive() => GiveItem<CalamityMod.Items.Accessories.RoverDrive>();
        void GiveMarniteRepulsionShield() => GiveItem<CalamityMod.Items.Accessories.MarniteRepulsionShield>();
        void GiveFrostBarrier() => GiveItem<CalamityMod.Items.Accessories.FrostBarrier>();
        void GiveAncientFossil() => GiveItem<CalamityMod.Items.Accessories.AncientFossil>();
        void GiveSpelunkersAmulet() => GiveItem<CalamityMod.Items.Accessories.SpelunkersAmulet>();
        void GiveFungalSymbiote() => GiveItem<CalamityMod.Items.Accessories.FungalSymbiote>();
        void GiveGladiatorsLocket() => GiveItem<CalamityMod.Items.Accessories.GladiatorsLocket>();
        void GiveWulfrumAcrobaticsPack() => GiveItem<CalamityMod.Items.Accessories.WulfrumAcrobaticsPack>();
        void GiveDepthsCharm() => GiveItem<CalamityMod.Items.Accessories.DepthCharm>();
        void GiveAnechoicPlating() => GiveItem<CalamityMod.Items.Accessories.AnechoicPlating>();
        void GiveIronBoots() => GiveItem<CalamityMod.Items.Accessories.IronBoots>();
        void GiveSpritGlyph() => GiveItem<CalamityMod.Items.Accessories.SpiritGlyph>();
        void GiveAbyssalAmulet() => GiveItem<CalamityMod.Items.Accessories.AbyssalAmulet>();

        void CalamityOnKill(object obj, MethodInfo method)
        {
            var seldomArchipelago = ModContent.GetInstance<SeldomArchipelago>();
            seldomArchipelago.temp = true;
            method.Invoke(obj, new object[] { });
            seldomArchipelago.temp = false;
        }

        void CalamityOnKillGiantClam(bool hardmode)
        {
            var downed = hardmode ? CalamityMod.DownedBossSystem.downedCLAM : CalamityMod.DownedBossSystem.downedCLAMHardMode;
            var isHardmode = Main.hardMode;
            Main.hardMode = hardmode;
            CalamityOnKill(new CalamityMod.NPCs.SunkenSea.GiantClam(), SeldomArchipelago.giantClamOnKill);
            if (hardmode) CalamityMod.DownedBossSystem.downedCLAM = downed;
            else CalamityMod.DownedBossSystem.downedCLAMHardMode = downed;
            Main.hardMode = isHardmode;
        }

        void CalamityOnKillLeviathanAndAnahita()
        {
            var seldomArchipelago = ModContent.GetInstance<SeldomArchipelago>();
            seldomArchipelago.temp = true;
            SeldomArchipelago.leviathanRealOnKill.Invoke(new CalamityMod.NPCs.Leviathan.Leviathan(), new object[] { null });
            seldomArchipelago.temp = false;
        }

        void CalamityAcidRainTier1Downed() => CalamityMod.DownedBossSystem.downedEoCAcidRain = true;
        void CalamityDreadnautilusDowned() => CalamityMod.DownedBossSystem.downedDreadnautilus = true;
        void CalamityAcidRainTier2Downed() => CalamityMod.DownedBossSystem.downedAquaticScourgeAcidRain = true;
        void CalamityAdultEidolonWyrmDowned() => CalamityMod.DownedBossSystem.downedAdultEidolonWyrm = true;
        void CalamityBossRushDowned() => CalamityMod.DownedBossSystem.downedBossRush = true;

        void CalamityOnKillDesertScourge() => CalamityOnKill(new CalamityMod.NPCs.DesertScourge.DesertScourgeHead(), SeldomArchipelago.desertScourgeHeadOnKill);
        void CalamityOnKillCrabulon() => CalamityOnKill(new CalamityMod.NPCs.Crabulon.Crabulon(), SeldomArchipelago.crabulonOnKill);
        void CalamityOnKillTheHiveMind() => CalamityOnKill(new CalamityMod.NPCs.HiveMind.HiveMind(), SeldomArchipelago.hiveMindOnKill);
        void CalamityOnKillThePerforators() => CalamityOnKill(new CalamityMod.NPCs.Perforator.PerforatorHive(), SeldomArchipelago.perforatorHiveOnKill);
        void CalamityOnKillTheSlimeGod() => CalamityOnKill(new CalamityMod.NPCs.SlimeGod.SlimeGodCore(), SeldomArchipelago.slimeGodCoreOnKill);
        void CalamityOnKillAquaticScourge() => CalamityOnKill(new CalamityMod.NPCs.AquaticScourge.AquaticScourgeHead(), SeldomArchipelago.aquaticScourgeHeadOnKill);
        void CalamityOnKillCragmawMire() => CalamityOnKill(new CalamityMod.NPCs.AcidRain.CragmawMire(), SeldomArchipelago.cragmawMireOnKill);
        void CalamityOnKillBrimstoneElemental() => CalamityOnKill(new CalamityMod.NPCs.BrimstoneElemental.BrimstoneElemental(), SeldomArchipelago.brimstoneElementalOnKill);
        void CalamityOnKillCryogen() => CalamityOnKill(new CalamityMod.NPCs.Cryogen.Cryogen(), SeldomArchipelago.cryogenOnKill);
        void CalamityOnKillCalamitasClone() => CalamityOnKill(new CalamityMod.NPCs.CalClone.CalamitasClone(), SeldomArchipelago.calamitasCloneOnKill);
        void CalamityOnKillGreatSandShark() => CalamityOnKill(new CalamityMod.NPCs.GreatSandShark.GreatSandShark(), SeldomArchipelago.greatSandSharkOnKill);
        void CalamityOnKillAstrumAureus() => CalamityOnKill(new CalamityMod.NPCs.AstrumAureus.AstrumAureus(), SeldomArchipelago.astrumAureusOnKill);
        void CalamityOnKillThePlaguebringerGoliath() => CalamityOnKill(new CalamityMod.NPCs.PlaguebringerGoliath.PlaguebringerGoliath(), SeldomArchipelago.plaguebringerGoliathOnKill);
        void CalamityOnKillRavager() => CalamityOnKill(new CalamityMod.NPCs.Ravager.RavagerBody(), SeldomArchipelago.ravagerBodyOnKill);
        void CalamityOnKillAstrumDeus() => CalamityOnKill(new CalamityMod.NPCs.AstrumDeus.AstrumDeusHead(), SeldomArchipelago.astrumDeusHeadOnKill);
        void CalamityOnKillProfanedGuardians() => CalamityOnKill(new CalamityMod.NPCs.ProfanedGuardians.ProfanedGuardianCommander(), SeldomArchipelago.profanedGuardianCommanderOnKill);
        void CalamityOnKillTheDragonfolly() => CalamityOnKill(new CalamityMod.NPCs.Bumblebirb.Bumblefuck(), SeldomArchipelago.bumblefuckOnKill);
        void CalamityOnKillProvidenceTheProfanedGoddess() => CalamityOnKill(new CalamityMod.NPCs.Providence.Providence(), SeldomArchipelago.providenceOnKill);
        void CalamityOnKillStormWeaver() => CalamityOnKill(new CalamityMod.NPCs.StormWeaver.StormWeaverHead(), SeldomArchipelago.stormWeaverHeadOnKill);
        void CalamityOnKillCeaselessVoid() => CalamityOnKill(new CalamityMod.NPCs.CeaselessVoid.CeaselessVoid(), SeldomArchipelago.ceaselessVoidOnKill);
        void CalamityOnKillSignusEnvoyOfTheDevourer() => CalamityOnKill(new CalamityMod.NPCs.Signus.Signus(), SeldomArchipelago.signusOnKill);
        void CalamityOnKillPolterghast() => CalamityOnKill(new CalamityMod.NPCs.Polterghast.Polterghast(), SeldomArchipelago.polterghastOnKill);
        void CalamityOnKillMauler() => CalamityOnKill(new CalamityMod.NPCs.AcidRain.Mauler(), SeldomArchipelago.maulerOnKill);
        void CalamityOnKillNuclearTerror() => CalamityOnKill(new CalamityMod.NPCs.AcidRain.NuclearTerror(), SeldomArchipelago.nuclearTerrorOnKill);
        void CalamityOnKillTheOldDuke() => CalamityOnKill(new CalamityMod.NPCs.OldDuke.OldDuke(), SeldomArchipelago.oldDukeOnKill);
        void CalamityOnKillTheDevourerOfGods() => CalamityOnKill(new CalamityMod.NPCs.DevourerofGods.DevourerofGodsHead(), SeldomArchipelago.devourerofGodsHeadOnKill);
        void CalamityOnKillYharonDragonOfRebirth() => CalamityOnKill(new CalamityMod.NPCs.Yharon.Yharon(), SeldomArchipelago.yharonOnKill);
        void CalamityOnKillExoMechs() => CalamityOnKill(new CalamityMod.NPCs.ExoMechs.Ares.AresBody(), SeldomArchipelago.aresBodyDoMiscDeathEffects);
        void CalamityOnKillSupremeWitchCalamitas() => CalamityOnKill(new CalamityMod.NPCs.SupremeCalamitas.SupremeCalamitas(), SeldomArchipelago.supremeCalamitasOnKill);
    }
}
