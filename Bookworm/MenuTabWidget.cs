using Microsoft.Xna.Framework;

namespace Sexy
{
    /// <summary>
    /// Sexy::MenuTabWidget (0x320bc, screen 7): the bar of menu tabs along the bottom (Play, Stats, Books,
    /// Options, More Games). The current menu's tab shows its lit image; Books shows the collections'
    /// completion. It slides up from the bottom.
    /// </summary>
    public class MenuTabWidget : SlideInWidget, ButtonListener
    {
        public const int TAB_PLAY = 0;

        public const int TAB_STATS = 1;

        public const int TAB_BOOKS = 2;

        public const int TAB_OPTIONS = 3;

        public const int TAB_MORE_GAMES = 4;

        /// <summary>The tab lit in each interface state (0x10c9fc; -1 none).</summary>
        private static readonly int[] STATE_TABS = { -1, -1, 0, 1, 2, 2, 2, 3, 4, 0, 0, 0, 0, 0, 0, 0 };

        /// <summary>The interface state each tab opens.</summary>
        private static readonly int[] TAB_STATES = { 2, 3, 4, 7, 8 };

        /// <summary>The books tab's percentage colour (0x10cf6c).</summary>
        private static readonly Color LABEL_COLOR = new Color(0, 0, 0, 255);

        public Image mImage; // 0xdc

        public BookWormButton[] mTabs = new BookWormButton[5]; // 0xe0

        public MenuTabWidget(BookWormApp theApp) : base(theApp)
        {
            mImage = AtlasResourceBase.IMAGE_GROUP11_01;
            // Each tab: its unlit and lit images, and its place (ScreenPos + 0x40, 8 bytes apart).
            Image[,] aTabImages =
            {
                { AtlasResourceBase.IMAGE_GROUP1_16, AtlasResourceBase.IMAGE_GROUP1_15 },
                { AtlasResourceBase.IMAGE_GROUP1_11, AtlasResourceBase.IMAGE_GROUP1_12 },
                { AtlasResourceBase.IMAGE_GROUP1_07, AtlasResourceBase.IMAGE_GROUP1_08 },
                { AtlasResourceBase.IMAGE_GROUP1_09, AtlasResourceBase.IMAGE_GROUP1_10 },
                { AtlasResourceBase.IMAGE_GROUP1_13, AtlasResourceBase.IMAGE_GROUP1_14 }
            };
            for (int i = 0; i < mTabs.Length; i++)
            {
                BookWormButton aTab = new MenuTabButton(i, this, theApp);
                Image anUnlit = aTabImages[i, 0];
                aTab.Resize(theApp.mScreenPos.At(0x40 + 8 * i), theApp.mScreenPos.At(0x44 + 8 * i), anUnlit.mWidth, anUnlit.mHeight);
                aTab.mButtonImage = anUnlit;
                aTab.mOverImage = aTabImages[i, 1];
                AddWidget(aTab);
                mTabs[i] = aTab;
            }
        }

        /// <summary>vf80 (0x32684): lights the tab of the new state.</summary>
        /// <summary>Port (dynamic aspect): the tabs are placed once, in the constructor.</summary>
        public override void Relayout()
        {
            ScreenPos aPos = mApp.mScreenPos;
            for (int i = 0; i < mTabs.Length; i++)
            {
                mTabs[i].Move(aPos.At(0x40 + 8 * i), aPos.At(0x44 + 8 * i));
            }
            base.Relayout();
        }

        public override void SetInterfaceState(int theState, int theTarget, int theParam)
        {
            int aTab = STATE_TABS[theState];
            for (int i = 0; i < mTabs.Length; i++)
            {
                mTabs[i].SetSelected(i == aTab ? 1 : 0);
            }
            base.SetInterfaceState(theState, theTarget, theParam);
        }

        /// <summary>vf82 (0x3272c): placed (ScreenPos + 0x38) to slide up; the Books tab shows the completion.</summary>
        public override void AttachToManager(int theState, int theTarget, int theParam)
        {
            base.AttachToManager(theState, theTarget, theParam);
            // Port (wide screens): the bar spans the screen; the tabs (ScreenPos) keep to the middle.
            Resize(mApp.mScreenPos.At(0x38), mApp.mScreenPos.At(0x3c), System.Math.Max(mImage.mWidth, mApp.mWidth), mImage.mHeight);
            SetupSlide(SLIDE_FROM_BOTTOM, 0.5f);
            BookWormButton aBooks = mTabs[TAB_BOOKS];
            aBooks.mLabel = string.Format("{0}%", mApp.mCollectionator.GetCompletionPercent());
            aBooks.mLabelJustify = 2;
            aBooks.SetFont(Res.FONT_ARIAL_SMALL);
            aBooks.SetColor(0, LABEL_COLOR);
            aBooks.SetColor(1, LABEL_COLOR);
            aBooks.SetLabelPos(new TPoint(mApp.mScreenPos.At(0x68), mApp.mScreenPos.At(0x6c)));
        }

