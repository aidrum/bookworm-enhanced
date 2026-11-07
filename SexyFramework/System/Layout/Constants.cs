using Microsoft.Xna.Framework;

namespace Sexy
{
    /// <summary>
    /// Layout constants used by the framework layer. Same shape as PlantsVsZombies.NET's Constants
    /// (BackBufferSize is stored portrait-first: X = short side, Y = long side).
    /// Bookworm is a portrait universal app that runs at the device's own pixel size and only knows five
    /// screens (ScreenPos::Init, FUN_00042cd4): iPhone 320x480, iPhone Retina 640x960, iPhone 5 640x1136,
    /// iPad 768x1024 and iPad Retina 1536x2048. The port loads the iPad (or iPad Retina) textures and
    /// lays the game out for the screen's shape, again whenever it changes: the iPad design for tablet-like
    /// shapes, the iPhone design for taller ones, any height (<see cref="ChooseLayout(int, int)"/>).
    /// </summary>
    public static class Constants
    {
        public enum LanguageIndex
        {
            en,
            fr,
            de,
            es,
            it
        }

        public static bool Loaded = true;

        /// <summary>The original's five screens (short side, long side), smallest first within each family.</summary>
        public static readonly Point LAYOUT_IPHONE = new Point(320, 480);

        public static readonly Point LAYOUT_IPHONE_RETINA = new Point(640, 960);

        public static readonly Point LAYOUT_IPHONE5 = new Point(640, 1136);

        public static readonly Point LAYOUT_IPAD = new Point(768, 1024);

        public static readonly Point LAYOUT_IPAD_RETINA = new Point(1536, 2048);

        public static Point BackBufferSize = LAYOUT_IPAD;

        /// <summary>
        /// Port: true while the interface is landscape. The original swapped the app's width and height on
        /// rotation (SexyAppBase vf13); <see cref="BackBufferSize"/> stays the portrait layout.
        /// </summary>
        public static bool Landscape;

        /// <summary>The game's width: the layout's short side in portrait, its long side in landscape.</summary>
        public static int ScreenWidth => Landscape ? BackBufferSize.Y : BackBufferSize.X;

        /// <summary>The game's height: the layout's long side in portrait, its short side in landscape.</summary>
        public static int ScreenHeight => Landscape ? BackBufferSize.X : BackBufferSize.Y;

        /// <summary>The desktop window's starting width (its height is <see cref="DefaultWindowHeight"/>).</summary>
        public static int DefaultWindowWidth = 768;

        public static int DefaultWindowHeight = 1024;

        public static float S = 1f;

        public static DisplayOrientation SupportedOrientations = DisplayOrientation.Portrait | DisplayOrientation.PortraitDown;

        private static LanguageIndex mLanguage = LanguageIndex.en;

        public static LanguageIndex Language
        {
            get
            {
                return mLanguage;
            }
            set
            {
                mLanguage = value;
                ContentFS.Language = value.ToString();
            }
        }

        /// <summary>
        /// Port: the texture set loaded at launch, by the original's key for it (its screens' short side): 768
        /// iPad or 1536 iPad Retina (the port's own choice), 320 / 640 only when SEXY_LAYOUT forces an iPhone
        /// screen. The iPad sets hold everything the iPhone Retina set has, at the same size, plus the iPad's
        /// wider backgrounds and borders, so they serve both designs (<see cref="LayoutFor"/>).
        /// </summary>
        public static int TextureSide = 768;

        /// <summary>
        /// Port: picks the textures and the first layout for a display (or window) of this size, as it is
        /// held now. The original universal app took the device's own screen, one of five. The port keeps to
        /// the iPad texture sets and lays the game out for the screen's shape (<see cref="LayoutFor"/>), again
        /// whenever the window is resized or the device turned (<see cref="RelayoutFor"/>). iPad Retina is
        /// loaded when the first layout would show at least 5% bigger than with the iPad set (<see
        /// cref="HIGHER_RES_THRESHOLD"/>). The SEXY_LAYOUT environment variable forces a layout (debug aid):
        /// 320, 640, 1136, 768 or 1536 for the original screens with their own textures, or WIDTHxHEIGHT to
        /// start as for a display of that size (e.g. 1080x2400); a forced layout stays as it is.
        /// </summary>
        public static void ChooseLayout(int theScreenWidth, int theScreenHeight)
        {
            ChooseLayoutCore(theScreenWidth, theScreenHeight);
            if (!LayoutForced)
            {
                ApplySavedTextures(theScreenWidth, theScreenHeight);
            }
        }

        /// <summary>
        /// Port: the texture set chosen in the options (Low 320 iPhone, Medium 768 iPad, High 1536 iPad Retina),
        /// kept in <see cref="TexturesFileName"/> in <see cref="StorageDir"/>. The first launch keeps the set
        /// picked for the screen; later launches load the kept one, which the options can change (from the next
        /// launch: the textures are loaded once).
        /// </summary>
        public static string StorageDir;

        private const string TexturesFileName = "textures.txt";

