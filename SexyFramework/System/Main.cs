using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;

namespace Sexy
{
    /// <summary>
    /// MonoGame host for a SexyApp game. Adapted from PlantsVsZombies.NET's Lawn_PCGL Main:
    /// the WP7 GamerServices, IronPython modding and JSON config handling are removed, and the
    /// concrete app is supplied by the game through <see cref="AppFactory"/>.
    /// </summary>
    public class Main : Game
    {
        public static Graphics graphics;

        public static Func<Main, SexyAppBase> AppFactory;

        /// <summary>Full-screen platforms (Android, iOS): the screen's size in pixels, for the backbuffer.</summary>
        public static Point? NativeScreenSize;

        /// <summary>
        /// Port: phones and tablets (Android, iOS). Full screen, touch only, lazy loading and no PC Controls;
        /// desktops get a resizable window with mouse, keyboard and text input.
        /// </summary>
        public static bool IsMobile => OperatingSystem.IsAndroid() || OperatingSystem.IsIOS();

        /// <summary>
        /// Port: set by the platform (Android, iOS): how far down from the screen's top a camera cutout (notch) reaches,
        /// in screen pixels as the device is held; 0 without one, and on the desktop.
        /// </summary>
        public static volatile int SafeInsetTop;

        /// <summary>Port: SafeInsetTop in the game's coordinates (the letterboxed layout's), 0 below its top.</summary>
        public static int GetSafeInsetTopInGame()
        {
            SexyAppBase anApp = GlobalStaticVars.gSexyAppBase;
            int anInset = SafeInsetTop;
            if (!IsMobile || anInset <= 0 || anApp == null)
            {
                return 0;
            }
            return Math.Max(0, (int)Math.Ceiling(anApp.mScreenScales.InvMapTouchY(anInset)));
        }

        /// <summary>Set by the platform (Android's back button); handled as a back press on the next update.</summary>
        public static volatile bool BackRequested;

        /// <summary>
        /// Port: accelerometer samples from the platform (Android's sensor; the iOS app got them from
        /// UIAccelerometer), in iOS units (g, with UIAcceleration's signs) and seconds. Handed to
        /// SexyAppBase.AccelerometerDidAccelerate on the next update, on the game thread (shaking scrambles).
        /// </summary>
        public static readonly System.Collections.Concurrent.ConcurrentQueue<double[]> AccelerometerSamples = new System.Collections.Concurrent.ConcurrentQueue<double[]>();

        /// <summary>
        /// Set by a platform host to close the app itself. On Android, Game.Exit only moves the task to
        /// the back, so the still-running game kept exiting every time it was reopened.
        /// </summary>
        public static Action ExitHandler;

        /// <summary>
        /// Set by a platform host that opens web links itself (Android starts a view intent). Without
        /// one, SexyAppBase.OpenURL hands the link to the desktop's default browser.
        /// </summary>
        public static Action<string> OpenUrlHandler;

        /// <summary>
        /// Port addition (Android): switches the launcher icon to the alias named first (one of the second's,
        /// the app's icon aliases, which are all others disabled). Null where the platform can't. Android ends
        /// the app's task when its launcher alias changes, so the game quits (saving) to apply it.
        /// </summary>
        public static Action<string, string[]> SetAppIconHandler;

        /// <summary>Port addition (Android): which of the given icon aliases the launcher shows.</summary>
        public static Func<string[], string> GetAppIconHandler;

        public static bool LOW_MEMORY_DEVICE { get; private set; }

        public static bool DO_LOW_MEMORY_OPTIONS { get; private set; }

        public static bool IsInTrialMode => false;

        private static UI_ORIENTATION orientationUsed;

        private static bool newOrientation;

        private static bool wantToSuppressDraw;

        private MouseState previousMouseState;

        // UIKit-style tap counting (UITouch.tapCount): the game toggles its zoom on a double tap.
        private const double MULTI_TAP_SECONDS = 0.35;

        private const float MULTI_TAP_DISTANCE = 40f;

        private int mTapCount;

