using Microsoft.Xna.Framework;

namespace Sexy
{
    /// <summary>
    /// Sexy::OptionsDialog (0x1fe80, screen 13, state 7): the options page of the book: sound effect and
    /// music volume sliders, switches for hints, definitions and the scramble confirmation, and the version.
    /// Slides up from the bottom.
    /// </summary>
    public class OptionsDialog : BookWidget, SliderListener, SwitchListener
    {
        public const int SLIDER_SFX = 0;

        public const int SLIDER_MUSIC = 1;

        public const int SWITCH_HINTS = 2;

        public const int SWITCH_DEFINITIONS = 3;

        public const int SWITCH_CONFIRM_SCRAMBLE = 4;

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

        /// <summary>How many times hints were switched on while this page was up (0x138).</summary>
        public int mHintsOnCount;

        public OptionsDialog(BookWormApp theApp) : base(theApp)
        {
            mHasAlpha = true;
            mSfxLabel = MakeLabel("Sound Effects");
            mMusicLabel = MakeLabel("Music Tracks");
            mHintsLabel = MakeLabel("Show Hints");
            mDefinitionsLabel = MakeLabel("Show Definitions");
            mConfirmScrambleLabel = MakeLabel("Confirm to Scramble");
            mSfxSlider = MakeSlider(SLIDER_SFX);
            mMusicSlider = MakeSlider(SLIDER_MUSIC);
            mHintsSwitch = MakeSwitch(SWITCH_HINTS);
            mDefinitionsSwitch = MakeSwitch(SWITCH_DEFINITIONS);
            mConfirmScrambleSwitch = MakeSwitch(SWITCH_CONFIRM_SCRAMBLE);
            mVersionLabel = new LabelWidget(theApp.mVersion, Res.FONT_DEFINITIONS_SMALL);
            mVersionLabel.SizeToText();
            mVersionLabel.SetColor(0, new Color(0, 0, 0, 255));
            AddWidget(mVersionLabel);
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
            mHintsOnCount = 0;
            PlaceIn(mApp.mScreenPos.RectAt(0x1f0));
            LayoutControls();
            SetupSlide(SLIDE_FROM_BOTTOM, 0.5f);
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
            mConfirmScrambleSwitch.Layout(0x2200, mDefinitionsSwitch, 0, aGap, 0, 0);
            mHintsLabel.Layout(0x200800, mHintsSwitch, -aPad, 0, 0, 0);
            mDefinitionsLabel.Layout(0x200800, mDefinitionsSwitch, -aPad, 0, 0, 0);
            mConfirmScrambleLabel.Layout(0x200800, mConfirmScrambleSwitch, -aPad, 0, 0, 0);
            mSfxLabel.mJustifyH = 0;
            mMusicLabel.mJustifyH = 0;
            mHintsLabel.mJustifyH = 1;
            mDefinitionsLabel.mJustifyH = 1;
            mConfirmScrambleLabel.mJustifyH = 1;
            mVersionLabel.Layout(0xa000, this, -aRect.mWidth, 5 - aRect.mHeight, 0, 0);
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
            default:
                return;
            }
            mApp.SaveSettings();
        }
    }
}
