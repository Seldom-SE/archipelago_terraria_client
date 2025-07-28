using Microsoft.CodeAnalysis.Operations;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using SeldomArchipelago.NPCs;
using SeldomArchipelago.Players;
using SeldomArchipelago.Systems;
using SeldomArchipelago.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using Terraria;
using Terraria.Achievements;
using Terraria.Chat;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.GameContent.Events;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace SeldomArchipelago
{
    // TODO Use a data-oriented approach to get rid of all this repetition
    public class SeldomArchipelago : Mod
    {
        // We reuse some parts of Terraria's code for multiple purposes in this mod. For example,
        // when you kill a boss, we have to prevent that code from making permanent world changes
        // and instead send a location, but we reuse that same code when making permanent changes
        // after receiving a boss flag as an item, so we have to not prevent the code from making
        // such changes in that case. So, we use this flag to determine whether the code is run by
        // the game naturally (false) or run by us (true). Terraria is single-threaded, don't worry.
        public bool temp;

        public static MethodInfo desertScourgeHeadOnKill = null;
        public static MethodInfo giantClamOnKill = null; // downedCLAM and downedCLAMHardMode
        public static MethodInfo cragmawMireOnKill = null;
        public static MethodInfo acidRainEventUpdateInvasion = null; // downedEoCAcidRain and downedAquaticScourgeAcidRain
        public static MethodInfo crabulonOnKill = null;
        public static MethodInfo hiveMindOnKill = null;
        public static MethodInfo perforatorHiveOnKill = null;
        public static MethodInfo slimeGodCoreOnKill = null;
        public static MethodInfo calamityGlobalNpcOnKill = null;
        public static MethodInfo aquaticScourgeHeadOnKill = null;
        public static MethodInfo maulerOnKill = null;
        public static MethodInfo brimstoneElementalOnKill = null;
        public static MethodInfo cryogenOnKill = null;
        public static MethodInfo calamitasCloneOnKill = null;
        public static MethodInfo greatSandSharkOnKill = null;
        public static MethodInfo leviathanRealOnKill = null;
        public static MethodInfo astrumAureusOnKill = null;
        public static MethodInfo plaguebringerGoliathOnKill = null;
        public static MethodInfo ravagerBodyOnKill = null;
        public static MethodInfo astrumDeusHeadOnKill = null;
        public static MethodInfo profanedGuardianCommanderOnKill = null;
        public static MethodInfo bumblefuckOnKill = null;
        public static MethodInfo providenceOnKill = null;
        public static MethodInfo stormWeaverHeadOnKill = null;
        public static MethodInfo ceaselessVoidOnKill = null;
        public static MethodInfo signusOnKill = null;
        public static MethodInfo polterghastOnKill = null;
        public static MethodInfo nuclearTerrorOnKill = null;
        public static MethodInfo oldDukeOnKill = null;
        public static MethodInfo devourerofGodsHeadOnKill = null;
        public static MethodInfo yharonOnKill = null;
        public static MethodInfo aresBodyOnKill = null;
        public static MethodInfo apolloOnKill = null;
        public static MethodInfo thanatosHeadOnKill = null;
        public static MethodInfo supremeCalamitasOnKill = null;
        public static MethodInfo calamityGlobalNpcSetNewBossJustDowned = null;

        public override void Load()
        {
            var archipelagoSystem = ModContent.GetInstance<ArchipelagoSystem>();

            // Begin cursed IL editing

            // Manage Ghost Spawning
            IL_WorldGen.SpawnTownNPC += il =>
            {
                var cursor = new ILCursor(il);
                var label = il.DefineLabel();

                cursor.GotoNext(i => i.MatchCall(out var mref) && mref.Name == "NewNPC");
                cursor.Remove();
                cursor.EmitDelegate((IEntitySource source, int x, int y, int type, int mysteryNumber, float _, float _, float _, float _, int target) => // whatever floats ur boat, dude
                {
                    if (archipelagoSystem.ghostNPCqueue.TryDequeue(out int ghostID))
                    {
                        int ghostIndex = NPC.NewNPC(source, WorldGen.bestX * 16, WorldGen.bestY * 16, ModContent.NPCType<GhostNPC>(), mysteryNumber, 0f, 0f, 0f, 0f, target);
                        //CheckNPC ghost = Main.npc[ghostIndex].ModNPC as CheckNPC;
                        //ghost.home = new(WorldGen.bestX, WorldGen.bestY);
                        NPC ghost = Main.npc[ghostIndex];
                        ghost.homeTileX = WorldGen.bestX;
                        ghost.homeTileY = WorldGen.bestY;
                        GhostNPC modGhost = ghost.ModNPC as GhostNPC;
                        modGhost.GhostType = ghostID;
                        return ghostIndex;
                    } else
                    {
                        return NPC.NewNPC(source, x, y, type, mysteryNumber, 0f, 0f, 0f, 0f, target);
                    }
                });
                cursor.GotoNext(i => i.MatchCallvirt(out var mref) && mref.Name == "get_FullName");
                cursor.EmitDelegate((NPC npc) =>
                {
                    if (npc.ModNPC is GhostNPC ghost)
                    {
                        if (Main.netMode == 0)
                            Main.NewText($"The {Lang.GetNPCName(ghost.GhostType)} has arrived...?", 0, 255, 100);
                        else if (Main.netMode == 2)
                        {
                            ChatHelper.BroadcastChatMessage(Terraria.Localization.NetworkText.FromLiteral("A strange NPC has arrived...?"), new Color(0, 255, 100));
                        }
                    }
                    else
                    {
                        npc.netUpdate = true;
                        string fullName = npc.FullName;
                        if (Main.netMode == 0)
                            Main.NewText(Language.GetTextValue("Announcement.HasArrived", fullName), 50, 125);
                        else if (Main.netMode == 2)
                            ChatHelper.BroadcastChatMessage(NetworkText.FromKey("Announcement.HasArrived", npc.GetFullNetName()), new Color(50, 125, 255));
                    }
                });
                cursor.EmitBr(label);
                cursor.GotoNext(i => i.MatchCall(out var mref) && mref.Name == "NotifyProgressionEvent");
                cursor.Index--;
                cursor.MarkLabel(label);
            };

            // Manage Ghost Redeeming
            On_Player.SetTalkNPC += (On_Player.orig_SetTalkNPC orig, Player player, int index, bool fromNet) =>
            {
                if (-1 < index && index <= Main.npc.Length && Main.npc[index].ModNPC is GhostNPC ghost)
                {
                    archipelagoSystem.QueueLocationClient(ArchipelagoSystem.npcIDtoName[ghost.GhostType]);
                    archipelagoSystem.world.checkedNPCs.Add(ghost.GhostType);
                    if (archipelagoSystem.world.npcLocTypeToNpcItemType is not null && archipelagoSystem.world.npcLocTypeToNpcItemType.TryGetValue(ghost.GhostType, out int newNpcType))
                    {
                        Main.npc[index].Transform(newNpcType);
                        orig(player, index, fromNet);
                    }
                    else if (Main.netMode == NetmodeID.MultiplayerClient)
                    {
                        NetMessage.SendStrikeNPC(ghost.NPC, new NPC.HitInfo() { InstantKill = true });
                    }
                    else
                    {
                        ghost.NPC.StrikeInstantKill();
                        NPC.FairyEffects(ghost.NPC.Center, Main.rand.Next(3));
                    }
                }
                else
                {
                    orig(player, index, fromNet);
                }
            };

            // Manage Ghost Occupying Rooms
            On_WorldGen.IsRoomConsideredAlreadyOccupied += (On_WorldGen.orig_IsRoomConsideredAlreadyOccupied orig, int i, int j, int type) =>
            {
                GhostNPC[] existingGhosts = [.. (from npc in Main.npc where npc.ModNPC is GhostNPC select npc.ModNPC as GhostNPC)];
                foreach (var ghost in existingGhosts)
                {
                    if (ghost.NPC.active && ghost.NPC.homeTileX == i && ghost.NPC.homeTileY == j) return true;
                }
                bool result = orig(i, j, type);
                return result;
            };

            On_WorldGen.ScoreRoom_IsThisRoomOccupiedBySomeone += (On_WorldGen.orig_ScoreRoom_IsThisRoomOccupiedBySomeone orig, int ignoreNPC, int npcTypeAsking) =>
            {
                 GhostNPC[] existingGhosts = [.. (from npc in Main.npc where npc.active && npc.ModNPC is GhostNPC select npc.ModNPC as GhostNPC)];
                foreach (var ghost in existingGhosts)
                {
                    for (int i = 0; i < WorldGen.numRoomTiles; i++)
                    {
                        if (ghost.NPC.homeTileX == WorldGen.roomX[i] && ghost.NPC.homeTileY - 1 == WorldGen.roomY[i])
                        {
                            return true;
                        }
                    }
                }
                return orig(ignoreNPC, npcTypeAsking);
            };

            // Manage Town/Ghost NPC Spawn Conditions
            IL_Main.UpdateTime_SpawnTownNPCs += il =>
            {
                var cursor = new ILCursor(il);

                // SKip Active NPC Count Loop (Prevents local bools from getting set)
                cursor.GotoNext(i => i.MatchLdsfld(typeof(Main).GetField(nameof(Main.npc))));
                cursor.GotoNext(i => i.MatchLdfld(typeof(Entity).GetField(nameof(Entity.active))));
                cursor.Index++;
                cursor.EmitPop();
                cursor.EmitLdcI4(0);

                // Disable Static Bools
                cursor.GotoNext(i => i.MatchLdsfld(typeof(WorldGen).GetField(nameof(WorldGen.prioritizedTownNPCType))));
                cursor.Index++;
                cursor.EmitDelegate((int prioritizedType) =>
                {
                    NPC.unlockedArmsDealerSpawn = false;
                    NPC.unlockedDemolitionistSpawn = false;
                    NPC.unlockedDyeTraderSpawn = false;
                    NPC.unlockedMerchantSpawn = false;
                    NPC.unlockedNurseSpawn = false;
                    NPC.unlockedPartyGirlSpawn = false;
                    NPC.unlockedPrincessSpawn = false;
                    NPC.unlockedTruffleSpawn = false;
                    return prioritizedType;
                });

                // Count NPCs
                cursor.GotoNext(i => i.MatchLdsfld(typeof(NPC).GetField(nameof(NPC.downedBoss3))));
                cursor.EmitDelegate(() =>
                {
                    int count = 0;
                    for (int i = 0; i < Main.npc.Length; i++)
                    {
                        if (Main.npc[i].active && Main.npc[i].townNPC)
                        {
                            count++;
                        }
                    }
                      return count;
                });
                cursor.EmitStloc(40);

                // Old Man
                cursor.Index++;
                cursor.EmitPop();
                cursor.EmitDelegate(() =>
                {
                    if (archipelagoSystem.session is null) return NPC.downedBoss3 || NPC.AnyNPCs(NPCID.OldMan);
                    return archipelagoSystem.session.collectedLocations.Contains("Skeletron") || NPC.AnyNPCs(NPCID.OldMan);
                });

                // Town NPCs
                cursor.GotoNext(i => i.MatchLdsfld(typeof(NPC).GetField(nameof(NPC.unlockedSlimeGreenSpawn))));
                cursor.GotoNext(i => i.MatchLdsfld(typeof(NPC).GetField(nameof(NPC.boughtCat))));
                cursor.Index++;
                cursor.EmitPop();
                cursor.EmitDelegate(() =>
                {
                    // Evaluate
                    HashSet<int> validGhostTypes = new();
                    for (int i = 0; i < Main.townNPCCanSpawn.Length; i++)
                        if (Main.townNPCCanSpawn[i] && GhostNPC.GhostableType(i))
                            validGhostTypes.Add(i);
                    // Build
                    if (archipelagoSystem.world.NPCRandoActive())
                        for (int i = 0; i < Main.townNPCCanSpawn.Length; i++)
                            Main.townNPCCanSpawn[i] = archipelagoSystem.world.receivedNPCs.Contains(i);
                    // Remove Dupes
                    for (int i = 0; i < Main.npc.Length; i++)
                    {
                        NPC npc = Main.npc[i];
                        if (!npc.active) continue;
                        if (npc.townNPC)
                        {
                            Main.townNPCCanSpawn[npc.type] = false;
                        }
                        else if (npc.ModNPC is GhostNPC ghost)
                        {
                            validGhostTypes.Remove(ghost.GhostType);
                        }
                    }
                    // Enqueue Ghosts
                    if (archipelagoSystem.world.NPCRandoActive())
                    {
                        foreach (int type in validGhostTypes)
                        {
                            if (!archipelagoSystem.ghostNPCqueue.Contains(type) && !archipelagoSystem.world.checkedNPCs.Contains(type))
                                archipelagoSystem.ghostNPCqueue.Enqueue(type);
                        }
                    }
                    // Set prioritizedNPC if Ghost NPC is next
                    if (archipelagoSystem.ghostNPCqueue.Count > 0)
                    {
                        int ghostType = archipelagoSystem.ghostNPCqueue.Peek();
                        if (ArchipelagoSystem.specialSpawnGhosts.Contains(ghostType))
                        {
                            // If the NPC needs to spawn in a specific house, we need to trick SpawnNPC into triggering only when
                            // the coords found satisfy that NPC's spawn condition...
                            Main.townNPCCanSpawn[ghostType] = true;
                            WorldGen.prioritizedTownNPCType = ghostType;
                        }
                        else
                        {
                            // ...otherwise the value we assign here is arbitrary, it just needs to be set to a town npc's type.
                            Main.townNPCCanSpawn[NPCID.SantaClaus] = true;
                            WorldGen.prioritizedTownNPCType = NPCID.SantaClaus;
                        }
                        return;
                    }
                    // Set prioritizedNPC if Vanilla NPC is next
                    if (WorldGen.prioritizedTownNPCType == 0)
                    {
                        for (int i = 0; i < Main.townNPCCanSpawn.Length; i++)
                        {
                            if (Main.townNPCCanSpawn[i])
                            {
                                WorldGen.prioritizedTownNPCType = i;
                                return;
                            }
                        }
                    }


                });
                cursor.EmitLdsfld(typeof(NPC).GetField(nameof(NPC.boughtCat)));
                cursor.Index--;
            };

            // Allow Bound NPCs/Old Man to spawn until checked
            Terraria.IL_NPC.AI_007_TownEntities += il =>
            {
                var cursor = new ILCursor(il);

                void SkipInstruction(string varName)
                {
                    var label = il.DefineLabel();
                    cursor.GotoNext(i => i.MatchStsfld(typeof(NPC).GetField(varName)));
                    cursor.EmitPop();
                    cursor.EmitBr(label);
                    cursor.GotoNext(i => i.MatchBr(out var _));
                    cursor.MarkLabel(label);
                }

                SkipInstruction(nameof(NPC.savedGolfer));
                SkipInstruction(nameof(NPC.savedTaxCollector));
                SkipInstruction(nameof(NPC.savedGoblin));
                SkipInstruction(nameof(NPC.savedWizard));
                SkipInstruction(nameof(NPC.savedMech));
                SkipInstruction(nameof(NPC.savedStylist));
                SkipInstruction(nameof(NPC.savedAngler));
                SkipInstruction(nameof(NPC.savedBartender));

                cursor.GotoNext(i => i.MatchLdsfld(typeof(NPC).GetField(nameof(NPC.downedBoss3))));
                cursor.Index++;
                cursor.EmitPop();
                cursor.EmitDelegate<Func<bool>>(() => archipelagoSystem.session is null ? NPC.downedBoss3 : archipelagoSystem.session.collectedLocations.Contains("Skeletron"));
            };

            // Add Checks To Bound NPCs

            Terraria.IL_NPC.AI_000_TransformBoundNPC += il =>
            {
                var cursor = new ILCursor(il);
                var skipRando = il.DefineLabel();

                cursor.EmitDelegate(() =>
                {
                    return archipelagoSystem.world.NPCRandoActive();
                });
                cursor.EmitLdcI4(1);
                cursor.EmitBlt(skipRando);

                cursor.EmitLdarg(2);
                cursor.EmitDelegate((int npcType) =>
                {
                    int boundNPCtype;
                    string locName;
                    switch (npcType)
                    {
                        case NPCID.Angler: boundNPCtype = NPCID.SleepingAngler; locName = "Angler"; NPC.savedAngler = true; break;
                        case NPCID.Golfer: boundNPCtype = NPCID.GolferRescue; locName = "Golfer"; NPC.savedGolfer = true; break;
                        case NPCID.DD2Bartender: boundNPCtype = NPCID.BartenderUnconscious; locName = "Tavernkeep"; NPC.savedBartender = true; break;
                        case NPCID.Stylist: boundNPCtype = NPCID.WebbedStylist; locName = "Stylist"; NPC.savedStylist = true; break;
                        case NPCID.GoblinTinkerer: boundNPCtype = NPCID.BoundGoblin; locName = "Goblin Tinkerer"; NPC.savedGoblin = true; break;
                        case NPCID.Mechanic: boundNPCtype = NPCID.BoundMechanic; locName = "Mechanic"; NPC.savedMech = true; break;
                        case NPCID.Wizard: boundNPCtype = NPCID.BoundWizard; locName = "Wizard"; NPC.savedWizard = true; break;
                        default: throw new Exception($"NPC type {npcType} unaccounted for in TransformBoundNPC");
                    }
                    archipelagoSystem.QueueLocationClient(locName);
                    if (archipelagoSystem.world.npcLocTypeToNpcItemType is not null && archipelagoSystem.world.npcLocTypeToNpcItemType.TryGetValue(npcType, out int newNpcType))
                        return newNpcType;
                    NPC npc = Main.npc[NPC.FindFirstNPC(boundNPCtype)];
                    if (Main.netMode == NetmodeID.MultiplayerClient)
                    {
                        NetMessage.SendStrikeNPC(npc, new NPC.HitInfo() { InstantKill = true });
                    }
                    else
                    {
                        npc.StrikeInstantKill();
                    }
                    return 0;
                });
                cursor.EmitStarg(2);
                cursor.EmitLdarg(2);
                cursor.EmitLdcI4(1);
                cursor.EmitBge(skipRando);
                cursor.EmitRet();
                cursor.MarkLabel(skipRando);
                ;
            };

            // Add Check to Purifying Tax Collector
            // The method we edit for this is suspiciously large yet not identified as one that gets affected by garbage collection. If this randomly stops working in gameplay, find a different way to do this.

            Terraria.IL_Projectile.Damage += il =>
            {
                var cursor = new ILCursor(il);
                var label = il.DefineLabel();

                cursor.GotoNext(i => i.MatchCallvirt(typeof(NPC).GetMethod(nameof(NPC.Transform))));
                cursor.Index += 2;
                cursor.MarkLabel(label);
                cursor.Index -= 3;
                cursor.EmitDelegate<Func<NPC, bool>>((NPC npc) =>
                {
                    NPC.savedTaxCollector = true;
                    if (!archipelagoSystem.world.NPCRandoActive()) return false;
                    archipelagoSystem.QueueLocationClient("Tax Collector");

                    if (archipelagoSystem.world.npcLocTypeToNpcItemType.TryGetValue(NPCID.TaxCollector, out int type))
                    {
                        npc.Transform(type);
                        return true;
                    }

                    if (Main.netMode == NetmodeID.MultiplayerClient)
                    {
                        NetMessage.SendStrikeNPC(npc, new NPC.HitInfo() { InstantKill = true });
                    }
                    else
                    {
                        npc.StrikeInstantKill();
                    }
                    return true;
                });
                cursor.EmitLdcI4(1);
                cursor.EmitBge(label);
                cursor.EmitDelegate<Func<NPC>>(() => Main.npc[NPC.FindFirstNPC(NPCID.DemonTaxCollector)]);
            };


            // Torch God reward Terraria.Player:13794
            IL_Player.TorchAttack += il =>
            {
                var cursor = new ILCursor(il);

                cursor.GotoNext(i => i.MatchLdsfld(typeof(Main).GetField(nameof(Main.netMode))));
                cursor.EmitDelegate(() => archipelagoSystem.QueueLocationClient("Torch God"));
                cursor.Emit(OpCodes.Ret);
            };

            // Allow Torch God even if you have `unlockedBiomeTorches`
            IL_Player.UpdateTorchLuck_ConsumeCountersAndCalculate += il =>
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
            IL_NPC.SetEventFlagCleared += il =>
            {
                var cursor = new ILCursor(il);

                var label = cursor.DefineLabel();
                cursor.MarkLabel(label);
                cursor.Emit(OpCodes.Pop);
                cursor.Index--;
                cursor.Emit(OpCodes.Ldarg_1);
                cursor.Emit(OpCodes.Dup);
                cursor.Emit(OpCodes.Ldc_I4_M1);
                cursor.Emit(OpCodes.Beq, label);
                cursor.EmitDelegate((int id) =>
                {
                    var location = id switch
                    {
                        GameEventClearedID.DefeatedSlimeKing => "King Slime",
                        GameEventClearedID.DefeatedEyeOfCthulu => "Eye of Cthulhu",
                        GameEventClearedID.DefeatedEaterOfWorldsOrBrainOfChtulu => "Evil Boss",
                        GameEventClearedID.DefeatedGoblinArmy => "Goblin Army",
                        GameEventClearedID.DefeatedQueenBee => "Queen Bee",
                        GameEventClearedID.DefeatedSkeletron => "Skeletron",
                        GameEventClearedID.DefeatedDeerclops => "Deerclops",
                        GameEventClearedID.DefeatedWallOfFleshAndStartedHardmode => "Wall of Flesh",
                        GameEventClearedID.DefeatedPirates => "Pirate Invasion",
                        GameEventClearedID.DefeatedQueenSlime => "Queen Slime",
                        GameEventClearedID.DefeatedTheTwins => "The Twins",
                        GameEventClearedID.DefeatedDestroyer => "The Destroyer",
                        GameEventClearedID.DefeatedSkeletronPrime => "Skeletron Prime",
                        GameEventClearedID.DefeatedPlantera => "Plantera",
                        GameEventClearedID.DefeatedGolem => "Golem",
                        GameEventClearedID.DefeatedMartians => "Martian Madness",
                        GameEventClearedID.DefeatedFishron => "Duke Fishron",
                        GameEventClearedID.DefeatedHalloweenTree => "Mourning Wood",
                        GameEventClearedID.DefeatedHalloweenKing => "Pumpking",
                        GameEventClearedID.DefeatedChristmassTree => "Everscream",
                        GameEventClearedID.DefeatedSantank => "Santa-NK1",
                        GameEventClearedID.DefeatedIceQueen => "Ice Queen",
                        GameEventClearedID.DefeatedFrostArmy => "Frost Legion",
                        GameEventClearedID.DefeatedEmpressOfLight => "Empress of Light",
                        GameEventClearedID.DefeatedAncientCultist => "Lunatic Cultist",
                        GameEventClearedID.DefeatedMoonlord => "Moon Lord",
                        _ => null,
                    };

                    if (location != null) archipelagoSystem.QueueLocation(location);
                });
                cursor.Emit(OpCodes.Ret);
            };

            // Old One's Army locations
            IL_DD2Event.WinInvasionInternal += il =>
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
                    cursor.EmitDelegate(() =>
                    {
                        flag.SetValue(null, temp);
                        archipelagoSystem.QueueLocation($"Old One's Army Tier {tier}");
                    });
                }
            };

            IL_NPC.DoDeathEvents += il =>
            {
                var cursor = new ILCursor(il);

                // Prevent NPC.downedTower* from being set
                foreach (var flag in new string[] { nameof(NPC.downedTowerSolar), nameof(NPC.downedTowerVortex), nameof(NPC.downedTowerNebula), nameof(NPC.downedTowerStardust) })
                {
                    var field = typeof(NPC).GetField(flag, BindingFlags.Static | BindingFlags.Public);
                    cursor.GotoNext(i => i.MatchStsfld(field));
                    // Crimes
                    cursor.EmitDelegate<Action>(() => temp = (bool)field.GetValue(null));
                    cursor.Index++;
                    cursor.EmitDelegate(() => field.SetValue(null, temp));
                }

                // Prevent NPC.downedMechBossAny from being set
                while (cursor.TryGotoNext(i => i.MatchStsfld(typeof(NPC).GetField(nameof(NPC.downedMechBossAny)))))
                {
                    cursor.EmitDelegate<Action>(() => temp = NPC.downedMechBossAny);
                    cursor.Index++;
                    cursor.EmitDelegate<Action>(() => NPC.downedMechBossAny = temp);
                }

                // Prevent Hardmode generation Terraria.NPC:69104
                cursor.GotoNext(i => i.MatchCall(typeof(WorldGen).GetMethod(nameof(WorldGen.StartHardmode))));
                cursor.EmitDelegate(() =>
                {
                    temp = Main.hardMode;
                    Main.hardMode = true;
                });
                cursor.Index++;
                cursor.EmitDelegate<Action>(() => Main.hardMode = temp);
            };

            IL_WorldGen.UpdateLunarApocalypse += il =>
            {
                var cursor = new ILCursor(il);

                cursor.GotoNext(i => i.MatchCall(typeof(WorldGen).GetMethod(nameof(WorldGen.StartImpendingDoom))));
                cursor.Index--;
                cursor.EmitDelegate(() => archipelagoSystem.QueueLocation("Lunar Events"));
            };

            // Stop loading achievements from disk
            IL_AchievementManager.Load += il =>
            {
                var cursor = new ILCursor(il);
                cursor.Emit(OpCodes.Ret);
            };

            if (Main.netMode != NetmodeID.Server) Main.Achievements.OnAchievementCompleted += OnAchievementCompleted;

            // Unmaintainable reflection

            if (!ModLoader.HasMod("CalamityMod")) return;
            var calamity = ModLoader.GetMod("CalamityMod");

            var calamityAssembly = calamity.GetType().Assembly;
            foreach (var type in calamityAssembly.GetTypes()) switch (type.Name)
                {
                    case "DesertScourgeHead": desertScourgeHeadOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public); break;
                    case "GiantClam": giantClamOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public); break;
                    case "CragmawMire": cragmawMireOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public); break;
                    case "AcidRainEvent": acidRainEventUpdateInvasion = type.GetMethod("UpdateInvasion", BindingFlags.Static | BindingFlags.Public); break;
                    case "Crabulon": crabulonOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public); break;
                    case "HiveMind": hiveMindOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public); break;
                    case "PerforatorHive": perforatorHiveOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public); break;
                    case "SlimeGodCore": slimeGodCoreOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public); break;
                    case "CalamityGlobalNPC":
                        calamityGlobalNpcOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public);
                        calamityGlobalNpcSetNewBossJustDowned = type.GetMethod("SetNewBossJustDowned", BindingFlags.Static | BindingFlags.Public);
                        break;
                    case "AquaticScourgeHead": aquaticScourgeHeadOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public); break;
                    case "Mauler": maulerOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public); break;
                    case "BrimstoneElemental": brimstoneElementalOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public); break;
                    case "Cryogen": cryogenOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public); break;
                    case "CalamitasClone": calamitasCloneOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public); break;
                    case "GreatSandShark": greatSandSharkOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public); break;
                    case "Leviathan": leviathanRealOnKill = type.GetMethod("RealOnKill", BindingFlags.Static | BindingFlags.Public); break;
                    case "AstrumAureus": astrumAureusOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public); break;
                    case "PlaguebringerGoliath": plaguebringerGoliathOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public); break;
                    case "RavagerBody": ravagerBodyOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public); break;
                    case "AstrumDeusHead": astrumDeusHeadOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public); break;
                    case "ProfanedGuardianCommander": profanedGuardianCommanderOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public); break;
                    case "Bumblefuck": bumblefuckOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public); break;
                    case "Providence": providenceOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public); break;
                    case "StormWeaverHead": stormWeaverHeadOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public); break;
                    case "CeaselessVoid": ceaselessVoidOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public); break;
                    case "Signus": signusOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public); break;
                    case "Polterghast": polterghastOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public); break;
                    case "NuclearTerror": nuclearTerrorOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public); break;
                    case "OldDuke": oldDukeOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public); break;
                    case "DevourerofGodsHead": devourerofGodsHeadOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public); break;
                    case "Yharon": yharonOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public); break;
                    case "AresBody": aresBodyOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public); break;
                    case "Apollo": apolloOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public); break;
                    case "ThanatosHead": thanatosHeadOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public); break;
                    case "SupremeCalamitas": supremeCalamitasOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public); break;
                }

            onDesertScourgeHeadOnKill += OnDesertScourgeHeadOnKill;
            onGiantClamOnKill += OnGiantClamOnKill;
            onCragmawMireOnKill += OnCragmawMireOnKill;
            editAcidRainEventUpdateInvasion += EditAcidRainEventUpdateInvasion;
            onCrabulonOnKill += OnCrabulonOnKill;
            onHiveMindOnKill += OnHiveMindOnKill;
            onPerforatorHiveOnKill += OnPerforatorHiveOnKill;
            onSlimeGodCoreOnKill += OnSlimeGodCoreOnKill;
            onCalamityGlobalNpcOnKill += OnCalamityGlobalNpcOnKill;
            editCalamityGlobalNPCOnKill += EditCalamityGlobalNPCOnKill;
            onAquaticScourgeHeadOnKill += OnAquaticScourgeHeadOnKill;
            onMaulerOnKill += OnMaulerOnKill;
            onBrimstoneElementalOnKill += OnBrimstoneElementalOnKill;
            onCryogenOnKill += OnCryogenOnKill;
            onCalamitasCloneOnKill += OnCalamitasCloneOnKill;
            onGreatSandSharkOnKill += OnGreatSandSharkOnKill;
            onLeviathanRealOnKill += OnLeviathanRealOnKill;
            onAstrumAureusOnKill += OnAstrumAureusOnKill;
            onPlaguebringerGoliathOnKill += OnPlaguebringerGoliathOnKill;
            onRavagerBodyOnKill += OnRavagerBodyOnKill;
            onAstrumDeusHeadOnKill += OnAstrumDeusHeadOnKill;
            onProfanedGuardianCommanderOnKill += OnProfanedGuardianCommanderOnKill;
            onBumblefuckOnKill += OnBumblefuckOnKill;
            onProvidenceOnKill += OnProvidenceOnKill;
            onStormWeaverHeadOnKill += OnStormWeaverHeadOnKill;
            onCeaselessVoidOnKill += OnCeaselessVoidOnKill;
            onSignusOnKill += OnSignusOnKill;
            onPolterghastOnKill += OnPolterghastOnKill;
            onNuclearTerrorOnKill += OnNuclearTerrorOnKill;
            onOldDukeOnKill += OnOldDukeOnKill;
            onDevourerofGodsHeadOnKill += OnDevourerofGodsHeadOnKill;
            onYharonOnKill += OnYharonOnKill;
            onAresBodyOnKill += OnAresBodyOnKill;
            onApolloOnKill += OnApolloOnKill;
            onThanatosHeadOnKill += OnThanatosHeadOnKill;
            onSupremeCalamitasOnKill += OnSupremeCalamitasOnKill;
            onCalamityGlobalNpcSetNewBossJustDowned += OnCalamityGlobalNpcSetNewBossJustDowned;
        }

        public override void HandlePacket(BinaryReader reader, int whoAmI)
        {
            var message = reader.ReadString();
            var archipelagoSystem = ModContent.GetInstance<ArchipelagoSystem>();

            // The way we handle packets kind of sucks. It's using string IDs with some special
            // cases.
            if (message == "") archipelagoSystem.Chat(archipelagoSystem.Status(), whoAmI);
            else if (message.StartsWith("deathlink")) archipelagoSystem.TriggerDeathlink(message.Substring(9), whoAmI);
            else if (message.StartsWith("[DeathLink]"))
            {
                var player = Main.player[Main.myPlayer];
                if (player.active && !player.dead) player.Hurt(PlayerDeathReason.ByCustomReason(message), 999999, 1);
            }
            else if (message == "YouGotAnItem") Main.LocalPlayer.GetModPlayer<ArchipelagoPlayer>().ReceivedReward(reader.ReadInt32());
            else if (message == "RecievedRewardsForSetupShop")
            {
                var rewards = archipelagoSystem.ReceivedRewards();

                var packet = GetPacket();
                packet.Write("SetupShop");
                foreach (var reward in rewards) packet.Write(reward);
                packet.Write(-1);
                var player = Main.player[whoAmI];
                var position = player.position;
                var npc = NPC.NewNPC(new EntitySource_Misc("Open collection"), (int)position.X, (int)position.Y, ModContent.NPCType<CollectionNPC>(), 0, whoAmI, reader.ReadInt32());
                player.SetTalkNPC(npc);
                packet.Write(npc);
                packet.Send(whoAmI);
            }
            else if (message == "SetupShop")
            {
                var items = new List<int>();

                while (true)
                {
                    var item = reader.ReadInt32();
                    if (item == -1) break;
                    items.Add(item);
                }

                CollectionButton.SetupShop(items, reader.ReadInt32());
            }
            else archipelagoSystem.QueueLocation(message);
        }

        public override void Unload()
        {
            if (Main.netMode != NetmodeID.Server) Main.Achievements.OnAchievementCompleted -= OnAchievementCompleted;

            if (!ModLoader.HasMod("CalamityMod")) return;

            onDesertScourgeHeadOnKill -= OnDesertScourgeHeadOnKill;
            onGiantClamOnKill -= OnGiantClamOnKill;
            onCragmawMireOnKill -= OnCragmawMireOnKill;
            editAcidRainEventUpdateInvasion -= EditAcidRainEventUpdateInvasion;
            onCrabulonOnKill -= OnCrabulonOnKill;
            onHiveMindOnKill -= OnHiveMindOnKill;
            onPerforatorHiveOnKill -= OnPerforatorHiveOnKill;
            onSlimeGodCoreOnKill -= OnSlimeGodCoreOnKill;
            onCalamityGlobalNpcOnKill -= OnCalamityGlobalNpcOnKill;
            editCalamityGlobalNPCOnKill -= EditCalamityGlobalNPCOnKill;
            onAquaticScourgeHeadOnKill -= OnAquaticScourgeHeadOnKill;
            onMaulerOnKill -= OnMaulerOnKill;
            onBrimstoneElementalOnKill -= OnBrimstoneElementalOnKill;
            onCryogenOnKill -= OnCryogenOnKill;
            onCalamitasCloneOnKill -= OnCalamitasCloneOnKill;
            onGreatSandSharkOnKill -= OnGreatSandSharkOnKill;
            onLeviathanRealOnKill -= OnLeviathanRealOnKill;
            onAstrumAureusOnKill -= OnAstrumAureusOnKill;
            onPlaguebringerGoliathOnKill -= OnPlaguebringerGoliathOnKill;
            onRavagerBodyOnKill -= OnRavagerBodyOnKill;
            onAstrumDeusHeadOnKill -= OnAstrumDeusHeadOnKill;
            onProfanedGuardianCommanderOnKill -= OnProfanedGuardianCommanderOnKill;
            onBumblefuckOnKill -= OnBumblefuckOnKill;
            onProvidenceOnKill -= OnProvidenceOnKill;
            onStormWeaverHeadOnKill -= OnStormWeaverHeadOnKill;
            onCeaselessVoidOnKill -= OnCeaselessVoidOnKill;
            onSignusOnKill -= OnSignusOnKill;
            onPolterghastOnKill -= OnPolterghastOnKill;
            onNuclearTerrorOnKill -= OnNuclearTerrorOnKill;
            onOldDukeOnKill -= OnOldDukeOnKill;
            onDevourerofGodsHeadOnKill -= OnDevourerofGodsHeadOnKill;
            onYharonOnKill -= OnYharonOnKill;
            onAresBodyOnKill -= OnAresBodyOnKill;
            onApolloOnKill -= OnApolloOnKill;
            onThanatosHeadOnKill -= OnThanatosHeadOnKill;
            onSupremeCalamitasOnKill -= OnSupremeCalamitasOnKill;
            onCalamityGlobalNpcSetNewBossJustDowned -= OnCalamityGlobalNpcSetNewBossJustDowned;
        }

        void OnAchievementCompleted(Achievement achievement)
        {
            var name = achievement.Name switch
            {
                "TIMBER" => "Timber!!",
                "BENCHED" => "Benched",
                "OBTAIN_HAMMER" => "Stop! Hammer Time!",
                "MATCHING_ATTIRE" => "Matching Attire",
                "FASHION_STATEMENT" => "Fashion Statement",
                "OOO_SHINY" => "Ooo! Shiny!",
                "NO_HOBO" => "No Hobo",
                "HEAVY_METAL" => "Heavy Metal",
                "FREQUENT_FLYER" => "The Frequent Flyer",
                "GET_GOLDEN_DELIGHT" => "Feast of Midas",
                "DYE_HARD" => "Dye Hard",
                "LUCKY_BREAK" => "Lucky Break",
                "STAR_POWER" => "Star Power",
                "YOU_CAN_DO_IT" => "You Can Do It!",
                "DRINK_BOTTLED_WATER_WHILE_DROWNING" => "Unusual Survival Strategies",
                "TURN_GNOME_TO_STATUE" => "Heliophobia",
                "ARCHAEOLOGIST" => "Archaeologist",
                "PET_THE_PET" => "Feeling Petty",
                "FLY_A_KITE_ON_A_WINDY_DAY" => "A Rather Blustery Day",
                "PRETTY_IN_PINK" => "Pretty in Pink",
                "MARATHON_MEDALIST" => "Marathon Medalist",
                "SERVANT_IN_TRAINING" => "Servant-in-Training",
                "GOOD_LITTLE_SLAVE" => "10 Fishing Quests",
                "TROUT_MONKEY" => "Trout Monkey",
                "GLORIOUS_GOLDEN_POLE" => "Glorious Golden Pole",
                "FAST_AND_FISHIOUS" => "Fast and Fishious",
                "SUPREME_HELPER_MINION" => "Supreme Helper Minion!",
                "INTO_ORBIT" => "Into Orbit",
                "WATCH_YOUR_STEP" => "Watch Your Step!",
                "THROWING_LINES" => "Throwing Lines",
                "VEHICULAR_MANSLAUGHTER" => "Vehicular Manslaughter",
                "FIND_A_FAIRY" => "Hey! Listen!",
                "I_AM_LOOT" => "I Am Loot!",
                "HEART_BREAKER" => "Heart Breaker",
                "HOLD_ON_TIGHT" => "Hold on Tight!",
                "LIKE_A_BOSS" => "Like a Boss",
                "JEEPERS_CREEPERS" => "Jeepers Creepers",
                "FUNKYTOWN" => "Funkytown",
                "DECEIVER_OF_FOOLS" => "Deceiver of Fools",
                "DIE_TO_DEAD_MANS_CHEST" => "Dead Men Tell No Tales",
                "BULLDOZER" => "Bulldozer",
                "THERE_ARE_SOME_WHO_CALL_HIM" => "There are Some Who Call Him...",
                "THROW_A_PARTY" => "Jolly Jamboree",
                "TRANSMUTE_ITEM" => "A Shimmer In The Dark",
                "ITS_GETTING_HOT_IN_HERE" => "It's Getting Hot in Here",
                "ROCK_BOTTOM" => "Rock Bottom",
                "SMASHING_POPPET" => "Smashing, Poppet!",
                "TALK_TO_NPC_AT_MAX_HAPPINESS" => "Leading Landlord",
                "COMPLETELY_AWESOME" => "Completely Awesome",
                "STICKY_SITUATION" => "Sticky Situation",
                "THE_CAVALRY" => "The Cavalry",
                "BLOODBATH" => "Bloodbath",
                "TIL_DEATH" => "Til Death...",
                "FOUND_GRAVEYARD" => "Quiet Neighborhood",
                "PURIFY_ENTIRE_WORLD" => "And Good Riddance!",
                "MINER_FOR_FIRE" => "Miner for Fire",
                "GO_LAVA_FISHING" => "Hot Reels!",
                "GET_TERRASPARK_BOOTS" => "Boots of the Hero",
                "WHERES_MY_HONEY" => "Where's My Honey?",
                "NOT_THE_BEES" => "Not the Bees!",
                "DUNGEON_HEIST" => "Dungeon Heist",
                "GET_CELL_PHONE" => "Black Mirror",
                "BEGONE_EVIL" => "Begone, Evil!",
                "EXTRA_SHINY" => "Extra Shiny!",
                "GET_ANKH_SHIELD" => "Ankhumulation Complete",
                "GELATIN_WORLD_TOUR" => "Gelatin World Tour",
                "HEAD_IN_THE_CLOUDS" => "Head in the Clouds",
                "DEFEAT_DREADNAUTILUS" => "Don't Dread on Me",
                "IT_CAN_TALK" => "It Can Talk?!",
                "ALL_TOWN_SLIMES" => "The Great Slime Mitosis",
                "PRISMANCER" => "Prismancer",
                "GET_A_LIFE" => "Get a Life",
                "TOPPED_OFF" => "Topped Off",
                "BUCKETS_OF_BOLTS" => "Buckets of Bolts",
                "MECHA_MAYHEM" => "Mecha Mayhem",
                "DRAX_ATTAX" => "Drax Attax",
                "PHOTOSYNTHESIS" => "Photosynthesis",
                "YOU_AND_WHAT_ARMY" => "You and What Army?",
                "TO_INFINITY_AND_BEYOND" => "To Infinity... and Beyond!",
                "REAL_ESTATE_AGENT" => "Real Estate Agent",
                "ROBBING_THE_GRAVE" => "Robbing the Grave",
                "BIG_BOOTY" => "Big Booty",
                "RAINBOWS_AND_UNICORNS" => "Rainbows and Unicorns",
                "TEMPLE_RAIDER" => "Temple Raider",
                "SWORD_OF_THE_HERO" => "Sword of the Hero",
                "KILL_THE_SUN" => "Kill the Sun",
                "BALEFUL_HARVEST" => "Baleful Harvest",
                "ICE_SCREAM" => "Ice Scream",
                "SLAYER_OF_WORLDS" => "Slayer of Worlds",
                "SICK_THROW" => "Sick Throw",
                "GET_ZENITH" => "Infinity +1 Sword",
                _ => null,
            };

            if (name != null) ModContent.GetInstance<ArchipelagoSystem>().QueueLocationClient(name);
            ModContent.GetInstance<ArchipelagoSystem>().Achieved(achievement.Name);
        }

        delegate void OnKill(ModNPC self);

        void OnDesertScourgeHeadOnKill(OnKill orig, ModNPC self)
        {
            if (temp) orig(self);
            else ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Desert Scourge");
        }

        void OnGiantClamOnKill(OnKill orig, ModNPC self)
        {
            if (temp) orig(self);
            else
            {
                ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Giant Clam");
                if (Main.hardMode) ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Hardmode Giant Clam");
            }
        }

        void OnCragmawMireOnKill(OnKill orig, ModNPC self)
        {
            if (temp) orig(self);
            else ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Cragmaw Mire");
        }

        void EditAcidRainEventUpdateInvasion(ILContext il)
        {
            var archipelagoSystem = ModContent.GetInstance<ArchipelagoSystem>();
            var calamitySystem = ModContent.GetInstance<CalamitySystem>();
            var cursor = new ILCursor(il);

            cursor.GotoNext(i => i.MatchLdarg(0));
            cursor.Index++;
            cursor.EmitDelegate<Action<bool>>(won =>
            {
                if (won)
                {
                    archipelagoSystem.QueueLocation("Acid Rain Tier 1");
                    if (calamitySystem.DownedAquaticScourge()) archipelagoSystem.QueueLocation("Acid Rain Tier 2");
                }
            });
            cursor.Emit(OpCodes.Ldc_I4_0);
        }

        void OnCrabulonOnKill(OnKill orig, ModNPC self)
        {
            if (temp) orig(self);
            else ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Crabulon");
        }

        void OnHiveMindOnKill(OnKill orig, ModNPC self)
        {
            if (temp) orig(self);
            else ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("The Hive Mind");
        }

        void OnPerforatorHiveOnKill(OnKill orig, ModNPC self)
        {
            if (temp) orig(self);
            else ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("The Perforators");
        }

        void OnSlimeGodCoreOnKill(OnKill orig, ModNPC self)
        {
            if (temp) orig(self);
            else ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("The Slime God");
        }

        int[] vanillaBosses = { NPCID.KingSlime, NPCID.EyeofCthulhu, NPCID.EaterofWorldsHead, NPCID.EaterofWorldsBody, NPCID.EaterofWorldsTail, NPCID.BrainofCthulhu, NPCID.QueenBee, NPCID.SkeletronHead, NPCID.Deerclops, NPCID.WallofFlesh, NPCID.BloodNautilus, NPCID.QueenSlimeBoss, NPCID.Retinazer, NPCID.Spazmatism, NPCID.TheDestroyer, NPCID.SkeletronPrime, NPCID.Plantera, NPCID.Golem, NPCID.DukeFishron, NPCID.MourningWood, NPCID.Pumpking, NPCID.Everscream, NPCID.SantaNK1, NPCID.IceQueen, NPCID.HallowBoss, NPCID.CultistBoss, NPCID.MoonLordCore };

        delegate void CalamityGlobalNpcOnKill(object self, NPC npc);
        void OnCalamityGlobalNpcOnKill(CalamityGlobalNpcOnKill orig, object self, NPC npc)
        {
            if (temp || !vanillaBosses.Contains(npc.type)) orig(self, npc);
            else ModContent.GetInstance<CalamitySystem>().HandleBossRush(npc);
        }

        void EditCalamityGlobalNPCOnKill(ILContext il)
        {
            var seldomArchipelago = ModContent.GetInstance<ArchipelagoSystem>();
            var cursor = new ILCursor(il);

            cursor.GotoNext(i => i.MatchLdcI4(NPCID.WallofFlesh));
            cursor.Emit(OpCodes.Pop);
            cursor.Emit(OpCodes.Ldc_I4_0);
        }

        void OnAquaticScourgeHeadOnKill(OnKill orig, ModNPC self)
        {
            if (temp) orig(self);
            else ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Aquatic Scourge");
        }

        void OnMaulerOnKill(OnKill orig, ModNPC self)
        {
            if (temp) orig(self);
            else ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Mauler");
        }

        void OnBrimstoneElementalOnKill(OnKill orig, ModNPC self)
        {
            if (temp) orig(self);
            else ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Brimstone Elemental");
        }

        void OnCryogenOnKill(OnKill orig, ModNPC self)
        {
            if (temp) orig(self);
            else ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Cryogen");
        }

        void OnCalamitasCloneOnKill(OnKill orig, ModNPC self)
        {
            if (temp) orig(self);
            else ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Calamitas Clone");
        }

        void OnGreatSandSharkOnKill(OnKill orig, ModNPC self)
        {
            if (temp) orig(self);
            else ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Great Sand Shark");
        }

        delegate void RealOnKill(NPC npc);
        void OnLeviathanRealOnKill(RealOnKill orig, NPC npc)
        {
            if (temp) orig(npc);
            else ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Leviathan and Anahita");
        }

        void OnAstrumAureusOnKill(OnKill orig, ModNPC self)
        {
            if (temp) orig(self);
            else ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Astrum Aureus");
        }

        void OnPlaguebringerGoliathOnKill(OnKill orig, ModNPC self)
        {
            if (temp) orig(self);
            else ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("The Plaguebringer Goliath");
        }

        void OnRavagerBodyOnKill(OnKill orig, ModNPC self)
        {
            if (temp) orig(self);
            else ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Ravager");
        }

        void OnAstrumDeusHeadOnKill(OnKill orig, ModNPC self)
        {
            if (temp) orig(self);
            else ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Astrum Deus");
        }

        void OnProfanedGuardianCommanderOnKill(OnKill orig, ModNPC self)
        {
            if (temp) orig(self);
            else ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Profaned Guardians");
        }

        void OnBumblefuckOnKill(OnKill orig, ModNPC self)
        {
            if (temp) orig(self);
            else ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("The Dragonfolly");
        }

        void OnProvidenceOnKill(OnKill orig, ModNPC self)
        {
            if (temp) orig(self);
            else ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Providence, the Profaned Goddess");
        }

        void OnStormWeaverHeadOnKill(OnKill orig, ModNPC self)
        {
            if (temp) orig(self);
            else ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Storm Weaver");
        }

        void OnCeaselessVoidOnKill(OnKill orig, ModNPC self)
        {
            if (temp) orig(self);
            else ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Ceaseless Void");
        }

        void OnSignusOnKill(OnKill orig, ModNPC self)
        {
            if (temp) orig(self);
            else ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Signus, Envoy of the Devourer");
        }

        void OnPolterghastOnKill(OnKill orig, ModNPC self)
        {
            if (temp) orig(self);
            else ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Polterghast");
        }

        void OnNuclearTerrorOnKill(OnKill orig, ModNPC self)
        {
            if (temp) orig(self);
            else ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Nuclear Terror");
        }

        void OnOldDukeOnKill(OnKill orig, ModNPC self)
        {
            if (temp) orig(self);
            else ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("The Old Duke");
        }

        void OnDevourerofGodsHeadOnKill(OnKill orig, ModNPC self)
        {
            if (temp) orig(self);
            else ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("The Devourer of Gods");
        }

        void OnYharonOnKill(OnKill orig, ModNPC self)
        {
            if (temp) orig(self);
            else ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Yharon, Dragon of Rebirth");
        }

        void OnAresBodyOnKill(OnKill orig, ModNPC self)
        {
            if (temp) orig(self);
            else if (ModContent.GetInstance<CalamitySystem>().AreExosDead(0)) ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Exo Mechs");
        }

        void OnApolloOnKill(OnKill orig, ModNPC self)
        {
            if (temp) orig(self);
            else if (ModContent.GetInstance<CalamitySystem>().AreExosDead(1)) ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Exo Mechs");
        }

        void OnThanatosHeadOnKill(OnKill orig, ModNPC self)
        {
            if (temp) orig(self);
            else if (ModContent.GetInstance<CalamitySystem>().AreExosDead(2)) ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Exo Mechs");
        }

        void OnSupremeCalamitasOnKill(OnKill orig, ModNPC self)
        {
            if (temp) orig(self);
            else ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Supreme Witch, Calamitas");
        }

        delegate void CalamityGlobalNpcSetNewBossJustDowned(NPC npc);
        void OnCalamityGlobalNpcSetNewBossJustDowned(CalamityGlobalNpcSetNewBossJustDowned orig, NPC npc) { }

        delegate void OnOnKill(OnKill orig, ModNPC self);

        static event OnOnKill onDesertScourgeHeadOnKill
        {
            add => MonoModHooks.Add(desertScourgeHeadOnKill, value);
            remove { }
        }

        static event OnOnKill onGiantClamOnKill
        {
            add => MonoModHooks.Add(giantClamOnKill, value);
            remove { }
        }

        static event OnOnKill onCragmawMireOnKill
        {
            add => MonoModHooks.Add(cragmawMireOnKill, value);
            remove { }
        }

        static event ILContext.Manipulator editAcidRainEventUpdateInvasion
        {
            add => MonoModHooks.Modify(acidRainEventUpdateInvasion, value);
            remove { }
        }

        static event OnOnKill onCrabulonOnKill
        {
            add => MonoModHooks.Add(crabulonOnKill, value);
            remove { }
        }

        static event OnOnKill onHiveMindOnKill
        {
            add => MonoModHooks.Add(hiveMindOnKill, value);
            remove { }
        }

        static event OnOnKill onPerforatorHiveOnKill
        {
            add => MonoModHooks.Add(perforatorHiveOnKill, value);
            remove { }
        }

        static event OnOnKill onSlimeGodCoreOnKill
        {
            add => MonoModHooks.Add(slimeGodCoreOnKill, value);
            remove { }
        }

        delegate void OnCalamityGlobalNpcOnKillTy(CalamityGlobalNpcOnKill orig, object self, NPC npc);
        static event OnCalamityGlobalNpcOnKillTy onCalamityGlobalNpcOnKill
        {
            add => MonoModHooks.Add(calamityGlobalNpcOnKill, value);
            remove { }
        }

        static event ILContext.Manipulator editCalamityGlobalNPCOnKill
        {
            add => MonoModHooks.Modify(calamityGlobalNpcOnKill, value);
            remove { }
        }

        static event OnOnKill onAquaticScourgeHeadOnKill
        {
            add => MonoModHooks.Add(aquaticScourgeHeadOnKill, value);
            remove { }
        }

        static event OnOnKill onMaulerOnKill
        {
            add => MonoModHooks.Add(maulerOnKill, value);
            remove { }
        }

        static event OnOnKill onBrimstoneElementalOnKill
        {
            add => MonoModHooks.Add(brimstoneElementalOnKill, value);
            remove { }
        }

        static event OnOnKill onCryogenOnKill
        {
            add => MonoModHooks.Add(cryogenOnKill, value);
            remove { }
        }

        static event OnOnKill onCalamitasCloneOnKill
        {
            add => MonoModHooks.Add(calamitasCloneOnKill, value);
            remove { }
        }

        static event OnOnKill onGreatSandSharkOnKill
        {
            add => MonoModHooks.Add(greatSandSharkOnKill, value);
            remove { }
        }

        delegate void OnRealOnKill(RealOnKill orig, NPC npc);
        static event OnRealOnKill onLeviathanRealOnKill
        {
            add => MonoModHooks.Add(leviathanRealOnKill, value);
            remove { }
        }

        static event OnOnKill onAstrumAureusOnKill
        {
            add => MonoModHooks.Add(astrumAureusOnKill, value);
            remove { }
        }

        static event OnOnKill onPlaguebringerGoliathOnKill
        {
            add => MonoModHooks.Add(plaguebringerGoliathOnKill, value);
            remove { }
        }

        static event OnOnKill onRavagerBodyOnKill
        {
            add => MonoModHooks.Add(ravagerBodyOnKill, value);
            remove { }
        }

        static event OnOnKill onAstrumDeusHeadOnKill
        {
            add => MonoModHooks.Add(astrumDeusHeadOnKill, value);
            remove { }
        }

        static event OnOnKill onProfanedGuardianCommanderOnKill
        {
            add => MonoModHooks.Add(profanedGuardianCommanderOnKill, value);
            remove { }
        }

        static event OnOnKill onBumblefuckOnKill
        {
            add => MonoModHooks.Add(bumblefuckOnKill, value);
            remove { }
        }

        static event OnOnKill onProvidenceOnKill
        {
            add => MonoModHooks.Add(providenceOnKill, value);
            remove { }
        }

        static event OnOnKill onStormWeaverHeadOnKill
        {
            add => MonoModHooks.Add(stormWeaverHeadOnKill, value);
            remove { }
        }

        static event OnOnKill onCeaselessVoidOnKill
        {
            add => MonoModHooks.Add(ceaselessVoidOnKill, value);
            remove { }
        }

        static event OnOnKill onSignusOnKill
        {
            add => MonoModHooks.Add(signusOnKill, value);
            remove { }
        }

        static event OnOnKill onPolterghastOnKill
        {
            add => MonoModHooks.Add(polterghastOnKill, value);
            remove { }
        }

        static event OnOnKill onNuclearTerrorOnKill
        {
            add => MonoModHooks.Add(nuclearTerrorOnKill, value);
            remove { }
        }

        static event OnOnKill onOldDukeOnKill
        {
            add => MonoModHooks.Add(oldDukeOnKill, value);
            remove { }
        }

        static event OnOnKill onDevourerofGodsHeadOnKill
        {
            add => MonoModHooks.Add(devourerofGodsHeadOnKill, value);
            remove { }
        }

        static event OnOnKill onYharonOnKill
        {
            add => MonoModHooks.Add(yharonOnKill, value);
            remove { }
        }

        static event OnOnKill onAresBodyOnKill
        {
            add => MonoModHooks.Add(aresBodyOnKill, value);
            remove { }
        }

        static event OnOnKill onApolloOnKill
        {
            add => MonoModHooks.Add(apolloOnKill, value);
            remove { }
        }

        static event OnOnKill onThanatosHeadOnKill
        {
            add => MonoModHooks.Add(thanatosHeadOnKill, value);
            remove { }
        }

        static event OnOnKill onSupremeCalamitasOnKill
        {
            add => MonoModHooks.Add(supremeCalamitasOnKill, value);
            remove { }
        }

        delegate void OnCalamityGlobalNpcSetNewBossJustDownedTy(CalamityGlobalNpcSetNewBossJustDowned orig, NPC npc);
        static event OnCalamityGlobalNpcSetNewBossJustDownedTy onCalamityGlobalNpcSetNewBossJustDowned
        {
            add => MonoModHooks.Add(calamityGlobalNpcSetNewBossJustDowned, value);
            remove { }
        }
    }
}
