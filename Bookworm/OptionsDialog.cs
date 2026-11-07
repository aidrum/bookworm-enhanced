using Microsoft.Xna.Framework;

namespace Sexy
{
    /// <summary>
    /// Sexy::OptionsDialog (0x1fe80, screen 13, state 7): the options page of the book: sound effect and
    /// music volume sliders, switches for hints, definitions and the scramble confirmation, and the version.
    /// Slides up from the bottom.
    /// </summary>
    public class OptionsDialog : BookWidget, SliderListener, SwitchListener, ButtonListener
    {
        public const int SLIDER_SFX = 0;

        public const int SLIDER_MUSIC = 1;

        public const int SWITCH_HINTS = 2;

        public const int SWITCH_DEFINITIONS = 3;

        public const int SWITCH_CONFIRM_SCRAMBLE = 4;

        /// <summary>Port: the language link.</summary>
        public const int LINK_LANGUAGE = 5;

        /// <summary>Port: the link between the options and the extra options.</summary>
        public const int LINK_PAGE = 6;

        /// <summary>Port: the extra options' landscape books switch.</summary>
        public const int SWITCH_LANDSCAPE_BOOKS = 7;

        /// <summary>Port: the extra options' texture quality (a tap picks the next).</summary>
        public const int LINK_TEXTURES = 8;

        /// <summary>Port: the extra options' Submit by Dragging switch (BookWormApp.mSubmitByDragging).</summary>
        public const int SWITCH_SUBMIT_BY_DRAGGING = 9;

        /// <summary>Port: the extra options' Check for Updates switch (BookWormApp.mCheckForUpdates, UpdateChecker).</summary>
        public const int SWITCH_CHECK_FOR_UPDATES = 10;

        /// <summary>Port: the extra options' UI layout (a tap picks the next: Auto, iPhone, iPad).</summary>
        public const int LINK_LAYOUT = 11;

        /// <summary>Port: the UI layout names (Constants.LAYOUT_MODE_*).</summary>
        private static readonly string[] LAYOUT_NAMES = { "Auto", "iPhone", "iPad" };

        /// <summary>Port: the texture quality names, Low to High (Constants.TEXTURE_SIDES).</summary>
        private static readonly string[] TEXTURE_NAMES = { "Low", "Medium", "High" };

        /// <summary>The option labels colour (header static 0x135948).</summary>
        private static readonly Color LABEL_COLOR = new Color(255, 255, 255, 255);

        public LabelWidget mSfxLabel; // 0x100

        public LabelWidget mMusicLabel; // 0x104

        public LabelWidget mHintsLabel; // 0x108

        public LabelWidget mDefinitionsLabel; // 0x10c

        public LabelWidget mConfirmScrambleLabel; // 0x114

        public LabelWidget mVersionLabel; // 0x118

        public Slider mSfxSlider; // 0x120

        public Slider mMusicSlider; // 0x124

        public Switch mHintsSwitch; // 0x128

        public Switch mDefinitionsSwitch; // 0x12c

        public Switch mConfirmScrambleSwitch; // 0x130

        /// <summary>Port addition: the language's name at the page's bottom left (as in Peggle Enhanced); a tap switches it.</summary>
        public HyperlinkWidget mLanguageLink;

        /// <summary>Port: the link at the bottom middle, to the extra options and back.</summary>
        public HyperlinkWidget mPageLink;

        /// <summary>Port: true while the extra options page shows instead of the options.</summary>
        public bool mExtraPage;

        public LabelWidget mLandscapeBooksLabel;

        public Switch mLandscapeBooksSwitch;

        public LabelWidget mSubmitByDraggingLabel;

        public Switch mSubmitByDraggingSwitch;

        public LabelWidget mCheckForUpdatesLabel;

        public Switch mCheckForUpdatesSwitch;

        public LabelWidget mLayoutLabel;

        public BarButton mLayoutButton;

