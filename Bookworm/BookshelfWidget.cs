using Microsoft.Xna.Framework;

namespace Sexy
{
    /// <summary>
    /// Sexy::BookshelfWidget (0x32e50, screen 10, state 4; 13 in landscape): the Books tab: the collection
    /// shelf (id 0; a book opens its page, state 5 / 14) and, in a scroller, the word history shelves (id 1; a
    /// length opens its list, state 6 / 15), with the share of collection words found on a sign ("N%", or a
    /// plain sign at 100%). Slides up from the bottom.
    /// </summary>
    public class BookshelfWidget : SlideInWidget, ShelfListener, ProxyWidgetListener
    {
        /// <summary>The percent text colour (0x1362b8).</summary>
        private static readonly Color PERCENT_COLOR = new Color(0xff, 0xff, 0x00);

        /// <summary>Where IMAGE_GROUP7_04 (0xe0), IMAGE_GROUP7_05 and the sign (0xe8), the side image
        /// (0xf0; 0x100) and IMAGE_GROUP31_02 (0xf8) go.</summary>
        public TPoint mShelfTopPos;

        public TPoint mSignPos;

        public TPoint mSidePos;

        public TPoint mShelfEndPos;

        public Image mSideImage;

        public CollectionShelfWidget mCollectionShelf; // 0x104

        public HistoryShelfWidget mHistoryShelf; // 0x108

        public ScrollWidget mScroll; // 0x10c

        /// <summary>Draws the sign and the rest over the shelves (0x110).</summary>
        public ProxyWidget mOverlay;

        /// <summary>"N%" while the collections are not complete (0x114).</summary>
        public string mPercent = "";

        /// <summary>Set to switch to state 14 once shown and enabled (0x118; nothing in the port sets it yet).</summary>
        public int mPendingState14;

        /// <summary>Laid out for landscape (0x11c).</summary>
        public bool mLandscape;

        public BookshelfWidget(BookWormApp theApp) : base(theApp)
        {
            mCollectionShelf = new CollectionShelfWidget(theApp, 0, this);
            AddWidget(mCollectionShelf);
            mHistoryShelf = new HistoryShelfWidget(theApp, 1, this);
            mHistoryShelf.Resize(0, 0, 0, 0);
            mScroll = new ScrollWidget();
            mScroll.EnableIndicators(AtlasResourceBase.IMAGE_GROUP11_21);
            mScroll.AddWidget(mHistoryShelf);
            AddWidget(mScroll);
            mOverlay = new ProxyWidget(this);
            mOverlay.mMouseVisible = false;
            AddWidget(mOverlay);
        }

        private TPoint PosAt(int theOffset)
        {
            return new TPoint(mApp.mScreenPos.At(theOffset), mApp.mScreenPos.At(theOffset + 4));
        }

        private Insets InsetsAt(int theOffset)
        {
            ScreenPos aPos = mApp.mScreenPos;
            return new Insets(aPos.At(theOffset), aPos.At(theOffset + 4), aPos.At(theOffset + 8), aPos.At(theOffset + 12));
        }

        /// <summary>vf82 (0x333d0): rebuilds the shelves, lays out for the orientation (theParam 1 = landscape).</summary>
        public override void AttachToManager(int theState, int theTarget, int theParam)
        {
            base.AttachToManager(theState, theTarget, theParam);
            mCollectionShelf.Setup(theState);
            mHistoryShelf.Setup(theState);
            mScroll.ScrollToMin(false);
            mScroll.FlashIndicators();
            ScreenPos aPos = mApp.mScreenPos;
            mLandscape = theParam == 1;
            int aBase = mLandscape ? 0x100 : 0xe0;
            if (mLandscape)
            {
                Resize(0, 0, aPos.At(0x21c), aPos.At(0x218));
            }
            else
            {
                // Port (wide screens): centred with the design.
                Resize(aPos.mCenterX, 0, aPos.At(0x218), aPos.At(0x21c));
            }
            mOverlay.Resize(0, 0, mWidth, mHeight);
            mShelfTopPos = PosAt(aBase);
            mSignPos = PosAt(aBase + 8);
            mSidePos = PosAt(aBase + 0x10);
            mSideImage = mLandscape ? AtlasResourceBase.IMAGE_GROUP31_04 : AtlasResourceBase.IMAGE_GROUP31_05;
            mCollectionShelf.Resize(aPos.RectAt(mLandscape ? 0x2b0 : 0x290));
            mShelfEndPos = PosAt(aBase + 0x18);
            mScroll.Resize(aPos.RectAt(mLandscape ? 0x2c0 : 0x2a0));
            mScroll.SetIndicatorsInsets(InsetsAt(mLandscape ? 0x390 : 0x3a0));
            mScroll.ClientSizeChanged();
            SetupSlide(SLIDE_FROM_BOTTOM, 0.5f);
            int aPercent = mApp.mCollectionator.GetCompletionPercent();
            mPercent = aPercent < 100 ? aPercent + "%" : "";
        }

