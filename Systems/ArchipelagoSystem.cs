using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Packets;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Terraria;
using Terraria.Chat;
using Terraria.GameContent.Events;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace SeldomArchipelago.Systems
{
    public class ArchipelagoSystem : ModSystem
    {
        static List<string> locationBacklog = new List<string>();
        static List<Task<LocationInfoPacket>> locationQueue;
        static ArchipelagoSession session;
        static bool enabled;
        static List<string> collectedItems = new List<string>();

        public override void LoadWorldData(TagCompound tag)
        {
            collectedItems = tag.ContainsKey("ApCollectedItems") ? tag.Get<List<string>>("ApCollectedItems") : new List<string>();
        }

        public override void OnWorldLoad()
        {
            locationQueue = new List<Task<LocationInfoPacket>>();

            if (Main.netMode == NetmodeID.MultiplayerClient) return;

            var config = ModContent.GetInstance<Config.Config>();
            session = ArchipelagoSessionFactory.CreateSession(config.address, config.port);

            try
            {
                if (session.TryConnectAndLogin("Terraria", config.name, ItemsHandlingFlags.AllItems) is LoginFailure)
                {
                    session = null;
                }
            }
            catch
            {
                session = null;
            }
        }

        public override void PostUpdateWorld()
        {
            if (session == null) return;

            if (!session.Socket.Connected)
            {
                Chat("Disconnected from Archipelago. Reload the world to reconnect.");
                session = null;
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
                    if (status == TaskStatus.RanToCompletion)
                    {
                        foreach (var item in locationQueue[i].Result.Locations)
                        {
                            Chat($"Sent {session.Items.GetItemName(item.Item)} to {session.Players.GetPlayerAlias(item.Player)}!");
                        }
                    }
                    else
                    {
                        Chat($"Sent an item to a player...but failed to get info about it!");
                    }

                    unqueue.Add(i);
                }
            }

            unqueue.Reverse();
            foreach (var i in unqueue)
            {
                locationQueue.RemoveAt(i);
            }

            while (session.Items.Any())
            {
                var item = session.Items.DequeueItem();
                var itemName = session.Items.GetItemName(item.Item);

                if (collectedItems.Contains(itemName)) continue;

                switch (itemName)
                {
                    case "Torch God's Favor": GiveItem(itemName, ItemID.TorchGodsFavor); break;
                    case "Post-Goblin Army": NPC.downedGoblins = true; break;
                    case "Post-King Slime": NPC.downedSlimeKing = true; break;
                    case "Post-Eye of Cthulhu": NPC.downedBoss1 = true; break;
                    case "Post-Eater of Worlds or Brain of Cthulhu": NPC.downedBoss2 = true; break;
                    case "Post-Old One's Army Tier 1": DD2Event.DownedInvasionT1 = true; break;
                    case "Post-Queen Bee": NPC.downedQueenBee = true; break;
                    case "Post-Skeletron": NPC.downedBoss3 = true; break;
                    case "Post-Deerclops": NPC.downedDeerclops = true; break;
                    case "Hardmode": WorldGen.StartHardmode(); collectedItems.Add("Hardmode"); break;
                    case "Post-Pirate Invasion": NPC.downedPirates = true; break;
                    case "Post-Frost Legion": NPC.downedFrost = true; break;
                    case "Post-Queen Slime": NPC.downedQueenSlime = true; break;
                    case "Post-The Twins": NPC.downedMechBoss1 = NPC.downedMechBossAny = true; break;
                    case "Post-The Destroyer": NPC.downedMechBoss2 = NPC.downedMechBossAny = true; break;
                    case "Post-Skeletron Prime": NPC.downedMechBoss3 = NPC.downedMechBossAny = true; break;
                    case "Post-Old One's Army Tier 2": DD2Event.DownedInvasionT2 = true; break;
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
                    case "Post-Empress of Light": NPC.downedEmpressOfLight = true; break;
                    case "Post-Lunatic Cultist": NPC.downedAncientCultist = true; break;
                    case "Post-Lunar Events": NPC.downedTowerNebula = NPC.downedTowerSolar = NPC.downedTowerStardust = NPC.downedTowerVortex = true; break;
                    case "Post-Moon Lord": NPC.downedMoonlord = true; break;
                    case "Hermes Boots": GiveItem(itemName, ItemID.HermesBoots); break;
                    case "Magic Mirror": GiveItem(itemName, ItemID.MagicMirror); break;
                    case "Cloud in a Bottle": GiveItem(itemName, ItemID.CloudinaBalloon); break;
                    case "Grappling Hook": GiveItem(itemName, ItemID.GrapplingHook); break;
                    case "Climbing Claws": GiveItem(itemName, ItemID.ClimbingClaws); break;
                    case "Fledgling Wings": GiveItem(itemName, ItemID.CreativeWings); break;
                    case "Demon Conch": GiveItem(itemName, ItemID.DemonConch); break;
                    case "Magic Conch": GiveItem(itemName, ItemID.MagicConch); break;
                    case "Anklet of the Wind": GiveItem(itemName, ItemID.AnkletoftheWind); break;
                    case "Aglet": GiveItem(itemName, ItemID.Aglet); break;
                    case "Ice Skates": GiveItem(itemName, ItemID.IceSkates); break;
                    case "Lava Charm": GiveItem(itemName, ItemID.LavaCharm); break;
                    case "Obsidian Rose": GiveItem(itemName, ItemID.ObsidianRose); break;
                    case "Nature's Gift": GiveItem(itemName, ItemID.NaturesGift); break;
                    case "Feral Claws": GiveItem(itemName, ItemID.FeralClaws); break;
                    case "Magma Stone": GiveItem(itemName, ItemID.MagmaStone); break;
                    case "Shark Tooth Necklace": GiveItem(itemName, ItemID.SharkToothNecklace); break;
                    case "Cobalt Shield": GiveItem(itemName, ItemID.CobaltShield); break;
                    case "Band of Regeneration": GiveItem(itemName, ItemID.BandofRegeneration); break;
                    case "Philosopher's Stone": GiveItem(itemName, ItemID.PhilosophersStone); break;
                    case "Cross Necklace": GiveItem(itemName, ItemID.CrossNecklace); break;
                    case "Magic Quiver": GiveItem(itemName, ItemID.MagicQuiver); break;
                    case "Rifle Scope": GiveItem(itemName, ItemID.RifleScope); break;
                    case "Celestial Magnet": GiveItem(itemName, ItemID.CelestialMagnet); break;
                    case "Rod of Discord": GiveItem(itemName, ItemID.RodofDiscord); break;
                    case "Flying Carpet": GiveItem(itemName, ItemID.FlyingCarpet); break;
                    case "Lifeform Analyzer": GiveItem(itemName, ItemID.LifeformAnalyzer); break;
                    case "Ancient Chisel": GiveItem(itemName, ItemID.AncientChisel); break;
                    case "Moon Charm": GiveItem(itemName, ItemID.MoonCharm); break;
                    case "Neptune's Shell": GiveItem(itemName, ItemID.NeptunesShell); break;
                    case "Shoe Spikes": GiveItem(itemName, ItemID.ShoeSpikes); break;
                    case "Tabi": GiveItem(itemName, ItemID.Tabi); break;
                    case "Black Belt": GiveItem(itemName, ItemID.BlackBelt); break;
                    case "Flesh Knuckles": GiveItem(itemName, ItemID.FleshKnuckles); break;
                    case "Putrid Scent": GiveItem(itemName, ItemID.PutridScent); break;
                    case "Paladin's Shield": GiveItem(itemName, ItemID.PaladinsShield); break;
                    case "Frozen Turtle Shell": GiveItem(itemName, ItemID.FrozenTurtleShell); break;
                    case "Star Cloak": GiveItem(itemName, ItemID.StarCloak); break;
                    case "Discount Card": GiveItem(itemName, ItemID.DiscountCard); break;
                    case "Red Counterweight": GiveItem(itemName, ItemID.RedCounterweight); break;
                    case "Yoyo Glove": GiveItem(itemName, ItemID.YoYoGlove); break;
                    case "Depth Meter": GiveItem(itemName, ItemID.DepthMeter); break;
                    case "Compass": GiveItem(itemName, ItemID.Compass); break;
                    case "Radar": GiveItem(itemName, ItemID.Radar); break;
                    case "DPS Meter": GiveItem(itemName, ItemID.DPSMeter); break;
                    case "Metal Detector": GiveItem(itemName, ItemID.MetalDetector); break;
                    case "Sextant": GiveItem(itemName, ItemID.Sextant); break;
                    case "Stopwatch": GiveItem(itemName, ItemID.Stopwatch); break;
                    case "Tally Counter": GiveItem(itemName, ItemID.TallyCounter); break;
                    case "Fisherman's Pocket Guide": GiveItem(itemName, ItemID.FishermansGuide); break;
                    case "High Test Fishing Line": GiveItem(itemName, ItemID.HighTestFishingLine); break;
                    case "Angler Earring": GiveItem(itemName, ItemID.AnglerEarring); break;
                    case "Tackle Box": GiveItem(itemName, ItemID.TackleBox); break;
                    case "Lavaproof Fishing Hook": GiveItem(itemName, ItemID.LavaFishingHook); break;
                    case "Weather Radio": GiveItem(itemName, ItemID.WeatherRadio); break;
                    case "Blindfold": GiveItem(itemName, ItemID.Blindfold); break;
                    case "Pocket Mirror": GiveItem(itemName, ItemID.PocketMirror); break;
                    case "Vitamins": GiveItem(itemName, ItemID.Vitamins); break;
                    case "Armor Polish": GiveItem(itemName, ItemID.ArmorPolish); break;
                    case "Adhesive Bandage": GiveItem(itemName, ItemID.AdhesiveBandage); break;
                    case "Bezoar": GiveItem(itemName, ItemID.Bezoar); break;
                    case "Nazar": GiveItem(itemName, ItemID.Nazar); break;
                    case "Megaphone": GiveItem(itemName, ItemID.Megaphone); break;
                    case "Trifold Map": GiveItem(itemName, ItemID.TrifoldMap); break;
                    case "Fast Clock": GiveItem(itemName, ItemID.FastClock); break;
                    case "Brick Layer": GiveItem(itemName, ItemID.BrickLayer); break;
                    case "Extendo Grip": GiveItem(itemName, ItemID.ExtendoGrip); break;
                    case "Paint Sprayer": GiveItem(itemName, ItemID.PaintSprayer); break;
                    case "Portable Cement Mixer": GiveItem(itemName, ItemID.PortableCementMixer); break;
                    case "Treasure Magnet": GiveItem(itemName, ItemID.TreasureMagnet); break;
                    case "Step Stool": GiveItem(itemName, ItemID.PortableStool); break;
                    case "Gold Ring": GiveItem(itemName, ItemID.GoldRing); break;
                    case "Lucky Coin": GiveItem(itemName, ItemID.LuckyCoin); break;
                }

                Chat($"Recieved {itemName} from {session.Players.GetPlayerAlias(item.Player)}!");
            }
        }

        public override void SaveWorldData(TagCompound tag)
        {
            tag["ApCollectedItems"] = collectedItems;
        }

        public override void OnWorldUnload()
        {
            locationBacklog.Clear();
            locationQueue = null;
            session = null;
            enabled = false;
            collectedItems = new List<string>();

            Main.Achievements?.ClearAll();

            if (session == null) return;
            session.Socket.Disconnect();
        }

        public static string[] Status() => Tuple.Create(session != null, enabled) switch
        {
            (false, _) => new string[] {
                "The world is not connected to Archipelago and will need to reload.",
                "If you are the host, check your config in Workshop > Manage Mods > Config."
            },
            (true, false) => new string[] { @"Archipelago is connected but not enabled. Once everyone's joined, run ""/apstart"" to start it." },
            (true, true) => new string[] { "Archipelago is active!" },
        };

        public static bool Enable()
        {
            if (session == null)
            {
                return false;
            }

            enabled = true;

            foreach (var location in locationBacklog)
            {
                QueueLocation(location);
            }
            locationBacklog.Clear();

            return true;
        }

        public static void Chat(string message, Player player = null)
        {
            if (player == null)
            {
                if (Main.netMode == NetmodeID.Server)
                {
                    ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral(message), Color.White);
                    Console.WriteLine(message);
                }
                else Main.NewText(message);
            }
            else ChatHelper.SendChatMessageToClient(NetworkText.FromLiteral(message), Color.White, player.whoAmI);
        }

        public static void Chat(string[] messages, Player player = null)
        {
            foreach (var message in messages)
            {
                Chat(message, player);
            }
        }

        public static void QueueLocation(string locationName)
        {
            if (!enabled)
            {
                locationBacklog.Add(locationName);
                return;
            }

            var location = session.Locations.GetLocationIdFromName("Terraria", locationName);
            session.Locations.CompleteLocationChecks(new long[] { location });
            locationQueue.Add(session.Locations.ScoutLocationsAsync(new long[] { location }));
        }

        public static void QueueLocationClient(string locationName)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                QueueLocation(locationName);
                return;
            }

            var mod = ModContent.GetInstance<SeldomArchipelago>();

            if (mod == null) return;

            var packet = mod.GetPacket();
            packet.Write(locationName);
            packet.Send();
        }

        public static void GiveItem(string itemName, int item)
        {
            foreach (var player in Main.player)
            {
                player.QuickSpawnItem(player.GetSource_GiftOrReward(), item);
            }

            collectedItems.Add(itemName);
        }
    }
}