        public LabelWidget mTexturesLabel;

        public BarButton mTexturesButton;

        /// <summary>Port: shown while the texture quality picked differs from the one loaded.</summary>
        public LabelWidget mRestartLabel;

        /// <summary>Port: the texture set picked (Constants.TEXTURE_SIDES), kept for the next launch.</summary>
        private int mTextureSide;

        /// <summary>How many times hints were switched on while this page was up (0x138).</summary>
        public int mHintsOnCount;

        public OptionsDialog(BookWormApp theApp) : base(theApp)
        {
            mHasAlpha = true;
            mSfxLabel = MakeLabel("");
            mMusicLabel = MakeLabel("");
            mHintsLabel = MakeLabel("");
            mDefinitionsLabel = MakeLabel("");
            mConfirmScrambleLabel = MakeLabel("");
            mSfxSlider = MakeSlider(SLIDER_SFX);
            mMusicSlider = MakeSlider(SLIDER_MUSIC);
            mHintsSwitch = MakeSwitch(SWITCH_HINTS);
            mDefinitionsSwitch = MakeSwitch(SWITCH_DEFINITIONS);
            mConfirmScrambleSwitch = MakeSwitch(SWITCH_CONFIRM_SCRAMBLE);
            mVersionLabel = new LabelWidget(theApp.mVersion, Res.FONT_DEFINITIONS_SMALL);
            mVersionLabel.SizeToText();
            mVersionLabel.SetColor(0, new Color(0, 0, 0, 255));
            AddWidget(mVersionLabel);
            mLanguageLink = new HyperlinkWidget(LINK_LANGUAGE, this);
            mLanguageLink.SetFont(Res.FONT_DEFINITIONS_SMALL);
            mLanguageLink.mColor = new Color(0, 0, 0, 255);
            mLanguageLink.mOverColor = new Color(0, 0x5c, 0x09, 255);
            SizeUnderline(mLanguageLink);
            AddWidget(mLanguageLink);
            mPageLink = MakeLink(LINK_PAGE, Res.FONT_DEFINITIONS_SMALL, new Color(0, 0, 0, 255), new Color(0, 0x5c, 0x09, 255));
            WidenTouchArea(mLanguageLink);
            WidenTouchArea(mPageLink);
            mLandscapeBooksLabel = MakeLabel("");
            mLandscapeBooksSwitch = MakeSwitch(SWITCH_LANDSCAPE_BOOKS);
            mSubmitByDraggingLabel = MakeLabel("");
            mSubmitByDraggingSwitch = MakeSwitch(SWITCH_SUBMIT_BY_DRAGGING);
            mCheckForUpdatesLabel = MakeLabel("");
            mCheckForUpdatesSwitch = MakeSwitch(SWITCH_CHECK_FOR_UPDATES);
            mLayoutLabel = MakeLabel("");
            mLayoutButton = new BarButton(LINK_LAYOUT, this, theApp, AtlasResourceBase.IMAGE_GROUP1_20, AtlasResourceBase.IMAGE_GROUP1_22);
            mLayoutButton.SetFont(Res.FONT_ARIAL_LARGE);
            AddWidget(mLayoutButton);
            mTexturesLabel = MakeLabel("");
            // Port: drawn as a switch's ON track, its label in the switches' ON / OFF lettering.
            mTexturesButton = new BarButton(LINK_TEXTURES, this, theApp, AtlasResourceBase.IMAGE_GROUP1_20, AtlasResourceBase.IMAGE_GROUP1_22);
            mTexturesButton.SetFont(Res.FONT_ARIAL_LARGE);
            AddWidget(mTexturesButton);
            mRestartLabel = new LabelWidget("", Res.FONT_DEFINITIONS_SMALL);
            mRestartLabel.mClip = false;
            mRestartLabel.SetColor(0, new Color(0, 0, 0, 255));
            AddWidget(mRestartLabel);
            mTextureSide = Constants.ReadSavedTextureSide();
            if (mTextureSide == 0)
            {
                mTextureSide = Constants.TextureSide;
            }
            SetTexts();
        }

