using StringMap = System.Collections.Generic.Dictionary<string, string>;

namespace Sexy
{
    /// <summary>
    /// Port addition: the translated modes, each from an official PC Bookworm Deluxe of that language
    /// (references/BookwormWindowsSPANISH, references/NewLanguages: French, German and Italian 1.13, Swedish
    /// 1.20). The iOS game is English only. In another language the game takes that PC version's word list and
    /// bonus words (Content/&lt;lang&gt;.lproj/dict, found first by ContentFS while the language is picked), its
    /// letter values and odds (LETTER_VALUES / LETTER_FREQS in its properties/default.xml), the Lex clips it
    /// recorded anew (&lt;lang&gt;.lproj/sounds; the Swedish version kept the English voice) and the texts it has.
    /// Texts the PC version lacks are translated in its terms, art with English baked in has its words drawn over
    /// text-free copies (GradientText), and no PC version but English has definitions (HAS_DEFINITIONS false), so
    /// none are shown. net_content (More Games) stays English. Brazilian Portuguese is the port's own: no PC version
    /// had it, so its word list (references/NewLanguages/Portuguese) was built into pt.lproj/dict (only letters
    /// the tiles fold onto, 3 to 12 letters, a Q only before a U, accent twins merged into their most frequent
    /// spelling, Latin-1 byte order) and its texts, books, bonus words and letter tables were made for it.
    /// </summary>
    public static partial class Localization
    {
        /// <summary>The languages, in the order the Options page's link cycles through them. Indexes are saved
        /// (bonus word slots, saved game names): new languages go at the end.</summary>
        public static readonly Constants.LanguageIndex[] LANGUAGES =
        {
            Constants.LanguageIndex.en, Constants.LanguageIndex.es, Constants.LanguageIndex.fr,
            Constants.LanguageIndex.de, Constants.LanguageIndex.it, Constants.LanguageIndex.sv, Constants.LanguageIndex.pt
        };

        /// <summary>The names the Options page's link shows, by LANGUAGES.</summary>
        public static readonly string[] LANGUAGE_NAMES = { "English", "Español", "Français", "Deutsch", "Italiano", "Svenska", "Português" };

        /// <summary>True in any language but English: the iOS art's English is replaced, definitions are hidden.</summary>
        public static bool IsTranslated
        {
            get { return Constants.Language != Constants.LanguageIndex.en; }
        }

        /// <summary>The PC versions' LETTER_VALUES, A to Z, by language.</summary>
        private static readonly System.Collections.Generic.Dictionary<Constants.LanguageIndex, int[]> LETTER_VALUES =
            new System.Collections.Generic.Dictionary<Constants.LanguageIndex, int[]>
        {
            { Constants.LanguageIndex.es, new[] { 1, 3, 5, 2, 1, 4, 2, 4, 1, 8, 10, 1, 3, 1, 1, 3, 5, 1, 1, 1, 1, 4, 10, 8, 4, 10 } },
            { Constants.LanguageIndex.fr, new[] { 1, 3, 3, 2, 1, 4, 2, 4, 1, 8, 10, 1, 2, 1, 1, 3, 8, 1, 1, 1, 1, 4, 10, 10, 6, 10 } },
            { Constants.LanguageIndex.de, new[] { 1, 3, 4, 1, 1, 4, 2, 2, 1, 6, 4, 2, 3, 1, 2, 4, 10, 1, 1, 1, 1, 6, 3, 8, 10, 3 } },
            { Constants.LanguageIndex.it, new[] { 1, 5, 2, 5, 1, 5, 8, 8, 1, 10, 10, 3, 3, 3, 1, 5, 10, 2, 2, 2, 3, 5, 10, 10, 10, 8 } },
            { Constants.LanguageIndex.sv, new[] { 1, 3, 5, 2, 1, 4, 3, 4, 1, 4, 3, 3, 3, 1, 1, 3, 10, 2, 2, 2, 4, 4, 5, 8, 8, 4 } },
            // Portuguese has no PC version: Brazilian Scrabble's values, with 10 for K, W and Y (not in its set).
            { Constants.LanguageIndex.pt, new[] { 1, 3, 2, 2, 1, 4, 4, 4, 1, 5, 10, 2, 1, 3, 1, 2, 6, 1, 1, 1, 1, 4, 10, 8, 10, 8 } },
        };

