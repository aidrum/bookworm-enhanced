using System.Collections.Generic;
using System.Text;

namespace Sexy
{
    /// <summary>
    /// Sexy::WordScore (vtable 0x309c.., embedded in the Board at 0x214): the word being spelled from the
    /// selected tiles, whether the dictionary knows it, and what it scores.
    /// </summary>
    public class WordScore
    {
        /// <summary>Letter values, A to Z (0xccac0 + 'A' * 4).</summary>
        private static readonly int[] LETTER_VALUES =
        {
            1, 4, 4, 3, 1, 5, 3, 5, 1, 8, 7, 2, 4, 2, 1, 4, 7, 2, 1, 2, 2, 5, 6, 8, 6, 10
        };

        /// <summary>A letter's value in the current language (port: Spanish takes the PC Spanish version's).</summary>
        public static int GetLetterValue(char theLetter)
        {
            int[] aLetterValues = Localization.IsSpanish ? Localization.SPANISH_LETTER_VALUES : LETTER_VALUES;
            int i = theLetter - 'A';
            return i >= 0 && i < 26 ? aLetterValues[i] : 0;
        }

        /// <summary>Scoring mode 0's base by length (0xccba0); the game uses mode 1 (app + 0x11d8).</summary>
        private static readonly int[] LENGTH_SCORES = { 0, 0, 0, 50, 100, 200, 400, 600, 1000 };

        public BookWormApp mApp; // 0x04

        public Board mBoard; // 0x08

        /// <summary>The letters as spelled ('Q' adds a 'U') (0x0c).</summary>
        public string mWord = "";

        /// <summary>The dictionary's spelling of it (0x10).</summary>
        public string mMatch = "";

        public bool mValid; // 0x14

        /// <summary>Tiles of each type in the word (0x18..0x2c, by Piece.TYPE_*).</summary>
        public int[] mTypeCounts = new int[6];

        public int mTileCount; // 0x30

        public int mScore; // 0x34

        /// <summary>Scoring rules: 1 (the default) or 0 (app + 0x11d8).</summary>
        public int mScoringMode = 1;

        public WordScore(BookWormApp theApp, Board theBoard)
        {
            mApp = theApp;
            mBoard = theBoard;
        }

        /// <summary>FUN_000031d4: the word, counts and score of the tiles.</summary>
        public void Build(List<Piece> thePieces)
        {
            mWord = "";
            mMatch = "";
            mValid = false;
            System.Array.Clear(mTypeCounts, 0, mTypeCounts.Length);
            mTileCount = 0;
            mScore = 0;
            if (thePieces == null)
            {
                return;
            }
            mTileCount = thePieces.Count;
            StringBuilder aWord = new StringBuilder();
            foreach (Piece aPiece in thePieces)
            {
                int aType = System.Math.Clamp(aPiece.mType, 0, 5);
                mTypeCounts[aType]++;
                aWord.Append(aPiece.mLetter);
                if ((aPiece.mLetter | 0x20) == 'q')
                {
                    aWord.Append('U');
                }
            }
            mWord = aWord.ToString();
            if (mScoringMode == 1)
            {
                mScore = ScoreMode1();
            }
            else if (mScoringMode == 0 && mWord.Length > 2)
            {
                int aBase = LENGTH_SCORES[System.Math.Min(mWord.Length, 8)] + mBoard.mLevel;
                if (mWord.Length > 8)
                {
                    aBase = mWord.Length * 500 + aBase - 4000;
                }
                mScore = (mTypeCounts[4] * 7 + (mTypeCounts[1] * 2 | 1) + mTypeCounts[2] * 4 + mTypeCounts[5] * 10) * aBase * 10;
            }
        }

