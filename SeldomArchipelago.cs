using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.GameContent.Events;
using Terraria.GameContent.Generation;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;
using Terraria.WorldBuilding;

namespace SeldomArchipelago
{
    public class SeldomArchipelago : Mod
    {
        // Flags with empty comments need further testing
        public static bool BoundGoblinMaySpawn;
        public static bool DryadMaySpawn; // Known to be spawnable before any checks
        public static bool UnconsciousManMaySpawn;
        public static bool WitchDoctorMaySpawn;
        public static bool DungeonSafe;
        public static bool WizardMaySpawn;
        public static bool TruffleMaySpawn;
        public static bool HardmodeFishing;
        public static bool TruffleWormMaySpawn;
        public static bool SteampunkerMaySpawn; //
        public static bool LifeFruitMayGrow; //
        public static int OldOnesArmyTier = 1; //
        public static bool SolarEclipseMayOccur; //
        public static bool PlanterasBulbMayGrow; //
        public static bool CyborgMaySpawn; // Missing item sales
        public static bool MaySellAutohammer; //
        public static bool PlanteraDungeonEnemiesMaySpawn;
        public static bool BiomeChestUnlockable; //
        public static bool PlanteraEclipseEnemiesMaySpawn; //
        public static bool GolemMaySpawn; //
        public static bool PrismaticLacewingMaySpawn; //
        public static bool MartianProbeMaySpawn;
        public static bool CultistsMaySpawn; //

        public static bool UndergroundEvilGenerated;
        public static bool HallowGenerated;

        static int hallowI;
        static int evilI;
        static float baseSpeedX;

