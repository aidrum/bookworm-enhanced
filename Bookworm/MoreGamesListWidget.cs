using System.Collections.Generic;

namespace Sexy
{
    /// <summary>
    /// Sexy::MoreGamesListWidget, from the old iPhone-only version (2.2.1, 0x44d96; the later version replaced the
    /// More Games screen with a web view). One tray per game, stacked without gaps: the tray background, the game's
    /// banner centred on it, the tray frame over both. Touching a tray lights it with the touch glow; letting go on
    /// it tells the listener (DialogButtonDepress(id, row)). The trays exist at the iPhone size only, so the rows
    /// are drawn scaled by <see cref="mScale"/> (the layout's scale over the iPhone); banners made at the iPad's
    /// size are drawn at half that (<see cref="BannerScale"/>).
    /// </summary>
    public class MoreGamesListWidget : Widget
    {
        /// <summary>A row (0x18 bytes): its rect, the banner, the link.</summary>
        public class Row
        {
            public TRect mRect;

            public Image mImage;

            public string mLink = "";
        }

        public BookWormApp mApp; // 0x78

        public int mId; // 0x7c

        public DialogListener mListener; // 0x80

        public Image mTrayImage; // 0x84 IMAGE_MORE_GAMES_TRAY_BG

        public Image mTrayOverlayImage; // 0x88 IMAGE_MORE_GAMES_TRAY_OV

        public List<Row> mRows = new List<Row>(); // 0x8c

        /// <summary>The touched row, -1 when none (0x98).</summary>
        public int mPressed = -1;

        /// <summary>Port: the layout's scale over the iPhone, which the trays and banners were made for.</summary>
        public int mScale = 1;

        public MoreGamesListWidget(BookWormApp theApp, int theId, DialogListener theListener)
        {
            mApp = theApp;
            mId = theId;
            mListener = theListener;
            mTrayImage = theApp.GetImage("images/moregames/tray_bg", false);
            mTrayOverlayImage = theApp.GetImage("images/moregames/tray_ov", false);
        }

        public override void Dispose()
        {
            foreach (Row aRow in mRows)
            {
                aRow.mImage?.Dispose();
            }
            mRows.Clear();
            mTrayImage?.Dispose();
            mTrayOverlayImage?.Dispose();
            base.Dispose();
        }

        /// <summary>
        /// AddRow (0x44ec2): a tray-sized row under the last one; the widget is resized to the tray's width and the
        /// rows' height.
        /// </summary>
        public void AddRow(Image theImage, string theLink)
        {
            int aWidth = mTrayImage.mWidth * mScale;
            int aHeight = mTrayImage.mHeight * mScale;
            int y = 0;
            if (mRows.Count > 0)
            {
                TRect aLast = mRows[mRows.Count - 1].mRect;
                y = aLast.mY + aLast.mHeight;
            }
            mRows.Add(new Row { mRect = new TRect(0, y, aWidth, aHeight), mImage = theImage, mLink = theLink });
            Resize(0, 0, aWidth, y + aHeight);
        }

        /// <summary>Port: sets the scale and lays the rows out again.</summary>
        public void SetScale(int theScale)
        {
            mScale = theScale;
            List<Row> aRows = new List<Row>(mRows);
            mRows.Clear();
            foreach (Row aRow in aRows)
            {
                AddRow(aRow.mImage, aRow.mLink);
            }
        }

        /// <summary>GetLinkForRow (0x44b82).</summary>
        public string GetLinkForRow(int theRow)
        {
            return mRows[theRow].mLink;
        }

        /// <summary>
        /// Port: the scale a banner was made at over the iPhone tray. The original's banners are iPhone size,
        /// cropped to their content and centred on the tray (1); the newer ones are made at the iPad's size, a
        /// whole tray (640 x 200) with their own transparent margins (2).
        /// </summary>
        private int BannerScale(Image theImage)
        {
            return mTrayImage == null ? 1 : System.Math.Max(1, theImage.mWidth / mTrayImage.mWidth);
        }

        private void DrawScaled(Graphics g, Image theImage, int x, int y, int theImageScale = 1)
        {
            g.DrawImage(theImage, new TRect(x, y, theImage.mWidth * mScale / theImageScale, theImage.mHeight * mScale / theImageScale),
                new TRect(0, 0, theImage.mWidth, theImage.mHeight));
        }

        /// <summary>Draw (0x44ae8).</summary>
        public override void Draw(Graphics g)
        {
            base.Draw(g);
            foreach (Row aRow in mRows)
            {
                TRect aRect = aRow.mRect;
                g.PushState();
                g.ClipRect(aRect);
                DrawScaled(g, mTrayImage, aRect.mX, aRect.mY);
                if (aRow.mImage != null)
                {
                    int aBannerScale = BannerScale(aRow.mImage);
                    DrawScaled(g, aRow.mImage, (aRect.mWidth - aRow.mImage.mWidth * mScale / aBannerScale) / 2 + aRect.mX,
                        (aRect.mHeight - aRow.mImage.mHeight * mScale / aBannerScale) / 2 + aRect.mY, aBannerScale);
                }
                DrawScaled(g, mTrayOverlayImage, aRect.mX, aRect.mY);
                g.PopState();
            }
        }

        private void Glow(TRect theRect)
        {
            TPoint aPos = GetAbsPos();
            mApp.ShowTouchGlow(new TRect(aPos.mX + theRect.mX, aPos.mY + theRect.mY, theRect.mWidth, theRect.mHeight));
        }

        /// <summary>TouchBegan (0x44c20): the touched row glows.</summary>
        public override void MouseDown(int x, int y, int theClickCount)
        {
            base.MouseDown(x, y, theClickCount);
            for (int i = 0; i < mRows.Count; i++)
            {
                if (mRows[i].mRect.Contains(x, y))
                {
                    Glow(mRows[i].mRect);
                    mPressed = i;
                }
            }
        }

        /// <summary>TouchMoved (0x44b9e): the glow follows whether the touch is still on the row.</summary>
        public override void MouseDrag(int x, int y)
        {
            base.MouseDrag(x, y);
            if (mPressed < 0)
            {
                return;
            }
            TRect aRect = mRows[mPressed].mRect;
            if (aRect.Contains(x, y))
            {
                Glow(aRect);
            }
            else
            {
                mApp.HideTouchGlow();
            }
        }

        /// <summary>TouchEnded (0x44a84): let go on the touched row, the listener is told.</summary>
        public override void MouseUp(int x, int y, int theClickCount)
        {
            base.MouseUp(x, y, theClickCount);
            int aPressed = mPressed;
            mPressed = -1;
            mApp.HideTouchGlow();
            if (aPressed >= 0 && mRows[aPressed].mRect.Contains(x, y))
            {
                mListener?.DialogButtonDepress(mId, aPressed);
            }
        }

        /// <summary>TouchesCanceled (0x44a6e): the scroller took the touch.</summary>
        public override void TouchesCanceled()
        {
            base.TouchesCanceled();
            mApp.HideTouchGlow();
            mPressed = -1;
        }
    }
}
