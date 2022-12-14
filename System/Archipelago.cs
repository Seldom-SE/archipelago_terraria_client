using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.Events;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace SeldomArchipelago.System
{
    public class Archipelago : ModSystem
    {
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
                    CompleteLocation(location);
                }
            }
        }

        static ArchipelagoSession session;
        static int itemCount;
        static List<string> messages;
        static List<BossChecker> checkers;

        public override void OnWorldLoad()
        {
            if (Main.netMode == 1) return;

            checkers = new List<BossChecker> {
                new BossChecker("King Slime", () => NPC.downedSlimeKing),
                new BossChecker("Eye of Cthulhu", () => NPC.downedBoss1),
                new BossChecker("Eater of Worlds or Brain of Cthulhu", () => NPC.downedBoss2),
                new BossChecker("Queen Bee", () => NPC.downedQueenBee),
                new BossChecker("Skeletron", () => NPC.downedBoss3),
                new BossChecker("Deerclops", () => NPC.downedDeerclops),
                // Wall of Flesh is in WallOfFlesh.cs, because we take control of the Main.hardMode
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

            try
            {
                result = session.TryConnectAndLogin("Terraria", config.name, ItemsHandlingFlags.IncludeStartingInventory);

                if (result is LoginFailure failure)
                {
                    messages = new List<string>(failure.Errors);
                    session = null;
                }
            }
            catch (Exception e)
            {
                messages = new List<string> { e.ToString() };
            }

            if (messages != null)
            {
                messages.Add($"Failed to connect to Archipelago server as {config.name}");
                messages.Add("Perhaps check your config in Workshop > Manage Mods > Config?");
                messages.Add("Reload the world to try again");
            }
            else
            {
                messages = new List<string> { $"Connected to Archipelago server as {config.name}!" };
            }
        }

        public override void LoadWorldData(TagCompound tag)
        {
            SeldomArchipelago.UndergroundEvilGenerated = tag.GetBool("UndergroundEvilGenerated");
            SeldomArchipelago.HallowGenerated = tag.GetBool("HallowGenerated");
        }

        public override void SaveWorldData(TagCompound tag)
        {
            tag["UndergroundEvilGenerated"] = SeldomArchipelago.UndergroundEvilGenerated;
            tag["HallowGenerated"] = SeldomArchipelago.HallowGenerated;
        }

        public override void PostUpdateWorld()
        {
            if (messages != null)
            {
                foreach (string error in messages)
                {
                    Main.NewText(error);
                }

                messages = null;
            }

            if (session == null) return;

            if (!session.Socket.Connected)
            {
                Main.NewText("Disconnected from Archipelago. Reload the world to reconnect.");
                session = null;
                return;
            }

            var items = session.Items.AllItemsReceived;
            while (itemCount < items.Count)
            {
                var item = items[itemCount++];
                var itemName = session.Items.GetItemName(item.Item);
                switch (itemName)
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
                    case "Dungeon": SeldomArchipelago.DungeonSafe = true; break;
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
                    case "Post-Plantera Dungeon": SeldomArchipelago.PlanteraDungeonEnemiesMaySpawn = true; break;
                    case "Biome Chests": SeldomArchipelago.BiomeChestUnlockable = true; break;
                    case "Post-Plantera Eclipse": SeldomArchipelago.PlanteraEclipseEnemiesMaySpawn = true; break;
                    case "Golem": SeldomArchipelago.GolemMaySpawn = true; break;
                    case "Prismatic Lacewing": SeldomArchipelago.PrismaticLacewingMaySpawn = true; break;
                    case "Martian Probe": SeldomArchipelago.MartianProbeMaySpawn = true; break;
                    case "Cultists": SeldomArchipelago.CultistsMaySpawn = true; break;
                }

                Main.NewText($"Obtained {itemName}!");

                foreach (var checker in checkers)
                {
                    checker.Check();
                }
            }
        }

        public static void CompleteLocation(string location)
        {
            if (session == null) return;

            var locationId = session.Locations.GetLocationIdFromName("Terraria", location);
            if (locationId >= 0) session.Locations.CompleteLocationChecks(locationId);
        }
    }
}
