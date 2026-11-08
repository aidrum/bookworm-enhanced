using System.Collections.Generic;

namespace Sexy
{
    public partial class Board
    {
        /// <summary>How the current touch is being used (0x1dc): 0 picking, 1 released, 2 dragging, 3 on the last tile.</summary>
        public int mTouchMode;

        /// <summary>Port (Submit by Dragging): the current touch has dragged. mTouchMode stays 2 after a drag
        /// ends, so it can't tell a later tap from a drag.</summary>
        private bool mTouchDragged;

        /// <summary>The word being spelled (0x214).</summary>
        public WordScore mWordScore;

        /// <summary>The game's and the level's totals (0x260, 0x264).</summary>
        public StatKeeper mGameStats;

        public StatKeeper mLevelStats;

        /// <summary>The bonus word to make (0x25c; empty until announced), and the next one (0x258).</summary>
        public string mBonusWord = "";

        public string mNextBonusWord = "";

        /// <summary>
        /// Port: the bonus word and the next one of the languages played before ('|'-separated, by
        /// Localization.LANGUAGES index), swapped in when the language changes back (a missing or dropped one is
        /// picked then). The current language's slot is stale: mBonusWord / mNextBonusWord hold it.
        /// </summary>
        public string mLanguageBonusWords = "";

        public string mLanguageNextBonusWords = "";

        /// <summary>Port: the language (Localization.LANGUAGES index) mBonusWord and mNextBonusWord are in.</summary>
        public int mBonusWordLanguage;

        /// <summary>Bonus words picked (0x254), for their length.</summary>
        public int mBonusWordsPicked;

        /// <summary>The latest "+N BONUS!" style popup (0x338), the next one goes below it.</summary>
        public Points mBonusPopup;

        /// <summary>Bonus word lengths by words picked so far (0xcce54).</summary>
        private static readonly int[] BONUS_WORD_LENGTHS = { 3, 3, 3, 3, 3, 4, 4, 4, 5, 5 };

        /// <summary>A bonus word's points by bonus words made this game (0xccd8c).</summary>
        private static readonly int[] BONUS_WORD_POINTS = { 1000, 2000, 4000, 6000, 10000, 15000, 20000, 25000, 30000, 40000, 50000 };

        /// <summary>
        /// FUN_000042f4: the next bonus word, of the length for the words picked so far: up to as many random
        /// tries as there are bonus words for one of that length other than the current one.
        /// </summary>
        public void PickBonusWord()
        {
            SyncBonusWordLanguage();
            int aLength = BONUS_WORD_LENGTHS[System.Math.Min(mBonusWordsPicked, 9)];
            string aWord = PickBonusWord(mApp.mBonusWords, aLength, mNextBonusWord, out _);
            if (aWord != null)
            {
                mNextBonusWord = aWord;
                mBonusWordsPicked++;
                // Port: only the current language's words are loaded, so the other languages' next words are
                // dropped; SyncBonusWordLanguage picks one of this length (not by gRand) if the language changes.
                mLanguageNextBonusWords = "";
            }
        }

        /// <summary>
        /// FUN_000042f4's tries: a random word of theLength from theWords other than theLast, or null. theRoll is
        /// the random number that picked it.
        /// </summary>
        private static string PickBonusWord(List<string> theWords, int theLength, string theLast, out uint theRoll)
        {
            for (int aTries = theWords.Count; aTries > 0; aTries--)
            {
                theRoll = gRand.Next();
                string aWord = theWords[(int)(theRoll % (uint)theWords.Count)];
                if (aWord.Length == theLength && aWord != theLast)
                {
                    return aWord;
                }
            }
            theRoll = 0;
            return null;
        }

        /// <summary>Port: the word of theLength other than theLast that theRoll picks among theWords' such words, or null.</summary>
        private static string PickBonusWordByRoll(List<string> theWords, int theLength, string theLast, uint theRoll)
        {
            List<string> aMatches = theWords.FindAll(aWord => aWord.Length == theLength && aWord != theLast);
            return aMatches.Count == 0 ? null : aMatches[(int)(theRoll % (uint)aMatches.Count)];
        }

