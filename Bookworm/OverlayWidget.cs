using Microsoft.Xna.Framework;

namespace Sexy
{
    /// <summary>
    /// Sexy::OverlayWidget (0x20ff0, board + 0x1bc): a child of the board covering the whole screen (placed at
    /// minus the board's position), over the tiles: screen-space effects (the word flying into Lex) and the
    /// white flash when the library catches fire (board + 0x290); the bonus word banner and the fire spreading from
    /// the blazing column at game over. (0x9c, a double set to 10 with the banner and decayed by 0.025, is only
    /// saved: dead.)
    /// </summary>
    public class OverlayWidget : Widget
    {
        public BookWormApp mApp; // 0x78

        public Board mBoard; // 0x7c

        public EffectMgr mEffects; // 0x80

        public OverlayWidget(BookWormApp theApp, Board theBoard)
        {
            mApp = theApp;
            mBoard = theBoard;
            mMouseVisible = false;
            mHasTransparencies = true;
            mEffects = new EffectMgr(theApp);
        }

        /// <summary>FUN_00021324.</summary>
        public void AddEffect(Effect theEffect)
        {
            mEffects.Add(theEffect);
        }

        /// <summary>Frames into the banner's slide (0x84, 0..BANNER_FRAMES), shown (0x88), its place (0x8c, 0x90).</summary>
        public int mBannerFrame;

        public bool mBannerShown;

        public int mBannerX;

        public int mBannerY;

        /// <summary>DAT_00135a40.</summary>
        public const int BANNER_FRAMES = 0x19;

        /// <summary>
        /// 0x21420: the effects; the bonus word banner slides in (ScreenPos 0x190 to 0x198, eased out over 25
        /// frames) once the board announces a bonus word, and back out once it is made.
        /// </summary>
        public override void Update()
        {
            if (mBoard == null || mBoard.IsPaused())
            {
                return;
            }
            base.Update();
            if (mEffects.Update())
            {
                MarkDirty();
            }
            ScreenPos aPos = mApp.mScreenPos;
            bool aWord = mBoard.mBonusWord.Length != 0;
            if (!mBannerShown)
            {
                if (aWord)
                {
                    mBannerFrame = 0;
                    mBannerX = aPos.At(0x190);
                    mBannerY = aPos.At(0x194);
                    mBannerShown = true;
                }
                return;
            }
            if (!aWord)
            {
                if (mBannerFrame < 1)
                {
                    mBannerShown = false;
                }
                else
                {
                    mBannerFrame--;
                }
            }
            else if (mBannerFrame < BANNER_FRAMES)
            {
                mBannerFrame++;
            }
            // FUN_0009cd34 on the curve (1, 0, 0, 0): a quadratic ease out.
            float t = (float)mBannerFrame / BANNER_FRAMES;
            float anEase = t * (2f - t);
            mBannerX = (int)(aPos.At(0x190) + anEase * (aPos.At(0x198) - aPos.At(0x190)));
            mBannerY = (int)(aPos.At(0x194) + anEase * (aPos.At(0x19c) - aPos.At(0x194)));
        }

        /// <summary>0x21590: effects, the bonus word banner, the spreading fire, the white flash.</summary>
        public override void Draw(Graphics g)
        {
            mEffects.Draw(g);
            if (mBoard == null)
            {
                return;
            }
            if (mBannerShown && !mBoard.mGreyed)
            {
                DrawBanner(g);
            }
            if (mBoard.mBurningPiece != null)
            {
                DrawBurnSpread(g, mBoard.mBurningPiece);
            }
            if (mBoard.mFlash > 0)
            {
                int aLevel = (int)(mBoard.mFlash * 255.0);
                g.SetDrawMode(1);
                g.SetColor(new Color(aLevel, aLevel, aLevel));
                g.FillRect(0, 0, mWidth, mHeight);
                g.SetDrawMode(0);
            }
        }

