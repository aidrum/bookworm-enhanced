using System;

namespace Sexy
{
    /// <summary>
    /// Sexy::BookWormApp, the application (vtable 0x10f378). Field comments give the original offsets.
    /// Ported so far: start-up (Start, 0x133ec), the loading thread (vtable slot 25, 0x13cc8) up to the
    /// resources, music and atlas, the interface state machine (0x11f54) and the end of loading (slot 76,
    /// 0x15880). The screens the loading thread builds are not ported yet (see references/NEXT_STEPS.md).
    /// </summary>
    public class BookWormApp : SexyAppBase
    {
        /// <summary>Number of screens the app moves through its interface states (app + 0xd60).</summary>
        public const int SCREEN_COUNT = 0x13;

        public const int SCREEN_LOADING = 0;

        public const int SCREEN_DEMO_SPLASH = 1;

        public const int SCREEN_MENU_CURTAIN = 2;

        public const int SCREEN_MENU_TABS = 7;

        public const int SCREEN_GAME_SELECTOR = 8;

        public const int SCREEN_STATS = 9;

        public const int SCREEN_BOOKSHELF = 10;

        public const int SCREEN_WORD_COLLECTION = 11;

        public const int SCREEN_WORD_HISTORY = 12;

        public const int SCREEN_OPTIONS = 13;

        public const int SCREEN_MORE_GAMES = 14;

        public const int SCREEN_HELP = 15;

        public const int SCREEN_BOARD = 16;

        public const int SCREEN_LEVEL_UP = 17;

        public const int SCREEN_LEX = 18;

