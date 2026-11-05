using Microsoft.Xna.Framework;

namespace Sexy
{
    /// <summary>
    /// Sexy::WordBookWidget (0x35468): a book page of text (Stats, Level Up, the word lists): a FancyText,
    /// usually in a vertical scroller with shaded top and bottom edges, an optional close (X) button and an
    /// optional CONTINUE button. Helpers add the usual entries ("High Score (Classic)" and so on).
    /// </summary>
    public class WordBookWidget : BookWidget, ButtonListener, ProxyWidgetListener
    {
        /// <summary>The rank titles (0x10ca3c), by rank (capped at 31).</summary>
        public static readonly string[] RANK_NAMES =
        {
            "Book Burner", "Encyclopedia Salesman", "Trainee", "Clerk", "Scribe", "Bookkeeper", "Bookbinder",
            "Proofreader", "Assistant Librarian", "Librarian", "Senior Librarian", "Archivist", "Grand Archivist",
            "Editor", "Editor in Chief", "Publisher", "Publisher Emeritus", "Glossographer", "Wordstalker",
            "Wordhoarder", "Wordmaster", "Vocabulist", "Thesaurian", "Thesaurus Rex", "Lexographer",
            "Lexographer 1st Class", "Dictionarian", "Super Dictionarian", "Bookworm", "Mega Bookworm",
            "Ultra Bookworm", "Bookworm Supreme"
        };

        public const int BUTTON_CLOSE = 0;

        public const int BUTTON_CONTINUE = 1;

        protected static readonly Color BLACK = new Color(0, 0, 0, 255);

        private static readonly Color WHITE = new Color(255, 255, 255, 255);

        /// <summary>The rank title colour (0xc68700).</summary>
        private static readonly Color RANK_COLOR = new Color(0xc6, 0x87, 0x00, 0xff);

        /// <summary>The scroller's top and bottom edge shading (0x100, 0x104).</summary>
        public Image mTopEdge;

        public Image mBottomEdge;

        /// <summary>Space kept above the text (0x108).</summary>
        public int mTopOffset;

        /// <summary>A heading above the scroller, made by subclasses (0x110).</summary>
        public FancyTextWidget mTitle;

        public FancyTextWidget mText; // 0x114

        public ScrollWidget mScroll; // 0x118

        /// <summary>Draws the edges over the scroller (0x11c).</summary>
        public ProxyWidget mEdges;

        public BookWormButton mCloseButton; // 0x120

        public BookWormButton mContinueButton; // 0x124

        public WordBookWidget(BookWormApp theApp) : base(theApp)
        {
            mTopEdge = AtlasResourceBase.IMAGE_GROUP3_02;
            mBottomEdge = AtlasResourceBase.IMAGE_GROUP3_01;
        }

        /// <summary>FUN_00035794.</summary>
        protected void MakeTitle()
        {
            mTitle = new FancyTextWidget();
            AddWidget(mTitle);
        }

        /// <summary>FUN_00035850.</summary>
        public void SetTopOffset(int theOffset)
        {
            mTopOffset = theOffset;
        }

        /// <summary>FUN_00035858: the text in a vertical scroller (indicator insets ScreenPos + 0x3d0 at the sides).</summary>
        protected void MakeScroller()
        {
            mScroll = new ScrollWidget();
            mScroll.EnableIndicators(AtlasResourceBase.IMAGE_GROUP31_34);
            int aSide = mApp.mScreenPos.At(0x3d0);
            mScroll.SetIndicatorsInsets(new Insets(aSide, mTopEdge.mHeight, aSide, mBottomEdge.mHeight));
            AddWidget(mScroll);
            mText = new FancyTextWidget();
            mScroll.AddWidget(mText);
            mEdges = new ProxyWidget(this);
            mEdges.mMouseVisible = false;
            AddWidget(mEdges);
        }

        /// <summary>FUN_000359d8: the X button.</summary>
        protected void MakeCloseButton()
        {
            mCloseButton = new BookWormButton(BUTTON_CLOSE, this, mApp);
            mCloseButton.mButtonImage = AtlasResourceBase.IMAGE_GROUP31_22;
            AddWidget(mCloseButton);
        }

        /// <summary>FUN_00035ab4: the green CONTINUE button.</summary>
        protected void MakeContinueButton()
        {
            mContinueButton = new BookWormButton(BUTTON_CONTINUE, this, mApp);
            mContinueButton.SetStretchImage(true);
            mContinueButton.mButtonImage = AtlasResourceBase.IMAGE_GROUP11_03;
            mContinueButton.SetFont(Res.FONT_COOPER_OUTLINED);
            mContinueButton.SetColor(0, Dialogoid.GREEN_BUTTON_COLOR);
            mContinueButton.SetColor(1, Dialogoid.GREEN_BUTTON_COLOR);
            mContinueButton.mLabel = SexyAppBase.Loc("CONTINUE");
            AddWidget(mContinueButton);
        }

