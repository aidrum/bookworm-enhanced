namespace Sexy
{
    /// <summary>
    /// Arcade mode on the board: a tile of the top row catches fire on a timer instead of fire tiles dropping, the
    /// fire tiles burn down on that timer instead of after each word, and a fire tile in the bottom row only sets the
    /// library alight once a ten-second countdown runs out.
    /// </summary>
    public partial class Board
    {
        /// <summary>The frames the arcade timer starts from (0x2bc); the chosen tile catches when it drops below
        /// <see cref="ARCADE_CATCH"/>.</summary>
        public const int ARCADE_TIMER = 1000;

        public const int ARCADE_CATCH = 666;

        /// <summary>Arcade's countdown while Lex is in danger (0x2c0); it is held at 10 otherwise.</summary>
        public const int ARCADE_COUNTDOWN = 10;

        /// <summary>
        /// The arcade part of Board::Update state 1 (in 0x9f68): when the timer (0x2bc) runs out it starts again and
        /// a tile of the top zigzag is chosen by letter weight to catch fire; the fire tiles burn down then (and every
        /// frame once the countdown is past 0). Returns whether to run the burn step.
        /// </summary>
        private bool ArcadeTick()
        {
            bool aBurn = mArcadeCountdown < 0;
            if (--mArcadeTimer < 1)
            {
                mArcadeTimer = ARCADE_TIMER;
                Piece[] aTop = { mGrid[1, 0], mGrid[0, 1], mGrid[1, 2], mGrid[0, 3], mGrid[1, 4], mGrid[0, 5], mGrid[1, 6] };
                int aTotal = 0;
                foreach (Piece aPiece in aTop)
                {
                    if (aPiece != null)
                    {
                        aTotal += LetterWeight(aPiece);
                    }
                }
                // As decompiled the roll keeps running down on the retries, so after a miss on a special tile the
                // first tile of the zigzag is taken if it is plain.
                int aRoll = aTotal > 0 ? (int)(gRand.Next() % (uint)aTotal) : 0;
                for (int i = 0; i < 7; i++)
                {
                    Piece aHit = null;
                    foreach (Piece aPiece in aTop)
                    {
                        if (aPiece != null)
                        {
                            aRoll -= LetterWeight(aPiece);
                            if (aRoll < 1)
                            {
                                aHit = aPiece;
                                break;
                            }
                        }
                    }
                    if (aHit != null && aHit.mType < Piece.TYPE_GOLD)
                    {
                        aHit.mArcadeBurn = true;
                        break;
                    }
                }
                aBurn = true;
            }
            return aBurn;
        }

        /// <summary>The letter's weight in the random letter table (0xccce8 indexed by the letter = 0xccdec).</summary>
        private static int LetterWeight(Piece thePiece)
        {
            // Port: Spanish takes the PC Spanish version's letter odds.
            int[] aWeights = Localization.IsSpanish ? Localization.SPANISH_LETTER_WEIGHTS : LETTER_WEIGHTS;
            int anIndex = thePiece.mLetter - 'A';
            return anIndex >= 0 && anIndex < aWeights.Length ? aWeights[anIndex] : 0;
        }

        /// <summary>
        /// A tile chosen to catch (Piece + 0xb0), in the piece loop of Board::Update: it smokes (an ember and a glow an
        /// eighth of the frames); once the arcade timer is below 666 it turns into a fire tile with a burst of 20 embers
        /// round its lower edge. Returns whether it caught.
        /// </summary>
        private bool ArcadeSmoke(Piece thePiece)
        {
            if ((mRand.Next() & 7) == 0)
            {
                Ember anEmber = mEmbers.Add();
                anEmber.mType = mRand.Next() % 100 > 0x4f ? 1 : 0;
                anEmber.mX = thePiece.mX + mRand.Next() % (uint)mTileWidth;
                anEmber.mY = thePiece.mY + mTileHeight - 3.0;
                anEmber.mVelX = (mRand.Next() % 1000 - 500.0) / 800.0;
                anEmber.mVelY = (mRand.Next() % 1000 - 1000.0) / 1500.0;
                anEmber.mLoop = true;
                anEmber.mLife = 1.0;
                anEmber.mDecay = mRand.Next() % 1000 / 1000.0 * 0.025 + 0.01;
                Ember aGlow = mEmbers.Add();
                aGlow.mType = Ember.TYPE_GLOW;
                aGlow.mX = anEmber.mX - 8.0;
                aGlow.mY = anEmber.mY - 8.0;
                aGlow.mLife = mRand.Next() % 1000 / 1000.0;
                aGlow.mDecay = mRand.Next() % 1000 / 1000.0 * 0.01 + 0.004;
                aGlow.mVelY = -0.25;
                aGlow.mGravity = 0;
            }
            if (mArcadeTimer >= ARCADE_CATCH)
            {
                return false;
            }
            thePiece.mArcadeBurn = false;
            thePiece.mType = Piece.TYPE_FIRE;
            thePiece.mBurnStage = 0;
            if (mSelection.Count > 0)
            {
                mWordScore.Build(mSelection);
                mWordScore.Validate(mApp.mDictionary);
            }
            thePiece.mNextLetter = thePiece.mLetter;
            thePiece.mFlipTime = 0;
            thePiece.mFlipping = true;
            if (!mFireSoundPlayed)
            {
                mFireSoundPlayed = true;
                mApp.PlaySound(Res.SOUND_FLAME_TILE);
            }
            mFireTileCount++;
            for (int i = 0; i < 20; i++)
            {
                double anAngle = i / 20.0 * -3.14159;
                Ember anEmber = mEmbers.Add();
                anEmber.mType = 2;
                anEmber.mX = thePiece.mX + mRand.Next() % (uint)mTileWidth;
                anEmber.mY = thePiece.mY + mTileHeight;
                anEmber.mVelX = System.Math.Cos(anAngle) * 1.5;
                anEmber.mVelY = System.Math.Sin(anAngle) * 2.5;
                anEmber.mLoop = true;
                anEmber.mLife = 1.0;
                anEmber.mDecay = mRand.Next() % 1000 / 1000.0 * 0.01 + 0.02;
                anEmber.mDelay = 8;
                Ember aCopy = CopyEmber(anEmber, 1);
                aCopy.mDelay = 4;
                CopyEmber(anEmber, 2).mDelay = 0;
                Ember aGlow = mEmbers.Add();
                aGlow.mType = Ember.TYPE_GLOW;
                aGlow.mX = thePiece.mX + mRand.Next() % (uint)mTileWidth;
                aGlow.mY = thePiece.mY + mTileHeight - 3.0;
                aGlow.mLife = mRand.Next() % 1000 / 1000.0;
                aGlow.mDecay = mRand.Next() % 1000 / 1000.0 * 0.01 + 0.004;
                aGlow.mVelY = -0.25;
                aGlow.mGravity = 0;
            }
            return true;
        }

        /// <summary>A looping ember of theType starting where theEmber does.</summary>
        private Ember CopyEmber(Ember theEmber, int theType)
        {
            Ember anEmber = mEmbers.Add();
            anEmber.mType = theType;
            anEmber.mX = theEmber.mX;
            anEmber.mY = theEmber.mY;
            anEmber.mVelX = theEmber.mVelX;
            anEmber.mVelY = theEmber.mVelY;
            anEmber.mLoop = true;
            anEmber.mLife = theEmber.mLife;
            anEmber.mDecay = theEmber.mDecay;
            return anEmber;
        }

        /// <summary>
        /// The arcade countdown in the state-1 cues: held at 10 while Lex is out of danger; in danger, every 100th
        /// frame it shows as a Lockergnome popup with SOUND_WARNING and counts down. Past 0 the word is dropped and the
        /// burn step runs every frame (so the fire tile in the bottom row sets the library alight).
        /// </summary>
        private void UpdateArcadeCountdown(Lex theLex)
        {
            if (!theLex.mSparkle)
            {
                if (mArcadeCountdown < ARCADE_COUNTDOWN)
                {
                    mArcadeCountdown = ARCADE_COUNTDOWN;
                }
            }
            else if (mUpdateCnt % 100 == 0)
            {
                AddPopup(mArcadeCountdown.ToString(), 0x8e);
                mApp.PlaySound(Res.SOUND_WARNING);
                if (--mArcadeCountdown < 0)
                {
                    ClearSelection();
                }
            }
        }

        /// <summary>Board::Update state 6: a level made while the countdown ran stops it, with "TIME BONUS!" when Lex
        /// was still in danger (anim 7, SOUND_WORD_COMPLETE). No points are given.</summary>
        private void ArcadeTimeBonus()
        {
            if (mArcadeCountdown >= ARCADE_COUNTDOWN)
            {
                return;
            }
            mArcadeCountdown = ARCADE_COUNTDOWN;
            if (mApp.mGameMode == BookWormApp.GAME_ARCADE && mApp.mLex.mSparkle)
            {
                AddPopup("TIME BONUS!", 0xa0);
                mApp.mLex.PlayAnim(7, false);
                mApp.PlaySound(Res.SOUND_WORD_COMPLETE);
            }
        }
    }
}
