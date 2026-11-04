using System.Collections.Generic;
using System.IO;

namespace Sexy
{
    /// <summary>
    /// Bookworm's personal bests (class at 0x3bdb0, BookWormApp + 0xdcc; no RTTI): per game mode the high score,
    /// the highest rank, the longest word and the best-scoring word. Saved in the documents folder as "best".
    /// </summary>
    public class PersonalBests
    {
        /// <summary>One mode's bests (0x18 bytes from + 4).</summary>
        public class Record
        {
            public int mHighScore; // 0x00

            public int mHighestRank; // 0x04

            public int mLongestWordScore; // 0x08

            public int mBestWordScore; // 0x0c

            public string mLongestWord = ""; // 0x10

            public string mBestWord = ""; // 0x14
        }

        public BookWormApp mApp; // 0x00

        public Record[] mRecords = { new Record(), new Record() };

        public PersonalBests(BookWormApp theApp)
        {
            mApp = theApp;
        }

        /// <summary>FUN_0003be64.</summary>
        public Record GetRecord(int theMode)
        {
            return mRecords[theMode];
        }

        /// <summary>FUN_0003bde0.</summary>
        public void UpdateHighScore(int theMode, int theScore)
        {
            if (mRecords[theMode].mHighScore < theScore)
            {
                mRecords[theMode].mHighScore = theScore;
            }
        }

        /// <summary>FUN_0003be50.</summary>
        public void UpdateHighestRank(int theMode, int theRank)
        {
            if (mRecords[theMode].mHighestRank < theRank)
            {
                mRecords[theMode].mHighestRank = theRank;
            }
        }

        /// <summary>FUN_0003bdf4: a word made: is it the longest, or the best scoring?</summary>
        public void UpdateWord(int theMode, string theWord, int theScore)
        {
            Record aRecord = mRecords[theMode];
            if (aRecord.mLongestWord.Length < theWord.Length)
            {
                aRecord.mLongestWord = theWord;
                aRecord.mLongestWordScore = theScore;
            }
            if (aRecord.mBestWordScore < theScore)
            {
                aRecord.mBestWord = theWord;
                aRecord.mBestWordScore = theScore;
            }
        }

        private string GetPath()
        {
            return Path.Combine(mApp.applicationStoragePath ?? System.AppContext.BaseDirectory, "best");
        }

        /// <summary>FUN_0003be70: version byte 1, then per mode the scores and words.</summary>
        public void Save()
        {
            Buffer aBuffer = new Buffer();
            aBuffer.WriteByte(1);
            foreach (Record aRecord in mRecords)
            {
                aBuffer.WriteLong(aRecord.mHighScore);
                aBuffer.WriteLong(aRecord.mHighestRank);
                aBuffer.WriteLong(aRecord.mLongestWordScore);
                aBuffer.WriteString(aRecord.mLongestWord);
                aBuffer.WriteLong(aRecord.mBestWordScore);
                aBuffer.WriteString(aRecord.mBestWord);
            }
            try
            {
                File.WriteAllBytes(GetPath(), aBuffer.GetDataPtr());
            }
            catch (IOException e)
            {
                System.Console.Error.WriteLine("Could not save personal bests: " + e.Message);
            }
        }

        /// <summary>FUN_0003c180.</summary>
        public void Load()
        {
            mRecords = new[] { new Record(), new Record() };
            string aPath = GetPath();
            if (!File.Exists(aPath))
            {
                return;
            }
            Buffer aBuffer = new Buffer();
            aBuffer.SetData(new List<byte>(File.ReadAllBytes(aPath)));
            if (aBuffer.ReadByte() <= 0)
            {
                return;
            }
            foreach (Record aRecord in mRecords)
            {
                aRecord.mHighScore = aBuffer.ReadLong();
                aRecord.mHighestRank = aBuffer.ReadLong();
                aRecord.mLongestWordScore = aBuffer.ReadLong();
                aRecord.mLongestWord = aBuffer.ReadString();
                aRecord.mBestWordScore = aBuffer.ReadLong();
                aRecord.mBestWord = aBuffer.ReadString();
            }
        }
    }
}