        /// <summary>Port: rolls for the bonus words that saves from before the Spanish mode lack (not gRand, so the
        /// game's random sequence is not disturbed).</summary>
        private static readonly System.Random gBonusFillRand = new System.Random();

        /// <summary>Port: language theIndex's word in theSlots (mLanguageBonusWords or mLanguageNextBonusWords), or "".</summary>
        private static string GetBonusSlot(string theSlots, int theIndex)
        {
            string[] aWords = theSlots.Split('|');
            return theIndex < aWords.Length ? aWords[theIndex] : "";
        }

        /// <summary>Port: theSlots with language theIndex's word set to theWord.</summary>
        private static string SetBonusSlot(string theSlots, int theIndex, string theWord)
        {
            List<string> aWords = new List<string>(theSlots.Split('|'));
            while (aWords.Count <= theIndex)
            {
                aWords.Add("");
            }
            aWords[theIndex] = theWord;
            return string.Join("|", aWords);
        }

        /// <summary>
        /// Port: when the language is no longer the bonus words', the old language's are stored in their slots and
        /// the new one's swapped in. One missing there (a game saved before it was picked) is picked now, as long
        /// as the old one.
        /// </summary>
        public void SyncBonusWordLanguage()
        {
            int aLanguage = Localization.CurrentIndex();
            if (mBonusWordLanguage == aLanguage)
            {
                return;
            }
            string anOldWord = mBonusWord;
            string anOldNext = mNextBonusWord;
            mLanguageBonusWords = SetBonusSlot(mLanguageBonusWords, mBonusWordLanguage, anOldWord);
            mLanguageNextBonusWords = SetBonusSlot(mLanguageNextBonusWords, mBonusWordLanguage, anOldNext);
            mBonusWord = GetBonusSlot(mLanguageBonusWords, aLanguage);
            mNextBonusWord = GetBonusSlot(mLanguageNextBonusWords, aLanguage);
            mBonusWordLanguage = aLanguage;
            if (mBonusWord.Length == 0 && anOldWord.Length != 0)
            {
                mBonusWord = PickBonusWordByRoll(mApp.mBonusWords, anOldWord.Length, "", (uint)gBonusFillRand.Next()) ?? "";
            }
            if (mNextBonusWord.Length == 0 && anOldNext.Length != 0)
            {
                mNextBonusWord = PickBonusWordByRoll(mApp.mBonusWords, anOldNext.Length, mBonusWord, (uint)gBonusFillRand.Next()) ?? "";
            }
        }

        /// <summary>
        /// FUN_000093ec: the bonus word made. Lex cheers (anim 7, SOUND_BONUSWORD), "+N BONUS!" rises from the
        /// grid's left edge under the word (or under the last such popup), the points go to the level and game,
        /// and the next bonus word is picked (to be announced again).
        /// </summary>
        public void BonusWordMade(int thePoints, int theY)
        {
            mApp.PlaySound(Res.SOUND_BONUSWORD);
            mApp.mLex.PlayAnim(7, false);
            AddPopup(string.Format(SexyAppBase.Loc("+{0} BONUS!"), thePoints), theY + 0x3c);
            mLevelStats.mScore += thePoints;
            mLevelStats.mBonusWords++;
            mGameStats.mScore += thePoints;
            mGameStats.mBonusWords++;
            if (mBonusWord.Length != 0)
            {
                PickBonusWord();
                mBonusWord = "";
                mLanguageBonusWords = "";
            }
        }

        /// <summary>A left-aligned Lockergnome popup at the grid's left edge, at theY or below the last one.</summary>
        private void AddPopup(string theText, int theY)
        {
            if (mBonusPopup != null && mBonusPopup.mLife > 0 && theY < FromShown(mBonusPopup.mY) + 0x24)
            {
                theY = FromShown(mBonusPopup.mY) + 0x24;
            }
            Points aPopup = new Points(mApp, Res.FONT_LOCKERGNOME, theText, mApp.mScreenPos.At(0x178), theY, 200, -1);
            ShowOnBoard(aPopup);
            mPointsList.Add(aPopup);
            AddWidget(aPopup);
            mBonusPopup = aPopup;
        }

