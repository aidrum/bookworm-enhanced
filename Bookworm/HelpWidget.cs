using Microsoft.Xna.Framework;

namespace Sexy
{
    /// <summary>
    /// Sexy::HelpWidget (0x1c21c, screen 15, state 9): How to Play. Nine picture pages in a paged horizontal
    /// scroller with page dots, under a picture frame; Lex explains each page in his speech bubble, and tapping
    /// the bubble turns to the next page. Slides down from the top.
    /// </summary>
    public class HelpWidget : SlideInWidget, ScrollWidgetListener, ProxyWidgetListener, ButtonListener
    {
        public const int PAGE_COUNT = 9;

        /// <summary>What Lex says on each page (statics 0x135680).</summary>
        private static readonly string[] PAGE_TEXTS =
        {
            "Select adjacent letter tiles to spell words!",
            "Tap the word bubble or the last letter tile to submit your word!",
            "Don't let fire tiles burn through the bottom or it's game over!",
            "Shake your device to scramble all of your tiles!",
            "Turn your device to see the Bonus Book List! Can you spell them all?",
            "Credits: (n.) Acknowledgement and praise for the awesome creators of Lex!",
            "Update (n): The act or an instance of bringing something up to date. Now with 64-bit support!",
            "Original (adj): Preceding all others in time; first. There's nothing quite like Bookworm!",
            "Lex: (n.) Me! The best-spelling worm of all time!"
        };

        /// <summary>The pages' atlas image ids (0x2735...).</summary>
        private const int FIRST_PAGE_IMAGE_ID = 0x2735;

        public PageControl mPageControl; // 0xe4

        public ScrollWidget mScroll; // 0xe8

        /// <summary>The scroller's content: the pages side by side (0xec).</summary>
        public ProxyWidget mPages;

        /// <summary>Draws the frame over everything (0xf0).</summary>
        public ProxyWidget mFrame;

        public Image[] mPageImages = new Image[PAGE_COUNT]; // 0xf4

        public int mPage; // 0x118

        public HelpWidget(BookWormApp theApp) : base(theApp)
        {
            mPage = 0;
            for (int i = 0; i < PAGE_COUNT; i++)
            {
                mPageImages[i] = AtlasResourceBase.GetImageById(FIRST_PAGE_IMAGE_ID + i);
            }
            mScroll = new ScrollWidget(this);
            mScroll.Resize(theApp.mScreenPos.RectAt(0x280));
            mScroll.SetScrollMode(ScrollWidget.ScrollMode.Horizontal);
            mScroll.EnableBounce(false);
            mScroll.EnablePaging(true);
            AddWidget(mScroll);
            mPages = new ProxyWidget(this);
            mPages.mClip = false;
            mPages.Resize(0, 0, mScroll.mWidth * PAGE_COUNT, mScroll.mHeight);
            mScroll.AddWidget(mPages);
            mPageControl = new PageControl(AtlasResourceBase.IMAGE_GROUP1_24);
            mPageControl.SetNumberOfPages(PAGE_COUNT);
            mPageControl.SetCurrentPage(mPage);
            AddWidget(mPageControl);
            mScroll.SetPageControl(mPageControl);
            mFrame = new ProxyWidget(this);
            mFrame.Resize(0, 0, mWidth, mHeight);
            mFrame.mMouseVisible = false;
            mFrame.mClip = false;
            AddWidget(mFrame);
        }

        /// <summary>vf82 (0x1c77c): placed at ScreenPos + 0x1e0, slides down; the dots under the pages (ScreenPos + 0xc4).</summary>
        /// <summary>Port (dynamic aspect): the pages' scroller is placed once, in the constructor.</summary>
        public override void Relayout()
        {
            mScroll.Resize(mApp.mScreenPos.RectAt(0x280));
            base.Relayout();
        }

        public override void AttachToManager(int theState, int theTarget, int theParam)
        {
            base.AttachToManager(theState, theTarget, theParam);
            Resize(mApp.mScreenPos.RectAt(0x1e0));
            mFrame.Resize(0, 0, mWidth, mHeight);
            SetupSlide(SLIDE_FROM_TOP, 0.5f);
            mPageControl.Move((mWidth - mPageControl.mWidth) / 2, mApp.mScreenPos.At(0xc4) - mPageControl.mHeight / 2);
            mScroll.FlashIndicators();
        }