        /// <summary>0x336fc.</summary>
        public override void Update()
        {
            base.Update();
            if (mVisible && !mDisabled && mPendingState14 == 1)
            {
                mApp.GotoInterfaceState(14);
                mPendingState14 = 0;
            }
        }

        /// <summary>
        /// Port: the side pillars are the iPad design's (0x138 / 0x140, from the portrait bookshelf's origin at
        /// (mCenterX, 0)); the iPhone design has none. theX, theY: that origin in the drawing widget (the More
        /// Games screen has them too).
        /// </summary>
        public static void DrawPillars(Graphics g, ScreenPos thePos, int theX, int theY)
        {
            if (!thePos.mPillars)
            {
                return;
            }
            if (AtlasResourceBase.IMAGE_GROUP13_02 != null)
            {
                g.DrawImage(AtlasResourceBase.IMAGE_GROUP13_02, theX + thePos.At(0x138), theY + thePos.At(0x13c));
            }
            if (AtlasResourceBase.IMAGE_GROUP13_01 != null)
            {
                g.DrawImage(AtlasResourceBase.IMAGE_GROUP13_01, theX + thePos.At(0x140), theY + thePos.At(0x144));
            }
        }

        /// <summary>0x3372c: the shelf ends (portrait), then the shelf tops.</summary>
        public override void Draw(Graphics g)
        {
            base.Draw(g);
            ScreenPos aPos = mApp.mScreenPos;
            if (!mLandscape)
            {
                DrawPillars(g, aPos, 0, 0);
            }
            g.DrawImage(AtlasResourceBase.IMAGE_GROUP7_04, mShelfTopPos.mX, mShelfTopPos.mY);
            g.DrawImage(AtlasResourceBase.IMAGE_GROUP7_05, mSignPos.mX, mSignPos.mY);
        }

        /// <summary>
        /// vf94 (0x337b0): over the shelves: IMAGE_GROUP31_02, the side image, and the sign: plain
        /// (IMAGE_GROUP31_03) when complete, else IMAGE_GROUP31_24 with "N%" in yellow. In landscape, the extra
        /// scenery of the wide layouts: on iPad (short side 768/1536) IMAGE_GROUP6_01 (ScreenPos 0x120),
        /// IMAGE_GROUP13_03 (0x128) and IMAGE_GROUP13_04 (0x130); on iPhone 5 (long side 1136) IMAGE_GROUP13_02
        /// (0x128) and IMAGE_GROUP13_01 (0x130). Port: any iPhone-family screen wider than 960 gets the latter.
        /// </summary>
        public void DrawProxyWidget(Graphics g, ProxyWidget theProxy)
        {
            ScreenPos aPos = mApp.mScreenPos;
            g.DrawImage(AtlasResourceBase.IMAGE_GROUP31_02, mShelfEndPos.mX, mShelfEndPos.mY);
            g.DrawImage(mSideImage, mSidePos.mX, mSidePos.mY);
            if (mPercent.Length == 0)
            {
                g.DrawImage(AtlasResourceBase.IMAGE_GROUP31_03, mSignPos.mX + aPos.At(0xd0), mSignPos.mY + aPos.At(0xd4));
                return;
            }
            g.DrawImage(AtlasResourceBase.IMAGE_GROUP31_24, mSignPos.mX + aPos.At(0xc8), mSignPos.mY + aPos.At(0xcc));
            Font aFont = Res.FONT_ARIAL_LARGE;
            g.SetFont(aFont);
            g.SetColor(PERCENT_COLOR);
            g.SetColorizeImages(true);
            g.DrawString(mPercent, mSignPos.mX + aPos.At(0xd8) - aFont.StringWidth(mPercent) / 2, mSignPos.mY + aPos.At(0xdc));
            g.SetColorizeImages(false);
            DrawLandscapeScenery(g);
        }

