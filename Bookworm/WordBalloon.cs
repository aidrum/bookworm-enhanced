using Microsoft.Xna.Framework;

namespace Sexy
{
    /// <summary>
    /// Sexy::WordBalloon (0x2fbcc, BookWormApp + 0xdc0, z-order 0x55): Lex's speech balloon. Each kind of
    /// message is laid out with a FancyTextWidget and pre-rendered with its background into an offscreen
    /// image; the balloon then fades the old message out (20 ticks) and the new one in (10 ticks).
    /// </summary>
    public class WordBalloon : Widget
    {
        public const int MODE_NONE = 0;

        public const int MODE_HIDDEN = 1;

        /// <summary>The word being made, with its glow (FUN_00030c28; Board, not ported yet).</summary>
        public const int MODE_WORD = 2;

        public const int MODE_BONUS_WORD = 3;

        public const int MODE_TEXT = 4;

        /// <summary>The app's title text, shown for 500 frames.</summary>
        public const int MODE_TITLE = 5;

        public const int MODE_DEFINITION = 6;

        public const int MODE_EXCLAIM = 7;

        public const int MODE_SPEECH = 8;

        /// <summary>Fade in / out lengths, in ticks (0x135f70, 0x135f74).</summary>
        private const int FADE_IN_TICKS = 10;

        private const int FADE_OUT_TICKS = 0x14;

        private static readonly Color TEXT_COLOR = new Color(0, 0, 0, 255);

        public Timeline<WordBalloon> mTimeline; // 0x78

        /// <summary>Frames left before a timed message hides (0xb4).</summary>
        public int mTimer;

        /// <summary>What is shown (0xb8): MODE_HIDDEN when free.</summary>
        public int mMode = MODE_HIDDEN;

        /// <summary>The message shown (0xbc) and the one being prepared (0xcc); they swap when it shows.</summary>
        public MemoryImage mShownImage;

        public MemoryImage mNextImage;

        public string mText = ""; // 0xc0

        public int mAlpha; // 0xc4

        /// <summary>What to show next (0xc8).</summary>
        public int mPendingMode;

        /// <summary>Where the next message goes (0xd0).</summary>
        public TRect mNextRect;

        public FancyTextWidget mTextWidget = new FancyTextWidget(); // 0xe0

        public int mListenerId = -1; // 0xe4

        public ButtonListener mListener; // 0xe8

        public bool mPressed; // 0xec

        public BookWormApp mApp; // 0xf4

        public WordBalloon(BookWormApp theApp)
        {
            mApp = theApp;
            mTimeline = new Timeline<WordBalloon>(this);
            mHasAlpha = true;
            mClip = false;
            SetVisible(false);
            SetDisabled(true);
            mGlow = new GlowPulseWidget(AtlasResourceBase.IMAGE_GROUP51_02, 0, 0, 0, 0);
            AddWidget(mGlow);
        }

        /// <summary>The glow pulsed around the word (0xf0).</summary>
        public GlowPulseWidget mGlow;

        /// <summary>FUN_000300b0.</summary>
        public int GetMode()
        {
            return mPendingMode != MODE_NONE ? mPendingMode : mMode;
        }

        /// <summary>FUN_000300c0: nothing shown.</summary>
        public bool IsFree()
        {
            return mMode == MODE_HIDDEN;
        }

        /// <summary>FUN_000300cc: who hears taps on the balloon.</summary>
        public void SetListener(int theId, ButtonListener theListener)
        {
            mListenerId = theId;
            mListener = theListener;
        }

        /// <summary>The text area of a balloon of theSize, inset by the ScreenPos insets at theInsetsOffset.</summary>
        private TRect TextRect(int theInsetsOffset, int theSizeOffset)
        {
            ScreenPos aPos = mApp.mScreenPos;
            TRect anInsets = aPos.RectAt(theInsetsOffset);
            return new TRect(anInsets.mX, anInsets.mY,
                aPos.At(theSizeOffset) - anInsets.mX - anInsets.mWidth,
                aPos.At(theSizeOffset + 4) - anInsets.mY - anInsets.mHeight);
        }