        /// <summary>The PC versions' LETTER_FREQS, A to Z, by language (the PC rolls rand() % their sum, as iOS does).</summary>
        private static readonly System.Collections.Generic.Dictionary<Constants.LanguageIndex, int[]> LETTER_WEIGHTS =
            new System.Collections.Generic.Dictionary<Constants.LanguageIndex, int[]>
        {
            { Constants.LanguageIndex.es, new[] { 46899, 6372, 12403, 9018, 33055, 2799, 4225, 2760, 21939, 1651, 134, 10330, 8893,
                17569, 19353, 6600, 1059, 26221, 24918, 11693, 8236, 2554, 30, 614, 498, 1981 } },
            { Constants.LanguageIndex.fr, new[] { 235058, 37878, 86424, 56429, 333362, 32123, 41312, 26606, 195959, 5558, 4717, 94089, 60035,
                158785, 129919, 54241, 10890, 191182, 213037, 155287, 89912, 25521, 1343, 6989, 10206, 24011 } },
            { Constants.LanguageIndex.de, new[] { 5521, 2533, 2190, 2887, 16894, 1916, 3461, 3511, 4331, 115, 1828, 3874, 2426,
                7703, 2217, 1288, 44, 6711, 6265, 7668, 3677, 746, 906, 74, 61, 1127 } },
            { Constants.LanguageIndex.it, new[] { 10526, 1258, 3873, 2190, 8187, 1192, 1957, 379, 10553, 3, 13, 3345, 2668,
                4927, 7176, 2250, 113, 6804, 5901, 6565, 1952, 1979, 4, 6, 7, 948 } },
            { Constants.LanguageIndex.sv, new[] { 10299, 2942, 1333, 4620, 6763, 2881, 4301, 2102, 4966, 1529, 4561, 5593, 3311,
                6518, 5285, 2911, 350, 7454, 7189, 6839, 3078, 2948, 350, 549, 1762, 350 } },
            // Portuguese: each letter's count in its word list (accents folded), as the PC lists' odds look made.
            { Constants.LanguageIndex.pt, new[] { 530779, 57864, 173290, 116092, 384499, 50382, 63535, 34810, 361055, 14275, 345, 138391, 164330,
                185660, 341435, 89031, 15373, 316227, 261986, 176979, 114706, 54934, 181, 17823, 527, 21442 } },
        };

        /// <summary>The current language's letter values, A to Z, or null for the original's (English).</summary>
        public static int[] LetterValues
        {
            get { return LETTER_VALUES.TryGetValue(Constants.Language, out int[] aValues) ? aValues : null; }
        }

        /// <summary>The current language's letter odds, A to Z, or null for the original's (English).</summary>
        public static int[] LetterWeights
        {
            get { return LETTER_WEIGHTS.TryGetValue(Constants.Language, out int[] aWeights) ? aWeights : null; }
        }

        /// <summary>The sum of LetterWeights (the PC's roll range).</summary>
        public static int LetterWeightTotal
        {
            get
            {
                int[] aWeights = LetterWeights;
                return aWeights == null ? 0 : System.Linq.Enumerable.Sum(aWeights);
            }
        }

