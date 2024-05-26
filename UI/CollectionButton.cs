using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SeldomArchipelago.NPCs;
using SeldomArchipelago.Players;
using SeldomArchipelago.Systems;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace SeldomArchipelago.UI
{
    class CollectionButton : UIElement
    {
        static float left = 29f;
        static float top = 300f;

        static bool Clickable => Main.playerInventory && !Main.CreativeMenu.Blocked;

        bool hovered;

        public CollectionButton()
        {
            Left = StyleDimension.FromPixels(left);
            Top = StyleDimension.FromPixels(top);
            Width.Set(32f, 0f);
            Height.Set(32f, 0f);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            if (Clickable)
            {
                spriteBatch.Draw((Texture2D)ModContent.Request<Texture2D>(hovered ? "SeldomArchipelago/UI/CollectionButtonHover" : "SeldomArchipelago/UI/CollectionButton"), new Vector2(left, top), new Color(255, 255, 255));
                if (hovered)
                {
                    Main.LocalPlayer.mouseInterface = true;
                    Main.hoverItemName = "Open Archipelago Collection";
                }
            }
        }

        public override void MouseOver(UIMouseEvent evt)
        {
            base.MouseOver(evt);
            hovered = true;
        }

        public override void MouseOut(UIMouseEvent evt)
        {
            base.MouseOut(evt);
            hovered = false;
        }

        public override void LeftMouseDown(UIMouseEvent evt)
        {
            base.LeftClick(evt);
            if (Clickable)
            {
                var player = Main.LocalPlayer;
                SoundEngine.PlaySound(SoundID.Chat);
                typeof(Main).GetMethod("OpenShop", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(Main.instance, new object[] { 1 });

                if (Main.netMode == NetmodeID.MultiplayerClient)
                {
                    var packet = ModContent.GetInstance<SeldomArchipelago>().GetPacket();
                    packet.Write("RecievedRewardsForSetupShop");
                    packet.Write(player.chest);
                    packet.Send();
                }
                else
                {
                    var position = player.position;
                    SetupShop(ModContent.GetInstance<ArchipelagoSystem>().ReceivedRewards(), NPC.NewNPC(new EntitySource_Misc("Open collection"), (int)position.X, (int)position.Y, ModContent.NPCType<CollectionNPC>(), 0, Main.myPlayer, player.chest));
                }
            }
        }

        public static void SetupShop(List<int> items, int npc)
        {
            // Reverse the items so the newer ones show up earlier in the shop
            var itemsReversed = new List<int>(items);
            itemsReversed.Reverse();

            typeof(Main).GetMethod("OpenShop", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(Main.instance, new object[] { 1 });
            var player = Main.LocalPlayer;

            Main.CancelHairWindow();
            Main.InGuideCraftMenu = false;
            player.dropItemCheck();
            Main.npcChatCornerItem = 0;
            player.sign = -1;
            Main.editSign = false;
            player.SetTalkNPC(npc);
            Recipe.FindRecipes();
            Main.npcChatText = "";

            player.currentShoppingSettings.PriceAdjustment = 1.0;

            var shop = Main.instance.shop[1];
            var apPlayer = player.GetModPlayer<ArchipelagoPlayer>();

            for (var slot = 0; slot < Chest.maxItems; slot++)
            {
                shop.item[slot] = new Item();
                Item item = shop.item[slot];

                if (slot < itemsReversed.Count)
                {
                    var type = itemsReversed[slot];
                    item.SetDefaults(type);
                    item.isAShopItem = true;

                    if (!apPlayer.HasReceivedReward(type))
                    {
                        item.buyOnce = true;
                        item.shopCustomPrice = 0;
                    }
                }
                else item.SetDefaults();
            }
        }
    }
}