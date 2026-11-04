using Microsoft.Xna.Framework;

namespace Sexy
{
    /// <summary>
    /// Sexy::Points (0x21dc8): a line of text over the board ("+210", "TIME BONUS!") that the board drifts up a
    /// pixel every other frame and fades over its last 10 frames (mLife, 0x7c). Placed centred on (or, align 1,
    /// ending at) theX, kept between the grid's left edge and its mirror on the right, its baseline on theY.
    /// </summary>
    public class Points : Widget
    {
        public BookWormApp mApp; // 0x78

        /// <summary>Frames left (0x7c).</summary>
        public int mLife;

        public string mText; // 0x80

        public Font mFont; // 0x84

        public int mHue; // 0x88

        public Points(BookWormApp theApp, Font theFont, string theText, int theX, int theY, int theLife, int theAlign)
        {
            mApp = theApp;
            mFont = theFont;
            mText = theText;
            mLife = theLife;
            mMouseVisible = false;
            mHasTransparencies = true;
            int aWidth = theFont.StringWidth(theText) + 5;
            if (theAlign == 1)
            {
                theX -= aWidth;
            }
            else if (theAlign == 0)
            {
                theX -= aWidth / 2;
            }
            ScreenPos aPos = theApp.mScreenPos;
            int aLeft = aPos.At(0x178);
            int aRight = aPos.At(0x18c) * 7 + aLeft * 2;
            int aX = theX + aWidth > aRight ? aRight - aWidth : System.Math.Max(theX, aLeft);
            Resize(aX, theY - theFont.GetAscent(), aWidth, theFont.GetHeight());
        }

        /// <summary>vf27 (0x220fc).</summary>
        public override void Draw(Graphics g)
        {
            base.Draw(g);
            g.SetColor(new Color(255, 255, 255, System.Math.Min(mLife * 255 / 10, 255)));
            g.SetFont(mFont);
            g.DrawString(mText, 0, mFont.GetAscent());
        }
    }
}