        /// <summary>Some tiles of the word were fire tiles (0x2fc).</summary>
        public bool mBurnedWord;

        /// <summary>Fire-timer change by word length 3..8+ (0xccd5c).</summary>
        private static readonly double[] FIRE_TIMER_BY_LENGTH = { -0.5, 0.35, 1.45, 1.85, 3.0, 4.75 };

        /// <summary>The tile at a point.</summary>
        private Piece GetPieceAt(int x, int y)
        {
            for (int aRow = 0; aRow < ROWS; aRow++)
            {
                for (int aCol = 0; aCol < COLS; aCol++)
                {
                    Piece aPiece = mGrid[aRow, aCol];
                    if (aPiece != null && aPiece.mX <= x && aPiece.mY <= y && x < aPiece.mX + mTileWidth && y < aPiece.mY + mTileHeight)
                    {
                        return aPiece;
                    }
                }
            }
            return null;
        }

        /// <summary>Drops the selection and the word (the reset at 0x9c24).</summary>
        private void ClearSelection()
        {
            foreach (Piece aPiece in mSelection)
            {
                aPiece.mSelected = false;
            }
            mSelection.Clear();
            mWordScore.Build(null);
            mTouchMode = 0;
            MarkDirty();
            mApp.mWordBalloon.Hide();
        }

        /// <summary>
        /// 0x9b58: picking tiles. While picking, any tile adds; later a touch on the only tile drops it, on the
        /// first tile restarts from it, on the last tile arms the submit, next to the last extends the word, on
        /// a tile of the word cuts back to it, elsewhere drops the word. Tiles over a sinking tile don't respond.
        /// </summary>
        public override void MouseDown(int x, int y, int theClickCount)
        {
            x = FromShown(x);
            y = FromShown(y);
            base.MouseDown(x, y, theClickCount);
            mTouchDragged = false;
            if (mState != STATE_PLAYING)
            {
                return;
            }
            PieceDown(GetPieceAt(x, y));
        }

        /// <summary>Port: the tap logic of MouseDown on aPiece (null: off the tiles); the selector's A taps with it.</summary>
        private void PieceDown(Piece aPiece)
        {
            if (aPiece == null)
            {
                ClearSelection();
                return;
            }
            for (int aRow = aPiece.mRow; aRow < ROWS; aRow++)
            {
                Piece aBelow = mGrid[aRow, aPiece.mCol];
                if (aBelow != null && aBelow.mRemoving)
                {
                    return;
                }
            }
            if (mTouchMode == 0 || mSelection.Count == 0)
            {
                SelectPiece(aPiece);
                return;
            }
            if (aPiece == mSelection[0])
            {
                if (mSelection.Count != 1)
                {
                    SelectPiece(aPiece);
                    mTouchMode = 0;
                    return;
                }
                ClearSelection();
                return;
            }
            Piece aLast = mSelection[mSelection.Count - 1];
            if (aPiece == aLast)
            {
                mTouchMode = 3;
                return;
            }
            if (IsAdjacent(aLast, aPiece) || aPiece.mSelected)
            {
                SelectPiece(aPiece);
                return;
            }
            ClearSelection();
        }

        /// <summary>0x9dfc: lifting off the last tile submits the word; lifting after picking ends the pick.</summary>
        public override void MouseUp(int x, int y, int theClickCount)
        {
            x = FromShown(x);
            y = FromShown(y);
            base.MouseUp(x, y, theClickCount);
            if (mState == STATE_PLAYING)
            {
                if (mTouchMode == 3)
                {
                    if (mSelection.Count > 0)
                    {
                        Piece aLast = mSelection[mSelection.Count - 1];
                        SubmitWord((int)(aLast.mX + mTileWidth / 2), (int)(aLast.mY + mTileHeight / 2));
                    }
                }
                else if (mTouchMode == 0 && mSelection.Count > 0)
                {
                    mTouchMode = 1;
                }
                else if (mTouchMode == 2 && mTouchDragged && mApp.mSubmitByDragging && mSelection.Count > 0 && mWordScore.mValid)
                {
                    // Port (Submit by Dragging): a touch that dragged (from any tile of the word, or a new one)
                    // submits the word when let go if it is valid; a tap never does, even after an earlier drag.
                    Piece aLast = mSelection[mSelection.Count - 1];
                    SubmitWord((int)(aLast.mX + mTileWidth / 2), (int)(aLast.mY + mTileHeight / 2));
                }
            }
            MarkDirty();
        }

