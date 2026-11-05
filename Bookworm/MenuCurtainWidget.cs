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
            // Port (wide screens): as wide as the screen, the curtain repeated side by side.
            Resize(0, 0, System.Math.Max(mImage.mWidth, theApp.mWidth), System.Math.Max(mImage.mHeight, theApp.mHeight));
            SetVisible(false);
            SetDisabled(true);
        }

        /// <summary>Port (dynamic aspect): as big as the screen again.</summary>
        public override void Relayout()
        {
            Resize(0, 0, System.Math.Max(mImage.mWidth, mApp.mWidth), System.Math.Max(mImage.mHeight, mApp.mHeight));
            base.Relayout();
        }

        public override void Draw(Graphics g)
        {
            base.Draw(g);
            // Port: the iPad's curtain is centred in the iPhone design (ScreenPos.mWideImageX). On wide screens it
            // is centred too and tiled out from there to both edges.
            ScreenPos aPos = mApp.mScreenPos;
            int aWidth = mImage.mWidth;
            int aX = (aWidth > aPos.mDesignWidth ? aPos.mWideImageX : 0) + aPos.mCenterX;
            while (aX > 0)
            {
                aX -= aWidth;
            }
            for (; aX < mApp.mWidth; aX += aWidth)
            {
                if (mHeight > mImage.mHeight)
                {
                    g.DrawImage(mImage, aX, 0, aWidth, mHeight);
                }
                else
                {
                    g.DrawImage(mImage, aX, 0);
                }
            }
        }
    }
}
