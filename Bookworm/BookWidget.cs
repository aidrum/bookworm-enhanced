namespace Sexy
{
    /// <summary>
    /// Sexy::BookWidget (0x3a558): base of the menus drawn on an open book (Options, Stats, the word lists).
    /// The book image is stretched to the widget's height by repeating its middle third.
    /// </summary>
    public class BookWidget : SlideInWidget
    {
        /// <summary>The page colours (table 0x10cbec).</summary>
        private static readonly int[,] BOOK_COLORS = { { 20, 20, 20 }, { 100, 200, 100 }, { 0, 255, 0 }, { 254, 179, 0 } };

        /// <summary>The page area (ScreenPos + 0x2f0) (0xd8).</summary>
        public TRect mPageRect;

        public Image mBookImage; // 0xe8

        public Font mHeadingFont; // 0xec

        public Font mTextFont; // 0xf0

        public Font mScoreFont; // 0xf4

        public BookWidget(BookWormApp theApp) : base(theApp)
        {
            mBookImage = AtlasResourceBase.IMAGE_GROUP3_00;
            mHeadingFont = Res.FONT_ARIAL_MEDIUM_OUTLINED;
            mTextFont = Res.FONT_ARIAL_LARGE;
            mScoreFont = Res.FONT_GOLDSCORE;
            SetColors(BOOK_COLORS, 4);
            mPageRect = theApp.mScreenPos.RectAt(0x2f0);
        }

        /// <summary>0x3a778: the book in theRect, its middle third tiled to fill the height.</summary>
        public static void DrawBook(Graphics g, Image theImage, TRect theRect)
        {
            int aWidth = theImage.mWidth;
            int aThird = theImage.mHeight / 3;
            int aX = (theRect.mWidth - aWidth) / 2;
            int aMiddle = theRect.mHeight - aThird * 2;
            g.DrawImage(theImage, aX, 0, new TRect(0, 0, aWidth, aThird));
            g.PushState();
            g.ClipRect(aX, aThird, aWidth, aMiddle);
            for (int y = aThird; y < aMiddle + aThird; y += aThird)
            {
                g.DrawImage(theImage, aX, y, new TRect(0, aThird, aWidth, aThird));
            }
            g.PopState();
            g.DrawImage(theImage, aX, aMiddle + aThird, new TRect(0, aThird * 2, aWidth, aThird));
        }

        /// <summary>0x3a838.</summary>
        public override void Draw(Graphics g)
        {
            base.Draw(g);
            DrawBook(g, mBookImage, new TRect(0, 0, mWidth, mHeight));
        }

        /// <summary>FUN_0003a864: as wide as the book, centred in theRect, from 10 below its top.</summary>
        public void PlaceIn(TRect theRect)
        {
            Resize((theRect.mWidth - mBookImage.mWidth) / 2, theRect.mY + 10, mBookImage.mWidth, theRect.mHeight - 10);
        }
    }
}
