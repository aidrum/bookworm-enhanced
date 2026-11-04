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
                BookWormButton aTab = new BookWormButton(i, this, theApp);
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
            Resize(mApp.mScreenPos.At(0x38), mApp.mScreenPos.At(0x3c), mImage.mWidth, mImage.mHeight);
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
            // Port: the iPad's full-width tab bar is centred in the iPhone design (ScreenPos.mWideImageX).
            g.DrawImage(mImage, mImage.mWidth > mApp.mWidth ? mApp.mScreenPos.mWideImageX : 0, 0);
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
}
