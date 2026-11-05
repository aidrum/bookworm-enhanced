using Microsoft.Xna.Framework;

namespace Sexy
{
    /// <summary>
    /// Sexy::WordCollectionWidget (0x37034, screen 11): a collection book's page: its name (upper-cased) and
    /// "(found/total)", or the name in cream with a badge once complete, then its words in two columns, the
    /// found ones in the second colour with a check mark. Shown from the bookshelf (state 5, X back to 4), from
    /// state 14 (X back to 13), and by the board when a word is new to a book (state 12: CONTINUE back to 10;
    /// the new word's check pulses three times).
    /// </summary>
    public class WordCollectionWidget : WordBookWidget
    {
        /// <summary>The completed book's title colour (0xfefddf).</summary>
        private static readonly Color COMPLETE_COLOR = new Color(0xfe, 0xfd, 0xdf, 0xff);

        /// <summary>The book shown (0x128).</summary>
        public CollectionBook mBook;

        /// <summary>The text chunk of the book's first word (0x12c).</summary>
        public int mFirstWordChunk;

        /// <summary>Draws the check marks, over the text (0x130).</summary>
        public ProxyWidget mChecks;

        /// <summary>Draws the complete badge (0x134, image 0x138), at ScreenPos + 0x70.</summary>
        public ProxyWidget mBadge;

        public Image mBadgeImage;

        /// <summary>Every word of the book is found (0x13c).</summary>
        public bool mComplete;

        /// <summary>The new word's pulse (0x140): alpha (0x17c), scale (0x180), and its plain check shown (0x184).</summary>
        public Timeline<WordCollectionWidget> mTimeline;

        public int mPulseAlpha;

        public float mPulseScale;

        public bool mCheckShown;

        public WordCollectionWidget(BookWormApp theApp) : base(theApp)
        {
            mBadgeImage = AtlasResourceBase.IMAGE_GROUP5_01;
            mTimeline = new Timeline<WordCollectionWidget>(this);
            MakeScroller();
            mBadge = new ProxyWidget(this);
            mBadge.mMouseVisible = false;
            AddWidget(mBadge);
            MakeTitle();
            MakeCloseButton();
            MakeContinueButton();
            mChecks = new ProxyWidget(this);
            mChecks.mMouseVisible = false;
            mText.AddWidget(mChecks);
        }

        /// <summary>vf82 (0x37418): the current book's page for theState.</summary>
        public override void AttachToManager(int theState, int theTarget, int theParam)
        {
            Collectionator aCollectionator = mApp.mCollectionator;
            ScreenPos aPos = mApp.mScreenPos;
            mBook = aCollectionator.GetBook(aCollectionator.mCurrentBook);
            if (theState == 12)
            {
                mCloseButton.SetVisible(false);
                mContinueButton.SetVisible(true);
                PlaceIn(aPos.RectAt(0x220));
                SetupPulse();
            }
            else if (theState == 5 || theState == 14)
            {
                mCloseButton.SetVisible(true);
                mContinueButton.SetVisible(false);
                PlaceIn(aPos.RectAt(theState == 5 ? 0x1e0 : 0x200));
            }
            SetTopOffset(aPos.At(0x3dc));
            LayoutPage();
            int aCount = mBook != null ? mBook.mWords.Count : 0;
            int aFound = mBook != null ? mBook.mFoundCount : 0;
            mComplete = aFound == aCount;
            string aName = mBook != null ? SexyAppBase.Loc(mBook.mName).ToUpperInvariant() : "";

            mTitle.Clear();
            mTitle.SetFont(mHeadingFont);
            mTitle.SetJustify(0);
            if (!mComplete)
            {
                mTitle.SetColor(GetColor(2));
                mTitle.AddChunk(aName);
                mTitle.NewLine(0);
                mTitle.SetColor(GetColor(3));
                mTitle.AddChunk(string.Format("({0}/{1})", aFound, aCount));
            }
            else
            {
                mTitle.SetColor(COMPLETE_COLOR);
                mTitle.AddChunk(aName);
            }
            mTitle.NewLine(0);
            mTitle.Fit(1);
            mBadge.Resize(aPos.At(0x70), aPos.At(0x74), mBadgeImage.mWidth, mBadgeImage.mHeight);

            mText.Clear();
            mText.SetJustify(0);
            mText.SetFont(mTextFont);
            mText.SetColumn(0, 2);
            mFirstWordChunk = mText.mChunks.Count;
            int aLastInFirstColumn = (aCount + 1) / 2 - 1;
            for (int i = 0; i < aCount; i++)
            {
                CollectionWord aWord = mBook.mWords[i];
                mText.SetColor(GetColor(aWord.mFound ? 1 : 0));
                mText.AddChunk(aWord.mForms.Count > 0 ? aWord.mForms[0] : "");
                EndLine();
                if (i == aLastInFirstColumn)
                {
                    mText.SetColumn(1, 2);
                }
            }
            mText.Fit(0);
            mChecks.Resize(0, 0, mText.mWidth, mText.mHeight);
            base.AttachToManager(theState, theTarget, theParam);
            SetupSlide(SLIDE_FROM_BOTTOM, 0.5f);
            if (theState == 12 && mBook != null)
            {
                int aChunk = mFirstWordChunk + mBook.mField4;
                if (aChunk >= 0 && aChunk < mText.mChunks.Count)
                {
                    FancyTextWidget.Chunk aRect = mText.mChunks[aChunk];
                    mScroll.ScrollRectIntoView(new TRect(aRect.mX, aRect.mY, aRect.mWidth, aRect.mHeight), false);
                }
            }
        }