        /// <summary>Port: the labels and the language link in the current language.</summary>
        private void SetTexts()
        {
            SetLabel(mSfxLabel, "Sound Effects");
            SetLabel(mMusicLabel, "Music Tracks");
            SetLabel(mHintsLabel, "Show Hints");
            SetLabel(mDefinitionsLabel, "Show Definitions");
            SetLabel(mConfirmScrambleLabel, "Confirm to Scramble");
            mLanguageLink.mLabel = Localization.LANGUAGE_NAMES[Localization.CurrentIndex()];
            SizeLink(mLanguageLink);
            SetLabel(mLandscapeBooksLabel, "Landscape Books");
            SetLabel(mSubmitByDraggingLabel, "Submit by Dragging");
            SetLabel(mCheckForUpdatesLabel, "Check for Updates");
            // Port: the switches' ON / OFF art is English; translated, their names are drawn as text.
            foreach (Switch aSwitch in new[] { mHintsSwitch, mDefinitionsSwitch, mConfirmScrambleSwitch, mLandscapeBooksSwitch, mSubmitByDraggingSwitch, mCheckForUpdatesSwitch })
            {
                aSwitch.mOnText = Localization.IsSpanish ? "SÍ" : null;
                aSwitch.mOffText = Localization.IsSpanish ? "NO" : null;
                aSwitch.MarkDirty();
            }
            SetLabel(mTexturesLabel, "Texture Quality");
            SetLabel(mRestartLabel, "Restart the game to apply");
            mPageLink.mLabel = SexyAppBase.Loc(mExtraPage ? "Back" : "More Options");
            SizeLink(mPageLink);
            int anIndex = System.Math.Max(0, System.Array.IndexOf(Constants.TEXTURE_SIDES, mTextureSide));
            mTexturesButton.mLabel = SexyAppBase.Loc(TEXTURE_NAMES[anIndex]);
            mTexturesButton.SizeToLabel();
            SetLabel(mLayoutLabel, "UI Layout");
            // Low quality (the iPhone textures) only has the iPhone layout: shown faded, and a tap does nothing.
            bool aFixed = LayoutFixed();
            mLayoutButton.mLabel = SexyAppBase.Loc(LAYOUT_NAMES[aFixed ? Constants.LAYOUT_MODE_IPHONE : Constants.LayoutMode]);
            mLayoutButton.mAlpha = aFixed ? 0x80 : 0xff;
            mLayoutButton.SetDisabled(aFixed);
            mLayoutButton.SizeToLabel();
        }

        /// <summary>Port: true while the game runs with the Low quality's iPhone textures (loaded at launch, not
        /// just picked), which always get the iPhone layout.</summary>
        private bool LayoutFixed()
        {
            return Constants.TextureSide == 320 || Constants.TextureSide == 640;
        }

        /// <summary>
        /// Port: the links' underline scales with their font (1 pixel a step of 14 of its ascent, the gap below the
        /// baseline the same): a fixed 1-pixel line vanished where the screen is drawn scaled down.
        /// </summary>
        private static void SizeUnderline(HyperlinkWidget theLink)
        {
            int aStep = System.Math.Max(1, (int)System.Math.Round(theLink.mFont.GetAscent() / 14.0));
            theLink.mUnderlineSize = aStep;
            theLink.mUnderlineOffset = aStep;
            // The underline reaches below the font's height: not clipped by the link's rect.
            theLink.mClip = false;
        }