        /// <summary>
        /// FUN_00035c00: lays out the page inside the book's margins (mPageRect: left, top, right, bottom): the X
        /// at ScreenPos + 0xa0, CONTINUE along the bottom, the scroller in what is left.
        /// </summary>
        public void LayoutPage()
        {
            TRect aRect = new TRect(mPageRect.mX, mPageRect.mY, mWidth - mPageRect.mX - mPageRect.mWidth, mHeight - mPageRect.mY - mPageRect.mHeight);
            mTitle?.Resize(aRect.mX, aRect.mY, aRect.mWidth, mTopOffset);
            aRect.mY += mTopOffset;
            aRect.mHeight -= mTopOffset;
            if (mCloseButton != null && mCloseButton.mVisible)
            {
                mCloseButton.Resize(mApp.mScreenPos.At(0xa0), mApp.mScreenPos.At(0xa4), mCloseButton.mButtonImage.mWidth, mCloseButton.mButtonImage.mHeight);
            }
            if (mContinueButton != null && mContinueButton.mVisible)
            {
                int aHeight = mContinueButton.mButtonImage.mHeight;
                mContinueButton.Resize(mPageRect.mX, mHeight - aHeight - mPageRect.mHeight, aRect.mWidth, aHeight);
                aRect.mHeight -= mApp.mScreenPos.At(0x3cc) + aHeight;
            }
            if (mScroll != null)
            {
                mScroll.SetScrollInsets(new Insets(0, mTopEdge.mHeight, 0, mBottomEdge.mHeight));
                mScroll.Resize(aRect);
            }
            mText?.Resize(0, 0, aRect.mWidth, aRect.mHeight);
            mEdges?.Resize(0, 0, mWidth, mHeight);
        }

        /// <summary>FUN_00035f64: a heading in green, then " (Classic)" or " (Action)" in orange for a mode.</summary>
        public void AddHeading(string theHeading, int theMode)
        {
            mText.SetFont(mHeadingFont);
            mText.SetColor(GetColor(2));
            mText.AddChunk(SexyAppBase.Loc(theHeading));
            mText.SetColor(GetColor(3));
            if (theMode == BookWormApp.GAME_CLASSIC)
            {
                mText.AddChunk(SexyAppBase.Loc(" (Classic)"));
            }
            else if (theMode == BookWormApp.GAME_ARCADE)
            {
                mText.AddChunk(SexyAppBase.Loc(" (Action)"));
            }
            mText.NewLine(0);
        }

        /// <summary>FUN_000361a8: ends an entry, with the entry gap (ScreenPos + 0x3cc).</summary>
        public void EndEntry()
        {
            mText.NewLine(mApp.mScreenPos.At(0x3cc));
        }

        /// <summary>FUN_00036748.</summary>
        public void EndLine()
        {
            mText.NewLine(0);
        }

        /// <summary>FUN_000363d8: a score in the gold score font.</summary>
        public void AddScore(int theScore)
        {
            mText.SetFont(mScoreFont);
            mText.SetColor(WHITE);
            mText.AddChunk(theScore.ToString());
        }

        /// <summary>FUN_00036524: "Rank N: " and the rank's title.</summary>
        public void AddRank(int theRank)
        {
            string aName = SexyAppBase.Loc(RANK_NAMES[System.Math.Min(theRank, 0x1f)]);
            mText.SetFont(mTextFont);
            mText.SetColor(BLACK);
            mText.AddChunk(string.Format(SexyAppBase.Loc("Rank {0}: "), theRank));
            mText.SetColor(RANK_COLOR);
            mText.AddChunk(aName);
        }

        /// <summary>FUN_00035d48.</summary>
        public void AddLongestWord(int theMode, string theWord)
        {
            AddHeading("Longest Word", theMode);
            mText.SetFont(mTextFont);
            mText.SetColor(BLACK);
            mText.AddChunk(string.Format(SexyAppBase.Loc("{0} ({1} letters)"), theWord, theWord.Length));
            EndEntry();
        }

        /// <summary>FUN_000361c0.</summary>
        public void AddBestWord(int theMode, string theWord, int theScore)
        {
            AddHeading("Best Word", theMode);
            mText.SetFont(mTextFont);
            mText.SetColor(BLACK);
            mText.AddChunk(string.Format(SexyAppBase.Loc("{0} ({1} pts)"), theWord, theScore));
            EndEntry();
        }

        /// <summary>vf82 (0x36758): the scroller back at the top, its indicators flashed.</summary>
        public override void AttachToManager(int theState, int theTarget, int theParam)
        {
            base.AttachToManager(theState, theTarget, theParam);
            if (mScroll == null)
            {
                return;
            }
            mScroll.ClientSizeChanged();
            mScroll.ScrollToMin(false);
            mScroll.FlashIndicators();
        }

        /// <summary>vf94 (0x36784): the shaded edges over the scroller (x at ScreenPos + 0x1a8 / 0x1b0).</summary>
        public virtual void DrawProxyWidget(Graphics g, ProxyWidget theProxy)
        {
            if (mScroll == null || theProxy != mEdges)
            {
                return;
            }
            ScreenPos aPos = mApp.mScreenPos;
            g.DrawImage(mTopEdge, aPos.At(0x1a8), mScroll.mY);
            g.DrawImage(mBottomEdge, aPos.At(0x1b0), mScroll.mY + mScroll.mHeight - mBottomEdge.mHeight + aPos.At(0x1b4));
        }

        /// <summary>ButtonListener::ButtonPress: nothing by default.</summary>
        public virtual void ButtonPress(int theId)
        {
        }

        public void ButtonPress(int theId, int theClickCount)
        {
            ButtonPress(theId);
        }

        /// <summary>vf95 (ButtonDepress, 0x36820): nothing by default.</summary>
        public virtual void ButtonDepress(int theId)
        {
        }

        public void ButtonDownTick(int theId)
        {
        }

        public void ButtonMouseEnter(int theId)
        {
        }

        public void ButtonMouseLeave(int theId)
        {
        }

        public void ButtonMouseMove(int theId, int theX, int theY)
        {
        }
    }
}