        /// <summary>
        /// The pulse keys (0x37418, state 12): nothing for 40 ticks, then the check appears (SOUND_BONUS_TILE)
        /// and three times a white copy of it grows from 1x to 2x while fading out (ticks 40-100, 101-160,
        /// 161-240); the timeline ends at 240.
        /// </summary>
        private void SetupPulse()
        {
            mTimeline.Clear();
            Timeline<WordCollectionWidget>.IntChannel anAlpha = mTimeline.AddIntChannel((w, v) => w.mPulseAlpha = v);
            anAlpha.mKeys.SetKey(0, 0, false, true);
            anAlpha.mKeys.SetKey(0x28, 0xff, false, false);
            anAlpha.mKeys.SetKey(0x64, 0, false, true);
            anAlpha.mKeys.SetKey(0x65, 0xff, false, false);
            anAlpha.mKeys.SetKey(0xa0, 0, false, true);
            anAlpha.mKeys.SetKey(0xa1, 0xff, false, false);
            anAlpha.mKeys.SetKey(0xf0, 0, false, true);
            Timeline<WordCollectionWidget>.FloatChannel aScale = mTimeline.AddFloatChannel((w, v) => w.mPulseScale = v);
            aScale.mKeys.SetKey(0, 1f, false, true);
            aScale.mKeys.SetKey(0x28, 1f, false, true);
            aScale.mKeys.SetKey(0x64, 2f, true, true);
            aScale.mKeys.SetKey(0x65, 1f, false, false);
            aScale.mKeys.SetKey(0xa0, 2f, true, true);
            aScale.mKeys.SetKey(0xa1, 1f, false, false);
            aScale.mKeys.SetKey(0xf0, 2f, true, true);
            Timeline<WordCollectionWidget>.IntChannel aShown = mTimeline.AddIntChannel((w, v) => w.mCheckShown = v != 0);
            aShown.mKeys.SetKey(0, 0, false, false);
            aShown.mKeys.SetKey(0x28, 1, false, false);
            aShown.mKeys.SetKey(0x64, 1, false, false);
            mTimeline.AddEvent(0x28, w => w.mApp.PlaySound(Res.SOUND_BONUS_TILE));
            mTimeline.AddEvent(0xf0, w => { });
            mTimeline.Apply();
        }

