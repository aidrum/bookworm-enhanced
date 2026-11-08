using System;
using Microsoft.Xna.Framework;

namespace Sexy
{
    /// <summary>Told which book of a shelf was tapped (BookshelfWidget_sub216 vf1).</summary>
    public interface ShelfListener
    {
        void ShelfPicked(int theId, int theIndex);
    }

    /// <summary>
    /// The touch handling CollectionShelfWidget and HistoryShelfWidget share (0x39a6c-0x39bc8 / 0x3a320-0x3a464):
    /// touching a book glows around it; moving off hides the glow and back on shows it again; letting go on it
    /// tells the listener (id, book).
    /// </summary>
    public abstract class ShelfWidget : Widget
    {
        public BookWormApp mApp; // 0x78

        public int mId; // 0x7c

        public ShelfListener mListener; // 0x80

        /// <summary>Each book's rect, in the widget.</summary>
        public TRect[] mRects;

        /// <summary>The book being touched (-1 none), and whether the touch is on it.</summary>
        public int mPressed = -1;

        public bool mInside;

        protected ShelfWidget(BookWormApp theApp, int theId, ShelfListener theListener, int theBooks)
        {
            mApp = theApp;
            mId = theId;
            mListener = theListener;
            mRects = new TRect[theBooks];
        }

        protected virtual bool CanPick(int theIndex)
        {
            return true;
        }

        /// <summary>Port addition (keyboard/controller): whether a book can be picked (MenuNavigator).</summary>
        public bool IsPickable(int theIndex)
        {
            return CanPick(theIndex);
        }

        private void Glow(int theIndex)
        {
            TPoint aPos = GetAbsPos();
            TRect aRect = mRects[theIndex];
            mApp.ShowTouchGlow(new TRect(aPos.mX + aRect.mX, aPos.mY + aRect.mY, aRect.mWidth, aRect.mHeight));
        }

        public override void MouseDown(int x, int y, int theClickCount)
        {
            base.MouseDown(x, y, theClickCount);
            for (int i = 0; i < mRects.Length; i++)
            {
                if (CanPick(i) && mRects[i].Contains(x, y))
                {
                    Glow(i);
                    mPressed = i;
                    mInside = true;
                    return;
                }
            }
        }

        public override void MouseDrag(int x, int y)
        {
            base.MouseDrag(x, y);
            if (mPressed < 0)
            {
                return;
            }
            bool anInside = mRects[mPressed].Contains(x, y);
            if (anInside && !mInside)
            {
                Glow(mPressed);
            }
            else if (!anInside && mInside)
            {
                mApp.HideTouchGlow();
            }
            mInside = anInside;
        }

        public override void MouseUp(int x, int y, int theClickCount)
        {
            base.MouseUp(x, y, theClickCount);
            mApp.HideTouchGlow();
            if (mPressed < 0)
            {
                return;
            }
            int aPressed = mPressed;
            mPressed = -1;
            if (mInside)
            {
                mInside = false;
                mListener.ShelfPicked(mId, aPressed);
            }
        }

        /// <summary>The scroller took the touch (as MoreGamesListWidget, 0x44a6e): the glow goes, nothing is picked.</summary>
        public override void TouchesCanceled()
        {
            base.TouchesCanceled();
            mApp.HideTouchGlow();
            mPressed = -1;
            mInside = false;
        }
    }

    /// <summary>
    /// Sexy::CollectionShelfWidget (0x391ac): the nine collection books, drawn once into an image (0x393c0): an
    /// opened book's spine (images 0x274d+) is filled from the left with its colour (0x2756+, or 0x275f+ when
    /// complete) by the share of words found, with its name; an unopened one is plain (IMAGE_GROUP3_13) and can't
    /// be picked. Stacked spines in landscape (states 13-15); otherwise turned upright (a quarter turn, the fill
    /// rising from the bottom).
    /// </summary>
    public class CollectionShelfWidget : ShelfWidget
    {
        public const int BOOKS = 9;

        /// <summary>
        /// The first opened spine's image id (0x274d-0x2755, all 322 x 52 at iPad Retina). 0x274c before them is
        /// the bookshelf's landscape side image (IMAGE_GROUP31_04, 52 x 486), not a spine.
        /// </summary>
        private const int SPINE_ID = 0x274d;

        /// <summary>Port: the part of a spine's width (in percent, between its end decorations) a name may use.</summary>
        private const int SPINE_TEXT_PERCENT = 80;

