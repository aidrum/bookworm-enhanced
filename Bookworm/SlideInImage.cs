namespace Sexy
{
    /// <summary>Sexy::SlideInImage (0x3e738): one image that slides in from an edge (the logo, the curtain swags...).</summary>
    public class SlideInImage : SlideInWidget
    {
        public Image mImage; // 0xd8

        public TPoint mPos; // 0xdc

        public int mDirection; // 0xe4

        public SlideInImage(BookWormApp theApp, Image theImage, TPoint thePos, int theDirection) : base(theApp)
        {
            mImage = theImage;
            mPos = thePos;
            mDirection = theDirection;
        }

        /// <summary>vf82 (0x3e91c): placed at its position, then set to slide in over half a second.</summary>
        public override void AttachToManager(int theState, int theTarget, int theParam)
        {
            base.AttachToManager(theState, theTarget, theParam);
            ScreenPos aPos = mApp.mScreenPos;
            if (mStretchEdges)
            {
                // Port (wide screens): across the whole screen, the image centred, its end columns stretched.
                Resize(mPos.mX, mPos.mY, System.Math.Max(mImage.mWidth, mApp.mWidth), mImage.mHeight);
            }
            else
            {
                // Port (wide screens): a full-width image (the swags) is centred with the design.
                int aX = mImage.mWidth >= aPos.mDesignWidth ? mPos.mX + aPos.mCenterX : mPos.mX;
                Resize(aX, mPos.mY, mImage.mWidth, mImage.mHeight);
            }
            SetupSlide(mDirection, 0.5f);
        }

        /// <summary>Port (wide screens): drawn across the screen with its end columns stretched (the pedestal).</summary>
        public bool mStretchEdges;

        public override void Draw(Graphics g)
        {
            base.Draw(g);
            // Port: the iPad's full-width swags are centred in the iPhone design (ScreenPos.mWideImageX).
            ScreenPos aPos = mApp.mScreenPos;
            int aX = mImage.mWidth > aPos.mDesignWidth ? aPos.mWideImageX : 0;
            if (mStretchEdges)
            {
                ScreenPos.DrawStretchedEdges(g, mImage, aX + aPos.mCenterX, 0, mApp.mWidth);
            }
            else
            {
                g.DrawImage(mImage, aX, 0);
            }
        }
    }
}
