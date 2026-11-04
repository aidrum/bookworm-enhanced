namespace Sexy
{
    /// <summary>Sexy::MenuCurtainWidget (0x32a84): the menus' full-screen backdrop (the red curtain).</summary>
    public class MenuCurtainWidget : InterfaceWidget
    {
        public Image mImage; // 0x88

        public MenuCurtainWidget(BookWormApp theApp) : base(theApp)
        {
            mImage = AtlasResourceBase.IMAGE_GROUP13_00;
            // Port (dynamic aspect): as tall as the screen; the curtain's folds are vertical, so it stretches.
            Resize(0, 0, mImage.mWidth, System.Math.Max(mImage.mHeight, theApp.GetScreenLongSide()));
            SetVisible(false);
            SetDisabled(true);
        }

        /// <summary>Port (dynamic aspect): as tall as the screen again.</summary>
        public override void Relayout()
        {
            Resize(0, 0, mImage.mWidth, System.Math.Max(mImage.mHeight, mApp.GetScreenLongSide()));
            base.Relayout();
        }

        public override void Draw(Graphics g)
        {
            base.Draw(g);
            // Port: the iPad's curtain is centred in the iPhone design (ScreenPos.mWideImageX).
            int aX = mImage.mWidth > mApp.mWidth ? mApp.mScreenPos.mWideImageX : 0;
            if (mHeight > mImage.mHeight)
            {
                g.DrawImage(mImage, aX, 0, mImage.mWidth, mHeight);
            }
            else
            {
                g.DrawImage(mImage, aX, 0);
            }
        }
    }
}
