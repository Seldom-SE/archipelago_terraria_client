using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;
using Microsoft.Xna.Framework;
using Color = Microsoft.Xna.Framework.Color;
using Newtonsoft.Json.Linq;
using SeldomArchipelago.Players;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
using SeldomArchipelago.FlagItem;
using System.Linq;
using SeldomArchipelago.NPCs;
using System.Diagnostics.Metrics;
using Terraria.GameContent.UI.States;

namespace SeldomArchipelago.Systems
{
    class ArchipelagoSystem : ModSystem
    {
        // Data that's reset between worlds
        public class WorldState
        {
            // Achievements can be completed while loading into the world, but those complete before
            // `ArchipelagoPlayer::OnEnterWorld`, where achievements are reset, is run. So, this
            // keeps track of which achievements have been completed since `OnWorldLoad` was run, so
            // `ArchipelagoPlayer` knows not to clear them.
            public List<string> achieved = new List<string>();
            // Stores locations that were collected before Archipelago is started so they can be
            // queued once it's started
            public List<string> locationBacklog = new List<string>();
            // Number of items the player has collected in this world
            public int collectedItems;
            // List of rewards received in this world, so they don't get reapplied. Saved in the
            // Terraria world instead of Archipelago data in case the player is, for example,
            // playing Hardcore and wants to receive all the rewards again when making a new player/
            // world.
            public List<int> receivedRewards = new List<int>();
            // List of flags that have been received but not triggered
            public List<string> suspendedFlags = new List<string>();
            // All NPCs that have been randomized.
            public ImmutableHashSet<int> randomizedNPCs = null;
            // Set of town NPC items received in this world. Since this is saved to the world and
            // modded NPC IDs are not stable, the type will need to change if Calamity NPC support
            // is added.
            public HashSet<int> receivedNPCs = new();
            // Contains all ghosts that are available to spawn.
            public Queue<int> ghostNPCqueue = new();
            // Dict of loc npc ids to item npc ids, if a player's npc item happens to be placed in one of their npc locations.
            // If this is the case, we can transform the ghost/bound npc into the item npc as soon as it is activated, for both expediency and cuteness.
            public Dictionary<int, int> npcLocTypeToNpcItemType = null;

            public bool NPCRandoActive() => randomizedNPCs is not null;
        }

        // Data that's reset between Archipelago sessions
        public class SessionState
        {
            // List of locations that are currently being sent
            public List<Task<Dictionary<long, ScoutedItemInfo>>> locationQueue = new List<Task<Dictionary<long, ScoutedItemInfo>>>();
            public ArchipelagoSession session;
            public DeathLinkService deathlink;
            // Like `collectedItems`, but unique to this Archipelago session, and doesn't save, so
            // it starts at 0 each session. While less than `collectedItems`, it discards items
            // instead of collecting them. This is needed bc AP just gives us a list of items that
            // we have, and it's up to us to keep track of which ones we've already applied.
            public int currentItem;
            public List<string> collectedLocations = new List<string>();
            public List<string> goals = new List<string>();

            public bool victory;
            public int slot;
        }

        public WorldState world = new();
        public SessionState session;

        // Contains ghosts that require special housing conditions to spawn.
        public readonly static ImmutableHashSet<int> specialSpawnGhosts =
        [
            NPCID.Truffle
        ];

        public override void LoadWorldData(TagCompound tag)
        {
            world.collectedItems = tag.ContainsKey("ApCollectedItems") ? tag.Get<int>("ApCollectedItems") : 0;
            world.receivedRewards = tag.ContainsKey("ApReceivedRewards") ? tag.Get<List<int>>("ApReceivedRewards") : new();
            world.suspendedFlags = tag.ContainsKey("ApSuspendedFlags") ? tag.Get<List<string>>("ApSuspendedFlags") : new();
            world.receivedNPCs = tag.ContainsKey("ApReceivedNPCs") ? tag.Get<List<int>>("ApReceivedNPCs").ToHashSet() : new();
            world.randomizedNPCs = tag.ContainsKey("ApRandomizedNPCs") ? tag.Get<List<int>>("ApRandomizedNPCs").ToImmutableHashSet() : null;
        }

