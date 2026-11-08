using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Sexy
{
    /// <summary>
    /// Port addition: keyboard and controller play. A selector moves over the grid: sideways it keeps its height
    /// (the columns are staggered by half a tile, so it may sit between two tiles of the next column), up and down
    /// it snaps to the tile above or below. It shows on the first key or pad input and hides on mouse or touch
    /// input (BookWormApp.PointerInputUsed).
    /// Keyboard: arrows/WASD move, Space/Enter taps, Backspace drops the last letter, X drops the word, Z scrambles.
    /// Pad: D-pad/left stick move, A taps, B drops the last letter, X drops the word, Y scrambles.
    /// </summary>
    public partial class Board
    {
        /// <summary>Whether the selector shows (and takes input).</summary>
        public bool mSelectorVisible;

        /// <summary>The selector's column.</summary>
        private int mSelectorCol = COLS / 2;

        /// <summary>
        /// The selector's height in half tiles: a tile of an even column (half a tile higher) is at 2 * row - 1,
        /// one of an odd column at 2 * row, so -1 (top) to 2 * (ROWS - 1) (bottom).
        /// </summary>
        private int mSelectorHalf = (ROWS / 2) * 2;

        private const int SELECTOR_MIN_HALF = -1;

        private const int SELECTOR_MAX_HALF = (ROWS - 1) * 2;

        /// <summary>
        /// Escape, Start, Back (and Android's back button) work the MENU button while the board is up with no
        /// dialog over it.
        /// </summary>
        public override bool BackButtonPress()
        {
            if (mApp.mInterfaceState != 10 || mDisabled || mWidgetManager == null || mWidgetManager.mBaseModalWidget != null ||
                mHintShowing || !CanSaveGame())
            {
                return false;
            }
            ButtonPress(BUTTON_MENU);
            ButtonDepress(BUTTON_MENU);
            return true;
        }

        /// <summary>The pad direction held last (0 none, else DIR_*) and when it repeats (ms).</summary>
        private int mPadDir;

        private long mPadRepeatAt;

        private const int DIR_LEFT = 1;

        private const int DIR_RIGHT = 2;

        private const int DIR_UP = 3;

        private const int DIR_DOWN = 4;

        private const int PAD_REPEAT_DELAY = 400;

        private const int PAD_REPEAT_RATE = 130;

        private const float PAD_STICK_THRESHOLD = 0.5f;

        /// <summary>The selector art per art scale (images/selector.png, R, iPadR).</summary>
        private static readonly System.Collections.Generic.Dictionary<int, Image> mSelectorImages =
            new System.Collections.Generic.Dictionary<int, Image>();

        /// <summary>Whether play takes the keys and the pad: the board up and playing, with no dialog over it.</summary>
        internal bool CanUseSelector()
        {
            return mState == STATE_PLAYING && mPauseFlags == 0 && !mDisabled && mWidgetManager != null &&
                mWidgetManager.mBaseModalWidget == null && mApp.mInterfaceState == 10;
        }

        public override void KeyDown(KeyCode theKey)
        {
            if (!CanUseSelector())
            {
                base.KeyDown(theKey);
                return;
            }
            switch (theKey)
            {
            case KeyCode.Left:
            case (KeyCode)'A':
                SelectorMove(DIR_LEFT);
                return;
            case KeyCode.Right:
            case (KeyCode)'D':
                SelectorMove(DIR_RIGHT);
                return;
            case KeyCode.Up:
            case (KeyCode)'W':
                SelectorMove(DIR_UP);
                return;
            case KeyCode.Down:
            case (KeyCode)'S':
                SelectorMove(DIR_DOWN);
                return;
            case KeyCode.Space:
            case KeyCode.Return:
                SelectorTap();
                return;
            case KeyCode.Back:
                SelectorDropLast();
                return;
            case (KeyCode)'X':
                SelectorDropWord();
                return;
            case (KeyCode)'Z':
                ShowSelector();
                RequestScramble();
                return;
            }
            base.KeyDown(theKey);
        }

        /// <summary>The pad's turn (BookWormApp.GamePadUpdate): false when play doesn't take it.</summary>
        public bool GamePadUpdate(GamePadState thePad, GamePadState thePrevPad)
        {
            if (!thePad.IsConnected || !CanUseSelector())
            {
                mPadDir = 0;
                return false;
            }
            int aDir = GetPadDir(thePad);
            long aNow = System.Environment.TickCount64;
            if (aDir == 0)
            {
                mPadDir = 0;
            }
            else if (aDir != mPadDir)
            {
                mPadDir = aDir;
                mPadRepeatAt = aNow + PAD_REPEAT_DELAY;
                SelectorMove(aDir);
            }
            else if (aNow >= mPadRepeatAt)
            {
                mPadRepeatAt = aNow + PAD_REPEAT_RATE;
                SelectorMove(aDir);
            }
            if (PadPressed(thePad.Buttons.A, thePrevPad.Buttons.A))
            {
                SelectorTap();
            }
            if (PadPressed(thePad.Buttons.B, thePrevPad.Buttons.B))
            {
                SelectorDropLast();
            }
            if (PadPressed(thePad.Buttons.X, thePrevPad.Buttons.X))
            {
                SelectorDropWord();
            }
            if (PadPressed(thePad.Buttons.Y, thePrevPad.Buttons.Y))
            {
                ShowSelector();
                RequestScramble();
            }
            return true;
        }

        private static bool PadPressed(ButtonState theNow, ButtonState thePrev)
        {
            return theNow == ButtonState.Pressed && thePrev == ButtonState.Released;
        }

        /// <summary>The D-pad's direction, else the left stick's (its larger axis, past the threshold).</summary>
        private static int GetPadDir(GamePadState thePad)
        {
            if (thePad.DPad.Left == ButtonState.Pressed)
            {
                return DIR_LEFT;
            }
            if (thePad.DPad.Right == ButtonState.Pressed)
            {
                return DIR_RIGHT;
            }
            if (thePad.DPad.Up == ButtonState.Pressed)
            {
                return DIR_UP;
            }
            if (thePad.DPad.Down == ButtonState.Pressed)
            {
                return DIR_DOWN;
            }
            Vector2 aStick = thePad.ThumbSticks.Left;
            if (System.Math.Max(System.Math.Abs(aStick.X), System.Math.Abs(aStick.Y)) < PAD_STICK_THRESHOLD)
            {
                return 0;
            }
            if (System.Math.Abs(aStick.X) >= System.Math.Abs(aStick.Y))
            {
                return aStick.X < 0 ? DIR_LEFT : DIR_RIGHT;
            }
            return aStick.Y > 0 ? DIR_UP : DIR_DOWN;
        }

        /// <summary>Shows the selector (on the word's last tile, if any); false when it already showed.</summary>
        private bool ShowSelector()
        {
            if (mSelectorVisible)
            {
                return false;
            }
            mSelectorVisible = true;
            SnapSelectorToLast();
            MarkDirty();
            return true;
        }

        /// <summary>Puts the selector on the word's last tile.</summary>
        private void SnapSelectorToLast()
        {
            if (mSelection.Count == 0)
            {
                return;
            }
            Piece aLast = mSelection[mSelection.Count - 1];
            mSelectorCol = aLast.mCol;
            mSelectorHalf = HalfOf(aLast.mCol, aLast.mRow);
        }

        /// <summary>A tile's height in half tiles.</summary>
        private static int HalfOf(int theCol, int theRow)
        {
            return (theCol & 1) == 0 ? theRow * 2 - 1 : theRow * 2;
        }

        /// <summary>Whether a height is a tile's in theCol (even columns' tiles are at odd heights).</summary>
        private static bool IsTileHalf(int theCol, int theHalf)
        {
            return ((theHalf & 1) != 0) == ((theCol & 1) == 0);
        }

        /// <summary>The row at a tile height of theCol.</summary>
        private static int RowOf(int theCol, int theHalf)
        {
            return (theCol & 1) == 0 ? (theHalf + 1) / 2 : theHalf / 2;
        }

        /// <summary>
        /// Moves the selector (the first input only shows it). Sideways keeps the height, so the selector may stay
        /// above or below a shorter column at the grid's ends; up and down go to the next tile that way, or to
        /// the grid's top or bottom edge past the column's last tile.
        /// </summary>
        private void SelectorMove(int theDir)
        {
            if (ShowSelector())
            {
                return;
            }
            switch (theDir)
            {
            case DIR_LEFT:
                mSelectorCol = System.Math.Max(0, mSelectorCol - 1);
                break;
            case DIR_RIGHT:
                mSelectorCol = System.Math.Min(COLS - 1, mSelectorCol + 1);
                break;
            default:
            {
                int aStep = theDir == DIR_UP ? -1 : 1;
                int aHalf = mSelectorHalf + (IsTileHalf(mSelectorCol, mSelectorHalf) ? aStep * 2 : aStep);
                // Past the grid's top or bottom it stops at the edge, maybe half a step off this column's tiles.
                mSelectorHalf = System.Math.Clamp(aHalf, SELECTOR_MIN_HALF, SELECTOR_MAX_HALF);
                break;
            }
            }
            MarkDirty();
        }

        /// <summary>The tile under the selector, if it sits on one.</summary>
        private Piece GetSelectorPiece()
        {
            if (!IsTileHalf(mSelectorCol, mSelectorHalf))
            {
                return null;
            }
            int aRow = RowOf(mSelectorCol, mSelectorHalf);
            if (aRow < 0 || aRow >= ROWS)
            {
                return null;
            }
            return mGrid[aRow, mSelectorCol];
        }

        /// <summary>A/Space/Enter: taps the tile under the selector (as a touch would), when on one.</summary>
        private void SelectorTap()
        {
            if (ShowSelector())
            {
                return;
            }
            Piece aPiece = GetSelectorPiece();
            if (aPiece == null || aPiece.mRemoving)
            {
                return;
            }
            mTouchDragged = false;
            PieceDown(aPiece);
            MouseUp(0, 0, 1);
        }

        /// <summary>B/Backspace: drops the word's last letter; the selector goes to the new last one.</summary>
        private void SelectorDropLast()
        {
            ShowSelector();
            if (mSelection.Count == 0)
            {
                return;
            }
            if (mSelection.Count == 1)
            {
                ClearSelection();
                return;
            }
            SelectPiece(mSelection[mSelection.Count - 2]);
            mTouchMode = 1;
            SnapSelectorToLast();
        }

        /// <summary>X: drops the whole word.</summary>
        private void SelectorDropWord()
        {
            ShowSelector();
            if (mSelection.Count > 0)
            {
                ClearSelection();
            }
        }

        /// <summary>The selector over the tiles (DrawPieces), centred on its cell.</summary>
        private void DrawSelector(Graphics g)
        {
            if (!mSelectorVisible || mState != STATE_PLAYING || mPauseFlags != 0)
            {
                return;
            }
            int aScale = System.Math.Max(1, AtlasResourceBase.IMAGE_GROUP31_17.mWidth / 45);
            aScale = aScale >= 4 ? 4 : aScale >= 2 ? 2 : 1;
            if (!mSelectorImages.TryGetValue(aScale, out Image anImage))
            {
                anImage = GradientText.LoadScaled(mApp, "images/selector", aScale);
                mSelectorImages[aScale] = anImage;
            }
            if (anImage == null)
            {
                return;
            }
            // A height between two tiles is drawn as a tile of the other column parity would be.
            int aParityCol = (mSelectorHalf & 1) != 0 ? 0 : 1;
            int aX = GetColX(mSelectorCol) + (mTileWidth - anImage.mWidth) / 2;
            int aY = GetRowY(aParityCol, RowOf(aParityCol, mSelectorHalf)) + (mTileHeight - anImage.mHeight) / 2;
            g.SetColorizeImages(false);
            g.DrawImage(anImage, aX, aY);
        }
    }
}
