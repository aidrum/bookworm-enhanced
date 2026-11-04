using System.Collections.Generic;

namespace Sexy
{
    /// <summary>
    /// Sexy::Board (ctor 0x3858, init 0x3bdc, screen 16): the game. A 7x7 grid of letter tiles in staggered
    /// columns (even columns sit half a tile higher); the player chains adjacent tiles into words. Being
    /// ported in parts: Board.cs (state, setup), Board.Grid.cs (filling, falling, adjacency),
    /// Board.Draw.cs, Board.Update.cs. See references/NEXT_STEPS.md "Board map".
    /// </summary>
    public partial class Board : SlideInWidget, ButtonListener, DialogListener
    {
        public const int COLS = 7;

        public const int ROWS = 7;

        public const int STATE_NEW = 0;

        public const int STATE_PLAYING = 1;

        /// <summary>The library has caught fire: Lex panics, then the game over page (interface state 11).</summary>
        public const int STATE_GAME_OVER_START = 2;

        public const int STATE_GAME_OVER = 3;

        /// <summary>A fire tile reached the bottom: the tiles burn for 0x2a0 frames.</summary>
        public const int STATE_BURNING = 4;

        /// <summary>The level's score is reached: once the tiles settle, Lex cheers and the level page shows.</summary>
        public const int STATE_LEVEL_UP = 5;

        /// <summary>Back from the level page: the next level starts (0x6e5c).</summary>
        public const int STATE_NEXT_LEVEL = 6;

        public const int STATE_SCRAMBLE = 7;

        /// <summary>A book collection was completed (interface state 12).</summary>
        public const int STATE_COLLECTION = 8;

        /// <summary>Points to reach each level (0xcccb4), past the last: see <see cref="GetLevelScore"/>.</summary>
        private static readonly int[] LEVEL_SCORES =
        {
            3000, 7000, 12000, 18000, 25000, 34000, 44000, 56000, 69000, 80000, 100000, 120000, 140000, 160000,
            190000, 220000, 250000, 290000, 330000, 380000, 430000, 490000, 560000, 630000, 710000, 800000, 900000,
            1000000, 1100000, 1200000, 1300000, 1500000, 1700000, 1900000, 2100000, 2300000, 2600000, 2900000,
            3200000, 3500000, 3900000, 4300000
        };

        /// <summary>By level (0xccc8c): stored as a double in the global 0x10c650 by the ctor, NextLevel and LoadGame, but
        /// never read (the arcade timer is a flat <see cref="ARCADE_TIMER"/>). Kept for reference.</summary>
        public static readonly int[] ARCADE_BURN_DELAYS = { 550, 500, 450, 400, 350, 300, 250, 200, 150, 125 };

        public const int BUTTON_MENU = 0;

        /// <summary>The previous random letter (0x10c658) and the level of the previous refill (0x10c65c).</summary>
        private static int gLastLetter = -1;

        private static int gLastLevel;

        /// <summary>The shared random generator (PTR 0x109b38).</summary>
        public static MTRand gRand = new MTRand((uint)System.Environment.TickCount);

        public bool mUpdated; // 0xe8

        public MTRand mRand; // 0xec

        public Piece[,] mGrid = new Piece[ROWS, COLS]; // 0xf8, [row, col]

        public BookWormButton mMenuButton;

        /// <summary>STATE_* (0x1d8).</summary>
        public int mState;

        /// <summary>The selected tiles, in order (0x1e0).</summary>
        public List<Piece> mSelection = new List<Piece>();

        /// <summary>Counts down by the words made; at 0 a fire tile drops (0x1e8, starts at 5).</summary>
        public double mFireTimer = 5.0;

        /// <summary>The next random fire tile roll is skipped (0x1f0; set at the start and by a bonus word).</summary>
        public bool mSkipFire = true;

        /// <summary>Game over: everything drawn grey (0x1f1).</summary>
        public bool mGreyed;

        /// <summary>The fire sound played this refill (0x1f3).</summary>
        public bool mFireSoundPlayed;

        /// <summary>The level bar's fill, 0-1 (0x20c).</summary>
        public double mLevelProgress;

        /// <summary>The score as shown (0x324) and its heat glow (0x24c).</summary>
        public int mDisplayScore;

        public double mScoreHeat;

        /// <summary>Fire tiles dropped (0x2b8).</summary>
        public int mFireTileCount;

        /// <summary>Arcade: frames to the next tile catching fire (0x2bc).</summary>
        public int mArcadeTimer = ARCADE_TIMER;