        /// <summary>Port: the texture sets the options offer, Low to High.</summary>
        public static readonly int[] TEXTURE_SIDES = { LAYOUT_IPHONE.X, LAYOUT_IPAD.X, LAYOUT_IPAD_RETINA.X };

        private static string TexturesPath => string.IsNullOrEmpty(StorageDir) ? null : System.IO.Path.Combine(StorageDir, TexturesFileName);

        /// <summary>Port: the kept texture set, or 0 when none is kept (or it cannot be read).</summary>
        public static int ReadSavedTextureSide()
        {
            try
            {
                string aPath = TexturesPath;
                if (aPath != null && System.IO.File.Exists(aPath) &&
                    int.TryParse(System.IO.File.ReadAllText(aPath).Trim(), out int aSide) &&
                    System.Array.IndexOf(TEXTURE_SIDES, aSide) >= 0)
                {
                    return aSide;
                }
            }
            catch (System.Exception)
            {
            }
            return 0;
        }

        /// <summary>Port: keeps a texture set for the next launches.</summary>
        public static void SaveTextureSide(int theSide)
        {
            try
            {
                string aPath = TexturesPath;
                if (aPath != null)
                {
                    System.IO.Directory.CreateDirectory(StorageDir);
                    System.IO.File.WriteAllText(aPath, theSide.ToString(System.Globalization.CultureInfo.InvariantCulture));
                }
            }
            catch (System.Exception)
            {
            }
        }

        /// <summary>Port: loads the kept texture set, or keeps the one just picked (the first launch).</summary>
        private static void ApplySavedTextures(int theScreenWidth, int theScreenHeight)
        {
            int aSaved = ReadSavedTextureSide();
            if (aSaved == 0)
            {
                SaveTextureSide(TextureSide);
                return;
            }
            TextureSide = aSaved;
            BackBufferSize = LayoutFor(TextureSide, theScreenWidth, theScreenHeight);
        }

        private static void ChooseLayoutCore(int theScreenWidth, int theScreenHeight)
        {
            BackBufferSize = PickLayout(theScreenWidth, theScreenHeight);
            string aForced = System.Environment.GetEnvironmentVariable("SEXY_LAYOUT");
            LayoutForced = !string.IsNullOrEmpty(aForced);
            switch (aForced)
            {
            case null:
            case "":
                break;
            case "320":
                Force(320, LAYOUT_IPHONE);
                break;
            case "640":
                Force(640, LAYOUT_IPHONE_RETINA);
                break;
            case "1136":
                Force(640, LAYOUT_IPHONE5);
                break;
            case "768":
                Force(768, LAYOUT_IPAD);
                break;
            case "1536":
                Force(1536, LAYOUT_IPAD_RETINA);
                break;
            default:
                string[] aParts = aForced.ToLowerInvariant().Split('x');
                if (aParts.Length == 2 && int.TryParse(aParts[0], out int aWidth) && int.TryParse(aParts[1], out int aHeight))
                {
                    BackBufferSize = PickLayout(aWidth, aHeight);
                }
                break;
            }
        }

        private static void Force(int theTextureSide, Point theLayout)
        {
            TextureSide = theTextureSide;
            BackBufferSize = theLayout;
        }

        /// <summary>
        /// Port: a display at least this much bigger than the layout made with the iPad textures gets the
        /// iPad Retina ones.
        /// </summary>
        public const double HIGHER_RES_THRESHOLD = 1.05;

        /// <summary>Port: screens below this aspect (height / width) get the iPad design, taller ones the
        /// iPhone design (between 16:10 tablets and 16:9 phones).</summary>
        public const double IPAD_FAMILY_MAX_ASPECT = 1.64;

        /// <summary>Port: the iPad's 4:3; wider screens get a wider layout of the iPad design's height (pillarboxed
        /// while the interface is turned to landscape).</summary>
        public const double MIN_ASPECT = 4.0 / 3.0;

        /// <summary>Port: the iPhone layouts' squarest (3:2, the iPhone 4), for the forced iPhone texture sets.</summary>
        public const double IPHONE_MIN_ASPECT = 1.5;

        /// <summary>Port: true when SEXY_LAYOUT forces a layout, which then stays as it is when the window changes.</summary>
        public static bool LayoutForced;

        private static Point PickLayout(int theScreenWidth, int theScreenHeight)
        {
            if (theScreenWidth <= 0 || theScreenHeight <= 0)
            {
                TextureSide = LAYOUT_IPAD.X;
                return LAYOUT_IPAD;
            }
            Point aLayout = LayoutFor(LAYOUT_IPAD.X, theScreenWidth, theScreenHeight);
            double aScale = System.Math.Min((double)theScreenWidth / aLayout.X, (double)theScreenHeight / aLayout.Y);
            TextureSide = aScale >= HIGHER_RES_THRESHOLD ? LAYOUT_IPAD_RETINA.X : LAYOUT_IPAD.X;
            return LayoutFor(TextureSide, theScreenWidth, theScreenHeight);
        }