        /// <summary>The string tables, by language (English has none: its keys are its texts).</summary>
        private static System.Collections.Generic.Dictionary<Constants.LanguageIndex, StringMap> TABLES
        {
            get
            {
                return new System.Collections.Generic.Dictionary<Constants.LanguageIndex, StringMap>
                {
                    { Constants.LanguageIndex.es, SPANISH }, { Constants.LanguageIndex.fr, FRENCH },
                    { Constants.LanguageIndex.de, GERMAN }, { Constants.LanguageIndex.it, ITALIAN },
                    { Constants.LanguageIndex.sv, SWEDISH }, { Constants.LanguageIndex.pt, PORTUGUESE }
                };
            }
        }

        /// <summary>The index of the current language in LANGUAGES.</summary>
        public static int CurrentIndex()
        {
            int anIndex = System.Array.IndexOf(LANGUAGES, Constants.Language);
            return anIndex < 0 ? 0 : anIndex;
        }

        /// <summary>The language saved, or on the first launch the system's (English unless it is one of LANGUAGES).</summary>
        public static Constants.LanguageIndex GetStartLanguage(SexyAppBase theApp)
        {
            int aLanguage = 0;
            if (!theApp.RegistryReadInteger("Language", ref aLanguage))
            {
                string aCode = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
                aLanguage = (int)Constants.LanguageIndex.en;
                foreach (Constants.LanguageIndex aCandidate in LANGUAGES)
                {
                    if (aCandidate.ToString() == aCode)
                    {
                        aLanguage = (int)aCandidate;
                    }
                }
                theApp.RegistryWriteInteger("Language", aLanguage);
            }
            Constants.LanguageIndex aResult = (Constants.LanguageIndex)aLanguage;
            return System.Array.IndexOf(LANGUAGES, aResult) < 0 ? Constants.LanguageIndex.en : aResult;
        }

        /// <summary>Fills the app's string table (SexyAppBase.Loc) for the current language.</summary>
        public static void LoadStrings(SexyAppBase theApp)
        {
            theApp.mPopLoc.mStrings.Clear();
            if (TABLES.TryGetValue(Constants.Language, out StringMap aTable))
            {
                foreach (System.Collections.Generic.KeyValuePair<string, string> aString in aTable)
                {
                    theApp.mPopLoc.mStrings[aString.Key] = aString.Value;
                }
            }
        }

        /// <summary>A text of one or more lines ('\n') in the current language, split into its lines.</summary>
        public static string[] Lines(string theEnglish)
        {
            return SexyAppBase.Loc(theEnglish).Split('\n');
        }

