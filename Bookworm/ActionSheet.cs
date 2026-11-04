using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Sexy
{
    /// <summary>
    /// Sexy::ActionSheet (0x3431c): an iOS-style action sheet. A stack of bar buttons (the red one first, any
    /// others, the green one last, under an optional message) slides up from the bottom over a dimmed screen
    /// in 25 frames; a choice slides it back down, then the listener hears DialogButtonDepress.
    /// </summary>
    public class ActionSheet : Dialogoid
    {
        private const int SLIDE_TICKS = 0x19;

        /// <summary>How dark the screen gets behind the sheet.</summary>
        private const int DIM_ALPHA = 192;

        /// <summary>The dimmed area: the whole screen, at the top of the widget (0x8c).</summary>
        public TRect mDimRect;

        /// <summary>The sheet's background, under the buttons (0x9c).</summary>
        public Image mBackground;

        public BookWormButton mRedButton; // 0xa0

        public BookWormButton mGreenButton; // 0xa4

        public List<BookWormButton> mButtons = new List<BookWormButton>(); // 0xa8

        public FancyTextWidget mMessage; // 0xb4

        public bool mShowing; // 0xb8

        public int mSlideTicks; // 0xbc

        public int mSlideTick; // 0xc0

        public KeyInterpolatorTPoint mSlidePath = KeyInterpolatorTPoint.GetNewKeyInterpolatorTPoint(); // 0xc4

        public KeyInterpolatorInt mDimPath = KeyInterpolatorInt.GetNewKeyInterpolatorInt(); // 0x108

        public Color mDimColor = new Color(0, 0, 0, 0); // 0x14c

        public ActionSheet(int theId, DialogListener theListener, BookWormApp theApp) : base(theId, theListener, theApp)
        {
            mBackground = AtlasResourceBase.IMAGE_GROUP12_00;
            mHasAlpha = true;
        }

        /// <summary>FUN_0003463c: the red button (theId, theLabel), made on first use.</summary>
        public void SetRedButton(int theId, string theLabel)
        {
            if (mRedButton != null)
            {
                mRedButton.mLabel = theLabel;
                return;
            }
            mRedButton = MakeRedButton(theId, theLabel);
            AddWidget(mRedButton);
        }

        /// <summary>FUN_0003466c: the green button.</summary>
        public void SetGreenButton(int theId, string theLabel)
        {
            if (mGreenButton != null)
            {
                mGreenButton.mLabel = theLabel;
                return;
            }
            mGreenButton = MakeGreenButton(theId, theLabel);
            AddWidget(mGreenButton);
        }

        /// <summary>FUN_0003469c: the message above the buttons, as wide as the buttons.</summary>
        public FancyTextWidget GetMessage()
        {
            if (mMessage == null)
            {
                ScreenPos aPos = mApp.mScreenPos;
                mMessage = new FancyTextWidget();
                mMessage.Resize(0, 0, mApp.mWidth - aPos.At(0x360) - aPos.At(0x368), 0);
                AddWidget(mMessage);
            }
            return mMessage;
        }

        /// <summary>vf80 (0x347a4): laid out and slid in.</summary>
        public override void Show()
        {
            mShowing = true;
            if (mSlideTick != 0)
            {
                return;
            }
            Layout();
            SetDisabled(true);
            base.Show();
            mResult = -1;
        }

        /// <summary>
        /// FUN_000347dc: the widget is the screen plus the sheet below it (insets at ScreenPos + 0x360: left, top,
        /// right, bottom); it slides up by the sheet's height.
        /// </summary>
        private void Layout()
        {
            ScreenPos aPos = mApp.mScreenPos;
            int aLeft = aPos.At(0x360);
            int aWidth = mApp.mWidth - aLeft - aPos.At(0x368);
            mDimRect = new TRect(0, 0, mApp.mWidth, mApp.mHeight);
            mWidth = mApp.mWidth;
            mHeight = aPos.At(0x364) + mApp.mHeight;
            if (mMessage != null)
            {
                mMessage.Move(aLeft, mHeight);
                mHeight += mMessage.mHeight + 8;
            }
            if (mRedButton != null)
            {
                int aHeight = mRedButton.mButtonImage.mHeight;
                mRedButton.Resize(aLeft, mHeight, aWidth, aHeight);
                mHeight += aHeight + 8;
            }
            foreach (BookWormButton aButton in mButtons)
            {
                int aHeight = aButton.mButtonImage.mHeight;
                aButton.Resize(aLeft, mHeight, aWidth, aHeight);
                mHeight += aHeight + 8;
            }
            if (mButtons.Count > 1)
            {
                mHeight += aPos.At(0x364) - 8;
            }
            if (mGreenButton != null)
            {
                int aHeight = mGreenButton.mButtonImage.mHeight;
                mGreenButton.Resize(aLeft, mHeight, aWidth, aHeight);
                mHeight += aPos.At(0x36c) + aHeight;
            }
            mSlideTicks = SLIDE_TICKS;
            mSlideTick = 0;
            mSlidePath.Clear();
            mSlidePath.SetKey(0, new TPoint(0, 0), false, true);
            mSlidePath.SetKey(mSlideTicks, new TPoint(0, mApp.mHeight - mHeight), true, true);
            Move(0, 0);
            mDimPath.Clear();
            mDimPath.SetKey(0, 0, false, true);
            mDimPath.SetKey(mSlideTicks, DIM_ALPHA, false, true);
        }

        /// <summary>vf81 (0x34998): slides out; the update finishes closing.</summary>
        public override void Close()
        {
            mShowing = false;
        }

        /// <summary>0x349a0.</summary>
        public override void Update()
        {
            if (mParent == null)
            {
                return;
            }
            if (!mShowing)
            {
                if (mSlideTick > 0)
                {
                    mSlideTick--;
                    Step();
                    if (mSlideTick == 0)
                    {
                        CloseNow();
                        mListener?.DialogButtonDepress(mId, mResult);
                    }
                }
            }
            else if (mSlideTick < mSlideTicks)
            {
                mSlideTick++;
                Step();
                if (mSlideTick == mSlideTicks)
                {
                    SetDisabled(false);
                }
            }
        }

        private void Step()
        {
            TPoint aPos = mSlidePath.Tick(mSlideTick);
            Move(aPos.mX, aPos.mY);
            mDimColor = new Color(0, 0, 0, mDimPath.Tick(mSlideTick));
        }

        /// <summary>0x34a80: the dim, then the sheet's background below the screen part.</summary>
        public override void Draw(Graphics g)
        {
            g.SetColor(mDimColor);
            g.SetColorizeImages(true);
            g.FillRect(mDimRect);
            g.SetColorizeImages(false);
            // Port: the iPad's backdrop is centred in the iPhone design (ScreenPos.mWideImageX).
            g.DrawImage(mBackground, mBackground.mWidth > mApp.mWidth ? mApp.mScreenPos.mWideImageX : 0, mDimRect.mHeight);
        }
    }
}
