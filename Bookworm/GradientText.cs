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
        /// lower. The gradient runs from the first line's ascent to the last line's baseline. theScale (below 1:
        /// the fonts cannot be scaled themselves) shrinks it all about (theCenterX, theFirstBaseline); the line
        /// spacing is before scaling.
        /// </summary>
        public static void Draw(Graphics g, Font theFont, string[] theLines, int theCenterX, int theFirstBaseline, Color theTop, Color theBottom, Color? theOutline = null, int theLineSpacing = 0, float theScale = 1f)
        {
            bool aScaled = theScale != 1f;
            if (aScaled)
            {
                g.SetScale(theScale, theScale, theCenterX, theFirstBaseline);
            }
            int aLineHeight = theLineSpacing > 0 ? theLineSpacing : theFont.GetLineSpacing();
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
                    // The clip is not scaled with the drawing: the band's place once scaled.
                    int aBandTop = (int)System.Math.Round(theFirstBaseline + (aBand - theFirstBaseline) * theScale);
                    int aBandBottom = (int)System.Math.Round(theFirstBaseline + (aBand + aUnit - theFirstBaseline) * theScale);
                    g.ClipRect(-0x4000, aBandTop, 0x8000, aBandBottom - aBandTop);
                    // The font directly: Graphics.DrawString skips a string whose baseline is above the clip's
                    // top, which would drop every band below the baseline (the descenders).
                    aTextFont.DrawString(g, aX, aY, theLines[i], Color.Lerp(theTop, theBottom, t), g.GetClipRect());
                    g.mClipRect = anOldClip;
                }
            }
            if (aScaled)
            {
                g.SetScale(1f, 1f, 0f, 0f);
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

        /// <summary>A capital's height against the font's ascent (estimated: the fonts' metrics hold no cap height).</summary>
        private const float CAP_RATIO = 0.72f;

        /// <summary>
        /// Draws the lines centred on (theCenterX, theCenterY) by their capitals (the first line's cap top to the
        /// last's baseline), shrunk to fit theMaxWidth and theMaxHeight (never grown). theLinePitch is the baseline
        /// step before scaling (0: 1.25 capitals).
        /// </summary>
        public static void DrawFitted(Graphics g, Font theFont, string[] theLines, int theCenterX, int theCenterY, int theMaxWidth, int theMaxHeight, Color theTop, Color theBottom, Color? theOutline = null, int theLinePitch = 0)
        {
            float aCap = theFont.GetAscent() * CAP_RATIO;
            int aPitch = theLinePitch > 0 ? theLinePitch : (int)System.Math.Round(aCap * 1.25f);
            int aWidest = 1;
            foreach (string aLine in theLines)
            {
                aWidest = System.Math.Max(aWidest, theFont.StringWidth(aLine));
            }
            float aBlock = aCap + aPitch * (theLines.Length - 1);
            float aScale = System.Math.Min(1f, System.Math.Min(theMaxWidth / (float)aWidest, theMaxHeight / aBlock));
            int aFirstBaseline = theCenterY + (int)System.Math.Round(aScale * (aCap - aPitch * (theLines.Length - 1)) / 2f);
            Draw(g, theFont, theLines, theCenterX, aFirstBaseline, theTop, theBottom, theOutline, aPitch, aScale);
        }

        /// <summary>A banner's title: one line, centred on a banner drawn at (theX, theY), theWidth wide.</summary>
        public static void DrawBannerTitle(Graphics g, string theTitle, int theX, int theY, int theWidth)
        {
            int aBaseline = theY + theWidth * BANNER_BASELINE / 320;
            Draw(g, Res.FONT_COOPER_OUTLINED, new[] { theTitle }, theX + theWidth / 2, aBaseline, BANNER_TOP, BANNER_BOTTOM, BANNER_OUTLINE);
        }
    }
}