        /// <summary>vf81 (0x1c7e8): once shown, Lex explains the page; tapping his bubble turns the page.</summary>
        public override void InterfaceStateDone()
        {
            base.InterfaceStateDone();
            if (mInterfaceState != 9)
            {
                return;
            }
            SayPageText();
            mApp.mWordBalloon.SetListener(0, this);
        }

        private void SayPageText()
        {
            if (PAGE_TEXTS[mPage].Length == 0)
            {
                mApp.mWordBalloon.Hide();
            }
            else
            {
                mApp.mWordBalloon.Speak(SexyAppBase.Loc(PAGE_TEXTS[mPage]));
            }
        }

        /// <summary>0x1c83c: the "How to Play" title, centred at the top.</summary>
        public override void Draw(Graphics g)
        {
            base.Draw(g);
            Image aTitle = AtlasResourceBase.IMAGE_GROUP11_15;
            // Port (phones): below a camera cutout (notch) reaching past the page's top (its resting place, 0x1e0).
            int aTitleY = System.Math.Max(0, Main.GetSafeInsetTopInGame() - mApp.mScreenPos.At(0x1e4));
            if (Localization.IsSpanish)
            {
                // Port (Spanish): the title art has the English baked in; draw it as the menu button does.
                Font aFont = Res.FONT_COOPER_OUTLINED;
                int aBaseline = aTitleY + (aTitle.mHeight - aFont.GetLineSpacing()) / 2 + aFont.GetAscent();
                GradientText.Draw(g, aFont, new[] { BWGameSelector.HOW_TO_PLAY_TEXT_ES }, mWidth / 2, aBaseline,
                    BWGameSelector.HOW_TO_PLAY_TEXT_TOP, BWGameSelector.HOW_TO_PLAY_TEXT_BOTTOM);
                return;
            }
            g.DrawImage(aTitle, (mWidth - aTitle.mWidth) / 2, aTitleY);
        }

        /// <summary>
        /// vf94 (0x1c86c): the pages strip (black beyond its ends, each page one scroller width apart), or the
        /// frame (IMAGE_GROUP23_00 at the scroller's position).
        /// </summary>
        public void DrawProxyWidget(Graphics g, ProxyWidget theProxy)
        {
            if (theProxy == mPages)
            {
                int aPageWidth = mScroll.mWidth;
                int aHeight = mScroll.mHeight;
                g.SetColor(new Color(0, 0, 0, 255));
                g.FillRect(-aPageWidth, 0, aPageWidth, aHeight);
                g.FillRect(aPageWidth * PAGE_COUNT, 0, aPageWidth, aHeight);
                for (int i = 0; i < PAGE_COUNT; i++)
                {
                    g.DrawImage(mPageImages[i], aPageWidth * i, 0);
                }
            }
            else if (theProxy == mFrame)
            {
                g.DrawImage(AtlasResourceBase.IMAGE_GROUP23_00, mApp.mScreenPos.At(0x280), mApp.mScreenPos.At(0x284));
            }
        }

        /// <summary>vf95 (0x1c988): on another page, Lex explains it.</summary>
        public void ScrollTargetReached(ScrollWidget theScrollWidget)
        {
            int aPage = theScrollWidget.GetPageHorizontal();
            if (aPage == mPage)
            {
                return;
            }
            mPage = aPage;
            SayPageText();
        }

        /// <summary>vf96 (0x1ca10).</summary>
        public void ScrollTargetInterrupted(ScrollWidget theScrollWidget)
        {
            ScrollTargetReached(theScrollWidget);
        }

        /// <summary>vf97 (0x1ca30): the bubble was touched.</summary>
        public void ButtonPress(int theId)
        {
            mApp.PlaySound(Res.SOUND_BUTTON_CLICK);
        }

        public void ButtonPress(int theId, int theClickCount)
        {
            ButtonPress(theId);
        }

        /// <summary>vf98 (0x1ca68): tapping the bubble turns to the next page.</summary>
        public void ButtonDepress(int theId)
        {
            if (theId != 0 || mPage + 1 >= PAGE_COUNT)
            {
                return;
            }
            mScroll.SetPageHorizontal(mPage + 1, true);
            mPageControl.SetCurrentPage(mScroll.GetPageHorizontal());
            mApp.mWordBalloon.Hide();
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