        /// <summary>
        /// FUN_000033b4: (length + 2 per bonus, 4 per gold, 7 per sapphire, 10 per diamond tile; a word all of
        /// fire tiles adds them) * (letter values + 1 + level) * 10.
        /// </summary>
        private int ScoreMode1()
        {
            int aLength = mWord.Length;
            if (aLength < 3)
            {
                return 0;
            }
            int aValues = 1;
            foreach (char c in mWord)
            {
                aValues += GetLetterValue(c);
            }
            int aMultiplier = aLength + mTypeCounts[1] * 2 + mTypeCounts[2] * 4 + mTypeCounts[4] * 7 + mTypeCounts[5] * 10;
            if (mTypeCounts[3] == mTileCount)
            {
                aMultiplier += mTypeCounts[3] - 1;
            }
            return aMultiplier * (aValues + mBoard.mLevel) * 10;
        }

        /// <summary>FUN_0000345c: valid when 3+ letters long and in the dictionary (which gives the spelling).</summary>
        public void Validate(Dictionary theDictionary)
        {
            byte[] aMatch = new byte[mWord.Length + 1];
            if (mWord.Length < 3 || theDictionary.IsEmpty() || !theDictionary.IsWord(mWord, aMatch))
            {
                mMatch = "";
                mValid = false;
                return;
            }
            int aLen = System.Array.IndexOf(aMatch, (byte)0);
            mMatch = Encoding.Latin1.GetString(aMatch, 0, aLen < 0 ? aMatch.Length : aLen);
            mValid = true;
        }
    }

    /// <summary>
    /// Sexy::StatKeeper (vtable 0x2ca0.., 0x48 bytes): totals of the words made (one for the game at Board +
    /// 0x260, one for the level at + 0x264).
    /// </summary>
    public class StatKeeper
    {
        public BookWormApp mApp; // 0x04

        public int mScore; // 0x08

        public int mWords; // 0x0c

        public int mLetters; // 0x10

        public int mTiles; // 0x14

        public int mFireTiles; // 0x18

        public int mBonusTiles; // 0x1c

        public int mGoldTiles; // 0x20

        public int mSapphireTiles; // 0x24

        public int mDiamondTiles; // 0x28

        /// <summary>Tiles burned by fire (0x2c).</summary>
        public int mField2c;

        /// <summary>Scrambles (0x30).</summary>
        public int mScrambles;

        /// <summary>Bonus words made (indexes the bonus reward table) (0x34).</summary>
        public int mBonusWords;

        public string mBestWord = ""; // 0x38

        public int mBestWordScore; // 0x3c

        public string mLongestWord = ""; // 0x40

        public int mLongestWordScore; // 0x44

        public StatKeeper(BookWormApp theApp)
        {
            mApp = theApp;
        }

        /// <summary>The level's counts start again (in 0x6e5c; the sapphire and diamond counts are kept).</summary>
        public void Reset()
        {
            mScore = mWords = mLetters = mTiles = mFireTiles = mBonusTiles = mGoldTiles = 0;
            mField2c = mScrambles = mBonusWords = 0;
            mBestWord = "";
            mBestWordScore = 0;
            mLongestWord = "";
            mLongestWordScore = 0;
        }

        /// <summary>FUN_00002dd4.</summary>
        public void AddWord(WordScore theWord)
        {
            mWords++;
            mScore += theWord.mScore;
            mLetters += theWord.mWord.Length;
            mTiles += theWord.mTileCount;
            mBonusTiles += theWord.mTypeCounts[1];
            mGoldTiles += theWord.mTypeCounts[2];
            mFireTiles += theWord.mTypeCounts[3];
            mSapphireTiles += theWord.mTypeCounts[4];
            mDiamondTiles += theWord.mTypeCounts[5];
            if (mLongestWord.Length < theWord.mWord.Length)
            {
                mLongestWord = theWord.mWord;
                mLongestWordScore = theWord.mScore;
            }
            if (theWord.mScore > mBestWordScore)
            {
                mBestWord = theWord.mWord;
                mBestWordScore = theWord.mScore;
            }
        }
    }
}
