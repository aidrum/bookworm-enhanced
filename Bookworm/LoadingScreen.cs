namespace Sexy
{
    /// <summary>
    /// Sexy::LoadingScreen (0x2a0a0): the PopCap splash shown while the loading thread runs. The original
    /// decodes a JPEG compiled into the executable (one per screen size, table at 0x10c920);
    /// tools/convert_assets.py extracts them to images/embedded/splash_&lt;short side&gt;.jpg.
    /// </summary>
    public class LoadingScreen : InterfaceWidget
    {
        /// <summary>Shown progress, catching up with the loading thread's by 1% a frame.</summary>
        public float mProgress; // 0x88

        /// <summary>The splash is 3:2 (4:3 on iPad); centred vertically (88 pixels down on the iPhone 5).</summary>
        public int mSplashY; // 0x8c

        public Image mSplashImage; // 0x90

        public LoadingScreen(BookWormApp theApp) : base(theApp)
        {
            mProgress = 0f;
            // Port: the texture set's splash in its own design; the iPhone design (iPad textures laid out 640 /
            // 1280 wide) shows the iPhone Retina one, scaled to the layout's width.
            int aSide = theApp.mScreenPos.mIPadDesign ? theApp.GetTextureSide() : theApp.GetScreenShortSide() >= 0x280 ? 0x280 : 320;
            mSplashImage = theApp.GetImage("images/embedded/splash_" + aSide, true);
            mSplashWidth = theApp.GetScreenShortSide();
            mSplashHeight = mSplashImage != null ? mSplashImage.mHeight * mSplashWidth / mSplashImage.mWidth : 0;
            // The iPhone 5 centred the 3:2 splash 88 pixels down; port: centred on any screen height.
            mSplashY = System.Math.Max(0, (theApp.GetScreenLongSide() - mSplashHeight) / 2);
        }

        /// <summary>Port: the splash's size on screen (the layout's width).</summary>
        public int mSplashWidth;

        public int mSplashHeight;

        public override void Draw(Graphics g)
        {
            if (mSplashImage == null)
            {
            }
            else if (mSplashWidth == mSplashImage.mWidth)
            {
                g.DrawImage(mSplashImage, 0, mSplashY);
            }
            else
            {
                g.DrawImage(mSplashImage, 0, mSplashY, mSplashWidth, mSplashHeight);
            }
        }

        public override void Update()
        {
            base.Update();
            float aProgress = (float)mApp.GetLoadingThreadProgress();
            if (mProgress < aProgress)
            {
                mProgress += 0.01f;
                if (mProgress > aProgress)
                {
                    mProgress = aProgress;
                }
            }
            if (mApp.mLoadingDone || (mApp.mLoadingThreadCompleted && mProgress >= 0.99f))
            {
                mApp.LoadingScreenDone();
            }
        }

        public override void Dispose()
        {
            mSplashImage?.Dispose();
            mSplashImage = null;
            base.Dispose();
        }
    }
}