        private double mLastTapTime = double.NegativeInfinity;

        private CGPoint mLastTapPos;

        private int mMouseClickCount = 1;

        private KeyboardState previousKeyboardState;

        private GamePadState previousGamepadState;

        public string applicationStoragePath;

        public Main()
        {
            Main.graphics = Graphics.GetNew(this);
            Main.graphics.IsFullScreen = IsMobile;
            if (IsMobile)
            {
                // Port: the game is always laid out portrait, but follows the device whichever way it is held
                // (a landscape screen gets the portrait layout pillarboxed; see OnResize).
                Constants.SupportedOrientations = DisplayOrientation.Portrait | DisplayOrientation.PortraitDown |
                    DisplayOrientation.LandscapeLeft | DisplayOrientation.LandscapeRight;
                Main.graphics.GraphicsDeviceManager.SupportedOrientations = Constants.SupportedOrientations;
            }
            // The original universal app ran at the device's own screen (one of five it knew); a full screen
            // (Android, iOS) gets a layout of its own aspect, a desktop window one that suits the monitor.
            if (NativeScreenSize is Point aScreen)
            {
                Constants.ChooseLayout(aScreen.X, aScreen.Y);
            }
            else
            {
                DisplayMode aMode = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
                Constants.ChooseDesktopLayout(aMode.Width, aMode.Height);
            }
            // A backbuffer of the game's size gets letterboxed by MonoGame Android through the
            // viewport alone, which is lost whenever a render target is unset; there the backbuffer
            // takes the whole screen and ScreenScales letterboxes, as it does in a resized window.
            Main.graphics.PreferredBackBufferWidth = NativeScreenSize?.X ?? Constants.DefaultWindowWidth;
            Main.graphics.PreferredBackBufferHeight = NativeScreenSize?.Y ?? Constants.DefaultWindowHeight;
            GraphicsState.mGraphicsDeviceManager.DeviceCreated += graphics_DeviceCreated;
            // The game renders into offscreen images mid-frame (e.g. floating texts build their
            // image lazily in Draw). With the default DiscardContents, switching back to the
            // backbuffer clears everything drawn so far, which flashes black for a frame.
            GraphicsState.mGraphicsDeviceManager.PreparingDeviceSettings += (sender, e) =>
                e.GraphicsDeviceInformation.PresentationParameters.RenderTargetUsage = RenderTargetUsage.PreserveContents;
            base.TargetElapsedTime = TimeSpan.FromSeconds(0.01);
            base.Exiting += Main_Exiting;
            Window.AllowUserResizing = true;
            base.Window.ClientSizeChanged += OnResize;
            IsMouseVisible = true;
            if (!IsMobile)
            {
                HookTextInput();
            }
            DebugScript.Init();
        }

        /// <summary>
        /// Kept out of the constructor: GameWindow.TextInput only exists in the desktop MonoGame
        /// builds, so referencing it inline would fail when the constructor is compiled on Android/iOS.
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private void HookTextInput()
        {
            Window.TextInput += OnTextInput;
        }

        private void OnTextInput(object sender, TextInputEventArgs e)
        {
            // Backspace/Enter arrive as KeyDown; only printable characters are typed text.
            if (e.Character >= ' ' && GlobalStaticVars.gSexyAppBase?.mWidgetManager != null)
            {
                GlobalStaticVars.gSexyAppBase.mWidgetManager.KeyChar(new SexyChar(e.Character));
            }
        }

        public void OnResize(object sender, EventArgs e)
        {
            Rectangle bounds = WindowBounds();
            mFittedSize = new Point(bounds.Width, bounds.Height);
            if (GlobalStaticVars.gSexyAppBase != null && bounds.Width > 0 && bounds.Height > 0)
            {
                FitToWindow();
                // Port: the layout follows the window's shape (dynamic aspect), once it has settled.
                mPendingLayout = Constants.RelayoutFor(bounds.Width, bounds.Height);
                mPendingLayoutAge = 0;
            }
        }