        /// <summary>
        /// Port: the small links are hard to hit with a finger: they take clicks a font height to each side and
        /// three quarters of one above and below their text (negative mouse insets reach past the widget's rect).
        /// </summary>
        private static void WidenTouchArea(HyperlinkWidget theLink)
        {
            int aHeight = theLink.mFont.GetHeight();
            int aVertical = aHeight * 3 / 4;
            theLink.mMouseInsets.mLeft = -aHeight;
            theLink.mMouseInsets.mRight = -aHeight;
            theLink.mMouseInsets.mTop = -aVertical;
            theLink.mMouseInsets.mBottom = -aVertical;
        }

        private static void SizeLink(HyperlinkWidget theLink)
        {
            Font aFont = theLink.mFont;
            theLink.Resize(theLink.mX, theLink.mY, aFont.StringWidth(theLink.mLabel), aFont.GetHeight());
        }

        private HyperlinkWidget MakeLink(int theId, Font theFont, Color theColor, Color theOverColor)
        {
            HyperlinkWidget aLink = new HyperlinkWidget(theId, this);
            aLink.SetFont(theFont);
            aLink.mColor = theColor;
            aLink.mOverColor = theOverColor;
            SizeUnderline(aLink);
            // The outlined fonts reach past the text's width and height.
            aLink.mClip = false;
            AddWidget(aLink);
            return aLink;
        }

        private static void SetLabel(LabelWidget theLabel, string theText)
        {
            theLabel.mText = SexyAppBase.Loc(theText);
            theLabel.SizeToText();
        }

        /// <summary>FUN_0002070c.</summary>
        private LabelWidget MakeLabel(string theText)
        {
            LabelWidget aLabel = new LabelWidget(theText, mHeadingFont);
            aLabel.SetInsets(new TRect(0, 0, 0, 0));
            aLabel.SizeToText();
            aLabel.mClip = false;
            aLabel.SetColor(0, LABEL_COLOR);
            AddWidget(aLabel);
            return aLabel;
        }

        private Slider MakeSlider(int theId)
        {
            Slider aSlider = new Slider(theId, this, AtlasResourceBase.IMAGE_GROUP11_20, AtlasResourceBase.IMAGE_GROUP11_19, AtlasResourceBase.IMAGE_GROUP11_18);
            aSlider.Resize(0, 0, 0, AtlasResourceBase.IMAGE_GROUP11_18.mHeight);
            AddWidget(aSlider);
            return aSlider;
        }

        /// <summary>FUN_00044cc0.</summary>
        private Switch MakeSwitch(int theId)
        {
            Switch aSwitch = new Switch(theId, this, AtlasResourceBase.IMAGE_GROUP1_19, AtlasResourceBase.IMAGE_GROUP1_22,
                AtlasResourceBase.IMAGE_GROUP1_20, AtlasResourceBase.IMAGE_GROUP1_23, AtlasResourceBase.IMAGE_GROUP1_21, mApp.mScreenPos.RectAt(0x340));
            aSwitch.mTextFont = Res.FONT_ARIAL_LARGE;
            AddWidget(aSwitch);
            return aSwitch;
        }

        /// <summary>vf82 (0x20be8): the controls show the current settings; placed on the book, slides up.</summary>
        public override void AttachToManager(int theState, int theTarget, int theParam)
        {
            base.AttachToManager(theState, theTarget, theParam);
            mSfxSlider.SetValue(mApp.GetSfxVolume());
            mMusicSlider.SetValue(mApp.GetMusicVolume());
            mHintsSwitch.SetOn(mApp.mShowHints, false);
            mDefinitionsSwitch.SetOn(mApp.mShowDefinitions, false);
            mConfirmScrambleSwitch.SetOn(mApp.mConfirmScramble, false);
            mLandscapeBooksSwitch.SetOn(mApp.mLandscapeBooks, false);
            mSubmitByDraggingSwitch.SetOn(mApp.mSubmitByDragging, false);
            mCheckForUpdatesSwitch.SetOn(mApp.mCheckForUpdates, false);
            mHintsOnCount = 0;
            mExtraPage = false;
            SetTexts();
            PlaceIn(mApp.mScreenPos.RectAt(0x1f0));
            LayoutControls();
            SetupSlide(SLIDE_FROM_BOTTOM, 0.5f);
        }