        /// <summary>
        /// The banner (IMAGE_GROUP4_06) with the bonus word in yellow, centred on ScreenPos 0x1a0 (y 0x1a4), in a
        /// smaller font when wider than ScreenPos 0x3c8; the letters of it the word being spelled starts with
        /// (FUN_000034f8) over it in white.
        /// </summary>
        private void DrawBanner(Graphics g)
        {
            g.DrawImage(AtlasResourceBase.IMAGE_GROUP4_06, mBannerX, mBannerY);
            if (mBoard.mBonusWord.Length == 0)
            {
                return;
            }
            ScreenPos aPos = mApp.mScreenPos;
            string aText = mBoard.mBonusWord.ToUpperInvariant();
            Font aFont = Res.FONT_ARIAL_MEDIUM_OUTLINED;
            int aWidth = aFont.StringWidth(aText);
            int aMaxWidth = aPos.At(0x3c8);
            if (aWidth > aMaxWidth)
            {
                aFont = Res.FONT_DEFINITIONS_LARGE;
                aWidth = aFont.StringWidth(aText);
                if (aWidth > aMaxWidth)
                {
                    aFont = Res.FONT_DEFINITIONS_MEDIUM;
                    aWidth = aFont.StringWidth(aText);
                }
            }
            int aX = mBannerX + aPos.At(0x1a0) - aWidth / 2;
            int aY = aPos.At(0x1a4) + mBannerY;
            g.SetFont(aFont);
            g.SetColor(new Color(0xff, 0xff, 0, 0xff));
            g.DrawString(aText, aX, aY);
            string aSpelled = mBoard.mWordScore.mWord;
            int aMatch = 0;
            while (aMatch < aText.Length && aMatch < aSpelled.Length && char.ToUpperInvariant(aSpelled[aMatch]) == aText[aMatch])
            {
                aMatch++;
            }
            if (aMatch > 0)
            {
                g.SetColor(new Color(0xff, 0xff, 0xff, 0xff));
                g.DrawString(aText.Substring(0, aMatch), aX, aY);
            }
        }

        /// <summary>
        /// The fire spreading from the blazing column: IMAGE_GROUP51_01 tiled twice across (scrolled by board +
        /// 0x2b4) inside a band centred on the column, as wide as twice the screen times board + 0x2ac. The band's
        /// edges taper: 8-pixel strips 24, 16 and 8 in from each side get flames 1/4, 1/2 and 3/4 high; the middle
        /// full height, its foot at the tile's bottom + (height - 50).
        /// </summary>
        private void DrawBurnSpread(Graphics g, Piece thePiece)
        {
            Board aBoard = mBoard;
            Image aFire = AtlasResourceBase.IMAGE_GROUP51_01;
            int aBandWidth = (int)(aBoard.mBurnSpread * mWidth * 2.0);
            int aLeft = aBoard.GetColX(thePiece.mCol) + aBoard.mX + aBoard.mTileWidth / 2 - aBandWidth / 2;
            int aBottom = aBoard.mY + aBoard.GetRowY(thePiece.mCol, thePiece.mRow) + aBoard.mTileHeight;
            int aScroll = aBoard.mBurnScroll;
            TRect aSrc = new TRect(0, 0, aFire.mWidth, aFire.mHeight);
            for (int i = 1; i < 4; i++)
            {
                int anInset = (3 - i) * 8;
                int aHeight = aFire.mHeight * (4 - i) / 4;
                int aY = aBottom - aHeight * 0x32 / aFire.mHeight;
                for (int aSide = 0; aSide < 2; aSide++)
                {
                    int aStripX = aSide == 0 ? anInset : aBandWidth - 8 - anInset;
                    g.PushState();
                    g.ClipRect(aLeft, 0, aBandWidth, mHeight);
                    g.ClipRect(aLeft + aStripX, 0, 8, mHeight);
                    g.DrawImage(aFire, new TRect(-aScroll, aY, aFire.mWidth, aHeight), aSrc);
                    g.DrawImage(aFire, new TRect(aFire.mWidth - aScroll, aY, aFire.mWidth, aHeight), aSrc);
                    g.PopState();
                }
            }
            g.PushState();
            g.ClipRect(aLeft + 0x18, 0, aBandWidth - 0x30, mHeight);
            int aFullY = aBottom - 0x32;
            g.DrawImage(aFire, -aScroll, aFullY);
            g.DrawImage(aFire, aFire.mWidth - aScroll, aFullY);
            g.PopState();
        }
    }
}