        /// <summary>Scales the game's current layout into the window (letterboxed where their shapes differ).</summary>
        private void FitToWindow()
        {
            Rectangle bounds = WindowBounds();
            if (bounds.Width <= 0 || bounds.Height <= 0)
            {
                return;
            }
            MatchBackBufferToWindow(bounds);
            GlobalStaticVars.gSexyAppBase.mScreenScales.Init(bounds.Width, bounds.Height, Constants.ScreenWidth, Constants.ScreenHeight);
            Graphics.Resized();
        }

        /// <summary>
        /// Port: the window's (or surface's) size, at (0, 0). On the desktop from SDL itself (SdlWindow):
        /// ClientBounds went stale when the window was maximized after leaving full screen.
        /// </summary>
        private Rectangle WindowBounds()
        {
            Rectangle aBounds = Window.ClientBounds;
            if (!IsMobile && SdlWindow.TryGetSize(Window.Handle, out int aWidth, out int aHeight))
            {
                return new Rectangle(0, 0, aWidth, aHeight);
            }
            return new Rectangle(0, 0, aBounds.Width, aBounds.Height);
        }

        /// <summary>The window size the picture was last fitted to (OnResize).</summary>
        private Point mFittedSize;

        /// <summary>
        /// Port: refits when the window's size or the backbuffer's no longer match what was fitted. Maximizing a
        /// desktop window could leave the picture fitted to a size the window then left without another
        /// ClientSizeChanged, or the backbuffer at the old size.
        /// </summary>
        private void CheckWindowSize()
        {
            Rectangle aBounds = WindowBounds();
            if (aBounds.Width <= 0 || aBounds.Height <= 0 || GlobalStaticVars.gSexyAppBase == null)
            {
                return;
            }
            PresentationParameters pp = base.GraphicsDevice.PresentationParameters;
            Point aBackBuffer = new Point(pp.BackBufferWidth, pp.BackBufferHeight);
            bool aMismatch = mFittedSize.X != aBounds.Width || mFittedSize.Y != aBounds.Height || aBackBuffer.X != aBounds.Width || aBackBuffer.Y != aBounds.Height;
            // Once per change only: a mismatch that stays (OnResize could not fix it) must not keep restarting the
            // layout's settling.
            if (aMismatch && (aBackBuffer != mCheckedBackBuffer || aBounds.Size != mCheckedBounds))
            {
                OnResize(null, null);
            }
            mCheckedBackBuffer = new Point(base.GraphicsDevice.PresentationParameters.BackBufferWidth, base.GraphicsDevice.PresentationParameters.BackBufferHeight);
            mCheckedBounds = WindowBounds().Size;
        }

        private Point mCheckedBackBuffer;

        private Point mCheckedBounds;

        /// <summary>
        /// The backbuffer must be exactly the window's (or surface's) size. Otherwise MonoGame letterboxes it
        /// through the viewport, which is lost whenever a render target is unset, so the picture shrank
        /// and later jumped (black bands at the top when the surface was smaller than the display). Port: the
        /// desktop too (a maximized window kept the old backbuffer size).
        /// </summary>
        private void MatchBackBufferToWindow(Rectangle theBounds)
        {
            if (theBounds.Width <= 0 || theBounds.Height <= 0)
            {
                return;
            }
            PresentationParameters pp = base.GraphicsDevice.PresentationParameters;
            if (pp.BackBufferWidth != theBounds.Width || pp.BackBufferHeight != theBounds.Height)
            {
                if (IsMobile)
                {
                    Main.graphics.PreferredBackBufferWidth = theBounds.Width;
                    Main.graphics.PreferredBackBufferHeight = theBounds.Height;
                    Main.graphics.GraphicsDeviceManager.ApplyChanges();
                }
                else
                {
                    // The desktop's GL backbuffer is the window itself: only MonoGame's record of its size (used
                    // for the viewport whenever a render target is unset) is put right; ApplyChanges would resize
                    // the window, un-maximizing it.
                    pp.BackBufferWidth = theBounds.Width;
                    pp.BackBufferHeight = theBounds.Height;
                }
            }
            base.GraphicsDevice.Viewport = new Viewport(0, 0, theBounds.Width, theBounds.Height);
        }