        private void StartText(TRect theRect, Font theFont, int theJustify)
        {
            mTextWidget.Resize(theRect);
            mTextWidget.Clear();
            mTextWidget.SetFont(theFont);
            mTextWidget.SetColor(TEXT_COLOR);
            mTextWidget.SetJustify(theJustify);
        }

        /// <summary>FUN_000300d8: an exclamation ("OPTIONS!") in the oval balloon.</summary>
        public void Say(string theText)
        {
            int aSpacing = mApp.mScreenPos.At(0x3e4);
            StartText(TextRect(0x320, 0x248), Res.FONT_COOPER, 0);
            mTextWidget.AddText(theText, aSpacing);
            mTextWidget.NewLine(aSpacing);
            mTextWidget.Fit(2);
            Prepare(mApp.mScreenPos.RectAt(0x240), AtlasResourceBase.IMAGE_GROUP4_00, false);
            mPendingMode = MODE_EXCLAIM;
            mText = "";
        }

        /// <summary>FUN_00030394: speech in the big bubble; too long, it gets twice the line spacing.</summary>
        public void Speak(string theText)
        {
            int aSpacing = mApp.mScreenPos.At(0x3e8);
            StartText(TextRect(0x330, 0x258), Res.FONT_ARIAL_LARGE, 0);
            mTextWidget.AddText(theText, aSpacing);
            mTextWidget.NewLine(aSpacing);
            mTextWidget.Fit(2);
            if (!mTextWidget.Fit(1))
            {
                mTextWidget.Clear();
                mTextWidget.SetColor(TEXT_COLOR);
                mTextWidget.SetJustify(0);
                mTextWidget.SetFont(Res.FONT_ARIAL_LARGE);
                mTextWidget.AddText(theText, aSpacing << 1);
                mTextWidget.NewLine(aSpacing << 1);
                mTextWidget.Fit(2);
            }
            Prepare(mApp.mScreenPos.RectAt(0x250), AtlasResourceBase.IMAGE_GROUP31_00, false);
            mPendingMode = MODE_SPEECH;
            mText = "";
        }

        /// <summary>
        /// PresentMoreGames (old version 2.2.1, 0x33ffe; gone from the later one): the More Games blurb in the box
        /// balloon stretched to FRAME_MORE_GAMES (ScreenPos 0x270, which the later version still carries).
        /// </summary>
        public void PresentMoreGames(string theText)
        {
            StartText(TextRect(0x310, 0x278), Res.FONT_ARIAL_LARGE, 0);
            mTextWidget.AddText(theText, 0);
            mTextWidget.NewLine(0);
            mTextWidget.Fit(2);
            Prepare(mApp.mScreenPos.RectAt(0x270), AtlasResourceBase.IMAGE_GROUP51_03, true);
            mPendingMode = MODE_SPEECH;
            mText = "";
        }

        /// <summary>FUN_000304f8: "Make the word "X" for bonus points!".</summary>
        public void ShowBonusWord(string theWord)
        {
            StartText(TextRect(0x310, 0x238), Res.FONT_ARIAL_MEDIUM, 0);
            mTextWidget.AddChunk(SexyAppBase.Loc("Make the word"));
            mTextWidget.NewLine(mApp.mScreenPos.At(0x3ec));
            mTextWidget.SetFont(Res.FONT_ARIAL_MEDIUM_OUTLINED);
            mTextWidget.SetColor(new Color(0, 0xff, 0, 0xff));
            mTextWidget.AddChunk("\"" + theWord + "\"");
            mTextWidget.NewLine(mApp.mScreenPos.At(0x3f0));
            mTextWidget.SetFont(Res.FONT_ARIAL_MEDIUM);
            mTextWidget.SetColor(TEXT_COLOR);
            mTextWidget.AddChunk(SexyAppBase.Loc("for bonus points!"));
            mTextWidget.NewLine(0);
            mTextWidget.Fit(1);
            Prepare(mApp.mScreenPos.RectAt(0x230), AtlasResourceBase.IMAGE_GROUP51_03, false);
            mPendingMode = MODE_BONUS_WORD;
            mText = "";
        }