        /// <summary>Arcade: the countdown once Lex is in danger (0x2c0); below 0 the library burns.</summary>
        public int mArcadeCountdown;

        /// <summary>Warning sounds left to play, 3 when a fire tile reaches the bottom row (0x2c4), and the
        /// frames to the next (0x2c8).</summary>
        public int mWarnings;

        public int mWarningTimer;

        /// <summary>The level bar's target fill, 0-1 (0x204); <see cref="mLevelProgress"/> eases to it.</summary>
        public double mLevelTarget;

        /// <summary>The score roll-up step (0x328, 1.0).</summary>
        public double mScoreStep = 1.0;

        /// <summary>The rising tones after a word: tone played, word length, frames to the next (0x278-0x280).</summary>
        public int mToneIndex;

        public int mToneCount;

        public int mToneTimer;

        /// <summary>Lex has yet to cheer the level up (0x2ee).</summary>
        public bool mLevelUpPending;

        /// <summary>The level's time was counted and its page asked for (0x344).</summary>
        public bool mLevelTimeDone;

        /// <summary>Play clock: start of the running stretch (ms, 0x1c8), seconds before it (0x1d4), and the
        /// last level's and the game's seconds (0x1cc, 0x1d0).</summary>
        public int mClockStart = System.Environment.TickCount;

        public int mPlaySeconds;

        public int mLevelSeconds;

        public int mTotalSeconds;

        /// <summary>Game over: the fire tile that did it (0x284), frames until the tiles catch (0x28c), the burn
        /// countdown (0x2a0).</summary>
        public Piece mFatalPiece;

        public int mCatchTimer;

        public int mGameOverTimer;

        /// <summary>The collection page was asked for (0x345).</summary>
        public bool mCollectionShown;

        /// <summary>Board-space effects (0xf0: gem sparkles) and embers (0xf4: fire, ash).</summary>
        public EffectMgr mEffects;

        public EmberListMgr mEmbers;

        /// <summary>The screen-space overlay (0x1bc): the word flying into Lex, the flash.</summary>
        public OverlayWidget mOverlay;

        /// <summary>Frames until Lex burps after eating a fire tile (0x300; only after a word with two or more).</summary>
        public int mBurpTimer;

        /// <summary>The burp cloud is up (0x2fd), and its alpha, 1 to 0 (0x304).</summary>
        public bool mBurpCloud;

        public double mBurpAlpha;

        /// <summary>The white flash as the library catches, 1 to 0 (0x290).</summary>
        public double mFlash;

        /// <summary>Hint countdown and kind for the tutorial hints (0x2cc, 0x2d0).</summary>
        public int mHintTimer;

        public int mHintKind;

        /// <summary>Frame of the last landing sound (0x30c).</summary>
        public int mLastHitFrame;

        /// <summary>Tile size (0x310, 0x314) and grid size (0x318, 0x31c).</summary>
        public int mTileWidth;

        public int mTileHeight;

        public int mCols = COLS;

        public int mRows = ROWS;

        /// <summary>Some tile is still falling (0x346).</summary>
        public bool mPiecesMoving;

        public int mLevel; // 0x348

        /// <summary>Pause reasons, a bit mask (0x34c).</summary>
        public int mPauseFlags;

        public Board(BookWormApp theApp) : base(theApp)
        {
            mClip = false;
            Init();
        }

        /// <summary>0x3bdc: the menu button, random generator, tile size, and the rest of the starting state.</summary>
        private void Init()
        {
            ScreenPos aPos = mApp.mScreenPos;
            Image aMenuImage = AtlasResourceBase.IMAGE_GROUP4_21;
            mMenuButton = new BookWormButton(BUTTON_MENU, this, mApp);
            mMenuButton.Resize(aPos.At(0x148), aPos.At(0x14c), aMenuImage.mWidth, aMenuImage.mHeight);
            mMenuButton.mButtonImage = aMenuImage;
            AddWidget(mMenuButton);
            mRand = new MTRand((uint)System.Environment.TickCount);
            mTileWidth = aPos.At(0x188);
            mTileHeight = aPos.At(0x18c);
            mCols = COLS;
            mRows = ROWS;
            mGreyed = false;
            mFireTimer = 5.0;
            mWordScore = new WordScore(mApp, this);
            mGameStats = new StatKeeper(mApp);
            mLevelStats = new StatKeeper(mApp);
            mEffects = new EffectMgr(mApp);
            mEmbers = new EmberListMgr(mApp);
            mOverlay = new OverlayWidget(mApp, this);
            AddWidget(mOverlay);
            PickBonusWord();
            // (Arcade also sets a global double to 550 here; nothing reads it.)
            mGlowPulse = new GlowPulseWidget(AtlasResourceBase.IMAGE_GROUP4_10, aPos.At(0x380), aPos.At(0x384), aPos.At(0x388), aPos.At(0x38c));
            AddWidget(mGlowPulse);
        }