        private Point mWindowedSize;

        /// <summary>Port: whether the interface was turned to landscape when full screen was entered.</summary>
        private bool mWindowedLandscape;

        /// <summary>
        /// Switches between a window and borderless full screen at the desktop resolution (no mode change),
        /// restoring the window size on the way back.
        /// </summary>
        public void ToggleFullscreen()
        {
            // Port: on the desktop SDL's own borderless full screen, which brings the window back as it was
            // (size, place, maximized); MonoGame's switch below left a later maximize wrongly sized.
            if (!IsMobile && SdlWindow.TrySetFullscreen(Window.Handle, !SdlWindow.IsFullscreen(Window.Handle)))
            {
                OnResize(null, null);
                return;
            }
            GraphicsDeviceManager aManager = Main.graphics.GraphicsDeviceManager;
            if (!aManager.IsFullScreen)
            {
                mWindowedSize = new Point(WindowBounds().Width, WindowBounds().Height);
                mWindowedLandscape = Constants.Landscape;
                DisplayMode aMode = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
                aManager.HardwareModeSwitch = false;
                aManager.PreferredBackBufferWidth = aMode.Width;
                aManager.PreferredBackBufferHeight = aMode.Height;
                aManager.IsFullScreen = true;
            }
            else
            {
                aManager.IsFullScreen = false;
                if (mWindowedSize.X > 0 && mWindowedSize.Y > 0)
                {
                    // Turned while full screen: the window comes back turned too (a wide window is not turned).
                    if (mWindowedLandscape != Constants.Landscape)
                    {
                        mWindowedSize = new Point(mWindowedSize.Y, mWindowedSize.X);
                    }
                    aManager.PreferredBackBufferWidth = mWindowedSize.X;
                    aManager.PreferredBackBufferHeight = mWindowedSize.Y;
                }
            }
            aManager.ApplyChanges();
            OnResize(null, null);
        }

        /// <summary>
        /// Port addition, after the interface turned (SexyAppBase.InterfaceOrientationChanged): a desktop window
        /// swaps its width and height; full screen (and phones, whose surface the OS turns) refits the picture.
        /// </summary>
        public void InterfaceRotated()
        {
            GraphicsDeviceManager aManager = Main.graphics.GraphicsDeviceManager;
            Rectangle aBounds = WindowBounds();
            if (!IsMobile && !aManager.IsFullScreen && (aBounds.Width > aBounds.Height) != Constants.Landscape)
            {
                aManager.PreferredBackBufferWidth = aBounds.Height;
                aManager.PreferredBackBufferHeight = aBounds.Width;
                aManager.ApplyChanges();
            }
            OnResize(null, null);
        }

        private void graphics_DeviceCreated(object sender, EventArgs e)
        {
            base.GraphicsDevice.PresentationParameters.PresentationInterval = PresentInterval.Immediate;
        }

        private bool mExiting;

        private void ExitGame()
        {
            if (ExitHandler == null)
            {
                base.Exit();
                return;
            }
            if (mExiting)
            {
                return;
            }
            mExiting = true;
            // Save and shut down as the Exiting event would, then let the host close the app.
            GlobalStaticVars.gSexyAppBase?.AppExit();
            ExitHandler();
        }

        private void Main_Exiting(object sender, ExitingEventArgs e)
        {
            GlobalStaticVars.gSexyAppBase?.AppExit();
        }

        internal string FetchApplicationStoragePath()
        {
            return applicationStoragePath ?? AppContext.BaseDirectory;
        }

        protected override void Initialize()
        {
            base.Initialize();
        }