        /// <summary>The fill text colours by book (0x136e50); a complete book's is white (0x136d60).</summary>
        private static readonly Color[] BOOK_COLORS =
        {
            new Color(0xf0, 0xb5, 0xff), new Color(0xff, 0xf1, 0xc6), new Color(0xf4, 0xff, 0xc3),
            new Color(0xe8, 0xc8, 0xff), new Color(0xff, 0xd2, 0xbc), new Color(0xc8, 0xff, 0xa6),
            new Color(0x83, 0xff, 0x80), new Color(0xff, 0xc6, 0x00), new Color(0xc9, 0xdd, 0xff)
        };

        public Font mFont; // 0x84

        public MemoryImage mImage; // 0x88

        /// <summary>The book is opened, so it can be picked (0x11c).</summary>
        public bool[] mOpened = new bool[BOOKS];

        public CollectionShelfWidget(BookWormApp theApp, int theId, ShelfListener theListener)
            : base(theApp, theId, theListener, BOOKS)
        {
            mFont = Res.FONT_ARIAL_MEDIUM_OUTLINED;
        }

        protected override bool CanPick(int theIndex)
        {
            return mOpened[theIndex];
        }

        /// <summary>FUN_00039a04: redraws the shelf for theState and sizes the widget to it.</summary>
        public void Setup(int theState)
        {
            MakeImage(theState < 13 || theState > 15);
            Resize(mX, mY, mImage.mWidth, mImage.mHeight);
        }

        /// <summary>FUN_000393c0.</summary>
        private void MakeImage(bool theUpright)
        {
            int aWidth = 0;
            int aHeight = 10;
            for (int anId = SPINE_ID; anId < SPINE_ID + BOOKS; anId++)
            {
                Image aSpine = AtlasResourceBase.GetImageById(anId);
                aWidth = Math.Max(aWidth, aSpine.mWidth);
                aHeight += aSpine.mHeight;
            }
            MemoryImage anImage = new MemoryImage();
            anImage.Create(aWidth, aHeight);
            Graphics g = Graphics.GetNew(anImage);
            g.Clear(Color.Transparent);
            int aY = 1;
            for (int i = 0; i < BOOKS; i++)
            {
                CollectionBook aBook = mApp.mCollectionator.GetBook(i);
                int aFound = aBook != null ? aBook.mFoundCount : 0;
                int aTotal = aBook != null ? aBook.mWords.Count : 0;
                string aName = "";
                Image aSpine;
                if (aFound == 0)
                {
                    aSpine = AtlasResourceBase.IMAGE_GROUP3_13;
                    mOpened[i] = false;
                }
                else
                {
                    aName = SexyAppBase.Loc(aBook.mName).ToUpperInvariant();
                    aSpine = AtlasResourceBase.GetImageById(SPINE_ID + i);
                    mOpened[i] = true;
                }
                bool aComplete = aFound == aTotal;
                Image aFill = AtlasResourceBase.GetImageById((aComplete ? 0x275f : 0x2756) + i);
                Color aColor = aComplete ? Color.White : BOOK_COLORS[i];
                int aSpineWidth = aSpine.mWidth;
                int aSpineHeight = aSpine.mHeight;
                int aFillWidth = aTotal == 0 ? 0 : (int)(aSpineWidth * ((float)aFound / aTotal));
                if (aFillWidth > 0)
                {
                    g.DrawImage(aFill, 0, aY, new TRect(0, 0, aFillWidth, aSpineHeight));
                }
                g.DrawImage(aSpine, aFillWidth, aY, new TRect(aFillWidth, 0, aSpineWidth - aFillWidth, aSpineHeight));
                if (aName.Length != 0)
                {
                    g.SetFont(mFont);
                    g.SetColor(aColor);
                    // Port: a name too wide for the spine (the Spanish ones) is shrunk about the spine's middle to fit.
                    int aNameWidth = mFont.StringWidth(aName);
                    int aMaxWidth = aSpineWidth * SPINE_TEXT_PERCENT / 100;
                    if (aNameWidth > aMaxWidth)
                    {
                        float aScale = (float)aMaxWidth / aNameWidth;
                        g.SetScale(aScale, aScale, aSpineWidth / 2f, aY + aSpineHeight / 2f);
                    }
                    g.DrawString(aName, (aSpineWidth - aNameWidth) / 2, aY + mFont.GetAscent() + (aSpineHeight - mFont.GetHeight()) / 2);
                    g.SetScale(1f, 1f, 0f, 0f);
                }
                mRects[i] = new TRect(0, aY, aSpineWidth, aSpineHeight);
                aY += aSpineHeight + 1;
            }
            g.SetRenderTarget(null);
            g.PrepareForReuse();
            if (theUpright)
            {
                MemoryImage anUpright = new MemoryImage();
                anUpright.Create(aHeight, aWidth);
                g = Graphics.GetNew(anUpright);
                g.Clear(Color.Transparent);
                g.DrawImageRotatedF(anImage, 0, aWidth, Math.PI / 2, 0, 0);
                g.SetRenderTarget(null);
                g.PrepareForReuse();
                anImage.Dispose();
                anImage = anUpright;
                for (int i = 0; i < BOOKS; i++)
                {
                    TRect aRect = mRects[i];
                    mRects[i] = new TRect(aRect.mY, aRect.mX, aRect.mHeight, aRect.mWidth);
                }
            }
            mImage?.Dispose();
            mImage = anImage;
        }

