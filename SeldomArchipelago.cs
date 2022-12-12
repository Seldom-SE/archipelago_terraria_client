using System;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Terraria;
using Terraria.ModLoader;

// ReLogic.dll C:\Program Files (x86)\Steam\steamapps\common\tModLoader\Libraries\ReLogic\1.0.0\ReLogic.dll

namespace SeldomArchipelago
{
    public class SeldomArchipelago : Mod
    {
        public static bool DryadMaySpawn = true;

        public override void Load()
        {
            // Take control of Dryad spawning
            // Dryad spawn logic Terraria / Main.cs:60053
            // IL.Terraria.Main.UpdateTime_SpawnTownNPCs += il =>
            // {
            //     var cursor = new ILCursor(il);
            //     if (!cursor.TryGotoNext(instruction => instruction.MatchLdsfld(typeof(NPC).GetField(nameof(NPC.downedBoss1)))))
            //     {
            //         Logger.Error("Failed to locate Dryad spawning logic");
            //         return;
            //     }

            //     // Skip first boss killed check because some things jump here
            //     cursor.Index++;
            //     // Jump over Dryad's former spawn logic
            //     var label = il.DefineLabel();
            //     cursor.Emit(OpCodes.Br, label);

            //     // Skip to the end of Dryad spawn logic
            //     if (!cursor.TryGotoNext(instruction => instruction.MatchLdsfld(typeof(NPC).GetField(nameof(NPC.downedBoss3)))))
            //     {
            //         Logger.Error("Failed to navigate Dryad spawning logic");
            //         return;
            //     }

            //     cursor.Index++;
            //     // Jump here
            //     cursor.MarkLabel(label);
            //     // Replace whether boss 1 was killed with a controlled value
            //     // cursor.EmitDelegate<Func<bool, bool>>(_ => SeldomArchipelago.DryadMaySpawn);
            //     cursor.Emit(OpCodes.Pop);
            //     cursor.Emit(OpCodes.Ldsfld, typeof(SeldomArchipelago).GetField(nameof(SeldomArchipelago.DryadMaySpawn)));
            //     // After this, it decides whether to spawn Dryad based on the value in stack

            //     foreach (var instruction in il.Instrs)
            //     {
            //         var operand = instruction.Operand == null ? "" : instruction.Operand is ILLabel labelOp ? $"Label of {labelOp.Target.Offset} {labelOp.Target.OpCode} {labelOp.Target.Operand}" : instruction.Operand.ToString();
            //         Logger.Debug($"{instruction.Offset} {instruction.OpCode} {operand}");
            //     }
            // };

            // Take control of Dryad spawning
            IL.Terraria.Main.UpdateTime_SpawnTownNPCs += il =>
            {
                var cursor = new ILCursor(il);
                if (!cursor.TryGotoNext(i => i.MatchLdsfld(typeof(NPC).GetField(nameof(NPC.downedBoss1)))))
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

                foreach (var instruction in il.Instrs)
                {
                    var operand = instruction.Operand == null ? "" : instruction.Operand is ILLabel labelOp ? $"Label of {labelOp.Target.Offset} {labelOp.Target.OpCode} {labelOp.Target.Operand}" : instruction.Operand.ToString();
                    Logger.Debug($"{instruction.Offset} {instruction.OpCode} {operand}");
                }
            };
        }
    }
}
