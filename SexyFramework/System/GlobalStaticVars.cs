using System;

namespace Sexy
{
    public /*internal*/ static partial class GlobalStaticVars
    {
        public static void initialize(Main main)
        {
            initializeReusables();
            GlobalStaticVars.g = Main.graphics;
            GlobalStaticVars.g.Init();
            GlobalStaticVars.gSexyAppBase = Main.AppFactory(main);
            GlobalStaticVars.gSexyAppBase.Init();
            GlobalStaticVars.gSexyAppBase.Start();
        }

        public static void initializeReusables() 
        {
            //LazyReusableObjectHelper<Graphics>.Initialize(20);
            Graphics.PreAllocateMemory();
        } 

        public static void shutdown()
        {
            GlobalStaticVars.gSexyAppBase.Shutdown();
            GlobalStaticVars.gSexyAppBase.Dispose();
        }

        internal static string GetResourceDir()
        {
            return "";
        }

        internal static string CommaSeperate_(int theDispPoints)
        {
            if (theDispPoints == 0)
            {
                return "0";
            }
            return string.Format("{0:#,#}", theDispPoints);
        }

        internal static string GetDocumentsDir()
        {
            return "docs/";
        }

        public static Graphics g;

        public static SexyAppBase gSexyAppBase;

        /// <summary>
        /// Resolution sub-folder of the art (a Peggle notion). Bookworm names each resolution's files in
        /// its resource manifest instead, so this stays empty.
        /// </summary>
        public static string mResPathPrefix = "";

        internal static int gProfileVersion = 14;

        public static bool gIsPartnerBuild = false;

        public static int gSlowMoCounter = 0;

        public static bool gSlowMo = false;

        public static bool gFastMo = false;

        public static bool gLowFramerate = false;

        public static string gGetCurrentLevelName;

        public static bool gAppCloseRequest;

        public static bool gAppHasUsedCheatKeys;

        public struct CGPoint
        {
            public float x;

            public float y;
        }

        public enum Phase
        {
            TOUCH_BEGAN,
            TOUCH_MOVED,
            TOUCH_STATIONARY,
            TOUCH_ENDED,
            TOUCH_CANCELLED
        }

        public struct Touch
        {
            public GlobalStaticVars.CGPoint location;
        }
    }
}
