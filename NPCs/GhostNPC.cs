using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.GameContent.Personalities;
using Terraria.GameContent.UI;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.Utilities;
using ReLogic.Content;
using System.Collections.Immutable;
using Newtonsoft.Json.Linq;

namespace SeldomArchipelago.NPCs
{
    [AutoloadHead]
    public class GhostNPC : ModNPC
    {
        public int GhostType
        {
            get => ghostType;
            set
            {
                if (allGhostTypes.Contains(value))
                {
                    ghostType = value;
                }
                else
                {
                    throw new Exception($"Attempted to set ghostType to value {value}.");
                }
            }
        }
        int ghostType;
        static readonly ImmutableHashSet<int> allGhostTypes =
        [
            NPCID.Guide,
            NPCID.Merchant,
            NPCID.Nurse,
            NPCID.Demolitionist,
            NPCID.DyeTrader,
            NPCID.BestiaryGirl,
            NPCID.Dryad,
            NPCID.Painter,
            NPCID.ArmsDealer,
            NPCID.WitchDoctor,
            NPCID.Clothier,
            NPCID.PartyGirl,
            NPCID.Truffle,
            NPCID.Pirate,
            NPCID.Steampunker,
            NPCID.Cyborg,
            NPCID.SantaClaus,
            NPCID.Princess
        ];
        public static bool GhostableType(int type) => allGhostTypes.Contains(type);
        static Dictionary<int, Asset<Texture2D>> typeToTexture = new();
        Asset<Texture2D> GetTexture() => typeToTexture[ghostType];
        public override void SetStaticDefaults()
        {
            typeToTexture[0] = ModContent.Request<Texture2D>($"Terraria/Images/NPC_{NPCID.GolferRescue}");
            foreach (var npcType in allGhostTypes)
            {
                typeToTexture[npcType] = ModContent.Request<Texture2D>($"Terraria/Images/NPC_{npcType}");
            }
            NPC.townNPC = true;
        }
        public override void SetDefaults()
        {
            NPC.CloneDefaults(NPCID.Guide);
            NPC.aiStyle = 0;
        }
        public override void ModifyTypeName(ref string typeName) => typeName = $"{Lang.GetNPCName(ghostType)} Check";
        public override Color? GetAlpha(Color drawColor)
        {
            return drawColor;
        }
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            var texture = GetTexture();
            drawColor = NPC.GetNPCColorTintedByBuffs(drawColor);
            drawColor.A = 50;
            float num35 = 0f;
            float num36 = Main.NPCAddHeight(NPC);
            Vector2 halfSize = new Vector2(texture.Width() / 2, texture.Height() / Main.npcFrameCount[ghostType] / 2);
            SpriteEffects spriteEffects = SpriteEffects.None;
            Rectangle frame6 = texture.Frame(1, 25, 0, 0);
            float x = NPC.position.X - screenPos.X + (float)(NPC.width / 2) - (float)texture.Width() * NPC.scale / 2f + halfSize.X * NPC.scale;
            float y = NPC.position.Y - screenPos.Y + (float)NPC.height - (float)texture.Height() * NPC.scale / (float)Main.npcFrameCount[ghostType] + 4f + halfSize.Y * NPC.scale + num36 + num35 + NPC.gfxOffY;
            spriteBatch.Draw(texture.Value, new Vector2(x, y), frame6, NPC.GetAlpha(drawColor), NPC.rotation, halfSize, NPC.scale, spriteEffects, 0f);

            return false;
        }
        public override void FindFrame(int frameHeight)
        {
            NPC.frame = GetTexture().Frame(1, 25, 0, 0);
        }
        public override bool CanChat() => true;
        public override bool NeedSaving() => true;
        public static bool AnyGhosts(int type) => Main.npc.Any(npc => npc.active && npc.ModNPC is GhostNPC checkNPC && checkNPC.ghostType == type);
        public override void SaveData(TagCompound tag)
        {
            tag[nameof(ghostType)] = ghostType;
        }
        public override void LoadData(TagCompound tag)
        {
            ghostType = tag.GetAsInt(nameof(ghostType));
        }
    }
}