        public override void Draw(Graphics g)
        {
            base.Draw(g);
            if (mImage != null)
            {
                g.DrawImage(mImage, 0, 0);
            }
        }
    }

    /// <summary>
    /// Sexy::HistoryShelfWidget (0x39d50): ten shelves (IMAGE_GROUP7_01), one a word length 3..12 (label cel of
    /// IMAGE_GROUP31_01), with books standing for how many words of that length were made (images 0x276c+ worth
    /// 10000, 5000, 1000, 500, 100, 50, 10, 5, 1) and the count in the gold score font.
    /// </summary>
    public class HistoryShelfWidget : ShelfWidget
    {
        /// <summary>What each book image is worth (0xef950).</summary>
        private static readonly int[] BOOK_VALUES = { 10000, 5000, 1000, 500, 100, 50, 10, 5, 1 };

        public Image[] mBookImages = new Image[BOOK_VALUES.Length]; // 0x124

        public HistoryShelfWidget(BookWormApp theApp, int theId, ShelfListener theListener)
            : base(theApp, theId, theListener, Collectionator.HISTORY_LENGTHS)
        {
            for (int i = 0; i < mBookImages.Length; i++)
            {
                mBookImages[i] = AtlasResourceBase.GetImageById(0x276c + i);
            }
        }

        /// <summary>FUN_00039f88: a shelf a length, one under the other.</summary>
        public void Setup(int theState)
        {
            Image aShelf = AtlasResourceBase.IMAGE_GROUP7_01;
            for (int i = 0; i < mRects.Length; i++)
            {
                mRects[i] = new TRect(0, aShelf.mHeight * i, aShelf.mWidth, aShelf.mHeight);
            }
            Resize(0, 0, aShelf.mWidth, aShelf.mHeight * mRects.Length);
        }

        /// <summary>
        /// The labels' art (IMAGE_GROUP31_01's cels, iPad Retina rows of 192): the number above row NUMBER_ROWS,
        /// "letter words" below; "letras" goes where those were, in their gold (white to yellow).
        /// </summary>
        private const int LABEL_ROWS = 192;

        private const int NUMBER_ROWS = 102;

        private const int LETRAS_BASELINE = 144;

        private const float LETRAS_SCALE = 0.7f;

        private static readonly Color LETRAS_TOP = new Color(255, 248, 190, 255);

        private static readonly Color LETRAS_BOTTOM = new Color(240, 200, 10, 255);

        /// <summary>
        /// The shelf's label, cel theIndex of IMAGE_GROUP31_01 ("3 letter words" to "12 letter words"). Port: in
        /// another language, only the cel's number, with its word ("letras") drawn under it.
        /// </summary>
        private static void DrawLengthLabel(Graphics g, int theX, int theY, int theIndex)
        {
            Image anImage = AtlasResourceBase.IMAGE_GROUP31_01;
            if (!Localization.IsTranslated)
            {
                g.DrawImageCel(anImage, theX, theY, theIndex);
                return;
            }
            int aCelWidth = anImage.GetCelWidth();
            int aCelHeight = anImage.GetCelHeight();
            MemoryImage aLabels = GetTranslatedLabels(anImage);
            if (aLabels == null)
            {
                // Inside an offscreen render the strip cannot be built: drawn piece by piece.
                DrawTranslatedLabel(g, anImage, theX, theY, theIndex);
                return;
            }
            g.DrawImage(aLabels, theX, theY, new TRect(theIndex * aCelWidth, 0, aCelWidth, aCelHeight));
        }

