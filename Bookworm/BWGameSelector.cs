using Microsoft.Xna.Framework;

namespace Sexy
{
    /// <summary>
    /// Sexy::BWGameSelector (0x16a84, screen 8): the main menu's panel with Classic Game, Arcade Game and How
    /// to Play, plus a "Saved Game" ribbon on each mode that has a game to continue. Slides in from the right.
    /// </summary>
    public class BWGameSelector : SlideInWidget, ButtonListener, DialogListener
    {
        public const int BUTTON_CLASSIC = 0;

        public const int BUTTON_ARCADE = 1;

        public const int BUTTON_CONTINUE_CLASSIC = 4;

        public const int BUTTON_CONTINUE_ARCADE = 5;

        public const int BUTTON_HOW_TO_PLAY = 6;

        /// <summary>The action sheet asking "Start New Game" / "Continue Game".</summary>
        public const int DIALOG_NEW_OR_CONTINUE = 7;

        /// <summary>The action sheet confirming "Discard Saved Game".</summary>
        public const int DIALOG_DISCARD = 8;

        public Image mImage; // 0xe0

        public BookWormButton mContinueClassic; // 0xe4

        public BookWormButton mContinueArcade; // 0xe8

        public BWGameSelector(BookWormApp theApp) : base(theApp)
        {
            mImage = AtlasResourceBase.IMAGE_GROUP1_00;
            mHasAlpha = true;
            // The buttons and their places (ScreenPos + 0x10 ... 0x30).
            mClassic = (TextOrImageButton)AddButton(BUTTON_CLASSIC, AtlasResourceBase.IMAGE_GROUP11_04, 0x10);
            mArcade = (TextOrImageButton)AddButton(BUTTON_ARCADE, AtlasResourceBase.IMAGE_GROUP11_05, 0x20);
            mHowToPlay = (TextOrImageButton)AddButton(BUTTON_HOW_TO_PLAY, AtlasResourceBase.IMAGE_GROUP11_15, 0x30);
            mContinueClassic = AddButton(BUTTON_CONTINUE_CLASSIC, AtlasResourceBase.IMAGE_GROUP11_16, 0x18);
            mContinueArcade = AddButton(BUTTON_CONTINUE_ARCADE, AtlasResourceBase.IMAGE_GROUP11_16, 0x28);
        }

        /// <summary>Port (translated): the three mode buttons, whose English titles are baked into their images.</summary>
        private TextOrImageButton mClassic;

        private TextOrImageButton mArcade;

        private TextOrImageButton mHowToPlay;

        /// <summary>Port: the art's gradients, top to bottom (sampled from it): yellow (Classic, Arcade), pink (How to Play).</summary>
        internal static readonly Color MODE_TEXT_TOP = new Color(254, 250, 20, 255);

        internal static readonly Color MODE_TEXT_BOTTOM = new Color(251, 210, 5, 255);

        internal static readonly Color HOW_TO_PLAY_TEXT_TOP = new Color(238, 198, 224, 255);

        internal static readonly Color HOW_TO_PLAY_TEXT_BOTTOM = new Color(180, 67, 117, 255);

        /// <summary>Port: the "Saved Game" ribbon's yellow, top to bottom (sampled from the art).</summary>
        internal static readonly Color RIBBON_TEXT_TOP = new Color(255, 246, 80, 255);

        internal static readonly Color RIBBON_TEXT_BOTTOM = new Color(238, 200, 30, 255);

        /// <summary>
        /// Port: translated, the mode buttons draw their titles in outlined Cooper Black (the art's typeface), two
        /// lines like the art for the modes (worded as the PC version's mode art), and the Continue ribbons are the
        /// ribbon without its "Saved Game" (localized sheet) with the language's words drawn over it; in English,
        /// the original images.
        /// </summary>
        private void ApplyLanguage()
        {
            bool aText = Localization.IsTranslated;
            mClassic.SetText(aText ? SexyAppBase.Loc("CLASSIC\nGAME") : null, MODE_TEXT_TOP, MODE_TEXT_BOTTOM);
            mArcade.SetText(aText ? SexyAppBase.Loc("ACTION\nGAME") : null, MODE_TEXT_TOP, MODE_TEXT_BOTTOM);
            mHowToPlay.SetText(aText ? SexyAppBase.Loc("HOW TO PLAY") : null, HOW_TO_PLAY_TEXT_TOP, HOW_TO_PLAY_TEXT_BOTTOM);
            Image aRibbon = AtlasResourceBase.IMAGE_GROUP11_16;
            Image aBlank = aText ? AtlasResourceBase.GetLocalizedImage(mApp, aRibbon.mWidth / 47, AtlasResourceBase.LOCALIZED_BLANK_RIBBON) : null;
            foreach (BookWormButton aButton in new[] { mContinueClassic, mContinueArcade })
            {
                TextOrImageButton aContinue = (TextOrImageButton)aButton;
                aContinue.mButtonImage = aBlank ?? aRibbon;
                aContinue.SetRibbonText(aBlank != null ? SexyAppBase.Loc("Saved\nGame") : null, RIBBON_TEXT_TOP, RIBBON_TEXT_BOTTOM);
            }
        }

