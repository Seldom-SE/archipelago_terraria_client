using Mono.Cecil.Cil;
using MonoMod.Cil;
using Terraria;
using Terraria.GameContent.Events;
using Terraria.ModLoader;

namespace SeldomArchipelago
{
    public class SeldomArchipelago : Mod
    {
        public static bool DryadMaySpawn = false;
        public static bool UnconsciousManMaySpawn = false;
        public static bool WitchDoctorMaySpawn = false;

        public override void Load()
        {
            // Take control of town NPC spawning
            // Dryad spawn logic Terraria/Main.cs:60053
            IL.Terraria.Main.UpdateTime_SpawnTownNPCs += il =>
            {
                var cursor = new ILCursor(il);

                // Dryad spawning
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
                }

                cursor.Index++;
                cursor.Emit(OpCodes.Pop);
                cursor.Emit(OpCodes.Ldsfld, typeof(SeldomArchipelago).GetField(nameof(WitchDoctorMaySpawn)));
            };

            // Take control of Unconscious Man spawning
            // Unconscious Man spawn logic Terraria/NPC.cs:71052 and Terraria.GameContent.Events/DD2Event.cs:58
            IL.Terraria.NPC.SpawnNPC += il =>
            {
                var cursor = new ILCursor(il);
                if (!cursor.TryGotoNext(instruction => instruction.MatchCall(typeof(DD2Event).GetProperty(nameof(DD2Event.ReadyToFindBartender)).GetGetMethod())))
                {
                    Logger.Error("Failed to find Unconscious Man spawning logic");
                    return;
                }

                cursor.Index++;
                cursor.Emit(OpCodes.Pop);
                cursor.Emit(OpCodes.Ldsfld, typeof(SeldomArchipelago).GetField(nameof(UnconsciousManMaySpawn)));
            };
        }
    }
}