        /// <summary>Port: laid out again for a new layout (AttachToManager), it stays on the page it was showing.</summary>
        public override void Relayout()
        {
            bool anExtra = mExtraPage;
            int aHintsOnCount = mHintsOnCount;
            base.Relayout();
            mHintsOnCount = aHintsOnCount;
            if (anExtra && !mExtraPage)
            {
                ShowPage(true);
            }
        }

        /// <summary>FUN_000209fc: the sliders under their labels, the switches down the right, their labels beside them.</summary>
        private void LayoutControls()
        {
            int aPad = mApp.mScreenPos.At(0x1b8);
            int aGap = mApp.mScreenPos.At(0x1bc);
            TRect aRect = mPageRect;
            aRect.mX += aPad;
            aRect.mWidth += aPad;
            mSfxLabel.Layout(0x70, this, aRect.mX, aRect.mY, mWidth - (aRect.mWidth + aRect.mX), 0);
            mSfxSlider.Layout(0x1201, mSfxLabel, 0, aGap, 0, 0);
            mMusicLabel.Layout(0x1201, mSfxSlider, 0, aGap, 0, 0);
            mMusicSlider.Layout(0x1201, mMusicLabel, 0, aGap, 0, 0);
            mHintsSwitch.Layout(0x2200, mMusicSlider, 0, aGap, 0, 0);
            mDefinitionsSwitch.Layout(0x2200, mHintsSwitch, 0, aGap, 0, 0);
            // Port: the PC Spanish version has no definitions (HAS_DEFINITIONS false), so neither has its switch.
            bool aDefinitions = !Localization.IsSpanish;
            mDefinitionsSwitch.SetVisible(aDefinitions);
            mDefinitionsLabel.SetVisible(aDefinitions);
            mConfirmScrambleSwitch.Layout(0x2200, aDefinitions ? mDefinitionsSwitch : mHintsSwitch, 0, aGap, 0, 0);
            mHintsLabel.Layout(0x200800, mHintsSwitch, -aPad, 0, 0, 0);
            mDefinitionsLabel.Layout(0x200800, mDefinitionsSwitch, -aPad, 0, 0, 0);
            mConfirmScrambleLabel.Layout(0x200800, mConfirmScrambleSwitch, -aPad, 0, 0, 0);
            mSfxLabel.mJustifyH = 0;
            mMusicLabel.mJustifyH = 0;
            mHintsLabel.mJustifyH = 1;
            mDefinitionsLabel.mJustifyH = 1;
            mConfirmScrambleLabel.mJustifyH = 1;
            mVersionLabel.Layout(0xa000, this, -aRect.mWidth, 5 - aRect.mHeight, 0, 0);
            // Port: the language link at the bottom left, level with the version.
            mLanguageLink.Layout(0x9000, this, aRect.mX, 5 - aRect.mHeight, 0, 0);
            // Port: the extra options link at the bottom middle; the extra options down the right as the switches,
            // from the top (the landscape books switch only where the device turns: phones and tablets).
            mPageLink.Layout(0x108000, this, (aRect.mX - aRect.mWidth) / 2, 5 - aRect.mHeight, 0, 0);
            bool aLandscapeBooks = Main.IsMobile;
            mLandscapeBooksSwitch.Layout(0x6000, mSfxLabel, 0, 0, 0, 0);
            mLandscapeBooksLabel.Layout(0x200800, mLandscapeBooksSwitch, -aPad, 0, 0, 0);
            if (aLandscapeBooks)
            {
                mSubmitByDraggingSwitch.Layout(0x2200, mLandscapeBooksSwitch, 0, aGap, 0, 0);
            }
            else
            {
                mSubmitByDraggingSwitch.Layout(0x6000, mSfxLabel, 0, 0, 0, 0);
            }
            mSubmitByDraggingLabel.Layout(0x200800, mSubmitByDraggingSwitch, -aPad, 0, 0, 0);
            mCheckForUpdatesSwitch.Layout(0x2200, mSubmitByDraggingSwitch, 0, aGap, 0, 0);
            mCheckForUpdatesLabel.Layout(0x200800, mCheckForUpdatesSwitch, -aPad, 0, 0, 0);
            mLayoutButton.Layout(0x2200, mCheckForUpdatesSwitch, 0, aGap, 0, 0);
            mLayoutLabel.Layout(0x200800, mLayoutButton, -aPad, 0, 0, 0);
            mLayoutLabel.mJustifyH = 1;
            mTexturesButton.Layout(0x2200, mLayoutButton, 0, aGap, 0, 0);
            mTexturesLabel.Layout(0x200800, mTexturesButton, -aPad, 0, 0, 0);
            mRestartLabel.Layout(0x2200, mTexturesButton, 0, aGap, 0, 0);
            mLandscapeBooksLabel.mJustifyH = 1;
            mSubmitByDraggingLabel.mJustifyH = 1;
            mCheckForUpdatesLabel.mJustifyH = 1;
            mTexturesLabel.mJustifyH = 1;
            // One page at a time.
            bool aMain = !mExtraPage;
            mSfxLabel.SetVisible(aMain);
            mSfxSlider.SetVisible(aMain);
            mMusicLabel.SetVisible(aMain);
            mMusicSlider.SetVisible(aMain);
            mHintsLabel.SetVisible(aMain);
            mHintsSwitch.SetVisible(aMain);
            mDefinitionsSwitch.SetVisible(aMain && aDefinitions);
            mDefinitionsLabel.SetVisible(aMain && aDefinitions);
            mConfirmScrambleLabel.SetVisible(aMain);
            mConfirmScrambleSwitch.SetVisible(aMain);
            mLandscapeBooksLabel.SetVisible(mExtraPage && aLandscapeBooks);
            mLandscapeBooksSwitch.SetVisible(mExtraPage && aLandscapeBooks);
            mSubmitByDraggingLabel.SetVisible(mExtraPage);
            mSubmitByDraggingSwitch.SetVisible(mExtraPage);
            mCheckForUpdatesLabel.SetVisible(mExtraPage);
            mCheckForUpdatesSwitch.SetVisible(mExtraPage);
            mLayoutLabel.SetVisible(mExtraPage);
            mLayoutButton.SetVisible(mExtraPage);
            mTexturesLabel.SetVisible(mExtraPage);
            mTexturesButton.SetVisible(mExtraPage);
            mRestartLabel.SetVisible(mExtraPage && mTextureSide != Constants.TextureSide);
        }

