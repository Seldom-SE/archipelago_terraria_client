using System;
using System.Collections.Generic;
using System.Reflection;
using CalamityMod;
using CalamityMod.NPCs;
using CalamityMod.Tiles.Ores;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SeldomArchipelago.Systems
{
    // Direct usage of Calamity must happen in here, else the mod won't compile without Calamity
    [ExtendsFromMod("CalamityMod")]
    public class CalamitySystem : ModSystem
    {
        public bool DownedAquaticScourge() => CalamityMod.DownedBossSystem.downedAquaticScourge;

        public void GiveCosmolight() => ModContent.GetInstance<ArchipelagoSystem>().GiveItem<CalamityMod.Items.Tools.ClimateChange.Cosmolight>();

        public void GiveCorruptFlask() => ModContent.GetInstance<ArchipelagoSystem>().GiveItem<CalamityMod.Items.Accessories.CorruptFlask>();
        public void GiveCrimsonFlask() => ModContent.GetInstance<ArchipelagoSystem>().GiveItem<CalamityMod.Items.Accessories.CrimsonFlask>();
        public void GiveCrawCarapace() => ModContent.GetInstance<ArchipelagoSystem>().GiveItem<CalamityMod.Items.Accessories.CrawCarapace>();
        public void GiveGiantShell() => ModContent.GetInstance<ArchipelagoSystem>().GiveItem<CalamityMod.Items.Accessories.GiantShell>();
        public void GiveLifeJelly() => ModContent.GetInstance<ArchipelagoSystem>().GiveItem<CalamityMod.Items.Accessories.LifeJelly>();
        public void GiveVitalJelly() => ModContent.GetInstance<ArchipelagoSystem>().GiveItem<CalamityMod.Items.Accessories.VitalJelly>();
        public void GiveCleansingJelly() => ModContent.GetInstance<ArchipelagoSystem>().GiveItem<CalamityMod.Items.Accessories.CleansingJelly>();
        public void GiveGiantTortoiseShell() => ModContent.GetInstance<ArchipelagoSystem>().GiveItem<CalamityMod.Items.Accessories.GiantTortoiseShell>();
        public void GiveCoinOfDeceit() => ModContent.GetInstance<ArchipelagoSystem>().GiveItem<CalamityMod.Items.Accessories.CoinofDeceit>();
        public void GiveInkBomb() => ModContent.GetInstance<ArchipelagoSystem>().GiveItem<CalamityMod.Items.Accessories.InkBomb>();
        public void GiveVoltaicJelly() => ModContent.GetInstance<ArchipelagoSystem>().GiveItem<CalamityMod.Items.Accessories.VoltaicJelly>();
        public void GiveWulfrumBattery() => ModContent.GetInstance<ArchipelagoSystem>().GiveItem<CalamityMod.Items.Accessories.WulfrumBattery>();
        public void GiveLuxorsGift() => ModContent.GetInstance<ArchipelagoSystem>().GiveItem<CalamityMod.Items.Accessories.LuxorsGift>();
        public void GiveRaidersTalisman() => ModContent.GetInstance<ArchipelagoSystem>().GiveItem<CalamityMod.Items.Accessories.RaidersTalisman>();
        public void GiveRottenDogtooth() => ModContent.GetInstance<ArchipelagoSystem>().GiveItem<CalamityMod.Items.Accessories.RottenDogtooth>();
        public void GiveScuttlersJewel() => ModContent.GetInstance<ArchipelagoSystem>().GiveItem<CalamityMod.Items.Accessories.ScuttlersJewel>();
        public void GiveUnstableGraniteCore() => ModContent.GetInstance<ArchipelagoSystem>().GiveItem<CalamityMod.Items.Accessories.UnstableGraniteCore>();
        public void GiveAmidiasSpark() => ModContent.GetInstance<ArchipelagoSystem>().GiveItem<CalamityMod.Items.Accessories.AmidiasSpark>();
        public void GiveUrsaSergeant() => ModContent.GetInstance<ArchipelagoSystem>().GiveItem<CalamityMod.Items.Fishing.AstralCatches.UrsaSergeant>();
        public void GiveTrinketOfChi() => ModContent.GetInstance<ArchipelagoSystem>().GiveItem<CalamityMod.Items.Accessories.TrinketofChi>();
        public void GiveTheTransformer() => ModContent.GetInstance<ArchipelagoSystem>().GiveItem<CalamityMod.Items.Accessories.TheTransformer>();
        public void GiveRoverDrive() => ModContent.GetInstance<ArchipelagoSystem>().GiveItem<CalamityMod.Items.Accessories.RoverDrive>();
        public void GiveMarniteRepulsionShield() => ModContent.GetInstance<ArchipelagoSystem>().GiveItem<CalamityMod.Items.Accessories.MarniteRepulsionShield>();
        public void GiveFrostBarrier() => ModContent.GetInstance<ArchipelagoSystem>().GiveItem<CalamityMod.Items.Accessories.FrostBarrier>();
        public void GiveAncientFossil() => ModContent.GetInstance<ArchipelagoSystem>().GiveItem<CalamityMod.Items.Accessories.AncientFossil>();
        public void GiveSpelunkersAmulet() => ModContent.GetInstance<ArchipelagoSystem>().GiveItem<CalamityMod.Items.Accessories.SpelunkersAmulet>();
        public void GiveFungalSymbiote() => ModContent.GetInstance<ArchipelagoSystem>().GiveItem<CalamityMod.Items.Accessories.FungalSymbiote>();
        public void GiveGladiatorsLocket() => ModContent.GetInstance<ArchipelagoSystem>().GiveItem<CalamityMod.Items.Accessories.GladiatorsLocket>();
        public void GiveWulfrumAcrobaticsPack() => ModContent.GetInstance<ArchipelagoSystem>().GiveItem<CalamityMod.Items.Accessories.WulfrumAcrobaticsPack>();
        public void GiveDepthsCharm() => ModContent.GetInstance<ArchipelagoSystem>().GiveItem<CalamityMod.Items.Accessories.DepthCharm>();
        public void GiveAnechoicPlating() => ModContent.GetInstance<ArchipelagoSystem>().GiveItem<CalamityMod.Items.Accessories.AnechoicPlating>();
        public void GiveIronBoots() => ModContent.GetInstance<ArchipelagoSystem>().GiveItem<CalamityMod.Items.Accessories.IronBoots>();
        public void GiveSpritGlyph() => ModContent.GetInstance<ArchipelagoSystem>().GiveItem<CalamityMod.Items.Accessories.SpiritGlyph>();
        public void GiveAbyssalAmulet() => ModContent.GetInstance<ArchipelagoSystem>().GiveItem<CalamityMod.Items.Accessories.AbyssalAmulet>();

        public void CalamityPostUpdateWorld()
        {
            if (CalamityMod.DownedBossSystem.downedBossRush) ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Boss Rush");
        }

        public bool CheckCalamityFlag(string flag) => flag switch
        {
            "Post-Desert Scourge" => CalamityMod.DownedBossSystem.downedDesertScourge,
            "Post-Giant Clam" => CalamityMod.DownedBossSystem.downedCLAM,
            "Post-Acid Rain Tier 1" => CalamityMod.DownedBossSystem.downedEoCAcidRain,
            "Post-Crabulon" => CalamityMod.DownedBossSystem.downedCrabulon,
            "Post-The Hive Mind" => CalamityMod.DownedBossSystem.downedHiveMind,
            "Post-The Perforators" => CalamityMod.DownedBossSystem.downedPerforator,
            "Post-The Slime God" => CalamityMod.DownedBossSystem.downedSlimeGod,
            "Post-Dreadnautilus" => CalamityMod.DownedBossSystem.downedDreadnautilus,
            "Post-Hardmode Giant Clam" => CalamityMod.DownedBossSystem.downedCLAMHardMode,
            "Post-Aquatic Scourge" => CalamityMod.DownedBossSystem.downedAquaticScourge,
            "Post-Cragmaw Mire" => CalamityMod.DownedBossSystem.downedCragmawMire,
            "Post-Acid Rain Tier 2" => CalamityMod.DownedBossSystem.downedAquaticScourgeAcidRain,
            "Post-Brimstone Elemental" => CalamityMod.DownedBossSystem.downedBrimstoneElemental,
            "Post-Cryogen" => CalamityMod.DownedBossSystem.downedCryogen,
            "Post-Calamitas Clone" => CalamityMod.DownedBossSystem.downedCalamitasClone,
            "Post-Great Sand Shark" => CalamityMod.DownedBossSystem.downedGSS,
            "Post-Leviathan and Anahita" => CalamityMod.DownedBossSystem.downedLeviathan,
            "Post-Astrum Aureus" => CalamityMod.DownedBossSystem.downedAstrumAureus,
            "Post-The Plaguebringer Goliath" => CalamityMod.DownedBossSystem.downedPlaguebringer,
            "Post-Ravager" => CalamityMod.DownedBossSystem.downedRavager,
            "Post-Astrum Deus" => CalamityMod.DownedBossSystem.downedAstrumDeus,
            "Post-Profaned Guardians" => CalamityMod.DownedBossSystem.downedGuardians,
            "Post-The Dragonfolly" => CalamityMod.DownedBossSystem.downedDragonfolly,
            "Post-Providence, the Profaned Goddess" => CalamityMod.DownedBossSystem.downedProvidence,
            "Post-Storm Weaver" => CalamityMod.DownedBossSystem.downedStormWeaver,
            "Post-Ceaseless Void" => CalamityMod.DownedBossSystem.downedCeaselessVoid,
            "Post-Signus, Envoy of the Devourer" => CalamityMod.DownedBossSystem.downedSignus,
            "Post-Polterghast" => CalamityMod.DownedBossSystem.downedPolterghast,
            "Post-Mauler" => CalamityMod.DownedBossSystem.downedMauler,
            "Post-Nuclear Terror" => CalamityMod.DownedBossSystem.downedNuclearTerror,
            "Post-The Old Duke" => CalamityMod.DownedBossSystem.downedBoomerDuke,
            "Post-The Devourer of Gods" => CalamityMod.DownedBossSystem.downedDoG,
            "Post-Yharon, Dragon of Rebirth" => CalamityMod.DownedBossSystem.downedYharon,
            "Post-Exo Mechs" => CalamityMod.DownedBossSystem.downedExoMechs,
            "Post-Supreme Witch, Calamitas" => CalamityMod.DownedBossSystem.downedCalamitas,
            "Post-Primordial Wyrm" => CalamityMod.DownedBossSystem.downedPrimordialWyrm,
            "Post-Boss Rush" => CalamityMod.DownedBossSystem.downedBossRush,
            _ => ((Func<bool>)(() =>
            {
                ModContent.GetInstance<ArchipelagoSystem>().Chat($"Unknown flag: {flag}");
                return false;
            }))(),
        };

        public void CalamityStartHardmode()
        {
            if (CalamityConfig.Instance.EarlyHardmodeProgressionRework && NPC.downedMechBoss1 && NPC.downedMechBoss2 && NPC.downedMechBoss3) SpawnMechOres();
        }

        public void VanillaBossKilled(int boss)
        {
            var npc = new NPC { type = boss };
            var calamityNpc = new CalamityGlobalNPC();
            typeof(NPC).GetField("_globals", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(npc, new GlobalNPC[] { calamityNpc });
            var seldomArchipelago = ModContent.GetInstance<SeldomArchipelago>();
            seldomArchipelago.temp = true;
            calamityNpc.OnKill(npc);
            seldomArchipelago.temp = false;
        }

        public void CalamityOnKill<T>(MethodInfo method) where T : ModNPC, new() => CalamityOnKill<T>(method, new float[] { 0, 0, 0, 0 });

        public void CalamityOnKill<T>(MethodInfo method, float[] newAi) where T : ModNPC, new()
        {
            var npc = new T();
            var entity = new NPC
            {
                type = ModContent.NPCType<T>(),
                target = 0
            };
            var calamityNpc = new CalamityGlobalNPC();
            calamityNpc.newAI = newAi;
            var globalNpcs = new List<GlobalNPC>();
            var index = ModContent.GetInstance<CalamityGlobalNPC>().PerEntityIndex;
            for (int i = 0; i < index; i++) globalNpcs.Add(null);
            globalNpcs.Add(calamityNpc);
            typeof(NPC).GetField("_globals", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(entity, globalNpcs.ToArray());
            typeof(ModType<NPC>).GetProperty("Entity").SetValue(npc, entity);
            var seldomArchipelago = ModContent.GetInstance<SeldomArchipelago>();
            seldomArchipelago.temp = true;
            method.Invoke(npc, new object[] { });
            seldomArchipelago.temp = false;
        }

        public void CalamityOnKillGiantClam(bool hardmode)
        {
            var downed = hardmode ? CalamityMod.DownedBossSystem.downedCLAM : CalamityMod.DownedBossSystem.downedCLAMHardMode;
            var isHardmode = Main.hardMode;
            Main.hardMode = hardmode;
            CalamityOnKill<CalamityMod.NPCs.SunkenSea.GiantClam>(SeldomArchipelago.giantClamOnKill);
            if (hardmode) CalamityMod.DownedBossSystem.downedCLAM = downed;
            else CalamityMod.DownedBossSystem.downedCLAMHardMode = downed;
            Main.hardMode = isHardmode;
        }

        public void CalamityOnKillLeviathanAndAnahita()
        {
            var seldomArchipelago = ModContent.GetInstance<SeldomArchipelago>();
            seldomArchipelago.temp = true;
            SeldomArchipelago.leviathanRealOnKill.Invoke(new CalamityMod.NPCs.Leviathan.Leviathan(), new object[] { null });
            seldomArchipelago.temp = false;
        }

        public void CalamityOnKillCryogen()
        {
            CalamityOnKill<CalamityMod.NPCs.Cryogen.Cryogen>(SeldomArchipelago.cryogenOnKill);
            if (Main.netMode == NetmodeID.SinglePlayer) CalamityUtils.SpawnOre(ModContent.TileType<CryonicOre>(), 0.00015, 0.45f, 0.7f, 3, 8, new int[] {
                147,
                161,
                163,
                200,
                164,
                0,
                0,
            });
        }
        public void CalamityOnKillExoMechs()
        {
            CalamityMod.DownedBossSystem.downedExoMechs = CalamityMod.DownedBossSystem.downedAres = CalamityMod.DownedBossSystem.downedArtemisAndApollo = CalamityMod.DownedBossSystem.downedThanatos = true;
        }

        public void CalamityAcidRainTier1Downed() => CalamityMod.DownedBossSystem.downedEoCAcidRain = true;
        public void CalamityDreadnautilusDowned() => CalamityMod.DownedBossSystem.downedDreadnautilus = true;
        public void CalamityAcidRainTier2Downed() => CalamityMod.DownedBossSystem.downedAquaticScourgeAcidRain = true;
        public void CalamityPrimordialWyrmDowned() => CalamityMod.DownedBossSystem.downedPrimordialWyrm = true;
        public void CalamityBossRushDowned() => CalamityMod.DownedBossSystem.downedBossRush = true;

        public void CalamityOnKillDesertScourge() => CalamityOnKill<CalamityMod.NPCs.DesertScourge.DesertScourgeHead>(SeldomArchipelago.desertScourgeHeadOnKill);
        public void CalamityOnKillCrabulon() => CalamityOnKill<CalamityMod.NPCs.Crabulon.Crabulon>(SeldomArchipelago.crabulonOnKill);
        public void CalamityOnKillTheHiveMind() => CalamityOnKill<CalamityMod.NPCs.HiveMind.HiveMind>(SeldomArchipelago.hiveMindOnKill);
        public void CalamityOnKillThePerforators() => CalamityOnKill<CalamityMod.NPCs.Perforator.PerforatorHive>(SeldomArchipelago.perforatorHiveOnKill);
        public void CalamityOnKillTheSlimeGod() => CalamityOnKill<CalamityMod.NPCs.SlimeGod.SlimeGodCore>(SeldomArchipelago.slimeGodCoreOnKill);
        public void CalamityOnKillAquaticScourge() => CalamityOnKill<CalamityMod.NPCs.AquaticScourge.AquaticScourgeHead>(SeldomArchipelago.aquaticScourgeHeadOnKill);
        public void CalamityOnKillCragmawMire() => CalamityOnKill<CalamityMod.NPCs.AcidRain.CragmawMire>(SeldomArchipelago.cragmawMireOnKill);
        public void CalamityOnKillBrimstoneElemental() => CalamityOnKill<CalamityMod.NPCs.BrimstoneElemental.BrimstoneElemental>(SeldomArchipelago.brimstoneElementalOnKill);
        public void CalamityOnKillCalamitasClone() => CalamityOnKill<CalamityMod.NPCs.CalClone.CalamitasClone>(SeldomArchipelago.calamitasCloneOnKill);
        public void CalamityOnKillGreatSandShark() => CalamityOnKill<CalamityMod.NPCs.GreatSandShark.GreatSandShark>(SeldomArchipelago.greatSandSharkOnKill);
        public void CalamityOnKillAstrumAureus() => CalamityOnKill<CalamityMod.NPCs.AstrumAureus.AstrumAureus>(SeldomArchipelago.astrumAureusOnKill);
        public void CalamityOnKillThePlaguebringerGoliath() => CalamityOnKill<CalamityMod.NPCs.PlaguebringerGoliath.PlaguebringerGoliath>(SeldomArchipelago.plaguebringerGoliathOnKill);
        public void CalamityOnKillRavager() => CalamityOnKill<CalamityMod.NPCs.Ravager.RavagerBody>(SeldomArchipelago.ravagerBodyOnKill);
        public void CalamityOnKillAstrumDeus() => CalamityOnKill<CalamityMod.NPCs.AstrumDeus.AstrumDeusHead>(SeldomArchipelago.astrumDeusHeadOnKill, new float[] { 3, 0, 0, 0 });
        public void CalamityOnKillProfanedGuardians() => CalamityOnKill<CalamityMod.NPCs.ProfanedGuardians.ProfanedGuardianCommander>(SeldomArchipelago.profanedGuardianCommanderOnKill);
        public void CalamityOnKillTheDragonfolly() => CalamityOnKill<CalamityMod.NPCs.Bumblebirb.Bumblefuck>(SeldomArchipelago.bumblefuckOnKill);
        public void CalamityOnKillProvidenceTheProfanedGoddess() => CalamityOnKill<CalamityMod.NPCs.Providence.Providence>(SeldomArchipelago.providenceOnKill);
        public void CalamityOnKillStormWeaver() => CalamityOnKill<CalamityMod.NPCs.StormWeaver.StormWeaverHead>(SeldomArchipelago.stormWeaverHeadOnKill);
        public void CalamityOnKillCeaselessVoid() => CalamityOnKill<CalamityMod.NPCs.CeaselessVoid.CeaselessVoid>(SeldomArchipelago.ceaselessVoidOnKill);
        public void CalamityOnKillSignusEnvoyOfTheDevourer() => CalamityOnKill<CalamityMod.NPCs.Signus.Signus>(SeldomArchipelago.signusOnKill);
        public void CalamityOnKillPolterghast() => CalamityOnKill<CalamityMod.NPCs.Polterghast.Polterghast>(SeldomArchipelago.polterghastOnKill);
        public void CalamityOnKillMauler() => CalamityOnKill<CalamityMod.NPCs.AcidRain.Mauler>(SeldomArchipelago.maulerOnKill);
        public void CalamityOnKillNuclearTerror() => CalamityOnKill<CalamityMod.NPCs.AcidRain.NuclearTerror>(SeldomArchipelago.nuclearTerrorOnKill);
        public void CalamityOnKillTheOldDuke() => CalamityOnKill<CalamityMod.NPCs.OldDuke.OldDuke>(SeldomArchipelago.oldDukeOnKill);
        public void CalamityOnKillTheDevourerOfGods() => CalamityOnKill<CalamityMod.NPCs.DevourerofGods.DevourerofGodsHead>(SeldomArchipelago.devourerofGodsHeadOnKill);
        public void CalamityOnKillYharonDragonOfRebirth() => CalamityOnKill<CalamityMod.NPCs.Yharon.Yharon>(SeldomArchipelago.yharonOnKill);
        public void CalamityOnKillSupremeWitchCalamitas() => CalamityOnKill<CalamityMod.NPCs.SupremeCalamitas.SupremeCalamitas>(SeldomArchipelago.supremeCalamitasOnKill);

        public void SpawnHardOres()
        {
            if (!CalamityMod.CalamityConfig.Instance.EarlyHardmodeProgressionRework) return;
            CalamityMod.CalamityUtils.SpawnOre(107, 0.00012, 0.45f, 0.7f, 3, 8, Array.Empty<int>());
            CalamityMod.CalamityUtils.SpawnOre(221, 0.00012, 0.45f, 0.7f, 3, 8, Array.Empty<int>());
        }

        public void SpawnMechOres()
        {
            if (!CalamityMod.CalamityConfig.Instance.EarlyHardmodeProgressionRework) return;
            typeof(CalamityMod.NPCs.CalamityGlobalNPC).GetMethod("SpawnMechBossHardmodeOres", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(new CalamityMod.NPCs.CalamityGlobalNPC(), null);
        }

        // I'm using an int because I'm a hater
        public bool AreExosDead(int thisExoIsDead)
        {
            return (thisExoIsDead == 0 || !(CalamityGlobalNPC.draedonExoMechPrime != -1 && Main.npc[CalamityGlobalNPC.draedonExoMechPrime].active)) && (thisExoIsDead == 1 || !(CalamityGlobalNPC.draedonExoMechTwinGreen != -1 && Main.npc[CalamityGlobalNPC.draedonExoMechTwinGreen].active)) && (thisExoIsDead == 2 || !(CalamityGlobalNPC.draedonExoMechWorm != -1 && Main.npc[CalamityGlobalNPC.draedonExoMechWorm].active));
        }
    }
}
