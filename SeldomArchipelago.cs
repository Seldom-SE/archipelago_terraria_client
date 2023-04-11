using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using SeldomArchipelago.Systems;
using Terraria;
using Terraria.Achievements;
using Terraria.GameContent.Events;
using Terraria.ID;
using Terraria.ModLoader;
using System.IO;

namespace SeldomArchipelago
{
    public class SeldomArchipelago : Mod
    {
        // TODO
        // Make achievements UI sync
        // Save temp achievement criteria
        // /apstart reconnect
        // Magic Storage compat
        // Broken on Linux

        // Terraria is single-threaded so this *should* be fine
        bool temp;

        public override void Load()
        {
            var archipelagoSystem = ModContent.GetInstance<ArchipelagoSystem>();

            // Begin cursed IL editing

            // Torch God reward Terraria.Player:13794
            IL.Terraria.Player.TorchAttack += il =>
            {
                var cursor = new ILCursor(il);

                cursor.GotoNext(i => i.MatchLdsfld(typeof(Main).GetField(nameof(Main.netMode))));
                cursor.EmitDelegate<Action>(() => archipelagoSystem.QueueLocationClient("Torch God"));
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
                cursor.EmitDelegate<Action<int>>((int id) => archipelagoSystem.QueueLocation(id switch
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
                    GameEventClearedID.DefeatedMartians => "Martian Invasion",
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
                        archipelagoSystem.QueueLocation($"Old One's Army Tier {tier}");
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

            if (Main.netMode != NetmodeID.Server) Main.Achievements.OnAchievementCompleted += OnAchievementCompleted;
        }

        public override void HandlePacket(BinaryReader reader, int whoAmI)
        {
            var message = reader.ReadString();
            var archipelagoSystem = ModContent.GetInstance<ArchipelagoSystem>();
            if (message == "")
            {
                archipelagoSystem.Chat(archipelagoSystem.Status(), whoAmI);
            }
            else
            {
                archipelagoSystem.QueueLocation(message);
            }
        }

        public override void Unload()
        {
            if (Main.netMode != NetmodeID.Server) Main.Achievements.OnAchievementCompleted -= OnAchievementCompleted;
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
                "DYE_HARD" => "Dye Hard",
                "LUCKY_BREAK" => "Lucky Break",
                "STAR_POWER" => "Star Power",
                "YOU_CAN_DO_IT" => "You Can Do It!",
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
                "I_AM_LOOT" => "I Am Loot!",
                "HEART_BREAKER" => "Heart Breaker",
                "HOLD_ON_TIGHT" => "Hold on Tight!",
                "LIKE_A_BOSS" => "Like a Boss",
                "JEEPERS_CREEPERS" => "Jeepers Creepers",
                "DECEIVER_OF_FOOLS" => "Deceiver of Fools",
                "DIE_TO_DEAD_MANS_CHEST" => "Dead Men Tell No Tales",
                "BULLDOZER" => "Bulldozer",
                "THERE_ARE_SOME_WHO_CALL_HIM" => "There are Some Who Call Him...",
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
                "THROW_A_PARTY" => "Jolly Jamboree",
                "MINER_FOR_FIRE" => "Miner for Fire",
                "GO_LAVA_FISHING" => "Hot Reels!",
                "GET_TERRASPARK_BOOTS" => "Boots of the Hero",
                "WHERES_MY_HONEY" => "Where's My Honey?",
                "NOT_THE_BEES" => "Not the Bees!",
                "DUNGEON_HEIST" => "Dungeon Heist",
                "BEGONE_EVIL" => "Begone, Evil!",
                "EXTRA_SHINY" => "Extra Shiny!",
                "HEAD_IN_THE_CLOUDS" => "Head in the Clouds",
                "GELATIN_WORLD_TOUR" => "Gelatin World Tour",
                "DEFEAT_DREADNAUTILUS" => "Don't Dread on Me",
                "PRISMANCER" => "Prismancer",
                "GET_A_LIFE" => "Get a Life",
                "TOPPED_OFF" => "Topped Off",
                "BUCKETS_OF_BOLTS" => "Buckets of Bolts",
                "MECHA_MAYHEM" => "Mecha Mayhem",
                "DRAX_ATTAX" => "Drax Attax",
                "PHOTOSYNTHESIS" => "Photosynthesis",
                "FUNKYTOWN" => "Funkytown",
                "IT_CAN_TALK" => "It Can Talk?!",
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
                "YOU_AND_WHAT_ARMY" => "You and What Army?",
                _ => null,
            };

            if (name != null) ModContent.GetInstance<ArchipelagoSystem>().QueueLocationClient(name);
        }
    }
}