        /// <summary>Port: shows the options or the extra options.</summary>
        private void ShowPage(bool theExtra)
        {
            mExtraPage = theExtra;
            SetTexts();
            LayoutControls();
            MarkDirty();
        }

        /// <summary>Port (the back button): the extra options go back to the options.</summary>
        public override bool BackButtonPress()
        {
            if (mExtraPage && mApp.mInterfaceState == 7)
            {
                mApp.PlaySound(Res.SOUND_BUTTON_CLICK);
                ShowPage(false);
                return true;
            }
            return false;
        }

        /// <summary>
        /// vf94 (0x20cd8): the volumes follow the sliders; the effects slider plays a sample when let go. The
        /// settings are saved once a slider is let go. (iOS: raising the music while the iPod plays turned it
        /// off first, FUN_000a2fc0; not needed here.)
        /// </summary>
        public void SliderVal(int theId, double theVal)
        {
            Slider aSlider = theId == SLIDER_MUSIC ? mMusicSlider : mSfxSlider;
            if (theId == SLIDER_MUSIC)
            {
                mApp.SetMusicVolume(theVal);
            }
            else if (theId == SLIDER_SFX)
            {
                mApp.SetSfxVolume(theVal);
                if (!aSlider.mDragging)
                {
                    mApp.PlaySound(Res.SOUND_SUPER_BONUS_TILE);
                }
            }
            if (!aSlider.mDragging)
            {
                mApp.SaveSettings();
            }
        }

