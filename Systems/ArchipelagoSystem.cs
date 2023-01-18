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

namespace SeldomArchipelago.Systems
{
    public class ArchipelagoSystem : ModSystem
    {
        static List<Task<LocationInfoPacket>> locationQueue;
        static ArchipelagoSession session;
        static bool enabled;

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

                switch (itemName)
                {
                    case "Torch God's Favor": GiveItem(ItemID.TorchGodsFavor); break;
                    case "Post-Goblin Army": NPC.downedGoblins = true; break;
                    case "Post-King Slime": NPC.downedSlimeKing = true; break;
                    case "Post-Eye of Cthulhu": NPC.downedBoss1 = true; break;
                    case "Post-Eater of Worlds or Brain of Cthulhu": NPC.downedBoss2 = true; break;
                    case "Post-Old One's Army Tier 1": DD2Event.DownedInvasionT1 = true; break;
                    case "Post-Queen Bee": NPC.downedQueenBee = true; break;
                    case "Post-Skeletron": NPC.downedBoss3 = true; break;
                    case "Post-Deerclops": NPC.downedDeerclops = true; break;
                    case "Hardmode": WorldGen.StartHardmode(); break;
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
                    case "Hermes Boots": GiveItem(ItemID.HermesBoots); break;
                    case "Magic Mirror": GiveItem(ItemID.MagicMirror); break;
                    case "Cloud in a Bottle": GiveItem(ItemID.CloudinaBalloon); break;
                    case "Grappling Hook": GiveItem(ItemID.GrapplingHook); break;
                    case "Climbing Claws": GiveItem(ItemID.ClimbingClaws); break;
                    case "Fledgling Wings": GiveItem(ItemID.CreativeWings); break;
                    case "Demon Conch": GiveItem(ItemID.DemonConch); break;
                    case "Magic Conch": GiveItem(ItemID.MagicConch); break;
                    case "Anklet of the Wind": GiveItem(ItemID.AnkletoftheWind); break;
                    case "Aglet": GiveItem(ItemID.Aglet); break;
                    case "Ice Skates": GiveItem(ItemID.IceSkates); break;
                    case "Lava Charm": GiveItem(ItemID.LavaCharm); break;
                    case "Obsidian Rose": GiveItem(ItemID.ObsidianRose); break;
                    case "Nature's Gift": GiveItem(ItemID.NaturesGift); break;
                    case "Feral Claws": GiveItem(ItemID.FeralClaws); break;
                    case "Magma Stone": GiveItem(ItemID.MagmaStone); break;
                    case "Shark Tooth Necklace": GiveItem(ItemID.SharkToothNecklace); break;
                    case "Cobalt Shield": GiveItem(ItemID.CobaltShield); break;
                    case "Band of Regeneration": GiveItem(ItemID.BandofRegeneration); break;
                    case "Philosopher's Stone": GiveItem(ItemID.PhilosophersStone); break;
                    case "Cross Necklace": GiveItem(ItemID.CrossNecklace); break;
                    case "Magic Quiver": GiveItem(ItemID.MagicQuiver); break;
                    case "Rifle Scope": GiveItem(ItemID.RifleScope); break;
                    case "Celestial Magnet": GiveItem(ItemID.CelestialMagnet); break;
                    case "Rod of Discord": GiveItem(ItemID.RodofDiscord); break;
                    case "Flying Carpet": GiveItem(ItemID.FlyingCarpet); break;
                    case "Lifeform Analyzer": GiveItem(ItemID.LifeformAnalyzer); break;
                    case "Ancient Chisel": GiveItem(ItemID.AncientChisel); break;
                    case "Moon Charm": GiveItem(ItemID.MoonCharm); break;
                    case "Neptune's Shell": GiveItem(ItemID.NeptunesShell); break;
                    case "Shoe Spikes": GiveItem(ItemID.ShoeSpikes); break;
                    case "Tabi": GiveItem(ItemID.Tabi); break;
                    case "Black Belt": GiveItem(ItemID.BlackBelt); break;
                    case "Flesh Knuckles": GiveItem(ItemID.FleshKnuckles); break;
                    case "Putrid Scent": GiveItem(ItemID.PutridScent); break;
                    case "Paladin's Shield": GiveItem(ItemID.PaladinsShield); break;
                    case "Frozen Turtle Shell": GiveItem(ItemID.FrozenTurtleShell); break;
                    case "Star Cloak": GiveItem(ItemID.StarCloak); break;
                    case "Discount Card": GiveItem(ItemID.DiscountCard); break;
                    case "Red Counterweight": GiveItem(ItemID.RedCounterweight); break;
                    case "Yoyo Glove": GiveItem(ItemID.YoYoGlove); break;
                    case "Depth Meter": GiveItem(ItemID.DepthMeter); break;
                    case "Compass": GiveItem(ItemID.Compass); break;
                    case "Radar": GiveItem(ItemID.Radar); break;
                    case "DPS Meter": GiveItem(ItemID.DPSMeter); break;
                    case "Metal Detector": GiveItem(ItemID.MetalDetector); break;
                    case "Sextant": GiveItem(ItemID.Sextant); break;
                    case "Stopwatch": GiveItem(ItemID.Stopwatch); break;
                    case "Tally Counter": GiveItem(ItemID.TallyCounter); break;
                    case "Fisherman's Pocket Guide": GiveItem(ItemID.FishermansGuide); break;
                    case "High Test Fishing Line": GiveItem(ItemID.HighTestFishingLine); break;
                    case "Angler Earring": GiveItem(ItemID.AnglerEarring); break;
                    case "Tackle Box": GiveItem(ItemID.TackleBox); break;
                    case "Lavaproof Fishing Hook": GiveItem(ItemID.LavaFishingHook); break;
                    case "Weather Radio": GiveItem(ItemID.WeatherRadio); break;
                    case "Blindfold": GiveItem(ItemID.Blindfold); break;
                    case "Pocket Mirror": GiveItem(ItemID.PocketMirror); break;
                    case "Vitamins": GiveItem(ItemID.Vitamins); break;
                    case "Armor Polish": GiveItem(ItemID.ArmorPolish); break;
                    case "Adhesive Bandage": GiveItem(ItemID.AdhesiveBandage); break;
                    case "Bezoar": GiveItem(ItemID.Bezoar); break;
                    case "Nazar": GiveItem(ItemID.Nazar); break;
                    case "Megaphone": GiveItem(ItemID.Megaphone); break;
                    case "Trifold Map": GiveItem(ItemID.TrifoldMap); break;
                    case "Fast Clock": GiveItem(ItemID.FastClock); break;
                    case "Brick Layer": GiveItem(ItemID.BrickLayer); break;
                    case "Extendo Grip": GiveItem(ItemID.ExtendoGrip); break;
                    case "Paint Sprayer": GiveItem(ItemID.PaintSprayer); break;
                    case "Portable Cement Mixer": GiveItem(ItemID.PortableCementMixer); break;
                    case "Treasure Magnet": GiveItem(ItemID.TreasureMagnet); break;
                    case "Step Stool": GiveItem(ItemID.PortableStool); break;
                    case "Gold Ring": GiveItem(ItemID.GoldRing); break;
                    case "Lucky Coin": GiveItem(ItemID.LuckyCoin); break;
                }

                Chat($"Recieved {itemName} from {session.Players.GetPlayerAlias(item.Player)}!");
            }
        }

        public override void OnWorldUnload()
        {
            locationQueue = null;
            session = null;
            enabled = false;

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
            if (session == null) return;

            var location = session.Locations.GetLocationIdFromName("Terraria", locationName);
            session.Locations.CompleteLocationChecks(new long[] { location });
            locationQueue.Add(session.Locations.ScoutLocationsAsync(new long[] { location }));
        }

        public static void GiveItem(int item)
        {
            foreach (var player in Main.player)
            {
                player.QuickSpawnItem(player.GetSource_GiftOrReward(), item);
            }
        }
    }
}
