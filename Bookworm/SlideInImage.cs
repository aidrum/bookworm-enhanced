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
            Resize(mPos.mX, mPos.mY, mImage.mWidth, mImage.mHeight);
            SetupSlide(mDirection, 0.5f);
        }

        public override void Draw(Graphics g)
        {
            base.Draw(g);
            // Port: the iPad's full-width swags are centred in the iPhone design (ScreenPos.mWideImageX).
            g.DrawImage(mImage, mImage.mWidth > mApp.mWidth ? mApp.mScreenPos.mWideImageX : 0, 0);
        }
    }
}
