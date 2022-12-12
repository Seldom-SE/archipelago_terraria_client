using Mono.Cecil.Cil;
using MonoMod.Cil;
using Terraria;
using Terraria.GameContent.Events;
using Terraria.ModLoader;

namespace SeldomArchipelago
{
    public class SeldomArchipelago : Mod
    {
        public static bool BoundGoblinMaySpawn;
        public static bool UnconsciousManMaySpawn;
        public static bool WitchDoctorMaySpawn;
        public static bool DungeonSafe;

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

                // Dungeon Guardian spawning IL_60F3
                cursor.GotoNext(instruction => instruction.MatchLdsfld(typeof(NPC).GetField(nameof(NPC.downedBoss3))));
                cursor.Index++;
                cursor.Emit(OpCodes.Pop);
                cursor.Emit(OpCodes.Ldsfld, typeof(SeldomArchipelago).GetField(nameof(DungeonSafe)));
            };

            // Town NPC spawning
            IL.Terraria.Main.UpdateTime_SpawnTownNPCs += il =>
            {
                var cursor = new ILCursor(il);

                // Witch Doctor spawning
                cursor.GotoNext(instruction => instruction.MatchLdsfld(typeof(NPC).GetField(nameof(NPC.downedQueenBee))));
                cursor.Index++;
                cursor.Emit(OpCodes.Pop);
                cursor.Emit(OpCodes.Ldsfld, typeof(SeldomArchipelago).GetField(nameof(WitchDoctorMaySpawn)));

                // Witch Doctor prioritization
                cursor.GotoNext(instruction => instruction.MatchLdsfld(typeof(NPC).GetField(nameof(NPC.downedQueenBee))));
                cursor.Index++;
                cursor.Emit(OpCodes.Pop);
                cursor.Emit(OpCodes.Ldsfld, typeof(SeldomArchipelago).GetField(nameof(WitchDoctorMaySpawn)));
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
        }
    }
}
