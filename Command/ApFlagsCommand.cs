using Terraria;
using Terraria.GameContent.Events;
using Terraria.ModLoader;

namespace SeldomArchipelago.Command
{
    public class ApFlagsCommand : ModCommand
    {
        public override string Command => "apflags";
        public override CommandType Type => CommandType.World;
        public override string Description => "Lists the set boss/event flags";

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            if (args.Length > 0)
            {
                caller.Reply("Error: Unexpected arguments");
                return;
            }

            foreach (var flag in new (bool, string)[] {
                (NPC.downedSlimeKing, "Post-King Slime"),
                (NPC.downedBoss1, "Post-Eye of Cthulhu"),
                (NPC.downedBoss2, "Post-Evil Boss"),
                (DD2Event.DownedInvasionT1, "Post-Old One's Army Tier 1"),
                (NPC.downedGoblins, "Post-Goblin Army"),
                (NPC.downedQueenBee, "Post-Queen Bee"),
                (NPC.downedBoss3, "Post-Skeletron"),
                (NPC.downedDeerclops, "Post-Deerclops"),
                (Main.hardMode, "Hardmode"),
                (NPC.downedPirates, "Post-Pirate Invasion"),
                (NPC.downedQueenSlime, "Post-Queen Slime"),
                (NPC.downedMechBoss2, "Post-The Twins"),
                (DD2Event.DownedInvasionT2, "Post-Old One's Army Tier 2"),
                (NPC.downedMechBoss1, "Post-The Destroyer"),
                (NPC.downedMechBoss3, "Post-Skeletron Prime"),
                (NPC.downedPlantBoss, "Post-Plantera"),
                (NPC.downedGolemBoss, "Post-Golem"),
                (DD2Event.DownedInvasionT3, "Post-Old One's Army Tier 3"),
                (NPC.downedMartians, "Post-Martian Madness"),
                (NPC.downedFishron, "Post-Duke Fishron"),
                (NPC.downedHalloweenTree, "Post-Mourning Wood"),
                (NPC.downedHalloweenKing, "Post-Pumpking"),
                (NPC.downedChristmasTree, "Post-Everscream"),
                (NPC.downedChristmasSantank, "Post-Santa-NK1"),
                (NPC.downedChristmasIceQueen, "Post-Ice Queen"),
                (NPC.downedFrost, "Post-Frost Legion"),
                (NPC.downedEmpressOfLight, "Post-Empress of Light"),
                (NPC.downedAncientCultist, "Post-Lunatic Cultist"),
                (NPC.downedTowerNebula, "Post-Lunar Events"),
                (NPC.downedMoonlord, "Post-Moon Lord"),
            })
            {
                if (flag.Item1) caller.Reply(flag.Item2);
            }
        }
    }
}