        /// <summary>
        /// vf95 (0x20df8): the settings follow the switches. Switching hints on starts their tutorial over (the
        /// first time the first steps, the second time the later ones).
        /// </summary>
        public void SwitchChanged(int theId, bool theOn)
        {
            mApp.PlaySound(Res.SOUND_BUTTON_CLICK);
            switch (theId)
            {
            case SWITCH_HINTS:
                if (theOn)
                {
                    int aCount = mHintsOnCount++;
                    if (aCount == 0)
                    {
                        System.Array.Clear(mApp.mHintsSeen);
                    }
                    else if (mHintsOnCount == 2)
                    {
                        mApp.mHintTutorialShown = false;
                        mApp.mHintTutorialStep2 = 0;
                    }
                }
                mApp.mShowHints = theOn;
                break;
            case SWITCH_DEFINITIONS:
                mApp.mShowDefinitions = theOn;
                break;
            case SWITCH_CONFIRM_SCRAMBLE:
                mApp.mConfirmScramble = theOn;
                break;
            case SWITCH_LANDSCAPE_BOOKS:
                mApp.mLandscapeBooks = theOn;
                break;
            case SWITCH_SUBMIT_BY_DRAGGING:
                mApp.mSubmitByDragging = theOn;
                break;
            case SWITCH_CHECK_FOR_UPDATES:
                mApp.mCheckForUpdates = theOn;
                // Turned on, the check runs now (once per launch); the prompt waits for the main menu.
                if (theOn)
                {
                    UpdateChecker.Start();
                }
                break;
            default:
                return;
            }
            mApp.SaveSettings();
        }

        /// <summary>Port: the language link switches the language; the page shows it at once.</summary>
        public void ButtonDepress(int theId)
        {
            if (theId == LINK_PAGE)
            {
                mApp.PlaySound(Res.SOUND_BUTTON_CLICK);
                ShowPage(!mExtraPage);
                return;
            }
            if (theId == LINK_TEXTURES)
            {
                // The next quality, Low after High; kept for the next launch (the textures are loaded once).
                mApp.PlaySound(Res.SOUND_BUTTON_CLICK);
                int anIndex = System.Array.IndexOf(Constants.TEXTURE_SIDES, mTextureSide);
                mTextureSide = Constants.TEXTURE_SIDES[(anIndex + 1) % Constants.TEXTURE_SIDES.Length];
                Constants.SaveTextureSide(mTextureSide);
                ShowPage(true);
                return;
            }
            if (theId == LINK_LAYOUT)
            {
                if (LayoutFixed())
                {
                    return;
                }
                // Auto or iPad (the iPhone layout is only Low quality's); the game takes it at once and keeps it.
                mApp.PlaySound(Res.SOUND_BUTTON_CLICK);
                Constants.SaveLayoutMode(Constants.LayoutMode == Constants.LAYOUT_MODE_IPAD ? Constants.LAYOUT_MODE_AUTO : Constants.LAYOUT_MODE_IPAD);
                SexyAppBase.XnaGame.RequestRelayout();
                ShowPage(true);
                return;
            }
            if (theId != LINK_LANGUAGE)
            {
                return;
            }
            mApp.PlaySound(Res.SOUND_BUTTON_CLICK);
            mApp.ChangeLanguage();
            // Port: Lex's "OPTIONS!" balloon says it again in the new language (fading the old text out).
            if (mApp.mWordBalloon.GetMode() == WordBalloon.MODE_EXCLAIM)
            {
                mApp.mWordBalloon.Say(SexyAppBase.Loc("OPTIONS!"));
            }
            SetTexts();
            LayoutControls();
            MarkDirty();
        }

