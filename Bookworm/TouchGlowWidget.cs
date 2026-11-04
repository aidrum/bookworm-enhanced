using Microsoft.Xna.Framework;

namespace Sexy
{
    /// <summary>
    /// Sexy::TouchGlowWidget (0x34e88, BookWormApp + 0xdb4): the additive glow box shown around a touched
    /// button. Created on first use (BookWormApp.ShowTouchGlow, 0x12040) at z-order 0x6e.
    /// </summary>
    public class TouchGlowWidget : Widget
    {
        /// <summary>The glow's colour when fully shown (0x136830); it fades from black (0x136840).</summary>
        private static readonly Color GLOW_COLOR = new Color(200, 200, 100, 255);

        private static readonly Color GLOW_OFF_COLOR = new Color(0, 0, 0, 255);

        public BookWormApp mApp; // 0x78

        public Image mImage; // 0x7c

        /// <summary>How far the glow reaches past the button (ScreenPos + 0x350: left, top, right, bottom).</summary>
        public TRect mInsets; // 0x80

        /// <summary>1 while shown, 0 once let go (0x90).</summary>
        public float mTarget;

        public float mFade; // 0x94

        public Color mColor; // 0x98

        public TouchGlowWidget(BookWormApp theApp)
        {
            mApp = theApp;
            mImage = AtlasResourceBase.IMAGE_GROUP31_06;
            mTarget = 0f;
            mFade = 0f;
            mColor = GLOW_OFF_COLOR;
            mInsets = theApp.mScreenPos.RectAt(0x350);
            SetVisible(false);
            mMouseVisible = false;
        }

        /// <summary>
        /// FUN_00035078: frames theRect, at least two thirds of the image wide and high. (When too small the
        /// original centres it on (x + width) / 2 and (y + height) / 2, using the width's third for both.)
        /// </summary>
        public void SetGlowRect(TRect theRect)
        {
            int aThirdW = mImage.mWidth / 3;
            int aWidth = mInsets.mWidth + mInsets.mX + theRect.mWidth;
            int aX;
            if (aWidth < aThirdW * 2)
            {
                aWidth = aThirdW * 2;
                aX = (theRect.mWidth + theRect.mX) / 2 - aThirdW;
            }
            else
            {
                aX = theRect.mX - mInsets.mX;
            }
            int aThirdH = mImage.mHeight / 3;
            int aHeight = mInsets.mY + theRect.mHeight + mInsets.mHeight;
            int aY;
            if (aHeight < aThirdH * 2)
            {
                aHeight = aThirdH * 2;
                aY = (theRect.mHeight + theRect.mY) / 2 - aThirdW;
            }
            else
            {
                aY = theRect.mY - mInsets.mY;
            }
            Resize(aX, aY, aWidth, aHeight);
        }

        /// <summary>FUN_00035100.</summary>
        public void Appear()
        {
            mTarget = 1f;
            SetVisible(true);
        }

        /// <summary>FUN_00035118: fades out.</summary>
        public void Disappear()
        {
            mTarget = 0f;
        }

        /// <summary>0x35120: fades in by a third a frame, out by a seventh.</summary>
        public override void Update()
        {
            if (mTarget == 0f)
            {
                if (mFade <= 0f)
                {
                    return;
                }
                mFade -= 1f / 7f;
                if (mFade <= 0f)
                {
                    mFade = 0f;
                    SetVisible(false);
                }
            }
            else
            {
                if (mFade >= 1f)
                {
                    return;
                }
                mFade = System.Math.Min(1f, mFade + 1f / 3f);
            }
            mColor = new Color(
                (int)(GLOW_OFF_COLOR.R + mFade * (GLOW_COLOR.R - GLOW_OFF_COLOR.R)),
                (int)(GLOW_OFF_COLOR.G + mFade * (GLOW_COLOR.G - GLOW_OFF_COLOR.G)),
                (int)(GLOW_OFF_COLOR.B + mFade * (GLOW_COLOR.B - GLOW_OFF_COLOR.B)),
                (int)(GLOW_OFF_COLOR.A + mFade * (GLOW_COLOR.A - GLOW_OFF_COLOR.A)));
        }

        /// <summary>0x35334: additive, colourised 9-slice box.</summary>
        public override void Draw(Graphics g)
        {
            base.Draw(g);
            g.SetDrawMode(1);
            g.SetColorizeImages(true);
            g.SetColor(mColor);
            g.DrawImageBox(new TRect(0, 0, mWidth, mHeight), mImage);
        }
    }
}
