using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using SeldomArchipelago.Systems;
using Terraria;
using Terraria.GameContent.Events;
using Terraria.ID;
using Terraria.ModLoader;

namespace SeldomArchipelago
{
    public class SeldomArchipelago : Mod
    {
        // Terraria is single-threaded so this *should* be fine
        bool temp;

        public override void Load()
        {
            // Begin cursed IL editing

            // Torch God reward Terraria.Player:13794
            IL.Terraria.Player.TorchAttack += il =>
            {
                var cursor = new ILCursor(il);

                cursor.GotoNext(i => i.MatchLdsfld(typeof(Main).GetField(nameof(Main.netMode))));
                cursor.EmitDelegate<Action>(() => ArchipelagoSystem.QueueLocation("Torch God"));
                cursor.Emit(OpCodes.Ret);
            };

            // Allow Torch God even if you have `unlockedBiomeTorches`
            IL.Terraria.Player.UpdateTorchLuck_ConsumeCountersAndCalculate += il =>
            {
                var cursor = new ILCursor(il);

                cursor.GotoNext(i => i.MatchLdfld(typeof(Player).GetField(nameof(Player.unlockedBiomeTorches))));
                cursor.Index++;
                cursor.Emit(OpCodes.Pop);
                cursor.Emit(OpCodes.Ldc_I4_0);

                cursor.GotoNext(i => i.MatchLdcI4(ItemID.TorchGodsFavor));
                cursor.Emit(OpCodes.Pop);
                cursor.Emit(OpCodes.Ldc_I4_0);
            };

            // General event clear locations
            IL.Terraria.NPC.SetEventFlagCleared += il =>
            {
                var cursor = new ILCursor(il);

                cursor.Emit(OpCodes.Ldarg_1);
                cursor.EmitDelegate<Action<int>>((int id) => ArchipelagoSystem.QueueLocation(id switch
                {
                    GameEventClearedID.DefeatedGoblinArmy => "Goblin Army",
                    GameEventClearedID.DefeatedSlimeKing => "King Slime",
                    GameEventClearedID.DefeatedEyeOfCthulu => "Eye of Cthulhu",
                    GameEventClearedID.DefeatedEaterOfWorldsOrBrainOfChtulu => "Eater of Worlds or Brain of Cthulhu",
                    GameEventClearedID.DefeatedQueenBee => "Queen Bee",
                    GameEventClearedID.DefeatedSkeletron => "Skeletron",
                    GameEventClearedID.DefeatedDeerclops => "Deerclops",
                    GameEventClearedID.DefeatedWallOfFleshAndStartedHardmode => "Wall of Flesh",
                    GameEventClearedID.DefeatedPirates => "Pirate Invasion",
                    GameEventClearedID.DefeatedFrostArmy => "Frost Legion",
                    GameEventClearedID.DefeatedQueenSlime => "Queen Slime",
                    GameEventClearedID.DefeatedDestroyer => "The Destroyer",
                    GameEventClearedID.DefeatedTheTwins => "The Twins",
                    GameEventClearedID.DefeatedSkeletronPrime => "Skeletron Prime",
                    GameEventClearedID.DefeatedPlantera => "Plantera",
                    GameEventClearedID.DefeatedGolem => "Golem",
                    GameEventClearedID.DefeatedMartians => "Martian Invasion",
                    GameEventClearedID.DefeatedFishron => "Duke Fishron",
                    GameEventClearedID.DefeatedHalloweenTree => "Mourning Wood",
                    GameEventClearedID.DefeatedHalloweenKing => "Pumpking",
                    GameEventClearedID.DefeatedChristmassTree => "Everscream",
                    GameEventClearedID.DefeatedSantank => "Santa-NK1",
                    GameEventClearedID.DefeatedIceQueen => "Ice Queen",
                    GameEventClearedID.DefeatedEmpressOfLight => "Empress of Light",
                    GameEventClearedID.DefeatedAncientCultist => "Lunatic Cultist",
                    GameEventClearedID.DefeatedMoonlord => "Moon Lord",
                    _ => throw new ArgumentOutOfRangeException(),
                }));
                cursor.Emit(OpCodes.Ret);
            };

            // Old One's Army locations
            IL.Terraria.GameContent.Events.DD2Event.WinInvasionInternal += il =>
            {
                var cursor = new ILCursor(il);

                foreach (var (flagName, tier) in new Tuple<string, int>[] {
                    Tuple.Create(nameof(DD2Event.DownedInvasionT1), 1),
                    Tuple.Create(nameof(DD2Event.DownedInvasionT2), 2),
                    Tuple.Create(nameof(DD2Event.DownedInvasionT3), 3),
                })
                {
                    var flag = typeof(DD2Event).GetField(flagName);
                    cursor.GotoNext(i => i.MatchStsfld(flag));
                    cursor.EmitDelegate<Action>(() => temp = (bool)flag.GetValue(null));
                    cursor.Index++;
                    cursor.EmitDelegate<Action>(() =>
                    {
                        flag.SetValue(null, temp);
                        ArchipelagoSystem.QueueLocation($"Old One's Army Tier {tier}");
                    });
                }
            };

            IL.Terraria.NPC.DoDeathEvents += il =>
            {
                var cursor = new ILCursor(il);

                // Prevent NPC.downedMechBossAny from being set
                while (cursor.TryGotoNext(i => i.MatchStsfld(typeof(NPC).GetField(nameof(NPC.downedMechBossAny)))))
                {
                    cursor.EmitDelegate<Action>(() => temp = NPC.downedMechBossAny);
                    cursor.Index++;
                    cursor.EmitDelegate<Action>(() => NPC.downedMechBossAny = temp);
                }

                // Prevent Hardmode generation Terraria.NPC:69104
                cursor.GotoNext(i => i.MatchCall(typeof(WorldGen).GetMethod(nameof(WorldGen.StartHardmode))));
                cursor.EmitDelegate<Action>(() =>
                {
                    temp = Main.hardMode;
                    Main.hardMode = true;
                });
                cursor.Index++;
                cursor.EmitDelegate<Action>(() => Main.hardMode = temp);
            };
        }
    }
}
