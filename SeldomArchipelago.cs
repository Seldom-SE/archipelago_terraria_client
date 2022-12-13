using Mono.Cecil.Cil;
using MonoMod.Cil;
using System.Linq;
using Terraria;
using Terraria.GameContent.Events;
using Terraria.ID;
using Terraria.ModLoader;

namespace SeldomArchipelago
{
    public class SeldomArchipelago : Mod
    {
        // Flags with empty comments need further testing
        public static bool BoundGoblinMaySpawn;
        public static bool UnconsciousManMaySpawn; //
        public static bool WitchDoctorMaySpawn;
        public static bool DungeonSafe;
        public static bool WizardMaySpawn;
        public static bool TruffleMaySpawn;
        public static bool HardmodeFishing; //
        public static bool TruffleWormMaySpawn; //
        public static bool SteampunkerMaySpawn; //
        public static bool LifeFruitMayGrow; //
        public static int OldOnesArmyTier = 1; //
        public static bool SolarEclipseMayOccur; //
        public static bool PlanterasBulbMayGrow; //
        public static bool CyborgMaySpawn; //
        public static bool MaySellAutohammer; //
        public static bool PlanteraDungeonEnemiesMaySpawn; //
        public static bool BiomeChestUnlockable; //
        public static bool PlanteraEclipseEnemiesMaySpawn; //
        public static bool GolemMaySpawn; //
        public static bool PrismaticLacewingMaySpawn; //

        public override void Load()
        {
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
            };

            // Town NPC spawning
            IL.Terraria.Main.UpdateTime_SpawnTownNPCs += il =>
            {
                var cursor = new ILCursor(il);

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
        }
    }
}
