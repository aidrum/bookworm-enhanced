namespace Sexy
{
    /// <summary>
    /// Sexy::WordHistoryWidget (0x368fc, screen 12): "RECENT n LETTER WORDS", the newest first, in two columns
    /// up to 8 letters and one beyond. Shown from the bookshelf (state 6, X back to 4) or state 15 (X back to 13).
    /// </summary>
    public class WordHistoryWidget : WordBookWidget
    {
        public WordHistoryWidget(BookWormApp theApp) : base(theApp)
        {
            MakeTitle();
            MakeScroller();
            MakeCloseButton();
        }

        /// <summary>vf82 (0x36a94).</summary>
        public override void AttachToManager(int theState, int theTarget, int theParam)
        {
            ScreenPos aPos = mApp.mScreenPos;
            if (theState == 6)
            {
                PlaceIn(aPos.RectAt(0x1e0));
            }
            else if (theState == 15)
            {
                PlaceIn(aPos.RectAt(0x200));
            }
            SetTopOffset(aPos.At(0x3dc));
            LayoutPage();
            int aTab = mApp.mCollectionator.mHistoryTab;
            System.Collections.Generic.List<string> aWords = mApp.mCollectionator.GetHistory(aTab);
            int aLength = aTab + 3;

            mTitle.Clear();
            mTitle.SetJustify(0);
            mTitle.SetFont(mHeadingFont);
            mTitle.SetColor(GetColor(2));
            mTitle.AddChunk(string.Format(SexyAppBase.Loc("RECENT {0} LETTER WORDS"), aLength));
            mTitle.NewLine(0);
            mTitle.Fit(1);

            mText.Clear();
            mText.SetJustify(0);
            mText.SetColor(GetColor(0));
            mText.SetFont(mTextFont);
            bool aTwoColumns = aLength < 9;
            if (aTwoColumns)
            {
                mText.SetColumn(0, 2);
            }
            int aLastInFirstColumn = (aWords.Count + 1) / 2 - 1;
            for (int i = 0; i < aWords.Count; i++)
            {
                mText.AddChunk(aWords[i]);
                EndLine();
                if (aTwoColumns && i == aLastInFirstColumn)
                {
                    mText.SetColumn(1, 2);
                }
            }
            mText.Fit(0);
            base.AttachToManager(theState, theTarget, theParam);
            SetupSlide(SLIDE_FROM_BOTTOM, 0.5f);
        }

        /// <summary>vf95 (0x36efc): X back to the bookshelf.</summary>
        public override void ButtonDepress(int theId)
        {
            if (theId != BUTTON_CLOSE)
            {
                return;
            }
            if (mInterfaceState == 6)
            {
                mApp.GotoInterfaceState(4);
            }
            else if (mInterfaceState == 15)
            {
                mApp.GotoInterfaceState(13);
            }
        }
    }
}