        protected override void LoadContent()
        {
            GraphicsState.Init();
            GlobalStaticVars.initialize(this);
            MatchBackBufferToWindow(WindowBounds());
            GlobalStaticVars.gSexyAppBase.mScreenScales.Init(WindowBounds().Width, WindowBounds().Height, Constants.ScreenWidth, Constants.ScreenHeight);
            Graphics.Resized();
            // The window may not have the shape asked for (the desktop clamps it to the monitor).
            mPendingLayout = Constants.RelayoutFor(WindowBounds().Width, WindowBounds().Height);
            GlobalStaticVars.gSexyAppBase.StartLoadingThread();
        }


        protected override void Update(GameTime gameTime)
        {
            if (!base.IsActive && !DebugScript.Active)
            {
                return;
            }
            if (DebugScript.ShotPending)
            {
                base.Update(gameTime);
                return;
            }
            if (!DebugScript.Update(GlobalStaticVars.gSexyAppBase))
            {
                ExitGame();
                return;
            }
            if (GlobalStaticVars.gSexyAppBase.WantsToExit)
            {
                ExitGame();
                return;
            }
            // A scripted run drives all input itself; the real mouse would otherwise click into it.
            if (!DebugScript.Active)
            {
                HandleInput(gameTime);
            }
            CheckWindowSize();
            FollowSurfaceOrientation(GlobalStaticVars.gSexyAppBase);
            UpdateLayout(GlobalStaticVars.gSexyAppBase);
            long aStart = System.Diagnostics.Stopwatch.GetTimestamp();
            GlobalStaticVars.gSexyAppBase.UpdateApp();
            mPerfUpdateTicks += System.Diagnostics.Stopwatch.GetTimestamp() - aStart;
            base.Update(gameTime);
        }

        /// <summary>Port: the layout the window or screen asked for last (Constants.RelayoutFor), not applied yet.</summary>
        private Point? mPendingLayout;

        /// <summary>Port: updates since the window last changed; the layout follows once it has settled.</summary>
        private int mPendingLayoutAge;

        /// <summary>Port: updates the window must stay unchanged before the layout follows it (a resize drag).</summary>
        private const int LAYOUT_SETTLE_UPDATES = 10;

        /// <summary>
        /// Port addition: after the window was resized (desktop) or the device turned (phones and tablets), and
        /// once that has settled, the game is laid out for the new shape (SexyAppBase.ApplyLayout), which may
        /// wait for a moving interface or an open dialog; until then the old layout is letterboxed in.
        /// </summary>
        private void UpdateLayout(SexyAppBase theApp)
        {
            if (mPendingLayout is not Point aSize || ++mPendingLayoutAge < LAYOUT_SETTLE_UPDATES)
            {
                return;
            }
            if (aSize == Constants.BackBufferSize)
            {
                mPendingLayout = null;
                return;
            }
            if (theApp.ApplyLayout(aSize))
            {
                mPendingLayout = null;
                FitToWindow();
            }
        }

        /// <summary>
        /// Port (phones and tablets): the interface turns with the surface where the game allows it now
        /// (ShouldAutorotateToInterfaceOrientation: the landscape bookshelf from the board), as on iOS; asked
        /// every update, so a turn that was not allowed yet (a dialog was up) follows once it is.
        /// </summary>
        private void FollowSurfaceOrientation(SexyAppBase theApp)
        {
            Rectangle aBounds = WindowBounds();
            if (!IsMobile || theApp == null || aBounds.Width <= 0 || aBounds.Height <= 0)
            {
                return;
            }
            bool aLandscape = aBounds.Width > aBounds.Height;
            if (aLandscape == Constants.Landscape)
            {
                return;
            }
            UI_ORIENTATION anOrientation = aLandscape ? UI_ORIENTATION.UI_ORIENTATION_LANDSCAPE_LEFT : UI_ORIENTATION.UI_ORIENTATION_PORTRAIT;
            if (theApp.ShouldAutorotateToInterfaceOrientation(anOrientation))
            {
                theApp.InterfaceOrientationChanged(anOrientation);
            }
        }