        /// <summary>0x9e98: dragging extends the word to the neighbour the drag points at.</summary>
        public override void MouseDrag(int x, int y)
        {
            x = FromShown(x);
            y = FromShown(y);
            base.MouseDrag(x, y);
            if (mState != STATE_PLAYING || mSelection.Count == 0)
            {
                return;
            }
            Piece aLast = mSelection[mSelection.Count - 1];
            Piece aTarget = GetDragTarget(x, y, aLast);
            if (aTarget == null || aTarget == aLast)
            {
                return;
            }
            mTouchMode = 2;
            mTouchDragged = true;
            if (IsAdjacent(aLast, aTarget) && !aTarget.mRemoving)
            {
                SelectPiece(aTarget);
            }
        }

        /// <summary>
        /// FUN_0000701c: with the drag 0.75 to 1.5 tiles from the tile's centre, the tile it points at: sideways
        /// (half a tile up or down) when mostly horizontal, else straight up or down.
        /// </summary>
        private Piece GetDragTarget(int x, int y, Piece theFrom)
        {
            int aCenterX = (int)(theFrom.mX + mTileWidth / 2);
            int aCenterY = (int)(theFrom.mY + mTileHeight / 2);
            float aDX = x - aCenterX;
            float aDY = y - aCenterY;
            float aDist = (float)System.Math.Sqrt(aDX * aDX + aDY * aDY);
            if (aDist <= mTileWidth * 0.75f || aDist >= mTileWidth * 1.5f)
            {
                return null;
            }
            int aStepY = mTileHeight;
            int aStepX = 0;
            if (System.Math.Abs(aDY + aDY) <= System.Math.Abs(aDX * 3f))
            {
                aStepY = mTileHeight / 2;
                aStepX = aDX <= 0 ? -mTileWidth : mTileWidth;
            }
            int aSign = aDY < 0 ? -1 : 1;
            return GetPieceAt(aCenterX + aStepX, aCenterY + aStepY * aSign);
        }

        /// <summary>
        /// FUN_000099d4: theTile becomes the word's last tile (cutting the word back if it was already in it); the
        /// word is rebuilt, checked and shown in Lex's balloon. A valid word pulses the balloon and a glow over the
        /// tile, and restarts the tap-to-submit wait; one that just became valid with special tiles chimes.
        /// </summary>
        public void SelectPiece(Piece thePiece)
        {
            int anIndex = mSelection.IndexOf(thePiece);
            if (anIndex >= 0)
            {
                for (int i = anIndex; i < mSelection.Count; i++)
                {
                    mSelection[i].mSelected = false;
                }
                mSelection.RemoveRange(anIndex, mSelection.Count - anIndex);
            }
            bool aWasSelected = thePiece.mSelected;
            thePiece.mSelected = true;
            mSelection.Add(thePiece);
            mWordScore.Build(mSelection);
            int[] aCounts = mWordScore.mTypeCounts;
            bool aPlain = aCounts[Piece.TYPE_BONUS] < 1 && aCounts[Piece.TYPE_GOLD] < 1 && aCounts[Piece.TYPE_SAPPHIRE] < 1 &&
                aCounts[Piece.TYPE_DIAMOND] < 1;
            mWordScore.Validate(mApp.mDictionary);
            mApp.mWordBalloon.ShowWord(mWordScore);
            int aSound = Res.SOUND_NEXT_SELECT;
            if (mWordScore.mValid)
            {
                mApp.mWordBalloon.PulseWord();
                mGlowPulse.Resize(ToShown(thePiece.mX), ToShown(thePiece.mY), ToShown(mTileWidth), ToShown(mTileHeight));
                mGlowPulse.Pulse();
                mSubmitWaitCount = 0;
                mSubmitArrow = false;
                if (!aWasSelected && !aPlain)
                {
                    aSound = Res.SOUND_BONUS_COMPLETE;
                }
            }
            mApp.PlaySound(aSound);
            MarkDirty();
        }

