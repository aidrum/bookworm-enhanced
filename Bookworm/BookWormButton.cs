namespace Sexy
{
    /// <summary>
    /// Sexy::BookWormButton (0x38e14): an image button that can show a "selected" image (the menu tabs),
    /// stretch its image to its size, and glow while touched.
    /// </summary>
    public class BookWormButton : ButtonWidget
    {
        public BookWormApp mApp; // 0xf8

        /// <summary>Shows the touch glow while the button is touched (0xfc).</summary>
        public bool mShowGlow; // 0xfc

        /// <summary>Draws the image stretched to the button (0xfd).</summary>
        public bool mStretchImage; // 0xfd

        /// <summary>0: the button image, 1: the over image (the tab is the current one) (0x100).</summary>
        public int mSelected; // 0x100

        /// <summary>Where a centred label (justify 2) is drawn: its centre x and baseline y (0x104).</summary>
        public TPoint mLabelPos; // 0x104

        public BookWormButton(int theId, ButtonListener theListener, BookWormApp theApp) : base(theId, theListener)
        {
            mApp = theApp;
            mShowGlow = true;
            mStretchImage = false;
            mSelected = 0;
        }

        /// <summary>FUN_00038f00.</summary>
        public void SetStretchImage(bool theStretch)
        {
            mStretchImage = theStretch;
        }

        /// <summary>FUN_00038f08.</summary>
        public void SetLabelPos(TPoint thePos)
        {
            mLabelPos = thePos;
        }

        /// <summary>FUN_00038f14.</summary>
        public void SetSelected(int theSelected)
        {
            mSelected = theSelected;
        }

        /// <summary>
        /// 0x38f1c: the selected tab shows its over image, an unselected one its button image (the disabled
        /// image only when the one wanted is missing); then the label, in colour 1 while pressed.
        /// </summary>
        public override void Draw(Graphics g)
        {
            if (mBtnNoDraw)
            {
                return;
            }
            bool aPressed = mIsOver && mIsDown && !mDisabled;
            Image anImage = mDisabled ? mDisabledImage : null;
            if (mSelected == 1)
            {
                if (mOverImage != null)
                {
                    anImage = mOverImage;
                }
            }
            else if (mSelected == 0 && mButtonImage != null)
            {
                anImage = mButtonImage;
            }
            if (anImage != null)
            {
                if (mStretchImage)
                {
                    g.DrawImage(anImage, new TRect(0, 0, mWidth, mHeight), new TRect(0, 0, anImage.mWidth, anImage.mHeight));
                }
                else
                {
                    g.DrawImage(anImage, 0, 0);
                }
            }
            if (mFont == null || string.IsNullOrEmpty(mLabel))
            {
                return;
            }
            int aWidth = mFont.StringWidth(mLabel);
            int aX;
            int aY;
            if (mLabelJustify == 2)
            {
                aX = mLabelPos.mX - aWidth / 2;
                aY = mLabelPos.mY;
            }
            else
            {
                int anAscent = mFont.GetAscent();
                aY = (-anAscent / 6 + mHeight + mFont.GetAscent() - 1) / 2;
                if (mLabelJustify == 1)
                {
                    aX = mWidth - aWidth;
                }
                else if (mLabelJustify == 0)
                {
                    aX = (mWidth - aWidth) / 2;
                }
                else
                {
                    aX = 0;
                }
            }
            g.SetColor(GetColor(aPressed != mInverted ? 1 : 0));
            g.SetFont(mFont);
            g.DrawString(mLabel, aX, aY);
        }

        /// <summary>0x39070.</summary>
        public override void MouseEnter()
        {
            base.MouseEnter();
            if (mShowGlow)
            {
                TPoint aPos = GetAbsPos();
                mApp.ShowTouchGlow(new TRect(aPos.mX, aPos.mY, mWidth, mHeight));
            }
        }

        /// <summary>0x390b8.</summary>
        public override void MouseLeave()
        {
            if (mShowGlow)
            {
                mApp.HideTouchGlow();
            }
            base.MouseLeave();
        }
    }
}
