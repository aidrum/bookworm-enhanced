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

        /// <summary>
        /// Words that can be the bonus word (dict/bonus_words.txt, only those in the dictionary) (0x11f0). Port: the
        /// current language's only; LoadWords replaces them when the language changes.
        /// </summary>
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
            if (mCheckForUpdates)
            {
                UpdateChecker.Start();
            }
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
            AtlasResourceBase.UnpackWideImages();
            if (!LoadWords())
            {
                mLoadingFailed = true;
                return;
            }
            mCompletedLoadingThreadTasks += 2;
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
            mScreens[4] = new SlideInImage(this, AtlasResourceBase.IMAGE_GROUP1_01, new TPoint(0, 1), SlideInWidget.SLIDE_FROM_TOP) { mTile = true };
            mScreens[5] = new SlideInImage(this, AtlasResourceBase.IMAGE_GROUP1_02, new TPoint(0, 1), SlideInWidget.SLIDE_FROM_TOP) { mTile = true };
            // Port (wider than the iPad design): the whole pedestal (AtlasResourceBase.IMAGE_WIDE_LEX_PLATFORM, iPad textures only), which
            // reaches 36 iPad pixels further left and starts 4 lower than the atlas's cut-off one (210 wide on iPad).
            int aPedestalScale = AtlasResourceBase.IMAGE_GROUP11_14.mWidth / 210;
            mScreens[6] = new SlideInImage(this, AtlasResourceBase.IMAGE_GROUP11_14, new TPoint(mScreenPos.At(0x78), mScreenPos.At(0x7c)), SlideInWidget.SLIDE_FROM_BOTTOM)
            {
                mStretchEdges = false,
                mWideImage = AtlasResourceBase.IMAGE_WIDE_LEX_PLATFORM,
                mWideOffset = new TPoint(-36 * aPedestalScale, 4 * aPedestalScale)
            };
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

        /// <summary>
        /// The loading thread's dictionary, bonus words (only those in the dictionary) and definitions. Port: only
        /// the current language's; ChangeLanguage drops them and calls this again for the new one. In another
        /// language ContentFS finds its PC version's lists in &lt;lang&gt;.lproj/dict.
        /// </summary>
        public bool LoadWords()
        {
            Dictionary aDictionary = new Dictionary();
            if (!aDictionary.LoadFile("dict/oed_encoded.txt"))
            {
                return false;
            }
            System.Collections.Generic.List<string> aBonusWords = new System.Collections.Generic.List<string>();
            LoadWordList("dict/bonus_words.txt", aBonusWords);
            int aBonusCount = aBonusWords.Count;
            aBonusWords.RemoveAll(aWord => !aDictionary.IsWord(aWord));
            Console.Error.WriteLine(string.Format("Dictionary ({0}): {1} words, {2} of {3} bonus words kept", Constants.Language, aDictionary.mWordCount, aBonusWords.Count, aBonusCount));
            Definitionator aDefinitionator = new Definitionator(this);
            // Port: the PC versions of the other languages have no definitions (HAS_DEFINITIONS false, an empty popdefs.txt).
            if (!Localization.IsTranslated)
            {
                aDefinitionator.Load("dict/oed_definitions.txt");
            }
            mDictionary = aDictionary;
            mBonusWords = aBonusWords;
            mDefinitionator = aDefinitionator;
            return true;
        }

        /// <summary>Port: drops the current language's words (ChangeLanguage, before LoadWords reads the new one's).</summary>
        private void UnloadWords()
        {
            mDictionary = null;
            mBonusWords = new System.Collections.Generic.List<string>();
            mDefinitionator = null;
            GC.Collect();
        }

        /// <summary>
        /// Port addition: the Options page's language link. Switches to the next language (Localization.LANGUAGES),
        /// keeps it, and reloads the texts and word lists. A game in progress goes on with the new words.
        /// </summary>
        public void ChangeLanguage()
        {
            Constants.LanguageIndex anOld = Constants.Language;
            // The found collection words are kept per language: write the old language's first.
            mCollectionator.Save();
            Constants.Language = Localization.LANGUAGES[(Localization.CurrentIndex() + 1) % Localization.LANGUAGES.Length];
            UnloadWords();
            if (!LoadWords())
            {
                // The new language's lists are missing: back to the old one's.
                Constants.Language = anOld;
                LoadWords();
                return;
            }
            RegistryWriteInteger("Language", (int)Constants.Language);
            Localization.LoadStrings(this);
            // Lex's voice: the PC versions' own clips are in <lang>.lproj/sounds.
            mResourceManager.ReloadLocalizedSounds();
            mCollectionator.ReloadBooks();
            ((MenuTabWidget)mScreens[SCREEN_MENU_TABS]).UpdateCompletion();
            foreach (InterfaceWidget aScreen in mScreens)
            {
                if (aScreen is WordBookWidget aBook && aBook.mContinueButton != null)
                {
                    aBook.mContinueButton.mLabel = SexyAppBase.Loc("CONTINUE");
                }
            }
            // The board swaps in its bonus words for the new language.
            mBoard?.SyncBonusWordLanguage();
        }

        /// <summary>
        /// Port (dynamic aspect): how much higher than his board spot Lex (and his balloon) go in theState: the
        /// menu pages that put him at the board spot without the board (Stats, Options, More Games) are lifted
        /// on screens taller than 16:9 (ScreenPos.mPageLift); 0 elsewhere.
        /// </summary>
        public int GetLexLift(int theState)
        {
            if (theState < 0 || theState >= INTERFACE_TARGETS.GetLength(0))
            {
                return 0;
            }
            return INTERFACE_TARGETS[theState, SCREEN_LEX] == 2 && INTERFACE_TARGETS[theState, SCREEN_BOARD] == 0 ? mScreenPos.mPageLift : 0;
        }

        /// <summary>
        /// Port: Lex's board spot (ScreenPos 0x90, or where he fades in from: 0x98) in theState: beside the board
        /// in the wide game layout (ScreenPos.mWideLex) while the board is up or hidden under a page, else the
        /// portrait place, lifted on the menu pages (GetLexLift).
        /// </summary>
        public TPoint GetLexBoardSpot(int theState, bool theEntry)
        {
            ScreenPos aPos = mScreenPos;
            if (aPos.mWideGame && theState >= 0 && theState < INTERFACE_TARGETS.GetLength(0) && INTERFACE_TARGETS[theState, SCREEN_BOARD] != 0)
            {
                return theEntry ? aPos.mWideLexEntry : aPos.mWideLex;
            }
            int anOffset = theEntry ? 0x98 : 0x90;
            return new TPoint(aPos.At(anOffset), aPos.At(anOffset + 4) - GetLexLift(theState));
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

        /// <summary>
        /// The app version (0xd8); the title is "BookWorm Deluxe " + version (0x28, FUN_0001160c). Port: the build's
        /// own version (Directory.Build.props &lt;Version&gt;, as the Android package's) instead of the iOS 1.0.5.
        /// </summary>
        public string mVersion = GetBuildVersion();

        /// <summary>Port: this build's version, without the source revision the SDK appends after a '+'.</summary>
        private static string GetBuildVersion()
        {
            System.Reflection.Assembly anAssembly = typeof(BookWormApp).Assembly;
            string aVersion = System.Reflection.CustomAttributeExtensions.GetCustomAttribute<System.Reflection.AssemblyInformationalVersionAttribute>(anAssembly)?.InformationalVersion
                ?? anAssembly.GetName().Version?.ToString(3) ?? "";
            int aPlus = aVersion.IndexOf('+');
            return aPlus >= 0 ? aVersion.Substring(0, aPlus) : aVersion;
        }

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
            RegistryWriteBoolean("LandscapeBooks", mLandscapeBooks);
            RegistryWriteBoolean("SubmitByDragging", mSubmitByDragging);
            RegistryWriteBoolean("CheckForUpdates", mCheckForUpdates);
        }

        /// <summary>
        /// Port addition (the extra options): looks for a newer Bookworm Enhanced release on GitHub at launch
        /// (UpdateChecker) and offers it on the main menu.
        /// </summary>
        public bool mCheckForUpdates = true;

        /// <summary>
        /// Port addition (the extra options, "Submit by Dragging", the PC version's name for it): a touch that
        /// drags across tiles (from any tile, new or already in the word) submits the word when let go, if it is valid (else nothing happens,
        /// as on iOS). On by default on desktops, as on the PC; off on phones and tablets, as on iOS.
        /// </summary>
        public bool mSubmitByDragging = !Main.IsMobile;

        /// <summary>
        /// Port addition (the extra options): turning a phone or tablet to landscape on the board shows the
        /// landscape bookshelf, as on iOS. Off, the interface stays portrait and the board is laid out for the
        /// wide screen instead (ScreenPos.mWideGame, as a wide desktop window).
        /// </summary>
        public bool mLandscapeBooks = true;

        /// <summary>Port: the settings SaveSettings kept.</summary>
        public void LoadSettings()
        {
            Constants.Language = Localization.GetStartLanguage(this);
            Localization.LoadStrings(this);
            RegistryReadBoolean("ShowHints", ref mShowHints);
            RegistryReadBoolean("ShowDefinitions", ref mShowDefinitions);
            RegistryReadBoolean("ConfirmScramble", ref mConfirmScramble);
            RegistryReadBoolean("LandscapeBooks", ref mLandscapeBooks);
            RegistryReadBoolean("SubmitByDragging", ref mSubmitByDragging);
            RegistryReadBoolean("CheckForUpdates", ref mCheckForUpdates);
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

        /// <summary>Port addition: the update prompt's dialog id and its Download button (UpdateChecker).</summary>
        public const int DIALOG_UPDATE_AVAILABLE = 0x7550;

        private const int BUTTON_UPDATE_DOWNLOAD = 1;

        private const int BUTTON_UPDATE_CANCEL = 2;

        /// <summary>Port addition: the release page of the update the prompt offers.</summary>
        private string mUpdateUrl = UpdateChecker.RELEASES_PAGE_URL;

        /// <summary>Port addition: updates the main menu has been settled for (the update prompt waits a second).</summary>
        private int mMenuSettledTicks;

        public override void UpdateFrames()
        {
            base.UpdateFrames();
            if (UpdateChecker.HasUpdate)
            {
                CheckUpdatePrompt();
            }
        }

        /// <summary>
        /// Port addition: shows the update prompt (UpdateChecker found a newer release) once the main menu is
        /// settled: loaded, nothing moving, no other dialog up, and it has been so for a second. Turned off in
        /// the meantime, it isn't shown.
        /// </summary>
        private void CheckUpdatePrompt()
        {
            if (!mCheckForUpdates)
            {
                UpdateChecker.TakeUpdate(out _, out _);
                return;
            }
            if (!mLoadingDone || mInterfaceState != 2 || mInterfaceLock != 0 || mWidgetManager.mBaseModalWidget != null)
            {
                mMenuSettledTicks = 0;
                return;
            }
            if (++mMenuSettledTicks < 100 || !UpdateChecker.TakeUpdate(out string aNewVersion, out mUpdateUrl))
            {
                return;
            }
            Alert anAlert = new Alert(DIALOG_UPDATE_AVAILABLE, this, this);
            anAlert.SetTitle(Loc("Update Available"));
            anAlert.SetMessage(Loc("A new version of Bookworm Enhanced is available!") + "\n\n" +
                Loc("Current version: ") + mVersion + "\n" + Loc("New version: ") + aNewVersion);
            anAlert.AddGreenButton(BUTTON_UPDATE_DOWNLOAD, Loc("Download"));
            anAlert.AddRedButton(BUTTON_UPDATE_CANCEL, Loc("Cancel"));
            anAlert.Show();
        }

        /// <summary>Port addition: Download opens the release page; either button closes the prompt.</summary>
        public override void DialogButtonDepress(int theDialogId, int theButtonId)
        {
            if (theDialogId == DIALOG_UPDATE_AVAILABLE)
            {
                if (theButtonId == BUTTON_UPDATE_DOWNLOAD)
                {
                    OpenURL(mUpdateUrl);
                }
                return;
            }
            base.DialogButtonDepress(theDialogId, theButtonId);
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

        /// <summary>
        /// FUN_00015b70's path: the mode's saved game in the documents folder. Port: each game keeps to the
        /// language it was started in (Board.mLanguage); English's are the original names (classic.dat,
        /// action.dat), another language's get its code (classic_es.dat, action_es.dat). The other language's
        /// games wait on standby until it is picked again.
        /// </summary>
        public string GetSavedGamePath(int theMode, int theLanguage)
        {
            string aName = theMode == GAME_ARCADE ? "action" : "classic";
            if (theLanguage > 0 && theLanguage < Localization.LANGUAGES.Length)
            {
                aName += "_" + Localization.LANGUAGES[theLanguage];
            }
            return System.IO.Path.Combine(applicationStoragePath ?? AppContext.BaseDirectory, aName + ".dat");
        }

        /// <summary>The current language's saved game of the mode.</summary>
        public string GetSavedGamePath(int theMode)
        {
            return GetSavedGamePath(theMode, Localization.CurrentIndex());
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
            mBoard.mLanguage = Localization.CurrentIndex();
            mBoard.mZOrder = 0x46;
            mScreens[SCREEN_BOARD] = mBoard;
            if (HasSavedGame(theMode))
            {
                mBoard.LoadGame(theMode);
                // Port: the save's bonus words may be in the other language.
                mBoard.SyncBonusWordLanguage();
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
        public void DialogOpened(Dialogoid theDialog)
        {
            mOpenDialogs++;
            if (!mOpenDialogList.Contains(theDialog))
            {
                mOpenDialogList.Add(theDialog);
            }
            mBoard?.SetPaused(2, true);
        }

        /// <summary>Port: how many Dialogoids are open now.</summary>
        public int mOpenDialogs;

        /// <summary>Port: the Dialogoids open now, placed again by ApplyLayout (Dialogoid.Relayout).</summary>
        private readonly System.Collections.Generic.List<Dialogoid> mOpenDialogList = new System.Collections.Generic.List<Dialogoid>();

        /// <summary>
        /// Port addition (dynamic aspect): the window was resized or the device turned. Once nothing is moving and
        /// everything is loaded, ScreenPos is laid out for the new size (its design may change between iPad and
        /// iPhone too: Constants.LayoutFor), the screens that are up are placed again (InterfaceWidget.Relayout),
        /// and so are the open dialogs (Dialogoid.Relayout, keeping their animation); a balloon Lex was saying
        /// goes. Until then Main keeps the old layout letterboxed in the window.
        /// </summary>
        public override bool ApplyLayout(Microsoft.Xna.Framework.Point theSize)
        {
            if (!mLoadingDone || mInterfaceLock != 0)
            {
                return false;
            }
            base.ApplyLayout(theSize);
            // The tiles keep their places on the board; its grid's origin moves with the design (0x178 / 0x17c).
            int anOldGridX = mScreenPos.At(0x178);
            int anOldGridY = mScreenPos.At(0x17c);
            // Always the portrait size: ScreenPos holds the landscape pages' places too (turned: mWidth/mHeight swap).
            mScreenPos.Init(GetTextureSide(), Constants.BackBufferSize.X, Constants.BackBufferSize.Y);
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
            foreach (Dialogoid aDialog in mOpenDialogList.ToArray())
            {
                aDialog.Relayout();
            }
            mWordBalloon?.HideNow();
            return true;
        }

        /// <summary>Vtable slot 54 (0x13184): resumes the board (FUN_00007184(board, 2, 0)).</summary>
        public void DialogClosed(Dialogoid theDialog)
        {
            mOpenDialogList.Remove(theDialog);
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
            // Port: landscape books off, the interface stays portrait (the layout follows the wide screen).
            if (!mLandscapeBooks)
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
        /// Port (phones and tablets, Main.FollowSurfaceOrientation): the interface is turned and laid out for the
        /// new shape first (the 4:3 landscape layout, pillarboxed), so the pages arrive in their landscape places.
        /// </summary>
        public override void InterfaceOrientationChanged(UI_ORIENTATION toOrientation)
        {
            bool aWasLandscape = OrientationIsLandscape(mInterfaceOrientation);
            int aState = mInterfaceState;
            base.InterfaceOrientationChanged(toOrientation);
            XnaGame.ApplyPendingLayoutNow(this);
            if (OrientationIsLandscape(toOrientation))
            {
                GotoInterfaceState(13 + mCollectionator.mLandscapeView);
            }
            else if (toOrientation == UI_ORIENTATION.UI_ORIENTATION_PORTRAIT || toOrientation == UI_ORIENTATION.UI_ORIENTATION_PORTRAIT_UPSIDE_DOWN)
            {
                if (aState >= 13 && aState <= 15)
                {
                    mCollectionator.mLandscapeView = aState - 13;
                }
                if (aWasLandscape)
                {
                    GotoInterfaceState(10);
                }
            }
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
            else
            {
                SaveProgress();
            }
        }

        /// <summary>Vtable slot 58 (0x12708): quitting saves the game in progress.</summary>
        public override void Shutdown()
        {
            if (!mShutdown)
            {
                if (mBoard != null && mBoard.CanSaveGame())
                {
                    mBoard.SaveGame(mGameMode);
                }
                else
                {
                    SaveProgress();
                }
            }
            base.Shutdown();
        }

        /// <summary>
        /// Port addition: saves the personal bests and the collections when there is no game in progress to save
        /// (SaveGame saves them too), so the Stats page survives a game that ended before the app closed.
        /// </summary>
        public void SaveProgress()
        {
            mPersonalBests?.Save();
            if (mCollectionator != null && mScreens[SCREEN_MENU_CURTAIN] != null)
            {
                mCollectionator.Save();
            }
        }

        /// <summary>
        /// Vtable slot 33 (0x12424): shaking the device scrambles. The acceleration less its slow drift is a jolt
        /// when its squared size passes 0.32; jolts along the same line keep the strongest; a jolt the other way
        /// (dot below -0.6) within a quarter second of the last counts, rattling (SOUND_SHAKE at half volume a
        /// count); the fourth asks the board to scramble and shakes are ignored for 2 seconds. Ignored while there
        /// is no board, it is disabled, or a modal dialog is up. (The port's heads have to feed this.)
        /// </summary>
        /// <summary>Port addition (keyboard/controller): the menus and dialogs (MenuNavigator).</summary>
        public MenuNavigator mNavigator;

        /// <summary>
        /// Port addition (keyboard/controller): while playing, the pad works the board's selector (Board.Selector.cs;
        /// the board gets the keys itself); otherwise the pad and the keys work the menus and dialogs.
        /// </summary>
        public override bool GamePadUpdate(Microsoft.Xna.Framework.Input.GamePadState thePad, Microsoft.Xna.Framework.Input.GamePadState thePrevPad)
        {
            mNavigator ??= new MenuNavigator(this);
            if (mBoard != null && (mBoard.GamePadUpdate(thePad, thePrevPad) || mBoard.CanUseSelector()))
            {
                mNavigator.Idle();
                return true;
            }
            mNavigator.Update(thePad, thePrevPad);
            return true;
        }

        /// <summary>Port addition: mouse or touch hides the keyboard/controller selector and menu focus.</summary>
        public override void PointerInputUsed()
        {
            if (mBoard != null)
            {
                mBoard.mSelectorVisible = false;
            }
            mNavigator?.Hide();
        }

        /// <summary>Port addition (Android TV): the remote's Menu button scrambles while playing.</summary>
        public override void MenuButtonPress()
        {
            mBoard?.MenuScramble();
        }

        /// <summary>Port addition: back (Escape, B, Back, Start) closes a book page first (MenuNavigator.BackPressed).</summary>
        public override bool BackButtonPress()
        {
            if (mNavigator != null && mNavigator.BackPressed())
            {
                return true;
            }
            return base.BackButtonPress();
        }

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
        public bool LoadWordList(string thePath, System.Collections.Generic.List<string> theWords, string theLanguage = null)
        {
            // Port: theLanguage reads another language's file than the current one's.
            byte[] aData = theLanguage == null ? ContentFS.ReadAllBytes(thePath) : ContentFS.ReadAllBytes(thePath, theLanguage);
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
            // Port: back from the board to the menus, the personal bests (the Stats page) and the collections are
            // saved at once; desktops never suspend the app the way iOS does, and closing it may skip Shutdown.
            if (HasBoard(mInterfaceState) && !HasBoard(theState))
            {
                SaveProgress();
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

        internal static bool HasBoard(int theState)
        {
            return theState >= 0 && theState < INTERFACE_TARGETS.GetLength(0) && INTERFACE_TARGETS[theState, SCREEN_BOARD] != 0;
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
            // Port: the window may have changed while loading (the layout waits for it): laid out for it now, before
            // the menu slides in, rather than just after.
            XnaGame.RelayoutNow(this);
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