        /// <summary>
        /// FUN_00008d20: submitting the word. A word not in the dictionary buzzes. A real one: Lex may define it,
        /// the totals and bests count it, the fire timer moves by its length, the bonus word pays out, and its
        /// tiles leave the board, which refills. A word new to a book collection shows the collection (state 8).
        /// </summary>
        public void SubmitWord(int x, int y)
        {
            mHintIdleCount = 0;
            if (!mWordScore.mValid)
            {
                if (mTouchMode == 3 && mSelection.Count > 0)
                {
                    mApp.PlaySound(Res.SOUND_BUZZER);
                }
                mTouchMode = 1;
                mFireSoundPlayed = false;
                MarkDirty();
                return;
            }
            if (mApp.mShowDefinitions)
            {
                string aDefinition = mApp.mDefinitionator.GetDefinition(mWordScore.mWord);
                if (aDefinition.Length != 0)
                {
                    mApp.mLex.SetPendingSpeech(aDefinition, mWordScore.mWord);
                }
            }
            mLevelStats.AddWord(mWordScore);
            mGameStats.AddWord(mWordScore);
            mApp.mPersonalBests.UpdateHighScore(mApp.mGameMode, mGameStats.mScore);
            mApp.mPersonalBests.UpdateWord(mApp.mGameMode, mWordScore.mWord, mWordScore.mScore);
            int aLength = mWordScore.mWord.Length;
            if (mWordScore.mTypeCounts[3] > 1)
            {
                mBurnedWord = true;
            }
            int anIndex = System.Math.Min(aLength - 3, 5);
            if (mWordScore.mTypeCounts[3] == 0 || anIndex != 0)
            {
                mFireTimer += FIRE_TIMER_BY_LENGTH[anIndex];
            }
            if (string.Equals(mWordScore.mMatch, mBonusWord, System.StringComparison.OrdinalIgnoreCase))
            {
                BonusWordMade(BONUS_WORD_POINTS[System.Math.Min(mGameStats.mBonusWords, 10)], y);
                mFireTimer += aLength;
                mSkipFire = true;
            }
            if (mApp.mGameMode == BookWormApp.GAME_CLASSIC)
            {
                BurnStep(true);
            }
            mFireTimer -= 1.0;
            mToneIndex = 0;
            mToneCount = aLength;
            mToneTimer = 0x14;
            mApp.PlaySound(mWordScore.mTypeCounts[3] == 0 ? Res.SOUND_WORD_COMPLETE : Res.SOUND_RED_TILE_CLEARED);
            foreach (Piece aPiece in mSelection)
            {
                mGrid[aPiece.mRow, aPiece.mCol] = null;
                if (aPiece.mType == Piece.TYPE_FIRE)
                {
                    mFireTileCount--;
                    Piece aBelow = aPiece.mRow < ROWS - 1 ? mGrid[aPiece.mRow + 1, aPiece.mCol] : null;
                    if (aBelow != null)
                    {
                        aBelow.mBurnStage = Piece.BurnStageFor(aBelow.mType);
                    }
                }
            }
            // The tiles fly off into Lex, in screen space (the overlay).
            CGPoint anAbs = GetAbsPos();
            foreach (Piece aPiece in mSelection)
            {
                // Port: from where the (maybe scaled: wide game layout) board shows them.
                aPiece.mX = ToShown(aPiece.mX) + (int)anAbs.mX;
                aPiece.mY = ToShown(aPiece.mY) + (int)anAbs.mY;
            }
            mOverlay.AddEffect(new FlyingPieceList(mSelection));
            mSelection.Clear();
            mLevelTarget = GetLevelFill();
            if (mApp.mCollectionator.RecordWord(mWordScore.mMatch) == Collectionator.RECORD_NEW)
            {
                mState = STATE_COLLECTION;
            }
            Refill();
            RewardTile(mWordScore);
            AddPoints(mWordScore.mScore, x, y);
            mWordScore.Build(null);
            mTouchMode = 0;
            mFireSoundPlayed = false;
            MarkDirty();
            mApp.mWordBalloon.Hide();
        }

