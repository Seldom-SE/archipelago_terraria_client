using Mono.Cecil.Cil;
using MonoMod.Cil;
using Terraria;
using Terraria.ModLoader;

// ReLogic.dll C:\Program Files (x86)\Steam\steamapps\common\tModLoader\Libraries\ReLogic\1.0.0\ReLogic.dll

namespace SeldomArchipelago
{
    public class SeldomArchipelago : Mod
    {
        public static bool DryadMaySpawn = false;

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
                // Pop whether boss 1 is killed. We don't need it.
                cursor.Emit(OpCodes.Pop);
                // Get whether Dryad should spawn
                cursor.Emit(OpCodes.Ldsfld, typeof(SeldomArchipelago).GetField(nameof(SeldomArchipelago.DryadMaySpawn)));
                // After this, it decides whether to spawn Dryad based on the value in stack
            };
        }
    }
}
