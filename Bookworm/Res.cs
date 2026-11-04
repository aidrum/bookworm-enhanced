namespace Sexy
{
    /// <summary>
    /// The resources named in Bookworm's manifests (resources*.xml, group "Init"). Port of the resource
    /// binding function FUN_000256f0, which fetches each one from the ResourceManager after the group has
    /// loaded. The atlas sub-images cut from the IMAGE_GROUP sheets are in <see cref="AtlasResourceBase"/>.
    /// </summary>
    public static class Res
    {
        public static Font FONT_ARIAL_LARGE;

        public static Font FONT_ARIAL_LARGE_OUTLINED;

        public static Font FONT_ARIAL_MEDIUM;

        public static Font FONT_ARIAL_MEDIUM_OUTLINED;

        public static Font FONT_ARIAL_SMALL;

        public static Font FONT_COOPER;

        public static Font FONT_COOPER_OUTLINED;

        public static Font FONT_DEFINITIONS_LARGE;

        public static Font FONT_DEFINITIONS_MEDIUM;

        public static Font FONT_DEFINITIONS_SMALL;

        public static Font FONT_GOLDSCORE;

        public static Font FONT_GOLDSCORE_HEATED;

        public static Font FONT_LOCKERGNOME;

        public static Image IMAGE_GROUP1;

        public static Image IMAGE_GROUP2;

        public static Image IMAGE_GROUP3;

        public static Image IMAGE_GROUP4;

        public static Image IMAGE_GROUP5;

        public static Image IMAGE_GROUP6;

        /// <summary>iPad (768) only.</summary>
        public static Image IMAGE_IPAD1;

        public static Image IMAGE_IPAD2;

        /// <summary>iPhone Retina (640) only.</summary>
        public static Image IMAGE_IPHONE1;

        public static Image IMAGE_IPHONE2;

        /// <summary>iPad Retina (1536) only, from here to IMAGE_GROUP51.</summary>
        public static Image IMAGE_GROUP7;

        public static Image IMAGE_GROUP11;

        public static Image IMAGE_GROUP12;

        public static Image IMAGE_GROUP13;

        public static Image IMAGE_GROUP21;

        public static Image IMAGE_GROUP22;

        public static Image IMAGE_GROUP23;

        public static Image IMAGE_GROUP24;

        public static Image IMAGE_GROUP31;

        public static Image IMAGE_GROUP51;

        public static int SOUND_BAD;

        public static int SOUND_BONUSWORD;

        public static int SOUND_BONUS_COMPLETE;

        public static int SOUND_BONUS_TILE;

        public static int SOUND_BUTTON_CLICK;

        public static int SOUND_BUTTON_HIT;

        public static int SOUND_BUZZER;

        public static int SOUND_CHOMP;

        public static int SOUND_FLAME_TILE;

        public static int SOUND_GAME_OVER_BURN;

        public static int SOUND_GAME_OVER_TONE;

        public static int SOUND_LEX_ASTONISHING;

        public static int SOUND_LEX_AWESOME;

        public static int SOUND_LEX_BURP;

        public static int SOUND_LEX_CAREFUL;

        public static int SOUND_LEX_DANGER;

        public static int SOUND_LEX_EXCELLENT;

        public static int SOUND_LEX_FANTASTIC;

        public static int SOUND_LEX_GETREADY;

        public static int SOUND_LEX_GO;

        public static int SOUND_LEX_INCREDIBLE;

        public static int SOUND_LEX_LETSGO;

        public static int SOUND_LEX_LEVELUP;

        public static int SOUND_LEX_LOOKOUT;

        public static int SOUND_LEX_SCRAMBLE;

        public static int SOUND_LEX_UNBELIEVABLE;

        public static int SOUND_LEX_VERYGOOD;

        public static int SOUND_LEX_WATCHOUT;

        public static int SOUND_LEX_WELCOMETOBOOKWORM;

        public static int SOUND_NEXT_SELECT;

        public static int SOUND_PIECE_HIT;

        public static int SOUND_RED_TILE_CLEARED;

        public static int SOUND_SHAKE;

        public static int SOUND_SPIN_SUPER_BONUS;

        public static int SOUND_SUPER_BONUS_TILE;

        public static int SOUND_TILE_BURN;

        public static int SOUND_TONE_1;

        public static int SOUND_TONE_2;

        public static int SOUND_TONE_3;

        public static int SOUND_TONE_4;

        public static int SOUND_TONE_5;

        public static int SOUND_TONE_6;

        public static int SOUND_TONE_7;

        public static int SOUND_TONE_8;

        public static int SOUND_TONE_9;

        public static int SOUND_WARNING;

        public static int SOUND_WORD_COMPLETE;

        /// <summary>
        /// FUN_000256f0: fetches the "Init" group's resources. Which sheets exist depends on the screen's short
        /// side (the manifest the app parsed): IPAD1/2 at 768, IPHONE1/2 at 640, the extra sheets at 1536+.
        /// Returns false (with the manager's error set) when one is missing.
        /// </summary>
        public static bool ExtractInitResources(ResourceManager theManager, int theScreenSize)
        {
            try
            {
                FONT_ARIAL_LARGE = theManager.GetFontThrow("FONT_ARIAL_LARGE");
                FONT_ARIAL_LARGE_OUTLINED = theManager.GetFontThrow("FONT_ARIAL_LARGE_OUTLINED");
                FONT_ARIAL_MEDIUM = theManager.GetFontThrow("FONT_ARIAL_MEDIUM");
                FONT_ARIAL_MEDIUM_OUTLINED = theManager.GetFontThrow("FONT_ARIAL_MEDIUM_OUTLINED");
                FONT_ARIAL_SMALL = theManager.GetFontThrow("FONT_ARIAL_SMALL");
                FONT_COOPER = theManager.GetFontThrow("FONT_COOPER");
                FONT_COOPER_OUTLINED = theManager.GetFontThrow("FONT_COOPER_OUTLINED");
                FONT_DEFINITIONS_LARGE = theManager.GetFontThrow("FONT_DEFINITIONS_LARGE");
                FONT_DEFINITIONS_MEDIUM = theManager.GetFontThrow("FONT_DEFINITIONS_MEDIUM");
                FONT_DEFINITIONS_SMALL = theManager.GetFontThrow("FONT_DEFINITIONS_SMALL");
                FONT_GOLDSCORE = theManager.GetFontThrow("FONT_GOLDSCORE");
                FONT_GOLDSCORE_HEATED = theManager.GetFontThrow("FONT_GOLDSCORE_HEATED");
                FONT_LOCKERGNOME = theManager.GetFontThrow("FONT_LOCKERGNOME");
                IMAGE_GROUP1 = theManager.GetImageThrow("IMAGE_GROUP1");
                IMAGE_GROUP2 = theManager.GetImageThrow("IMAGE_GROUP2");
                IMAGE_GROUP3 = theManager.GetImageThrow("IMAGE_GROUP3");
                IMAGE_GROUP4 = theManager.GetImageThrow("IMAGE_GROUP4");
                IMAGE_GROUP5 = theManager.GetImageThrow("IMAGE_GROUP5");
                IMAGE_GROUP6 = theManager.GetImageThrow("IMAGE_GROUP6");
                if (theScreenSize == 0x300)
                {
                    IMAGE_IPAD1 = theManager.GetImageThrow("IMAGE_IPAD1");
                    IMAGE_IPAD2 = theManager.GetImageThrow("IMAGE_IPAD2");
                }
                else if (theScreenSize == 0x280)
                {
                    IMAGE_IPHONE1 = theManager.GetImageThrow("IMAGE_IPHONE1");
                    IMAGE_IPHONE2 = theManager.GetImageThrow("IMAGE_IPHONE2");
                }
                else if (theScreenSize >= 0x600)
                {
                    IMAGE_GROUP7 = theManager.GetImageThrow("IMAGE_GROUP7");
                    IMAGE_GROUP11 = theManager.GetImageThrow("IMAGE_GROUP11");
                    IMAGE_GROUP12 = theManager.GetImageThrow("IMAGE_GROUP12");
                    IMAGE_GROUP13 = theManager.GetImageThrow("IMAGE_GROUP13");
                    IMAGE_GROUP21 = theManager.GetImageThrow("IMAGE_GROUP21");
                    IMAGE_GROUP22 = theManager.GetImageThrow("IMAGE_GROUP22");
                    IMAGE_GROUP23 = theManager.GetImageThrow("IMAGE_GROUP23");
                    IMAGE_GROUP24 = theManager.GetImageThrow("IMAGE_GROUP24");
                    IMAGE_GROUP31 = theManager.GetImageThrow("IMAGE_GROUP31");
                    IMAGE_GROUP51 = theManager.GetImageThrow("IMAGE_GROUP51");
                }
                SOUND_BAD = theManager.GetSoundThrow("SOUND_BAD");
                SOUND_BONUSWORD = theManager.GetSoundThrow("SOUND_BONUSWORD");
                SOUND_BONUS_COMPLETE = theManager.GetSoundThrow("SOUND_BONUS_COMPLETE");
                SOUND_BONUS_TILE = theManager.GetSoundThrow("SOUND_BONUS_TILE");
                SOUND_BUTTON_CLICK = theManager.GetSoundThrow("SOUND_BUTTON_CLICK");
                SOUND_BUTTON_HIT = theManager.GetSoundThrow("SOUND_BUTTON_HIT");
                SOUND_BUZZER = theManager.GetSoundThrow("SOUND_BUZZER");
                SOUND_CHOMP = theManager.GetSoundThrow("SOUND_CHOMP");
                SOUND_FLAME_TILE = theManager.GetSoundThrow("SOUND_FLAME_TILE");
                SOUND_GAME_OVER_BURN = theManager.GetSoundThrow("SOUND_GAME_OVER_BURN");
                SOUND_GAME_OVER_TONE = theManager.GetSoundThrow("SOUND_GAME_OVER_TONE");
                SOUND_LEX_ASTONISHING = theManager.GetSoundThrow("SOUND_LEX_ASTONISHING");
                SOUND_LEX_AWESOME = theManager.GetSoundThrow("SOUND_LEX_AWESOME");
                SOUND_LEX_BURP = theManager.GetSoundThrow("SOUND_LEX_BURP");
                SOUND_LEX_CAREFUL = theManager.GetSoundThrow("SOUND_LEX_CAREFUL");
                SOUND_LEX_DANGER = theManager.GetSoundThrow("SOUND_LEX_DANGER");
                SOUND_LEX_EXCELLENT = theManager.GetSoundThrow("SOUND_LEX_EXCELLENT");
                SOUND_LEX_FANTASTIC = theManager.GetSoundThrow("SOUND_LEX_FANTASTIC");
                SOUND_LEX_GETREADY = theManager.GetSoundThrow("SOUND_LEX_GETREADY");
                SOUND_LEX_GO = theManager.GetSoundThrow("SOUND_LEX_GO");
                SOUND_LEX_INCREDIBLE = theManager.GetSoundThrow("SOUND_LEX_INCREDIBLE");
                SOUND_LEX_LETSGO = theManager.GetSoundThrow("SOUND_LEX_LETSGO");
                SOUND_LEX_LEVELUP = theManager.GetSoundThrow("SOUND_LEX_LEVELUP");
                SOUND_LEX_LOOKOUT = theManager.GetSoundThrow("SOUND_LEX_LOOKOUT");
                SOUND_LEX_SCRAMBLE = theManager.GetSoundThrow("SOUND_LEX_SCRAMBLE");
                SOUND_LEX_UNBELIEVABLE = theManager.GetSoundThrow("SOUND_LEX_UNBELIEVABLE");
                SOUND_LEX_VERYGOOD = theManager.GetSoundThrow("SOUND_LEX_VERYGOOD");
                SOUND_LEX_WATCHOUT = theManager.GetSoundThrow("SOUND_LEX_WATCHOUT");
                SOUND_LEX_WELCOMETOBOOKWORM = theManager.GetSoundThrow("SOUND_LEX_WELCOMETOBOOKWORM");
                SOUND_NEXT_SELECT = theManager.GetSoundThrow("SOUND_NEXT_SELECT");
                SOUND_PIECE_HIT = theManager.GetSoundThrow("SOUND_PIECE_HIT");
                SOUND_RED_TILE_CLEARED = theManager.GetSoundThrow("SOUND_RED_TILE_CLEARED");
                SOUND_SHAKE = theManager.GetSoundThrow("SOUND_SHAKE");
                SOUND_SPIN_SUPER_BONUS = theManager.GetSoundThrow("SOUND_SPIN_SUPER_BONUS");
                SOUND_SUPER_BONUS_TILE = theManager.GetSoundThrow("SOUND_SUPER_BONUS_TILE");
                SOUND_TILE_BURN = theManager.GetSoundThrow("SOUND_TILE_BURN");
                SOUND_TONE_1 = theManager.GetSoundThrow("SOUND_TONE_1");
                SOUND_TONE_2 = theManager.GetSoundThrow("SOUND_TONE_2");
                SOUND_TONE_3 = theManager.GetSoundThrow("SOUND_TONE_3");
                SOUND_TONE_4 = theManager.GetSoundThrow("SOUND_TONE_4");
                SOUND_TONE_5 = theManager.GetSoundThrow("SOUND_TONE_5");
                SOUND_TONE_6 = theManager.GetSoundThrow("SOUND_TONE_6");
                SOUND_TONE_7 = theManager.GetSoundThrow("SOUND_TONE_7");
                SOUND_TONE_8 = theManager.GetSoundThrow("SOUND_TONE_8");
                SOUND_TONE_9 = theManager.GetSoundThrow("SOUND_TONE_9");
                SOUND_WARNING = theManager.GetSoundThrow("SOUND_WARNING");
                SOUND_WORD_COMPLETE = theManager.GetSoundThrow("SOUND_WORD_COMPLETE");
            }
            catch (ResourceManagerException e)
            {
                theManager.Fail(e.Message);
                return false;
            }
            return true;
        }
    }
}
