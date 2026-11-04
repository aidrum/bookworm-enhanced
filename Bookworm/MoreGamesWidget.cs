using Microsoft.Xna.Framework;

namespace Sexy
{
    /// <summary>
    /// Sexy::MoreGamesWidget (screen 14, state 8), as in the old iPhone-only version (2.2.1, ctor 0x4461e): the
    /// games of net_content/more_games.xml in trays (<see cref="MoreGamesListWidget"/>) in a scroller filled
    /// black, under the "More Games" title (drawn by a proxy so it stays on top); once in place Lex presents the
    /// &lt;lexText&gt; blurb. The later version (0x417b4) is an empty screen under a web view of a catalog that is
    /// gone. The old version only knew the 320x480 screen; its frames are kept here in iPhone units and scaled by
    /// the layout (<see cref="LayoutScale"/>), and its images come from its atlas (tools/convert_assets.py
    /// --old-ipa).
    /// </summary>
    public class MoreGamesWidget : SlideInWidget, ProxyWidgetListener, DialogListener
    {
        /// <summary>MORE_GAMES_FRAME's top and bottom against the Options frame (ScreenPos 0x1f0, the old
        /// OPTIONS_CONTENT_FRAME (0, 88, 320, 315)): (0, 100, 320, 308).</summary>
        private const int FRAME_TOP = 12;

        private const int FRAME_BOTTOM = 5;

        /// <summary>MORE_GAMES_LIST_FRAME (0, 42, 320, 266): the scroller, to the bottom of the frame.</summary>
        private const int LIST_TOP = 42;

        /// <summary>The old version's screen width.</summary>
        private const int OLD_WIDTH = 320;

        private const string CONTENT_DIR = "net_content/";

        /// <summary>The &lt;lexText&gt; blurb (0xe0).</summary>
        public string mLexText = "";

        public ScrollWidget mScroll; // 0xe4

        public MoreGamesListWidget mList; // 0xe8

        public ProxyWidget mTitleProxy; // 0xec

        public Image mTitleImage; // IMAGE_MORE_GAMES_TITLE, at MORE_GAMES_TITLE_POS (0, 0)

        public MoreGamesWidget(BookWormApp theApp) : base(theApp)
        {
            mTitleImage = theApp.GetImage("images/moregames/title", false);
            mList = new MoreGamesListWidget(theApp, 0, this);
            LoadEmUp();
            mScroll = new ScrollWidget();
            mScroll.EnableIndicators(AtlasResourceBase.IMAGE_GROUP11_21);
            mScroll.SetColor(0, new Color(0, 0, 0, 255));
            mScroll.EnableBackgroundFill(true);
            mScroll.AddWidget(mList);
            AddWidget(mScroll);
            mTitleProxy = new ProxyWidget(this);
            mTitleProxy.mMouseVisible = false;
            AddWidget(mTitleProxy);
        }

        public override void Dispose()
        {
            RemoveAllWidgets(true, true);
            mTitleImage?.Dispose();
            base.Dispose();
        }

        /// <summary>Port: the layout's scale over the iPhone (1, 2 or 4), from the More Games balloon's width
        /// (ScreenPos 0x270, the old FRAME_MORE_GAMES (66, 3, 250, 94)).</summary>
        public int LayoutScale()
        {
            return System.Math.Max(1, mApp.mScreenPos.RectAt(0x270).mWidth / 250);
        }

        /// <summary>LoadEmUp (0x437d8): the blurb, and a row per &lt;entry image link&gt; whose banner loads (AddRow
        /// 0x43510: GetResourceDir() + "net_content/" + image).</summary>
        private void LoadEmUp()
        {
            XMLParser aParser = new XMLParser();
            if (!aParser.OpenFile(CONTENT_DIR + "more_games.xml"))
            {
                return;
            }
            string aText = "";
            XMLElement anElement = new XMLElement();
            while (aParser.NextElement(ref anElement))
            {
                if (anElement.mType == XMLElementType.TYPE_START)
                {
                    aText = "";
                    if (anElement.mValue == "entry")
                    {
                        anElement.mAttributes.TryGetValue("image", out string anImageName);
                        anElement.mAttributes.TryGetValue("link", out string aLink);
                        if (!string.IsNullOrEmpty(anImageName) && !string.IsNullOrEmpty(aLink))
                        {
                            Image anImage = mApp.GetImage(CONTENT_DIR + anImageName, false);
                            if (anImage != null)
                            {
                                mList.AddRow(anImage, aLink);
                            }
                        }
                    }
                }
                else if (anElement.mType == XMLElementType.TYPE_END)
                {
                    if (anElement.mValue == "lexText")
                    {
                        mLexText = aText;
                    }
                    aText = "";
                }
                else if (anElement.mType == XMLElementType.TYPE_ELEMENT)
                {
                    aText += anElement.mValue;
                }
            }
        }

        /// <summary>
        /// SetupForState (0x432f0): to MORE_GAMES_FRAME (centred, scaled), the proxy over all of it, the scroller
        /// to MORE_GAMES_LIST_FRAME with indicator insets (2, 2, 4, 2) flashing; slides up.
        /// </summary>
        public override void AttachToManager(int theState, int theTarget, int theParam)
        {
            base.AttachToManager(theState, theTarget, theParam);
            int s = LayoutScale();
            TRect anOptions = mApp.mScreenPos.RectAt(0x1f0);
            int aWidth = OLD_WIDTH * s;
            int aTop = anOptions.mY + FRAME_TOP * s;
            Resize((mApp.mWidth - aWidth) / 2, aTop, aWidth, anOptions.mY + anOptions.mHeight + FRAME_BOTTOM * s - aTop);
            if (mList.mScale != s)
            {
                mList.SetScale(s);
            }
            mTitleProxy.Resize(0, 0, mWidth, mHeight);
            mScroll.Resize(0, LIST_TOP * s, aWidth, mHeight - LIST_TOP * s);
            mScroll.SetIndicatorsInsets(new Insets(2 * s, 2 * s, 4 * s, 2 * s));
            mScroll.FlashIndicators();
            SetupSlide(SLIDE_FROM_BOTTOM, 0.5f);
        }

        /// <summary>InterfaceTransactionEnd (0x43388): in place in state 8, Lex presents the blurb.</summary>
        public override void InterfaceStateDone()
        {
            base.InterfaceStateDone();
            if (mInterfaceState == 8 && mInterfaceTarget != 0)
            {
                mApp.mWordBalloon.PresentMoreGames(mLexText);
            }
        }

        /// <summary>DrawProxyWidget (0x433c4): the title at MORE_GAMES_TITLE_POS.</summary>
        public void DrawProxyWidget(Graphics g, ProxyWidget theProxy)
        {
            if (mTitleImage == null)
            {
                return;
            }
            int s = LayoutScale();
            g.DrawImage(mTitleImage, new TRect(0, 0, mTitleImage.mWidth * s, mTitleImage.mHeight * s),
                new TRect(0, 0, mTitleImage.mWidth, mTitleImage.mHeight));
        }

        /// <summary>DialogButtonDepress (0x433e8): a tray of the list (id 0) opens its game's link.</summary>
        public void DialogButtonDepress(int theDialogId, int theButtonId)
        {
            if (theDialogId == 0)
            {
                mApp.OpenURL(mList.GetLinkForRow(theButtonId));
            }
        }

        public void DialogButtonPress(int theDialogId, int theButtonId)
        {
        }
    }
}
