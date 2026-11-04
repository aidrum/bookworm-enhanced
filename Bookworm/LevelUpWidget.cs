namespace Sexy
{
    /// <summary>
    /// Sexy::LevelUpWidget (0x1d53c, screen 17, state 11): the page between levels, and after the game: the
    /// rank reached and the points for the next, the level's (or game's) best and longest words, and the
    /// level and total play time. CONTINUE goes back to the board, or after a game over to the Stats page.
    /// </summary>
    public class LevelUpWidget : WordBookWidget
    {
        public LevelUpWidget(BookWormApp theApp) : base(theApp)
        {
            MakeScroller();
            MakeContinueButton();
        }

        /// <summary>vf82 (0x1d6d4): fills the page from the board.</summary>
        public override void AttachToManager(int theState, int theTarget, int theParam)
        {
            PlaceIn(mApp.mScreenPos.RectAt(0x2e0));
            LayoutPage();
            Board aBoard = mApp.mBoard;
            bool aGameOver = aBoard.mState == Board.STATE_GAME_OVER;
            StatKeeper aStats = aGameOver ? aBoard.mGameStats : aBoard.mLevelStats;
            int aRank = aGameOver ? aBoard.mLevel : aBoard.mLevel + 1;
            mText.Clear();
            mText.SetJustify(0);
            AddHeading("Your Rank", -1);
            AddRank(aRank);
            EndLine();
            mText.SetColor(BLACK);
            mText.AddChunk(string.Format("Next Rank at {0} pts", Board.GetLevelScore(aRank)));
            EndEntry();
            AddBestWord(-1, aStats.mBestWord, aStats.mBestWordScore);
            AddLongestWord(-1, aStats.mLongestWord);
            AddHeading("Time", -1);
            mText.SetFont(mTextFont);
            mText.SetColor(BLACK);
            mText.AddChunk("Level " + FormatTime(aBoard.mLevelSeconds));
            EndLine();
            mText.AddChunk("Total " + FormatTime(aBoard.mTotalSeconds));
            EndEntry();
            mText.Fit(0);
            base.AttachToManager(theState, theTarget, theParam);
            SetupSlide(SLIDE_FROM_BOTTOM, 0.5f);
        }

        /// <summary>FUN_00012760: [d:][hh:]mm:ss (hours shown once there are any).</summary>
        public static string FormatTime(int theSeconds)
        {
            string aText = "";
            if (theSeconds >= 86400)
            {
                aText += string.Format("{0}:", theSeconds / 86400);
            }
            int aHours = theSeconds / 3600 % 24;
            if (aHours > 0 || theSeconds >= 86400)
            {
                aText += string.Format("{0:00}:", aHours);
            }
            return aText + string.Format("{0:00}:{1:00}", theSeconds / 60 % 60, theSeconds % 60);
        }

        /// <summary>
        /// vf95 (0x1dd8c): CONTINUE. After a game over the save is dropped and the Stats page shows (song 4);
        /// otherwise the game is saved and back to the board.
        /// </summary>
        public override void ButtonDepress(int theId)
        {
            if (theId != BUTTON_CONTINUE)
            {
                return;
            }
            if (mApp.mBoard.mState == Board.STATE_GAME_OVER)
            {
                mApp.DeleteSavedGame(mApp.mGameMode);
                mApp.mMusicInterface?.PlayMusic(4, 0, true);
                mApp.GotoInterfaceState(3);
            }
            else
            {
                mApp.mBoard.SaveGame(mApp.mGameMode);
                mApp.GotoInterfaceState(10);
            }
        }
    }
}
