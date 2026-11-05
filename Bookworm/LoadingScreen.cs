namespace Sexy
{
    /// <summary>
    /// Sexy::LoadingScreen (0x2a0a0): the PopCap splash shown while the loading thread runs. The original
    /// decodes a JPEG compiled into the executable (one per screen size, table at 0x10c920);
    /// tools/convert_assets.py extracts them to images/embedded/splash_&lt;short side&gt;.jpg. Port: images/splash.png
    /// is shown instead, in every texture set.
    /// </summary>
    public class LoadingScreen : InterfaceWidget
    {
        /// <summary>Shown progress, catching up with the loading thread's by 1% a frame.</summary>
        public float mProgress; // 0x88

        /// <summary>The original's 3:2 (4:3 on iPad) splash sat 88 pixels down on the iPhone 5; port: centred.</summary>
        public int mSplashY; // 0x8c

        public Image mSplashImage; // 0x90

        public LoadingScreen(BookWormApp theApp) : base(theApp)
        {
            mProgress = 0f;
            // Port: one splash (images/splash.png, made for the iPad Retina) for every texture set, in place of
            // the embedded per-size JPEGs. Sized as an iPad Retina image would be: texture side / 1536 of its
            // pixels, centred on the screen over a white background (Draw).
            mSplashImage = theApp.GetImage("images/splash", true);
            int aSide = theApp.GetTextureSide();
            mSplashWidth = mSplashImage != null ? mSplashImage.mWidth * aSide / IPAD_RETINA_SIDE : 0;
            mSplashHeight = mSplashImage != null ? mSplashImage.mHeight * aSide / IPAD_RETINA_SIDE : 0;
            mSplashX = (theApp.mWidth - mSplashWidth) / 2;
            mSplashY = (theApp.mHeight - mSplashHeight) / 2;
            // The layout does not follow the window while loading (BookWormApp.ApplyLayout), so the letterbox
            // around it is white too, back to black once the splash goes (Dispose).
            theApp.mClearColor = Microsoft.Xna.Framework.Color.White;
        }

        /// <summary>Port: the short side of the iPad Retina textures, which splash.png is drawn for.</summary>
        private const int IPAD_RETINA_SIDE = 0x600;

        public int mSplashX;

        /// <summary>Port: the splash's size on screen (its iPad Retina size in the texture set's scale).</summary>
        public int mSplashWidth;

        public int mSplashHeight;

        public override void Draw(Graphics g)
        {
            // Port: white behind the splash, filling the whole screen.
            g.SetColor(Microsoft.Xna.Framework.Color.White);
            g.FillRect(0, 0, mApp.mWidth, mApp.mHeight);
            if (mSplashImage == null)
            {
            }
            else if (mSplashWidth == mSplashImage.mWidth)
            {
                g.DrawImage(mSplashImage, mSplashX, mSplashY);
            }
            else
            {
                g.DrawImage(mSplashImage, mSplashX, mSplashY, mSplashWidth, mSplashHeight);
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
            mApp.mClearColor = Microsoft.Xna.Framework.Color.Black;
            base.Dispose();
        }
    }
}
