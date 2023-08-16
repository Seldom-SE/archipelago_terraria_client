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

        Func<bool>[] flags = new[] { () => NPC.downedSlimeKing, () => NPC.downedBoss1, () => NPC.downedBoss2, () => DD2Event.DownedInvasionT1, () => NPC.downedGoblins, () => NPC.downedQueenBee, () => NPC.downedBoss3, () => NPC.downedDeerclops, () => Main.hardMode, () => NPC.downedPirates, () => NPC.downedQueenSlime, () => NPC.downedMechBoss1, () => NPC.downedMechBoss2, () => NPC.downedMechBoss3, () => NPC.downedPlantBoss, () => NPC.downedGolemBoss, () => DD2Event.DownedInvasionT2, () => NPC.downedMartians, () => NPC.downedFishron, () => NPC.downedHalloweenTree, () => NPC.downedHalloweenKing, () => NPC.downedChristmasTree, () => NPC.downedChristmasSantank, () => NPC.downedChristmasIceQueen, () => NPC.downedFrost, () => NPC.downedEmpressOfLight, () => NPC.downedAncientCultist, () => NPC.downedTowerNebula, () => NPC.downedMoonlord };

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
                    case "Hardmode": WorldGen.StartHardmode(); break;
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
                    case "Reward: Coins":
                        var flagCount = 0;
                        foreach (var flag in flags) if (flag()) flagCount++;
                        var silver = flagCount switch
                        {
                            0 => 15,
                            1 => 20,
                            2 => 25,
                            3 => 30,
                            4 => 40,
                            5 => 50,
                            6 => 70,
                            7 => 100,
                            8 => 150,
                            9 => 200,
                            10 => 250,
                            11 => 300,
                            12 => 400,
                            13 => 500,
                            14 => 700,
                            15 => 1000,
                            16 => 1500,
                            17 => 2000,
                            18 => 2500,
                            19 => 3000,
                            20 => 4000,
                            21 => 5000,
                            22 => 7000,
                            23 => 10000,
                            24 => 15000,
                            25 => 20000,
                            26 => 25000,
                            27 => 30000,
                            28 => 40000,
                            _ => 50000,
                        };
                        GiveItem(ItemID.SilverCoin, silver);
                        break;
                }

                collectedItems++;
            }

            if (victory) return;

            foreach (var goal in goals) if (!collectedLocations.Contains(goal)) return;

            var victoryPacket = new StatusUpdatePacket();
            victoryPacket.Status = ArchipelagoClientState.ClientGoal;
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

        public void GiveItem(int item, int count = 1)
        {
            for (var i = 0; i < Main.maxPlayers; i++)
            {
                var player = Main.player[i];
                if (player.active)
                {
                    if (item == ItemID.SilverCoin)
                    {
                        var platinum = count / 10000;
                        var gold = (count % 10000) / 100;
                        var silver = count % 100;
                        if (platinum > 0) player.QuickSpawnItem(player.GetSource_GiftOrReward(), ItemID.PlatinumCoin, platinum);
                        if (gold > 0) player.QuickSpawnItem(player.GetSource_GiftOrReward(), ItemID.GoldCoin, gold);
                        if (silver > 0) player.QuickSpawnItem(player.GetSource_GiftOrReward(), ItemID.SilverCoin, silver);
                    }
                    else player.QuickSpawnItem(player.GetSource_GiftOrReward(), item, count);
                }
            }
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
    }
}
