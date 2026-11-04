using Microsoft.Xna.Framework;

namespace Sexy
{
    /// <summary>
    /// Sexy::GlowPulseWidget (0x3f590, 0xf4 bytes): a glow image stretched over the widget grown by insets, that
    /// Pulse flashes in (alpha 220 at tick 5) and fades out while it grows by mGrowth more on every side (tick 60),
    /// then hides. The board pulses one over the last tile of a valid word (IMAGE_GROUP4_10, insets ScreenPos +
    /// 0x380), the word balloon one over itself in word mode (IMAGE_GROUP51_02, no insets).
    /// </summary>
    public class GlowPulseWidget : Widget
    {
        public Image mImage; // 0x78

        /// <summary>Left, top, right, bottom (0x7c..0x88).</summary>
        public int mInsetLeft;

        public int mInsetTop;

        public int mInsetRight;

        public int mInsetBottom;

        public Timeline<GlowPulseWidget> mTimeline; // 0x8c

        /// <summary>How far the glow grows on each side (0xc8, 20).</summary>
        public int mGrowth = 0x14;

        /// <summary>The glow's rect at the start (0xcc) and at the end (0xdc), in widget coordinates.</summary>
        public TRect mStartRect;

        public TRect mEndRect;

        public int mAlpha; // 0xec

        /// <summary>From mStartRect (0) to mEndRect (1) (0xf0).</summary>
        public float mGrow = 1f;

        public GlowPulseWidget(Image theImage, int theLeft, int theTop, int theRight, int theBottom)
        {
            mImage = theImage;
            mInsetLeft = theLeft;
            mInsetTop = theTop;
            mInsetRight = theRight;
            mInsetBottom = theBottom;
            mTimeline = new Timeline<GlowPulseWidget>(this);
            SetVisible(false);
            mMouseVisible = false;
            mClip = false;
        }

        /// <summary>0x3f8c0.</summary>
        public override void Resize(int theX, int theY, int theWidth, int theHeight)
        {
            base.Resize(theX, theY, theWidth, theHeight);
            mStartRect = new TRect(-mInsetLeft, -mInsetTop, mWidth + mInsetLeft + mInsetRight, mHeight + mInsetTop + mInsetBottom);
            mEndRect = new TRect(mStartRect.mX - mGrowth, mStartRect.mY - mGrowth, mStartRect.mWidth + mGrowth * 2, mStartRect.mHeight + mGrowth * 2);
        }

        /// <summary>0x3f940: shows and restarts the pulse.</summary>
        public void Pulse()
        {
            SetVisible(true);
            mTimeline.Clear();
            Timeline<GlowPulseWidget>.IntChannel anAlpha = mTimeline.AddIntChannel((w, v) => w.mAlpha = v);
            anAlpha.mKeys.SetKey(0, 0, false, false);
            anAlpha.mKeys.SetKey(5, 0xdc, false, true);
            anAlpha.mKeys.SetKey(0x3c, 0, false, true);
            Timeline<GlowPulseWidget>.FloatChannel aGrow = mTimeline.AddFloatChannel((w, v) => w.mGrow = v);
            aGrow.mKeys.SetKey(0, 0f, false, false);
            aGrow.mKeys.SetKey(5, 0f, false, false);
            aGrow.mKeys.SetKey(0x3c, 1f, true, true);
            mTimeline.AddEvent(0x3c, w => w.SetVisible(false));
            mTimeline.Start();
        }

        /// <summary>0x3fd80: a tick a frame while events remain.</summary>
        public override void Update()
        {
            if (mTimeline.IsRunning())
            {
                mTimeline.AddTime(1f);
                mTimeline.Tick();
            }
        }

        /// <summary>0x3fdb4: the glow, additive, stretched between the start and end rects.</summary>
        public override void Draw(Graphics g)
        {
            base.Draw(g);
            if (!mTimeline.IsRunning() || mAlpha <= 0)
            {
                return;
            }
            int aX = (int)(mStartRect.mX + mGrow * (mEndRect.mX - mStartRect.mX));
            int aY = (int)(mStartRect.mY + mGrow * (mEndRect.mY - mStartRect.mY));
            int aWidth = (int)(mStartRect.mWidth + mGrow * (mEndRect.mWidth - mStartRect.mWidth));
            int aHeight = (int)(mStartRect.mHeight + mGrow * (mEndRect.mHeight - mStartRect.mHeight));
            g.SetColor(new Color(255, 255, 255, mAlpha));
            g.SetColorizeImages(true);
            g.SetDrawMode(1);
            g.DrawImage(mImage, aX, aY, aWidth, aHeight);
            g.SetDrawMode(0);
            g.SetColorizeImages(false);
        }
    }
}
