using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Chat;
using Terraria.GameContent.Events;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace SeldomArchipelago.System
{
    public class Archipelago : ModSystem
    {
        static bool verbose = false;

        class BossChecker
        {
            string location;
            Func<bool> checker;

            public BossChecker(string location, Func<bool> checker)
            {
                this.location = location;
                this.checker = checker;
            }

            public void Check()
            {
                if (!session.Locations.AllLocationsChecked.Contains(session.Locations.GetLocationIdFromName("Terraria", location)) && checker())
                {
                    DebugLog($"Check completed at {location}");
                    CompleteLocation(location);
                }
            }
        }

        static ArchipelagoSession session;
        static bool inGame;
        static bool enabled;
        static List<string> ids = new List<string>();
        static string sessionId;
        static int itemCount;
        static List<string> messages;
        static List<BossChecker> checkers;

        public override void OnWorldLoad()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;

            checkers = new List<BossChecker> {
                new BossChecker("King Slime", () => NPC.downedSlimeKing),
                new BossChecker("Eye of Cthulhu", () => NPC.downedBoss1),
                new BossChecker("Eater of Worlds or Brain of Cthulhu", () => NPC.downedBoss2),
                new BossChecker("Queen Bee", () => NPC.downedQueenBee),
                new BossChecker("Skeletron", () => NPC.downedBoss3),
                new BossChecker("Deerclops", () => NPC.downedDeerclops),
                // Wall of Flesh is in WallOfFlesh.cs, because we take control of Main.hardMode
                new BossChecker("Queen Slime", () => NPC.downedQueenSlime),
                new BossChecker("The Twins", () => NPC.downedMechBoss2),
                new BossChecker("The Destroyer", () => NPC.downedMechBoss1),
                new BossChecker("Skeletron Prime", () => NPC.downedMechBoss3),
                new BossChecker("Plantera", () => NPC.downedPlantBoss),
                new BossChecker("Golem", () => NPC.downedGolemBoss),
                new BossChecker("Empress of Light", () => NPC.downedEmpressOfLight),
                new BossChecker("Duke Fishron", () => NPC.downedFishron),
                new BossChecker("Lunatic Cultist", () => NPC.downedAncientCultist),
                new BossChecker("Moon Lord", () => NPC.downedMoonlord),
                new BossChecker("Goblin Army", () => NPC.downedGoblins),
                new BossChecker("Old One's Army Tier 1", () => DD2Event.DownedInvasionT1),
                new BossChecker("Old One's Army Tier 2", () => DD2Event.DownedInvasionT2),
                new BossChecker("Old One's Army Tier 3", () => DD2Event.DownedInvasionT3),
                new BossChecker("Torch God", () => {
                    foreach (var player in Main.player) {
                        if (player.unlockedBiomeTorches)
                            return true;
                    }
                    return false;
                }),
                new BossChecker("Frost Legion", () => NPC.downedFrost),
                new BossChecker("Frost Moon", () => Main.forceXMasForToday),
                new BossChecker("Lunar Events", () => NPC.downedTowers),
                new BossChecker("Martian Madness", () => NPC.downedMartians),
                new BossChecker("Pirate Invasion", () => NPC.downedPirates),
                new BossChecker("Pumpkin Moon", () => Main.forceHalloweenForToday),
            };

            session = null;
            sessionId = null;
            inGame = true;
            itemCount = 0;
            messages = null;

            SeldomArchipelago.BoundGoblinMaySpawn = false;
            SeldomArchipelago.DryadMaySpawn = false;
            SeldomArchipelago.UnconsciousManMaySpawn = false;
            SeldomArchipelago.WitchDoctorMaySpawn = false;
            SeldomArchipelago.DungeonSafe = false;
            SeldomArchipelago.WizardMaySpawn = false;
            SeldomArchipelago.TruffleMaySpawn = false;
            SeldomArchipelago.HardmodeFishing = false;
            SeldomArchipelago.TruffleWormMaySpawn = false;
            SeldomArchipelago.SteampunkerMaySpawn = false;
            SeldomArchipelago.LifeFruitMayGrow = false;
            SeldomArchipelago.OldOnesArmyTier = 1;
            SeldomArchipelago.SolarEclipseMayOccur = false;
            SeldomArchipelago.PlanterasBulbMayGrow = false;
            SeldomArchipelago.CyborgMaySpawn = false;
            SeldomArchipelago.MaySellAutohammer = false;
            SeldomArchipelago.PlanteraDungeonEnemiesMaySpawn = false;
            SeldomArchipelago.BiomeChestUnlockable = false;
            SeldomArchipelago.PlanteraEclipseEnemiesMaySpawn = false;
            SeldomArchipelago.GolemMaySpawn = false;
            SeldomArchipelago.PrismaticLacewingMaySpawn = false;
            SeldomArchipelago.MartianProbeMaySpawn = false;
            SeldomArchipelago.CultistsMaySpawn = false;

            SeldomArchipelago.UndergroundEvilGenerated = false;
            SeldomArchipelago.HallowGenerated = false;

            var config = ModContent.GetInstance<Config.Config>();
            session = ArchipelagoSessionFactory.CreateSession(config.address, config.port);
            LoginResult result;

            bool connectionError = false;

            try
            {
                result = session.TryConnectAndLogin("Terraria", config.name, ItemsHandlingFlags.AllItems);

                if (result is LoginFailure failure)
                {
                    connectionError = true;
                    messages = new List<string>(failure.Errors);
                    session = null;
                    DebugLog("LoginFailure received", true);
                }
                else
                {
                    string id = session.DataStorage[Scope.Game, "SessionId"];
                    if (id == null)
                    {
                        sessionId = Guid.NewGuid().ToString();
                        session.DataStorage[Scope.Game, "SessionId"] = sessionId;
                    }
                    else
                    {
                        sessionId = id;
                    }

                    if (ids.Contains(sessionId))
                    {
                        enabled = true;
                    }
                    else if (enabled)
                    {
                        ids.Add(sessionId);
                    }
                    else
                    {
                        messages = new List<string> {
                            "This world is not part of your Archipelago instance, so checks will not be sent or received",
                            @"To add this world to your instance, run ""/apstart"""
                        };
                    }
                }
            }
            catch (Exception e)
            {
                connectionError = true;
                messages = new List<string> { e.ToString() };
                DebugLog("An exception was raised when trying to connect", true);
            }

            if (connectionError)
            {
                messages.Add($"Failed to connect to Archipelago server as {config.name}");
                messages.Add("Perhaps check your config in Workshop > Manage Mods > Config?");
                messages.Add("Reload the world to try again");
            }
            else if (messages == null)
            {
                messages = new List<string> { $"Connected to Archipelago server as {config.name}!" };
            }
        }

        public override void LoadWorldData(TagCompound tag)
        {
            SeldomArchipelago.UndergroundEvilGenerated = tag.ContainsKey("UndergroundEvilGenerated") ? tag.GetBool("UndergroundEvilGenerated") : false;
            SeldomArchipelago.HallowGenerated = tag.ContainsKey("HallowGenerated") ? tag.GetBool("HallowGenerated") : false;
            enabled = tag.ContainsKey("SeekingArchipelago") ? tag.GetBool("SeekingArchipelago") : false;
            ids = tag.ContainsKey("ArchipelagoIds") ? tag.Get<List<string>>("ArchipelagoIds") : new List<string>();
        }

        public override void SaveWorldData(TagCompound tag)
        {
            tag["UndergroundEvilGenerated"] = SeldomArchipelago.UndergroundEvilGenerated;
            tag["HallowGenerated"] = SeldomArchipelago.HallowGenerated;
            tag["SeekingArchipelago"] = !inGame;
            tag["ArchipelagoIds"] = ids;
        }

        public override void PostUpdateWorld()
        {
            if (messages != null)
            {
                foreach (string message in messages)
                {
                    Chat(message);
                }

                messages = null;
            }

            if (session == null) return;

            if (!session.Socket.Connected)
            {
                Chat("Disconnected from Archipelago. Reload the world to reconnect.");
                session = null;
                return;
            }

            if (!enabled) return;

            var items = session.Items.AllItemsReceived;
            while (itemCount < items.Count)
            {
                var item = session.Items.GetItemName(items[itemCount++].Item);
                DebugLog($"Processing item: {item}");
                switch (item)
                {
                    case "Bound Goblin": SeldomArchipelago.BoundGoblinMaySpawn = true; break;
                    case "Dryad": SeldomArchipelago.DryadMaySpawn = true; break;
                    case "Progressive Old One's Army":
                        if (SeldomArchipelago.UnconsciousManMaySpawn)
                            SeldomArchipelago.OldOnesArmyTier++;
                        else
                            SeldomArchipelago.UnconsciousManMaySpawn = true;
                        break;
                    case "Witch Doctor": SeldomArchipelago.WitchDoctorMaySpawn = true; break;
                    case "Progressive Dungeon":
                        if (SeldomArchipelago.DungeonSafe)
                            SeldomArchipelago.PlanteraDungeonEnemiesMaySpawn = true;
                        else
                            SeldomArchipelago.DungeonSafe = true;
                        break;
                    case "Hardmode":
                        if (!Main.hardMode)
                            SeldomArchipelago.StartHardmode();
                        break;
                    case "Underground Evil":
                        if (!SeldomArchipelago.UndergroundEvilGenerated)
                            SeldomArchipelago.GenerateUndergroundEvil();
                        break;
                    case "Hallow":
                        if (!SeldomArchipelago.HallowGenerated)
                            SeldomArchipelago.GenerateHallow();
                        break;
                    case "Wizard": SeldomArchipelago.WizardMaySpawn = true; break;
                    case "Truffle": SeldomArchipelago.TruffleMaySpawn = true; break;
                    case "Hardmode Fishing": SeldomArchipelago.HardmodeFishing = true; break;
                    case "Truffle Worm": SeldomArchipelago.TruffleWormMaySpawn = true; break;
                    case "Steampunker": SeldomArchipelago.SteampunkerMaySpawn = true; break;
                    case "Life Fruit": SeldomArchipelago.LifeFruitMayGrow = true; break;
                    case "Solar Eclipse": SeldomArchipelago.SolarEclipseMayOccur = true; break;
                    case "Plantera's Bulb": SeldomArchipelago.PlanterasBulbMayGrow = true; break;
                    case "Cyborg": SeldomArchipelago.CyborgMaySpawn = true; break;
                    case "Autohammer": SeldomArchipelago.MaySellAutohammer = true; break;
                    case "Biome Chests": SeldomArchipelago.BiomeChestUnlockable = true; break;
                    case "Post-Plantera Eclipse": SeldomArchipelago.PlanteraEclipseEnemiesMaySpawn = true; break;
                    case "Lihzahrd Altar": SeldomArchipelago.GolemMaySpawn = true; break;
                    case "Prismatic Lacewing": SeldomArchipelago.PrismaticLacewingMaySpawn = true; break;
                    case "Martian Probe": SeldomArchipelago.MartianProbeMaySpawn = true; break;
                    case "Cultists": SeldomArchipelago.CultistsMaySpawn = true; break;
                }

                Chat($"Obtained {item}!");
            }

            foreach (var checker in checkers)
            {
                checker.Check();
            }
        }

        public override void OnWorldUnload()
        {
            SeldomArchipelago.UndergroundEvilGenerated = false;
            SeldomArchipelago.HallowGenerated = false;
            ids = new List<string>();
            inGame = false;

            if (session == null || !session.Socket.Connected) return;
            session.Socket.Disconnect();
        }

        public static void CompleteLocation(string location)
        {
            if (verbose) DebugLog($"Sending location: {location}");
            if (session == null || !enabled) return;

            var locationId = session.Locations.GetLocationIdFromName("Terraria", location);
            if (locationId >= 0) session.Locations.CompleteLocationChecks(locationId);
        }

        public static bool Enable()
        {
            if (session == null || !session.Socket.Connected || sessionId == null) return false;

            if (!ids.Contains(sessionId))
            {
                ids.Add(sessionId);
            }

            enabled = true;
            return true;
        }

        public static void DebugLog(string message, bool preLoad = false)
        {
            if (verbose)
            {
                Chat(message);
                ModContent.GetInstance<SeldomArchipelago>().Logger.Debug(message);

                if (preLoad)
                {
                    if (messages == null) messages = new List<string>();
                    messages.Add(message);
                }
            }
        }

        static void Chat(string message)
        {
            if (Main.netMode == NetmodeID.Server)
            {
                ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral(message), Color.White);
                Console.WriteLine(message);
            }
            else
                Main.NewText(message);
        }
    }
}