        /// <summary>
        /// Port: lays the game out for the window's size now, without waiting for the window to settle (the splash
        /// going: the window may have been resized while loading, when the layout cannot change). False when the game cannot take a new layout now (SexyAppBase.ApplyLayout).
        /// </summary>
        public bool RelayoutNow(SexyAppBase theApp)
        {
            Rectangle aBounds = WindowBounds();
            if (aBounds.Width <= 0 || aBounds.Height <= 0)
            {
                return true;
            }
            FitToWindow();
            mPendingLayout = Constants.RelayoutFor(aBounds.Width, aBounds.Height);
            return ApplyPendingLayoutNow(theApp);
        }

        /// <summary>
        /// Port: the game is laid out again for the window as it is (the UI layout option changed), on the next
        /// update rather than inside the click that asked for it; it may wait as a resize does (UpdateLayout).
        /// </summary>
        public void RequestRelayout()
        {
            Rectangle aBounds = WindowBounds();
            if (aBounds.Width <= 0 || aBounds.Height <= 0)
            {
                return;
            }
            mPendingLayout = Constants.RelayoutFor(aBounds.Width, aBounds.Height);
            mPendingLayoutAge = LAYOUT_SETTLE_UPDATES - 1;
        }

        /// <summary>Port: as RelayoutNow, with the layout the window asked for last.</summary>
        public bool ApplyPendingLayoutNow(SexyAppBase theApp)
        {
            if (mPendingLayout is not Point aSize)
            {
                return true;
            }
            if (aSize != Constants.BackBufferSize && !theApp.ApplyLayout(aSize))
            {
                return false;
            }
            mPendingLayout = null;
            FitToWindow();
            return true;
        }

        public static void SuppressNextDraw()
        {
            Main.wantToSuppressDraw = true;
        }

        public static void NeedToSetUpOrientationMatrix(UI_ORIENTATION orientation)
        {
            Main.orientationUsed = orientation;
            Main.newOrientation = true;
        }

        protected override void Draw(GameTime gameTime)
        {
            Main.newOrientation = false;
            if (Main.wantToSuppressDraw)
            {
                Main.wantToSuppressDraw = false;
                return;
            }
            lock (ResourceManager.DrawLocker)
            {
                base.GraphicsDevice.Clear(GlobalStaticVars.gSexyAppBase.mClearColor);
                long aStart = System.Diagnostics.Stopwatch.GetTimestamp();
                GlobalStaticVars.gSexyAppBase.DrawGame(gameTime);
                base.Draw(gameTime);
                if (gPerf)
                {
                    PerfFrame(System.Diagnostics.Stopwatch.GetTimestamp() - aStart);
                }
                DebugScript.AfterDraw(base.GraphicsDevice);
            }
        }

