namespace Sexy
{
    /// <summary>Listener of <see cref="Switch"/> (OptionsDialog's second listener interface).</summary>
    public interface SwitchListener
    {
        void SwitchChanged(int theId, bool theOn);
    }

    /// <summary>
    /// Sexy::Switch (0x44cf0): an iOS-style ON/OFF switch: a track (grey off, green on), a knob, and the
    /// state's label beside the knob. Touching it flips it.
    /// </summary>
    public class Switch : Widget
    {
        public int mId; // 0x78

        public bool mOn; // 0x7c

        public Image mOffImage; // 0x80

        public Image mOffLabel; // 0x84

        public Image mOnImage; // 0x88

        public Image mOnLabel; // 0x8c

        public Image mKnob; // 0x90

        /// <summary>How far the knob's art reaches past it: left, top, right, bottom (0x94).</summary>
        public TRect mInsets;

        public SwitchListener mListener; // 0xa4

        /// <summary>The knob's x when on (0xb4).</summary>
        public int mKnobOnX;

        public TPoint mOffLabelPos; // 0xb8

        public TPoint mOnLabelPos; // 0xc0

        public Switch(int theId, SwitchListener theListener, Image theOffImage, Image theOffLabel, Image theOnImage, Image theOnLabel, Image theKnob, TRect theInsets)
        {
            mId = theId;
            mListener = theListener;
            mOffImage = theOffImage;
            mOffLabel = theOffLabel;
            mOnImage = theOnImage;
            mOnLabel = theOnLabel;
            mKnob = theKnob;
            mInsets = theInsets;
            mClip = false;
            Resize(0, 0, mOffImage.mWidth, mOffImage.mHeight);
            int aKnobWidth = mKnob.mWidth - mInsets.mWidth - mInsets.mX;
            mKnobOnX = mOffImage.mWidth - aKnobWidth;
            mOffLabelPos = new TPoint(aKnobWidth + (mKnobOnX - mOffLabel.mWidth) / 2, (mOffImage.mHeight - mOffLabel.mHeight) / 2);
            mOnLabelPos = new TPoint(-(mKnobOnX + mOnLabel.mWidth) / 2, (mOnImage.mHeight - mOnLabel.mHeight) / 2);
        }

        /// <summary>vf80 (0x44e98).</summary>
        public void SetOn(bool theOn, bool theNotify)
        {
            mOn = theOn;
            if (theNotify)
            {
                mListener?.SwitchChanged(mId, theOn);
            }
            MarkDirty();
        }

        /// <summary>vf81.</summary>
        public bool IsOn()
        {
            return mOn;
        }

        /// <summary>0x44ecc.</summary>
        public override void Draw(Graphics g)
        {
            base.Draw(g);
            if (!mOn)
            {
                g.DrawImage(mOffImage, 0, 0);
                g.DrawImage(mKnob, -mInsets.mX, -mInsets.mY);
                g.DrawImage(mOffLabel, mOffLabelPos.mX - mInsets.mX, mOffLabelPos.mY - mInsets.mY);
            }
            else
            {
                int aKnobX = mKnobOnX - mInsets.mX;
                g.DrawImage(mOnImage, 0, 0);
                g.DrawImage(mKnob, aKnobX, -mInsets.mY);
                g.DrawImage(mOnLabel, mOnLabelPos.mX + aKnobX, mOnLabelPos.mY - mInsets.mY);
            }
        }

        /// <summary>0x44f5c (TouchBegan).</summary>
        public override void MouseDown(int x, int y, int theClickCount)
        {
            base.MouseDown(x, y, theClickCount);
            SetOn(!mOn, true);
            MarkDirty();
        }
    }
}
