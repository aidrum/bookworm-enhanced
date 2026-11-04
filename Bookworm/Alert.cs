using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Sexy
{
    /// <summary>
    /// Sexy::Alert (0x3c820): a centred message box (a title in Lockergnome, a message, and bar buttons below)
    /// drawn as a 9-slice box. Its timeline enables the buttons 25 frames after it shows and tells the listener
    /// DialogButtonDepress 25 frames after a choice. (It also animates three members at 0xf4-0xfc that nothing
    /// reads; they are left out.)
    /// </summary>
    public class Alert : Dialogoid
    {
        private const int ANIM_TICKS = 0x19;

        /// <summary>The text colour (0x10cf7c).</summary>
        private static readonly Color TEXT_COLOR = new Color(255, 255, 255, 255);

        public Image mBackground; // 0x8c

        public string mTitle = ""; // 0x90

        public string mMessage = ""; // 0x94

        public FancyTextWidget mText; // 0x98

        public List<BookWormButton> mButtons = new List<BookWormButton>(); // 0x9c

        public Timeline<Alert> mTimeline; // 0xa8

        public Alert(int theId, DialogListener theListener, BookWormApp theApp) : base(theId, theListener, theApp)
        {
            mBackground = AtlasResourceBase.IMAGE_GROUP51_00;
            mTimeline = new Timeline<Alert>(this);
            mHasAlpha = true;
            mText = new FancyTextWidget();
            AddWidget(mText);
        }

        /// <summary>FUN_0003cd14.</summary>
        public void SetTitle(string theTitle)
        {
            mTitle = theTitle;
        }

        /// <summary>FUN_0003cd20.</summary>
        public void SetMessage(string theMessage)
        {
            mMessage = theMessage;
        }

        /// <summary>FUN_0003cd2c.</summary>
        public void AddRedButton(int theId, string theLabel)
        {
            BookWormButton aButton = MakeRedButton(theId, theLabel);
            mButtons.Add(aButton);
            AddWidget(aButton);
        }

        /// <summary>FUN_0003cd78.</summary>
        public void AddGreenButton(int theId, string theLabel)
        {
            BookWormButton aButton = MakeGreenButton(theId, theLabel);
            mButtons.Add(aButton);
            AddWidget(aButton);
        }

        /// <summary>
        /// FUN_0003d354: width from ScreenPos + 0x3c4, insets at + 0x370 (left, top, right, bottom), button gap
        /// + 0x3c0; the box is centred on the screen.
        /// </summary>
        private void Layout()
        {
            ScreenPos aPos = mApp.mScreenPos;
            int aWidth = aPos.At(0x3c4);
            int aLeft = aPos.At(0x370);
            int aTextWidth = aWidth - aLeft - aPos.At(0x378);
            mText.Resize(aLeft, aPos.At(0x374), aTextWidth, 0);
            mText.Clear();
            mText.SetJustify(0);
            mText.SetFont(Res.FONT_LOCKERGNOME);
            mText.SetColor(TEXT_COLOR);
            mText.AddText(mTitle, 0);
            mText.NewLine(4);
            mText.SetFont(Res.FONT_ARIAL_LARGE);
            mText.SetColor(TEXT_COLOR);
            mText.AddText(mMessage, 0);
            mText.NewLine(4);
            mText.Fit(0);
            int aY = mText.mY + mText.mHeight;
            foreach (BookWormButton aButton in mButtons)
            {
                int aHeight = aButton.mButtonImage.mHeight;
                aY += aPos.At(0x3c0);
                aButton.Resize(aLeft, aY, aTextWidth, aHeight);
                aY += aHeight;
            }
            aY += aPos.At(0x37c);
            Resize((mApp.mWidth - aWidth) / 2, (mApp.mHeight - aY) / 2, aWidth, aY);
        }

        /// <summary>vf80 (0x3cdc4).</summary>
        public override void Show()
        {
            Layout();
            SetDisabled(true);
            base.Show();
            SetVisible(true);
            mResult = -1;
            mTimeline.Clear();
            mTimeline.AddEvent(ANIM_TICKS, theAlert => theAlert.SetDisabled(false));
            mTimeline.Start();
        }

        /// <summary>vf81 (0x3d4e0): closes after the timeline (FUN_0003d8b0), then tells the listener.</summary>
        public override void Close()
        {
            SetDisabled(true);
            mTimeline.Clear();
            mTimeline.AddEvent(ANIM_TICKS, theAlert =>
            {
                theAlert.SetVisible(false);
                theAlert.CloseNow();
                theAlert.mListener?.DialogButtonDepress(theAlert.mId, theAlert.mResult);
            });
            mTimeline.Start();
        }

        /// <summary>0x3d8dc.</summary>
        public override void Update()
        {
            if (mVisible && mTimeline.IsRunning())
            {
                mTimeline.AddTime(1f);
                mTimeline.Tick();
            }
        }

        /// <summary>0x3d918.</summary>
        public override void Draw(Graphics g)
        {
            g.DrawImageBox(new TRect(0, 0, mWidth, mHeight), mBackground);
        }
    }
}
