using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Sexy
{
    /// <summary>
    /// Sexy::FancyTextWidget (0x3a97c): text laid out as chunks, each with its own font and colour, word
    /// wrapped across one or more columns and justified per line. WordBalloon and the definitions use it.
    /// </summary>
    public class FancyTextWidget : Widget
    {
        /// <summary>Sexy::FancyTextWidget::Chunk (0x28 bytes).</summary>
        public class Chunk
        {
            public int mX; // 0x00

            public int mY; // 0x04

            public int mWidth; // 0x08

            public int mHeight; // 0x0c

            public Font mFont; // 0x10

            public Color mColor; // 0x14

            public string mText = ""; // 0x24
        }

        /// <summary>The text colour after Clear (0x10cf6c).</summary>
        private static readonly Color DEFAULT_COLOR = new Color(0, 0, 0, 255);

        public List<Chunk> mChunks = new List<Chunk>(); // 0x78

        /// <summary>The first chunk of the line being built (0x84).</summary>
        public int mLineStart;

        /// <summary>0 centred, 1 right, else left (0x88).</summary>
        public int mJustify = -1;

        public Color mColor; // 0x8c

        public Font mFont; // 0x9c

        public int mColumn; // 0xa0

        public int mColumns = 1; // 0xa4

        /// <summary>Where the current set of columns starts (0xa8).</summary>
        public int mColumnTop;

        /// <summary>The bottom of everything laid out (0xac).</summary>
        public int mTextHeight;

        /// <summary>The current line's top (0xb0).</summary>
        public int mLineY;

        /// <summary>The current line's baseline (0xb4).</summary>
        public int mBaseline;

        /// <summary>The current line's width so far (0xb8).</summary>
        public int mLineWidth;

        public FancyTextWidget()
        {
            mColor = DEFAULT_COLOR;
        }

        /// <summary>FUN_0003ab68: empties the text and resets the style.</summary>
        public void Clear()
        {
            mJustify = -1;
            mColor = DEFAULT_COLOR;
            mFont = null;
            mColumn = 0;
            mColumns = 1;
            mLineStart = 0;
            mLineWidth = 0;
            mBaseline = 0;
            mLineY = 0;
            mTextHeight = 0;
            mColumnTop = 0;
            mChunks.Clear();
        }

        /// <summary>FUN_0003add0: continues in column theColumn of theColumns (a new set of columns starts below the text).</summary>
        public void SetColumn(int theColumn, int theColumns)
        {
            if (mColumns == theColumns)
            {
                mLineY = mColumnTop;
            }
            else
            {
                mColumnTop = mTextHeight;
            }
            mBaseline = mLineY;
            mColumn = theColumn;
            mColumns = theColumns;
        }

        /// <summary>FUN_0003adfc.</summary>
        public void SetJustify(int theJustify)
        {
            mJustify = theJustify;
        }

        /// <summary>FUN_0003ae04.</summary>
        public void SetColor(Color theColor)
        {
            mColor = theColor;
        }

        /// <summary>FUN_0003ae18.</summary>
        public void SetFont(Font theFont)
        {
            mFont = theFont;
        }

        /// <summary>
        /// FUN_0003ae20: adds text, wrapping at spaces to the column width (a new line gets theLineSpacing
        /// more). "\n" breaks the line; "^^" is a literal '^' and '^' otherwise leaves the next character to
        /// the font (a colour code). The last line is left open. Returns the range of chunks added.
        /// </summary>
        public (int, int) AddText(string theText, int theLineSpacing)
        {
            int aFirst = mChunks.Count;
            int aMaxWidth = mWidth / mColumns;
            int aStart = 0;
            int aBreak = 0;
            int aBreakWidth = 0;
            int aWidth = 0;
            char aPrevChar = '\0';
            int i = 0;
            while (true)
            {
                if (i >= theText.Length)
                {
                    AddChunk(theText.Substring(aStart));
                    return (aFirst, mChunks.Count + 1);
                }
                char c = theText[i++];
                int aWidthBefore = aWidth;
                if (c == '\n')
                {
                    if (aStart < i)
                    {
                        string aLine = theText.Substring(aStart, i - 1 - aStart);
                        if (aLine.Length != 0)
                        {
                            AddChunk(aLine);
                        }
                        NewLine(theLineSpacing);
                    }
                    aWidthBefore = 0;
                    aStart = i;
                    aBreak = i;
                }
                else
                {
                    if (c == '^')
                    {
                        if (i >= theText.Length)
                        {
                            continue;
                        }
                        c = theText[i++];
                    }
                    if (char.IsWhiteSpace(c))
                    {
                        aBreak = i;
                        aBreakWidth = aWidth;
                    }
                }
                aWidth = aWidthBefore + mFont.CharWidthKern(c, aPrevChar);
                aPrevChar = c;
                if (aMaxWidth < mLineWidth + aWidth && aStart < aBreak)
                {
                    AddChunk(theText.Substring(aStart, aBreak - aStart).TrimEnd());
                    NewLine(theLineSpacing);
                    aWidth -= aBreakWidth;
                    aStart = aBreak;
                }
            }
        }

        /// <summary>FUN_0003b2d0: one chunk in the current font and colour, on the current line.</summary>
        public void AddChunk(string theText)
        {
            Chunk aChunk = new Chunk();
            aChunk.mWidth = mFont.StringWidth(theText);
            aChunk.mHeight = mFont.GetHeight();
            aChunk.mFont = mFont;
            aChunk.mColor = mColor;
            aChunk.mText = theText;
            mChunks.Add(aChunk);
            mLineWidth += aChunk.mWidth;
            mBaseline = System.Math.Max(mBaseline, mFont.GetAscent() + mLineY);
        }

        /// <summary>
        /// FUN_0003b4fc: places the current line's chunks (justified in their column, on one baseline) and
        /// moves down by the tallest line spacing plus theExtraSpacing.
        /// </summary>
        public void NewLine(int theExtraSpacing)
        {
            int aColumnWidth = mWidth / mColumns;
            int aX = mColumn * aColumnWidth;
            if (mJustify == 0)
            {
                aX += (aColumnWidth - mLineWidth) / 2;
            }
            else if (mJustify == 1)
            {
                aX = aColumnWidth + aX - mLineWidth;
            }
            int aLineSpacing = 0;
            for (int i = mLineStart; i < mChunks.Count; i++)
            {
                Chunk aChunk = mChunks[i];
                aChunk.mX = aX;
                aChunk.mY = mBaseline - aChunk.mFont.GetAscent();
                aLineSpacing = System.Math.Max(aLineSpacing, aChunk.mFont.GetLineSpacing());
                aX += aChunk.mWidth;
            }
            if (aLineSpacing == 0)
            {
                aLineSpacing = mFont.GetLineSpacing();
            }
            mLineStart = mChunks.Count;
            mLineY += theExtraSpacing + aLineSpacing;
            mTextHeight = System.Math.Max(mTextHeight, mLineY);
            mLineWidth = 0;
        }

        /// <summary>
        /// FUN_0003b624: 0 sizes the widget to the text; 2 centres the text vertically. Returns whether the
        /// text fits the widget's height (all that 1 does).
        /// </summary>
        public bool Fit(int theMode)
        {
            if (theMode == 0)
            {
                Resize(mX, mY, mWidth, mTextHeight);
                return true;
            }
            if (theMode == 2)
            {
                foreach (Chunk aChunk in mChunks)
                {
                    aChunk.mY += (mHeight - mTextHeight) / 2;
                }
            }
            return mTextHeight <= mHeight;
        }

        /// <summary>0x3b6b8.</summary>
        public override void Draw(Graphics g)
        {
            foreach (Chunk aChunk in mChunks)
            {
                g.SetFont(aChunk.mFont);
                g.SetColor(aChunk.mColor);
                g.DrawString(aChunk.mText, aChunk.mX, aChunk.mFont.GetAscent() + aChunk.mY);
            }
        }
    }
}
