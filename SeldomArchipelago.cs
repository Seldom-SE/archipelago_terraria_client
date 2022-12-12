using Mono.Cecil.Cil;
using MonoMod.Cil;
using Terraria;
using Terraria.GameContent.Events;
using Terraria.ModLoader;

namespace SeldomArchipelago
{
    public class SeldomArchipelago : Mod
    {
        public static bool DryadMaySpawn;
        public static bool UnconsciousManMaySpawn;
        public static bool WitchDoctorMaySpawn;
        public static bool DungeonSafe;

        public override void Load()
        {
            // Take control of town NPC spawning
            IL.Terraria.Main.UpdateTime_SpawnTownNPCs += il =>
            {
                var cursor = new ILCursor(il);

                // Dryad spawning Terraria/Main.cs:60053
                if (!cursor.TryGotoNext(instruction => instruction.MatchLdsfld(typeof(NPC).GetField(nameof(NPC.downedBoss1)))))
                {
                    Logger.Error("Failed to find Dryad spawning logic");
                    return;
                }

                // Skip the first boss killed check so its label isn't broken
                cursor.Index++;
                // Remove code to check whether bosses were killed
                cursor.RemoveRange(4);
                // Replace whether boss 1 was killed with a controlled value
                cursor.Emit(OpCodes.Pop);
                cursor.Emit(OpCodes.Ldsfld, typeof(SeldomArchipelago).GetField(nameof(DryadMaySpawn)));
                // After this, it decides whether to spawn Dryad based on the value in stack

                // Witch Doctor spawning
                if (!cursor.TryGotoNext(instruction => instruction.MatchLdsfld(typeof(NPC).GetField(nameof(NPC.downedQueenBee)))))
                {
                    Logger.Error("Failed to find Witch Doctor spawning logic");
                    return;
                }

                cursor.Index++;
                cursor.Emit(OpCodes.Pop);
                cursor.Emit(OpCodes.Ldsfld, typeof(SeldomArchipelago).GetField(nameof(WitchDoctorMaySpawn)));

                // Dryad prioritization
                if (!cursor.TryGotoNext(instruction => instruction.MatchLdsfld(typeof(NPC).GetField(nameof(NPC.downedBoss1)))))
                {
                    Logger.Error("Failed to find Dryad prioritization logic");
                    return;
                }

                cursor.Index++;
                cursor.RemoveRange(4);
                cursor.Emit(OpCodes.Pop);
                cursor.Emit(OpCodes.Ldsfld, typeof(SeldomArchipelago).GetField(nameof(DryadMaySpawn)));

                // Witch Doctor prioritization
                if (!cursor.TryGotoNext(instruction => instruction.MatchLdsfld(typeof(NPC).GetField(nameof(NPC.downedQueenBee)))))
                {
                    Logger.Error("Failed to find Witch Doctor prioritization logic");
                    return;
                }

                cursor.Index++;
                cursor.Emit(OpCodes.Pop);
                cursor.Emit(OpCodes.Ldsfld, typeof(SeldomArchipelago).GetField(nameof(WitchDoctorMaySpawn)));
            };

            // Take control of NPC spawning
            IL.Terraria.NPC.SpawnNPC += il =>
            {
                var cursor = new ILCursor(il);

                // Dungeon enemy spawning IL_0E34
                if (!cursor.TryGotoNext(instruction => instruction.MatchLdsfld(typeof(NPC).GetField(nameof(NPC.downedBoss3)))))
                {
                    Logger.Error("Failed to find Dungeon enemy spawning logic");
                    return;
                }

                cursor.Index++;
                cursor.Emit(OpCodes.Pop);
                cursor.Emit(OpCodes.Ldsfld, typeof(SeldomArchipelago).GetField(nameof(DungeonSafe)));

                // Unconscious Man spawning Terraria/NPC.cs:71052, Terraria.GameContent.Events/DD2Event.cs:58, IL_30DD
                if (!cursor.TryGotoNext(instruction => instruction.MatchCall(typeof(DD2Event).GetProperty(nameof(DD2Event.ReadyToFindBartender)).GetGetMethod())))
                {
                    Logger.Error("Failed to find Unconscious Man spawning logic");
                    return;
                }

                cursor.Index++;
                cursor.Emit(OpCodes.Pop);
                cursor.Emit(OpCodes.Ldsfld, typeof(SeldomArchipelago).GetField(nameof(UnconsciousManMaySpawn)));

                // Dungeon Guardian spawning IL_60F3
                if (!cursor.TryGotoNext(instruction => instruction.MatchLdsfld(typeof(NPC).GetField(nameof(NPC.downedBoss3)))))
                {
                    Logger.Error("Failed to find Dungeon Guardian spawning logic");
                    return;
                }

                cursor.Index++;
                cursor.Emit(OpCodes.Pop);
                cursor.Emit(OpCodes.Ldsfld, typeof(SeldomArchipelago).GetField(nameof(DungeonSafe)));
            };
        }
    }
}