        private void DrawLandscapeScenery(Graphics g)
        {
            if (!mLandscape)
            {
                return;
            }
            ScreenPos aPos = mApp.mScreenPos;
            int aShort = System.Math.Min(mApp.mWidth, mApp.mHeight);
            // Port (dynamic aspect): the shelf is centred (0x100 = its left margin), so on screens wider than
            // the original's the side fillers stretch to the margin instead of leaving a gap.
            int aMargin = aPos.At(0x100);
            if (aShort == 0x300 || aShort == 0x600)
            {
                if (AtlasResourceBase.IMAGE_GROUP6_01 != null)
                {
                    g.DrawImage(AtlasResourceBase.IMAGE_GROUP6_01, aPos.At(0x120), aPos.At(0x124));
                }
                DrawFiller(g, AtlasResourceBase.IMAGE_GROUP13_03, aPos.At(0x128), aPos.At(0x12c), aMargin + 1 - aPos.At(0x128));
                if (AtlasResourceBase.IMAGE_GROUP13_04 != null)
                {
                    g.DrawImage(AtlasResourceBase.IMAGE_GROUP13_04, aPos.At(0x130), aPos.At(0x134));
                }
            }
            else if (aMargin > 0)
            {
                // The iPhone 5 (long side 1136, margin 88); the 960 layout had no margin and no fillers.
                DrawFiller(g, AtlasResourceBase.IMAGE_GROUP13_02, aPos.At(0x128), aPos.At(0x12c), aMargin - aPos.At(0x128));
                DrawFiller(g, AtlasResourceBase.IMAGE_GROUP13_01, aPos.At(0x130), aPos.At(0x134), System.Math.Max(mApp.mWidth, mApp.mHeight) - aPos.At(0x130));
            }
        }

        /// <summary>Port: a side filler at its own size, or stretched across a wider margin.</summary>
        private static void DrawFiller(Graphics g, Image theImage, int theX, int theY, int theWidth)
        {
            if (theImage == null)
            {
                return;
            }
            if (theWidth <= theImage.mWidth)
            {
                g.DrawImage(theImage, theX, theY);
            }
            else
            {
                g.DrawImage(theImage, theX, theY, theWidth, theImage.mHeight);
            }
        }

        /// <summary>vf95 (0x33964): a history shelf (id 1) opens that length's list; a collection book (id 0) its page.</summary>
        public void ShelfPicked(int theId, int theIndex)
        {
            Collectionator aCollectionator = mApp.mCollectionator;
            if (theId == 1)
            {
                if (theIndex < Collectionator.HISTORY_LENGTHS)
                {
                    aCollectionator.mHistoryTab = theIndex;
                }
                if (mInterfaceState == 4)
                {
                    mApp.GotoInterfaceState(6);
                }
                else if (mInterfaceState == 13)
                {
                    mApp.GotoInterfaceState(15);
                }
            }
            else if (theId == 0)
            {
                if (aCollectionator.GetBook(theIndex) != null)
                {
                    aCollectionator.mCurrentBook = theIndex;
                }
                if (mInterfaceState == 4)
                {
                    mApp.GotoInterfaceState(5);
                }
                else if (mInterfaceState == 13)
                {
                    mApp.GotoInterfaceState(14);
                }
            }
        }
    }
}
