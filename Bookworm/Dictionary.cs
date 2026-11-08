namespace Sexy
{
    /// <summary>
    /// Sexy::Dictionary (0x2a62c, BookWormApp + 0x11dc): the word list as a trie packed into one array.
    /// Each node is (character | index of the next sibling &lt;&lt; 8); a node's first child follows it; a
    /// word ends with a 0 node. Loaded from dict/oed_encoded.txt, where each line is an optional count of
    /// letters kept from the previous word followed by the new ending (the count carries over to lines
    /// without one): "1BACA", "5S", "4K", "US" are ABACA, ABACAS, ABACK, ABACUS.
    /// </summary>
    public class Dictionary
    {
        /// <summary>Accent folding for characters 0xc0-0xff when matching loosely (table at 0x10ce7c).</summary>
        private const string ACCENT_FOLD = "AAAAAA#CEEEEIIIIDNOOOOO#OUUUUYY#aaaaaa#ceeeeiiiidnooooo#ouuuuyyy";

        public uint[] mNodes; // 0x04

        public int mCapacity; // 0x08

        public int mUsed; // 0x0c

        public int mWordCount; // 0x10

        /// <summary>FUN_000150a0: loads an encoded word file. False if it can't be read.</summary>
        public bool LoadFile(string thePath, string theLanguage = null)
        {
            // Port: theLanguage reads another language's file than the current one's.
            byte[] aData = theLanguage == null ? ContentFS.ReadAllBytes(thePath) : ContentFS.ReadAllBytes(thePath, theLanguage);
            if (aData == null)
            {
                return false;
            }
            Load(aData);
            return true;
        }

        /// <summary>FUN_0002a694: builds the trie; the node array gets one node per byte of the file.</summary>
        public void Load(byte[] theData)
        {
            mCapacity = theData.Length;
            mNodes = new uint[mCapacity];
            mUsed = 0;
            mWordCount = 0;
            byte[] aWord = new byte[0x100];
            bool inNumber = false;
            int aLen = 0;
            int aKeep = 0;
            // Port: the previous word's length. A prefix count longer than it (the PC German list had "90" after
            // a 9-letter word) would take stale bytes, 0 terminators among them, and corrupt the trie into a loop.
            int aPrevLen = 0;
            foreach (byte c in theData)
            {
                if (c >= (byte)'0' && c <= (byte)'9')
                {
                    aKeep = (inNumber ? aKeep * 10 : 0) + (c - '0');
                    inNumber = true;
                    continue;
                }
                if (IsSpace(c) && !IsBlank(c))
                {
                    // End of line: the word is complete.
                    inNumber = false;
                    if (aLen > 0)
                    {
                        aWord[aLen] = 0;
                        AddWord(aWord, aLen);
                        aPrevLen = aLen;
                    }
                    aLen = 0;
                }
                else
                {
                    if (aKeep > 0 && aLen == 0)
                    {
                        aLen = System.Math.Min(aKeep, aPrevLen);
                    }
                    if (aLen >= aWord.Length - 1)
                    {
                        continue;
                    }
                    aWord[aLen++] = (byte)char.ToUpperInvariant((char)c);
                }
            }
        }

        private static bool IsSpace(byte c)
        {
            return c == ' ' || (c >= 9 && c <= 13);
        }

        private static bool IsBlank(byte c)
        {
            return c == ' ' || c == '\t';
        }

        /// <summary>FUN_0002a7b0: adds a word (theLen letters plus its 0 terminator at theWord[theLen]).</summary>
        private void AddWord(byte[] theWord, int theLen)
        {
            if (mUsed == 0)
            {
                int aCount = theLen + 1;
                if (aCount < mCapacity)
                {
                    mUsed = aCount;
                    for (int i = 0; i < aCount; i++)
                    {
                        mNodes[i] = theWord[i];
                    }
                }
                mWordCount++;
                return;
            }
            int aNode = 0;
            int aPos = 0;
            int aLeft = theLen;
            while (true)
            {
                while ((byte)mNodes[aNode] != theWord[aPos])
                {
                    if ((mNodes[aNode] >> 8) == 0)
                    {
                        // No sibling matches: append the rest of the word and link it as the next sibling.
                        int aStart = mUsed;
                        int anEnd = aLeft + aStart + 1;
                        int aLink = 0;
                        if (anEnd < mCapacity)
                        {
                            mUsed = anEnd;
                            aLink = aStart;
                            for (int i = aStart; i < anEnd; i++)
                            {
                                mNodes[i] = theWord[aPos++];
                            }
                        }
                        mNodes[aNode] = (mNodes[aNode] & 0xff) | (uint)(aLink << 8);
                        mWordCount++;
                        return;
                    }
                    aNode = (int)(mNodes[aNode] >> 8);
                }
                aLeft--;
                aPos++;
                aNode++;
            }
        }

        /// <summary>
        /// FUN_0002a904: whether a word is in the dictionary. With theMatch, characters from 0xc0 also match
        /// their unaccented letter, and theMatch receives the spelling found (0-terminated).
        /// </summary>
        public bool IsWord(string theWord, byte[] theMatch = null)
        {
            if (mNodes == null || mUsed == 0)
            {
                return false;
            }
            byte[] aWord = new byte[theWord.Length + 1];
            for (int i = 0; i < theWord.Length; i++)
            {
                aWord[i] = (byte)theWord[i];
            }
            return Find(0, aWord, 0, theMatch, 0);
        }

        /// <summary>FUN_0002a854: matches theWord from thePos against the siblings starting at theNode.</summary>
        private bool Find(int theNode, byte[] theWord, int thePos, byte[] theMatch, int theMatchPos)
        {
            while (true)
            {
                uint aNodeChar = mNodes[theNode] & 0xff;
                bool aSame = aNodeChar == theWord[thePos];
                if (!aSame && theMatch != null && aNodeChar >= 0xc0)
                {
                    aSame = (byte)ACCENT_FOLD[(int)aNodeChar - 0xc0] == theWord[thePos];
                }
                if (aSame)
                {
                    if (theMatch != null)
                    {
                        theMatch[theMatchPos] = (byte)aNodeChar;
                    }
                    if (aNodeChar == 0 || Find(theNode + 1, theWord, thePos + 1, theMatch, theMatchPos + 1))
                    {
                        return true;
                    }
                }
                if ((mNodes[theNode] >> 8) == 0)
                {
                    return false;
                }
                theNode = (int)(mNodes[theNode] >> 8);
            }
        }

        /// <summary>FUN_0002a91c.</summary>
        public bool IsEmpty()
        {
            return mWordCount == 0;
        }
    }
}
