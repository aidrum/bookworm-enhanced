namespace Sexy
{
    /// <summary>
    /// Bookworm's hard-coded layout values (Sexy::ScreenPos, BookWormApp + 0xdd4): 255 words of positions,
    /// sizes and TRects for one of the five screens the original knew, filled by Init (FUN_00042cd4). The
    /// tables are generated into ScreenPos.Data.cs by tools/gen_screenpos.py. Words are addressed by their
    /// index (offset / 4) until their meaning is known; give them named accessors here as code is ported.
    /// </summary>
    public partial class ScreenPos
    {
        public int[] mData = new int[SIZE];

        /// <summary>The long side the iPhone Retina and iPhone 5 layouts were made for (same width, 640).</summary>
        private const int IPHONE_RETINA_LONG = 960;

        private const int IPHONE5_LONG = 1136;

        /// <summary>Port: true while the iPad design is laid out (its borders, pillars and scenery are drawn).</summary>
        public bool mIPadDesign;

        /// <summary>
        /// Port: where the iPad textures' full-width images (backgrounds, curtain swags, the menu tab bar, all
        /// 768 / 1536 wide with the iPad's borders) go in the iPhone design: centred on its narrower screen
        /// (-64 / -128), cutting the borders off. 0 otherwise.
        /// </summary>
        public int mWideImageX;

        /// <summary>
        /// Port (wide screens): how far the design's own width (768 / 1536) sits from the left of a layout wider
        /// than it (Constants.LayoutFor), half the extra width; 0 when the layout is no wider than the design.
        /// The screens' places in the middle of the design are moved by it in Init (CENTRED_X).
        /// </summary>
        public int mCenterX;

        /// <summary>Port (wide screens): all of the extra width, for what keeps to the right edge (RIGHT_X).</summary>
        public int mRightX;

        /// <summary>Port: the width the design's words were made for (the layout's, less the extra width).</summary>
        public int mDesignWidth;

        /// <summary>
        /// FUN_00042cd4 picked one of five fixed layouts from the screen's short and long sides. Port: lays out
        /// any portrait screen, from the texture set (theTextureSide: 768 / 1536 iPad, 320 / 640 iPhone) and the
        /// layout's size (Constants.LayoutFor).
        /// - The design: the texture set's own (iPad, or iPhone for the iPhone sets), or, for an iPad set laid
        ///   out narrower (640 / 1280), the iPhone design. The iPad sets have the iPhone Retina images at the same
        ///   size, so that is the iPhone Retina layout plus the iPad set's own adjustments: iPad (Retina) word +
        ///   scale x (iPhone Retina word - iPad word), the scale being 1 (iPad) or 2 (iPad Retina).
        /// - The height: the iPhone Retina (960) and iPhone 5 (1136) layouts share their textures and only
        ///   differ in the words that follow the screen's height, so each word moves by (iPhone 5 - iPhone
        ///   Retina) / 176 pixels per pixel of extra height: exact between and at those two, extrapolated beyond
        ///   them and applied to the other designs and scales too (the slopes are unitless). Words equal in both
        ///   stay as they are.
        /// </summary>
        public void Init(int theTextureSide, int theWidth, int theHeight)
        {
            int[] aLayout;
            int aScale = 1;
            bool anIPadTextures = true;
            if (theTextureSide == 0x600)
            {
                aLayout = LAYOUT_IPAD_RETINA;
                aScale = 2;
            }
            else if (theTextureSide == 0x300)
            {
                aLayout = LAYOUT_IPAD;
            }
            else if (theTextureSide == 0x280)
            {
                aLayout = LAYOUT_IPHONE_RETINA;
                anIPadTextures = false;
            }
            else
            {
                aLayout = LAYOUT_IPHONE;
                anIPadTextures = false;
            }
            mIPadDesign = anIPadTextures && theWidth >= 0x300 * aScale;
            int aLayoutHeight;
            int[] aBase = new int[SIZE];
            if (mIPadDesign)
            {
                System.Array.Copy(aLayout, aBase, SIZE);
                aLayoutHeight = 1024 * aScale;
                mWideImageX = 0;
            }
            else if (anIPadTextures)
            {
                for (int i = 0; i < SIZE; i++)
                {
                    aBase[i] = i >= FLOAT_WORD ? aLayout[i] : aLayout[i] + aScale * (LAYOUT_IPHONE_RETINA[i] - LAYOUT_IPAD[i]);
                }
                aLayoutHeight = IPHONE_RETINA_LONG * aScale;
                mWideImageX = -(0x300 - 0x280) / 2 * aScale;
            }
            else
            {
                System.Array.Copy(aLayout, aBase, SIZE);
                aLayoutHeight = theTextureSide == 0x280 ? IPHONE_RETINA_LONG : 480;
                mWideImageX = 0;
            }
            int anExtra = theHeight - aLayoutHeight;
            // The extra height at 16:9, past which BOARD_ANCHORED words stop following their slope.
            int aWideExtra = (int)System.Math.Round(theWidth * 16.0 / 9.0) - aLayoutHeight;
            for (int i = 0; i < SIZE; i++)
            {
                int aShort = LAYOUT_IPHONE_RETINA[i];
                int aLong = LAYOUT_IPHONE5[i];
                double aSlope = (aLong - aShort) / (double)(IPHONE5_LONG - IPHONE_RETINA_LONG);
                if (mIPadDesign && (i == 0x120 / 4 || i == 0x130 / 4))
                {
                    // iPad-only landscape scenery around the centred shelf (0x100 moves by half the extra width).
                    aSlope = 0.5;
                }
                if (i < FLOAT_WORD && anExtra > aWideExtra && System.Array.IndexOf(BOARD_ANCHORED, i * 4) >= 0)
                {
                    // Taller than 16:9: these keep their 16:9 place relative to the board and Lex (which sit
                    // at the bottom, moving 1:1 with the height); only the space above grows.
                    mData[i] = aBase[i] + (int)System.Math.Round(aSlope * aWideExtra) + (anExtra - aWideExtra);
                }
                else if (aSlope == 0.0 || anExtra == 0 || i >= FLOAT_WORD)
                {
                    mData[i] = aBase[i];
                }
                else
                {
                    mData[i] = aBase[i] + (int)System.Math.Round(aSlope * anExtra);
                }
            }
            if (!mIPadDesign)
            {
                // The iPhone 5's right landscape filler (IMAGE_GROUP13_01) sits as far from the right edge as
                // the centred shelf (0x100) is from the left; the 960 layout had no room for it (0).
                mData[0x130 / 4] = theHeight - mData[0x100 / 4];
            }
            // Port (wide screens): the design stays in the middle, the game selector's books go to the right edge
            // and Lex at the menu stays at the left (his menu and off-screen places and his speech are not moved).
            mDesignWidth = mIPadDesign && !Constants.Landscape ? 0x300 * aScale : theWidth;
            mRightX = System.Math.Max(0, theWidth - mDesignWidth);
            mCenterX = mRightX / 2;
            foreach (int anOffset in CENTRED_X)
            {
                mData[anOffset / 4] += mCenterX;
            }
            foreach (int anOffset in RIGHT_X)
            {
                mData[anOffset / 4] += mRightX;
            }
        }

        /// <summary>
        /// Port: x words (byte offsets) of screen places that keep to the middle of a wide layout: the logo (0x00),
        /// the menu tabs (0x40 ... 0x60, in the tab bar, which spans the screen), Lex on the board and entering it
        /// (0x90, 0x98), the books, dialogs and help pages (0x1e0, 0x1f0, 0x200, 0x220, 0x2e0), the board (0x2d0)
        /// and the board's balloons (0x230, 0x240, 0x260, 0x270), plus the bonus word banner's slide (0x190, 0x198,
        /// in the board's full-screen overlay). The full-width images (swags, pedestal: 0x78) are centred by
        /// SlideInImage; places inside the board or a screen are relative to it and stay.
        /// </summary>
        private static readonly int[] CENTRED_X =
        {
            0x00, 0x40, 0x48, 0x50, 0x58, 0x60, 0x90, 0x98, 0x190, 0x198,
            0x1e0, 0x1f0, 0x200, 0x220, 0x2d0, 0x2e0, 0x230, 0x240, 0x260, 0x270
        };

        /// <summary>Port: x words that keep to the right edge of a wide layout: the game selector (0x08).</summary>
        private static readonly int[] RIGHT_X = { 0x08 };

        /// <summary>
        /// Port (wide screens): a full-width image (backgrounds, the menu tab bar) at theX, its first and last
        /// columns stretched out to the screen's edges (0 and theScreenWidth) where it does not reach them.
        /// </summary>
        public static void DrawStretchedEdges(Graphics g, Image theImage, int theX, int theY, int theScreenWidth)
        {
            int aWidth = theImage.mWidth;
            int aHeight = theImage.mHeight;
            g.DrawImage(theImage, theX, theY);
            if (theX > 0)
            {
                g.DrawImage(theImage, new TRect(0, theY, theX, aHeight), new TRect(0, 0, 1, aHeight));
            }
            int aRight = theX + aWidth;
            if (aRight < theScreenWidth)
            {
                g.DrawImage(theImage, new TRect(aRight, theY, theScreenWidth - aRight, aHeight), new TRect(aWidth - 1, 0, 1, aHeight));
            }
        }

        /// <summary>
        /// Port: words (byte offsets) placed relative to Lex or the board on the iPhone 5 but moving slower than
        /// the screen's height between the 960 and 1136 layouts: Lex on the board (0x94) and entering it (0x9c),
        /// the bonus word banner's stop (0x19c), and the speech balloons' tops (0x234, 0x244, 0x264; 0x254 already
        /// moves 1:1). Lex's menu place (0x84) and the board (0x2d4) move 1:1 with the height, so past 16:9 these
        /// do too, and they stay where they were relative to Lex and the board instead of floating. The More Games
        /// balloon's top (0x274) is fixed in both layouts (slope 0) but sits over Lex at the menu, so past 16:9 it
        /// follows him the same way instead of staying pinned to the top of the screen.
        /// </summary>
        private static readonly int[] BOARD_ANCHORED = { 0x94, 0x9c, 0x19c, 0x234, 0x244, 0x264, 0x274 };

        /// <summary>The first of the two float words (0xfd, 0xfe).</summary>
        private const int FLOAT_WORD = 0xfd;

        /// <summary>The word at a byte offset into the ScreenPos.</summary>
        public int At(int theOffset)
        {
            return mData[theOffset / 4];
        }

        /// <summary>The float stored at a byte offset.</summary>
        public float FloatAt(int theOffset)
        {
            return System.BitConverter.Int32BitsToSingle(mData[theOffset / 4]);
        }

        /// <summary>The four words at a byte offset as a TRect (x, y, width, height).</summary>
        public TRect RectAt(int theOffset)
        {
            int i = theOffset / 4;
            return new TRect(mData[i], mData[i + 1], mData[i + 2], mData[i + 3]);
        }
    }
}