        public override void Draw(Graphics g)
        {
            base.Draw(g);
            // Port: the iPad's full-width tab bar is centred in the iPhone design (ScreenPos.mWideImageX); on wide
            // screens it is centred too, its end columns stretched to the screen's edges.
            ScreenPos aPos = mApp.mScreenPos;
            int aX = (mImage.mWidth > aPos.mDesignWidth ? aPos.mWideImageX : 0) + aPos.mCenterX;
            ScreenPos.DrawStretchedEdges(g, mImage, aX, 0, mApp.mWidth);
        }

        /// <summary>vf94 (0x32914).</summary>
        public void ButtonPress(int theId)
        {
            mApp.PlaySound(Res.SOUND_BUTTON_CLICK);
        }

        public void ButtonPress(int theId, int theClickCount)
        {
            ButtonPress(theId);
        }

        /// <summary>
        /// vf95 (0x3294c): opens the tab's menu. More Games first checks the connection (FUN_00041e34) and
        /// otherwise shows a "No network connection" alert (FUN_000a157c); the port's page is local, so it always opens.
        /// </summary>
        public void ButtonDepress(int theId)
        {
            if (theId >= 0 && theId < TAB_STATES.Length)
            {
                mApp.GotoInterfaceState(TAB_STATES[theId]);
            }
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

    /// <summary>
    /// Port (Spanish): a menu tab whose art is cut above its English name (baked into the art), with the Spanish
    /// name drawn below the icon instead, like the art's (rounded bold, outlined, with a shadow; gold while lit,
    /// khaki otherwise). In English, a plain BookWormButton.
    /// </summary>
    public class MenuTabButton : BookWormButton
    {
        /// <summary>The Spanish names, by tab.</summary>
        private static readonly string[][] SPANISH_NAMES =
        {
            new[] { "Jugar" },
            new[] { "Info" },
            new[] { "Libros" },
            new[] { "Opciones" },
            new[] { "Más", "Juegos" }
        };

        /// <summary>
        /// Where each tab's name starts in its art, and the art's height (iPad Retina rows: Play, Stats, Books,
        /// Options, More Games); the same art at every resolution, so it scales with the image.
        /// </summary>
        private static readonly int[,] TEXT_TOPS = { { 206, 272 }, { 212, 280 }, { 215, 268 }, { 217, 280 }, { 184, 292 } };

        /// <summary>The art's names: their cap height and line pitch in the iPad Retina art.</summary>
        private const int CAP_HEIGHT = 38;

        private const int LINE_PITCH = 40;

        /// <summary>
        /// The names a little smaller than the font (Arial Rounded, 52pt at iPad Retina; there is no bold one), to
        /// the art's size.
        /// </summary>
        private const float TEXT_SCALE = 0.85f;

        private static readonly Color LIT_TOP = new Color(255, 248, 150, 255);

        private static readonly Color LIT_BOTTOM = new Color(252, 200, 10, 255);

        private static readonly Color UNLIT_TOP = new Color(176, 168, 124, 255);

        private static readonly Color UNLIT_BOTTOM = new Color(140, 128, 60, 255);

        public MenuTabButton(int theId, ButtonListener theListener, BookWormApp theApp) : base(theId, theListener, theApp)
        {
        }

        public override void Draw(Graphics g)
        {
            bool aSpanish = Localization.IsSpanish && mId >= 0 && mId < SPANISH_NAMES.Length && mButtonImage != null;
            int aHeight = mButtonImage != null ? mButtonImage.mHeight : mHeight;
            int aTextTop = aSpanish ? aHeight * TEXT_TOPS[mId, 0] / TEXT_TOPS[mId, 1] : 0;
            mImageRows = aTextTop;
            mClip = !aSpanish;
            base.Draw(g);
            if (!aSpanish || mBtnNoDraw)
            {
                return;
            }
            // This resolution's scale against the iPad Retina art.
            float aUnit = aHeight / (float)TEXT_TOPS[mId, 1];
            bool aLit = mSelected == 1;
            Font aFont = Res.FONT_ARIAL_MEDIUM_OUTLINED;
            GradientText.Draw(g, aFont, SPANISH_NAMES[mId], mWidth / 2, aTextTop + (int)System.Math.Round(CAP_HEIGHT * aUnit),
                aLit ? LIT_TOP : UNLIT_TOP, aLit ? LIT_BOTTOM : UNLIT_BOTTOM, null, (int)System.Math.Round(LINE_PITCH * aUnit / TEXT_SCALE), TEXT_SCALE);
        }
    }
}
