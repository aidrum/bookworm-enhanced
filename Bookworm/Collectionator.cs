using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
namespace Sexy
{
    /// <summary>Sexy::CollectionWord (16 bytes): the forms of one collectable word ("FIG", "FIGS").</summary>
    public class CollectionWord
    {
        public List<string> mForms = new List<string>(); // 0x00

        public bool mFound; // 0x0c
    }

    /// <summary>Sexy::CollectionBook (0x18 bytes): one word collection from books/.</summary>
    public class CollectionBook
    {
        public string mName = ""; // 0x00

        public int mField4; // 0x04

        public List<CollectionWord> mWords = new List<CollectionWord>(); // 0x08

        /// <summary>How many of the words have been found (0x14).</summary>
        public int mFoundCount; // 0x14
    }

    /// <summary>
    /// Sexy::Collectionator (0x2a938, BookWormApp + 0xdc8): the word collections ("Bonus Book List") and the
    /// word history (words made by length, the newest first). Saved in the storage folder: books/&lt;book&gt; lists
    /// the found words of each book, "histogram" a line per length 3..12: the count, then the history.
    /// </summary>
    public class Collectionator
    {
        /// <summary>Words keep at most MAX_WORD_LENGTH + 1 letters (the original stops storing past 12).</summary>
        private const int MAX_WORD_LENGTH = 12;

        public BookWormApp mApp; // 0x04

        public string mBooksDir = ""; // 0x08

        public List<CollectionBook> mBooks = new List<CollectionBook>(); // 0x0c

        /// <summary>The next book LoadNextBook reads (0x18).</summary>
        public int mNextBookToLoad;

        /// <summary>Words of each length 3..12 made (0x1c).</summary>
        public int[] mWordCounts = new int[HISTORY_LENGTHS];

        /// <summary>The words made of each length 3..12, newest first, at most HISTORY_SIZE (deques at 0x44).</summary>
        public List<string>[] mHistory = new List<string>[HISTORY_LENGTHS];

        /// <summary>The book of the last collection word made, shown by the collection page (0x1d4).</summary>
        public int mCurrentBook;

        /// <summary>The word length shown by the word history page, 0-9 (0x1d8).</summary>
        public int mHistoryTab;

        /// <summary>
        /// The landscape page last shown (0x1dc; FUN_0002cf14 / FUN_0002cf1c): 0 the bookshelf (state 13), 1 a
        /// collection (14), 2 the word history (15). Rotating to landscape returns to it.
        /// </summary>
        public int mLandscapeView;

        public const int HISTORY_LENGTHS = 10;

        public const int HISTORY_SIZE = 0xb2;

        /// <summary><see cref="RecordWord"/>: not in a book, in a book but found before, newly found.</summary>
        public const int RECORD_NONE = 0;

        public const int RECORD_FOUND = 1;

        public const int RECORD_NEW = 2;

        public Collectionator(BookWormApp theApp)
        {
            mApp = theApp;
            for (int i = 0; i < HISTORY_LENGTHS; i++)
            {
                mHistory[i] = new List<string>();
            }
        }

        /// <summary>FUN_0002cc00: the book at theIndex, or null.</summary>
        public CollectionBook GetBook(int theIndex)
        {
            return theIndex >= 0 && theIndex < mBooks.Count ? mBooks[theIndex] : null;
        }

        /// <summary>FUN_0002cec8: the history of words of theIndex + 3 letters (0-9).</summary>
        public List<string> GetHistory(int theIndex)
        {
            return mHistory[theIndex];
        }