        /// <summary>
        /// FUN_00030c28: the word being spelled (the dictionary's spelling once valid, cut to 12 letters, never
        /// ending in a lone Q), outlined in a colour by its tiles: tan under 3 letters, cream all plain, else
        /// diamond white, sapphire blue, gold yellow, bonus green, fire orange. A valid word adds "+N pts." and
        /// "TAP TO SUBMIT". Shown at once when the balloon already shows a word.
        /// </summary>
        public void ShowWord(WordScore theWord)
        {
            string aWord = theWord.mValid ? theWord.mMatch : theWord.mWord;
            if (aWord.Length != 0)
            {
                Color aColor = new Color(0, 0, 0, 255);
                if (aWord.Length < 3)
                {
                    aColor = new Color(0xcc, 0xad, 0x6e, 0xff);
                }
                else if (theWord.mTypeCounts[Piece.TYPE_NORMAL] == theWord.mTileCount)
                {
                    aColor = new Color(0xff, 0xdd, 0x7b, 0xff);
                }
                else if (theWord.mTypeCounts[Piece.TYPE_DIAMOND] > 0)
                {
                    aColor = new Color(0xff, 0xff, 0xff, 0xff);
                }
                else if (theWord.mTypeCounts[Piece.TYPE_SAPPHIRE] > 0)
                {
                    aColor = new Color(0x2d, 0xc3, 0xff, 0xff);
                }
                else if (theWord.mTypeCounts[Piece.TYPE_GOLD] > 0)
                {
                    aColor = new Color(0xff, 0xdc, 0, 0xff);
                }
                else if (theWord.mTypeCounts[Piece.TYPE_BONUS] > 0)
                {
                    aColor = new Color(0, 0xff, 0, 0xff);
                }
                else if (theWord.mTypeCounts[Piece.TYPE_FIRE] > 0)
                {
                    aColor = new Color(0xff, 100, 0, 0xff);
                }
                if (aWord.Length > 12)
                {
                    aWord = aWord.Substring(0, 12);
                    if (aWord[11] == 'Q')
                    {
                        aWord = aWord.Substring(0, 11);
                    }
                }
                mTextWidget.Resize(TextRect(0x310, 0x238));
                mTextWidget.Clear();
                mTextWidget.SetJustify(0);
                mTextWidget.SetColor(aColor);
                mTextWidget.SetFont(Res.FONT_ARIAL_MEDIUM_OUTLINED);
                mTextWidget.AddChunk(aWord);
                mTextWidget.NewLine(0);
                if (theWord.mValid)
                {
                    mTextWidget.SetFont(Res.FONT_GOLDSCORE);
                    mTextWidget.SetColor(new Color(255, 255, 255, 255));
                    mTextWidget.AddChunk(string.Format("+{0} pts.", theWord.mScore));
                    mTextWidget.NewLine(0);
                    mTextWidget.SetFont(Res.FONT_ARIAL_SMALL);
                    mTextWidget.SetColor(new Color(0, 0x5c, 0x09, 0xff));
                    mTextWidget.AddChunk("TAP TO SUBMIT");
                    mTextWidget.NewLine(0);
                }
            }
            mTextWidget.Fit(2);
            Prepare(mApp.mScreenPos.RectAt(0x230), AtlasResourceBase.IMAGE_GROUP51_03, false);
            if (mMode == MODE_WORD)
            {
                MemoryImage aShown = mShownImage;
                mShownImage = mNextImage;
                mNextImage = aShown;
            }
            else
            {
                mPendingMode = MODE_WORD;
            }
            mText = "";
        }

