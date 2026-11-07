using Microsoft.Xna.Framework;

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

        /// <summary>
        /// Port (translations): the states' names drawn as text in place of the label art (whose English ON / OFF
        /// is baked in), centred where the art's are, at its cap height (45 of the 56-pixel OFF art) and in its
        /// colours (OFF mid grey, ON white); null draws the art.
        /// </summary>
        public string mOffText;

        public string mOnText;

        public Font mTextFont;

        private static readonly Color OFF_TEXT_COLOR = new Color(130, 130, 130, 255);

        private static readonly Color ON_TEXT_COLOR = new Color(255, 255, 255, 255);

        /// <summary>Port: the label art's cap height against its height (the OFF art at iPad Retina).</summary>
        private const float LABEL_CAP_HEIGHT = 45f / 56f;

        /// <summary>
        /// Port: how much wider than the font (Arial Narrow Bold) the names are drawn, to the art's Helvetica Bold
        /// proportions (Arial Narrow is about 82% as wide as Arial Bold).
        /// </summary>
        private const float TEXT_STRETCH_X = 1.22f;

        /// <summary>
        /// Port: theText centred in the label art's box at theX, theY (theLabel's size): its letters' actual ink
        /// (the glyphs' boxes from the font, not the advance widths) centred both ways, the capitals at the art's
        /// cap height (the accent of an Í reaches above them, as in the art's own font).
        /// </summary>
        private void DrawLabelText(Graphics g, string theText, Image theLabel, int theX, int theY, Color theColor)
        {
            Font aFont = mTextFont;
            float aCap = mOffLabel.mHeight * LABEL_CAP_HEIGHT;
            MeasureInk(aFont, theText, out int anInkLeft, out int anInkRight);
            int aFontCap = MeasureCapHeight(aFont);
            float aScaleY = aCap / System.Math.Max(1, aFontCap);
            float aScaleX = aScaleY * TEXT_STRETCH_X;
            float aCenterX = theX + theLabel.mWidth / 2f;
            float aBaseline = theY + (theLabel.mHeight + aCap) / 2f;
            // Drawn at the origin of the scale with its ink centred on it, then scaled out from there.
            int aDrawX = (int)System.Math.Round(aCenterX - (anInkLeft + anInkRight) / 2f);
            int aDrawY = (int)System.Math.Round(aBaseline);
            g.SetScale(aScaleX, aScaleY, aCenterX, aDrawY);
            g.SetFont(aFont);
            g.SetColor(theColor);
            g.DrawString(theText, aDrawX, aDrawY);
            g.SetScale(1f, 1f, 0f, 0f);
        }

        /// <summary>Port: the string's ink from its pen's start: the first glyph's left to the last's right.</summary>
        private static void MeasureInk(Font theFont, string theText, out int theLeft, out int theRight)
        {
            theLeft = 0;
            theRight = theFont.StringWidth(theText);
            if (!(theFont is ImageFont anImageFont) || anImageFont.mActiveLayerList.Count == 0)
            {
                return;
            }
            FontLayer aLayer = anImageFont.mActiveLayerList[0].mBaseFontLayer;
            int aPen;
            bool aFirst = true;
            for (int i = 0; i < theText.Length; i++)
            {
                aPen = theFont.StringWidth(theText.Substring(0, i));
                if (!aLayer.GetCharData(anImageFont.GetMappedChar(theText[i]), out CharData aData) || aData.mImageRect.mWidth <= 0)
                {
                    continue;
                }
                int aLeft = aPen + aLayer.mOffset.mX + aData.mOffset.mX;
                int aRight = aLeft + aData.mImageRect.mWidth;
                if (aFirst)
                {
                    theLeft = aLeft;
                    theRight = aRight;
                    aFirst = false;
                }
                else
                {
                    theLeft = System.Math.Min(theLeft, aLeft);
                    theRight = System.Math.Max(theRight, aRight);
                }
            }
        }

        /// <summary>Port: the font's capital height: how far the top of an N's glyph is above the baseline.</summary>
        private static int MeasureCapHeight(Font theFont)
        {
            if (theFont is ImageFont anImageFont && anImageFont.mActiveLayerList.Count > 0)
            {
                anImageFont.StringWidth("N");
                FontLayer aLayer = anImageFont.mActiveLayerList[0].mBaseFontLayer;
                if (aLayer.GetCharData(anImageFont.GetMappedChar('N'), out CharData aData) && aData.mImageRect.mHeight > 0)
                {
                    return aLayer.mAscent - aLayer.mOffset.mY - aData.mOffset.mY;
                }
            }
            return (int)(theFont.GetAscent() * 0.79f);
        }

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
                if (mOffText != null && mTextFont != null)
                {
                    DrawLabelText(g, mOffText, mOffLabel, mOffLabelPos.mX - mInsets.mX, mOffLabelPos.mY - mInsets.mY, OFF_TEXT_COLOR);
                }
                else
                {
                    g.DrawImage(mOffLabel, mOffLabelPos.mX - mInsets.mX, mOffLabelPos.mY - mInsets.mY);
                }
            }
            else
            {
                int aKnobX = mKnobOnX - mInsets.mX;
                g.DrawImage(mOnImage, 0, 0);
                g.DrawImage(mKnob, aKnobX, -mInsets.mY);
                if (mOnText != null && mTextFont != null)
                {
                    DrawLabelText(g, mOnText, mOnLabel, mOnLabelPos.mX + aKnobX, mOnLabelPos.mY - mInsets.mY, ON_TEXT_COLOR);
                }
                else
                {
                    g.DrawImage(mOnLabel, mOnLabelPos.mX + aKnobX, mOnLabelPos.mY - mInsets.mY);
                }
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