        public override void Load()
        {
            // Hardmode biome gen params
            if (Main.rand == null) Main.rand = new UnifiedRandom((int)DateTime.Now.Ticks);
            var iRatio1 = (float)WorldGen.genRand.Next(300, 400) * 0.001f;
            var iRatio2 = (float)WorldGen.genRand.Next(200, 300) * 0.001f;

            hallowI = (int)((float)Main.maxTilesX * iRatio1);
            evilI = (int)((float)Main.maxTilesX * (1f - iRatio1));
            baseSpeedX = 1;

            if (WorldGen.genRand.Next(2) == 0)
            {
                (hallowI, evilI) = (evilI, hallowI);
                baseSpeedX = -1;
            }

            if (WorldGen.dungeonX < Main.maxTilesX / 2)
            {
                var newI = (int)((float)Main.maxTilesX * iRatio2);
                if (hallowI > evilI)
                {
                    evilI = newI;
                }
                else
                {
                    hallowI = newI;
                }
            }
            else
            {
                var newI = (int)((float)Main.maxTilesX * (1f - iRatio2));
                if (evilI > hallowI)
                {
                    evilI = newI;
                }
                else
                {
                    hallowI = newI;
                }
            }

            // NPC spawning
            IL.Terraria.NPC.SpawnNPC += il =>
            {
                var cursor = new ILCursor(il);

                // Plantera Dungeon enemy Terraria/NPC.cs:69945, spawning IL_018C
                cursor.GotoNext(instruction => instruction.MatchLdsfld(typeof(NPC).GetField(nameof(NPC.downedPlantBoss))));
                cursor.GotoNext(instruction => instruction.MatchStloc(out int _));
                var variable = (VariableDefinition)cursor.Next.Operand;
                cursor.Index++;
                cursor.Emit(OpCodes.Ldsfld, typeof(SeldomArchipelago).GetField(nameof(PlanteraDungeonEnemiesMaySpawn)));
                cursor.Emit(OpCodes.Stloc_S, variable);

                // Dungeon enemy spawning IL_0E34
                cursor.GotoNext(instruction => instruction.MatchLdsfld(typeof(NPC).GetField(nameof(NPC.downedBoss3))));
                cursor.Index++;
                cursor.Emit(OpCodes.Pop);
                cursor.Emit(OpCodes.Ldsfld, typeof(SeldomArchipelago).GetField(nameof(DungeonSafe)));

                // Martian probe spawning IL_23DE
                cursor.GotoNext(instruction => instruction.MatchLdsfld(typeof(NPC).GetField(nameof(NPC.downedMartians))));
                cursor.GotoPrev(instruction => instruction.MatchLdsfld(typeof(Main).GetField(nameof(Main.hardMode))));
                cursor.Index++;
                cursor.Emit(OpCodes.Pop);
                cursor.Emit(OpCodes.Ldsfld, typeof(SeldomArchipelago).GetField(nameof(MartianProbeMaySpawn)));
                cursor.Index += 2;
                cursor.Emit(OpCodes.Pop);
                cursor.Emit(OpCodes.Ldc_I4_1);
                cursor.GotoNext(instruction => instruction.MatchLdsfld(typeof(Main).GetField(nameof(Main.hardMode))));
                cursor.Index++;
                cursor.Emit(OpCodes.Pop);
                cursor.Emit(OpCodes.Ldsfld, typeof(SeldomArchipelago).GetField(nameof(MartianProbeMaySpawn)));
                cursor.Index += 2;
                cursor.Emit(OpCodes.Pop);
                cursor.Emit(OpCodes.Ldc_I4_1);

                // Unconscious Man spawning Terraria/NPC.cs:71052, Terraria.GameContent.Events/DD2Event.cs:58, IL_30DD
                cursor.GotoNext(instruction => instruction.MatchCall(typeof(DD2Event).GetProperty(nameof(DD2Event.ReadyToFindBartender)).GetGetMethod()));
                cursor.Index++;
                cursor.Emit(OpCodes.Pop);
                cursor.Emit(OpCodes.Ldsfld, typeof(SeldomArchipelago).GetField(nameof(UnconsciousManMaySpawn)));

                // Bound Goblin spawning IL_45F7
                cursor.GotoNext(instruction => instruction.MatchLdsfld(typeof(NPC).GetField(nameof(NPC.downedGoblins))));
                cursor.Index++;
                cursor.Emit(OpCodes.Pop);
                cursor.Emit(OpCodes.Ldsfld, typeof(SeldomArchipelago).GetField(nameof(BoundGoblinMaySpawn)));

                // Bound Wizard spawning IL_4674
                cursor.GotoNext(instruction => instruction.MatchLdsfld(typeof(Main).GetField(nameof(Main.hardMode))));
                cursor.Index++;
                cursor.Emit(OpCodes.Pop);
                cursor.Emit(OpCodes.Ldsfld, typeof(SeldomArchipelago).GetField(nameof(WizardMaySpawn)));

                // Dungeon Guardian spawning IL_60F3
                cursor.GotoNext(instruction => instruction.MatchLdsfld(typeof(NPC).GetField(nameof(NPC.downedBoss3))));
                cursor.Index++;
                cursor.Emit(OpCodes.Pop);
                cursor.Emit(OpCodes.Ldsfld, typeof(SeldomArchipelago).GetField(nameof(DungeonSafe)));

                // Plantera Eclipse enemy spawning IL_965B
                foreach (var _ in Enumerable.Range(0, 6))
                {
                    cursor.GotoNext(instruction => instruction.MatchLdsfld(typeof(NPC).GetField(nameof(NPC.downedPlantBoss))));
                    cursor.Index++;
                    cursor.Emit(OpCodes.Pop);
                    cursor.Emit(OpCodes.Ldsfld, typeof(SeldomArchipelago).GetField(nameof(PlanteraEclipseEnemiesMaySpawn)));
                }

                // Truffle Worm and other mushroom enemy spawning Terraria/NPC.cs:72269, IL_A10A
                cursor.GotoNext(instruction => instruction.MatchLdcI4(NPCID.TruffleWorm));
                cursor.GotoPrev(instruction => instruction.MatchLdsfld(typeof(Main).GetField(nameof(Main.hardMode))));
                cursor.Index++;
                cursor.Emit(OpCodes.Pop);
                cursor.Emit(OpCodes.Ldsfld, typeof(SeldomArchipelago).GetField(nameof(TruffleWormMaySpawn)));
                cursor.GotoPrev(instruction => instruction.MatchLdsfld(typeof(Main).GetField(nameof(Main.hardMode))));
                cursor.Index++;
                cursor.Emit(OpCodes.Pop);
                cursor.Emit(OpCodes.Ldsfld, typeof(SeldomArchipelago).GetField(nameof(TruffleWormMaySpawn)));

                // Prismatic Lacewing spawning Terraria/NPC.cs:72733, IL_C303
                cursor.GotoNext(instruction => instruction.MatchLdsfld(typeof(NPC).GetField(nameof(NPC.downedPlantBoss))));
                cursor.GotoPrev(instruction => instruction.MatchLdsfld(typeof(Main).GetField(nameof(Main.hardMode))));
                cursor.Index++;
                cursor.Emit(OpCodes.Pop);
                cursor.Emit(OpCodes.Ldc_I4_1);
                var label = (ILLabel)cursor.Next.Operand;
                cursor.GotoNext(instruction => instruction.MatchLdsfld(typeof(NPC).GetField(nameof(NPC.downedPlantBoss))));
                cursor.Index++;
                cursor.Emit(OpCodes.Pop);
                cursor.Emit(OpCodes.Ldsfld, typeof(SeldomArchipelago).GetField(nameof(PrismaticLacewingMaySpawn)));
                cursor.GotoNext(instruction => instruction.MatchLdsfld(typeof(Main).GetField(nameof(Main.cloudAlpha))));
                cursor.Index++;
                cursor.Emit(OpCodes.Pop);
                cursor.Emit(OpCodes.Ldsfld, typeof(Main).GetField(nameof(Main.hardMode)));
                cursor.Emit(OpCodes.Brfalse_S, label);
                cursor.Emit(OpCodes.Ldsfld, typeof(Main).GetField(nameof(Main.cloudAlpha)));

                // Martian Probe spawning again IL_CD7B
                cursor.GotoNext(instruction => instruction.MatchLdsfld(typeof(NPC).GetField(nameof(NPC.downedMartians))));
                cursor.GotoPrev(instruction => instruction.MatchLdsfld(typeof(Main).GetField(nameof(Main.hardMode))));
                cursor.Index++;
                cursor.Emit(OpCodes.Pop);
                cursor.Emit(OpCodes.Ldsfld, typeof(SeldomArchipelago).GetField(nameof(MartianProbeMaySpawn)));
                cursor.Index += 2;
                cursor.Emit(OpCodes.Pop);
                cursor.Emit(OpCodes.Ldc_I4_1);
            };

            // Town NPC spawning
            IL.Terraria.Main.UpdateTime_SpawnTownNPCs += il =>
            {
                var cursor = new ILCursor(il);

                // Dryad spawning IL_0817
                cursor.GotoNext(instruction => instruction.MatchLdsfld(typeof(NPC).GetField(nameof(NPC.downedBoss1))));
                cursor.Index++;
                cursor.Emit(OpCodes.Pop);
                cursor.Emit(OpCodes.Ldsfld, typeof(SeldomArchipelago).GetField(nameof(DryadMaySpawn)));
                cursor.Index += 2;
                cursor.Emit(OpCodes.Pop);
                cursor.Emit(OpCodes.Ldc_I4_1);
                cursor.Index += 2;
                cursor.Emit(OpCodes.Pop);
                cursor.Emit(OpCodes.Ldc_I4_1);

                // Steampunker spawning IL_0912
                cursor.GotoNext(instruction => instruction.MatchLdsfld(typeof(NPC).GetField(nameof(NPC.downedMechBossAny))));
                cursor.Index++;
                cursor.Emit(OpCodes.Pop);
                cursor.Emit(OpCodes.Ldsfld, typeof(SeldomArchipelago).GetField(nameof(SteampunkerMaySpawn)));

                // Witch Doctor spawning IL_0944
                cursor.GotoNext(instruction => instruction.MatchLdsfld(typeof(NPC).GetField(nameof(NPC.downedQueenBee))));
                cursor.Index++;
                cursor.Emit(OpCodes.Pop);
                cursor.Emit(OpCodes.Ldsfld, typeof(SeldomArchipelago).GetField(nameof(WitchDoctorMaySpawn)));

                // Truffle spawning IL_0979
                cursor.GotoNext(instruction => instruction.MatchLdsfld(typeof(Main).GetField(nameof(Main.hardMode))));
                cursor.Index++;
                cursor.Emit(OpCodes.Pop);
                cursor.Emit(OpCodes.Ldsfld, typeof(SeldomArchipelago).GetField(nameof(TruffleMaySpawn)));

                // Cyborg spawning IL_098C
                cursor.GotoNext(instruction => instruction.MatchLdsfld(typeof(Main).GetField(nameof(Main.hardMode))));
                cursor.Index++;
                cursor.Emit(OpCodes.Pop);
                cursor.Emit(OpCodes.Ldsfld, typeof(SeldomArchipelago).GetField(nameof(CyborgMaySpawn)));
                cursor.Index += 2;
                cursor.Emit(OpCodes.Pop);
                cursor.Emit(OpCodes.Ldc_I4_1);

                // Truffle prioritization IL_0BF8
                cursor.GotoNext(instruction => instruction.MatchLdcI4(NPCID.Truffle));
                cursor.GotoPrev(instruction => instruction.MatchLdsfld(typeof(Main).GetField(nameof(Main.hardMode))));
                cursor.Index++;
                cursor.Emit(OpCodes.Pop);
                cursor.Emit(OpCodes.Ldsfld, typeof(SeldomArchipelago).GetField(nameof(TruffleMaySpawn)));

                // Dryad prioritization IL_0C03
                cursor.GotoNext(instruction => instruction.MatchLdsfld(typeof(NPC).GetField(nameof(NPC.downedBoss1))));
                cursor.Index++;
                cursor.Emit(OpCodes.Pop);
                cursor.Emit(OpCodes.Ldsfld, typeof(SeldomArchipelago).GetField(nameof(DryadMaySpawn)));
                cursor.Index += 2;
                cursor.Emit(OpCodes.Pop);
                cursor.Emit(OpCodes.Ldc_I4_1);
                cursor.Index += 2;
                cursor.Emit(OpCodes.Pop);
                cursor.Emit(OpCodes.Ldc_I4_1);

                // Witch Doctor prioritization IL_0C3C
                cursor.GotoNext(instruction => instruction.MatchLdsfld(typeof(NPC).GetField(nameof(NPC.downedQueenBee))));
                cursor.Index++;
                cursor.Emit(OpCodes.Pop);
                cursor.Emit(OpCodes.Ldsfld, typeof(SeldomArchipelago).GetField(nameof(WitchDoctorMaySpawn)));

                // Steampunker prioritization IL_0C53
                cursor.GotoNext(instruction => instruction.MatchLdsfld(typeof(NPC).GetField(nameof(NPC.downedMechBossAny))));
                cursor.Index++;
                cursor.Emit(OpCodes.Pop);
                cursor.Emit(OpCodes.Ldsfld, typeof(SeldomArchipelago).GetField(nameof(SteampunkerMaySpawn)));

                // Cyborg prioritization IL_0C95
                cursor.GotoNext(instruction => instruction.MatchLdsfld(typeof(Main).GetField(nameof(Main.hardMode))));
                cursor.Index++;
                cursor.Emit(OpCodes.Pop);
                cursor.Emit(OpCodes.Ldsfld, typeof(SeldomArchipelago).GetField(nameof(CyborgMaySpawn)));
                cursor.Index += 2;
                cursor.Emit(OpCodes.Pop);
                cursor.Emit(OpCodes.Ldc_I4_1);
            };

            // NPC defeat events
            IL.Terraria.NPC.DoDeathEvents += il =>
            {
                var cursor = new ILCursor(il);

                // Hardmode start Terraria/NPC.cs:69103, IL_093B
                cursor.GotoNext(instruction => instruction.MatchLdsfld(typeof(Main).GetField(nameof(Main.hardMode))));
                cursor.GotoPrev(instruction => instruction.MatchBeq(out ILLabel _));
                var label = (ILLabel)cursor.Next.Operand;
                cursor.GotoNext(instruction => instruction.MatchLdsfld(typeof(Main).GetField(nameof(Main.hardMode))));
                cursor.Emit(OpCodes.Br, label);
            };

            // Fishing drops
            IL.Terraria.Projectile.FishingCheck_RollItemDrop += il =>
            {
                var cursor = new ILCursor(il);

                // Hardmode fishing drops
                while (cursor.TryGotoNext(instruction => instruction.MatchLdsfld(typeof(Main).GetField(nameof(Main.hardMode)))))
                {
                    cursor.Index++;
                    cursor.Emit(OpCodes.Pop);
                    cursor.Emit(OpCodes.Ldsfld, typeof(SeldomArchipelago).GetField(nameof(HardmodeFishing)));
                }
            };

            // Plant growth
            IL.Terraria.WorldGen.UpdateWorld_GrassGrowth += il =>
            {
                var cursor = new ILCursor(il);

                // Plantera's Bulb growth IL_01EF
                cursor.GotoNext(instruction => instruction.MatchLdsfld(typeof(Main).GetField(nameof(Main.hardMode))));
                cursor.Index++;
                cursor.Emit(OpCodes.Pop);
                cursor.Emit(OpCodes.Ldsfld, typeof(SeldomArchipelago).GetField(nameof(PlanterasBulbMayGrow)));
                cursor.Index += 2;
                cursor.Emit(OpCodes.Pop);
                cursor.Emit(OpCodes.Ldsfld, typeof(SeldomArchipelago).GetField(nameof(PlanterasBulbMayGrow)));
                cursor.Index += 2;
                cursor.Emit(OpCodes.Pop);
                cursor.Emit(OpCodes.Ldsfld, typeof(SeldomArchipelago).GetField(nameof(PlanterasBulbMayGrow)));
                cursor.Index += 2;
                cursor.Emit(OpCodes.Pop);
                cursor.Emit(OpCodes.Ldsfld, typeof(SeldomArchipelago).GetField(nameof(PlanterasBulbMayGrow)));

                // Life Fruit growth IL_031F
                cursor.GotoNext(instruction => instruction.MatchLdsfld(typeof(Main).GetField(nameof(Main.hardMode))));
                cursor.Index++;
                cursor.Emit(OpCodes.Pop);
                cursor.Emit(OpCodes.Ldsfld, typeof(SeldomArchipelago).GetField(nameof(LifeFruitMayGrow)));
                cursor.Index += 2;
                cursor.Emit(OpCodes.Pop);
                cursor.Emit(OpCodes.Ldsfld, typeof(SeldomArchipelago).GetField(nameof(LifeFruitMayGrow)));
            };

            // Old One's Army tier
            IL.Terraria.GameContent.Events.DD2Event.FindProperDifficulty += il =>
            {
                var cursor = new ILCursor(il);

                cursor.Emit(OpCodes.Ldsfld, typeof(SeldomArchipelago).GetField(nameof(OldOnesArmyTier)));
                cursor.Emit(OpCodes.Stsfld, typeof(DD2Event).GetField(nameof(DD2Event.OngoingDifficulty)));
                cursor.Emit(OpCodes.Ret);
            };

            // Day start events
            IL.Terraria.Main.UpdateTime_StartDay += il =>
            {
                var cursor = new ILCursor(il);

                // Eclipse starting Terraria/Main.cs:59650
                cursor.GotoNext(instruction => instruction.MatchLdsfld(typeof(Main).GetField(nameof(Main.hardMode))));
                cursor.Index++;
                cursor.Emit(OpCodes.Pop);
                cursor.Emit(OpCodes.Ldsfld, typeof(SeldomArchipelago).GetField(nameof(SolarEclipseMayOccur)));
                cursor.Index += 2;
                cursor.Emit(OpCodes.Pop);
                cursor.Emit(OpCodes.Ldc_I4_1);
            };

            // Town NPC shops
            IL.Terraria.Chest.SetupShop += il =>
            {
                var cursor = new ILCursor(il);

                // Autohammer IL_1340
                cursor.GotoNext(instruction => instruction.MatchLdcI4(10));
                cursor.GotoNext(instruction => instruction.MatchLdsfld(typeof(NPC).GetField(nameof(NPC.downedPlantBoss)))); // IL_137E
                cursor.Index++;
                cursor.Emit(OpCodes.Pop);
                cursor.Emit(OpCodes.Ldsfld, typeof(SeldomArchipelago).GetField(nameof(MaySellAutohammer)));
            };

            // Dungeon Spirit spawning Terraria/NPC.cs:68692
            IL.Terraria.NPC.DoDeathEvents_SummonDungeonSpirit += il =>
            {
                var cursor = new ILCursor(il);

                cursor.GotoNext(instruction => instruction.MatchLdsfld(typeof(Main).GetField(nameof(Main.hardMode))));
                cursor.Index++;
                cursor.Emit(OpCodes.Pop);
                cursor.Emit(OpCodes.Ldsfld, typeof(SeldomArchipelago).GetField(nameof(PlanteraDungeonEnemiesMaySpawn)));
                cursor.Index += 2;
                cursor.Emit(OpCodes.Pop);
                cursor.Emit(OpCodes.Ldc_I4_1);
            };

            // Chest unlocking
            IL.Terraria.Chest.Unlock += il =>
            {
                var cursor = new ILCursor(il);

                // Biome chest unlocking
                while (cursor.TryGotoNext(instruction => instruction.MatchLdsfld(typeof(NPC).GetField(nameof(NPC.downedPlantBoss)))))
                {
                    cursor.Index++;
                    cursor.Emit(OpCodes.Pop);
                    cursor.Emit(OpCodes.Ldsfld, typeof(SeldomArchipelago).GetField(nameof(BiomeChestUnlockable)));
                }
            };

            // Player tile usages
            IL.Terraria.Player.TileInteractionsUse += il =>
            {
                var cursor = new ILCursor(il);

                // Golem spawning Terraria/Player.cs:25954
                cursor.GotoNext(instruction => instruction.MatchLdsfld(typeof(Main).GetField(nameof(Main.hardMode))));
                cursor.Index++;
                cursor.Emit(OpCodes.Pop);
                cursor.Emit(OpCodes.Ldsfld, typeof(SeldomArchipelago).GetField(nameof(GolemMaySpawn)));
                cursor.Index += 2;
                cursor.Emit(OpCodes.Pop);
                cursor.Emit(OpCodes.Ldc_I4_1);
            };

            // Cultist ritual spawning
            IL.Terraria.GameContent.Events.CultistRitual.CheckRitual += il =>
            {
                var cursor = new ILCursor(il);

                cursor.GotoNext(instruction => instruction.MatchLdsfld(typeof(Main).GetField(nameof(Main.hardMode))));
                cursor.Index++;
                cursor.Emit(OpCodes.Pop);
                cursor.Emit(OpCodes.Ldsfld, typeof(SeldomArchipelago).GetField(nameof(CultistsMaySpawn)));
                cursor.Index += 2;
                cursor.Emit(OpCodes.Pop);
                cursor.Emit(OpCodes.Ldc_I4_1);
            };
        }