        public override void OnWorldLoad()
        {
            // Needed for achievements to work right
            typeof(SocialAPI).GetField("_mode", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, SocialMode.None);

            if (Main.netMode == NetmodeID.MultiplayerClient) return;

            var config = ModContent.GetInstance<Config.Config>();

            LoginResult result;
            ArchipelagoSession newSession;
            try
            {
                newSession = ArchipelagoSessionFactory.CreateSession(config.address, config.port);

                result = newSession.TryConnectAndLogin("Terraria", config.name, ItemsHandlingFlags.AllItems, null, null, null, config.password == "" ? null : config.password);
                if (result is LoginFailure)
                {
                    return;
                }
            }
            catch
            {
                return;
            }

            session = new();
            session.session = newSession;

            var locations = session.session.DataStorage[Scope.Slot, "CollectedLocations"].To<String[]>();
            if (locations != null)
            {
                session.collectedLocations = new List<string>(locations);
            }

            var success = (LoginSuccessful)result;
            session.goals = new List<string>(((JArray)success.SlotData["goal"]).ToObject<string[]>());

            session.session.MessageLog.OnMessageReceived += (message) =>
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
                session.deathlink = session.session.CreateDeathLinkService();
                session.deathlink.EnableDeathLink();

                session.deathlink.OnDeathLinkReceived += ReceiveDeathlink;
            }

            string[] randomizedNPCnames = ((JArray)success.SlotData["randomize_npcs"]).ToObject<string[]>();
            if (randomizedNPCnames.Length > 0)
            {
                world.randomizedNPCs = (from name in randomizedNPCnames select npcNameToID[name]).ToImmutableHashSet();
                string[] allNPCnames = npcNameToID.Keys.ToArray();
                var locIDtoNPCname = new Dictionary<long, string>();
                foreach (string loc in allNPCnames)
                {
                    locIDtoNPCname[session.session.Locations.GetLocationIdFromName("Terraria", loc)] = loc;
                }
                if (locIDtoNPCname.ContainsKey(-1))
                {
                    throw new Exception($"Some retrieved NPC locations turned up -1 ids.");
                }
                var task = session.session.Locations.ScoutLocationsAsync(locIDtoNPCname.Keys.ToArray());
                if (task.Wait(1000))
                {
                    world.npcLocTypeToNpcItemType = new();
                    int playerID = success.Slot;
                    var npcLocDict = task.Result;
                    foreach (long key in npcLocDict.Keys)
                    {
                        ItemInfo itemInfo = npcLocDict[key];
                        if (itemInfo.Player.Slot == playerID && allNPCnames.Contains(itemInfo.ItemName))
                        {
                            int npcType = npcNameToID[locIDtoNPCname[key]];
                            world.npcLocTypeToNpcItemType[npcType] = npcNameToID[itemInfo.ItemName];
                        }
                    }
                }

            }

            session.slot = success.Slot;

            foreach (var location in world.locationBacklog) QueueLocation(location);
            world.locationBacklog.Clear();
        }
        public bool LocationCollected(string loc) => session.collectedLocations.Contains(loc) || world.locationBacklog.Contains(loc);

        public static string[] flags = { "Post-King Slime", "Post-Desert Scourge", "Post-Giant Clam", "Post-Eye of Cthulhu", "Post-Acid Rain Tier 1", "Post-Crabulon", "Post-Evil Boss", "Post-Old One's Army Tier 1", "Post-Goblin Army", "Post-Queen Bee", "Post-The Hive Mind", "Post-The Perforators", "Post-Skeletron", "Post-Deerclops", "Post-The Slime God", "Hardmode", "Post-Dreadnautilus", "Post-Hardmode Giant Clam", "Post-Pirate Invasion", "Post-Queen Slime", "Post-Aquatic Scourge", "Post-Cragmaw Mire", "Post-Acid Rain Tier 2", "Post-The Twins", "Post-Old One's Army Tier 2", "Post-Brimstone Elemental", "Post-The Destroyer", "Post-Cryogen", "Post-Skeletron Prime", "Post-Calamitas Clone", "Post-Plantera", "Post-Great Sand Shark", "Post-Leviathan and Anahita", "Post-Astrum Aureus", "Post-Golem", "Post-Old One's Army Tier 3", "Post-Martian Madness", "Post-The Plaguebringer Goliath", "Post-Duke Fishron", "Post-Mourning Wood", "Post-Pumpking", "Post-Everscream", "Post-Santa-NK1", "Post-Ice Queen", "Post-Frost Legion", "Post-Ravager", "Post-Empress of Light", "Post-Lunatic Cultist", "Post-Astrum Deus", "Post-Lunar Events", "Post-Moon Lord", "Post-Profaned Guardians", "Post-The Dragonfolly", "Post-Providence, the Profaned Goddess", "Post-Storm Weaver", "Post-Ceaseless Void", "Post-Signus, Envoy of the Devourer", "Post-Polterghast", "Post-Mauler", "Post-Nuclear Terror", "Post-The Old Duke", "Post-The Devourer of Gods", "Post-Yharon, Dragon of Rebirth", "Post-Exo Mechs", "Post-Supreme Witch, Calamitas", "Post-Primordial Wyrm", "Post-Boss Rush" };

