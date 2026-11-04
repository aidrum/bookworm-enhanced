using Microsoft.Xna.Framework;

namespace Sexy
{
    /// <summary>Sexy::LabelWidget (0x1d0a8): one line of text, justified inside its insets.</summary>
    public class LabelWidget : Widget
    {
        public string mText = ""; // 0x7c

        public Font mFont; // 0x80

        /// <summary>0 centred, 1 right, -1 at the left inset (0x84).</summary>
        public int mJustifyH = -1;

        /// <summary>0 centred, 1 bottom, -1 at the top inset (0x88).</summary>
        public int mJustifyV = -1;

        /// <summary>Left, top, right, bottom (0x8c).</summary>
        public TRect mInsets;

        public LabelWidget(string theText, Font theFont)
        {
            mText = theText;
            mFont = theFont;
            // Colour table 0x10c7e0 (one colour).
            SetColor(0, new Color(255, 255, 255, 255));
        }

        /// <summary>FUN_0001d380.</summary>
        public void SetInsets(TRect theInsets)
        {
            mInsets = theInsets;
        }

        /// <summary>FUN_0001d394: sized to the text plus the insets.</summary>
        public void SizeToText()
        {
            Resize(0, 0, mInsets.mX + mFont.StringWidth(mText) + mInsets.mWidth, mFont.GetHeight() + mInsets.mY + mInsets.mHeight);
        }

        /// <summary>0x1d3f0.</summary>
        public override void Draw(Graphics g)
        {
            int aX;
            switch (mJustifyH)
            {
            case 0:
                aX = (mInsets.mX + mWidth - mInsets.mWidth - mFont.StringWidth(mText)) / 2;
                break;
            case 1:
                aX = mWidth - mInsets.mWidth - mFont.StringWidth(mText);
                break;
            case -1:
                aX = mInsets.mX;
                break;
            default:
                aX = 0;
                break;
            }
            int aY;
            switch (mJustifyV)
            {
            case 0:
                aY = mInsets.mY + (mHeight - mInsets.mY - mInsets.mHeight - mFont.GetHeight()) / 2 + mFont.GetAscent();
                break;
            case 1:
                aY = mHeight - mInsets.mHeight - mFont.GetHeight() + mFont.GetAscent();
                break;
            case -1:
                aY = mFont.GetAscent() + mInsets.mY;
                break;
            default:
                aY = 0;
                break;
            }
            g.SetFont(mFont);
            g.SetColor(GetColor(0));
            g.DrawString(mText, aX, aY);
        }
    }
}
