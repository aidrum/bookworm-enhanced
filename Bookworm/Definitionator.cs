using System.Collections.Generic;
using System.Text;

namespace Sexy
{
    /// <summary>
    /// Sexy::Definitionator (0x17868, BookWormApp + 0xdc4): word definitions shown for the words the player
    /// makes, from dict/oed_definitions.txt ("WORD&lt;tab&gt;(n.) definition" per line).
    /// </summary>
    public class Definitionator
    {
        public BookWormApp mApp; // 0x04

        /// <summary>std::map&lt;string, string&gt; (0x08), sorted like the original's.</summary>
        public SortedDictionary<string, string> mDefinitions = new SortedDictionary<string, string>(System.StringComparer.Ordinal);

        public Definitionator(BookWormApp theApp)
        {
            mApp = theApp;
        }

        /// <summary>
        /// FUN_00017ae8: reads the definitions. Each line splits at its first space or tab; the rest, trimmed
        /// of spaces, tabs and line ends, is the definition. Lines missing either part are skipped.
        /// </summary>
        public void Load(string thePath)
        {
            byte[] aData = ContentFS.ReadAllBytes(thePath);
            if (aData == null)
            {
                return;
            }
            foreach (string aLine in Encoding.Latin1.GetString(aData).Split('\n'))
            {
                if (aLine.Length == 0)
                {
                    continue;
                }
                int aSplit = aLine.IndexOfAny(new[] { ' ', '\t' });
                if (aSplit < 0)
                {
                    continue;
                }
                string aWord = aLine.Substring(0, aSplit);
                string aDefinition = aLine.Substring(aSplit).Trim(' ', '\t', '\r', '\n');
                if (aWord.Length != 0 && aDefinition.Length != 0)
                {
                    mDefinitions[aWord] = aDefinition;
                }
            }
            System.Console.Error.WriteLine("Loaded " + mDefinitions.Count + " definitions (" + thePath + ")");
        }

        /// <summary>FUN_0001796c: a word's definition, or "" when it has none.</summary>
        public string GetDefinition(string theWord)
        {
            return mDefinitions.TryGetValue(theWord, out string aDefinition) ? aDefinition : "";
        }

        /// <summary>Sexy::Definitionator::size.</summary>
        public int Size()
        {
            return mDefinitions.Count;
        }
    }
}