        /// <summary>
        /// The game's English texts that the PC version has, with its Spanish (properties/default.xml; the id
        /// is noted where the text is not the whole PC string). The PC's double spaces between sentences are
        /// the iOS texts' line breaks here.
        /// </summary>
        private static readonly StringMap SPANISH = new StringMap(System.StringComparer.Ordinal)
        {
            // HINT_DIALOG_TITLES / HINT_DIALOG_TEXTS
            { "How to Play", "Cómo jugar" },
            { "Link letters together to make words. Longer words are worth more points!",
                "Junta las letras para formar palabras. ¡Las palabras más largas valen más puntos!" },
            { "Watch out!", "¡Cuidado!" },
            { "Don't let burning tiles reach the bottom.  They will set the library on fire!\n\nHINT: Make longer words and you get fewer red tiles.",
                "¡No dejes que las fichas de fuego lleguen a la parte inferior o incendiarán tu biblioteca!\n\nPISTA: Forma palabras más largas y tendrás menos fichas rojas." },
            { "Bonus Tile!", "¡Bonificación!" },
            { "Use that green tile in a word for extra bonus points.",
                "Utiliza esa ficha verde en una palabra para conseguir puntos de bonificación extra." },
            { "Super Bonus Tile!", "¡Super ficha!" },
            { "Gold tiles are worth mega points!\n\nCreate words with 5+ letters for a chance to earn more gold tiles!",
                "¡Las fichas de oro valen mega puntos!\n\n¡Crea palabras con más de 5 letras y podrás ganar más fichas de oro!" },
            { "Warning!", "¡Cuidado!" },
            { "Scrambling letters causes burning tiles to appear, so do it sparingly.",
                "Al mezclar las letras aparecerán las fichas de fuego, así que hazlo con moderación." },
            { "Submit Button", "Botón enviar" },
            { "You may also submit selected words by clicking on the SUBMIT button.",
                "Para enviar las palabras seleccionadas también puedes hacer clic en el botón ENVIAR." },
            { "Submit by Clicking", "Hacer clic" },
            { "You may also submit selected words by clicking on the last tile in the word.",
                "Para enviar las palabras seleccionadas también puedes hacer clic en la última ficha de la palabra." },
            { "You may also submit words by clicking the left mouse button and dragging to select tiles.\n\nRelease the mouse button to submit the word.",
                "Para enviar las palabras seleccionadas también puedes hacer clic con el botón primario del ratón y arrastrar para seleccionar fichas.\n\nSuelta el botón del ratón para enviar la palabra." },
            { "Submit by Keyboard", "El teclado" },
            { "You may also submit selected words by pressing the ENTER key on the keyboard.",
                "Para enviar las palabras seleccionadas también puedes pulsar la tecla ENTER del teclado." },
            { "Bonus Word!", "¡Bonificación!" },
            { "For even more points, submit a word that matches the Bonus Word.",
                "Para conseguir más puntos, envía una palabra que coincida con la Palabra de bonificación." },
            { "Fire Alert!", "¡Incendio!" },
            { "That burning tile is too close!\n\nUse it this turn or your library will catch fire!",
                "¡Esa ficha de fuego está demasiado cerca!\n\n¡Úsala en este turno o tu biblioteca se incendiará!" },
            // Not in the PC version: translated for the port, in its terms (ficha, mezclar, bonificación).
            { "That burning tile is too close!\n\nUse it soon or your library will catch fire!",
                "¡Esa ficha de fuego está demasiado cerca!\n\n¡Úsala pronto o tu biblioteca se incendiará!" },
            // CLICK_DISABLE_HINTS, CONTINUE, CANCEL, OK
            { "Disable Hints", "Desactivar pistas" },
            { "Continue", "Continuar" },
            { "CONTINUE", "CONTINUAR" },
            { "Cancel", "Cancelar" },
            { "OK", "Aceptar" },
            // CANT_SCRAMBLE, CANT_SCRAMBLE_TEXT_1, CANT_SCRAMBLE_TEXT_2
            { "Can't Scramble!", "¡No se puede mezclar!" },
            { "Scrambling now would put too many fire tiles on the board!",
                "¡Si mezclas ahora pondrás muchas fichas de fuego en el tablero!" },
            { "Scrambling now would cause you to lose the game!", "¡Si mezclas ahora, perderás el juego!" },
            // SOUND_FX, MUSIC, SHOW_HINTS, SHOW_DEFINITIONS
            { "Sound Effects", "Sonido" },
            { "Music Tracks", "Música" },
            { "Show Hints", "Mostrar pistas" },
            { "Show Definitions", "Mostrar definiciones" },
            // OPTIONS, LEVEL_UP
            { "OPTIONS!", "¡OPCIONES!" },
            { "LEVEL UP!", "¡PASAS DE NIVEL!" },
            // MAKE_X_FOR_BONUS ("Haz la palabra “%s” para conseguir puntos de bonificación.")
            { "Make the word", "Haz la palabra" },
            { "for bonus points!", "para puntos bonus!" },
            // X_BONUS
            { "+{0} BONUS!", "+{0} ¡BONUS!" },
            // YOUR_RANK, NEXT_RANK_AT + PTS, TIME, LEVEL_X, TOTAL
            { "Your Rank", "Tu posición" },
            { "Next Rank at {0} pts", "Próxima posición a {0} puntos" },
            { "Rank {0}: ", "Posición {0}: " },
            { "Time", "Tiempo" },
            { "Level ", "Nivel " },
            { "Total ", "Total " },
            // HIGH_SCORE, LONGEST_WORD, BEST_WORD, X_LETTERS, PTS, HELP_BUTTON_NAMES (Action Mode)
            { "High Score", "Puntuación más alta" },
            { "Longest Word", "Palabra más larga" },
            { "Best Word", "Mejor palabra" },
            { "{0} ({1} letters)", "{0} ({1} letras)" },
            { "{0} ({1} pts)", "{0} ({1} puntos)" },
            { " (Action)", " (Acción)" },
            // HELP_TEXTS
            { "Select adjacent letter tiles to spell words!", "Haz clic en las letras para formar palabras." },
            { "Don't let fire tiles burn through the bottom or it's game over!",
                "¡Cuidado con las fichas de fuego! Si llegan abajo, ¡fin del juego!" },
            // Not in the PC version: translated for the port.
            { "Start New Game", "Nueva partida" },
            { "Continue Game", "Continuar partida" },
            { "Discard Saved Game", "Descartar partida" },
            { "TIME BONUS!", "¡BONUS DE TIEMPO!" },
            { "There are too many fire tiles at the top of the board!", "¡Hay demasiadas fichas de fuego en la parte superior del tablero!" },
            { "Scrambling the board will add fire tiles and remove your bonus tiles.",
                "Al mezclar el tablero aparecerán fichas de fuego y perderás tus fichas de bonificación." },
            { "Scramble", "Mezclar" },
            { "If you're really stuck, tap me or try shaking things up!", "Si de verdad estás atascado, ¡tócame o agita el dispositivo!" },
            { "Highest Rank", "Posición más alta" },
            { "PERSONAL BESTS!", "¡MEJORES MARCAS!" },
            { "GAME OVER!", "¡FIN DEL JUEGO!" },
            { "Confirm to Scramble", "Confirmar al mezclar" },
            { "More Options", "Más opciones" },
            { "Back", "Volver" },
            { "Landscape Books", "Libros en horizontal" },
            { "Submit by Dragging", "Enviar al arrastrar" },
            { "Check for Updates", "Buscar actualizaciones" },
            // The update prompt (UpdateChecker).
            { "Update Available", "Actualización disponible" },
            { "A new version of Bookworm Enhanced is available!", "¡Hay una nueva versión de Bookworm Enhanced disponible!" },
            { "Current version: ", "Versión actual: " },
            { "New version: ", "Nueva versión: " },
            { "Download", "Descargar" },
            { "Texture Quality", "Calidad de texturas" },
            { "UI Layout", "Diseño de interfaz" },
            { "Low", "Baja" },
            { "Medium", "Media" },
            { "High", "Alta" },
            { "Restart the game to apply", "Reinicia el juego para aplicar" },
            { "TAP TO SUBMIT", "TOCA PARA ENVIAR" },
            { " (Classic)", " (Clásico)" },
            { "You've opened a new Collection Book!", "¡Has abierto un nuevo libro de colección!" },
            { "You've spelled every word in this Collection Book!", "¡Has formado todas las palabras de este libro de colección!" },
            { "You've spelled a new Collection Word!", "¡Has formado una nueva palabra de colección!" },
            { "RECENT {0} LETTER WORDS", "P. RECIENTES DE {0} LETRAS" },
            { "Tap the word bubble or the last letter tile to submit your word!",
                "¡Toca el globo de la palabra o la última ficha para enviar tu palabra!" },
            { "Shake your device to scramble all of your tiles!", "¡Tócame o agita el dispositivo para mezclar todas tus fichas!" },
            { "Turn your device to see the Bonus Book List! Can you spell them all?",
                "¡Gira el dispositivo para ver la lista de libros bonus! ¿Podrás formar todas sus palabras?" },
            { "Credits: (n.) Acknowledgement and praise for the awesome creators of Lex!",
                "Créditos: (m. pl.) ¡Reconocimiento y elogio para los geniales creadores de Lex!" },
            { "Update (n): The act or an instance of bringing something up to date. Now with 64-bit support!",
                "Actualizar (f.): Acción y efecto de poner algo al día. ¡Por fín en móviles modernos!" },
            { "Original (adj): Preceding all others in time; first. There's nothing quite like Bookworm!",
                "Original (adj.): Que precede a todos los demás en el tiempo; primero. ¡No hay nada como Bookworm!" },
            { "Lex: (n.) Me! The best-spelling worm of all time!", "Lex (m.): ¡Yo! ¡El gusano que mejor deletrea de todos los tiempos!" },
            // The collection books' names (their file names).
            { "body parts", "partes del cuerpo" },
            { "clothing", "ropa" },
            { "colors", "colores" },
            { "fruits", "frutas" },
            { "insects", "insectos" },
            { "minerals", "minerales" },
            { "pets", "mascotas" },
            { "tools", "herramientas" },
            { "vegetables", "verduras" },
            // RANK_NAMES
            { "Book Burner", "Pirómano de libros" },
            { "Encyclopedia Salesman", "Vendedor de libros" },
            { "Trainee", "Aprendiz" },
            { "Clerk", "Dependiente" },
            { "Scribe", "Escriba" },
            { "Bookkeeper", "Tenedor de libros" },
            { "Bookbinder", "Encuadernador" },
            { "Proofreader", "Corrector" },
            { "Assistant Librarian", "Ayudante" },
            { "Librarian", "Bibliotecaria" },
            { "Senior Librarian", "Bibliotecaria senior" },
            { "Archivist", "Archivista" },
            { "Grand Archivist", "Gran archivista" },
            { "Editor", "Redactor" },
            { "Editor in Chief", "Redactor jefe" },
            { "Publisher", "Editor" },
            { "Publisher Emeritus", "Editor emérito" },
            { "Glossographer", "Glosógrafo" },
            { "Wordstalker", "Cazador de palabras" },
            { "Wordhoarder", "Acaparador de palabras" },
            { "Wordmaster", "Maestro terminólogo" },
            { "Vocabulist", "Vocabulista" },
            { "Thesaurian", "Tesauro" },
            { "Thesaurus Rex", "Tesauro Rex" },
            { "Lexographer", "Lexicógrafo" },
            { "Lexographer 1st Class", "Lexicógrafo de 1ª clase" },
            { "Dictionarian", "Diccionario" },
            { "Super Dictionarian", "Súper diccionario" },
            { "Bookworm", "Bookworm" },
            { "Mega Bookworm", "Mega Bookworm" },
            { "Ultra Bookworm", "Ultra Bookworm" },
            { "Bookworm Supreme", "Bookworm supremo" },
            // The banners' titles, drawn over text-free art (GradientText.DrawBannerTitle).
            { "Word History", "Historial" },
            { "More Games", "Más Juegos" },
            // The art's other baked-in English, drawn over its text-free parts (lines split at '\n').
            { "CLASSIC\nGAME", "JUEGO\nCLÁSICO" },
            { "ACTION\nGAME", "JUEGO\nDE ACCIÓN" },
            { "HOW TO PLAY", "CÓMO JUGAR" },
            { "Saved\nGame", "Juego\nGuardado" },
            { "BONUS\nWORD", "PALABRA\nBONUS" },
            { "BONUS\nBOOK LIST", "LIBROS\nBONUS" },
            { "Complete", "Completado" },
            { "letters", "letras" },
            { "LVL", "NV" },
            { "ON", "SÍ" },
            { "OFF", "NO" },
            // The menu tabs.
            { "Play", "Jugar" },
            { "Stats", "Info" },
            { "Books", "Libros" },
            { "Options", "Opciones" },
            { "More\nGames", "Más\nJuegos" },
        };
    }
}