        /// <summary>Pulses over the last tile of a valid word (0x1c0).</summary>
        public GlowPulseWidget mGlowPulse;

        /// <summary>Frames a valid word has waited, for its pulse every SUBMIT_PULSE_FRAMES (0x2d8).</summary>
        public int mSubmitWaitCount;

        /// <summary>The tap-to-submit arrow is wanted (0x2dc) at (0x2e0, 0x2e4); its alpha (0x2e8).</summary>
        public bool mSubmitArrow;

        public int mSubmitArrowX;

        public int mSubmitArrowY;

        public int mSubmitArrowAlpha;

        /// <summary>DAT_00134e80 (325).</summary>
        public const int SUBMIT_PULSE_FRAMES = 0x145;

        /// <summary>FUN_00007184: sets or clears pause reasons (1 interface, 2 dialog, 4 hidden).</summary>
        public void SetPaused(int theReasons, bool thePaused)
        {
            int anOld = mPauseFlags;
            mPauseFlags = thePaused ? anOld | theReasons : anOld & ~theReasons;
            if (anOld == 0 && mPauseFlags != 0)
            {
                foreach (Widget aWidget in mWidgets)
                {
                    aWidget.SetDisabled(true);
                }
                mPlaySeconds += (System.Environment.TickCount - mClockStart) / 1000;
            }
            else if (anOld != 0 && mPauseFlags == 0)
            {
                foreach (Widget aWidget in mWidgets)
                {
                    aWidget.SetDisabled(false);
                }
                mClockStart = System.Environment.TickCount;
            }
            MarkDirty();
        }

        /// <summary>FUN_00006e28: the score that ends theLevel.</summary>
        public static int GetLevelScore(int theLevel)
        {
            int aScore = LEVEL_SCORES[System.Math.Min(theLevel, 0x29)];
            if (theLevel > 0x29)
            {
                aScore += (theLevel * 25000 - 550000) * (theLevel - 0x29);
            }
            return aScore;
        }

        /// <summary>The level bar's fill for the level's points so far (0x8d20, 0x6e5c).</summary>
        private double GetLevelFill()
        {
            int aFrom = mLevel == 0 ? 0 : GetLevelScore(mLevel - 1);
            return mLevelStats.mScore / (double)(GetLevelScore(mLevel) - aFrom);
        }

        /// <summary>
        /// FUN_00006e5c: the next level. The level stats start again with the points past the old level's mark,
        /// the bar refills from empty, the arcade timer starts again and Arcade's Lex cries "GET READY" (0x2ec).
        /// </summary>
        public void NextLevel()
        {
            mLevelStats.Reset();
            mLevelStats.mScore = mGameStats.mScore - GetLevelScore(mLevel);
            mLevel++;
            mState = STATE_PLAYING;
            mLevelTarget = GetLevelFill();
            mLevelProgress = 0;
            mBestPointsLevel = 0;
            mArcadeTimer = ARCADE_TIMER;
            if (mApp.mGameMode == BookWormApp.GAME_ARCADE)
            {
                mStartCry = true;
            }
        }

        /// <summary>The play clock in seconds (0x1d4 plus the running stretch).</summary>
        private int StopClock()
        {
            return mPlaySeconds + (System.Environment.TickCount - mClockStart) / 1000;
        }

        /// <summary>FUN_00007260.</summary>
        public bool IsPaused()
        {
            return mPauseFlags != 0;
        }

        /// <summary>vf80 (0x10754).</summary>
        public override void SetInterfaceState(int theState, int theTarget, int theParam)
        {
            base.SetInterfaceState(theState, theTarget, theParam);
            SetPaused(1, true);
        }

        /// <summary>vf82 (0x106f8): fills ScreenPos + 0x2d0 and slides up.</summary>
        /// <summary>Port (dynamic aspect): the menu button is placed once (Init), then the board is attached again.</summary>
        public override void Relayout()
        {
            ScreenPos aPos = mApp.mScreenPos;
            mMenuButton?.Move(aPos.At(0x148), aPos.At(0x14c));
            base.Relayout();
        }