        /// <summary>FUN_00031980: in word mode, the glow pulses around the balloon.</summary>
        public void PulseWord()
        {
            if (mMode == MODE_WORD)
            {
                mGlow.Resize(0, 0, mWidth, mHeight);
                mGlow.Pulse();
            }
        }

        /// <summary>FUN_000308fc: plain text in the box balloon.</summary>
        public void ShowText(string theText)
        {
            StartText(TextRect(0x310, 0x238), Res.FONT_ARIAL_MEDIUM, 0);
            mTextWidget.AddText(theText, 0);
            mTextWidget.NewLine(0);
            mTextWidget.Fit(2);
            Prepare(mApp.mScreenPos.RectAt(0x230), AtlasResourceBase.IMAGE_GROUP51_03, false);
            mPendingMode = MODE_TEXT;
            mText = "";
        }

        /// <summary>FUN_000309ec: a word and its definition, in smaller fonts until it fits, in a stretched box.</summary>
        public void ShowDefinition(string theWord, string theDefinition)
        {
            Font[,] aFonts =
            {
                { Res.FONT_ARIAL_MEDIUM, Res.FONT_DEFINITIONS_LARGE },
                { Res.FONT_ARIAL_SMALL, Res.FONT_DEFINITIONS_MEDIUM },
                { Res.FONT_ARIAL_SMALL, Res.FONT_DEFINITIONS_SMALL }
            };
            TRect aRect = TextRect(0x310, 0x268);
            for (int i = 0; i < 3; i++)
            {
                if (i == 0)
                {
                    mTextWidget.Resize(aRect);
                }
                mTextWidget.Clear();
                mTextWidget.SetColor(TEXT_COLOR);
                mTextWidget.SetJustify(-1);
                mTextWidget.SetFont(aFonts[i, 0]);
                mTextWidget.AddChunk(theWord);
                mTextWidget.NewLine(0);
                mTextWidget.SetFont(aFonts[i, 1]);
                mTextWidget.AddText(theDefinition, 0);
                mTextWidget.NewLine(0);
                if (mTextWidget.Fit(1))
                {
                    break;
                }
            }
            Prepare(mApp.mScreenPos.RectAt(0x260), AtlasResourceBase.IMAGE_GROUP51_03, true);
            mPendingMode = MODE_DEFINITION;
            mText = "";
        }

        /// <summary>FUN_000311c4: the app's title text (BookWormApp + 0x34) for 500 frames.</summary>
        public void ShowTitle()
        {
            Font[] aFonts = { Res.FONT_ARIAL_MEDIUM, Res.FONT_ARIAL_SMALL, Res.FONT_DEFINITIONS_SMALL };
            TRect aRect = TextRect(0x310, 0x238);
            mTextWidget.Resize(aRect);
            for (int i = 0; i < 3; i++)
            {
                mTextWidget.Clear();
                mTextWidget.SetFont(aFonts[i]);
                mTextWidget.SetColor(TEXT_COLOR);
                mTextWidget.SetJustify(0);
                mTextWidget.AddText(mApp.mTitle ?? "", 0);
                mTextWidget.NewLine(0);
                if (i == 2 || mTextWidget.Fit(1))
                {
                    break;
                }
            }
            mTextWidget.Fit(2);
            Prepare(mApp.mScreenPos.RectAt(0x230), AtlasResourceBase.IMAGE_GROUP51_03, false);
            mPendingMode = MODE_TITLE;
            mText = "";
            mTimer = 500;
        }

        /// <summary>
        /// FUN_000301d0: renders the background (stretched as a box, or as is) and the laid-out text into the
        /// next image, to show at theRect.
        /// </summary>
        private void Prepare(TRect theRect, Image theBackground, bool theBox)
        {
            mTimer = 0;
            mNextRect = theRect;
            if (mNextImage != null && (theRect.mWidth > mNextImage.mWidth || theRect.mHeight > mNextImage.mHeight))
            {
                mNextImage.Dispose();
                mNextImage = null;
            }
            if (mNextImage == null)
            {
                mNextImage = new MemoryImage();
                mNextImage.Create(theRect.mWidth, theRect.mHeight);
            }
            Graphics g = Graphics.GetNew(mNextImage);
            g.Clear(Color.Transparent);
            if (theBox)
            {
                g.DrawImageBox(new TRect(0, 0, theRect.mWidth, theRect.mHeight), theBackground);
            }
            else
            {
                g.DrawImage(theBackground, 0, 0);
            }
            g.Translate(mTextWidget.mX, mTextWidget.mY);
            mTextWidget.Draw(g);
            g.SetRenderTarget(null);
            g.PrepareForReuse();
        }