        /// <summary>
        /// Where each screen goes in each interface state (0xccf30, [state][screen]): 0 away, 1 or 2 shown
        /// (the screen decides what the number means), -1 left as it is.
        /// </summary>
        private static readonly int[,] INTERFACE_TARGETS = new int[16, SCREEN_COUNT]
        {
            { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            { 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            { 0, 0, 1, 1, 0, 0, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
            { 0, 0, 1, 0, 1, 0, 0, 1, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 2 },
            { 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0 },
            { 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, -1, 1, 0, 0, 0, 0, 0, 0, 0 },
            { 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, -1, 0, 1, 0, 0, 0, 0, 0, 0 },
            { 0, 0, 1, 0, 1, 0, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 2 },
            { 0, 0, 1, 0, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 2 },
            { 0, 0, 1, 0, 0, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 1 },
            { 0, 0, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 2 },
            { 0, 0, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, 1, 2 },
            { 0, 0, 1, 0, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, -1, 0, 2 },
            { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0 },
            { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, 1, 0, 0, 0, 0, 0, 0, 0 },
            { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -1, 0, 1, 0, 0, 0, 0, 0, 0 }
        };

        /// <summary>The parameter each interface state passes its screens (0xcd3f0).</summary>
        private static readonly int[] INTERFACE_PARAMS = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1 };

        /// <summary>The song each interface state plays, for the states in INTERFACE_MUSIC_MASK (0xcd43c).</summary>
        private static readonly int[] INTERFACE_MUSIC = { 8, 8, 1, 8, 1, 1, 1, 1, 1, 1, 8, 8, 8, 8, 8, 8 };

        /// <summary>States that change the music (0x3f4: 2 and 4-9).</summary>
        private const int INTERFACE_MUSIC_MASK = 0x3f4;

        /// <summary>Each screen's z-order in the widget manager (0xcd47c, set at Widget + 0x44).</summary>
        private static readonly int[] SCREEN_PRIORITIES = { 0, 0, 10, 20, 30, 30, 51, 90, 40, 60, 60, 80, 80, 60, 60, 40, 70, 80, 50 };

        public static BookWormApp gApp => (BookWormApp)GlobalStaticVars.gSexyAppBase;

        public int mInterfaceState; // 0xd5c

        public InterfaceWidget[] mScreens = new InterfaceWidget[SCREEN_COUNT]; // 0xd60

        /// <summary>While non-zero, screens are not told their move is done at once (0xdac, FUN_00012000).</summary>
        public int mInterfaceLock; // 0xdac

        /// <summary>Set when loading was cut short; the loading screen then stops waiting (0x11d0).</summary>
        public bool mLoadingDone; // 0x11d0

        /// <summary>Set by the loading thread on a resource error (0xcc).</summary>
        public bool mLoadingFailed; // 0xcc

        public ScreenPos mScreenPos = new ScreenPos(); // 0xdd4

        public AtlasResourceBase mAtlasResources;

        public Definitionator mDefinitionator; // 0xdc4

        public Collectionator mCollectionator; // 0xdc8

        public PersonalBests mPersonalBests; // 0xdcc

        public Dictionary mDictionary = new Dictionary(); // 0x11dc

        /// <summary>Words that can be the bonus word (dict/bonus_words.txt, only those in the dictionary) (0x11f0).</summary>
        public System.Collections.Generic.List<string> mBonusWords = new System.Collections.Generic.List<string>();

        public BookWormApp(Main theGame) : base(theGame)
        {
            mProdName = "Bookworm";
            mTitle = "BookWorm Deluxe " + mVersion;
            mInterfaceState = 2;
        }

        /// <summary>
        /// Port: the texture set's key (the original's resolution key: 320, 640, 768 or 1536), picked at launch
        /// (Constants.TextureSide). The layout may be narrower (the iPhone design with iPad textures).
        /// </summary>
        public int GetTextureSide()
        {
            return Constants.TextureSide;
        }

        /// <summary>The screen's short side (the layout's width).</summary>
        public int GetScreenShortSide()
        {
            return Math.Min(mWidth, mHeight);
        }

        /// <summary>The screen's long side (1136 on the iPhone 5).</summary>
        public int GetScreenLongSide()
        {
            return Math.Max(mWidth, mHeight);
        }

        /// <summary>
        /// 0x133ec: parses the manifest for the screen (by its short side), lays out ScreenPos and shows the
        /// loading screen. The original also starts PopCap's metrics and sharing services and reads the
        /// demo limits (MaxExecutions, MaxPlays, MaxTime); those are not part of this port.
        /// </summary>
        public override void Start()
        {
            int aShortSide = GetTextureSide();
            string aManifest;
            switch (aShortSide)
            {
            case 0x280:
                aManifest = "resourcesR.xml";
                break;
            case 0x300:
                aManifest = "resources_iPad.xml";
                break;
            case 0x600:
                aManifest = "resources_iPadR.xml";
                break;
            default:
                aManifest = "resources.xml";
                break;
            }
            LoadSettings();
            mResourceManager.ParseResourcesFile(aManifest);
            // The original also creates two helpers here (0xdd0, FUN_000232cc), not ported yet.
            mPersonalBests = new PersonalBests(this);
            mPersonalBests.Load();
            mCollectionator = new Collectionator(this);
            mScreenPos.Init(aShortSide, mWidth, mHeight);
            if (mResourceManager.HadError())
            {
                Console.Error.WriteLine(mResourceManager.GetErrorText());
                mLoadingFailed = true;
                base.Start();
                return;
            }
            mInterfaceState = 0;
            LoadingScreen aLoadingScreen = new LoadingScreen(this);
            mScreens[SCREEN_LOADING] = aLoadingScreen;
            aLoadingScreen.mZOrder = 0;
            aLoadingScreen.Resize(0, 0, mWidth, mHeight);
            mWidgetManager.AddWidget(aLoadingScreen);
            // The original's SexyAppBase::Start comes first, but this framework's starts the loading thread,
            // which must not run before the manifest is parsed.
            base.Start();
        }

        /// <summary>
        /// Vtable slot 25 (0x13cc8): loads the "Init" group, the eight songs and the atlas, then (not ported
        /// yet) the dictionary, the definitions and the screens.
        /// </summary>
        public override void LoadingThreadProc()
        {
            mNumLoadingThreadTasks = mResourceManager.GetNumResources("Init") + 11;
            mNumLoadingThreadTasks += mCollectionator.CountBooks("books");
            for (int i = 0; i < 8; i++)
            {
                // Song i is Bookworm<i+1>: the menus play song 1 (Bookworm2.caf). Song 8, which some interface
                // states ask for, doesn't exist, so those leave the music alone.
                mMusicInterface.LoadMusic(i, string.Format("music/Bookworm{0}.caf", i + 1));
                mCompletedLoadingThreadTasks++;
            }
            mResourceManager.StartLoadResources("Init");
            while (mResourceManager.LoadNextResource() && !mShutdown)
            {
                mCompletedLoadingThreadTasks++;
            }
            if (mShutdown)
            {
                return;
            }
            if (mResourceManager.HadError() || !Res.ExtractInitResources(mResourceManager, GetTextureSide()))
            {
                Console.Error.WriteLine(mResourceManager.GetErrorText());
                mLoadingFailed = true;
                return;
            }
            mResourceManager.SetGroupAsLoaded("Init");
            mAtlasResources = AtlasResourceBase.Create(GetTextureSide());
            mAtlasResources.UnpackImages();
            if (!mDictionary.LoadFile("dict/oed_encoded.txt"))
            {
                mLoadingFailed = true;
                return;
            }
            mCompletedLoadingThreadTasks++;
            LoadWordList("dict/bonus_words.txt", mBonusWords);
            mCompletedLoadingThreadTasks++;
            int aBonusCount = mBonusWords.Count;
            mBonusWords.RemoveAll(aWord => !mDictionary.IsWord(aWord));
            Console.Error.WriteLine(string.Format("Dictionary: {0} words, {1} of {2} bonus words kept", mDictionary.mWordCount, mBonusWords.Count, aBonusCount));
            mDefinitionator = new Definitionator(this);
            mDefinitionator.Load("dict/oed_definitions.txt");
            do
            {
                mCompletedLoadingThreadTasks++;
            }
            while (mCollectionator.LoadNextBook());
            mCollectionator.Load();
            mScreens[SCREEN_MENU_CURTAIN] = new MenuCurtainWidget(this);
            // The logo (ScreenPos + 0x00) and the curtain swags (at 0,1, a header static) slide down; the
            // pedestal (ScreenPos + 0x78) slides up.
            mScreens[3] = new SlideInImage(this, AtlasResourceBase.IMAGE_GROUP11_00, new TPoint(mScreenPos.At(0), mScreenPos.At(4)), SlideInWidget.SLIDE_FROM_TOP);
            mScreens[4] = new SlideInImage(this, AtlasResourceBase.IMAGE_GROUP1_01, new TPoint(0, 1), SlideInWidget.SLIDE_FROM_TOP);
            mScreens[5] = new SlideInImage(this, AtlasResourceBase.IMAGE_GROUP1_02, new TPoint(0, 1), SlideInWidget.SLIDE_FROM_TOP);
            mScreens[6] = new SlideInImage(this, AtlasResourceBase.IMAGE_GROUP11_14, new TPoint(mScreenPos.At(0x78), mScreenPos.At(0x7c)), SlideInWidget.SLIDE_FROM_BOTTOM) { mStretchEdges = false };
            mScreens[SCREEN_MENU_TABS] = new MenuTabWidget(this);
            mScreens[SCREEN_GAME_SELECTOR] = new BWGameSelector(this);
            mScreens[SCREEN_STATS] = new FameWidget(this);
            mScreens[SCREEN_BOOKSHELF] = new BookshelfWidget(this);
            mScreens[SCREEN_WORD_COLLECTION] = new WordCollectionWidget(this);
            mScreens[SCREEN_WORD_HISTORY] = new WordHistoryWidget(this);
            mScreens[SCREEN_OPTIONS] = new OptionsDialog(this);
            mScreens[SCREEN_MORE_GAMES] = new MoreGamesWidget(this);
            mScreens[SCREEN_HELP] = new HelpWidget(this);
            mScreens[SCREEN_LEVEL_UP] = new LevelUpWidget(this);
            mLex = new Lex(this);
            mScreens[SCREEN_LEX] = mLex;
            // (The original makes the balloon after setting the screens' z-orders, below.)
            // Not ported (0x13cc8): DemoSplashScreen (0xd64, only with SPLASH_MESSAGE_TEXT).
            for (int i = 0; i < SCREEN_COUNT; i++)
            {
                if (mScreens[i] != null && i != SCREEN_LOADING)
                {
                    mScreens[i].mZOrder = SCREEN_PRIORITIES[i];
                }
            }
            mWordBalloon = new WordBalloon(this);
            mWordBalloon.mZOrder = 0x55;
            mWidgetManager.AddWidget(mWordBalloon);
        }

        /// <summary>0x1200c: a screen's move ended; when none is left moving, all of them are told the state is reached.</summary>
        public void ReleaseInterfaceLock()
        {
            if (--mInterfaceLock != 0)
            {
                return;
            }
            for (int i = 0; i < SCREEN_COUNT; i++)
            {
                mScreens[i]?.InterfaceStateDone();
            }
        }

        public TouchGlowWidget mTouchGlow; // 0xdb4

        /// <summary>The app version (0xd8); the title is "BookWorm Deluxe " + version (0x28, FUN_0001160c).</summary>
        public string mVersion = "1.0.5";

        /// <summary>Options (FUN_0001160c sets all four on): 0x11fd (unknown), hints, definitions, confirm scramble.</summary>
        public bool mOption11fd = true; // 0x11fd

        public bool mShowHints = true; // 0x11fe

        public bool mShowDefinitions = true; // 0x11ff

        public bool mConfirmScramble = true; // 0x1200

        /// <summary>
        /// Hints already given (0x1201-0x1204, cleared together when hints are switched on again): burning tiles,
        /// fire alert, bonus tile, scramble. Then the "really stuck" hint (0x1205, 0x1208).
        /// </summary>
        public bool[] mHintsSeen = new bool[4];

        public const int HINT_BURNING = 0;

        public const int HINT_FIRE_ALERT = 1;

        public const int HINT_BONUS_TILE = 2;

        public const int HINT_SCRAMBLE = 3;

        public bool mHintTutorialShown; // 0x1205

        public int mHintTutorialStep2; // 0x1208

        /// <summary>
        /// Vtable slot 26 (0x12e90) in the options: saves the settings. The original saves its data file
        /// (app + 0xdd0, not ported yet); the port keeps the settings in the registry.
        /// </summary>
        public void SaveSettings()
        {
            RegistryWriteBoolean("ShowHints", mShowHints);
            RegistryWriteBoolean("ShowDefinitions", mShowDefinitions);
            RegistryWriteBoolean("ConfirmScramble", mConfirmScramble);
            RegistryWriteInteger("SfxVolume", (int)(GetSfxVolume() * 100));
            RegistryWriteInteger("MusicVolume", (int)(GetMusicVolume() * 100));
        }

        /// <summary>Port: the settings SaveSettings kept.</summary>
        public void LoadSettings()
        {
            RegistryReadBoolean("ShowHints", ref mShowHints);
            RegistryReadBoolean("ShowDefinitions", ref mShowDefinitions);
            RegistryReadBoolean("ConfirmScramble", ref mConfirmScramble);
            int aVolume = 0;
            if (RegistryReadInteger("SfxVolume", ref aVolume))
            {
                SetSfxVolume(aVolume / 100.0);
            }
            if (RegistryReadInteger("MusicVolume", ref aVolume))
            {
                SetMusicVolume(aVolume / 100.0);
            }
        }

        public Board mBoard; // 0xdb8

        /// <summary>Rewarded words in a row earn better tiles (0x1228; set in the constructor, never cleared).</summary>
        public bool mRewardStreaks = true;

        public Lex mLex; // 0xdbc

        public WordBalloon mWordBalloon; // 0xdc0

        public const int GAME_CLASSIC = 0;

        public const int GAME_ARCADE = 1;

        /// <summary>The mode being played (0x11d4).</summary>
        public int mGameMode;

        /// <summary>Lex has said "Welcome to Bookworm" this run (0x11fc).</summary>
        public bool mWelcomePlayed;

        /// <summary>FUN_00015b70's path: the mode's saved game in the documents folder.</summary>
        public string GetSavedGamePath(int theMode)
        {
            return System.IO.Path.Combine(applicationStoragePath ?? AppContext.BaseDirectory, theMode == GAME_ARCADE ? "action.dat" : "classic.dat");
        }

        /// <summary>FUN_00015b70.</summary>
        public bool HasSavedGame(int theMode)
        {
            return (theMode == GAME_CLASSIC || theMode == GAME_ARCADE) && System.IO.File.Exists(GetSavedGamePath(theMode));
        }

        /// <summary>FUN_00015d58: true when it is gone (or never existed).</summary>
        public bool DeleteSavedGame(int theMode)
        {
            if (theMode != GAME_CLASSIC && theMode != GAME_ARCADE)
            {
                return false;
            }
            try
            {
                System.IO.File.Delete(GetSavedGamePath(theMode));
                return true;
            }
            catch (System.IO.IOException)
            {
                return false;
            }
        }

        /// <summary>
        /// FUN_00015f50: starts a game of a mode: a new Board (z-order 0x46, screen 16) that loads the saved game
        /// if there is one, the mode's music (Classic song 0, Arcade song 2), then interface state 10.
        /// </summary>
        public void StartGame(int theMode)
        {
            mGameMode = theMode;
            if (mBoard != null)
            {
                mBoard.mParent?.RemoveWidget(mBoard);
                mBoard.Dispose();
            }
            mBoard = new Board(this);
            mBoard.mZOrder = 0x46;
            mScreens[SCREEN_BOARD] = mBoard;
            if (HasSavedGame(theMode))
            {
                mBoard.LoadGame(theMode);
            }
            if (theMode == GAME_CLASSIC)
            {
                mMusicInterface.PlayMusic(0, 0, true);
            }
            else if (theMode == GAME_ARCADE)
            {
                mMusicInterface.PlayMusic(2, 0, true);
            }
            GotoInterfaceState(10);
        }

        /// <summary>0x12040: glows around a touched button (the glow is made on first use).</summary>
        public void ShowTouchGlow(TRect theRect)
        {
            if (mTouchGlow == null)
            {
                mTouchGlow = new TouchGlowWidget(this);
                mTouchGlow.mZOrder = 0x6e;
                mWidgetManager.AddWidget(mTouchGlow);
            }
            mTouchGlow.SetGlowRect(theRect);
            mTouchGlow.Appear();
        }

        /// <summary>0x12130.</summary>
        public void HideTouchGlow()
        {
            mTouchGlow?.Disappear();
        }

        /// <summary>
        /// Vtable slot 53 (0x13158), when a dialog opens: pauses the board (FUN_00007184(board, 2, 1)). The
        /// original also clears an iOS idle-timer flag (app + 0xdb0), not needed here.
        /// </summary>
        public void DialogOpened()
        {
            mOpenDialogs++;
            mBoard?.SetPaused(2, true);
        }

        /// <summary>Port: Dialogoids open now (ApplyLayout waits for them to close).</summary>
        public int mOpenDialogs;

        /// <summary>
        /// Port addition (dynamic aspect): the window was resized or the device turned. Once nothing is moving,
        /// no dialog is open and everything is loaded, ScreenPos is laid out for the new size (its design may
        /// change between iPad and iPhone too: Constants.LayoutFor) and the screens
        /// that are up are placed again (InterfaceWidget.Relayout); a balloon Lex was saying goes. Until then
        /// Main keeps the old layout letterboxed in the window.
        /// </summary>
        public override bool ApplyLayout(Microsoft.Xna.Framework.Point theSize)
        {
            if (!mLoadingDone || mInterfaceLock != 0 || mOpenDialogs > 0)
            {
                return false;
            }
            base.ApplyLayout(theSize);
            // The tiles keep their places on the board; its grid's origin moves with the design (0x178 / 0x17c).
            int anOldGridX = mScreenPos.At(0x178);
            int anOldGridY = mScreenPos.At(0x17c);
            mScreenPos.Init(GetTextureSide(), mWidth, mHeight);
            mBoard?.MovePieces(mScreenPos.At(0x178) - anOldGridX, mScreenPos.At(0x17c) - anOldGridY);
            // The logo and the pedestal take their places in the constructor (LoadingThreadProc).
            if (mScreens[3] is SlideInImage aLogo)
            {
                aLogo.mPos = new TPoint(mScreenPos.At(0), mScreenPos.At(4));
            }
            if (mScreens[6] is SlideInImage aPedestal)
            {
                aPedestal.mPos = new TPoint(mScreenPos.At(0x78), mScreenPos.At(0x7c));
            }
            for (int i = 0; i < SCREEN_COUNT; i++)
            {
                mScreens[i]?.Relayout();
            }
            mWordBalloon?.HideNow();
            return true;
        }

        /// <summary>Vtable slot 54 (0x13184): resumes the board (FUN_00007184(board, 2, 0)).</summary>
        public void DialogClosed()
        {
            mOpenDialogs = Math.Max(0, mOpenDialogs - 1);
            mBoard?.SetPaused(2, false);
        }

        /// <summary>
        /// Vtable slot 30 (0x12188): unless the interface is moving, portrait is always allowed; landscape only on
        /// the landscape bookshelf (state 13) or on the board (state 10) while it is enabled and not paused.
        /// </summary>
        public override bool ShouldAutorotateToInterfaceOrientation(UI_ORIENTATION theOrientation)
        {
            if (mInterfaceLock != 0)
            {
                return false;
            }
            if (theOrientation == UI_ORIENTATION.UI_ORIENTATION_PORTRAIT || theOrientation == UI_ORIENTATION.UI_ORIENTATION_PORTRAIT_UPSIDE_DOWN)
            {
                return true;
            }
            if (!OrientationIsLandscape(theOrientation))
            {
                return false;
            }
            if (mInterfaceState == 13)
            {
                return true;
            }
            return mInterfaceState == 10 && mBoard != null && !mBoard.IsPaused() && !mBoard.mDisabled;
        }

        /// <summary>
        /// Vtable slot 31 (0x121d8): the orientations allowed, as an iOS mask (1 &lt;&lt; UI_ORIENTATION): portrait
        /// ones only (6) while a board hint/question is up or away from the board and the landscape bookshelf;
        /// there every orientation (the framework's default, 0x1e).
        /// </summary>
        public override int GetSupportedOrientations()
        {
            if (mBoard != null && mBoard.mHintShowing)
            {
                return 6;
            }
            return mInterfaceState == 10 || mInterfaceState == 13 ? 0x1e : 6;
        }

        /// <summary>
        /// Vtable slot 13 (0x12204): turning to landscape shows the landscape page last seen (states 13 bookshelf,
        /// 14 collection, 15 word history, by <see cref="Collectionator.mLandscapeView"/>); turning back to
        /// portrait remembers it and, coming from landscape, returns to the board (state 10).
        /// </summary>
        public override void InterfaceOrientationChanged(UI_ORIENTATION toOrientation)
        {
            if (OrientationIsLandscape(toOrientation))
            {
                GotoInterfaceState(13 + mCollectionator.mLandscapeView);
            }
            else if (toOrientation == UI_ORIENTATION.UI_ORIENTATION_PORTRAIT || toOrientation == UI_ORIENTATION.UI_ORIENTATION_PORTRAIT_UPSIDE_DOWN)
            {
                if (mInterfaceState >= 13 && mInterfaceState <= 15)
                {
                    mCollectionator.mLandscapeView = mInterfaceState - 13;
                }
                if (OrientationIsLandscape(mInterfaceOrientation))
                {
                    GotoInterfaceState(10);
                }
            }
            base.InterfaceOrientationChanged(toOrientation);
        }

        /// <summary>The shake detector's low-pass filter (0x135c, factor 0.9 at 0x1368).</summary>
        private float[] mAccelLow = new float[3];

        private const float ACCEL_FILTER = 0.9f;

        /// <summary>The last jolt's direction and strength (0x136c-0x1378), its time (0x137c), the time of the jolt
        /// before (0x1390), back-and-forth jolts counted (0x1394), no shakes until (0x1398).</summary>
        private float[] mShakeDir = new float[3];

        private float mShakeStrength;

        private float mShakeTime;

        private float mShakeReverseTime;

        private int mShakeCount;

        private float mShakeQuietUntil = float.MinValue;

        /// <summary>Vtable slot 34 (0x11ec4): going to the background saves the game in progress.</summary>
        public override void AppEnteredBackground()
        {
            if (mBoard != null && mBoard.CanSaveGame())
            {
                mBoard.SaveGame(mGameMode);
            }
        }

        /// <summary>Vtable slot 58 (0x12708): quitting saves the game in progress.</summary>
        public override void Shutdown()
        {
            if (!mShutdown && mBoard != null && mBoard.CanSaveGame())
            {
                mBoard.SaveGame(mGameMode);
            }
            base.Shutdown();
        }

        /// <summary>
        /// Vtable slot 33 (0x12424): shaking the device scrambles. The acceleration less its slow drift is a jolt
        /// when its squared size passes 0.32; jolts along the same line keep the strongest; a jolt the other way
        /// (dot below -0.6) within a quarter second of the last counts, rattling (SOUND_SHAKE at half volume a
        /// count); the fourth asks the board to scramble and shakes are ignored for 2 seconds. Ignored while there
        /// is no board, it is disabled, or a modal dialog is up. (The port's heads have to feed this.)
        /// </summary>
        public override void AccelerometerDidAccelerate(double timestamp, double ax, double ay, double az)
        {
            float[] anAccel = { (float)ax, (float)ay, (float)az };
            float[] aJolt = new float[3];
            float aSize2 = 0;
            for (int i = 0; i < 3; i++)
            {
                mAccelLow[i] = anAccel[i] * (1f - ACCEL_FILTER) + mAccelLow[i] * ACCEL_FILTER;
                aJolt[i] = anAccel[i] - mAccelLow[i];
                aSize2 += aJolt[i] * aJolt[i];
            }
            if (aSize2 <= 0.32f)
            {
                return;
            }
            bool aBusy = mBoard == null || mBoard.mDisabled || mWidgetManager.mBaseModalWidget != null;
            if (!(mShakeQuietUntil < timestamp) || aBusy)
            {
                return;
            }
            float aSize = (float)System.Math.Sqrt(aSize2);
            float aDot = 0;
            for (int i = 0; i < 3; i++)
            {
                aJolt[i] /= aSize;
                aDot += aJolt[i] * mShakeDir[i];
            }
            if (!(aDot < -0.6f))
            {
                if (aSize2 <= mShakeStrength)
                {
                    return;
                }
            }
            else if (mShakeTime - mShakeReverseTime > 0.25f || timestamp - mShakeTime > 0.25)
            {
                mShakeCount = 0;
                PlayShake();
                mShakeReverseTime = mShakeTime;
            }
            else if (++mShakeCount < 4)
            {
                PlayShake();
                mShakeReverseTime = mShakeTime;
            }
            else
            {
                mBoard.RequestScramble();
                mShakeCount = 0;
                mShakeQuietUntil = (float)(timestamp + 2.0);
                mShakeReverseTime = mShakeTime;
            }
            mShakeDir = aJolt;
            mShakeStrength = aSize2;
            mShakeTime = (float)timestamp;
        }

        /// <summary>SOUND_SHAKE, louder with each jolt counted.</summary>
        private void PlayShake()
        {
            SoundInstance aSound = mSoundManager.GetSoundInstance((uint)Res.SOUND_SHAKE);
            if (aSound != null)
            {
                aSound.SetVolume(mShakeCount * 0.5);
                aSound.Play(false, true);
            }
        }

        /// <summary>FUN_0001613c: plays a sound effect.</summary>
        public void PlaySound(int theSoundId)
        {
            PlaySample(theSoundId);
        }

        /// <summary>
        /// FUN_00015208: reads a word list in the dictionary's encoding (a line may start with how many letters
        /// it keeps from the previous one). Words of more than two letters are added, upper-cased.
        /// </summary>
        public bool LoadWordList(string thePath, System.Collections.Generic.List<string> theWords)
        {
            byte[] aData = ContentFS.ReadAllBytes(thePath);
            if (aData == null)
            {
                return false;
            }
            char[] aWord = new char[0x100];
            char[] aPrevWord = new char[0x100];
            int aLen = 0;
            int aKeep = 0;
            bool inNumber = false;
            foreach (byte b in aData)
            {
                if (mShutdown)
                {
                    return false;
                }
                char c = (char)b;
                if (c >= '0' && c <= '9')
                {
                    aKeep = (inNumber ? aKeep * 10 : 0) + (c - '0');
                    inNumber = true;
                    continue;
                }
                if (c != '\n' && c != '\r')
                {
                    if (aKeep > 0 && aLen == 0)
                    {
                        System.Array.Copy(aPrevWord, aWord, aKeep);
                        aLen = aKeep;
                    }
                    aWord[aLen++] = c;
                    continue;
                }
                if (aLen > 0)
                {
                    System.Array.Copy(aWord, aPrevWord, aLen);
                }
                if (aLen > 2)
                {
                    theWords.Add(new string(aWord, 0, aLen).ToUpperInvariant());
                }
                aLen = 0;
                inNumber = false;
            }
            return true;
        }

        /// <summary>
        /// 0x11f54: moves every screen to its place in a new interface state, then, unless a move is in
        /// progress (<see cref="mInterfaceLock"/>), tells them it is done; some states change the music.
        /// </summary>
        public void GotoInterfaceState(int theState)
        {
            if (mInterfaceState == theState)
            {
                return;
            }
            mInterfaceState = theState;
            for (int i = 0; i < SCREEN_COUNT; i++)
            {
                mScreens[i]?.SetInterfaceState(theState, INTERFACE_TARGETS[theState, i], INTERFACE_PARAMS[theState]);
            }
            if (mInterfaceLock == 0)
            {
                for (int i = 0; i < SCREEN_COUNT; i++)
                {
                    mScreens[i]?.InterfaceStateDone();
                }
            }
            if (((INTERFACE_MUSIC_MASK >> theState) & 1) != 0 && mMusicInterface != null && INTERFACE_MUSIC[theState] < 8)
            {
                mMusicInterface.PlayMusic(INTERFACE_MUSIC[theState], 1.0f, true);
            }
        }

        /// <summary>
        /// Vtable slot 76 (0x15880), called by the loading screen once everything is loaded: drops it and goes
        /// to the main menu (state 2), or first to the demo splash (state 1) when properties/default.xml sets
        /// SPLASH_MESSAGE_TEXT. Screens are put in place at once, without moving.
        /// </summary>
        public void LoadingScreenDone()
        {
            if (mLoadingDone)
            {
                return;
            }
            mLoadingDone = true;
            InterfaceWidget aLoadingScreen = mScreens[SCREEN_LOADING];
            mScreens[SCREEN_LOADING] = null;
            if (aLoadingScreen != null)
            {
                mWidgetManager.RemoveWidget(aLoadingScreen);
                SafeDeleteWidget(aLoadingScreen);
            }
            // TODO: FUN_000159bc, FUN_00012fd4 and the More Games widget's AttachToManager(0x11, 0, 0).
            int aState = mScreens[SCREEN_DEMO_SPLASH] != null ? 1 : 2;
            if (mInterfaceState != aState)
            {
                mInterfaceState = aState;
                for (int i = 0; i < SCREEN_COUNT; i++)
                {
                    mScreens[i]?.SetInterfaceState(aState, INTERFACE_TARGETS[aState, i], 0);
                }
                if (mInterfaceLock == 0)
                {
                    for (int i = 0; i < SCREEN_COUNT; i++)
                    {
                        mScreens[i]?.InterfaceStateDone();
                    }
                }
                if (aState == 2)
                {
                    mMusicInterface?.PlayMusic(1, 1.0f, true);
                }
            }
        }
    }
}
