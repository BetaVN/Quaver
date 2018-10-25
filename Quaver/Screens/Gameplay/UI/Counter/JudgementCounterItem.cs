using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Quaver.API.Enums;
using Quaver.API.Helpers;
using Quaver.Assets;
using Quaver.Config;
using Quaver.Database.Maps;
using Quaver.Skinning;
using Wobble;
using Wobble.Graphics;
using Wobble.Graphics.Sprites;

namespace Quaver.Screens.Gameplay.UI.Counter
{
    public class JudgementCounterItem : Sprite
    {
        /// <summary>
        ///     The parent judgement display that controls the rest of them.
        /// </summary>
        private JudgementCounter ParentDisplay { get; }

        /// <summary>
        ///     The actual judgement this represents.
        /// </summary>
        public Judgement Judgement { get; }

        /// <summary>
        ///     The current judgement count for this
        /// </summary>
        private int _judgementCount;
        public int JudgementCount
        {
            get => _judgementCount;
            set
            {
                // Ignore if the judgement count is the same as the incoming value.
                if (_judgementCount == value)
                    return;

                _judgementCount = value;

                // Change the color to its active one.
                Tint = SkinManager.Skin.Keys[MapManager.Selected.Value.Mode].JudgeColors[Judgement];

                // Don't animate it if the user doesn't want to.
                if (!ConfigManager.AnimateJudgementCounter.Value)
                    return;

                // Make the size of the display look more pressed.
                Width = JudgementCounter.DisplayItemSize.Y - JudgementCounter.DisplayItemSize.Y / 4;
                Height = Width;
                X = -JudgementCounter.DisplayItemSize.Y / 16;
            }
        }

        /// <summary>
        ///     The sprite text for this given judgement.
        /// </summary>
        public SpriteText SpriteText { get; }

        /// <summary>
        ///     The inactive color for this.
        /// </summary>
        private Color InactiveColor { get; }

        /// <inheritdoc />
        /// <summary>
        ///     Ctor -
        /// </summary>
        /// <param name="parentDisplay"></param>
        /// <param name="j"></param>
        /// <param name="color"></param>
        /// <param name="size"></param>
        public JudgementCounterItem(JudgementCounter parentDisplay, Judgement j, Color color, Vector2 size)
        {
            Judgement = j;
            ParentDisplay = parentDisplay;

            Size = new ScalableVector2(size.X, size.Y);

            SpriteText = new SpriteText(BitmapFonts.Exo2Regular, JudgementHelper.JudgementToShortName(j), 18)
            {
                Alignment = Alignment.MidCenter,
                Parent = this,
                Tint = Color.Black,
                X = 0,
            };

            InactiveColor = color;
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <param name="gameTime"></param>
        public override void Update(GameTime gameTime)
        {
            // Make sure the color is always tweening down back to its inactive one.
            FadeToColor(InactiveColor, gameTime.ElapsedGameTime.TotalMilliseconds, 360);

            base.Update(gameTime);
        }
    }
}