        /// <summary>
        /// Port: the layout for a screen or window of this size (the game is always portrait) with these
        /// textures. The iPad sets get the iPad design (768 / 1536 wide) below <see
        /// cref="IPAD_FAMILY_MAX_ASPECT"/> and the iPhone design (640 / 1280 wide) above it, the height
        /// following the screen's shape (ScreenPos.Init lays either out for any height), up to any height (the
        /// extra height goes above the board). Screens wider than 3:4 keep the iPad design's height and get
        /// a wider layout (ScreenPos.mCenterX), except while the interface is turned to landscape (the
        /// landscape bookshelf), when they are pillarboxed at 4:3. The forced iPhone sets keep the iPhone
        /// design, from 3:2.
        /// </summary>
        public static Point LayoutFor(int theTextureSide, int theScreenWidth, int theScreenHeight)
        {
            double anAspect = theScreenWidth > 0 && theScreenHeight > 0 ? (double)theScreenHeight / theScreenWidth : MIN_ASPECT;
            if (Landscape && theScreenWidth > 0 && theScreenHeight > 0)
            {
                // Turned to landscape (the bookshelf): the layout stays portrait, so its shape is the screen's
                // turned back. A phone keeps the iPhone design at its own 16:9 (or longer) instead of falling
                // to the pillarboxed iPad 4:3; the iPad family stays at 4:3 as before.
                anAspect = (double)System.Math.Max(theScreenWidth, theScreenHeight) / System.Math.Min(theScreenWidth, theScreenHeight);
                if (theTextureSide != LAYOUT_IPHONE.X && theTextureSide != LAYOUT_IPHONE_RETINA.X && anAspect < IPAD_FAMILY_MAX_ASPECT)
                {
                    anAspect = MIN_ASPECT;
                }
            }
            int aWidth;
            if (theTextureSide == LAYOUT_IPHONE.X || theTextureSide == LAYOUT_IPHONE_RETINA.X)
            {
                aWidth = theTextureSide;
                anAspect = System.Math.Max(IPHONE_MIN_ASPECT, anAspect);
            }
            else if (anAspect < MIN_ASPECT && !Landscape)
            {
                // Wider than 3:4: the iPad design keeps its 4:3 height and grows wider (ScreenPos centres it).
                int aHeight = theTextureSide * 4 / 3;
                return new Point((int)System.Math.Round(aHeight / anAspect), aHeight);
            }
            else if (anAspect < IPAD_FAMILY_MAX_ASPECT)
            {
                aWidth = theTextureSide;
                anAspect = System.Math.Max(MIN_ASPECT, anAspect);
            }
            else
            {
                aWidth = theTextureSide / LAYOUT_IPAD.X * LAYOUT_IPHONE_RETINA.X;
            }
            return new Point(aWidth, (int)System.Math.Round(aWidth * anAspect));
        }

        /// <summary>
        /// Port: the layout a window or screen of this size should have now, with the textures loaded at
        /// launch (width and height may both change: <see cref="LayoutFor"/>). Null when it already has it, or
        /// when SEXY_LAYOUT forces the layout.
        /// </summary>
        public static Point? RelayoutFor(int theScreenWidth, int theScreenHeight)
        {
            if (LayoutForced || theScreenWidth <= 0 || theScreenHeight <= 0)
            {
                return null;
            }
            Point aLayout = LayoutFor(TextureSide, theScreenWidth, theScreenHeight);
            return aLayout == BackBufferSize ? null : aLayout;
        }

        /// <summary>
        /// Port: the desktop's choice. The window opens portrait, as tall as fits the monitor, at the iPad's
        /// 4:3 (the widest of the original's screens), so the layout follows the window's size. The textures
        /// are picked for the whole monitor (as the game would show full screen), not the window.
        /// </summary>
        public static void ChooseDesktopLayout(int theMonitorWidth, int theMonitorHeight)
        {
            int aHeight = System.Math.Max(480, theMonitorHeight * 88 / 100);
            int aWidth = aHeight * 3 / 4;
            if (aWidth > theMonitorWidth)
            {
                aWidth = theMonitorWidth;
                aHeight = aWidth * 4 / 3;
            }
            ChooseLayoutCore(aWidth, aHeight);
            if (!LayoutForced)
            {
                // The texture set follows the monitor, not the window: the window can be maximized or made
                // full screen at any time, but the textures are loaded once (RelayoutFor keeps them).
                PickLayout(theMonitorWidth, theMonitorHeight);
                BackBufferSize = LayoutFor(TextureSide, aWidth, aHeight);
                ApplySavedTextures(aWidth, aHeight);
            }
            // Keep the window at the chosen layout's aspect (a forced phone layout gets a phone window).
            DefaultWindowHeight = aHeight;
            // Rounded, not truncated: a window a pixel narrower than the layout's shape left a band at the top.
            DefaultWindowWidth = (int)System.Math.Round((double)aHeight * ScreenWidth / ScreenHeight);
        }

        public static float InvertAndScale(float x)
        {
            return x * S;
        }
    }
}
