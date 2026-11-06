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

        /// <summary>
        /// Port: taller than 16:9, how far the Stats / Options / More Games page (0x1f0, whose top keeps its slope
        /// below 1:1) sits above where it would be had it kept its 16:9 place under Lex at the board spot (which
        /// moves 1:1 past 16:9). On those pages Lex and his balloon are raised by it (BookWormApp.GetLexLift), or
        /// he slid behind the page. 0 up to 16:9.
        /// </summary>
        public int mPageLift;

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
            mPageLift = 0;
            if (anExtra > aWideExtra)
            {
                int aPageTop = 0x1f4 / 4;
                double aPageSlope = (LAYOUT_IPHONE5[aPageTop] - LAYOUT_IPHONE_RETINA[aPageTop]) / (double)(IPHONE5_LONG - IPHONE_RETINA_LONG);
                mPageLift = (int)System.Math.Round(aPageSlope * aWideExtra) + (anExtra - aWideExtra) - (int)System.Math.Round(aPageSlope * anExtra);
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
            // Port: the iPad design's pillars (board, bookshelf) only while the screen is no wider than the design;
            // wider, the curtain beside them would show their seam, so the board is the iPhone's (640 wide).
            mPillars = mIPadDesign && mRightX == 0;
            if (mIPadDesign && !mPillars)
            {
                NarrowBoard(aScale);
            }
            mWideGame = mIPadDesign && !Constants.Landscape && (long)theWidth * 3 >= (long)theHeight * 4;
            if (mWideGame)
            {
                LayOutWideGame(aScale, theWidth, theHeight);
            }
        }

        /// <summary>
        /// Port (landscape screens, 4:3 and wider, iPad design): the board screen is laid out side by side in a
        /// 4:3 block centred on the screen. The board goes to the block's right; the panel left of it gets the
        /// board is centred vertically, without the books strip under its bottom shelf; the panel left of it has Lex
        /// standing on the bottom bar (menu button, level bar, score, level) with his word balloons to his right
        /// and that strip behind the bar.
        /// The menu pages keep the portrait places (Lex's wide place is only taken with the board: mWideLex).
        /// </summary>
        public bool mWideGame;

        /// <summary>Port: true while the iPad design's side pillars are drawn (no extra width; see Init).</summary>
        public bool mPillars;

        /// <summary>
        /// Port: the iPad design with the iPhone's board, the iPad shelf image cut to its middle 640 (no pillars):
        /// the board's own words take the iPhone Retina values (scaled as for the iPhone design), the board and
        /// the pages over it are centred where the iPad's was.
        /// </summary>
        private void NarrowBoard(int theScale)
        {
            foreach (int anOffset in NARROW_BOARD_WORDS)
            {
                int i = anOffset / 4;
                mData[i] += theScale * (LAYOUT_IPHONE_RETINA[i] - LAYOUT_IPAD[i]);
            }
            foreach (int anOffset in new[] { 0x2d0, 0x220, 0x2e0 })
            {
                int i = anOffset / 4;
                int aWidth = mData[i + 2];
                mData[i + 2] = 0x280 * theScale;
                mData[i] += (aWidth - mData[i + 2]) / 2;
            }
        }

        /// <summary>Port: board-relative words the iPhone board has its own values for (menu button, level bar and its
        /// frame, score, level, grid origin).</summary>
        private static readonly int[] NARROW_BOARD_WORDS = { 0x148, 0x150, 0x158, 0x160, 0x168, 0x178, 0x17c };

        /// <summary>Port (mWideGame): how much of the board's bottom (the books strip under the bottom shelf) is cut
        /// off the board, where in the shelf image that strip starts, and where it goes behind the bar (board-relative
        /// y; x is the bar frame's).</summary>
        public int mWideBoardCut;

        public int mWideStripY;

        public int mWideStripDestY;

        /// <summary>Port (mWideGame): the bottom bar's width (the screen's): its frame's ends stay as they are and
        /// its middle stretches; the books behind it and the level bar's books are tiled.</summary>
        public int mWideBarWidth;

        /// <summary>Port (mWideGame): the board is shown this much bigger (Board.BoardScale), its widget rect (0x2d0)
        /// the shown size; its own width and height (the iPhone board less the strip).</summary>
        public float mWideBoardScale = 1f;

        public int mWideBoardWidth;

        public int mWideBoardHeight;

        /// <summary>Port: how much bigger the board shows in the wide game layout. More would not leave the panel
        /// room for the bar (640 wide) in a 4:3 block.</summary>
        public const float WIDE_BOARD_SCALE = 1.25f;

        /// <summary>Port (mWideGame): Lex's place beside the board, and where he fades in from (below it).</summary>
        public TPoint mWideLex;

        public TPoint mWideLexEntry;

        private void LayOutWideGame(int theScale, int theWidth, int theHeight)
        {
            int aBlockWidth = (int)System.Math.Round(theHeight * 4.0 / 3.0);
            int aBlockX = (theWidth - aBlockWidth) / 2;
            // The board's own size (the iPhone board without the books strip under its bottom shelf, which goes
            // behind the bar) and its shown size.
            mWideBoardCut = 0x64 * theScale;
            mWideBoardScale = WIDE_BOARD_SCALE;
            mWideBoardWidth = mData[0x2d8 / 4];
            mWideBoardHeight = mData[0x2dc / 4] - mWideBoardCut;
            int aBoardWidth = (int)System.Math.Round(mWideBoardWidth * mWideBoardScale);
            int aBoardHeight = (int)System.Math.Round(mWideBoardHeight * mWideBoardScale);
            int aBoardX = aBlockX + aBlockWidth - aBoardWidth;
            // The bar's screen place (it keeps its height at the bottom of the screen) and Lex's and his balloons'
            // places relative to each other, before the board moves.
            int aBoardY = mData[0x2d4 / 4];
            int aBalloonDX = mData[0x230 / 4] - mData[0x90 / 4];
            int aBalloonDY = mData[0x234 / 4] - mData[0x94 / 4];
            int aBigBalloonDX = mData[0x260 / 4] - mData[0x90 / 4];
            int aBigBalloonDY = mData[0x264 / 4] - mData[0x94 / 4];
            // The board, centred vertically at the block's right.
            int aNewBoardY = ((theHeight - aBoardHeight) / 2) - 55;
            mData[0x2d0 / 4] = aBoardX;
            mData[0x2d4 / 4] = aNewBoardY;
            mData[0x2d8 / 4] = aBoardWidth;
            mData[0x2dc / 4] = aBoardHeight;
            // The bottom bar (board-relative: menu button, level bar and its frame, score, level) stays at the
            // bottom of the screen and spans its whole width (mWideBarWidth): the menu button and the level bar keep
            // to its left end, the score to its middle, the level to its right end (Board.DrawWideBarFrame).
            int aFrameX = mData[0x158 / 4];
            int aBarDY = aBoardY - aNewBoardY;
            mWideBarWidth = theWidth;
            int aNewFrameX = -aBoardX;
            int aGrowth = mWideBarWidth - mWideBoardWidth;
            mData[0x148 / 4] = aNewFrameX + mData[0x148 / 4] - aFrameX;
            mData[0x150 / 4] = aNewFrameX + mData[0x150 / 4] - aFrameX;
            mData[0x160 / 4] = aNewFrameX + mData[0x160 / 4] - aFrameX + aGrowth / 2;
            mData[0x168 / 4] = aNewFrameX + mData[0x168 / 4] - aFrameX + aGrowth;
            mData[0x158 / 4] = aNewFrameX;
            foreach (int anOffset in WIDE_BAR_X)
            {
                mData[anOffset / 4 + 1] += aBarDY;
            }
            // The strip: from the shelf image's bottom shelf down, behind the bar where it was behind it on the board.
            mWideStripY = mWideBoardHeight;
            mWideStripDestY = mWideStripY + aBarDY;
            // The level-up page and the in-game word collection page go over the board.
            mData[0x220 / 4] = aBoardX + (aBoardWidth - mData[0x228 / 4]) / 2;
            mData[0x2e0 / 4] = aBoardX + (aBoardWidth - mData[0x2e8 / 4]) / 2;
            // Lex at the panel's left, standing behind the bar's frame as he stands behind the board's top, with his
            // balloons to his right as in portrait.
            int aFrameTop = aNewBoardY + mData[0x15c / 4];
            mWideLex = new TPoint(aBlockX + 0x30 * theScale, aFrameTop - 0xe4 * theScale);
            mWideLexEntry = new TPoint(mWideLex.mX + mData[0x98 / 4] - mData[0x90 / 4], mWideLex.mY + mData[0x9c / 4] - mData[0x94 / 4]);
            // The bonus word banner hangs at the top of the panel, straight above Lex (its width, 0xa0, is Lex's 0x80
            // plus 0x20).
            int aBannerX = mWideLex.mX - 0x10 * theScale;
            mData[0x190 / 4] = aBannerX;
            mData[0x198 / 4] = aBannerX;
            mData[0x230 / 4] = mWideLex.mX + aBalloonDX;
            mData[0x234 / 4] = mWideLex.mY + aBalloonDY;
            mData[0x260 / 4] = mWideLex.mX + aBigBalloonDX;
            mData[0x264 / 4] = mWideLex.mY + aBigBalloonDY;
        }

        /// <summary>Port (mWideGame): the bottom bar's board-relative x words (0x148 menu button, 0x150 level bar,
        /// 0x158 its frame, 0x160 score, 0x168 level).</summary>
        private static readonly int[] WIDE_BAR_X = { 0x148, 0x150, 0x158, 0x160, 0x168 };

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
