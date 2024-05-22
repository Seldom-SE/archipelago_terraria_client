using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace SeldomArchipelago.UI
{
    [Autoload(Side = ModSide.Client)]
    class UISystem : ModSystem
    {
        ArchipelagoUI ui;
        UserInterface _ui;

        public override void Load()
        {
            ui = new ArchipelagoUI();
            ui.Activate();
            _ui = new UserInterface();
            _ui.SetState(ui);
        }

        public override void UpdateUI(GameTime gameTime)
        {
            _ui?.Update(gameTime);
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int mouseTextIndex = layers.FindIndex(layers => layers.Name.Equals("Vanilla: Mouse Text"));
            if (mouseTextIndex != -1)
            {
                layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer(
                    "SeldomArchipelago: Collection Button",
                    delegate
                    {
                        _ui.Draw(Main.spriteBatch, new GameTime());
                        return true;
                    },
                    InterfaceScaleType.UI
                ));
            }
        }
    }
}