        public static bool FindFlag(string flag, out string fuzzy)
        {
            fuzzy = null;
            if (flags.Contains(flag))
            {
                return true;
            }
            else
            {
                string lowerItem = flag.ToLower();
                int assumedItemIndex = Array.FindIndex(flags, x => x.ToLower().Contains(lowerItem));
                if (assumedItemIndex > -1)
                {
                    fuzzy = flags[assumedItemIndex];
                    return true;
                }
                return false;

            }
        }

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
        public static Dictionary<string, int> npcNameToID = new()
            {
                {"Guide", NPCID.Guide },
                {"Merchant", NPCID.Merchant },
                {"Nurse", NPCID.Nurse },
                {"Demolitionist", NPCID.Demolitionist },
                {"Dye Trader", NPCID.DyeTrader },
                {"Angler", NPCID.Angler },
                {"Zoologist", NPCID.BestiaryGirl },
                {"Dryad", NPCID.Dryad },
                {"Painter", NPCID.Painter },
                {"Golfer", NPCID.Golfer },
                {"Arms Dealer", NPCID.ArmsDealer },
                {"Tavernkeep", NPCID.DD2Bartender },
                {"Stylist", NPCID.Stylist },
                {"Goblin Tinkerer", NPCID.GoblinTinkerer },
                {"Witch Doctor", NPCID.WitchDoctor },
                {"Clothier", NPCID.Clothier },
                {"Mechanic", NPCID.Mechanic },
                {"Party Girl", NPCID.PartyGirl },
                {"Wizard", NPCID.Wizard },
                {"Tax Collector", NPCID.TaxCollector },
                {"Truffle", NPCID.Truffle },
                {"Pirate", NPCID.Pirate },
                {"Steampunker", NPCID.Steampunker },
                {"Cyborg", NPCID.Cyborg },
                {"Santa Claus", NPCID.SantaClaus },
                {"Princess", NPCID.Princess },
            };
        public static Dictionary<int, string> npcIDtoName = npcNameToID.ToDictionary(x => x.Value, x => x.Key);
        public void Collect(string item, bool bypassStarterConfigCheck = false)
        {
            if (npcNameToID.ContainsKey(item))
            {
                world.receivedNPCs.Add(npcNameToID[item]);
                return;
            }
            if (!bypassStarterConfigCheck && ModContent.GetInstance<Config.Config>().manualFlags.Contains(item))
            {
                GiveItem(null, (Player player) =>
                {
                    Item flagStarter = new Item(ModContent.ItemType<FlagStarter>());
                    FlagStarter flagStarterMod = flagStarter.ModItem as FlagStarter;
                    flagStarterMod.FlagName = item;
                    player.QuickSpawnItem(player.GetSource_GiftOrReward(), flagStarter, 1);
                    Main.NewText($"Flag Starter for {item} received! If you ever lose a flagstarter item, use '/apflagstart' and '/apflagstart list'.");
                });
                world.suspendedFlags.Add(item);
                return;
            } else
            {
                world.suspendedFlags.Remove(item);
            }
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
                case "Hardmode": ActivateHardmode(); break;
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
                case "Reward: Life Crystal": GiveItem(ItemID.LifeCrystal); break;
                case "Reward: Enchanted Sword": GiveItem(ItemID.EnchantedSword); break;
                case "Reward: Starfury": GiveItem(ItemID.Starfury); break;
                case "Reward: Defender Medal": GiveItem(ItemID.DefenderMedal); break;
                case null: break;
                default: Chat($"Received unknown item: {item}"); break;
                    
            }
        }
        public override void PostUpdateWorld()
        {
            if (session == null) return;

            if (!session.session.Socket.Connected)
            {
                Chat("Disconnected from Archipelago. Reload the world to reconnect.");
                session = null;
                return;
            }

            if (world.NPCRandoActive() && !world.receivedNPCs.Contains(NPCID.Guide))
            {
                int guideIndex = NPC.FindFirstNPC(NPCID.Guide);
                if (guideIndex != -1)
                {
                    Main.npc[guideIndex].Transform(ModContent.GetInstance<GhostNPC>().Type);
                    GhostNPC ghost = Main.npc[guideIndex].ModNPC as GhostNPC;
                    ghost.GhostType = NPCID.Guide;
                }
            }

            var unqueue = new List<int>();
            for (var i = 0; i < session.locationQueue.Count; i++)
            {
                var status = session.locationQueue[i].Status;

                if (status switch
                {
                    TaskStatus.RanToCompletion or TaskStatus.Canceled or TaskStatus.Faulted => true,
                    _ => false,
                })
                {
                    if (status == TaskStatus.RanToCompletion) foreach (var item in session.locationQueue[i].Result.Values) Chat($"Sent {item.ItemName} to {session.session.Players.GetPlayerAlias(item.Player)}!");
                    else Chat("Sent an item to a player...but failed to get info about it!");

                    unqueue.Add(i);
                }
            }

            unqueue.Reverse();
            foreach (var i in unqueue) session.locationQueue.RemoveAt(i);

            while (session.session.Items.Any())
            {
                var itemName = session.session.Items.DequeueItem().ItemName;

                if (session.currentItem++ < world.collectedItems) continue;

                Collect(itemName);

                world.collectedItems++;
            }

            if (ModLoader.HasMod("CalamityMod")) ModContent.GetInstance<CalamitySystem>().CalamityPostUpdateWorld();

            if (session.victory) return;

            foreach (var goal in session.goals) if (!session.collectedLocations.Contains(goal)) return;

            var victoryPacket = new StatusUpdatePacket()
            {
                Status = ArchipelagoClientState.ClientGoal,
            };
            session.session.Socket.SendPacket(victoryPacket);

            session.victory = true;
        }

        public override void SaveWorldData(TagCompound tag)
        {
            tag["ApCollectedItems"] = world.collectedItems;
            if (session != null)
            {
                session.session.DataStorage[Scope.Slot, "CollectedLocations"] = session.collectedLocations.ToArray();
            }
            tag["ApReceivedRewards"] = world.receivedRewards;
            tag["ApSuspendedFlags"] = world.suspendedFlags;
            tag["ApReceivedNPCs"] = world.receivedNPCs.ToList();
            if (world.NPCRandoActive())
            {
                tag["ApRandomizedNPCs"] = world.randomizedNPCs.ToList();
            }
        }

        public void Reset()
        {
            typeof(SocialAPI).GetField("_mode", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, SocialMode.Steam);

            if (session != null) session.session.Socket.DisconnectAsync();
            session = null;
        }

        public override void OnWorldUnload()
        {
            world = new();
            Reset();
        }

        public string[] Status() => (session == null) switch
        {
            true => new[] {
                @"The world is not connected to Archipelago! Reload the world to try again.",
                "If you are the host, check your config in the main menu at Workshop > Manage Mods > Config",
            },
            false => (ModContent.GetInstance<CalamitySystem>()) switch
            {
                null => new[] { "Archipelago is active!" },
                _ => new[] {
                    "Archipelago is active!",
                    "Calamity Archipelago detected. If you beat a Calamity boss and it doesn't give you a check, restart your game and beat it again. It is a rare, unsolved bug."
                }
            },
        };

        public bool SendCommand(string command)
        {
            if (session == null) return false;

            var packet = new SayPacket()
            {
                Text = command,
            };
            session.session.Socket.SendPacket(packet);

            return true;
        }

        public string[] DebugInfo()
        {
            var info = new List<string>();

            if (world == null)
            {
                info.Add("The mod thinks you're not in a world, which should never happen");
            }
            else
            {
                info.Add("You are in a world");
                if (world.locationBacklog.Count > 0)
                {
                    info.Add("You have locations in the backlog, which should only be the case if Archipelago is inactive");
                    info.Add($"Location backlog: [{string.Join("; ", world.locationBacklog)}]");
                }
                else
                {
                    info.Add("No locations in the backlog, which is usually normal");
                }

                info.Add($"You've collected {world.collectedItems} items");
                info.Add($"NPC randomization is {(world.NPCRandoActive() ? "en" : "dis")}abled");
                info.Add($"Received NPC IDs: [{string.Join(", ", world.receivedNPCs)}]");
            }

            if (session == null)
            {
                info.Add("You're not connected to Archipelago");
            }
            else
            {
                if (session.session.Socket.Connected)
                {
                    info.Add("You're connected to Archipelago");
                }
                else
                {
                    info.Add("You're not connected to Archipelago, but the mod thinks you are");
                }

                if (session.locationQueue.Count > 0)
                {
                    info.Add($"You have locations queued for sending. In normal circumstances, these locations will be sent ASAP.");

                    var statuses = new List<string>();
                    foreach (var location in session.locationQueue) statuses.Add(location.Status switch
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

                info.Add($"DeathLink is {(session.deathlink == null ? "dis" : "en")}abled");
                info.Add($"{session.currentItem} items have been applied");
                info.Add($"Collected locations: [{string.Join("; ", session.collectedLocations)}]");
                info.Add($"Goals: [{string.Join("; ", session.goals)}]");
                info.Add($"Victory has {(session.victory ? "been achieved! Hooray!" : "not been achieved. Alas.")}");
                info.Add($"You are slot {session.slot}");
            }

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
            if (session == null)
            {
                world.locationBacklog.Add(locationName);
                return;
            }

            var location = session.session.Locations.GetLocationIdFromName("Terraria", locationName);
            if (location == -1 || !session.session.Locations.AllMissingLocations.Contains(location)) return;

            if (!session.collectedLocations.Contains(locationName))
            {
                session.locationQueue.Add(session.session.Locations.ScoutLocationsAsync(new[] { location }));
                session.collectedLocations.Add(locationName);
            }

            session.session.Locations.CompleteLocationChecks(new[] { location });
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
            world.achieved.Add(achievement);
        }

        public List<string> GetAchieved()
        {
            return world.achieved;
        }

        public void TriggerDeathlink(string message, int player)
        {
            if (session?.deathlink == null) return;

            var death = new DeathLink(session.session.Players.GetPlayerAlias(session.slot), message);
            session.deathlink.SendDeathLink(death);
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
        public static void ActivateHardmode()
        {
            if (Main.hardMode) return;
            ArchipelagoSystem.BossFlag(NPCID.WallofFlesh);
            WorldGen.StartHardmode();
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

        static void BossFlag(int boss)
        {
            if (ModLoader.HasMod("CalamityMod")) ModContent.GetInstance<CalamitySystem>().VanillaBossKilled(boss);
        }

        void GiveItem(int? item, Action<Player> giveItem)
        {
            if (item != null) world.receivedRewards.Add(item.Value);

            for (var i = 0; i < Main.maxPlayers; i++)
            {
                var player = Main.player[i];
                if (player.active)
                {
                    giveItem(player);
                    if (item != null)
                    {
                        if (Main.netMode == NetmodeID.Server)
                        {
                            var packet = ModContent.GetInstance<SeldomArchipelago>().GetPacket();
                            packet.Write("YouGotAnItem");
                            packet.Write(item.Value);
                            packet.Send(i);
                        }
                        else player.GetModPlayer<ArchipelagoPlayer>().ReceivedReward(item.Value);
                    }
                }
            }
        }

        void GiveItem(int item) => GiveItem(item, player => player.QuickSpawnItem(player.GetSource_GiftOrReward(), item, 1));
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
            GiveItem(null, player =>
            {
                if (platinum > 0) player.QuickSpawnItem(player.GetSource_GiftOrReward(), ItemID.PlatinumCoin, platinum);
                if (gold > 0) player.QuickSpawnItem(player.GetSource_GiftOrReward(), ItemID.GoldCoin, gold);
                if (silver > 0) player.QuickSpawnItem(player.GetSource_GiftOrReward(), ItemID.SilverCoin, silver);
            });
        }

        public List<int> ReceivedRewards() => world.receivedRewards;

        public override void ModifyHardmodeTasks(List<GenPass> list)
        {
            // If all mech boss flags are collected, but not Hardmode, there was no Hallow when
            // hallowed ore was generated, so no ore was generated. So, we generate new ore if this
            // is the case.
            list.Add(new PassLegacy("Hallowed Ore", (progress, config) =>
            {
                if (ModLoader.HasMod("CalamityMod")) ModContent.GetInstance<CalamitySystem>().CalamityStartHardmode();
            }));
        }
    }
}