        private BookWormButton AddButton(int theId, Image theImage, int thePosOffset)
        {
            BookWormButton aButton = new TextOrImageButton(theId, this, mApp);
            aButton.Resize(mApp.mScreenPos.At(thePosOffset), mApp.mScreenPos.At(thePosOffset + 4), theImage.mWidth, theImage.mHeight);
            aButton.mButtonImage = theImage;
            AddWidget(aButton);
            return aButton;
        }

        /// <summary>vf82 (0x16fc4): placed at ScreenPos + 0x08 to slide in from the right; ribbons on the saved modes.</summary>
        public override void AttachToManager(int theState, int theTarget, int theParam)
        {
            base.AttachToManager(theState, theTarget, theParam);
            Resize(mApp.mScreenPos.At(0x08), mApp.mScreenPos.At(0x0c), mImage.mWidth, mImage.mHeight);
            SetupSlide(SLIDE_FROM_RIGHT, 0.5f);
            ApplyLanguage();
            mContinueClassic.SetVisible(mApp.HasSavedGame(BookWormApp.GAME_CLASSIC));
            mContinueArcade.SetVisible(mApp.HasSavedGame(BookWormApp.GAME_ARCADE));
        }

        /// <summary>0x1703c: 100 frames after the menu first appears, Lex welcomes the player (once per run).</summary>
        public override void Update()
        {
            base.Update();
            if (mUpdateCnt == 100 && !mApp.mWelcomePlayed)
            {
                mApp.mWelcomePlayed = true;
                mApp.PlaySound(Res.SOUND_LEX_WELCOMETOBOOKWORM);
                mApp.mLex.PlayAnim(Lex.ANIM_WELCOME, false);
            }
        }

        /// <summary>0x17084: the panel under the buttons.</summary>
        public override void Draw(Graphics g)
        {
            g.DrawImage(mImage, 0, 0);
            base.Draw(g);
        }

        /// <summary>vf94 (0x170a4).</summary>
        public void ButtonPress(int theId)
        {
            mApp.PlaySound(Res.SOUND_BUTTON_CLICK);
        }

        public void ButtonPress(int theId, int theClickCount)
        {
            ButtonPress(theId);
        }

        /// <summary>
        /// vf95 (0x170dc): a mode with a saved game asks whether to start anew or continue (an ActionSheet,
        /// FUN_00017144); a ribbon continues; How to Play opens state 9.
        /// </summary>
        public void ButtonDepress(int theId)
        {
            switch (theId)
            {
            case BUTTON_CLASSIC:
                if (mApp.HasSavedGame(BookWormApp.GAME_CLASSIC))
                {
                    ShowNewOrContinue(2, 4);
                    return;
                }
                mApp.StartGame(BookWormApp.GAME_CLASSIC);
                return;
            case BUTTON_ARCADE:
                if (mApp.HasSavedGame(BookWormApp.GAME_ARCADE))
                {
                    ShowNewOrContinue(3, 5);
                    return;
                }
                mApp.StartGame(BookWormApp.GAME_ARCADE);
                return;
            case BUTTON_CONTINUE_CLASSIC:
                mApp.StartGame(BookWormApp.GAME_CLASSIC);
                return;
            case BUTTON_CONTINUE_ARCADE:
                mApp.StartGame(BookWormApp.GAME_ARCADE);
                return;
            case BUTTON_HOW_TO_PLAY:
                mApp.GotoInterfaceState(9);
                return;
            }
        }

        /// <summary>FUN_00017144: action sheet 7, "Start New Game" (theNewId) / "Continue Game" (theContinueId).</summary>
        private void ShowNewOrContinue(int theNewId, int theContinueId)
        {
            ActionSheet aSheet = new ActionSheet(DIALOG_NEW_OR_CONTINUE, this, mApp);
            aSheet.SetRedButton(theNewId, SexyAppBase.Loc("Start New Game"));
            aSheet.SetGreenButton(theContinueId, SexyAppBase.Loc("Continue Game"));
            aSheet.Show();
        }