        /// <summary>vf81 (0x381f8): in state 12 the balloon tells what happened, and the pulse starts.</summary>
        public override void InterfaceStateDone()
        {
            base.InterfaceStateDone();
            if (mInterfaceState != 12 || mBook == null)
            {
                return;
            }
            string aText;
            if (mBook.mFoundCount == 1)
            {
                aText = SexyAppBase.Loc("You've opened a new Collection Book!");
            }
            else if (mBook.mFoundCount == mBook.mWords.Count)
            {
                aText = SexyAppBase.Loc("You've spelled every word in this Collection Book!");
            }
            else
            {
                aText = SexyAppBase.Loc("You've spelled a new Collection Word!");
            }
            mApp.mWordBalloon.ShowText(aText);
            mTimeline.Start();
        }

        /// <summary>0x383b0: the pulse runs a tick a frame until its last event.</summary>
        public override void Update()
        {
            base.Update();
            if (mTimeline.IsActive() && !mTimeline.AtEnd())
            {
                mTimeline.AddTime(1f);
                mTimeline.Tick();
            }
        }

        /// <summary>
        /// vf94 (0x383f4): the check marks (IMAGE_GROUP31_28) at the bottom left of each found word (moved by
        /// ScreenPos + 0x3e0); the new word's only once the pulse shows it, and its pulse drawn unclipped. The
        /// badge proxy draws the badge on a complete book.
        /// </summary>
        public override void DrawProxyWidget(Graphics g, ProxyWidget theProxy)
        {
            if (theProxy == mChecks)
            {
                if (mBook == null)
                {
                    return;
                }
                Image aCheck = AtlasResourceBase.IMAGE_GROUP31_28;
                int anOffset = mApp.mScreenPos.At(0x3e0);
                for (int i = 0; i < mBook.mWords.Count; i++)
                {
                    int aChunk = mFirstWordChunk + i;
                    if (!mBook.mWords[i].mFound || aChunk >= mText.mChunks.Count)
                    {
                        continue;
                    }
                    FancyTextWidget.Chunk aRect = mText.mChunks[aChunk];
                    int aX = anOffset + aRect.mX - aCheck.mWidth;
                    int aY = anOffset + aRect.mY - aCheck.mHeight + aRect.mHeight;
                    bool aNew = i == mBook.mField4;
                    if (!aNew || mCheckShown)
                    {
                        g.SetColorizeImages(false);
                        g.DrawImage(aCheck, aX, aY);
                    }
                    if (aNew && mPulseAlpha > 0)
                    {
                        g.PushState();
                        g.ClearClipRect();
                        g.SetColor(new Color(0xff, 0xff, 0xff, mPulseAlpha));
                        g.SetColorizeImages(true);
                        int aWidth = (int)(aCheck.mWidth * mPulseScale);
                        int aHeight = (int)(aCheck.mHeight * mPulseScale);
                        g.DrawImage(aCheck, aX + (aCheck.mWidth - aWidth) / 2, aY + (aCheck.mHeight - aHeight) / 2, aWidth, aHeight);
                        g.SetColorizeImages(false);
                        g.PopState();
                    }
                }
            }
            else if (theProxy == mBadge)
            {
                if (mComplete)
                {
                    g.DrawImage(mBadgeImage, 0, 0);
                }
            }
            else
            {
                base.DrawProxyWidget(g, theProxy);
            }
        }

        /// <summary>vf95 (0x385f4): X from the bookshelf (state 5 -> 4) or state 14 (-> 13); CONTINUE in state 12 (-> 10).</summary>
        public override void ButtonDepress(int theId)
        {
            int aState = mInterfaceState;
            if (aState == 5 && theId == BUTTON_CLOSE)
            {
                mApp.GotoInterfaceState(4);
            }
            else if (aState == 14 && theId == BUTTON_CLOSE)
            {
                mApp.GotoInterfaceState(13);
            }
            else if (aState == 12 && theId == BUTTON_CONTINUE)
            {
                mApp.GotoInterfaceState(10);
            }
        }
    }
}