        public void ButtonPress(int theId)
        {
        }

        public void ButtonPress(int theId, int theClickCount)
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

    /// <summary>
    /// Port addition: a button on one of the dialogs' bars (green or red), sized to its label: the bar's ends
    /// keep their shape (scaled to the button's height) and its middle stretches.
    /// </summary>
    /// <summary>
    /// Port: a button drawn as a switch's green ON track (a little less tall than a switch, its rounded ends
    /// kept, stretched to the label), its label in uppercase in the switches' ON / OFF lettering.
    /// </summary>
    public class BarButton : BookWormButton
    {
        /// <summary>The track's height against a switch's (the art's own height).</summary>
        private const float SQUISH = 1f;

        private static readonly Color TEXT_COLOR = new Color(255, 255, 255, 255);

        private static readonly Color PRESSED_COLOR = new Color(190, 190, 190, 255);

        private readonly Image mBar;

        private readonly Image mLabelArt;

        /// <summary>Port: the track's and the label's opacity (0x80 for an option that can't change now).</summary>
        public int mAlpha = 0xff;

        /// <summary>theBar the switch's ON track; theLabelArt its OFF label art (for the lettering's size).</summary>
        public BarButton(int theId, ButtonListener theListener, BookWormApp theApp, Image theBar, Image theLabelArt) : base(theId, theListener, theApp)
        {
            mBar = theBar;
            mLabelArt = theLabelArt;
            mClip = false;
        }

        private float CapHeight()
        {
            return Switch.LabelCapHeight(mLabelArt) * SQUISH;
        }

        /// <summary>The label with the track's rounded ends (half its height each) and as much again around it.</summary>
        public void SizeToLabel()
        {
            int aHeight = (int)System.Math.Round(mBar.mHeight * SQUISH);
            int aWidth = (int)System.Math.Ceiling(Switch.CapTextWidth(mFont, mLabel.ToUpperInvariant(), CapHeight())) + aHeight * 3 / 2;
            aWidth = System.Math.Max(aWidth, aHeight * 2);
            Resize(mX + mWidth - aWidth, mY, aWidth, aHeight);
        }

        public override void Draw(Graphics g)
        {
            if (mBtnNoDraw || mBar == null)
            {
                return;
            }
            bool aPressed = mIsOver && mIsDown && !mDisabled;
            Color aTint = aPressed ? PRESSED_COLOR : TEXT_COLOR;
            aTint.A = (byte)mAlpha;
            if (aPressed || mAlpha < 0xff)
            {
                g.SetColorizeImages(true);
                g.SetColor(aTint);
            }
            // The ends (half the art's height, its rounded caps) scaled with the height; the middle stretched.
            int anEnd = mBar.mHeight / 2;
            int aDestEnd = System.Math.Min(mHeight / 2, mWidth / 2);
            g.DrawImage(mBar, new TRect(0, 0, aDestEnd, mHeight), new TRect(0, 0, anEnd, mBar.mHeight));
            g.DrawImage(mBar, new TRect(aDestEnd, 0, mWidth - 2 * aDestEnd, mHeight), new TRect(anEnd, 0, mBar.mWidth - 2 * anEnd, mBar.mHeight));
            g.DrawImage(mBar, new TRect(mWidth - aDestEnd, 0, aDestEnd, mHeight), new TRect(mBar.mWidth - anEnd, 0, anEnd, mBar.mHeight));
            g.SetColorizeImages(false);
            if (mFont != null && !string.IsNullOrEmpty(mLabel))
            {
                Switch.DrawCapText(g, mFont, mLabel.ToUpperInvariant(), mWidth / 2f, mHeight / 2f, CapHeight(), aTint);
            }
        }
    }
}