        public void HandleInput(GameTime gameTime)
        {
            SexyAppBase app = GlobalStaticVars.gSexyAppBase;
            ScreenScales s = app.mScreenScales;
            // Mobile input is touch only (below); polling the mouse there too would double taps.
            MouseState msstate = IsMobile ? previousMouseState : Mouse.GetState();
            CGPoint mouse = s.InvMapTouch(new CGPoint(msstate.Position.X, msstate.Position.Y));
            int mx = (int)mouse.x;
            int my = (int)mouse.y;
            if (previousMouseState.Position != msstate.Position)
            {
                app.PointerInputUsed();
                app.mWidgetManager.MouseMove(mx, my);
            }
            if (msstate.LeftButton == ButtonState.Pressed && previousMouseState.LeftButton == ButtonState.Released)
            {
                // Click count 3 means the middle button to WidgetManager, so mouse clicks cycle 1,2,1,2...
                mMouseClickCount = CountTap(mouse, gameTime.TotalGameTime.TotalSeconds);
                if (mMouseClickCount > 2)
                {
                    mMouseClickCount = mTapCount = 1;
                }
                app.PointerInputUsed();
                app.mWidgetManager.MouseDown(mx, my, mMouseClickCount);
            }
            else if (msstate.LeftButton == ButtonState.Released && previousMouseState.LeftButton == ButtonState.Pressed)
            {
                app.mWidgetManager.MouseUp(mx, my, mMouseClickCount);
            }
            if (msstate.RightButton == ButtonState.Pressed && previousMouseState.RightButton == ButtonState.Released)
            {
                app.mWidgetManager.MouseDown(mx, my, -1);
            }
            else if (msstate.RightButton == ButtonState.Released && previousMouseState.RightButton == ButtonState.Pressed)
            {
                app.mWidgetManager.MouseUp(mx, my, -1);
            }
            // Click count 3 is the middle button to WidgetManager (Widget.MouseDown passes it as button 2).
            if (msstate.MiddleButton == ButtonState.Pressed && previousMouseState.MiddleButton == ButtonState.Released)
            {
                app.mWidgetManager.MouseDown(mx, my, 3);
            }
            else if (msstate.MiddleButton == ButtonState.Released && previousMouseState.MiddleButton == ButtonState.Pressed)
            {
                app.mWidgetManager.MouseUp(mx, my, 3);
            }
            if (msstate.ScrollWheelValue != previousMouseState.ScrollWheelValue)
            {
                app.mWidgetManager.MouseWheel(msstate.ScrollWheelValue - previousMouseState.ScrollWheelValue);
            }
            GamePadState pad = GamePad.GetState(PlayerIndex.One);
            KeyboardState keys = Keyboard.GetState();
            // Port addition (controller): Start works as Back too.
            if ((pad.Buttons.Back == ButtonState.Pressed && previousGamepadState.Buttons.Back == ButtonState.Released) ||
                (pad.Buttons.Start == ButtonState.Pressed && previousGamepadState.Buttons.Start == ButtonState.Released) ||
                (keys.IsKeyDown(Keys.Escape) && previousKeyboardState.IsKeyUp(Keys.Escape)) || BackRequested)
            {
                BackRequested = false;
                app.BackButtonPress();
            }
            // Port addition (controller): the game's turn with the pad (the board's selector).
            app.GamePadUpdate(pad, previousGamepadState);
            while (AccelerometerSamples.TryDequeue(out double[] aSample))
            {
                app.AccelerometerDidAccelerate(aSample[0], aSample[1], aSample[2], aSample[3]);
            }
            bool began = false;
            foreach (TouchLocation touchLocation in TouchPanel.GetState())
            {
                _Touch touch = default(_Touch);
                touch.location.mX = s.InvMapTouchX(touchLocation.Position.X);
                touch.location.mY = s.InvMapTouchY(touchLocation.Position.Y);
                if (touchLocation.TryGetPreviousLocation(out TouchLocation prev))
                {
                    touch.previousLocation = s.InvMapTouch(new CGPoint(prev.Position.X, prev.Position.Y));
                }
                else
                {
                    touch.previousLocation = touch.location;
                }
                touch.timestamp = gameTime.TotalGameTime.TotalSeconds;
                if (touchLocation.State == TouchLocationState.Pressed && !began)
                {
                    mTouchTapCount = CountTap(touch.location, touch.timestamp);
                }
                touch.tapCount = mTouchTapCount;
                if (touchLocation.State == TouchLocationState.Pressed && !began)
                {
                    app.PointerInputUsed();
                    app.TouchBegan(touch);
                    began = true;
                }
                else if (touchLocation.State == TouchLocationState.Moved)
                {
                    app.TouchMoved(touch);
                }
                else if (touchLocation.State == TouchLocationState.Released)
                {
                    app.TouchEnded(touch);
                }
                else if (touchLocation.State == TouchLocationState.Invalid)
                {
                    app.TouchesCanceled();
                }
            }
            // Port addition: Alt+Enter toggles full screen on desktops; the Enter press isn't passed on.
            bool aAltEnter = !IsMobile && (keys.IsKeyDown(Keys.LeftAlt) || keys.IsKeyDown(Keys.RightAlt)) &&
                keys.IsKeyDown(Keys.Enter) && previousKeyboardState.IsKeyUp(Keys.Enter);
            if (aAltEnter)
            {
                ToggleFullscreen();
            }
            foreach (Keys k in keys.GetPressedKeys())
            {
                if (previousKeyboardState.IsKeyUp(k) && !(aAltEnter && k == Keys.Enter))
                {
                    app.mWidgetManager.KeyDown((KeyCode)k);
                }
            }
            foreach (Keys k in previousKeyboardState.GetPressedKeys())
            {
                if (keys.IsKeyUp(k))
                {
                    app.mWidgetManager.KeyUp((KeyCode)k);
                }
            }
            previousGamepadState = pad;
            previousMouseState = msstate;
            previousKeyboardState = keys;
        }

