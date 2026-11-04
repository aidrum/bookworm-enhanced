namespace Sexy
{
    /// <summary>
    /// Sexy::FameWidget (0x1bc30, screen 9, state 3): the Stats page: each mode's high score, highest rank,
    /// longest word and best word, from the personal bests. Slides up from the bottom.
    /// </summary>
    public class FameWidget : WordBookWidget
    {
        public FameWidget(BookWormApp theApp) : base(theApp)
        {
            MakeScroller();
        }

        /// <summary>vf82 (0x1bdb0): fills the page from the personal bests.</summary>
        public override void AttachToManager(int theState, int theTarget, int theParam)
        {
            PlaceIn(mApp.mScreenPos.RectAt(0x1f0));
            LayoutPage();
            PersonalBests.Record aClassic = mApp.mPersonalBests.GetRecord(BookWormApp.GAME_CLASSIC);
            PersonalBests.Record anArcade = mApp.mPersonalBests.GetRecord(BookWormApp.GAME_ARCADE);
            mText.Clear();
            AddHighScore(BookWormApp.GAME_CLASSIC, aClassic.mHighScore);
            AddHighScore(BookWormApp.GAME_ARCADE, anArcade.mHighScore);
            AddHighestRank(BookWormApp.GAME_CLASSIC, aClassic.mHighestRank);
            AddHighestRank(BookWormApp.GAME_ARCADE, anArcade.mHighestRank);
            AddLongestWord(BookWormApp.GAME_CLASSIC, aClassic.mLongestWord);
            AddLongestWord(BookWormApp.GAME_ARCADE, anArcade.mLongestWord);
            AddBestWord(BookWormApp.GAME_CLASSIC, aClassic.mBestWord, aClassic.mBestWordScore);
            AddBestWord(BookWormApp.GAME_ARCADE, anArcade.mBestWord, anArcade.mBestWordScore);
            mText.Fit(0);
            base.AttachToManager(theState, theTarget, theParam);
            SetupSlide(SLIDE_FROM_BOTTOM, 0.5f);
        }

        /// <summary>FUN_0001be7c.</summary>
        private void AddHighScore(int theMode, int theScore)
        {
            mText.SetJustify(0);
            AddHeading("High Score", theMode);
            AddScore(theScore);
            EndEntry();
        }

        /// <summary>FUN_0001bfd4.</summary>
        private void AddHighestRank(int theMode, int theRank)
        {
            mText.SetJustify(0);
            AddHeading("Highest Rank", theMode);
            AddRank(theRank);
            EndEntry();
        }
    }
}
