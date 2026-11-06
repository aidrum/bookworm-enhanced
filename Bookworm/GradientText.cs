using Microsoft.Xna.Framework;

namespace Sexy
{
    /// <summary>
    /// Port addition: text drawn like the titles baked into the iOS art (the mode buttons, the banners), for the
    /// places where the port draws a translated title over text-free art: a soft dark shadow down and to the
    /// right, then the text with a vertical gradient over the whole block (the font's outline layer stays dark).
    /// </summary>
    public static class GradientText
    {
        /// <summary>The shadow's samples: each a faint black copy, overlapping into a soft blur.</summary>
        private const int SHADOW_ALPHA = 40;

        /// <summary>
        /// Draws the lines centred on theCenterX, the first on theFirstBaseline and each next a line spacing
        /// lower. The gradient runs from the first line's ascent to the last line's baseline.
        /// </summary>
        public static void Draw(Graphics g, Font theFont, string[] theLines, int theCenterX, int theFirstBaseline, Color theTop, Color theBottom, Color? theOutline = null)
        {
            int aLineHeight = theFont.GetLineSpacing();
            int anAscent = theFont.GetAscent();
            // Below the baseline: the descenders (g, j, p, q, y) and their outline's glow.
            int aDescent = System.Math.Max(0, theFont.GetHeight() - anAscent) + anAscent / 4;
            Font aTextFont = theOutline is Color anOutline ? GetOutlinedFont(theFont, anOutline) : theFont;
            // One step scales with the font (19, 38 or 76pt by resolution): the shadow's spread and the bands' height.
            int aUnit = System.Math.Max(1, anAscent / 14);
            g.SetFont(theFont);
            // The shadow: a 3x3 grid of faint black copies, shifted down and right.
            g.SetColor(new Color(0, 0, 0, SHADOW_ALPHA));
            for (int i = 0; i < theLines.Length; i++)
            {
                int aX = theCenterX - theFont.StringWidth(theLines[i]) / 2;
                int aY = theFirstBaseline + i * aLineHeight;
                for (int dy = 1; dy <= 3; dy++)
                {
                    for (int dx = 0; dx <= 2; dx++)
                    {
                        g.DrawString(theLines[i], aX + dx * aUnit, aY + dy * aUnit);
                    }
                }
            }
            // The text, in horizontal bands each clipped and coloured by its place in the gradient.
            int aTop = theFirstBaseline - anAscent;
            int aBottom = theFirstBaseline + (theLines.Length - 1) * aLineHeight;
            TRect anOldClip = g.GetClipRect();
            for (int i = 0; i < theLines.Length; i++)
            {
                int aX = theCenterX - theFont.StringWidth(theLines[i]) / 2;
                int aY = theFirstBaseline + i * aLineHeight;
                for (int aBand = aY - anAscent * 3 / 2; aBand < aY + aDescent; aBand += aUnit)
                {
                    float t = System.Math.Clamp((aBand + aUnit * 0.5f - aTop) / (float)System.Math.Max(1, aBottom - aTop), 0f, 1f);
                    g.ClipRect(-0x4000, aBand, 0x8000, aUnit);
                    // The font directly: Graphics.DrawString skips a string whose baseline is above the clip's
                    // top, which would drop every band below the baseline (the descenders).
                    aTextFont.DrawString(g, aX, aY, theLines[i], Color.Lerp(theTop, theBottom, t), g.GetClipRect());
                    g.mClipRect = anOldClip;
                }
            }
        }

        /// <summary>Copies of outlined fonts with their outline (layer 0, black in its pixels) in another colour.</summary>
        private static readonly System.Collections.Generic.Dictionary<(Font, Color), Font> sOutlinedFonts =
            new System.Collections.Generic.Dictionary<(Font, Color), Font>();

        /// <summary>theFont with its outline layer (the first: the Cooper outlined fonts' black glow) in theOutline.</summary>
        private static Font GetOutlinedFont(Font theFont, Color theOutline)
        {
            if (!(theFont is ImageFont anImageFont))
            {
                return theFont;
            }
            if (!sOutlinedFonts.TryGetValue((theFont, theOutline), out Font aFont))
            {
                ImageFont aCopy = (ImageFont)anImageFont.Duplicate();
                aCopy.SetLayerSolidColor(0, theOutline);
                aFont = aCopy;
                sOutlinedFonts[(theFont, theOutline)] = aFont;
            }
            return aFont;
        }

        /// <summary>The banners' title outline, as in their art.</summary>
        public static readonly Color BANNER_OUTLINE = new Color(0x87, 0x52, 0x12, 0xff);

        /// <summary>The banners' title gold (sampled from the "More History" art), top to bottom.</summary>
        public static readonly Color BANNER_TOP = new Color(248, 234, 143, 255);

        public static readonly Color BANNER_BOTTOM = new Color(231, 175, 45, 255);

        /// <summary>
        /// Where the banners' title sits: its baseline this far down per 320 of the banner's width (the art's is at
        /// 128 in the 1280-wide iPad Retina "More History"; the frames are 44-45 rows per 320 at every scale).
        /// </summary>
        public const int BANNER_BASELINE = 32;

        /// <summary>
        /// A port image made at the three art scales (images/x.png at the iPhone's, xR.png at 2x, xiPadR.png at
        /// 4x), the one for theScale (1, 2 or 4).
        /// </summary>
        public static Image LoadScaled(SexyAppBase theApp, string thePath, int theScale)
        {
            string aSuffix = theScale >= 4 ? "iPadR" : theScale >= 2 ? "R" : "";
            return theApp.GetImage(thePath + aSuffix, false);
        }

        /// <summary>A banner's title: one line, centred on a banner drawn at (theX, theY), theWidth wide.</summary>
        public static void DrawBannerTitle(Graphics g, string theTitle, int theX, int theY, int theWidth)
        {
            int aBaseline = theY + theWidth * BANNER_BASELINE / 320;
            Draw(g, Res.FONT_COOPER_OUTLINED, new[] { theTitle }, theX + theWidth / 2, aBaseline, BANNER_TOP, BANNER_BOTTOM, BANNER_OUTLINE);
        }
    }
}