        public static void StartHardmode()
        {
            if (Main.netMode == 1) return;

            Main.hardMode = true;
            List<GenPass> list = new List<GenPass>();

            SystemLoader.ModifyHardmodeTasks(list);
            foreach (GenPass item in list)
            {
                item.Apply(null, null);
            }

            if (Main.netMode == 2)
            {
                Netplay.ResetSections();
            }
        }

        // Based on Terraria.WorldGen.smCallBack
        static void GenerateWalls()
        {
            var iterations = (int)((float)Main.maxTilesX / 168f);
            var iterations2 = 0;
            ShapeData shapeData = new ShapeData();

            while (iterations > 0)
            {
                if (++iterations2 % 15000 == 0)
                {
                    iterations--;
                }

                var point = WorldGen.RandomWorldPoint((int)Main.worldSurface - 100, 1, 190, 1);
                var tile = Main.tile[point.X, point.Y];
                var tile2 = Main.tile[point.X, point.Y - 1];
                ushort wall = 0;

                if (TileID.Sets.Crimson[tile.TileType])
                {
                    wall = (ushort)(192 + WorldGen.genRand.Next(4));
                }
                else if (TileID.Sets.Corrupt[tile.TileType])
                {
                    wall = (ushort)(188 + WorldGen.genRand.Next(4));
                }
                else if (TileID.Sets.Hallow[tile.TileType])
                {
                    wall = (ushort)(200 + WorldGen.genRand.Next(4));
                }

                if (tile.HasTile && wall != 0 && !tile2.HasTile)
                {
                    bool success = WorldUtils.Gen(new Point(point.X, point.Y - 1), new ShapeFloodFill(1000), Actions.Chain(new Modifiers.IsNotSolid(), new Modifiers.OnlyWalls(0, 54, 55, 56, 57, 58, 59, 61, 185, 212, 213, 214, 215, 2, 196, 197, 198, 199, 15, 40, 71, 64, 204, 205, 206, 207, 208, 209, 210, 211, 71), new Actions.Blank().Output(shapeData)));

                    if (shapeData.Count > 50 && success)
                    {
                        WorldUtils.Gen(new Point(point.X, point.Y), new ModShapes.OuterOutline(shapeData, useDiagonals: true, useInterior: true), new Actions.PlaceWall(wall));
                        iterations--;
                    }

                    shapeData.Clear();
                }
            }

            if (Main.netMode == 2)
            {
                Netplay.ResetSections();
            }
        }

        public static void GenerateUndergroundEvil()
        {
            if (Main.netMode == 1) return;

            WorldGen.GERunner(evilI, 0, 3 * -baseSpeedX, 5, false);
            GenerateWalls();

            UndergroundEvilGenerated = true;
        }

        public static void GenerateHallow()
        {
            if (Main.netMode == 1) return;

            WorldGen.GERunner(hallowI, 0, 3 * baseSpeedX, 5);
            GenerateWalls();

            HallowGenerated = true;
        }
    }
}