        /// <summary>
        /// FUN_0002cc2c: a word was made. Words of 3 to 12 letters are counted and go to the front of their
        /// history (moved there if already in it; the oldest drops off a full one). Then the books: the first
        /// book with the word becomes the current one; returns RECORD_NEW if the word was not found yet (it is
        /// now, and becomes the book's latest), RECORD_FOUND if it was, RECORD_NONE if no book has it.
        /// </summary>
        public int RecordWord(string theWord)
        {
            int aLength = theWord.Length - 3;
            if (aLength >= 0 && aLength < HISTORY_LENGTHS)
            {
                mWordCounts[aLength]++;
                List<string> aHistory = mHistory[aLength];
                if (!aHistory.Remove(theWord) && aHistory.Count == HISTORY_SIZE)
                {
                    aHistory.RemoveAt(aHistory.Count - 1);
                }
                aHistory.Insert(0, theWord);
            }
            for (int aBookIdx = 0; aBookIdx < mBooks.Count; aBookIdx++)
            {
                CollectionBook aBook = mBooks[aBookIdx];
                for (int i = 0; i < aBook.mWords.Count; i++)
                {
                    CollectionWord aWord = aBook.mWords[i];
                    if (aWord.mForms.Contains(theWord))
                    {
                        mCurrentBook = aBookIdx;
                        if (aWord.mFound)
                        {
                            return RECORD_FOUND;
                        }
                        aBook.mField4 = i;
                        aBook.mFoundCount++;
                        aWord.mFound = true;
                        return RECORD_NEW;
                    }
                }
            }
            return RECORD_NONE;
        }

        private string GetStoragePath(string theName)
        {
            return Path.Combine(mApp.applicationStoragePath ?? AppContext.BaseDirectory, theName);
        }

        /// <summary>
        /// Port: the storage folder of the books' found words. Each language has its own books (Spanish ones in
        /// es.lproj/books), so its own folder: "books" for English as in the original, "books_&lt;lang&gt;" otherwise.
        /// </summary>
        private static string GetBooksFolder()
        {
            return Localization.IsSpanish ? "books_" + Constants.Language : "books";
        }

        /// <summary>
        /// Port: the language changed (Save was called before). Reads the books again (ContentFS finds the
        /// new language's) and the words found in them.
        /// </summary>
        public void ReloadBooks()
        {
            mBooks.Clear();
            CountBooks(mBooksDir);
            while (LoadNextBook())
            {
            }
            Load();
        }

        /// <summary>FUN_0002ba78: each book with found words gets books/&lt;name&gt; (their first forms, one a
        /// line); then the histogram.</summary>
        public void Save()
        {
            try
            {
                string aDir = GetStoragePath(GetBooksFolder());
                Directory.CreateDirectory(aDir);
                foreach (CollectionBook aBook in mBooks)
                {
                    if (aBook.mFoundCount == 0)
                    {
                        continue;
                    }
                    StringBuilder aText = new StringBuilder();
                    foreach (CollectionWord aWord in aBook.mWords)
                    {
                        if (aWord.mFound && aWord.mForms.Count > 0)
                        {
                            aText.Append(aWord.mForms[0]).Append('\n');
                        }
                    }
                    File.WriteAllText(Path.Combine(aDir, aBook.mName), aText.ToString());
                }
                StringBuilder aHistogram = new StringBuilder();
                for (int i = 0; i < HISTORY_LENGTHS; i++)
                {
                    aHistogram.Append(mWordCounts[i]);
                    foreach (string aWord in mHistory[i])
                    {
                        aHistogram.Append(' ').Append(aWord);
                    }
                    aHistogram.Append('\n');
                }
                File.WriteAllText(GetStoragePath("histogram"), aHistogram.ToString());
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                Console.Error.WriteLine("Could not save the collections: " + e.Message);
            }
        }