        /// <summary>Port (dynamic aspect): the tiles follow the grid when the layout moves it (BookWormApp.ApplyLayout).</summary>
        public void MovePieces(int theDX, int theDY)
        {
            if (theDX == 0 && theDY == 0)
            {
                return;
            }
            foreach (Piece aPiece in mGrid)
            {
                if (aPiece != null)
                {
                    aPiece.mX += theDX;
                    aPiece.mY += theDY;
                }
            }
        }

        public override void AttachToManager(int theState, int theTarget, int theParam)
        {
            base.AttachToManager(theState, theTarget, theParam);
            Resize(mApp.mScreenPos.RectAt(0x2d0));
            mOverlay.Resize(-mX, -mY, mApp.mWidth, mApp.mHeight);
            SetupSlide(SLIDE_FROM_BOTTOM, 0.5f);
        }

        /// <summary>
        /// vf81 (0x1076c): arriving in state 10 a new board starts falling in; back from the collection page play
        /// goes on, back from the level page the next level starts (state 6).
        /// </summary>
        public override void InterfaceStateDone()
        {
            if (mInterfaceState == 10)
            {
                // Port: the board takes the keyboard (the space bar scrambles on the desktop).
                mApp.mWidgetManager.SetFocus(this);
                if (mState == STATE_NEW)
                {
                    NewGame();
                    mState = STATE_PLAYING;
                }
                else if (mCollectionShown)
                {
                    mState = STATE_PLAYING;
                    mCollectionShown = false;
                }
                else if (mLevelTimeDone)
                {
                    mState = STATE_NEXT_LEVEL;
                }
            }
            base.InterfaceStateDone();
            SetPaused(1, mInterfaceTarget != 1);
        }

        /// <summary>vf95.</summary>
        public void ButtonPress(int theId)
        {
            mApp.PlaySound(Res.SOUND_BUTTON_CLICK);
        }

        public void ButtonPress(int theId, int theClickCount)
        {
            ButtonPress(theId);
        }

        /// <summary>
        /// vf94 (0x10564): MENU saves the game and goes back to the main menu; tapping Lex (2) asks for a scramble;
        /// tapping the balloon (1) submits the word it shows (as a tap on the word's last tile), or dismisses any
        /// other message.
        /// </summary>
        public void ButtonDepress(int theId)
        {
            if (theId == BUTTON_MENU && CanSaveGame())
            {
                SaveGame(mApp.mGameMode);
                mApp.GotoInterfaceState(2);
            }
            else if (theId == 2)
            {
                RequestScramble();
            }
            else if (theId == 1)
            {
                WordBalloon aBalloon = mApp.mWordBalloon;
                if (aBalloon.GetMode() != WordBalloon.MODE_WORD)
                {
                    aBalloon.Hide();
                    return;
                }
                if (mSelection.Count != 0)
                {
                    Piece aLast = mSelection[mSelection.Count - 1];
                    mTouchMode = 3;
                    SubmitWord((int)(aLast.mX + mTileWidth / 2), (int)(aLast.mY + mTileHeight / 2));
                    mTouchMode = 1;
                }
            }
        }

        /// <summary>
        /// 0x4c1c: while the board is enabled, taps on Lex (id 2) and on the balloon (id 1) come to the board.
        /// (The original also flags the app's idle-timer mask, app + 0xdb0 bit 4.)
        /// </summary>
        public override void SetDisabled(bool isDisabled)
        {
            if (mDisabled && !isDisabled)
            {
                mApp.mLex.mListenerId = 2;
                mApp.mLex.mListener = this;
                mApp.mWordBalloon.SetListener(1, this);
            }
            else if (!mDisabled && isDisabled)
            {
                mApp.mLex.mListenerId = 0;
                mApp.mLex.mListener = null;
                mApp.mWordBalloon.SetListener(0, null);
            }
            base.SetDisabled(isDisabled);
        }

        public void DialogButtonPress(int theDialogId, int theButtonId)
        {
        }

        /// <summary>vf97 (0x10680): a dialog closed: "Disable Hints" turns hints off, "Scramble" scrambles.</summary>
        public void DialogButtonDepress(int theDialogId, int theButtonId)
        {
            mHintShowing = false;
            if (theDialogId == 0 && theButtonId == DIALOG_DISABLE_HINTS)
            {
                mApp.mShowHints = false;
                mApp.SaveSettings();
            }
            else if (theDialogId == DIALOG_SCRAMBLE && theButtonId == BUTTON_SCRAMBLE)
            {
                Scramble();
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
