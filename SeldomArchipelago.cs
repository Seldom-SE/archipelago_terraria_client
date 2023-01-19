using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using SeldomArchipelago.Systems;
using System.Reflection;
using Terraria;
using Terraria.GameContent.Events;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Social;
using System.IO;

namespace SeldomArchipelago
{
    public class SeldomArchipelago : Mod
    {
        // TODO
        // Mod has to be loaded twice

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

            On.Terraria.Achievements.AchievementCondition.Complete += (orig, condition) =>
            {
                var mode = typeof(SocialAPI).GetField("_mode", BindingFlags.Static | BindingFlags.NonPublic);
                mode.SetValue(null, SocialMode.None);
                orig(condition);
                mode.SetValue(null, SocialMode.Steam);
            };

            if (Main.netMode == NetmodeID.Server) return;

            Main.Achievements.OnAchievementCompleted += achievement =>
            {
                var name = achievement.Name switch
                {
                    "TIMBER" => "Timber!!",
                    "NO_HOBO" => "No Hobo",
                    "OBTAIN_HAMMER" => "Stop! Hammer Time!",
                    "OOO_SHINY" => "Ooo! Shiny!",
                    "HEART_BREAKER" => "Heart Breaker",
                    "HEAVY_METAL" => "Heavy Metal",
                    "I_AM_LOOT" => "I Am Loot!",
                    "STAR_POWER" => "Star Power",
                    "HOLD_ON_TIGHT" => "Hold on Tight!",
                    "SMASHING_POPPET" => "Smashing, Poppet!",
                    "WHERES_MY_HONEY" => "Where's My Honey?",
                    "DUNGEON_HEIST" => "Dungeon Heist",
                    "ITS_GETTING_HOT_IN_HERE" => "It's Getting Hot in Here",
                    "MINER_FOR_FIRE" => "Miner for Fire",
                    "LIKE_A_BOSS" => "Like a Boss",
                    "BLOODBATH" => "Bloodbath",
                    "NOT_THE_BEES" => "Not the Bees!",
                    "JEEPERS_CREEPERS" => "Jeepers Creepers",
                    "FUNKYTOWN" => "Funkytown",
                    "INTO_ORBIT" => "Into Orbit",
                    "ROCK_BOTTOM" => "Rock Bottom",
                    "FASHION_STATEMENT" => "Fashion Statement",
                    "VEHICULAR_MANSLAUGHTER" => "Vehicular Manslaughter",
                    "LUCKY_BREAK" => "Lucky Break",
                    "THROWING_LINES" => "Throwing Lines",
                    "DYE_HARD" => "Dye Hard",
                    "THE_CAVALRY" => "The Cavalry",
                    "COMPLETELY_AWESOME" => "Completely Awesome",
                    "TIL_DEATH" => "Til Death...",
                    "WATCH_YOUR_STEP" => "Watch Your Step!",
                    "YOU_CAN_DO_IT" => "You Can Do It!",
                    "MATCHING_ATTIRE" => "Matching Attire",
                    "BENCHED" => "Benched",
                    "FOUND_GRAVEYARD" => "Quiet Neighborhood",
                    "GO_LAVA_FISHING" => "Hot Reels!",
                    "TALK_TO_NPC_AT_MAX_HAPPINESS" => "Leading Landlord",
                    "PET_THE_PET" => "Feeling Petty",
                    "DIE_TO_DEAD_MANS_CHEST" => "Dead Men Tell No Tales",
                    "STICKY_SITUATION" => "Sticky Situation",
                    "THERE_ARE_SOME_WHO_CALL_HIM" => "There are Some Who Call Him...",
                    "DECEIVER_OF_FOOLS" => "Deceiver of Fools",
                    "ARCHAEOLOGIST" => "Archaeologist",
                    "PRETTY_IN_PINK" => "Pretty in Pink",
                    "GET_TERRASPARK_BOOTS" => "Boots of the Hero",
                    "FLY_A_KITE_ON_A_WINDY_DAY" => "A Rather Blustery Day",
                    "TURN_GNOME_TO_STATUE" => "Heliophobia",
                    "THROW_A_PARTY" => "Jolly Jamboree",
                    "GLORIOUS_GOLDEN_POLE" => "Glorious Golden Pole",
                    "SERVANT_IN_TRAINING" => "Servant-in-Training",
                    "GOOD_LITTLE_SLAVE" => "10 Fishing Quests",
                    "TROUT_MONKEY" => "Trout Monkey",
                    "FAST_AND_FISHIOUS" => "Fast and Fishious",
                    "SUPREME_HELPER_MINION" => "Supreme Helper Minion!",
                    "HEAD_IN_THE_CLOUDS" => "Head in the Clouds",
                    "BEGONE_EVIL" => "Begone, Evil!",
                    "EXTRA_SHINY" => "Extra Shiny!",
                    "DRAX_ATTAX" => "Drax Attax",
                    "PHOTOSYNTHESIS" => "Photosynthesis",
                    "GET_A_LIFE" => "Get a Life",
                    "KILL_THE_SUN" => "Kill the Sun",
                    "MECHA_MAYHEM" => "Mecha Mayhem",
                    "PRISMANCER" => "Prismancer",
                    "IT_CAN_TALK" => "It Can Talk?!",
                    "GELATIN_WORLD_TOUR" => "Gelatin World Tour",
                    "TOPPED_OFF" => "Topped Off",
                    "DEFEAT_DREADNAUTILUS" => "Don't Dread on Me",
                    "TEMPLE_RAIDER" => "Temple Raider",
                    "ROBBING_THE_GRAVE" => "Robbing the Grave",
                    "BALEFUL_HARVEST" => "Baleful Harvest",
                    "ICE_SCREAM" => "Ice Scream",
                    "SWORD_OF_THE_HERO" => "Sword of the Hero",
                    "BIG_BOOTY" => "Big Booty",
                    "REAL_ESTATE_AGENT" => "Real Estate Agent",
                    "RAINBOWS_AND_UNICORNS" => "Rainbows and Unicorns",
                    "SICK_THROW" => "Sick Throw",
                    "YOU_AND_WHAT_ARMY" => "You and What Army?",
                    _ => null,
                };

                if (name != null) ArchipelagoSystem.QueueLocationClient(name);
            };
        }

        public override void HandlePacket(BinaryReader reader, int whoAmI)
        {
            var message = reader.ReadString();
            if (message == "")
            {
                ArchipelagoSystem.Chat(ArchipelagoSystem.Status(), whoAmI);
            }
            else
            {
                ArchipelagoSystem.QueueLocation(message);
            }
        }
    }
}