        private int mTouchTapCount = 1;

        // SEXY_PERF=1 (port debug aid): logs average update/draw CPU time and GPU draw calls once a second.
        private static readonly bool gPerf = Environment.GetEnvironmentVariable("SEXY_PERF") == "1";

        private long mPerfUpdateTicks;

        private long mPerfDrawTicks;

        private long mPerfDrawCalls;

        private long mPerfPrimitives;

        private long mPerfMaxDrawTicks;

        private int mPerfFrames;

        private int mPerfGCs;

        private void PerfFrame(long theDrawTicks)
        {
            GraphicsMetrics aMetrics = base.GraphicsDevice.Metrics;
            mPerfDrawTicks += theDrawTicks;
            mPerfMaxDrawTicks = Math.Max(mPerfMaxDrawTicks, theDrawTicks);
            mPerfDrawCalls += aMetrics.DrawCount;
            mPerfPrimitives += aMetrics.PrimitiveCount;
            if (++mPerfFrames < 60)
            {
                return;
            }
            double aMs = 1000.0 / System.Diagnostics.Stopwatch.Frequency / mPerfFrames;
            int aGCs = GC.CollectionCount(0);
            Console.WriteLine($"[perf] update {mPerfUpdateTicks * aMs:F2}ms draw {mPerfDrawTicks * aMs:F2}ms (max {mPerfMaxDrawTicks * aMs * mPerfFrames:F2}) " +
                $"calls {mPerfDrawCalls / mPerfFrames} prims {mPerfPrimitives / mPerfFrames} gc0 {aGCs - mPerfGCs} alloc {GC.GetTotalAllocatedBytes() / 1024}K f{DebugScript.Frame}");
            mPerfGCs = aGCs;
            mPerfUpdateTicks = mPerfDrawTicks = mPerfDrawCalls = mPerfPrimitives = mPerfMaxDrawTicks = 0;
            mPerfFrames = 0;
        }

        private int CountTap(CGPoint thePos, double theTime)
        {
            float dx = thePos.X - mLastTapPos.X;
            float dy = thePos.Y - mLastTapPos.Y;
            if (theTime - mLastTapTime <= MULTI_TAP_SECONDS && dx * dx + dy * dy <= MULTI_TAP_DISTANCE * MULTI_TAP_DISTANCE)
            {
                mTapCount++;
            }
            else
            {
                mTapCount = 1;
            }
            mLastTapTime = theTime;
            mLastTapPos = thePos;
            return mTapCount;
        }

        protected override void OnActivated(object sender, EventArgs args)
        {
            SexyAppBase app = GlobalStaticVars.gSexyAppBase;
            // Scripted runs ignore real focus changes (their blur/focus steps simulate them).
            if (app != null && !DebugScript.Active)
            {
                app.GotFocus();
                if (!app.mMusicInterface.isStopped)
                {
                    app.mMusicInterface.ResumeMusic();
                }
            }
            base.OnActivated(sender, args);
        }

        protected override void OnDeactivated(object sender, EventArgs args)
        {
            SexyAppBase app = GlobalStaticVars.gSexyAppBase;
            // Scripted runs ignore real focus changes (their blur/focus steps simulate them).
            if (app != null && !DebugScript.Active)
            {
                app.LostFocus();
                if (!app.mMusicInterface.isStopped)
                {
                    app.mMusicInterface.PauseMusic();
                }
                app.AppEnteredBackground();
            }
            base.OnDeactivated(sender, args);
        }
    }
}