        /// <summary>Play frames since the last word (0x2d4), for the "really stuck" hint.</summary>
        public int mHintIdleCount;

        /// <summary>The "+N" popups drifting up the board (0x330).</summary>
        public List<Points> mPointsList = new List<Points>();

        /// <summary>Points shown in popups this game (0x320), the most for one word this level and game (0x350,
        /// 0x354).</summary>
        public int mPointsShown;

        public int mBestPointsLevel;

        public int mBestPointsGame;

        /// <summary>Words in a row that earned a reward tile (0x1f4).</summary>
        public int mRewardStreak;

        /// <summary>Lex has praise to give (0x2f0), and for which tile (0x2f4: 2 gold, 4 sapphire, 5 diamond).</summary>
        public bool mPraisePending;

        public int mPraiseKind;

        /// <summary>
        /// FUN_00006920: "+N" in the gold score font, centred on the tap, floating for N/3 frames (150 to 600).
        /// </summary>
        public void AddPoints(int thePoints, int theX, int theY)
        {
            int aLife = System.Math.Clamp(thePoints / 3, 150, 600);
            int aPoints = (int)(thePoints * mScoreStep);
            Points aPopup = new Points(mApp, Res.FONT_GOLDSCORE, string.Format("+{0}", thePoints), theX, theY, aLife, 0);
            ShowOnBoard(aPopup);
            mPointsList.Add(aPopup);
            AddWidget(aPopup);
            mBestPointsLevel = System.Math.Max(mBestPointsLevel, aPoints);
            mBestPointsGame = System.Math.Max(mBestPointsGame, aPoints);
            mPointsShown += aPoints;
        }

        /// <summary>Drifts the popups up a pixel every other frame and drops the spent ones (in 0x9f68).</summary>
        private void UpdatePoints()
        {
            for (int i = 0; i < mPointsList.Count; i++)
            {
                Points aPopup = mPointsList[i];
                aPopup.MarkDirty();
                aPopup.mHue = (aPopup.mHue + 7) % 0xff;
                if ((mUpdateCnt & 1) == 0 && aPopup.mY > 0)
                {
                    aPopup.Move(aPopup.mX, aPopup.mY - 1);
                }
                if (--aPopup.mLife < 1)
                {
                    mPointsList.RemoveAt(i--);
                    RemoveWidget(aPopup);
                    mApp.SafeDeleteWidget(aPopup);
                }
            }
        }