        /// <summary>FUN_0002c108 (with 0x2c8a0, 0x2c944): marks the saved words found (lines trimmed and
        /// upper-cased, matched with each word's first form), then reads the histogram.</summary>
        public void Load()
        {
            try
            {
                foreach (CollectionBook aBook in mBooks)
                {
                    aBook.mFoundCount = 0;
                    foreach (CollectionWord aWord in aBook.mWords)
                    {
                        aWord.mFound = false;
                    }
                    string aPath = Path.Combine(GetStoragePath(GetBooksFolder()), aBook.mName);
                    if (!File.Exists(aPath))
                    {
                        continue;
                    }
                    foreach (string aLine in File.ReadAllLines(aPath))
                    {
                        string aSaved = aLine.TrimEnd().ToUpperInvariant();
                        foreach (CollectionWord aWord in aBook.mWords)
                        {
                            if (aWord.mForms.Count > 0 && aWord.mForms[0] == aSaved)
                            {
                                aWord.mFound = true;
                                aBook.mFoundCount++;
                            }
                        }
                    }
                }
                for (int i = 0; i < HISTORY_LENGTHS; i++)
                {
                    mWordCounts[i] = 0;
                    mHistory[i].Clear();
                }
                string aHistogramPath = GetStoragePath("histogram");
                if (!File.Exists(aHistogramPath))
                {
                    return;
                }
                string[] aLines = File.ReadAllLines(aHistogramPath);
                for (int i = 0; i < HISTORY_LENGTHS && i < aLines.Length; i++)
                {
                    string[] aParts = aLines[i].Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                    if (aParts.Length == 0)
                    {
                        continue;
                    }
                    int.TryParse(aParts[0], out mWordCounts[i]);
                    for (int j = 1; j < aParts.Length; j++)
                    {
                        mHistory[i].Add(aParts[j].Length > MAX_WORD_LENGTH ? aParts[j].Substring(0, MAX_WORD_LENGTH) : aParts[j]);
                    }
                }
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                Console.Error.WriteLine("Could not load the collections: " + e.Message);
            }
        }

        /// <summary>FUN_0002ce74: the share of all the books' words found, in percent.</summary>
        public int GetCompletionPercent()
        {
            int aFound = 0;
            int aTotal = 0;
            foreach (CollectionBook aBook in mBooks)
            {
                aFound += aBook.mFoundCount;
                aTotal += aBook.mWords.Count;
            }
            return aTotal == 0 ? 0 : aFound * 100 / aTotal;
        }

        /// <summary>FUN_0002b060: lists the books (the files in theDir) and returns how many there are.</summary>
        public int CountBooks(string theDir)
        {
            foreach (string aName in ContentFS.ListFiles(theDir))
            {
                CollectionBook aBook = new CollectionBook();
                aBook.mName = aName;
                mBooks.Add(aBook);
            }
            mBooksDir = theDir;
            mNextBookToLoad = 0;
            return mBooks.Count;
        }

        /// <summary>
        /// FUN_0002b3c4: reads the next book. Each line is one CollectionWord, its forms separated by spaces;
        /// only letters are kept, upper-cased. Returns false once every book is loaded.
        /// </summary>
        public bool LoadNextBook()
        {
            if (mNextBookToLoad >= mBooks.Count)
            {
                return false;
            }
            CollectionBook aBook = mBooks[mNextBookToLoad++];
            byte[] aData = ContentFS.ReadAllBytes(mBooksDir + "/" + aBook.mName);
            if (aData == null)
            {
                return true;
            }
            List<string> aForms = new List<string>();
            char[] aWord = new char[MAX_WORD_LENGTH + 1];
            int aLen = 0;
            foreach (byte b in aData)
            {
                char c = (char)b;
                bool aSpace = c == ' ' || (c >= '\t' && c <= '\r');
                if (!aSpace)
                {
                    // Only letters count; past 13 letters the rest of the word is dropped.
                    if (char.IsLetter(c) && aLen <= MAX_WORD_LENGTH)
                    {
                        aWord[aLen++] = char.ToUpperInvariant(c);
                    }
                    continue;
                }
                if (aLen != 0)
                {
                    aForms.Add(new string(aWord, 0, aLen));
                }
                aLen = 0;
                if (c != ' ' && c != '\t' && aForms.Count != 0)
                {
                    // A line end closes the word.
                    CollectionWord aCollectionWord = new CollectionWord();
                    aCollectionWord.mForms.AddRange(aForms);
                    aBook.mWords.Add(aCollectionWord);
                    aForms.Clear();
                }
            }
            return true;
        }
    }
}
