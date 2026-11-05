namespace Sexy
{
    public partial class Board
    {
        /// <summary>The rising tones after a word, one a letter (SOUND_TONE_1 + i, capped at the 8th).</summary>
        private static int[] gTones;

        /// <summary>
        /// 0x9f68 Board::Update (in progress). Ported: the level-up check, the falling/sinking loop of states 1,
        /// 3, 4, 5 and 8, the refill after removals, fire reaching the bottom, the danger music, state 6 (next
        /// level), the warnings and letter tones, the level bar and score roll-up, the game-over burn and the
        /// level-up tail. TODO: the hint tutorial before the switch, arcade burning (state 1), particle effects
        /// (fire, score sparkles, burning library), state 7 (scramble), the TIME BONUS (Arcade), Lex's praise
        /// (0x2f0/0x2f4) and the burp (0x300) (see D:\bookworm-re\notes\update.c).
        /// </summary>
        public override void Update()
        {
            base.Update();
            if (mPauseFlags != 0)
            {
                return;
            }
            mUpdated = true;
            Lex aLex = mApp.mLex;
            if (mState == STATE_PLAYING && GetLevelScore(mLevel) <= mGameStats.mScore)
            {
                mState = STATE_LEVEL_UP;
                mLevelUpPending = true;
                mApp.mPersonalBests.UpdateHighestRank(mApp.mGameMode, mLevel + 1);
            }
            UpdateStuckHint();
            UpdateSubmitPulse();
            if (mState == STATE_PLAYING)
            {
                UpdatePlayingCues(aLex);
            }
            switch (mState)
            {
            case STATE_PLAYING:
                UpdatePieces(mApp.mGameMode == BookWormApp.GAME_ARCADE && ArcadeTick());
                break;
            case STATE_GAME_OVER:
            case STATE_BURNING:
            case STATE_LEVEL_UP:
            case STATE_COLLECTION:
                UpdatePieces(false);
                break;
            case STATE_NEXT_LEVEL:
                mLevelTimeDone = false;
                mPlaySeconds = 0;
                mClockStart = System.Environment.TickCount;
                NextLevel();
                ArcadeTimeBonus();
                mState = STATE_PLAYING;
                break;
            }
            bool aFlying = mOverlay.mEffects.HasType(1);
            if ((mState == STATE_GAME_OVER_START || mState == STATE_GAME_OVER) && !mGreyed && aLex.IsIdle())
            {
                aLex.SetSparkle(true);
            }
            if (mState == STATE_GAME_OVER_START)
            {
                if (aLex.IsIdle() && aLex.mSparkle && !mGreyed)
                {
                    aLex.PlayAnim(Lex.ANIM_SPARKLE, true);
                }
                // The original moves on to state 3 at once and asks for the page only when Lex is free; this
                // waits in state 2 instead, so a busy Lex can't strand the game.
                if (!mLevelTimeDone && !aFlying && aLex.IsIdleOrSparkling())
                {
                    mState = STATE_GAME_OVER;
                    EndLevelClock();
                }
            }
            if (mState == STATE_BURNING && !aFlying && aLex.IsIdleOrSparkling())
            {
                if (aLex.IsIdle() && aLex.mSparkle && !mGreyed)
                {
                    aLex.PlayAnim(Lex.ANIM_SPARKLE, true);
                }
                mState = STATE_GAME_OVER;
            }
            if (mState == STATE_SCRAMBLE)
            {
                UpdateScramble();
            }
            UpdateBurp(aLex, aFlying);
            UpdatePoints();
            UpdateSounds();
            UpdateLevelBar(mOverlay.mEffects.CountType(1));
            UpdateScore();
            ScoreSparkles();
            UpdateGameOver(aLex);
            UpdateBurnSpread(aLex, aFlying);
            if (mEffects.Update())
            {
                MarkDirty();
            }
            if (mEmbers.Update())
            {
                MarkDirty();
            }
            if (mState == STATE_LEVEL_UP && !aFlying && aLex.IsIdle())
            {
                if (mLevelUpPending)
                {
                    mApp.PlaySound(Res.SOUND_LEX_LEVELUP);
                    aLex.PlayAnim(0xf, false);
                    mLevelUpPending = false;
                }
                else if (!mLevelTimeDone)
                {
                    EndLevelClock();
                }
            }
            UpdatePraise(aLex, aFlying);
            if (mHintTimer > 0 && mTouchMode != 2 && mState == STATE_PLAYING && --mHintTimer == 0)
            {
                ShowHint(mHintKind);
            }
            if (mState == STATE_COLLECTION && !aFlying && aLex.IsIdle() && !mCollectionShown)
            {
                mCollectionShown = true;
                mApp.GotoInterfaceState(12);
            }
        }

        /// <summary>Frames of play before the "really stuck" hint, by how often it was given (DAT_00134e8c).</summary>
        private static readonly int[] STUCK_HINT_FRAMES = { 2000, 3000, 4000 };

        /// <summary>
        /// 0x9f68 (start): in Classic, while the balloon is free, play frames since the last word are counted
        /// (0x2d4); at 2000, 3000 or 4000 (by app + 0x1208) Lex suggests tapping him or shaking, once until hints
        /// are switched on again (app + 0x1205).
        /// </summary>
        private void UpdateStuckHint()
        {
            if (mState != STATE_PLAYING || mApp.mGameMode != BookWormApp.GAME_CLASSIC || mApp.mHintTutorialShown ||
                mApp.mWordBalloon.mMode != WordBalloon.MODE_HIDDEN || mApp.mHintTutorialStep2 >= 3)
            {
                return;
            }
            if (++mHintIdleCount == STUCK_HINT_FRAMES[mApp.mHintTutorialStep2])
            {
                mApp.mWordBalloon.ShowText("If you're really stuck, tap me or try shaking things up!");
                mApp.mHintTutorialShown = true;
                mApp.mHintTutorialStep2++;
                mApp.SaveSettings();
            }
        }

        /// <summary>
        /// 0x9f68 (start): a valid word left unsubmitted (with hints on, not while touching) pulses its glow and
        /// the balloon every 325 frames; at the second pulse an arrow points at its last tile. The arrow fades in
        /// 6 a frame while wanted and out 25 a frame otherwise.
        /// </summary>
        private void UpdateSubmitPulse()
        {
            if (mState == STATE_PLAYING)
            {
                if (!mWordScore.mValid)
                {
                    mSubmitArrow = false;
                }
                else if (!mIsDown && mApp.mShowHints)
                {
                    if (++mSubmitWaitCount % SUBMIT_PULSE_FRAMES == 0)
                    {
                        mGlowPulse.Pulse();
                        mApp.mWordBalloon.PulseWord();
                        if (mSubmitWaitCount / SUBMIT_PULSE_FRAMES == 1)
                        {
                            Piece aLast = mSelection[mSelection.Count - 1];
                            mSubmitArrowX = (int)aLast.mX;
                            mSubmitArrowY = (int)aLast.mY;
                            mSubmitArrow = true;
                        }
                    }
                }
            }
            if (!mSubmitArrow)
            {
                if (mSubmitArrowAlpha > 0)
                {
                    mSubmitArrowAlpha = System.Math.Max(mSubmitArrowAlpha - 0x19, 0);
                }
            }
            else if (mSubmitArrowAlpha < 0xff)
            {
                mSubmitArrowAlpha = System.Math.Min(mSubmitArrowAlpha + 6, 0xff);
            }
        }

        /// <summary>
        /// Once no word is flying and Lex is idle, the burp countdown runs; at 0, after a word with two or more fire
        /// tiles, Lex burps (anim 0x17, SOUND_LEX_BURP) and a cloud rises (0x2fd), fading 0.005 a frame; below 0.9
        /// Lex settles (anim 0x16).
        /// </summary>
        private void UpdateBurp(Lex theLex, bool theFlying)
        {
            if (!theFlying && mBurpTimer > 0 && theLex.IsIdle() && --mBurpTimer == 0 && mBurnedWord)
            {
                if (!mGreyed)
                {
                    mBurpCloud = true;
                    mBurpAlpha = 1.0;
                    theLex.PlayAnim(0x17, false);
                    mApp.PlaySound(Res.SOUND_LEX_BURP);
                }
                mBurnedWord = false;
            }
            if (mBurpAlpha > 0)
            {
                mBurpAlpha -= 0.005;
                if (mBurpAlpha < 0.9 && theLex.mAnim == 0x17)
                {
                    theLex.PlayAnim(0x16, false);
                }
                if (mBurpAlpha < 0)
                {
                    mBurpCloud = false;
                }
                MarkDirty();
            }
        }

        /// <summary>
        /// Before the switch, while playing: from the second level on (after its first word) the next bonus word
        /// is announced in the balloon; when a fire tile first reaches the bottom row Lex cries out (CAREFUL,
        /// DANGER, LOOK OUT or WATCH OUT, anims 4/5/2/3) and then sparkles while idle; at the start Lex says "GO!"
        /// or "LET'S GO!" (anims 0/1). TODO: the hint tutorial, the title
        /// balloon (app + 0xcd).
        /// </summary>
        private void UpdatePlayingCues(Lex theLex)
        {
            if (mBonusWord.Length == 0 && (mLevel > 1 || (mLevel == 1 && mLevelStats.mWords > 0)))
            {
                mBonusWord = mNextBonusWord;
                mOtherBonusWord = mOtherNextBonusWord;
                if (mBonusWord.Length != 0)
                {
                    mApp.mWordBalloon.ShowBonusWord(mBonusWord);
                }
            }
            if (!theLex.mSparkle)
            {
                mDangerArmed = true;
            }
            else
            {
                if (mDangerArmed)
                {
                    mDangerArmed = false;
                    switch (mRand.Next() & 3)
                    {
                    case 0:
                        theLex.PlayAnim(4, false);
                        mApp.PlaySound(Res.SOUND_LEX_CAREFUL);
                        break;
                    case 1:
                        theLex.PlayAnim(5, false);
                        mApp.PlaySound(Res.SOUND_LEX_DANGER);
                        break;
                    case 2:
                        theLex.PlayAnim(2, false);
                        mApp.PlaySound(Res.SOUND_LEX_LOOKOUT);
                        break;
                    case 3:
                        theLex.PlayAnim(3, false);
                        mApp.PlaySound(Res.SOUND_LEX_WATCHOUT);
                        break;
                    }
                }
                if (theLex.IsIdle())
                {
                    theLex.PlayAnim(Lex.ANIM_SPARKLE, true);
                }
            }
            if (mApp.mGameMode == BookWormApp.GAME_ARCADE)
            {
                UpdateArcadeCountdown(theLex);
            }
            if (mStartCry)
            {
                mStartCry = false;
                if (mApp.mGameMode == BookWormApp.GAME_ARCADE)
                {
                    theLex.PlayAnim(0x11, false);
                    mApp.PlaySound(Res.SOUND_LEX_GETREADY);
                }
                else if (mRand.Next() % 100 < 0x32)
                {
                    theLex.PlayAnim(0, false);
                    mApp.PlaySound(Res.SOUND_LEX_GO);
                }
                else
                {
                    theLex.PlayAnim(1, false);
                    mApp.PlaySound(Res.SOUND_LEX_LETSGO);
                }
            }
        }

        public const int HINT_HOW_TO_PLAY = 0;

        public const int HINT_BURNING = 1;

        public const int HINT_BONUS_TILE = 2;

        public const int HINT_SCRAMBLE = 4;

        public const int HINT_FIRE_ALERT = 10;

        /// <summary>The hints' titles and texts by kind (in 0x9f68; 3 and 5-9 are left from the desktop game and never
        /// given here). Kind 10 has its Arcade text second.</summary>
        private static readonly string[][] HINTS =
        {
            new[] { "How to Play", "Link letters together to make words. Longer words are worth more points!" },
            new[] { "Watch out!", "Don't let burning tiles reach the bottom.  They will set the library on fire!\n\nHINT: Make longer words and you get fewer red tiles." },
            new[] { "Bonus Tile!", "Use that green tile in a word for extra bonus points." },
            new[] { "Super Bonus Tile!", "Gold tiles are worth mega points!\n\nCreate words with 5+ letters for a chance to earn more gold tiles!" },
            new[] { "Warning!", "Scrambling letters causes burning tiles to appear, so do it sparingly." },
            new[] { "Submit Button", "You may also submit selected words by clicking on the SUBMIT button." },
            new[] { "Submit by Clicking", "You may also submit selected words by clicking on the last tile in the word." },
            new[] { "Submit by Dragging", "You may also submit words by clicking the left mouse button and dragging to select tiles.\n\nRelease the mouse button to submit the word." },
            new[] { "Submit by Keyboard", "You may also submit selected words by pressing the ENTER key on the keyboard." },
            new[] { "Bonus Word!", "For even more points, submit a word that matches the Bonus Word." },
            new[] { "Fire Alert!", "That burning tile is too close!\n\nUse it this turn or your library will catch fire!", "That burning tile is too close!\n\nUse it soon or your library will catch fire!" },
        };

        /// <summary>A hint, the scramble question or a "Can't Scramble!" is up (0x200).</summary>
        public bool mHintShowing;

        private void ShowHint(int theKind)
        {
            if (theKind < 0 || theKind >= HINTS.Length)
            {
                return;
            }
            string[] aHint = HINTS[theKind];
            string aText = theKind == HINT_FIRE_ALERT && mApp.mGameMode == BookWormApp.GAME_ARCADE ? aHint[2] : aHint[1];
            ShowHintAlert(SexyAppBase.Loc(aHint[0]), SexyAppBase.Loc(aText));
        }

        /// <summary>FUN_00008ad4: with hints on, an alert with the hint: "Disable Hints" (1001) or "Continue" (1000).</summary>
        public void ShowHintAlert(string theTitle, string theText)
        {
            if (!mApp.mShowHints)
            {
                return;
            }
            mHintShowing = true;
            Alert anAlert = new Alert(0, this, mApp);
            anAlert.SetTitle(theTitle);
            anAlert.SetMessage(theText);
            anAlert.AddGreenButton(DIALOG_DISABLE_HINTS, SexyAppBase.Loc("Disable Hints"));
            anAlert.AddRedButton(DIALOG_CONTINUE, SexyAppBase.Loc("Continue"));
            anAlert.Show();
        }

        public const int DIALOG_CONTINUE = 1000;

        public const int DIALOG_DISABLE_HINTS = 1001;

        /// <summary>Lex may cry out about the fire again (0x2ed), and he has yet to say "GO!" (0x2ec).</summary>
        private bool mDangerArmed;

        private bool mStartCry = true;

        /// <summary>The level's seconds go to the totals, and the level (or game over) page shows (state 11).</summary>
        private void EndLevelClock()
        {
            mLevelSeconds = StopClock();
            mTotalSeconds += mLevelSeconds;
            mLevelTimeDone = true;
            mApp.GotoInterfaceState(11);
        }

        /// <summary>The fire warning (three times, 0x40 frames apart) and the rising tones of a word.</summary>
        private void UpdateSounds()
        {
            if (mWarnings > 0 && mState == STATE_PLAYING)
            {
                if (mWarningTimer > 0)
                {
                    mWarningTimer--;
                }
                if (mWarningTimer == 0)
                {
                    mWarnings--;
                    mApp.PlaySound(Res.SOUND_WARNING);
                    if (mWarnings > 0)
                    {
                        mWarningTimer = 0x40;
                    }
                }
            }
            if (mToneIndex < mToneCount)
            {
                if (mToneTimer > 0)
                {
                    mToneTimer--;
                }
                if (mToneTimer == 0)
                {
                    if (gTones == null)
                    {
                        gTones = new int[] { Res.SOUND_TONE_1, Res.SOUND_TONE_2, Res.SOUND_TONE_3, Res.SOUND_TONE_4, Res.SOUND_TONE_5, Res.SOUND_TONE_6, Res.SOUND_TONE_7, Res.SOUND_TONE_8 };
                    }
                    mApp.PlaySound(gTones[System.Math.Min(mToneIndex, 7)]);
                    mToneIndex++;
                    mToneTimer = 10;
                }
            }
        }

        /// <summary>
        /// Every other frame the level bar eases toward its target (a 25th of the gap + 0.01 up; 0.05 down, to
        /// empty once greyed), never past the target; held while 4 or more words are flying.
        /// </summary>
        private void UpdateLevelBar(int theFlying)
        {
            if (mLevelProgress == mLevelTarget || (mUpdateCnt & 1) != 0 || theFlying >= 4)
            {
                return;
            }
            MarkDirty();
            double aTarget = mGreyed ? 0.0 : mLevelTarget;
            double aGap = aTarget - mLevelProgress;
            if (aGap > 0)
            {
                mLevelProgress += aGap / 25.0 + 0.01;
            }
            else
            {
                mLevelProgress -= 0.05;
                if (mLevelProgress < mLevelTarget)
                {
                    mLevelProgress = mLevelTarget;
                }
            }
            if (mLevelTarget > 0 && mLevelProgress > mLevelTarget)
            {
                mLevelProgress = mLevelTarget;
            }
        }

        /// <summary>
        /// The shown score rolls up to the game's (by (gap / step / 100 + 4) steps a frame) and glows hotter
        /// while it does (+0.00375, to 0.85); settled, the glow cools (-0.00675 every 4th frame).
        /// </summary>
        private void UpdateScore()
        {
            int aScore = mGameStats.mScore;
            if (mDisplayScore == aScore || (mUpdateCnt & 1) != 0)
            {
                if (mScoreHeat > 0 && (mUpdateCnt & 3) == 0)
                {
                    mScoreHeat -= 0.00675;
                    if (mScoreHeat < 0)
                    {
                        mScoreHeat = 0;
                    }
                    MarkDirty();
                }
                return;
            }
            if (mDisplayScore < aScore)
            {
                mDisplayScore += (int)(mScoreStep * ((aScore - mDisplayScore) / mScoreStep / 100.0 + 4.0));
                mScoreHeat = System.Math.Min(mScoreHeat + 0.00375, 0.85);
            }
            if (mDisplayScore > aScore)
            {
                mDisplayScore = aScore;
            }
            MarkDirty();
        }

        /// <summary>
        /// While the score glows above 0.385, every 8th frame a glow rises off a random point along it (centred on
        /// ScreenPos + 0x160 / 0x164), with a gold ember flying off while it is still counting up.
        /// </summary>
        private void ScoreSparkles()
        {
            if (mScoreHeat <= 0.385 || (mUpdateCnt & 7) != 0)
            {
                return;
            }
            ScreenPos aPos = mApp.mScreenPos;
            int aWidth = System.Math.Max(Res.FONT_GOLDSCORE.StringWidth(mDisplayScore.ToString()), 1);
            int aX = aPos.At(0x160) - aWidth / 2 + (int)(mRand.Next() % (uint)aWidth);
            int aY = aPos.At(0x164);
            if (mDisplayScore != mGameStats.mScore)
            {
                Ember anEmber = mEmbers.Add();
                anEmber.mType = mRand.Next() % 100 > 79 ? 1 : 0;
                anEmber.mX = aX;
                anEmber.mY = aY;
                anEmber.mVelX = (mRand.Next() % 1000 - 500.0) / 800.0;
                anEmber.mVelY = (mRand.Next() % 1000 - 1000.0) / 1500.0;
                anEmber.mLoop = true;
                anEmber.mLife = 1.0;
                anEmber.mDecay = mRand.Next() % 1000 / 1000.0 * 0.025 + 0.01;
            }
            Ember aGlow = mEmbers.Add();
            aGlow.mType = Ember.TYPE_GLOW;
            aGlow.mX = aX - 8;
            aGlow.mY = aY - 8;
            aGlow.mVelY = -0.25;
            aGlow.mGravity = 0;
            aGlow.mLife = mRand.Next() % 1000 / 1000.0;
            aGlow.mDecay = mRand.Next() % 1000 / 1000.0 * 0.01 + 0.004;
        }

        /// <summary>
        /// The game-over burn: 0x3c frames after the fatal turn every tile catches fire (the 8-cel burn), the
        /// board greys and Lex with it; 0xa0 frames after it the game is over (state 2: level bar emptied, the
        /// game-over song, Classic 6 / Arcade 5). Both count only while Lex is idle or panicking.
        /// The catch: <see cref="CatchFire"/>; its white flash (0x290) fades by 0.01 a frame.
        /// (The fatal turn also starts timers 0x298/0x29c that count a value 0x2f8 nothing reads: dead.)
        /// </summary>
        private void UpdateGameOver(Lex theLex)
        {
            if (mGameOverTimer > 0 && theLex.IsIdleOrSparkling())
            {
                mGameOverTimer--;
                if (mGameOverTimer == 0)
                {
                    mState = STATE_GAME_OVER_START;
                    mLevelTarget = 0;
                    mApp.mMusicInterface?.PlayMusic(mApp.mGameMode == BookWormApp.GAME_ARCADE ? 5 : 6, 0, false);
                }
            }
            if (mCatchTimer > 0 && theLex.IsIdleOrSparkling())
            {
                mCatchTimer--;
                if (mCatchTimer == 0)
                {
                    CatchFire(theLex);
                }
            }
            if (mFlash > 0 && theLex.IsIdleOrSparkling())
            {
                mFlash -= 0.01;
                mOverlay.MarkDirty();
            }
        }

        /// <summary>
        /// 0x9f68 (0x2a4/0x2a8): a frame after the fatal turn (held while Lex is busy and a word flies, in Classic)
        /// SOUND_GAME_OVER_BURN plays; then, once a bottom fire tile blazes (0x2a8), the fire spreads sideways
        /// from its column (0x2ac += 0.0125 up to 1) and its flames scroll (0x2b4 += 48..95 every 8 frames).
        /// The overlay draws it.
        /// </summary>
        private void UpdateBurnSpread(Lex theLex, bool theFlying)
        {
            if (mBurnSoundTimer > 0)
            {
                if (theLex.IsIdle() || mApp.mGameMode == BookWormApp.GAME_ARCADE || !theFlying)
                {
                    if (--mBurnSoundTimer == 0)
                    {
                        mApp.PlaySound(Res.SOUND_GAME_OVER_BURN);
                    }
                    MarkDirty();
                }
                return;
            }
            if (mBurningPiece == null)
            {
                return;
            }
            if (mBurnSpread < 1.0)
            {
                mBurnSpread += 0.0125;
                if (mBurnSpread > 1.0)
                {
                    mBurnSpread = 1.0;
                }
            }
            if ((mUpdateCnt & 7) == 0)
            {
                mBurnScroll = (int)((mBurnScroll + mRand.Next() % 0x30 + 0x30) % (uint)AtlasResourceBase.IMAGE_GROUP51_01.mWidth);
            }
            MarkDirty();
        }

        /// <summary>A frame's delay before the game-over burn sound (0x2a4).</summary>
        public int mBurnSoundTimer;

        /// <summary>The blazing bottom fire tile the fire spreads from (0x2a8), how far (0x2ac, 0..1, times twice
        /// the screen width) and the flames' scroll (0x2b4).</summary>
        public Piece mBurningPiece;

        public double mBurnSpread;

        public int mBurnScroll;

        /// <summary>
        /// The fall loop: per column, bottom up, each tile falls (gravity ScreenPos float + 0x3f4) onto the one
        /// below, landing with a knock (at least + 0x3f8 fast); removed tiles sink (+ 0x3d4 a frame) and, once
        /// gone, the board refills.
        /// </summary>
        /// <param name="theBurnStep">Arcade: the timer ran out this frame or the countdown is past 0.</param>
        private void UpdatePieces(bool theBurnStep)
        {
            ScreenPos aPos = mApp.mScreenPos;
            double aGravity = aPos.FloatAt(0x3f4);
            double aHitSpeed = aPos.FloatAt(0x3f8);
            int aSinkSpeed = aPos.At(0x3d4);
            bool aRemoved = false;
            bool aCaught = false;
            bool aBurnSound = false;
            bool aDanger = false;
            mPiecesMoving = false;
            for (int aCol = 0; aCol < COLS; aCol++)
            {
                int aTargetY = aPos.At(0x17c) + mTileHeight * 6;
                if ((aCol & 1) == 0)
                {
                    aTargetY -= mTileHeight / 2;
                }
                for (int aRow = ROWS - 1; aRow >= 0; aRow--)
                {
                    Piece aPiece = mGrid[aRow, aCol];
                    if (aPiece == null)
                    {
                        continue;
                    }
                    if (aPiece.mArcadeBurn && ArcadeSmoke(aPiece))
                    {
                        aCaught = true;
                    }
                    if (aRow == ROWS - 1 && aPiece.mType == Piece.TYPE_FIRE)
                    {
                        aDanger = true;
                    }
                    if (aPiece.mFlipping)
                    {
                        aPiece.mFlipTime += 0.01;
                        if (aPiece.mFlipTime >= 1.0)
                        {
                            aPiece.mFlipTime = 0;
                            aPiece.mFlipping = false;
                        }
                        MarkDirty();
                    }
                    if (aPiece.mAnimating && (mUpdateCnt & 3) == 0)
                    {
                        aPiece.mAnimCel = (aPiece.mAnimCel + 1) % 8;
                        MarkDirty();
                    }
                    if ((aPiece.mType == Piece.TYPE_SAPPHIRE || aPiece.mType == Piece.TYPE_DIAMOND) && (mUpdateCnt & 0xf) == 0 && mRand.Next() % 10 == 0)
                    {
                        Sparkle aSparkle = new Sparkle();
                        aSparkle.mX = aPiece.mX + mRand.Next() % (uint)mTileWidth - mTileWidth / 2;
                        aSparkle.mY = aPiece.mY + mRand.Next() % (uint)mTileHeight - mTileHeight / 2;
                        mEffects.Add(aSparkle);
                    }
                    if (aRow == ROWS - 1 && aPiece.mType == Piece.TYPE_FIRE)
                    {
                        BottomFireEmbers(aPiece);
                    }
                    if (aPiece.mShaking && aTargetY <= aPiece.mY && (mUpdateCnt & 1) == 0)
                    {
                        aPiece.mX = GetColX(aPiece.mCol) + (mRand.Next() % 1000 - 500.0) / 500.0 * aPiece.mShakeAmount;
                    }
                    if (aTargetY <= aPiece.mY)
                    {
                        if (aPiece.mRemoving)
                        {
                            if (aPiece.mSinkDelay < 1)
                            {
                                if (aPiece.mSinkOffset == 0 && !aBurnSound)
                                {
                                    mApp.PlaySound(Res.SOUND_TILE_BURN);
                                    aBurnSound = true;
                                }
                                aPiece.mSinkOffset += aSinkSpeed;
                            }
                            else
                            {
                                aPiece.mSinkDelay--;
                            }
                            if (mTileHeight <= aPiece.mSinkOffset)
                            {
                                mGrid[aRow, aCol] = null;
                                aRemoved = true;
                            }
                        }
                    }
                    else
                    {
                        aPiece.mVelY += aGravity;
                        aPiece.mY += aPiece.mVelY;
                        MarkDirty();
                        if (!aPiece.mShaking)
                        {
                            mPiecesMoving = true;
                        }
                    }
                    if (aTargetY <= aPiece.mY && !aPiece.mRemoving)
                    {
                        if (aPiece.mVelY >= aHitSpeed && mUpdateCnt - mLastHitFrame > 3)
                        {
                            mApp.PlaySound(Res.SOUND_PIECE_HIT);
                            mLastHitFrame = mUpdateCnt;
                        }
                        aPiece.mVelY = 0;
                        aPiece.mY = aTargetY;
                    }
                    aTargetY = aTargetY - mTileHeight + aPiece.mSinkOffset;
                }
            }
            if (theBurnStep)
            {
                BurnStep(true);
            }
            if (aRemoved || aCaught || theBurnStep)
            {
                Refill();
                // A fire tile now in the bottom row sounds the Classic warning three times (0x2c4 = 3).
                if (mApp.mGameMode == BookWormApp.GAME_CLASSIC && mWarnings == 0)
                {
                    for (int aCol = 0; aCol < COLS; aCol++)
                    {
                        Piece aPiece = mGrid[ROWS - 1, aCol];
                        if (aPiece != null && aPiece.mType == Piece.TYPE_FIRE)
                        {
                            mWarnings = 3;
                            break;
                        }
                    }
                }
            }
            // The first time burning tiles have settled on the board: one warning and the hint.
            if (!mPiecesMoving && mFireTileCount > 0 && !mApp.mHintsSeen[BookWormApp.HINT_BURNING] && mHintTimer == 0)
            {
                mWarnings = 1;
                mHintTimer = 0x50;
                mHintKind = HINT_BURNING;
                mApp.mHintsSeen[BookWormApp.HINT_BURNING] = true;
            }
            UpdateDangerMusic(aDanger);
        }

        /// <summary>
        /// A fire tile in the bottom row smoulders while playing (now and then an ember and a glow rising off its
        /// lower edge); once the game is lost (state 3) it blazes, shaking harder each frame.
        /// </summary>
        private void BottomFireEmbers(Piece thePiece)
        {
            if (mState == STATE_PLAYING)
            {
                if ((mRand.Next() & 7) == 0)
                {
                    Ember anEmber = mEmbers.Add();
                    anEmber.mType = 0;
                    anEmber.mX = thePiece.mX + mRand.Next() % (uint)mTileWidth;
                    anEmber.mY = thePiece.mY + mTileHeight - 3.0;
                    anEmber.mVelX = (mRand.Next() % 1000 - 500.0) / 800.0;
                    anEmber.mVelY = (mRand.Next() % 1000 - 1000.0) / 1500.0;
                    anEmber.mLife = 0.65;
                    anEmber.mDecay = mRand.Next() % 1000 / 1000.0 * 0.002 + 0.002;
                }
                if ((mRand.Next() & 7) == 0)
                {
                    Ember aGlow = mEmbers.Add();
                    aGlow.mType = Ember.TYPE_GLOW;
                    aGlow.mX = thePiece.mX + mRand.Next() % (uint)mTileWidth;
                    aGlow.mY = thePiece.mY + mTileHeight - 3.0;
                    aGlow.mVelY = -0.25;
                    aGlow.mGravity = 0;
                    aGlow.mLife = mRand.Next() % 1000 / 1000.0;
                    aGlow.mDecay = aGlow.mLife * 0.01 + 0.004;
                }
            }
            else if (mState == STATE_GAME_OVER)
            {
                for (int i = 0; i < 2; i++)
                {
                    Ember anEmber = mEmbers.Add();
                    anEmber.mType = mRand.Next() % 100 > 79 ? 1 : 0;
                    anEmber.mX = thePiece.mX + mRand.Next() % (uint)mTileWidth;
                    anEmber.mY = thePiece.mY + mTileHeight;
                    anEmber.mVelX = (mRand.Next() % 1000 - 500.0) / 300.0;
                    anEmber.mVelY = (mRand.Next() % 500 - 500.0) / 300.0;
                    anEmber.mLife = 0.5;
                    anEmber.mDecay = mRand.Next() % 1000 / 1000.0 * 0.002 + 0.002;
                    anEmber.mLoop = true;
                }
                thePiece.mShakeAmount += 0.075;
                mBurningPiece = thePiece;
            }
            else
            {
                return;
            }
            if (!thePiece.mShaking)
            {
                thePiece.mShaking = true;
                thePiece.mShakeAmount = 1.5;
            }
        }

        /// <summary>
        /// The library catches (0x28c runs out): every tile back in its cell, plain and still, the first column and
        /// a sixteenth of the rest starting the (never drawn) burn cels; 700 flakes of ash drift down the board
        /// (a 30th type 2, a 20th type 1); the screen flashes white, the board and Lex go grey.
        /// </summary>
        private void CatchFire(Lex theLex)
        {
            ScreenPos aPos = mApp.mScreenPos;
            for (int aRow = 0; aRow < ROWS; aRow++)
            {
                for (int aCol = 0; aCol < COLS; aCol++)
                {
                    Piece aPiece = mGrid[aRow, aCol];
                    if (aPiece == null)
                    {
                        continue;
                    }
                    if (aCol == 0 || (mRand.Next() & 0xf) == 0)
                    {
                        aPiece.mAnimating = true;
                        aPiece.mAnimCel = (int)(mRand.Next() & 7);
                    }
                    aPiece.mX = GetColX(aCol);
                    aPiece.mY = GetRowY(aCol, aRow);
                    aPiece.mRemoving = false;
                    aPiece.mShaking = false;
                    aPiece.mType = Piece.TYPE_NORMAL;
                }
            }
            for (int i = 0; i < 700; i++)
            {
                Ember anEmber = mEmbers.Add();
                anEmber.mType = 0;
                anEmber.mX = mRand.Next() % (uint)mWidth;
                anEmber.mY = mRand.Next() % (uint)mHeight;
                anEmber.mGravity = mRand.Next() % 1000 / 1000.0 * 0.005 + 0.002;
                anEmber.mLife = 1.0;
                anEmber.mDecay = mRand.Next() % 1000 / 1000.0 * 0.001 + 0.00075;
                if (mRand.Next() % 30 == 0)
                {
                    anEmber.mType = 2;
                    anEmber.mGravity = mRand.Next() % 1000 / 1000.0 * 0.005 + 0.008;
                }
                else if (mRand.Next() % 20 == 0)
                {
                    anEmber.mType = 1;
                    anEmber.mGravity = mRand.Next() % 1000 / 1000.0 * 0.005 + 0.005;
                }
            }
            // TODO: the original also calls vf38(0) on the board's widgets and drops the entries of a list.
            mBurningPiece = null;
            mBurnSpread = 0;
            mFlash = 1.0;
            mGreyed = true;
            theLex.SetGrey();
            MarkDirty();
        }

        /// <summary>Lex sparkles and the music turns tense while a fire tile is in the bottom row (songs 0/3, Arcade 2/7).</summary>
        private void UpdateDangerMusic(bool theDanger)
        {
            mApp.mLex.SetSparkle(theDanger);
            MusicInterface aMusic = mApp.mMusicInterface;
            if (mApp.mGameMode == BookWormApp.GAME_ARCADE)
            {
                if (theDanger != aMusic.IsPlaying(7))
                {
                    aMusic.PlayMusic(theDanger ? 7 : 2, 0, true);
                }
            }
            else if (theDanger != aMusic.IsPlaying(3))
            {
                aMusic.PlayMusic(theDanger ? 3 : 0, 0, true);
            }
        }
    }
}
