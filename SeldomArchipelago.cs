using Mono.Cecil.Cil;
using MonoMod.Cil;
using Terraria;
using Terraria.GameContent.Events;
using Terraria.ID;
using Terraria.ModLoader;

namespace SeldomArchipelago
{
    public class SeldomArchipelago : Mod
    {
        public static bool BoundGoblinMaySpawn;
        public static bool UnconsciousManMaySpawn;
        public static bool WitchDoctorMaySpawn;
        public static bool DungeonSafe;
        public static bool WizardMaySpawn;
        public static bool TruffleMaySpawn;
        public static bool HardmodeFishing;
        public static bool TruffleWormMaySpawn;
        public static bool SteampunkerMaySpawn;

        public override void Load()
        {
            // NPC spawning
            IL.Terraria.NPC.SpawnNPC += il =>
            {
                var cursor = new ILCursor(il);

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

                while (cursor.TryGotoNext(instruction => instruction.MatchLdsfld(typeof(Main).GetField(nameof(Main.hardMode)))))
                {
                    cursor.Index++;
                    cursor.Emit(OpCodes.Pop);
                    cursor.Emit(OpCodes.Ldsfld, typeof(SeldomArchipelago).GetField(nameof(HardmodeFishing)));
                }
            };
        }
    }
}