        /// <summary>
        /// FUN_0000963c: a long word turns a random plain tile precious. By length: 4 letters 1% gold, 5 20%
        /// gold, 6 gold, 7 gold (1 in 4) or sapphire, 8 gold or sapphire, 9 sapphire, 10 sapphire or diamond.
        /// Two or more sapphires, or three or more gold, in the word earn a diamond; two gold at least a sapphire;
        /// three or more bonus tiles at least gold or sapphire. Rewarded words in a row (with the app's streak
        /// option, app + 0x1228, on by default) earn at least a sapphire from the third and a diamond from the
        /// fifth. The tile flips over to show it (SOUND_SPIN_SUPER_BONUS) and Lex has praise for it. Two or
        /// more diamonds in the word earn praise too.
        /// </summary>
        private void RewardTile(WordScore theWord)
        {
            int aBase = 0;
            switch (theWord.mWord.Length)
            {
            case 4:
                aBase = gRand.Next() % 100 == 0 ? 2 : 0;
                break;
            case 5:
                aBase = gRand.Next() % 5 == 0 ? 2 : 0;
                break;
            case 6:
                aBase = 2;
                break;
            case 7:
                aBase = (gRand.Next() & 3) == 0 ? 2 : 4;
                break;
            case 8:
                aBase = (int)(gRand.Next() & 1) * 2 + 2;
                break;
            case 9:
                aBase = 4;
                break;
            case 10:
                aBase = (int)(gRand.Next() & 1) | 4;
                break;
            }
            int[] aCounts = theWord.mTypeCounts;
            if (aCounts[Piece.TYPE_DIAMOND] > 1)
            {
                mPraisePending = true;
                mPraiseKind = aBase;
            }
            int aReward;
            if (aCounts[Piece.TYPE_SAPPHIRE] >= 2 || aCounts[Piece.TYPE_GOLD] > 2)
            {
                aReward = 5;
            }
            else
            {
                aReward = aBase;
                if (aCounts[Piece.TYPE_GOLD] < 2)
                {
                    if (aCounts[Piece.TYPE_BONUS] > 2)
                    {
                        int anAlt = (int)(gRand.Next() & 1) * 2 + 2;
                        aReward = System.Math.Max(aReward, anAlt);
                    }
                }
                else
                {
                    aReward = System.Math.Max(aReward, 4);
                }
            }
            if (aReward != 0 && mApp.mRewardStreaks)
            {
                mRewardStreak++;
                if (mRewardStreak >= 5)
                {
                    aReward = System.Math.Max(aReward, 5);
                }
                else if (mRewardStreak > 2)
                {
                    aReward = System.Math.Max(aReward, 4);
                }
            }
            else
            {
                mRewardStreak = 0;
            }
            if (aReward == 0)
            {
                return;
            }
            List<Piece> aPlain = new List<Piece>();
            for (int aCol = 0; aCol < COLS; aCol++)
            {
                for (int aRow = ROWS - 1; aRow >= 0; aRow--)
                {
                    Piece aPiece = mGrid[aRow, aCol];
                    if (aPiece != null && aPiece.mType == Piece.TYPE_NORMAL && !aPiece.mRemoving && !aPiece.mArcadeBurn)
                    {
                        aPlain.Add(aPiece);
                    }
                }
            }
            if (aPlain.Count == 0)
            {
                return;
            }
            Piece aLucky = aPlain[(int)(gRand.Next() % (uint)aPlain.Count)];
            if (aReward < 6)
            {
                aLucky.mType = aReward;
                aLucky.mBurnStage = 0;
            }
            if (mSelection.Count > 0)
            {
                mWordScore.Build(mSelection);
                mWordScore.Validate(mApp.mDictionary);
            }
            mApp.PlaySound(Res.SOUND_SPIN_SUPER_BONUS);
            mPraisePending = true;
            mPraiseKind = aReward;
            aLucky.mNextLetter = aLucky.mLetter;
            aLucky.mFlipTime = 0;
            aLucky.mFlipping = true;
            MarkDirty();
        }

        /// <summary>
        /// The end of 0x9f68: with no word flying and Lex idle, while playing, burning or levelling up, Lex praises
        /// a reward: diamond "INCREDIBLE" / "UNBELIEVABLE", sapphire "FANTASTIC" / "ASTONISHING", gold "VERY GOOD"
        /// / "EXCELLENT" / "AWESOME" (each about as likely).
        /// </summary>
        private void UpdatePraise(Lex theLex, bool theFlying)
        {
            if (!mPraisePending || theFlying || !theLex.IsIdle())
            {
                return;
            }
            if (mState != STATE_PLAYING && mState != STATE_BURNING && mState != STATE_LEVEL_UP)
            {
                return;
            }
            mPraisePending = false;
            uint aRoll = gRand.Next() % 100;
            switch (mPraiseKind)
            {
            case 5:
                Praise(theLex, aRoll < 0x32 ? Res.SOUND_LEX_INCREDIBLE : Res.SOUND_LEX_UNBELIEVABLE, aRoll < 0x32 ? 0xc : 0xd);
                break;
            case 4:
                Praise(theLex, aRoll < 0x32 ? Res.SOUND_LEX_FANTASTIC : Res.SOUND_LEX_ASTONISHING, aRoll < 0x32 ? 0xb : 8);
                break;
            case 2:
                if (aRoll < 0x21)
                {
                    Praise(theLex, Res.SOUND_LEX_VERYGOOD, 0xe);
                }
                else if (aRoll < 0x42)
                {
                    Praise(theLex, Res.SOUND_LEX_EXCELLENT, 10);
                }
                else
                {
                    Praise(theLex, Res.SOUND_LEX_AWESOME, 9);
                }
                break;
            }
        }

        private void Praise(Lex theLex, int theSound, int theAnim)
        {
            mApp.PlaySound(theSound);
            theLex.PlayAnim(theAnim, false);
        }
    }
}