        /// <summary>Port (translated): a label's number cut from the art, "letras" drawn under it.</summary>
        private static void DrawTranslatedLabel(Graphics g, Image theArt, int theX, int theY, int theIndex)
        {
            int aCelWidth = theArt.GetCelWidth();
            int aCelHeight = theArt.GetCelHeight();
            g.DrawImage(theArt, theX, theY, new TRect(theIndex * aCelWidth, 0, aCelWidth, aCelHeight * NUMBER_ROWS / LABEL_ROWS));
            // The language's word ("letras"), shrunk to the cel when longer (Buchstaben, bokstäver).
            string aWord = SexyAppBase.Loc("letters");
            float aScale = System.Math.Min(LETRAS_SCALE, aCelWidth * 0.9f / System.Math.Max(1, Res.FONT_ARIAL_MEDIUM_OUTLINED.StringWidth(aWord)));
            GradientText.Draw(g, Res.FONT_ARIAL_MEDIUM_OUTLINED, new[] { aWord }, theX + aCelWidth / 2, theY + aCelHeight * LETRAS_BASELINE / LABEL_ROWS,
                LETRAS_TOP, LETRAS_BOTTOM, null, 0, aScale);
        }

        /// <summary>Port (performance, translated): every label ("3 letras" to "12 letras") drawn once into one strip
        /// laid out as the art's cels, then each blitted from it; built again for other art (another texture set)
        /// or another language.</summary>
        private static MemoryImage sTranslatedLabels;

        private static Image sTranslatedLabelsArt;

        /// <summary>The language the strip was drawn in.</summary>
        private static Constants.LanguageIndex sTranslatedLabelsLanguage;

        private static MemoryImage GetTranslatedLabels(Image theArt)
        {
            if (sTranslatedLabels != null && ReferenceEquals(sTranslatedLabelsArt, theArt) && sTranslatedLabelsLanguage == Constants.Language)
            {
                return sTranslatedLabels;
            }
            if (Graphics.IsOffscreen)
            {
                return null;
            }
            sTranslatedLabels?.Dispose();
            int aCels = theArt.mNumCols;
            MemoryImage aStrip = new MemoryImage();
            aStrip.Create(theArt.GetCelWidth() * aCels, theArt.GetCelHeight());
            Graphics g = Graphics.GetNew(aStrip);
            g.Clear(Color.Transparent);
            for (int i = 0; i < aCels; i++)
            {
                DrawTranslatedLabel(g, theArt, i * theArt.GetCelWidth(), 0, i);
            }
            g.SetRenderTarget(null);
            g.PrepareForReuse();
            sTranslatedLabels = aStrip;
            sTranslatedLabelsArt = theArt;
            sTranslatedLabelsLanguage = Constants.Language;
            return aStrip;
        }

        /// <summary>0x3a044.</summary>
        public override void Draw(Graphics g)
        {
            base.Draw(g);
            ScreenPos aPos = mApp.mScreenPos;
            Font aFont = Res.FONT_GOLDSCORE;
            for (int i = 0; i < mRects.Length; i++)
            {
                TRect aRect = mRects[i];
                g.DrawImage(AtlasResourceBase.IMAGE_GROUP7_01, aRect.mX, aRect.mY);
                DrawLengthLabel(g, aPos.At(0xa8) + aRect.mX, aPos.At(0xac) + aRect.mY, i);
                int aCount = mApp.mCollectionator.mWordCounts[i];
                int aX = aPos.At(0xb0);
                int aBase = aPos.At(0xb4);
                int aBook = 0;
                while (aCount > 0)
                {
                    while (aCount < BOOK_VALUES[aBook])
                    {
                        aBook++;
                    }
                    Image anImage = mBookImages[aBook];
                    g.DrawImage(anImage, aRect.mX + aX, aRect.mY + aBase - anImage.mHeight);
                    aCount -= BOOK_VALUES[aBook];
                    aX += anImage.mWidth + 1;
                }
                string aText = mApp.mCollectionator.mWordCounts[i].ToString();
                g.SetFont(aFont);
                g.SetColorizeImages(false);
                g.SetColor(Color.White);
                g.DrawString(aText, aRect.mX + (mWidth - aFont.StringWidth(aText)) / 2, aRect.mY + (aRect.mHeight + aFont.GetAscent()) / 2);
            }
        }
    }
}
