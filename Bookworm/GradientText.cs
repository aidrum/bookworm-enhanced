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
        /// spacing is before scaling. theMaxWidth above 0 shrinks it further so the widest line fits it.
        /// </summary>
        public static void Draw(Graphics g, Font theFont, string[] theLines, int theCenterX, int theFirstBaseline, Color theTop, Color theBottom, Color? theOutline = null, int theLineSpacing = 0, float theScale = 1f, int theMaxWidth = 0)
        {
            theScale = System.Math.Min(theScale, FitScale(theFont, theLines, theMaxWidth));
            // Inside another offscreen render the cache cannot be built (switching targets would lose that one).
            if (Graphics.IsOffscreen)
            {
                DrawDirect(g, theFont, theLines, theCenterX, theFirstBaseline, theTop, theBottom, theOutline, theLineSpacing, theScale);
                return;
            }
            int aLineHeight = theLineSpacing > 0 ? theLineSpacing : theFont.GetLineSpacing();
            CacheKey aKey = new CacheKey(theFont, string.Join("\n", theLines), theTop, theBottom, theOutline, aLineHeight);
            if (!sCache.TryGetValue(aKey, out CacheEntry anEntry))
            {
                anEntry = Build(theFont, theLines, theTop, theBottom, theOutline, aLineHeight);
                sCache[aKey] = anEntry;
            }
            MemoryImage anImage = anEntry.mImage;
            // The image's corner, scaled about (theCenterX, theFirstBaseline) as the direct drawing is.
            int aLeft = theCenterX + (int)System.Math.Round(anEntry.mLeft * theScale);
            int aTop = theFirstBaseline + (int)System.Math.Round(anEntry.mTop * theScale);
            int aRight = theCenterX + (int)System.Math.Round((anEntry.mLeft + anImage.mWidth) * theScale);
            int aBottom = theFirstBaseline + (int)System.Math.Round((anEntry.mTop + anImage.mHeight) * theScale);
            // The colours are baked in; the direct drawing ignored the caller's colour too.
            bool aColorize = g.GetColorizeImages();
            g.SetColorizeImages(false);
            g.DrawImage(anImage, new TRect(aLeft, aTop, aRight - aLeft, aBottom - aTop), new TRect(0, 0, anImage.mWidth, anImage.mHeight));
            g.SetColorizeImages(aColorize);
        }

        /// <summary>The scale (at most 1) at which the widest of theLines fits theMaxWidth; 1 when theMaxWidth is 0.</summary>
        public static float FitScale(Font theFont, string[] theLines, int theMaxWidth)
        {
            if (theMaxWidth <= 0)
            {
                return 1f;
            }
            int aWidest = 1;
            foreach (string aLine in theLines)
            {
                aWidest = System.Math.Max(aWidest, theFont.StringWidth(aLine));
            }
            return System.Math.Min(1f, theMaxWidth / (float)aWidest);
        }

        /// <summary>
        /// Port (performance): each title drawn once into an image, then blitted. Drawn directly it takes some 35
        /// string draws per line (the shadow's 9 copies, then a band every few pixels), every frame.
        /// </summary>
        private readonly struct CacheKey : System.IEquatable<CacheKey>
        {
            private readonly Font mFont;
            private readonly string mText;
            private readonly Color mTop;
            private readonly Color mBottom;
            private readonly Color? mOutline;
            private readonly int mLineHeight;

            public CacheKey(Font theFont, string theText, Color theTop, Color theBottom, Color? theOutline, int theLineHeight)
            {
                mFont = theFont;
                mText = theText;
                mTop = theTop;
                mBottom = theBottom;
                mOutline = theOutline;
                mLineHeight = theLineHeight;
            }

            public bool Equals(CacheKey theOther)
            {
                return ReferenceEquals(mFont, theOther.mFont) && mText == theOther.mText && mTop == theOther.mTop && mBottom == theOther.mBottom
                    && mOutline == theOther.mOutline && mLineHeight == theOther.mLineHeight;
            }

            public override bool Equals(object theObject)
            {
                return theObject is CacheKey aKey && Equals(aKey);
            }

            public override int GetHashCode()
            {
                return System.HashCode.Combine(System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(mFont), mText, mTop, mBottom, mOutline, mLineHeight);
            }
        }

        /// <summary>A cached title: its image, and the image's corner against (centre x, first baseline).</summary>
        private sealed class CacheEntry
        {
            public MemoryImage mImage;
            public int mLeft;
            public int mTop;
        }

        /// <summary>More than this many titles (a language switch, many screens visited): all are dropped.</summary>
        private const int CACHE_LIMIT = 48;

        private static readonly System.Collections.Generic.Dictionary<CacheKey, CacheEntry> sCache =
            new System.Collections.Generic.Dictionary<CacheKey, CacheEntry>();

        private static bool sResetHooked;

        private static void ClearCache()
        {
            foreach (CacheEntry anEntry in sCache.Values)
            {
                anEntry.mImage.Dispose();
            }
            sCache.Clear();
        }

        /// <summary>Draws the title unscaled into a new image, sized to hold its shadow and its outline's glow.</summary>
        private static CacheEntry Build(Font theFont, string[] theLines, Color theTop, Color theBottom, Color? theOutline, int theLineHeight)
        {
            if (!sResetHooked)
            {
                // The render targets' contents are lost with the device (an Android app sent to the background).
                Microsoft.Xna.Framework.Graphics.GraphicsDevice aDevice = GlobalStaticVars.g.GraphicsDevice;
                aDevice.DeviceResetting += (theSender, theArgs) => ClearCache();
                aDevice.DeviceReset += (theSender, theArgs) => ClearCache();
                sResetHooked = true;
            }
            if (sCache.Count >= CACHE_LIMIT)
            {
                ClearCache();
            }
            int anAscent = theFont.GetAscent();
            int aDescent = System.Math.Max(0, theFont.GetHeight() - anAscent) + anAscent / 4;
            int aUnit = System.Math.Max(1, anAscent / 14);
            int aPad = anAscent / 2;
            int aLeft = 0;
            int aRight = 0;
            foreach (string aLine in theLines)
            {
                int aWidth = theFont.StringWidth(aLine);
                aLeft = System.Math.Min(aLeft, -(aWidth / 2));
                aRight = System.Math.Max(aRight, aWidth - aWidth / 2);
            }
            aLeft -= aPad;
            aRight += 2 * aUnit + aPad;
            // The bands start 1.5 ascents above each baseline; the shadow reaches 3 units below the last band.
            int aTop = -anAscent * 3 / 2;
            int aBottom = (theLines.Length - 1) * theLineHeight + aDescent + 4 * aUnit + aPad;
            CacheEntry anEntry = new CacheEntry { mImage = new MemoryImage(), mLeft = aLeft, mTop = aTop };
            anEntry.mImage.Create(aRight - aLeft, aBottom - aTop);
            Graphics aG = Graphics.GetNew(anEntry.mImage);
            aG.Clear(Color.Transparent);
            DrawDirect(aG, theFont, theLines, -aLeft, -aTop, theTop, theBottom, theOutline, theLineHeight, 1f);
            aG.SetRenderTarget(null);
            aG.PrepareForReuse();
            return anEntry;
        }

        /// <summary>Draw, every string drawn each time (the cache's contents, and drawing inside an offscreen render).</summary>
        private static void DrawDirect(Graphics g, Font theFont, string[] theLines, int theCenterX, int theFirstBaseline, Color theTop, Color theBottom, Color? theOutline, int theLineSpacing, float theScale)
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

        /// <summary>How much of a banner's width its title may take (inside the frame's scrolled ends).</summary>
        private const float BANNER_TITLE_SHARE = 0.72f;

        /// <summary>
        /// A banner's title: one line, centred on a banner drawn at (theX, theY), theWidth wide. Port: a longer
        /// title shrinks to fit the banner's plaque, about its baseline's middle so it stays centred.
        /// </summary>
        public static void DrawBannerTitle(Graphics g, string theTitle, int theX, int theY, int theWidth)
        {
            Font aFont = Res.FONT_COOPER_OUTLINED;
            string[] aLines = { theTitle };
            float aScale = FitScale(aFont, aLines, (int)(theWidth * BANNER_TITLE_SHARE));
            // Scaled about the baseline, the caps would sit low on the plaque: raise the baseline by half the shrink.
            int aBaseline = theY + theWidth * BANNER_BASELINE / 320
                - (int)System.Math.Round(aFont.GetAscent() * CAP_RATIO * (1f - aScale) / 2f);
            Draw(g, aFont, aLines, theX + theWidth / 2, aBaseline, BANNER_TOP, BANNER_BOTTOM, BANNER_OUTLINE, 0, aScale);
        }
    }
}
