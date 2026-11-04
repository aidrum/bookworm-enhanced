using Microsoft.Xna.Framework;

namespace Sexy
{
    /// <summary>
    /// Sexy::Dialogoid (0x3e044): base of Bookworm's modal pop-ups (ActionSheet, Alert). Its buttons report
    /// presses to the dialog's listener as DialogButtonPress(dialog id, button id); a release records the
    /// button as the result and closes the dialog (subclasses animate out, then tell the listener
    /// DialogButtonDepress).
    /// </summary>
    public class Dialogoid : Widget, ButtonListener
    {
        /// <summary>Text colours of the two button styles (header statics 0xf0ff00 and 255,196,82).</summary>
        public static readonly Color GREEN_BUTTON_COLOR = new Color(0xf0, 0xff, 0x00, 0xff);

        public static readonly Color RED_BUTTON_COLOR = new Color(0xff, 0xc4, 0x52, 0xff);

        public int mId; // 0x7c

        public BookWormApp mApp; // 0x80

        public DialogListener mListener; // 0x84

        /// <summary>The button that closed the dialog (0x88, -1 none).</summary>
        public int mResult = -1;

        public Dialogoid(int theId, DialogListener theListener, BookWormApp theApp)
        {
            mId = theId;
            mApp = theApp;
            mListener = theListener;
        }

        /// <summary>FUN_0003e45c: a button on the green bar.</summary>
        protected BookWormButton MakeGreenButton(int theId, string theLabel)
        {
            return MakeButton(theId, theLabel, AtlasResourceBase.IMAGE_GROUP11_03, GREEN_BUTTON_COLOR);
        }

        /// <summary>FUN_0003e31c: a button on the red bar.</summary>
        protected BookWormButton MakeRedButton(int theId, string theLabel)
        {
            return MakeButton(theId, theLabel, AtlasResourceBase.IMAGE_GROUP11_02, RED_BUTTON_COLOR);
        }

        private BookWormButton MakeButton(int theId, string theLabel, Image theImage, Color theColor)
        {
            BookWormButton aButton = new BookWormButton(theId, this, mApp);
            aButton.SetFont(Res.FONT_COOPER_OUTLINED);
            aButton.SetColor(0, theColor);
            aButton.SetColor(1, theColor);
            aButton.SetStretchImage(true);
            aButton.mButtonImage = theImage;
            aButton.mLabel = theLabel;
            return aButton;
        }

        /// <summary>vf80 (0x3e59c): shown modally on top (z-order 100); the app pauses the game (slot 53).</summary>
        public virtual void Show()
        {
            mZOrder = 100;
            mApp.mWidgetManager.AddWidget(this);
            mApp.mWidgetManager.AddBaseModal(this);
            mApp.DialogOpened();
        }

        /// <summary>vf81 (0x3e5d0): removed and deleted; the app resumes (slot 54).</summary>
        public virtual void Close()
        {
            mApp.mWidgetManager.RemoveBaseModal(this);
            mApp.mWidgetManager.RemoveWidget(this);
            mApp.DialogClosed();
            mApp.SafeDeleteWidget(this);
        }

        /// <summary>Base close (Dialogoid::vf81 itself), for subclasses that animate first.</summary>
        protected void CloseNow()
        {
            mApp.mWidgetManager.RemoveBaseModal(this);
            mApp.mWidgetManager.RemoveWidget(this);
            mApp.DialogClosed();
            mApp.SafeDeleteWidget(this);
        }

        /// <summary>vf82 (0x3e60c).</summary>
        public virtual void ButtonPress(int theId)
        {
            mListener?.DialogButtonPress(mId, theId);
        }

        public void ButtonPress(int theId, int theClickCount)
        {
            ButtonPress(theId);
        }

        /// <summary>vf83 (0x3e634).</summary>
        public virtual void ButtonDepress(int theId)
        {
            mResult = theId;
            Close();
        }

        public void ButtonDownTick(int theId)
        {
        }

        public void ButtonMouseEnter(int theId)
        {
        }

        public void ButtonMouseLeave(int theId)
        {
        }

        public void ButtonMouseMove(int theId, int theX, int theY)
        {
        }
    }
}