        /// <summary>
        /// vf96 (0x17368): "Start New Game" asks to discard the save (action sheet 8, "Discard Saved Game" /
        /// "Cancel" = 1001); discarding deletes it and starts anew; "Continue Game" continues.
        /// </summary>
        public void DialogButtonDepress(int theDialogId, int theButtonId)
        {
            if (theDialogId == DIALOG_DISCARD)
            {
                if (theButtonId == 3)
                {
                    mApp.DeleteSavedGame(BookWormApp.GAME_ARCADE);
                    mApp.StartGame(BookWormApp.GAME_ARCADE);
                }
                else if (theButtonId == 2)
                {
                    mApp.DeleteSavedGame(BookWormApp.GAME_CLASSIC);
                    mApp.StartGame(BookWormApp.GAME_CLASSIC);
                }
                return;
            }
            if (theDialogId != DIALOG_NEW_OR_CONTINUE)
            {
                return;
            }
            switch (theButtonId)
            {
            case 2:
            case 3:
            {
                ActionSheet aSheet = new ActionSheet(DIALOG_DISCARD, this, mApp);
                aSheet.SetRedButton(theButtonId, SexyAppBase.Loc("Discard Saved Game"));
                aSheet.SetGreenButton(1001, SexyAppBase.Loc("Cancel"));
                aSheet.Show();
                break;
            }
            case 4:
                mApp.StartGame(BookWormApp.GAME_CLASSIC);
                break;
            case 5:
                mApp.StartGame(BookWormApp.GAME_ARCADE);
                break;
            }
        }

        public void DialogButtonPress(int theDialogId, int theButtonId)
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
    /// Port (translated): a game-selector button that draws its title as text, centred line by line in outlined
    /// Cooper Black, in place of its image (which still sizes it); with no text, a plain BookWormButton. Like the
    /// art, the text has a shadow and a gradient (GradientText). A ribbon (SetRibbonText) draws its text-free
    /// image with the text over it instead.
    /// </summary>
    public class TextOrImageButton : BookWormButton
    {
        private string[] mLines;

        private Color mTopColor;

        private Color mBottomColor;

        public TextOrImageButton(int theId, ButtonListener theListener, BookWormApp theApp) : base(theId, theListener, theApp)
        {
        }

        /// <summary>True: the image is drawn, the text fitted over it where the ribbon art's words were.</summary>
        private bool mOverImage;

        public void SetText(string theText, Color theTopColor, Color theBottomColor)
        {
            mLines = theText?.Split('\n');
            mTopColor = theTopColor;
            mBottomColor = theBottomColor;
            mOverImage = false;
            SetFont(Res.FONT_COOPER_OUTLINED);
        }

        /// <summary>Port: the Continue ribbon's words (null: the original art alone), in Arial Rounded as the art's.</summary>
        public void SetRibbonText(string theText, Color theTopColor, Color theBottomColor)
        {
            mLines = theText?.Split('\n');
            mTopColor = theTopColor;
            mBottomColor = theBottomColor;
            mOverImage = true;
        }

        /// <summary>
        /// The ribbon art's words (iPad Retina, 188 x 324): their capitals from row 98 to the last baseline at 196,
        /// 150 wide, centred; the same art at every resolution, so it scales with the image.
        /// </summary>
        private const int RIBBON_ART_WIDTH = 188;

        private const int RIBBON_ART_HEIGHT = 324;

        private const int RIBBON_TEXT_CENTER_Y = 147;

        private const int RIBBON_TEXT_WIDTH = 150;

        private const int RIBBON_TEXT_HEIGHT = 98;

        /// <summary>Port: how much of a mode button's width its translated title may take.</summary>
        private const float MODE_TEXT_WIDTH_SHARE = 0.9f;

        public override void Draw(Graphics g)
        {
            if (mLines == null)
            {
                base.Draw(g);
                return;
            }
            if (mOverImage)
            {
                base.Draw(g);
                if (!mBtnNoDraw)
                {
                    GradientText.DrawFitted(g, Res.FONT_ARIAL_MEDIUM, mLines, mWidth / 2, mHeight * RIBBON_TEXT_CENTER_Y / RIBBON_ART_HEIGHT,
                        mWidth * RIBBON_TEXT_WIDTH / RIBBON_ART_WIDTH, mHeight * RIBBON_TEXT_HEIGHT / RIBBON_ART_HEIGHT, mTopColor, mBottomColor);
                }
                return;
            }
            if (mBtnNoDraw)
            {
                return;
            }
            int aLineHeight = mFont.GetLineSpacing();
            // Port: a longer title than the art's (KLASSISCHES SPIEL) shrinks to the button, its block kept centred.
            float aScale = GradientText.FitScale(mFont, mLines, (int)(mWidth * MODE_TEXT_WIDTH_SHARE));
            int aFirstBaseline = (int)System.Math.Round((mHeight - aLineHeight * mLines.Length * aScale) / 2 + mFont.GetAscent() * aScale);
            GradientText.Draw(g, mFont, mLines, mWidth / 2, aFirstBaseline, mTopColor, mBottomColor, null, aLineHeight, aScale);
        }
    }
}