        /// <summary>FUN_0003137c: fades out.</summary>
        public void Hide()
        {
            mPendingMode = MODE_HIDDEN;
        }

        /// <summary>FUN_00031384: gone at once.</summary>
        public void HideNow()
        {
            mTimeline.Stop();
            mMode = MODE_HIDDEN;
            mPendingMode = MODE_NONE;
            SetVisible(false);
            SetDisabled(true);
        }

        /// <summary>0x313b8: runs the fade; a pending message fades the shown one out, then in.</summary>
        public override void Update()
        {
            base.Update();
            if (mTimeline.IsRunning())
            {
                mTimeline.AddTime(1f);
                mTimeline.Tick();
                return;
            }
            if (mPendingMode == MODE_NONE)
            {
                if (mTimer > 0 && --mTimer == 0)
                {
                    mPendingMode = MODE_HIDDEN;
                }
                return;
            }
            if (!mVisible)
            {
                FadeIn();
            }
            else
            {
                FadeOut();
            }
        }

        /// <summary>0x316b0: shows the prepared message (unless it is MODE_HIDDEN) and fades it in.</summary>
        private void FadeIn()
        {
            mMode = mPendingMode;
            mPendingMode = MODE_NONE;
            if (mMode == MODE_HIDDEN)
            {
                return;
            }
            Resize(mNextRect);
            MemoryImage aShown = mShownImage;
            mShownImage = mNextImage;
            mNextImage = aShown;
            SetVisible(true);
            mTimeline.Clear();
            Timeline<WordBalloon>.IntChannel aFade = mTimeline.AddIntChannel((theBalloon, theValue) => theBalloon.mAlpha = theValue);
            aFade.mKeys.SetKey(0, 0, false, true);
            aFade.mKeys.SetKey(FADE_IN_TICKS, 0xff, false, true);
            mTimeline.AddEvent(FADE_IN_TICKS, theBalloon => theBalloon.SetDisabled(false));
            mTimeline.Start();
        }

        /// <summary>0x31428: fades the shown message out, then hides.</summary>
        private void FadeOut()
        {
            SetDisabled(true);
            mTimeline.Clear();
            Timeline<WordBalloon>.IntChannel aFade = mTimeline.AddIntChannel((theBalloon, theValue) => theBalloon.mAlpha = theValue);
            aFade.mKeys.SetKey(0, mAlpha, false, true);
            aFade.mKeys.SetKey(FADE_OUT_TICKS, 0, false, true);
            mTimeline.AddEvent(FADE_OUT_TICKS, theBalloon => theBalloon.SetVisible(false));
            mTimeline.Start();
        }

        /// <summary>0x319b4.</summary>
        public override void Draw(Graphics g)
        {
            base.Draw(g);
            if (mShownImage != null)
            {
                g.SetColor(new Color(255, 255, 255, mAlpha));
                g.SetColorizeImages(true);
                g.DrawImage(mShownImage, 0, 0);
            }
        }

        /// <summary>0x31a00.</summary>
        public override void MouseDown(int x, int y, int theClickCount)
        {
            mPressed = true;
            mListener?.ButtonPress(mListenerId);
        }

        /// <summary>0x31a24.</summary>
        public override void MouseUp(int x, int y, int theClickCount)
        {
            if (mPressed)
            {
                mPressed = false;
                mListener?.ButtonDepress(mListenerId);
            }
        }
    }
}
