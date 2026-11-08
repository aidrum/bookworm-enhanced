using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Sexy
{
    /// <summary>
    /// Port addition: keyboard and controller control of the menus and dialogs (the board has its own selector,
    /// Board.Selector.cs). The focused item shows in a touch glow of its own, after the first key or pad input;
    /// mouse or touch input hides it (BookWormApp.PointerInputUsed).
    /// Keys: arrows/WASD move, Q/E or Page Up/Down switch tabs, Space/Enter select, Escape back.
    /// Pad: D-pad/left stick move, LT/RT (or LB/RB) switch tabs, A select, B back.
    /// </summary>
    public class MenuNavigator
    {
        private const int CMD_UP = 1;

        private const int CMD_DOWN = 2;

        private const int CMD_LEFT = 3;

        private const int CMD_RIGHT = 4;

        private const int CMD_PREV_TAB = 5;

        private const int CMD_NEXT_TAB = 6;

        private const int CMD_ACCEPT = 7;

        private const int CMD_BACK = 8;

        private const int REPEAT_DELAY = 400;

        private const int REPEAT_RATE = 130;

        private const float STICK_THRESHOLD = 0.5f;

        private const float TRIGGER_THRESHOLD = 0.5f;

        /// <summary>The tab of each interface state 2-9 (MenuTabWidget.STATE_TABS).</summary>
        private static readonly int[] STATE_TABS = { -1, -1, 0, 1, 2, 2, 2, 3, 4, 0 };

        /// <summary>The main menu's buttons, top to bottom (BWGameSelector ids).</summary>
        private static readonly int[] MAIN_MENU_BUTTONS = { BWGameSelector.BUTTON_CLASSIC, BWGameSelector.BUTTON_ARCADE, BWGameSelector.BUTTON_HOW_TO_PLAY };

        private readonly BookWormApp mApp;

        /// <summary>The focus glow (its own, so a touched button's glow doesn't move it).</summary>
        private TouchGlowWidget mGlow;

        /// <summary>Whether the focus shows (and moves).</summary>
        public bool mVisible;

        /// <summary>What the focus belongs to: a dialog, or a state (and the options page); a change resets it.</summary>
        private Widget mContextDialog;

        private int mContextKey = -1;

        /// <summary>The focused item of the context's list.</summary>
        private int mFocus;

        /// <summary>Books tab: 0 the collection books, 1 the word history (landscape: the two columns).</summary>
        private int mZone;

        /// <summary>Books tab: the focused collection book (mFocus is the history's).</summary>
        private int mBookFocus;

        private KeyboardState mPrevKeys;

        private int mHeldDir;

        private long mRepeatAt;

        private bool mPrevLeftTrigger;

        private bool mPrevRightTrigger;

        public MenuNavigator(BookWormApp theApp)
        {
            mApp = theApp;
            mPrevKeys = Keyboard.GetState();
        }

        /// <summary>Hides the focus (mouse or touch input).</summary>
        public void Hide()
        {
            mVisible = false;
            mGlow?.Disappear();
        }

        /// <summary>The board's selector has the input: the focus glow goes, the keys are tracked.</summary>
        public void Idle()
        {
            mPrevKeys = Keyboard.GetState();
            mHeldDir = 0;
            mGlow?.Disappear();
        }

        /// <summary>Each update while the board's selector doesn't take the input.</summary>
        public void Update(GamePadState thePad, GamePadState thePrevPad)
        {
            KeyboardState aKeys = Keyboard.GetState();
            List<int> aCommands = new List<int>();
            bool aPad = thePad.IsConnected;
            long aNow = System.Environment.TickCount64;
            float aDelta = mLastTick == 0 ? 0f : System.Math.Min(0.1f, (aNow - mLastTick) / 1000f);
            mLastTick = aNow;
            int aDir = aPad ? GetDPadDir(thePad) : 0;
            if (aDir == 0)
            {
                aDir = GetKeyDir(aKeys);
            }
            if (aDir == 0 && aPad)
            {
                // The stick moves a slider or the Stats page smoothly; elsewhere it steps like the D-pad.
                Vector2 aStick = thePad.ThumbSticks.Left;
                SyncContext();
                if (!AnalogUpdate(aStick, aDelta))
                {
                    aDir = GetStickDir(aStick);
                }
            }
            else
            {
                AnalogUpdate(Vector2.Zero, aDelta);
            }
            if (aDir == 0)
            {
                mHeldDir = 0;
            }
            else if (aDir != mHeldDir)
            {
                mHeldDir = aDir;
                mRepeatAt = aNow + REPEAT_DELAY;
                aCommands.Add(aDir);
            }
            else if (aNow >= mRepeatAt)
            {
                mRepeatAt = aNow + REPEAT_RATE;
                aCommands.Add(aDir);
            }
            bool aLeftTrigger = aPad && thePad.Triggers.Left > TRIGGER_THRESHOLD;
            bool aRightTrigger = aPad && thePad.Triggers.Right > TRIGGER_THRESHOLD;
            if ((aLeftTrigger && !mPrevLeftTrigger) || (aPad && Pressed(thePad.Buttons.LeftShoulder, thePrevPad.Buttons.LeftShoulder)) ||
                KeyPressed(aKeys, Keys.Q) || KeyPressed(aKeys, Keys.PageUp))
            {
                aCommands.Add(CMD_PREV_TAB);
            }
            if ((aRightTrigger && !mPrevRightTrigger) || (aPad && Pressed(thePad.Buttons.RightShoulder, thePrevPad.Buttons.RightShoulder)) ||
                KeyPressed(aKeys, Keys.E) || KeyPressed(aKeys, Keys.PageDown))
            {
                aCommands.Add(CMD_NEXT_TAB);
            }
            mPrevLeftTrigger = aLeftTrigger;
            mPrevRightTrigger = aRightTrigger;
            bool anAlt = aKeys.IsKeyDown(Keys.LeftAlt) || aKeys.IsKeyDown(Keys.RightAlt);
            if ((aPad && Pressed(thePad.Buttons.A, thePrevPad.Buttons.A)) || KeyPressed(aKeys, Keys.Space) ||
                (!anAlt && KeyPressed(aKeys, Keys.Enter)))
            {
                aCommands.Add(CMD_ACCEPT);
            }
            if ((aPad && Pressed(thePad.Buttons.B, thePrevPad.Buttons.B)) || KeyPressed(aKeys, Keys.X))
            {
                aCommands.Add(CMD_BACK);
            }
            mPrevKeys = aKeys;
            foreach (int aCommand in aCommands)
            {
                SyncContext();
                DoCommand(aCommand);
            }
            SyncContext();
            UpdateGlow();
        }

        private static bool Pressed(ButtonState theNow, ButtonState thePrev)
        {
            return theNow == ButtonState.Pressed && thePrev == ButtonState.Released;
        }

        private bool KeyPressed(KeyboardState theKeys, Keys theKey)
        {
            return theKeys.IsKeyDown(theKey) && mPrevKeys.IsKeyUp(theKey);
        }

        private static int GetDPadDir(GamePadState thePad)
        {
            if (thePad.DPad.Left == ButtonState.Pressed)
            {
                return CMD_LEFT;
            }
            if (thePad.DPad.Right == ButtonState.Pressed)
            {
                return CMD_RIGHT;
            }
            if (thePad.DPad.Up == ButtonState.Pressed)
            {
                return CMD_UP;
            }
            if (thePad.DPad.Down == ButtonState.Pressed)
            {
                return CMD_DOWN;
            }
            return 0;
        }

        private static int GetStickDir(Vector2 aStick)
        {
            if (System.Math.Max(System.Math.Abs(aStick.X), System.Math.Abs(aStick.Y)) < STICK_THRESHOLD)
            {
                return 0;
            }
            if (System.Math.Abs(aStick.X) >= System.Math.Abs(aStick.Y))
            {
                return aStick.X < 0 ? CMD_LEFT : CMD_RIGHT;
            }
            return aStick.Y > 0 ? CMD_UP : CMD_DOWN;
        }

        /// <summary>The stick's dead zone for the smooth slider and scrolling.</summary>
        private const float ANALOG_DEADZONE = 0.2f;

        /// <summary>A slider's whole range per second at full tilt.</summary>
        private const float SLIDER_SPEED = 0.8f;

        /// <summary>The Stats page's height per second at full tilt.</summary>
        private const float SCROLL_SPEED = 2f;

        private long mLastTick;

        /// <summary>The slider the stick is moving (dragging, so it doesn't play its sample or save every frame).</summary>
        private Slider mAnalogSlider;

        /// <summary>A stick axis past the dead zone, rescaled to 0-1 and eased (finer control near the middle).</summary>
        private static float AnalogScale(float theValue)
        {
            float aT = (System.Math.Abs(theValue) - ANALOG_DEADZONE) / (1f - ANALOG_DEADZONE);
            aT = System.Math.Max(0f, System.Math.Min(1f, aT));
            return System.Math.Sign(theValue) * aT * aT;
        }

        /// <summary>
        /// The stick's smooth moves: sideways on a focused volume slider, or up/down on the Stats page. False
        /// when it isn't one of those (the stick then steps like the D-pad).
        /// </summary>
        private bool AnalogUpdate(Vector2 theStick, float theDelta)
        {
            bool aHorizontal = System.Math.Abs(theStick.X) >= System.Math.Abs(theStick.Y);
            bool aNoDialog = GetDialog() == null;
            Slider aSlider = null;
            if (aNoDialog && mVisible && mApp.mInterfaceState == 7 && Screen<OptionsDialog>(BookWormApp.SCREEN_OPTIONS) is OptionsDialog anOptions)
            {
                List<Widget> anItems = GetOptions(anOptions);
                aSlider = mFocus < anItems.Count ? anItems[mFocus] as Slider : null;
            }
            if (aSlider != null && aHorizontal && System.Math.Abs(theStick.X) > ANALOG_DEADZONE)
            {
                if (mAnalogSlider != aSlider)
                {
                    ReleaseAnalogSlider();
                    mAnalogSlider = aSlider;
                }
                aSlider.mDragging = true;
                aSlider.SetValue(aSlider.mVal + AnalogScale(theStick.X) * SLIDER_SPEED * theDelta);
                aSlider.mListener?.SliderVal(aSlider.mId, aSlider.mVal);
                return true;
            }
            ReleaseAnalogSlider();
            if (aNoDialog && mApp.mInterfaceState == 3 && !aHorizontal && System.Math.Abs(theStick.Y) > ANALOG_DEADZONE)
            {
                ScrollWidget aScroll = Screen<WordBookWidget>(BookWormApp.SCREEN_STATS)?.mScroll;
                aScroll?.ScrollBy(0, AnalogScale(theStick.Y) * aScroll.mHeight * SCROLL_SPEED * theDelta, false);
                return true;
            }
            return false;
        }

        /// <summary>Lets go of the stick's slider: it plays its sample (effects) and the settings are saved.</summary>
        private void ReleaseAnalogSlider()
        {
            Slider aSlider = mAnalogSlider;
            if (aSlider == null)
            {
                return;
            }
            mAnalogSlider = null;
            aSlider.mDragging = false;
            aSlider.mListener?.SliderVal(aSlider.mId, aSlider.mVal);
        }

        private static int GetKeyDir(KeyboardState theKeys)
        {
            if (theKeys.IsKeyDown(Keys.Left) || theKeys.IsKeyDown(Keys.A))
            {
                return CMD_LEFT;
            }
            if (theKeys.IsKeyDown(Keys.Right) || theKeys.IsKeyDown(Keys.D))
            {
                return CMD_RIGHT;
            }
            if (theKeys.IsKeyDown(Keys.Up) || theKeys.IsKeyDown(Keys.W))
            {
                return CMD_UP;
            }
            if (theKeys.IsKeyDown(Keys.Down) || theKeys.IsKeyDown(Keys.S))
            {
                return CMD_DOWN;
            }
            return 0;
        }

        // ---------------------------------------------------------------- context

        private Dialogoid GetDialog()
        {
            return mApp.mWidgetManager?.mBaseModalWidget as Dialogoid;
        }

        /// <summary>Resets the focus when the dialog, the state or the options page changed.</summary>
        private void SyncContext()
        {
            Dialogoid aDialog = GetDialog();
            int aKey = mApp.mInterfaceState * 2;
            if (mApp.mInterfaceState == 7 && Screen<OptionsDialog>(BookWormApp.SCREEN_OPTIONS) is OptionsDialog anOptions && anOptions.mExtraPage)
            {
                aKey++;
            }
            if (aDialog == mContextDialog && aKey == mContextKey)
            {
                return;
            }
            mContextDialog = aDialog;
            mContextKey = aKey;
            mFocus = 0;
            mBookFocus = 0;
            mZone = 0;
            if (aDialog == null && (mApp.mInterfaceState == 4 || mApp.mInterfaceState == 13))
            {
                List<int> aBooks = GetPickableBooks();
                mZone = aBooks.Count > 0 ? 0 : 1;
            }
        }

        private T Screen<T>(int theScreen) where T : class
        {
            return mApp.mScreens[theScreen] as T;
        }

        /// <summary>Shows the focus; true when it was hidden (a move then only shows it).</summary>
        private bool Reveal()
        {
            if (mVisible)
            {
                return false;
            }
            mVisible = true;
            return true;
        }

        private void DoCommand(int theCommand)
        {
            if (theCommand == CMD_BACK)
            {
                mApp.BackButtonPress();
                return;
            }
            Dialogoid aDialog = GetDialog();
            if (aDialog != null)
            {
                DialogCommand(aDialog, theCommand);
                return;
            }
            if (Busy())
            {
                return;
            }
            switch (mApp.mInterfaceState)
            {
            case 2:
                MainMenuCommand(theCommand);
                break;
            case 3:
                ScrollCommand(Screen<WordBookWidget>(BookWormApp.SCREEN_STATS)?.mScroll, theCommand, true);
                break;
            case 4:
            case 13:
                BooksCommand(theCommand);
                break;
            case 5:
            case 12:
            case 14:
                BookPageCommand(Screen<WordBookWidget>(BookWormApp.SCREEN_WORD_COLLECTION), theCommand);
                break;
            case 6:
            case 15:
                BookPageCommand(Screen<WordBookWidget>(BookWormApp.SCREEN_WORD_HISTORY), theCommand);
                break;
            case 7:
                OptionsCommand(theCommand);
                break;
            case 8:
                MoreGamesCommand(theCommand);
                break;
            case 9:
                HelpCommand(theCommand);
                break;
            case 11:
                BookPageCommand(Screen<WordBookWidget>(BookWormApp.SCREEN_LEVEL_UP), theCommand);
                break;
            }
        }

        /// <summary>
        /// Back (BookWormApp.BackButtonPress: Escape, B, Back, Start): a book page's X or CONTINUE. False
        /// elsewhere (the widgets get it).
        /// </summary>
        public bool BackPressed()
        {
            if (GetDialog() != null || Busy())
            {
                return false;
            }
            WordBookWidget aPage = null;
            switch (mApp.mInterfaceState)
            {
            case 5:
            case 12:
            case 14:
                aPage = Screen<WordBookWidget>(BookWormApp.SCREEN_WORD_COLLECTION);
                break;
            case 6:
            case 15:
                aPage = Screen<WordBookWidget>(BookWormApp.SCREEN_WORD_HISTORY);
                break;
            case 11:
                aPage = Screen<WordBookWidget>(BookWormApp.SCREEN_LEVEL_UP);
                break;
            case 7:
                // The extra options go back to the options first (OptionsDialog.BackButtonPress).
                if (Screen<OptionsDialog>(BookWormApp.SCREEN_OPTIONS) is OptionsDialog anOptions && anOptions.mExtraPage)
                {
                    return false;
                }
                GoToPlayMenu();
                return true;
            case 3:
            case 4:
            case 8:
            case 9:
                GoToPlayMenu();
                return true;
            }
            ButtonWidget aButton = GetPageButton(aPage);
            if (aButton == null)
            {
                return false;
            }
            Click(aButton);
            return true;
        }

        // ---------------------------------------------------------------- helpers

        /// <summary>
        /// Screens are moving (BookWormApp.mInterfaceLock): they are disabled and take no taps, so no keys or pad
        /// either; a state change mid-move would leave them disabled for good.
        /// </summary>
        private bool Busy()
        {
            return mApp.mInterfaceLock != 0;
        }

        private static void Click(ButtonWidget theButton)
        {
            if (theButton?.mButtonListener == null || theButton.mDisabled)
            {
                return;
            }
            theButton.mButtonListener.ButtonPress(theButton.mId);
            theButton.mButtonListener.ButtonDepress(theButton.mId);
        }

        private static ButtonWidget FindButton(Widget theParent, int theId)
        {
            if (theParent == null)
            {
                return null;
            }
            foreach (Widget aChild in theParent.mWidgets)
            {
                if (aChild is ButtonWidget aButton && aButton.mId == theId)
                {
                    return aButton;
                }
            }
            return null;
        }

        private static bool Usable(Widget theWidget)
        {
            return theWidget != null && theWidget.mVisible && !theWidget.mDisabled;
        }

        private static TRect AbsRect(Widget theWidget)
        {
            TPoint aPos = theWidget.GetAbsPos();
            return new TRect(aPos.mX, aPos.mY, theWidget.mWidth, theWidget.mHeight);
        }

        private static TRect AbsRect(Widget theWidget, TRect theRect)
        {
            TPoint aPos = theWidget.GetAbsPos();
            return new TRect(aPos.mX + theRect.mX, aPos.mY + theRect.mY, theRect.mWidth, theRect.mHeight);
        }

        /// <summary>Moves the focus along a list of theCount (wrapping or not); false when hidden (only shown).</summary>
        private bool MoveFocus(int theDelta, int theCount, bool theWrap)
        {
            if (Reveal() || theCount == 0)
            {
                return false;
            }
            int aFocus = mFocus + theDelta;
            if (theWrap)
            {
                aFocus = (aFocus % theCount + theCount) % theCount;
            }
            mFocus = System.Math.Max(0, System.Math.Min(theCount - 1, aFocus));
            return true;
        }

        /// <summary>Left/right or LT/RT: the next or previous menu tab (states 2-9), as tapping it.</summary>
        private void SwitchTab(int theDelta)
        {
            int aState = mApp.mInterfaceState;
            if (aState < 2 || aState >= STATE_TABS.Length)
            {
                return;
            }
            int aTab = STATE_TABS[aState] + theDelta;
            if (aTab < MenuTabWidget.TAB_PLAY || aTab > MenuTabWidget.TAB_MORE_GAMES || aTab == STATE_TABS[aState])
            {
                return;
            }
            MenuTabWidget aTabs = Screen<MenuTabWidget>(BookWormApp.SCREEN_MENU_TABS);
            if (aTabs == null || aTabs.mDisabled)
            {
                return;
            }
            aTabs.ButtonPress(aTab);
            aTabs.ButtonDepress(aTab);
        }

        /// <summary>Back on a menu page: the Play tab (the main menu), as tapping it.</summary>
        private void GoToPlayMenu()
        {
            MenuTabWidget aTabs = Screen<MenuTabWidget>(BookWormApp.SCREEN_MENU_TABS);
            if (aTabs == null || aTabs.mDisabled)
            {
                return;
            }
            aTabs.ButtonPress(MenuTabWidget.TAB_PLAY);
            aTabs.ButtonDepress(MenuTabWidget.TAB_PLAY);
        }

        /// <summary>The tab commands: left/right and LT/RT switch tabs; true when theCommand was one.</summary>
        private bool TabCommand(int theCommand, bool theLeftRight)
        {
            if (theCommand == CMD_PREV_TAB || (theLeftRight && theCommand == CMD_LEFT))
            {
                SwitchTab(-1);
                return true;
            }
            if (theCommand == CMD_NEXT_TAB || (theLeftRight && theCommand == CMD_RIGHT))
            {
                SwitchTab(1);
                return true;
            }
            return false;
        }

        /// <summary>Up/down scroll theScroll a fifth of its height; true when theCommand was one.</summary>
        private static bool ScrollStep(ScrollWidget theScroll, int theCommand)
        {
            if (theCommand != CMD_UP && theCommand != CMD_DOWN)
            {
                return false;
            }
            if (theScroll != null)
            {
                float aStep = System.Math.Max(1, theScroll.mHeight / 5);
                theScroll.ScrollBy(0, theCommand == CMD_UP ? aStep : -aStep);
            }
            return true;
        }

        /// <summary>A scrolling page with no buttons (Stats): up/down scroll, the rest switch tabs.</summary>
        private void ScrollCommand(ScrollWidget theScroll, int theCommand, bool theTabs)
        {
            if (!ScrollStep(theScroll, theCommand) && theTabs)
            {
                TabCommand(theCommand, true);
            }
        }

        // ---------------------------------------------------------------- dialogs

        private static List<ButtonWidget> GetDialogButtons(Dialogoid theDialog)
        {
            List<ButtonWidget> aButtons = new List<ButtonWidget>();
            foreach (Widget aChild in theDialog.mWidgets)
            {
                if (aChild is ButtonWidget aButton && Usable(aButton))
                {
                    aButtons.Add(aButton);
                }
            }
            aButtons.Sort((a, b) => a.mY != b.mY ? a.mY.CompareTo(b.mY) : a.mX.CompareTo(b.mX));
            return aButtons;
        }

        /// <summary>Popups: up/down switch buttons, A presses (at once when it is the only one).</summary>
        private void DialogCommand(Dialogoid theDialog, int theCommand)
        {
            List<ButtonWidget> aButtons = GetDialogButtons(theDialog);
            switch (theCommand)
            {
            case CMD_UP:
                MoveFocus(-1, aButtons.Count, false);
                break;
            case CMD_DOWN:
                MoveFocus(1, aButtons.Count, false);
                break;
            case CMD_ACCEPT:
                if (Reveal() && aButtons.Count > 1)
                {
                    break;
                }
                if (mFocus < aButtons.Count)
                {
                    Click(aButtons[mFocus]);
                }
                break;
            }
        }

        // ---------------------------------------------------------------- main menu

        private List<ButtonWidget> GetMainMenuButtons()
        {
            List<ButtonWidget> aButtons = new List<ButtonWidget>();
            Widget aSelector = mApp.mScreens[BookWormApp.SCREEN_GAME_SELECTOR];
            foreach (int anId in MAIN_MENU_BUTTONS)
            {
                ButtonWidget aButton = FindButton(aSelector, anId);
                if (Usable(aButton))
                {
                    aButtons.Add(aButton);
                }
            }
            return aButtons;
        }

        private void MainMenuCommand(int theCommand)
        {
            if (TabCommand(theCommand, true))
            {
                return;
            }
            List<ButtonWidget> aButtons = GetMainMenuButtons();
            switch (theCommand)
            {
            case CMD_UP:
                MoveFocus(-1, aButtons.Count, false);
                break;
            case CMD_DOWN:
                MoveFocus(1, aButtons.Count, false);
                break;
            case CMD_ACCEPT:
                if (!Reveal() && mFocus < aButtons.Count)
                {
                    Click(aButtons[mFocus]);
                }
                break;
            }
        }

        // ---------------------------------------------------------------- book pages

        /// <summary>A book page's button: CONTINUE when shown, else X.</summary>
        private static ButtonWidget GetPageButton(WordBookWidget thePage)
        {
            if (thePage == null)
            {
                return null;
            }
            if (Usable(thePage.mContinueButton))
            {
                return thePage.mContinueButton;
            }
            return Usable(thePage.mCloseButton) ? thePage.mCloseButton : null;
        }

        /// <summary>Collection, history and level up pages: up/down scroll, A presses X or CONTINUE.</summary>
        private void BookPageCommand(WordBookWidget thePage, int theCommand)
        {
            if (thePage == null || ScrollStep(thePage.mScroll, theCommand))
            {
                return;
            }
            if (theCommand == CMD_ACCEPT)
            {
                Reveal();
                Click(GetPageButton(thePage));
                return;
            }
            int aState = mApp.mInterfaceState;
            TabCommand(theCommand, aState == 5 || aState == 6);
        }

        // ---------------------------------------------------------------- books tab

        private BookshelfWidget GetBookshelf()
        {
            return Screen<BookshelfWidget>(BookWormApp.SCREEN_BOOKSHELF);
        }

        private List<int> GetPickableBooks()
        {
            List<int> aBooks = new List<int>();
            BookshelfWidget aShelf = GetBookshelf();
            if (aShelf == null)
            {
                return aBooks;
            }
            for (int i = 0; i < aShelf.mCollectionShelf.mRects.Length; i++)
            {
                if (aShelf.mCollectionShelf.IsPickable(i))
                {
                    aBooks.Add(i);
                }
            }
            return aBooks;
        }

        /// <summary>
        /// The Books tab. Portrait: the collection books (left/right pick one, down goes to the history) over the
        /// word history (up/down go along it, scrolling; up from the first goes back to the books; left/right
        /// switch tabs). Landscape (state 13): left/right go between the two columns, up/down along one.
        /// LT/RT always switch tabs.
        /// </summary>
        private void BooksCommand(int theCommand)
        {
            BookshelfWidget aShelf = GetBookshelf();
            if (aShelf == null)
            {
                return;
            }
            bool aLandscape = mApp.mInterfaceState == 13;
            if (theCommand == CMD_PREV_TAB || theCommand == CMD_NEXT_TAB)
            {
                TabCommand(theCommand, false);
                return;
            }
            List<int> aBooks = GetPickableBooks();
            int aHistoryCount = aShelf.mHistoryShelf.mRects.Length;
            if (theCommand == CMD_ACCEPT)
            {
                if (Reveal())
                {
                    return;
                }
                if (mZone == 0 && mBookFocus < aBooks.Count)
                {
                    aShelf.mCollectionShelf.mListener.ShelfPicked(aShelf.mCollectionShelf.mId, aBooks[mBookFocus]);
                }
                else if (mZone == 1 && mFocus < aHistoryCount)
                {
                    aShelf.mHistoryShelf.mListener.ShelfPicked(aShelf.mHistoryShelf.mId, mFocus);
                }
                return;
            }
            if (aLandscape)
            {
                if (theCommand == CMD_LEFT || theCommand == CMD_RIGHT)
                {
                    if (Reveal())
                    {
                        return;
                    }
                    // The column that way: the collection's or the history's, by where they are.
                    bool aBooksLeft = aShelf.mCollectionShelf.GetAbsPos().mX <= aShelf.mScroll.GetAbsPos().mX;
                    int aZone = (theCommand == CMD_LEFT) == aBooksLeft ? 0 : 1;
                    if (aZone == 0 && aBooks.Count == 0)
                    {
                        return;
                    }
                    mZone = aZone;
                }
                else if (mZone == 0)
                {
                    MoveBookFocus(theCommand == CMD_UP ? -1 : 1, aBooks.Count);
                }
                else
                {
                    MoveHistoryFocus(aShelf, theCommand == CMD_UP ? -1 : 1, aHistoryCount);
                }
                return;
            }
            if (mZone == 0)
            {
                switch (theCommand)
                {
                case CMD_LEFT:
                    MoveBookFocus(-1, aBooks.Count);
                    break;
                case CMD_RIGHT:
                    MoveBookFocus(1, aBooks.Count);
                    break;
                case CMD_DOWN:
                    if (!Reveal())
                    {
                        mZone = 1;
                        mFocus = 0;
                        aShelf.mScroll.ScrollToMin(true);
                    }
                    break;
                case CMD_UP:
                    Reveal();
                    break;
                }
                return;
            }
            switch (theCommand)
            {
            case CMD_UP:
                if (mFocus == 0 && mVisible && aBooks.Count > 0)
                {
                    mZone = 0;
                    aShelf.mScroll.ScrollToMin(true);
                    break;
                }
                MoveHistoryFocus(aShelf, -1, aHistoryCount);
                break;
            case CMD_DOWN:
                MoveHistoryFocus(aShelf, 1, aHistoryCount);
                break;
            default:
                TabCommand(theCommand, true);
                break;
            }
        }

        private void MoveBookFocus(int theDelta, int theCount)
        {
            if (Reveal() || theCount == 0)
            {
                return;
            }
            mBookFocus = System.Math.Max(0, System.Math.Min(theCount - 1, mBookFocus + theDelta));
        }

        private void MoveHistoryFocus(BookshelfWidget theShelf, int theDelta, int theCount)
        {
            if (MoveFocus(theDelta, theCount, false))
            {
                theShelf.mScroll.ScrollRectIntoView(theShelf.mHistoryShelf.mRects[mFocus], true);
            }
        }

        // ---------------------------------------------------------------- options

        /// <summary>The options of the page shown, top to bottom; then More Options (or Back) and the language.</summary>
        private static List<Widget> GetOptions(OptionsDialog theOptions)
        {
            Widget[] anAll = theOptions.mExtraPage
                ? new Widget[] { theOptions.mLandscapeBooksSwitch, theOptions.mSubmitByDraggingSwitch, theOptions.mCheckForUpdatesSwitch,
                    theOptions.mLayoutButton, theOptions.mTexturesButton, theOptions.mPageLink, theOptions.mLanguageLink }
                : new Widget[] { theOptions.mSfxSlider, theOptions.mMusicSlider, theOptions.mHintsSwitch, theOptions.mDefinitionsSwitch,
                    theOptions.mConfirmScrambleSwitch, theOptions.mPageLink, theOptions.mLanguageLink };
            List<Widget> anOptions = new List<Widget>();
            foreach (Widget aWidget in anAll)
            {
                if (aWidget != null && aWidget.mVisible)
                {
                    anOptions.Add(aWidget);
                }
            }
            return anOptions;
        }

        /// <summary>
        /// Options: up/down go down the page and round (the language after More Options, then the top); left/right
        /// move a slider, or switch tabs; A flips a switch or presses a button (the page links reset the focus to
        /// the top, SyncContext).
        /// </summary>
        private void OptionsCommand(int theCommand)
        {
            OptionsDialog anOptions = Screen<OptionsDialog>(BookWormApp.SCREEN_OPTIONS);
            if (anOptions == null)
            {
                return;
            }
            List<Widget> anItems = GetOptions(anOptions);
            Widget aFocused = mFocus < anItems.Count ? anItems[mFocus] : null;
            switch (theCommand)
            {
            case CMD_UP:
                MoveFocus(-1, anItems.Count, true);
                return;
            case CMD_DOWN:
                MoveFocus(1, anItems.Count, true);
                return;
            case CMD_LEFT:
            case CMD_RIGHT:
                if (mVisible && aFocused is Slider aSlider)
                {
                    double aValue = System.Math.Max(0.0, System.Math.Min(1.0, aSlider.mVal + (theCommand == CMD_LEFT ? -0.1 : 0.1)));
                    aSlider.SetValue(aValue);
                    aSlider.mListener?.SliderVal(aSlider.mId, aSlider.mVal);
                    return;
                }
                TabCommand(theCommand, true);
                return;
            case CMD_ACCEPT:
                if (Reveal() || aFocused == null || aFocused.mDisabled)
                {
                    return;
                }
                if (aFocused is Switch aSwitch)
                {
                    aSwitch.SetOn(!aSwitch.mOn, true);
                    aSwitch.MarkDirty();
                }
                else if (aFocused is ButtonWidget aButton)
                {
                    Click(aButton);
                }
                return;
            default:
                TabCommand(theCommand, false);
                return;
            }
        }

        // ---------------------------------------------------------------- more games, help

        private void MoreGamesCommand(int theCommand)
        {
            MoreGamesWidget aMoreGames = Screen<MoreGamesWidget>(BookWormApp.SCREEN_MORE_GAMES);
            if (aMoreGames == null || aMoreGames.mList == null)
            {
                return;
            }
            MoreGamesListWidget aList = aMoreGames.mList;
            int aCount = aList.mRows.Count;
            switch (theCommand)
            {
            case CMD_UP:
            case CMD_DOWN:
                if (MoveFocus(theCommand == CMD_UP ? -1 : 1, aCount, false))
                {
                    aMoreGames.mScroll.ScrollRectIntoView(aList.mRows[mFocus].mRect, true);
                }
                return;
            case CMD_ACCEPT:
                if (!Reveal() && mFocus < aCount)
                {
                    mApp.PlaySound(Res.SOUND_BUTTON_CLICK);
                    aList.mListener?.DialogButtonDepress(aList.mId, mFocus);
                }
                return;
            default:
                TabCommand(theCommand, true);
                return;
            }
        }

        /// <summary>How to Play: up/down turn the pages.</summary>
        private void HelpCommand(int theCommand)
        {
            HelpWidget aHelp = Screen<HelpWidget>(BookWormApp.SCREEN_HELP);
            if (theCommand == CMD_UP || theCommand == CMD_DOWN)
            {
                aHelp?.TurnPage(theCommand == CMD_UP ? -1 : 1);
                return;
            }
            TabCommand(theCommand, true);
        }

        // ---------------------------------------------------------------- the glow

        /// <summary>Where the focus is, if the context has one.</summary>
        private bool GetFocusRect(out TRect theRect)
        {
            theRect = default(TRect);
            Dialogoid aDialog = GetDialog();
            if (aDialog != null)
            {
                List<ButtonWidget> aButtons = GetDialogButtons(aDialog);
                if (mFocus >= aButtons.Count)
                {
                    return false;
                }
                theRect = AbsRect(aButtons[mFocus]);
                return true;
            }
            switch (mApp.mInterfaceState)
            {
            case 2:
            {
                List<ButtonWidget> aButtons = GetMainMenuButtons();
                if (mFocus >= aButtons.Count)
                {
                    return false;
                }
                theRect = AbsRect(aButtons[mFocus]);
                return true;
            }
            case 4:
            case 13:
            {
                BookshelfWidget aShelf = GetBookshelf();
                if (aShelf == null)
                {
                    return false;
                }
                if (mZone == 0)
                {
                    List<int> aBooks = GetPickableBooks();
                    if (mBookFocus >= aBooks.Count)
                    {
                        return false;
                    }
                    theRect = AbsRect(aShelf.mCollectionShelf, aShelf.mCollectionShelf.mRects[aBooks[mBookFocus]]);
                    return true;
                }
                if (mFocus >= aShelf.mHistoryShelf.mRects.Length)
                {
                    return false;
                }
                theRect = AbsRect(aShelf.mHistoryShelf, aShelf.mHistoryShelf.mRects[mFocus]);
                return true;
            }
            case 5:
            case 12:
            case 14:
                return GetPageRect(Screen<WordBookWidget>(BookWormApp.SCREEN_WORD_COLLECTION), out theRect);
            case 6:
            case 15:
                return GetPageRect(Screen<WordBookWidget>(BookWormApp.SCREEN_WORD_HISTORY), out theRect);
            case 11:
                return GetPageRect(Screen<WordBookWidget>(BookWormApp.SCREEN_LEVEL_UP), out theRect);
            case 7:
            {
                OptionsDialog anOptions = Screen<OptionsDialog>(BookWormApp.SCREEN_OPTIONS);
                if (anOptions == null)
                {
                    return false;
                }
                List<Widget> anItems = GetOptions(anOptions);
                if (mFocus >= anItems.Count)
                {
                    return false;
                }
                theRect = AbsRect(anItems[mFocus]);
                return true;
            }
            case 8:
            {
                MoreGamesListWidget aList = Screen<MoreGamesWidget>(BookWormApp.SCREEN_MORE_GAMES)?.mList;
                if (aList == null || mFocus >= aList.mRows.Count)
                {
                    return false;
                }
                theRect = AbsRect(aList, aList.mRows[mFocus].mRect);
                return true;
            }
            }
            return false;
        }

        private static bool GetPageRect(WordBookWidget thePage, out TRect theRect)
        {
            theRect = default(TRect);
            ButtonWidget aButton = GetPageButton(thePage);
            if (aButton == null)
            {
                return false;
            }
            theRect = AbsRect(aButton);
            return true;
        }

        private void UpdateGlow()
        {
            if (!mVisible || mApp.mWidgetManager == null || !GetFocusRect(out TRect aRect))
            {
                mGlow?.Disappear();
                return;
            }
            if (mGlow == null)
            {
                mGlow = new TouchGlowWidget(mApp);
                mGlow.mZOrder = 0x6f;
                mApp.mWidgetManager.AddWidget(mGlow);
            }
            // SetGlowRect places a rect smaller than the glow's minimum wrongly (the original's centring, kept for
            // touches): small ones (the Options text links) are grown about their centre to the minimum first.
            int aMinWidth = mGlow.mImage.mWidth / 3 * 2 - mGlow.mInsets.mX - mGlow.mInsets.mWidth;
            int aMinHeight = mGlow.mImage.mHeight / 3 * 2 - mGlow.mInsets.mY - mGlow.mInsets.mHeight;
            if (aRect.mWidth < aMinWidth)
            {
                aRect.mX -= (aMinWidth - aRect.mWidth) / 2;
                aRect.mWidth = aMinWidth;
            }
            if (aRect.mHeight < aMinHeight)
            {
                aRect.mY -= (aMinHeight - aRect.mHeight) / 2;
                aRect.mHeight = aMinHeight;
            }
            mGlow.SetGlowRect(aRect);
            mGlow.Appear();
        }
    }
}
