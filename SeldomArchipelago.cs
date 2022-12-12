using Mono.Cecil.Cil;
using MonoMod.Cil;
using Terraria;
using Terraria.ModLoader;

namespace SeldomArchipelago
{
    public class SeldomArchipelago : Mod
    {
        public static bool DryadMaySpawn = true;
        public static int OldOnesArmyTier = 0;

        public override void Load()
        {
            // Take control of Dryad spawning
            // Dryad spawn logic Terraria/Main.cs:60053
            IL.Terraria.Main.UpdateTime_SpawnTownNPCs += il =>
            {
                var cursor = new ILCursor(il);
                if (!cursor.TryGotoNext(instruction => instruction.MatchLdsfld(typeof(NPC).GetField(nameof(NPC.downedBoss1)))))
                {
                    Logger.Error("Failed to locate Dryad spawning logic");
                    return;
                }

                // Skip the first boss killed check so its label isn't broken
                cursor.Index++;
                // Remove code to check whether bosses were killed
                cursor.RemoveRange(4);
                // Replace whether boss 1 was killed with a controlled value
                cursor.Emit(OpCodes.Pop);
                cursor.Emit(OpCodes.Ldsfld, typeof(SeldomArchipelago).GetField(nameof(SeldomArchipelago.DryadMaySpawn)));
                // After this, it decides whether to spawn Dryad based on the value in stack

                // Repeat for Dryad prioritization logic
                if (!cursor.TryGotoNext(instruction => instruction.MatchLdsfld(typeof(NPC).GetField(nameof(NPC.downedBoss1)))))
                {
                    Logger.Error("Failed to locate Dryad prioritization logic");
                    return;
                }

                cursor.Index++;
                cursor.RemoveRange(4);
                cursor.Emit(OpCodes.Pop);
                cursor.Emit(OpCodes.Ldsfld, typeof(SeldomArchipelago).GetField(nameof(SeldomArchipelago.DryadMaySpawn)));
            };
        }
    }
}
