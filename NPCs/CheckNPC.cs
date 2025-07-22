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

namespace SeldomArchipelago.NPCs
{
    [AutoloadHead]
    public class CheckNPC : ModNPC
    {
        static int funType = NPCID.BoundGoblin;
        static Asset<Texture2D> texture;
        public Point home;
        static int rotationIndex = 0;
        public override void SetStaticDefaults()
        {
            texture = ModContent.Request<Texture2D>($"Terraria/Images/NPC_{funType}");

            //NPCID.Sets.NoTownNPCHappiness[Type] = true;
            NPC.townNPC = true;
        }
        public override void SetDefaults()
        {
            NPC.CloneDefaults(funType);
        }
        public override Color? GetAlpha(Color drawColor)
        {
            return drawColor;
        }
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            drawColor = NPC.GetNPCColorTintedByBuffs(drawColor);
            drawColor.A = 5;
            float num35 = 0f;
            float num36 = Main.NPCAddHeight(NPC);
            Vector2 halfSize = new Vector2(texture.Width() / 2, texture.Height() / Main.npcFrameCount[funType] / 2);
            SpriteEffects spriteEffects = SpriteEffects.None;
            Microsoft.Xna.Framework.Rectangle frame6 = texture.Frame();
            float x = NPC.position.X - screenPos.X + (float)(NPC.width / 2) - (float)texture.Width() * NPC.scale / 2f + halfSize.X * NPC.scale;
            float y = NPC.position.Y - screenPos.Y + (float)NPC.height - (float)texture.Height() * NPC.scale / (float)Main.npcFrameCount[funType] + 4f + halfSize.Y * NPC.scale + num36 + num35 + NPC.gfxOffY;
            spriteBatch.Draw(texture.Value, new Vector2(x, y), frame6, NPC.GetAlpha(drawColor), NPC.rotation, halfSize, NPC.scale, spriteEffects, 0f);

            return false;
        }
        public override void FindFrame(int frameHeight)
        {
            NPC.frame = texture.Frame();
        }
        public override bool CanChat() => true;
        public override string GetChat() => "...";
        public override void SetChatButtons(ref string button, ref string button2) => button = "Redeem";
        public override void OnChatButtonClicked(bool firstButton, ref string shopName)
        {
            if (firstButton) NPC.StrikeInstantKill();
        }
        public override bool NeedSaving() => true;
    }
}
