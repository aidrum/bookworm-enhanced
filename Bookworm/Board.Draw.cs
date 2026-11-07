using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Sexy
{
    public partial class Board
    {
        private static readonly Color WHITE = new Color(255, 255, 255, 255);

        /// <summary>
        /// 0x103f0: the bookshelf, the level bar, the level and the score, then the tiles.
        /// The burp cloud rises off Lex (0x2fd/0x304), then the effects (0xf0) and embers (0xf4) over the tiles.
        /// </summary>
        public override void Draw(Graphics g)
        {
            ScreenPos aPos = mApp.mScreenPos;
            Image aShelf = mGreyed ? AtlasResourceBase.IMAGE_GROUP7_00 : AtlasResourceBase.IMAGE_GROUP6_00;
            float aScale = BoardScale;
            if (aScale != 1f)
            {
                // Port (wide game layout): the board at its own size into an image, drawn scaled.
                int aWidth = LogicalWidth;
                int aHeight = LogicalHeight;
                if (mScaledImage == null || mScaledImage.mWidth != aWidth || mScaledImage.mHeight != aHeight)
                {
                    mScaledImage?.Dispose();
                    mScaledImage = new MemoryImage();
                    mScaledImage.Create(aWidth, aHeight);
                }
                Graphics aBoardG = Graphics.GetNew(mScaledImage);
                aBoardG.Clear(Color.Transparent);
                DrawShelf(aBoardG, aShelf, aWidth, aHeight);
                DrawPieces(aBoardG);
                mEffects.Draw(aBoardG);
                mEmbers.Draw(aBoardG, false);
                aBoardG.SetRenderTarget(null);
                aBoardG.PrepareForReuse();
                g.DrawImage(mScaledImage, new TRect(0, 0, mWidth, mHeight), new TRect(0, 0, aWidth, aHeight));
            }
            else
            {
                DrawShelf(g, aShelf, mWidth, mHeight);
            }
            if (aPos.mWideGame && aShelf.mHeight > aPos.mWideStripY)
            {
                // Port (wide game layout): the books strip under the bottom shelf, behind the bar beside the board.
                int aShelfX = aShelf.mWidth > aPos.mWideBoardWidth ? (aShelf.mWidth - aPos.mWideBoardWidth) / 2 : 0;
                int aStripWidth = System.Math.Min(aShelf.mWidth, aPos.mWideBoardWidth);
                int aStripHeight = aShelf.mHeight - aPos.mWideStripY;
                // Tiled across the bar's whole width.
                int aBarLeft = aPos.At(0x158);
                for (int aStripX = aBarLeft; aStripX < aBarLeft + aPos.mWideBarWidth; aStripX += aStripWidth)
                {
                    int aWidth = System.Math.Min(aStripWidth, aBarLeft + aPos.mWideBarWidth - aStripX);
                    g.DrawImage(aShelf, new TRect(aStripX, aPos.mWideStripDestY, aWidth, aStripHeight), new TRect(aShelfX, aPos.mWideStripY, aWidth, aStripHeight));
                }
            }
            base.Draw(g);
            DrawLevelBar(g);
            DrawLevel(g);
            DrawScore(g);
            if (mBurpCloud)
            {
                Image aCloud = AtlasResourceBase.IMAGE_GROUP5_15;
                Lex aLex = mApp.mLex;
                g.SetColorizeImages(true);
                g.SetColor(new Color(255, 255, 255, (int)(mBurpAlpha * 255.0)));
                g.DrawImage(aCloud, aLex.mX + 0x3c - mX, aLex.mY + 0x32 - mY - aCloud.mHeight / 2 - (int)((1.0 - mBurpAlpha) * 48.0));
                g.SetColorizeImages(false);
            }
            if (aScale != 1f)
            {
                mEmbers.Draw(g, true);
                return;
            }
            DrawPieces(g);
            mEffects.Draw(g);
            mEmbers.Draw(g);
        }

        /// <summary>
        /// Port: the iPad's bookshelf (with its side pillars) centred on the iPhone's narrower board and cut to the
        /// board's size (ScreenPos.NarrowBoard; the board does not clip, so the pillars would show beside it).
        /// </summary>
        private void DrawShelf(Graphics g, Image theShelf, int theWidth, int theHeight)
        {
            int aShelfX = theShelf.mWidth > theWidth ? (theShelf.mWidth - theWidth) / 2 : 0;
            int aShelfWidth = System.Math.Min(theShelf.mWidth, theWidth);
            int aShelfHeight = System.Math.Min(theShelf.mHeight, theHeight);
            g.DrawImage(theShelf, new TRect(0, 0, aShelfWidth, aShelfHeight), new TRect(aShelfX, 0, aShelfWidth, aShelfHeight));
        }

        /// <summary>FUN_00010068: the book spines fill the bar from the left (ScreenPos + 0x150), under its frame (+ 0x158).</summary>
        private void DrawLevelBar(Graphics g)
        {
            ScreenPos aPos = mApp.mScreenPos;
            Image aFrame = mGreyed ? AtlasResourceBase.IMAGE_GROUP4_01 : AtlasResourceBase.IMAGE_GROUP4_02;
            // Port (wide game layout): the bar spans the screen (ScreenPos.mWideBarWidth), its books window grows
            // by as much as the frame.
            int aGrowth = aPos.mWideGame ? aPos.mWideBarWidth - aFrame.mWidth : 0;
            if (mLevelProgress > 0)
            {
                Image aBar = mGreyed ? AtlasResourceBase.IMAGE_GROUP7_03 : AtlasResourceBase.IMAGE_GROUP7_02;
                int aLeft = aPos.At(0x150);
                int aTop = aPos.At(0x154);
                int aBarWidth = aBar.mWidth + aGrowth;
                int aX = System.Math.Min(aLeft, (int)(aLeft + aBarWidth * mLevelProgress) - aBarWidth);
                g.PushState();
                g.ClipRect(aLeft, aTop, aBarWidth, aBar.mHeight);
                if (aGrowth > 0)
                {
                    // Port: the books tiled across the wider window. The last tile would reach past the fill's leading
                    // edge (aX + aBarWidth), so the clip stops there.
                    g.ClipRect(aLeft, aTop, aX + aBarWidth - aLeft, aBar.mHeight);
                    for (int aTileX = aX; aTileX < aX + aBarWidth; aTileX += aBar.mWidth)
                    {
                        g.DrawImage(aBar, aTileX, aTop);
                    }
                }
                else
                {
                    g.DrawImage(aBar, aX, aTop);
                }
                g.PopState();
            }
            // Port (Spanish): the frame's right end (the pillar with "LVL") is its left end mirrored, "NV" drawn on it.
            bool aSpanish = Localization.IsSpanish;
            int aFrameX = aPos.At(0x158);
            int aFrameY = aPos.At(0x15c);
            if (aGrowth > 0)
            {
                DrawStretchedMiddle(g, aFrame, aFrameX, aFrameY, aPos.mWideBarWidth, aSpanish);
            }
            else if (aSpanish)
            {
                int aHalf = (aFrame.mWidth + 1) / 2;
                TRect aLeftHalf = new TRect(0, 0, aHalf, aFrame.mHeight);
                g.DrawImage(aFrame, aFrameX, aFrameY, aLeftHalf);
                g.DrawImageMirror(aFrame, aFrameX + aFrame.mWidth - aHalf, aFrameY, aLeftHalf);
            }
            else
            {
                g.DrawImage(aFrame, aFrameX, aFrameY);
            }
            if (aSpanish)
            {
                DrawLevelLabel(g, aFrame, aFrameX + aFrame.mWidth + aGrowth, aFrameY);
            }
        }

        /// <summary>
        /// Port (Spanish): "NV" where the frame's "LVL" was, in its lettering: the outlined Arial Rounded (its fill a
        /// layer of its own to colour; the large outlined font's iPad Retina one is a single layer), lime (grey when
        /// greyed) brighter toward the bottom, its capitals as high and centred as LVL's. The places are the iPad
        /// Retina frame's (1284 x 268), from its right edge (theRight: the pillar stays at the end when widened).
        /// </summary>
        private void DrawLevelLabel(Graphics g, Image theFrame, int theRight, int theY)
        {
            float aUnit = theFrame.mHeight / 268f;
            int aCenterX = theRight - (int)System.Math.Round((1284 - 1213.5f) * aUnit);
            int aCenterY = theY + (int)System.Math.Round(102 * aUnit);
            Color aTop = mGreyed ? LEVEL_LABEL_GREY_TOP : LEVEL_LABEL_TOP;
            Color aBottom = mGreyed ? LEVEL_LABEL_GREY_BOTTOM : LEVEL_LABEL_BOTTOM;
            GradientText.DrawFitted(g, Res.FONT_ARIAL_MEDIUM_OUTLINED, new[] { "NV" }, aCenterX, aCenterY,
                (int)System.Math.Round(71 * aUnit), (int)System.Math.Round(27 * aUnit), aTop, aBottom);
        }

        /// <summary>Port: the "LVL" art's lime, top to bottom, and its greyed frame's grey.</summary>
        private static readonly Color LEVEL_LABEL_TOP = new Color(135, 183, 0, 255);

        private static readonly Color LEVEL_LABEL_BOTTOM = new Color(181, 244, 0, 255);

        private static readonly Color LEVEL_LABEL_GREY_TOP = new Color(112, 112, 114, 255);

        private static readonly Color LEVEL_LABEL_GREY_BOTTOM = new Color(148, 148, 150, 255);

        /// <summary>
        /// Port (wide game layout): the bar's frame at theWidth: its ends (the menu button's and the level's pillars,
        /// BAR_END_PARTS of its width each) as they are, its plain middle stretched between them.
        /// </summary>
        private static void DrawStretchedMiddle(Graphics g, Image theImage, int theX, int theY, int theWidth, bool theMirrorLeft = false)
        {
            int anEnd = theImage.mWidth * BAR_END_PARTS / 0x280;
            int aHeight = theImage.mHeight;
            int aMiddle = theImage.mWidth - anEnd * 2;
            g.DrawImage(theImage, new TRect(theX, theY, anEnd, aHeight), new TRect(0, 0, anEnd, aHeight));
            g.DrawImage(theImage, new TRect(theX + anEnd, theY, theWidth - anEnd * 2, aHeight), new TRect(anEnd, 0, aMiddle, aHeight));
            if (theMirrorLeft)
            {
                // Port (Spanish): the left end mirrored, in place of the right end's "LVL".
                g.DrawImageMirror(theImage, theX + theWidth - anEnd, theY, new TRect(0, 0, anEnd, aHeight));
                return;
            }
            g.DrawImage(theImage, new TRect(theX + theWidth - anEnd, theY, anEnd, aHeight), new TRect(theImage.mWidth - anEnd, 0, anEnd, aHeight));
        }

        /// <summary>Port: how much of the bar frame's 640 (iPhone Retina) pixels each end keeps (the pillars and
        /// their shading reach about 80 in; 100 to 540 is plain).</summary>
        private const int BAR_END_PARTS = 0x64;

        /// <summary>FUN_0001020c: the level number, green with a black shadow (centred on ScreenPos + 0x168).</summary>
        private void DrawLevel(Graphics g)
        {
            if (mGreyed)
            {
                return;
            }
            Font aFont = Res.FONT_ARIAL_SMALL;
            string aText = (mLevel + 1).ToString();
            int aX = mApp.mScreenPos.At(0x168) - aFont.StringWidth(aText) / 2;
            int aY = mApp.mScreenPos.At(0x16c);
            g.SetFont(aFont);
            g.SetColor(new Color(0, 0, 0, 255));
            g.DrawString(aText, aX + 1, aY + 1);
            g.SetColor(new Color(0, 0xff, 0x18, 0xff));
            g.DrawString(aText, aX, aY);
        }

        /// <summary>FUN_0000fe20: the score in gold (centred on ScreenPos + 0x160), glowing hot after a big word.</summary>
        private void DrawScore(Graphics g)
        {
            if (mGreyed)
            {
                return;
            }
            string aText = string.Format("{0:#,0}", mDisplayScore);
            Font aFont = Res.FONT_GOLDSCORE;
            int aX = mApp.mScreenPos.At(0x160) - aFont.StringWidth(aText) / 2;
            int aY = mApp.mScreenPos.At(0x164);
            g.SetColor(WHITE);
            g.SetFont(aFont);
            g.DrawString(aText, aX, aY);
            if (mScoreHeat > 0)
            {
                int anAlpha = mScoreHeat > 0.5 ? 0x7f : (int)(mScoreHeat * 255.0);
                g.SetFont(Res.FONT_GOLDSCORE_HEATED);
                g.SetColor(new Color(255, 255, 255, anAlpha));
                g.SetDrawMode(1);
                g.DrawString(aText, aX, aY);
                g.SetDrawMode(0);
            }
        }

        /// <summary>
        /// FUN_0000fd54: the tile backs bottom row first (0xed00), the glow of selected gems (0xf4a0), the letters
        /// (0xf3ec; hidden while an Arcade game is paused), the other gems' glow, the tile glows (0xf574), the
        /// word's path (0xeb50) and the tap-to-submit arrow (IMAGE_GROUP4_09 at 0x2e0/0x2e4, ScreenPos 0x1d8).
        /// </summary>
        private void DrawPieces(Graphics g)
        {
            for (int aRow = ROWS - 1; aRow >= 0; aRow--)
            {
                for (int aCol = 0; aCol < COLS; aCol++)
                {
                    Piece aPiece = mGrid[aRow, aCol];
                    if (aPiece != null)
                    {
                        DrawPieceBack(g, aPiece);
                    }
                }
            }
            if (!mGreyed)
            {
                DrawGemGlows(g, true);
            }
            bool aHidden = mApp.mGameMode != BookWormApp.GAME_CLASSIC && mPauseFlags != 0 && !mGreyed;
            if (!aHidden)
            {
                for (int aRow = ROWS - 1; aRow >= 0; aRow--)
                {
                    for (int aCol = 0; aCol < COLS; aCol++)
                    {
                        Piece aPiece = mGrid[aRow, aCol];
                        if (aPiece != null && !aPiece.mRemoving)
                        {
                            DrawLetter(g, aPiece);
                        }
                    }
                }
            }
            if (aHidden || !mGreyed)
            {
                DrawGemGlows(g, false);
                DrawTileGlows(g);
                if (mSelection.Count > 0)
                {
                    DrawPath(g);
                }
            }
            if (mSubmitArrowAlpha > 0)
            {
                g.SetColor(new Color(255, 255, 255, mSubmitArrowAlpha));
                g.SetColorizeImages(true);
                g.DrawImage(AtlasResourceBase.IMAGE_GROUP4_09, mSubmitArrowX - mApp.mScreenPos.At(0x1d8), mSubmitArrowY - mApp.mScreenPos.At(0x1dc));
                g.SetColorizeImages(false);
            }
        }

        /// <summary>FUN_0000f4a0: the additive glow (IMAGE_GROUP51_08) of the sapphire and diamond tiles selected or not.</summary>
        private void DrawGemGlows(Graphics g, bool theSelected)
        {
            g.SetColorizeImages(false);
            g.SetDrawMode(1);
            for (int aRow = ROWS - 1; aRow >= 0; aRow--)
            {
                for (int aCol = 0; aCol < COLS; aCol++)
                {
                    Piece aPiece = mGrid[aRow, aCol];
                    if (aPiece != null && !aPiece.mRemoving && aPiece.mSelected == theSelected && !aPiece.mFlipping &&
                        (aPiece.mType == Piece.TYPE_SAPPHIRE || aPiece.mType == Piece.TYPE_DIAMOND))
                    {
                        g.DrawImage(AtlasResourceBase.IMAGE_GROUP51_08, (int)aPiece.mX, (int)aPiece.mY);
                    }
                }
            }
            g.SetDrawMode(0);
        }

        /// <summary>
        /// FUN_0000f574: pulsing additive glows behind the letters: bonus tiles (IMAGE_GROUP4_12), gold and gem
        /// tiles (IMAGE_GROUP4_07), fire tiles, the tile burning under one and Arcade tiles about to burn
        /// (IMAGE_GROUP4_11; fire tiles also get the flames IMAGE_GROUP4_04/05, clipped to the tile). Plain tiles
        /// in a word with a special tile glow like it (gold/gem first, else bonus).
        /// </summary>
        private void DrawTileGlows(Graphics g)
        {
            List<Piece> aBonus = new List<Piece>();
            List<Piece> aGems = new List<Piece>();
            List<Piece> aFire = new List<Piece>();
            List<Piece> aSpecial = null;
            foreach (Piece aPiece in mSelection)
            {
                if (aPiece.mType == Piece.TYPE_BONUS && aSpecial == null)
                {
                    aSpecial = aBonus;
                }
                if (aPiece.mType == Piece.TYPE_GOLD || aPiece.mType == Piece.TYPE_SAPPHIRE || aPiece.mType == Piece.TYPE_DIAMOND)
                {
                    aSpecial = aGems;
                    break;
                }
            }
            for (int aRow = ROWS - 1; aRow >= 0; aRow--)
            {
                for (int aCol = 0; aCol < COLS; aCol++)
                {
                    Piece aPiece = mGrid[aRow, aCol];
                    if (aPiece == null || mGreyed || aPiece.mRemoving)
                    {
                        continue;
                    }
                    if (aPiece.mType == Piece.TYPE_FIRE)
                    {
                        aFire.Add(aPiece);
                        if (aPiece.mRow < ROWS - 1)
                        {
                            Piece aBelow = mGrid[aPiece.mRow + 1, aPiece.mCol];
                            if (aBelow != null && aBelow.mBurnStage > 0)
                            {
                                aFire.Add(aBelow);
                            }
                        }
                    }
                    else if (aPiece.mArcadeBurn)
                    {
                        aFire.Add(aPiece);
                    }
                    else if (aPiece.mType == Piece.TYPE_GOLD || aPiece.mType == Piece.TYPE_SAPPHIRE || aPiece.mType == Piece.TYPE_DIAMOND)
                    {
                        aGems.Add(aPiece);
                    }
                    else if (aPiece.mType == Piece.TYPE_BONUS)
                    {
                        aBonus.Add(aPiece);
                    }
                    else if (aSpecial != null && aPiece.mSelected)
                    {
                        aSpecial.Add(aPiece);
                    }
                }
            }
            int aCount = mUpdateCnt;
            int aShade = 0xb0 - System.Math.Abs(aCount * 2 % 0x100 - 0x80);
            DrawGlows(g, aBonus, AtlasResourceBase.IMAGE_GROUP4_12, AtlasResourceBase.IMAGE_GROUP4_19, new Color(aShade, aShade, aShade, 0xff));
            aShade = 0x80 - System.Math.Abs(aCount % 0x80 - 0x40);
            DrawGlows(g, aGems, AtlasResourceBase.IMAGE_GROUP4_07, AtlasResourceBase.IMAGE_GROUP51_07, new Color(aShade, aShade, aShade, 0xff));
            aShade = 0xff - System.Math.Abs(aCount * 4 % 0x1fe - 0xff);
            DrawGlows(g, aFire, AtlasResourceBase.IMAGE_GROUP4_11, AtlasResourceBase.IMAGE_GROUP31_21, new Color(aShade, aShade, aShade, aShade));
            foreach (Piece aPiece in aFire)
            {
                if (aPiece.mType == Piece.TYPE_FIRE)
                {
                    DrawFlames(g, aPiece);
                }
            }
        }

        /// <summary>The glow image centred on each tile (by theTile's size), additive in theColor.</summary>
        private void DrawGlows(Graphics g, List<Piece> thePieces, Image theGlow, Image theTile, Color theColor)
        {
            int anOffset = (theGlow.mWidth - theTile.mWidth) / 2;
            g.SetColor(theColor);
            g.SetColorizeImages(true);
            g.SetDrawMode(1);
            foreach (Piece aPiece in thePieces)
            {
                g.DrawImage(theGlow, (int)aPiece.mX - anOffset, (int)aPiece.mY - anOffset);
            }
            g.SetDrawMode(0);
            g.SetColorizeImages(false);
        }

        /// <summary>
        /// The flames of a fire tile (0xf574): IMAGE_GROUP4_04 and IMAGE_GROUP4_05 animated by the update count,
        /// additive, clipped to the tile; IMAGE_GROUP4_04 again with its halves swapped.
        /// </summary>
        private void DrawFlames(Graphics g, Piece thePiece)
        {
            Image aFlame = AtlasResourceBase.IMAGE_GROUP4_04;
            Image aFlame2 = AtlasResourceBase.IMAGE_GROUP4_05;
            int aHalf = aFlame.GetCelWidth() / 2;
            TRect aCel = aFlame.GetAnimCelRect(mUpdateCnt);
            TRect aLeft = new TRect(aCel.mX, aCel.mY, aHalf, aCel.mHeight);
            TRect aRight = new TRect(aCel.mX + aHalf, aCel.mY, aHalf, aCel.mHeight);
            TRect aCel2 = aFlame2.GetAnimCelRect(mUpdateCnt);
            int aX = (int)thePiece.mX;
            int aY = (int)thePiece.mY;
            g.PushState();
            g.SetDrawMode(1);
            g.ClipRect(aX, aY, mTileWidth, mTileHeight);
            g.DrawImage(aFlame, aX, aY, aCel);
            g.DrawImage(aFlame2, aX, aY, aCel2);
            g.DrawImage(aFlame, aX + aHalf, aY, aLeft);
            g.DrawImage(aFlame, aX, aY, aRight);
            g.SetDrawMode(0);
            g.PopState();
        }

        /// <summary>
        /// FUN_0000eb50: the links between the word's tiles (IMAGE_GROUP4_08, a 3x2 sheet by direction, centred on
        /// the tile), up to 12 letters (Qu counts 2); past 12 (8 without reward streaks) letters each tile gets
        /// IMAGE_GROUP4_17.
        /// </summary>
        private void DrawPath(Graphics g)
        {
            Image aLink = AtlasResourceBase.IMAGE_GROUP4_08;
            int aLetters = 0;
            g.SetColorizeImages(false);
            for (int i = 0; i < mSelection.Count; i++)
            {
                Piece aPiece = mSelection[i];
                aLetters += aPiece.mLetter == 'Q' ? 2 : 1;
                Piece aNext = i + 1 < mSelection.Count ? mSelection[i + 1] : null;
                if (aNext != null && aLetters < 12 && (aNext.mLetter != 'Q' || aLetters < 11))
                {
                    // Odd columns sit half a tile lower: a sideways link within a row goes up from an odd column.
                    int aDY = aNext.mRow - aPiece.mRow;
                    if (aNext.mCol != aPiece.mCol && aNext.mRow == aPiece.mRow)
                    {
                        aDY = (aPiece.mCol & 1) != 0 ? -1 : 0;
                    }
                    int aCelWidth = aLink.mWidth / 3;
                    int aCelHeight = aLink.mHeight / 2;
                    int aCelX = aCelWidth * (aNext.mCol + 1 - aPiece.mCol);
                    int aCelY = aDY >= 0 ? aCelHeight : 0;
                    g.DrawImage(aLink, aCelX + (int)aPiece.mX + (mTileWidth - aLink.mWidth) / 2,
                        aCelY + (int)aPiece.mY + (mTileHeight - aLink.mHeight) / 2, new TRect(aCelX, aCelY, aCelWidth, aCelHeight));
                }
                if (aLetters > (mApp.mRewardStreaks ? 12 : 8))
                {
                    g.DrawImage(AtlasResourceBase.IMAGE_GROUP4_17, (int)aPiece.mX, (int)aPiece.mY);
                }
            }
        }

        /// <summary>The flip animation's frame for a tile: (t * 18 / 4 - (row + col) / 4) * 8 (0xed88/0xf088).</summary>
        private int GetFlipFrame(Piece thePiece)
        {
            double aTime = thePiece.mFlipping ? thePiece.mFlipTime : mFlipTime;
            return (int)((aTime * 18.0 * 0.25 - (thePiece.mRow + thePiece.mCol) * 0.25) * 8.0);
        }

        /// <summary>Flipping during the board-wide flip (state 7, not fire tiles) or on its own.</summary>
        private bool IsFlipping(Piece thePiece)
        {
            return thePiece.mFlipping || (mState == 7 && thePiece.mType != Piece.TYPE_FIRE);
        }

        /// <summary>The board-wide flip time (0x1f8).</summary>
        public double mFlipTime;

        /// <summary>FUN_0000ed88: the tile's back by type (lit when selected); the gems are translucent.</summary>
        private void DrawPieceBack(Graphics g, Piece thePiece)
        {
            int aX = (int)thePiece.mX;
            int aY = (int)thePiece.mY;
            g.SetColorizeImages(false);
            Image anImage = AtlasResourceBase.IMAGE_GROUP31_17;
            if (mGreyed)
            {
                anImage = AtlasResourceBase.IMAGE_GROUP31_18;
            }
            else
            {
                if (IsFlipping(thePiece))
                {
                    int aFrame = GetFlipFrame(thePiece) - 1;
                    if (aFrame >= 0 && aFrame < 7)
                    {
                        g.DrawImageCel(AtlasResourceBase.IMAGE_GROUP4_03, aX, aY, aFrame);
                        return;
                    }
                }
                g.SetColor(WHITE);
                if (!thePiece.mFlipping && thePiece.mType <= 5)
                {
                    bool aSel = thePiece.mSelected;
                    switch (thePiece.mType)
                    {
                    case Piece.TYPE_NORMAL:
                        anImage = aSel ? AtlasResourceBase.IMAGE_GROUP4_14 : AtlasResourceBase.IMAGE_GROUP31_17;
                        break;
                    case Piece.TYPE_BONUS:
                        anImage = aSel ? AtlasResourceBase.IMAGE_GROUP4_13 : AtlasResourceBase.IMAGE_GROUP4_19;
                        break;
                    case Piece.TYPE_GOLD:
                        anImage = aSel ? AtlasResourceBase.IMAGE_GROUP4_18 : AtlasResourceBase.IMAGE_GROUP51_07;
                        break;
                    case Piece.TYPE_FIRE:
                        anImage = aSel ? AtlasResourceBase.IMAGE_GROUP31_16 : AtlasResourceBase.IMAGE_GROUP31_21;
                        break;
                    case Piece.TYPE_SAPPHIRE:
                    case Piece.TYPE_DIAMOND:
                        if (thePiece.mType == Piece.TYPE_SAPPHIRE)
                        {
                            anImage = aSel ? AtlasResourceBase.IMAGE_GROUP4_16 : AtlasResourceBase.IMAGE_GROUP4_15;
                        }
                        else
                        {
                            anImage = aSel ? AtlasResourceBase.IMAGE_GROUP31_20 : AtlasResourceBase.IMAGE_GROUP31_19;
                        }
                        g.SetColor(new Color(255, 255, 255, 0xb0));
                        g.SetColorizeImages(true);
                        break;
                    }
                }
            }
            g.DrawImageCel(anImage, aX, aY, 0);
            g.SetColorizeImages(false);
            if (!thePiece.mRemoving)
            {
                return;
            }
            // A sinking tile draws its letter (and gem glow) with its back.
            if (!thePiece.mSelected)
            {
                DrawLetter(g, thePiece);
            }
            if (!thePiece.mFlipping && (thePiece.mType == Piece.TYPE_SAPPHIRE || thePiece.mType == Piece.TYPE_DIAMOND))
            {
                g.SetDrawMode(1);
                g.DrawImage(AtlasResourceBase.IMAGE_GROUP51_08, aX, aY);
                g.SetDrawMode(0);
            }
            if (thePiece.mSelected)
            {
                DrawLetter(g, thePiece);
            }
        }

        /// <summary>
        /// FUN_0000f088: the letter, centred (Cooper; outlined white when selected, black otherwise, grey when
        /// greyed). Q is the "Qu" image. A flipping tile hides its letter mid-flip and shows the next one after.
        /// </summary>
        private void DrawLetter(Graphics g, Piece thePiece)
        {
            char aLetter = thePiece.mLetter;
            if (IsFlipping(thePiece))
            {
                int aFrame = GetFlipFrame(thePiece);
                if (aFrame > 0)
                {
                    if (aFrame < 8)
                    {
                        return;
                    }
                    aLetter = thePiece.mNextLetter;
                }
            }
            Font aFont;
            int aShade;
            if (mGreyed)
            {
                aShade = 0xb0;
                aFont = Res.FONT_COOPER;
            }
            else if (!thePiece.mFlipping && thePiece.mSelected)
            {
                aShade = 0xff;
                aFont = Res.FONT_COOPER_OUTLINED;
            }
            else
            {
                aShade = 0;
                aFont = Res.FONT_COOPER;
            }
            g.SetColor(new Color(aShade, aShade, aShade, 255));
            int aX = (int)thePiece.mX;
            int aY = (int)thePiece.mY;
            if (aLetter == 'Q')
            {
                Image aQu = AtlasResourceBase.IMAGE_GROUP4_23;
                aX += (mTileWidth - aQu.mWidth) / 2;
                aY += (mTileHeight - aQu.mHeight) / 2;
                if (aFont == Res.FONT_COOPER_OUTLINED)
                {
                    g.SetColorizeImages(false);
                    g.DrawImage(AtlasResourceBase.IMAGE_GROUP4_22, aX, aY);
                }
                g.SetColorizeImages(true);
                g.DrawImage(aQu, aX, aY);
                g.SetColorizeImages(false);
                // The "Qu" tile scores both letters.
                DrawValueDots(g, thePiece, WordScore.GetLetterValue('Q') + WordScore.GetLetterValue('U'));
                return;
            }
            string aText = aLetter.ToString();
            int aWidth = aFont.StringWidth(aText);
            int aBase = aFont.GetAscent() * 3 - aFont.GetHeight() + mTileHeight * 2;
            g.SetFont(aFont);
            g.SetColorizeImages(true);
            g.DrawString(aText, aX + (mTileWidth - aWidth) / 2, aY + aBase / 4);
            g.SetColorizeImages(false);
            DrawValueDots(g, thePiece, WordScore.GetLetterValue(aLetter));
        }

        /// <summary>
        /// Port addition: the letter's value as gold dots in the tile's bottom-right corner, from the art the iOS
        /// version ships unused (IMAGE_GROUP4_20: cel = value - 1, so 1 shows none, 2-4 one dot, 5-7 two, 8-10
        /// three). The value is the current language's (WordScore.GetLetterValue).
        /// </summary>
        private void DrawValueDots(Graphics g, Piece thePiece, int theValue)
        {
            Image aDots = AtlasResourceBase.IMAGE_GROUP4_20;
            int aCel = System.Math.Min(theValue, 10) - 1;
            if (aDots == null || aCel <= 0 || mGreyed)
            {
                return;
            }
            int aCelWidth = aDots.GetCelWidth();
            int aCelHeight = aDots.GetCelHeight();
            // Inside the tile's frame: 14 of the 48-pixel cel (iPad Retina) in from the tile's edges.
            int anInset = aCelWidth * 14 / 48;
            g.SetColorizeImages(false);
            g.DrawImageCel(aDots, (int)thePiece.mX + mTileWidth - aCelWidth - anInset,
                (int)thePiece.mY + mTileHeight - aCelHeight - anInset, aCel);
        }
    }
}
