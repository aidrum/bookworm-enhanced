namespace Sexy
{
    public partial class Board
    {
        /// <summary>Letter odds, A to Z, out of 95500 (0xccdec).</summary>
        private static readonly int[] LETTER_WEIGHTS =
        {
            9000, 2200, 2200, 4200, 12300, 2300, 3000, 2000, 7000, 900, 900, 4000, 2300,
            6000, 7000, 2300, 700, 6000, 4400, 6000, 4400, 1600, 1800, 500, 2000, 500
        };

        private const int LETTER_WEIGHT_TOTAL = 95500;

        /// <summary>Percent chance of a random fire tile per refill, by level (0xccdb8).</summary>
        private static readonly int[] FIRE_CHANCE = { 0, 0, 0, 2, 3, 4, 6, 8, 12, 16, 24, 32, 30 };

        /// <summary>What the fire timer gets back after it drops a fire tile, by level (0xccc2c).</summary>
        private static readonly double[] FIRE_TIMER_ADD = { 5.25, 4.0, 3.5, 3.0, 2.5, 2.0, 1.8, 1.6, 1.5, 1.4, 1.3, 1.2 };

        /// <summary>A random letter by the odds, never the same as the last one.</summary>
        private static char PickLetter()
        {
            int aLetter;
            do
            {
                int aRoll = (int)(gRand.Next() % LETTER_WEIGHT_TOTAL);
                aLetter = 0;
                while (aLetter < 25)
                {
                    aRoll -= LETTER_WEIGHTS[aLetter];
                    if (aRoll < 0)
                    {
                        break;
                    }
                    aLetter++;
                }
            }
            while (aLetter == gLastLetter);
            gLastLetter = aLetter;
            return (char)('A' + aLetter);
        }

        /// <summary>FUN_00006e18: a column's x.</summary>
        public int GetColX(int theCol)
        {
            return mTileWidth * theCol + mApp.mScreenPos.At(0x178);
        }

        /// <summary>FUN_00006ff8: a cell's y (even columns half a tile higher).</summary>
        public int GetRowY(int theCol, int theRow)
        {
            int aY = mTileHeight * theRow + mApp.mScreenPos.At(0x17c);
            if ((theCol & 1) == 0)
            {
                aY -= mTileHeight / 2;
            }
            return aY;
        }

        /// <summary>A new tile above the board in theCol, to fall into theRow.</summary>
        private Piece MakePiece(int theCol, int theRow, int theY)
        {
            Piece aPiece = new Piece();
            aPiece.mCol = theCol;
            aPiece.mRow = theRow;
            aPiece.mX = GetColX(theCol);
            aPiece.mY = theY;
            aPiece.mLetter = PickLetter();
            aPiece.mVelY = 0;
            return aPiece;
        }

        /// <summary>0x6bac: empties the board and drops in seven fresh columns.</summary>
        public void NewGame()
        {
            mLevelProgress = 0;
            for (int aRow = 0; aRow < mRows; aRow++)
            {
                for (int aCol = 0; aCol < mCols; aCol++)
                {
                    mGrid[aRow, aCol] = null;
                }
            }
            for (int aCol = 0; aCol < COLS; aCol++)
            {
                int aY = -0x40 - (int)(mRand.Next() & 0x3f);
                for (int aRow = ROWS - 1; aRow >= 0; aRow--)
                {
                    mGrid[aRow, aCol] = MakePiece(aCol, aRow, aY);
                    aY = aY - mTileHeight - (int)(mRand.Next() % 0x50);
                }
            }
        }

        /// <summary>FUN_0000xxxx (in 0x82e4): whether b may follow a in a word (staggered columns).</summary>
        public static bool IsAdjacent(Piece a, Piece b)
        {
            int aDCol = System.Math.Abs(a.mCol - b.mCol);
            int aDRow = a.mRow - b.mRow;
            if (aDCol == 0)
            {
                return System.Math.Abs(aDRow) == 1;
            }
            if (aDCol != 1)
            {
                return false;
            }
            if ((b.mCol & 1) == 0)
            {
                return aDRow == 0 || aDRow == -1;
            }
            return aDRow == 0 || aDRow == 1;
        }

        /// <summary>
        /// 0x82e4: the columns fall into the gaps and new tiles fill them from above. Classic mode may drop fire
        /// tiles into the top row (a random chance by level, and the fire timer); 1 in 20 new tiles is a bonus
        /// tile. A fire tile on top of a tile starts it burning. The selection is cut where tiles stopped being
        /// adjacent.
        /// </summary>
        public void Refill()
        {
            int aLevel = mLevel;
            int aChance = FIRE_CHANCE[System.Math.Min(aLevel, 12)];
            int aFireCount = 0;
            bool aClassic = mApp.mGameMode == BookWormApp.GAME_CLASSIC;
            if (!aClassic)
            {
                aChance = 0;
            }
            if (aChance > 0 && (int)(gRand.Next() % 100) < aChance)
            {
                if (mSkipFire)
                {
                    mSkipFire = false;
                }
                else
                {
                    aFireCount = 1;
                }
            }
            if (aLevel <= gLastLevel && mFireTimer <= 0 && aClassic)
            {
                aFireCount++;
                mFireTimer += FIRE_TIMER_ADD[System.Math.Min(aLevel, 11)];
            }
            gLastLevel = aLevel;
            bool aMoved = false;
            for (int aCol = 0; aCol < COLS; aCol++)
            {
                // Pack the column down.
                int aDest = ROWS - 1;
                for (int aRow = ROWS - 1; aRow >= 0; aRow--)
                {
                    Piece aPiece = mGrid[aRow, aCol];
                    if (aPiece == null)
                    {
                        continue;
                    }
                    if (aPiece.mRow != aDest)
                    {
                        aMoved = true;
                    }
                    mGrid[aRow, aCol] = null;
                    aPiece.mRow = aDest;
                    mGrid[aDest, aCol] = aPiece;
                    aDest--;
                }
                // New tiles above.
                if (aDest >= 0)
                {
                    int aY = -0x40 - (int)(mRand.Next() & 0x3f);
                    for (int aRow = aDest; aRow >= 0; aRow--)
                    {
                        Piece aPiece = MakePiece(aCol, aRow, aY);
                        if (aFireCount > 0 && aRow == 0)
                        {
                            aPiece.mType = Piece.TYPE_FIRE;
                            aPiece.mBurnStage = 0;
                            mFireTileCount++;
                            if (!mFireSoundPlayed)
                            {
                                mFireSoundPlayed = true;
                                mApp.PlaySound(Res.SOUND_FLAME_TILE);
                            }
                            aPiece.mLetter = PickLetter();
                            aFireCount--;
                        }
                        if (aPiece.mType == Piece.TYPE_NORMAL && gRand.Next() % 20 == 0)
                        {
                            aPiece.mType = Piece.TYPE_BONUS;
                            mApp.PlaySound(Res.SOUND_BONUS_TILE);
                            mApp.mLex.PlayAnim(0x14, false);
                            if (!mApp.mHintsSeen[BookWormApp.HINT_BONUS_TILE] && mHintTimer == 0)
                            {
                                mApp.mHintsSeen[BookWormApp.HINT_BONUS_TILE] = true;
                                mApp.SaveSettings();
                                mHintTimer = 0x50;
                                mHintKind = HINT_BONUS_TILE;
                            }
                        }
                        mGrid[aRow, aCol] = aPiece;
                        aY = aY - mTileHeight - (int)(mRand.Next() % 0x50);
                    }
                }
                // A fire tile burns the tile under it.
                for (int aRow = ROWS - 1; aRow >= 0; aRow--)
                {
                    Piece aPiece = mGrid[aRow, aCol];
                    if (aPiece == null || aPiece.mType != Piece.TYPE_FIRE)
                    {
                        continue;
                    }
                    if (aRow < ROWS - 1)
                    {
                        Piece aBelow = mGrid[aRow + 1, aCol];
                        if (aBelow != null && aBelow.mBurnStage < 1)
                        {
                            aBelow.mBurnStage = Piece.BurnStageFor(aBelow.mType);
                        }
                    }
                    else if (!mApp.mHintsSeen[BookWormApp.HINT_FIRE_ALERT])
                    {
                        mHintTimer = 0x1e;
                        mHintKind = HINT_FIRE_ALERT;
                        mApp.mHintsSeen[BookWormApp.HINT_FIRE_ALERT] = true;
                    }
                }
            }
            if (aMoved && mSelection.Count > 1)
            {
                // Cut the selection where it stopped being a chain.
                for (int i = 1; i < mSelection.Count; i++)
                {
                    if (!IsAdjacent(mSelection[i - 1], mSelection[i]))
                    {
                        for (int j = i; j < mSelection.Count; j++)
                        {
                            mSelection[j].mSelected = false;
                        }
                        mSelection.RemoveRange(i, mSelection.Count - i);
                        break;
                    }
                }
                // The original (0x82e4) re-selects the last tile through FUN_000099d4; here the word and its
                // balloon are refreshed without the select sound.
                mWordScore.Build(mSelection);
                mWordScore.Validate(mApp.mDictionary);
                mApp.mWordBalloon.ShowWord(mWordScore);
            }
        }

        /// <summary>
        /// FUN_000080e8, after each word in Classic: every fire tile not in the word burns into the tile below
        /// (its stage counts down; at 0 that tile sinks away). A fire tile left in the bottom row sets the library
        /// alight: game over (state 4). Arcade runs it on its timer instead: tiles burn under selected fire tiles too
        /// (a burn involving the word drops it), and the bottom row catches only once the countdown is past 0.
        /// </summary>
        public void BurnStep(bool theTurn)
        {
            bool anArcade = mApp.mGameMode == BookWormApp.GAME_ARCADE;
            for (int aCol = 0; aCol < COLS; aCol++)
            {
                for (int aRow = ROWS - 1; aRow >= 0; aRow--)
                {
                    Piece aFire = mGrid[aRow, aCol];
                    if (aFire == null || aFire.mType != Piece.TYPE_FIRE)
                    {
                        continue;
                    }
                    if (aRow < ROWS - 1)
                    {
                        Piece aBelow = aFire.mRow < ROWS - 1 ? mGrid[aFire.mRow + 1, aFire.mCol] : null;
                        if (aBelow == null || !theTurn || aBelow.mBurnStage < 1 || (!anArcade && aFire.mSelected))
                        {
                            continue;
                        }
                        aBelow.mBurnStage--;
                        if (aBelow.mBurnStage == 0 && anArcade && (aFire.mSelected || aBelow.mSelected))
                        {
                            ClearSelection();
                        }
                        if (aBelow.mBurnStage == 0 && !aBelow.mSelected)
                        {
                            mLevelStats.mField2c++;
                            mGameStats.mField2c++;
                            aBelow.mRemoving = true;
                            aBelow.mSinkOffset = 0;
                            aBelow.mSinkDelay = 0x14;
                        }
                    }
                    else if (anArcade ? mArcadeCountdown < 0 : !aFire.mSelected)
                    {
                        mFatalPiece = aFire;
                        mCatchTimer = 0x3c;
                        mGameOverTimer = 0xa0;
                        mBurnSoundTimer = 1;
                        aFire.mShaking = true;
                        mApp.mWordBalloon.Hide();
                        SetDisabled(true);
                        mState = STATE_BURNING;
                    }
                }
            }
        }
    }
}
