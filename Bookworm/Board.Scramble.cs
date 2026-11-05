using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Sexy
{
    public partial class Board
    {
        /// <summary>The scramble confirmation's dialog id and its "Scramble" button.</summary>
        public const int DIALOG_SCRAMBLE = 2;

        public const int BUTTON_SCRAMBLE = 0;

        public const int BUTTON_CANCEL = 1001;

        /// <summary>The "Can't Scramble!" alert's dialog id.</summary>
        public const int DIALOG_CANT_SCRAMBLE = 1;

        /// <summary>Plain tiles of the top row that a scramble may set alight (0x360).</summary>
        private List<Piece> mScrambleCandidates = new List<Piece>();

        /// <summary>
        /// FUN_0000726c, on a shake (or button 2): only while playing. Refused with a buzz and a "Can't Scramble!"
        /// alert with 10 or more fire tiles, with no plain tile in the top row, or in Classic with a fire tile in
        /// the bottom row (that would lose the game); nothing while tiles are leaving. Otherwise it scrambles, or
        /// first asks when the Options say so (app + 0x1200).
        /// </summary>
        public void RequestScramble()
        {
            if (mState != STATE_PLAYING)
            {
                return;
            }
            if (mFireTileCount >= 10)
            {
                mApp.PlaySound(Res.SOUND_BUZZER);
                ShowCantScramble(SexyAppBase.Loc("Scrambling now would put too many fire tiles on the board!"));
                return;
            }
            mScrambleCandidates.Clear();
            for (int aCol = 0; aCol < COLS; aCol++)
            {
                Piece aPiece = mGrid[0, aCol];
                if (aPiece != null && aPiece.mType == Piece.TYPE_NORMAL)
                {
                    mScrambleCandidates.Add(aPiece);
                }
            }
            if (mScrambleCandidates.Count == 0)
            {
                mApp.PlaySound(Res.SOUND_BUZZER);
                ShowCantScramble("There are too many fire tiles at the top of the board!");
                return;
            }
            if (mApp.mGameMode == BookWormApp.GAME_CLASSIC)
            {
                for (int aCol = 0; aCol < COLS; aCol++)
                {
                    Piece aBottom = mGrid[ROWS - 1, aCol];
                    if (aBottom != null && aBottom.mType == Piece.TYPE_FIRE)
                    {
                        mApp.PlaySound(Res.SOUND_BUZZER);
                        ShowCantScramble(SexyAppBase.Loc("Scrambling now would cause you to lose the game!"));
                        return;
                    }
                    for (int aRow = ROWS - 1; aRow >= 0; aRow--)
                    {
                        if (mGrid[aRow, aCol] != null && mGrid[aRow, aCol].mRemoving)
                        {
                            return;
                        }
                    }
                }
            }
            if (!mApp.mConfirmScramble)
            {
                Scramble();
                return;
            }
            mHintShowing = true;
            ActionSheet aSheet = new ActionSheet(DIALOG_SCRAMBLE, this, mApp);
            FancyTextWidget aMessage = aSheet.GetMessage();
            aMessage.SetFont(Res.FONT_ARIAL_MEDIUM_OUTLINED);
            aMessage.SetColor(Color.White);
            aMessage.SetJustify(0);
            aMessage.AddText("Scrambling the board will add fire tiles and remove your bonus tiles.", 0);
            aMessage.NewLine(0);
            aMessage.Fit(0);
            aSheet.SetRedButton(BUTTON_SCRAMBLE, "Scramble");
            aSheet.SetGreenButton(BUTTON_CANCEL, SexyAppBase.Loc("Cancel"));
            aSheet.Show();
        }

        /// <summary>Port: the desktop has no shake, so the space bar asks for a scramble.</summary>
        public override void KeyDown(KeyCode theKey)
        {
            if (theKey == KeyCode.Space)
            {
                RequestScramble();
                return;
            }
            base.KeyDown(theKey);
        }

        /// <summary>FUN_00007930: the "Can't Scramble!" alert (dialog 1) with an OK.</summary>
        private void ShowCantScramble(string theReason)
        {
            mHintShowing = true;
            Alert anAlert = new Alert(DIALOG_CANT_SCRAMBLE, this, mApp);
            anAlert.SetTitle(SexyAppBase.Loc("Can't Scramble!"));
            anAlert.SetMessage(theReason);
            anAlert.AddRedButton(DIALOG_CONTINUE, SexyAppBase.Loc("OK"));
            anAlert.Show();
        }

        /// <summary>
        /// FUN_00007b60: the scramble. The selection is dropped and the scramble counted; as many top-row plain
        /// tiles as scrambles made this game (at most all of them) catch fire, each in a burst of 100 embers (the
        /// first with SOUND_FLAME_TILE, Lex's anim 0x12 and SOUND_LEX_SCRAMBLE). Every other tile loses its bonus
        /// or gem and gets a new letter, shown after the board-wide flip (state 7). The scramble hint comes once
        /// (app + 0x1204); the fire burns a step and the board refills.
        /// </summary>
        public void Scramble()
        {
            foreach (Piece aPiece in mSelection)
            {
                aPiece.mSelected = false;
            }
            mSelection.Clear();
            mWordScore.Build(null);
            mTouchMode = 0;
            MarkDirty();
            mApp.mWordBalloon.Hide();
            mLevelStats.mScrambles++;
            mGameStats.mScrambles++;
            int aFires = System.Math.Min(mScrambleCandidates.Count, mGameStats.mScrambles);
            bool aCried = false;
            for (int i = 0; i < aFires; i++)
            {
                int anIndex = (int)(gRand.Next() % (uint)mScrambleCandidates.Count);
                Piece aPiece = mScrambleCandidates[anIndex];
                mScrambleCandidates.RemoveAt(anIndex);
                aPiece.mType = Piece.TYPE_FIRE;
                aPiece.mBurnStage = 0;
                mFireTileCount++;
                if (!aCried)
                {
                    aCried = true;
                    mApp.PlaySound(Res.SOUND_FLAME_TILE);
                    mApp.mLex.PlayAnim(0x12, false);
                    mApp.PlaySound(Res.SOUND_LEX_SCRAMBLE);
                }
                for (int j = 0; j < 100; j++)
                {
                    Ember anEmber = mEmbers.Add();
                    anEmber.mType = mRand.Next() % 100 < 0x50 ? 0 : 3;
                    anEmber.mX = aPiece.mX + mRand.Next() % (uint)mTileWidth;
                    anEmber.mY = aPiece.mY + mRand.Next() % (uint)mTileHeight;
                    anEmber.mVelX = (mRand.Next() % 1000 - 500.0) / 250.0;
                    anEmber.mVelY = (mRand.Next() % 1000 - 500.0) / 250.0;
                    anEmber.mGravity = 0;
                    anEmber.mLife = 1.0;
                    anEmber.mDecay = mRand.Next() % 1000 / 1000.0 * 0.05 + 0.02;
                }
            }
            for (int aCol = 0; aCol < COLS; aCol++)
            {
                for (int aRow = ROWS - 1; aRow >= 0; aRow--)
                {
                    Piece aPiece = mGrid[aRow, aCol];
                    if (aPiece == null)
                    {
                        continue;
                    }
                    if (aPiece.mType == Piece.TYPE_FIRE)
                    {
                        aPiece.mNextLetter = aPiece.mLetter;
                    }
                    else
                    {
                        aPiece.mType = Piece.TYPE_NORMAL;
                        aPiece.mNextLetter = PickLetter();
                    }
                }
            }
            if (!mApp.mHintsSeen[BookWormApp.HINT_SCRAMBLE] && mHintTimer == 0)
            {
                mHintTimer = 100;
                mHintKind = HINT_SCRAMBLE;
                mApp.mHintsSeen[BookWormApp.HINT_SCRAMBLE] = true;
                mApp.SaveSettings();
            }
            BurnStep(mApp.mGameMode == BookWormApp.GAME_CLASSIC);
            Refill();
            if (mApp.mGameMode == BookWormApp.GAME_CLASSIC && mWarnings == 0)
            {
                for (int aCol = 0; aCol < COLS; aCol++)
                {
                    Piece aPiece = mGrid[ROWS - 1, aCol];
                    if (aPiece != null && aPiece.mType == Piece.TYPE_FIRE)
                    {
                        mWarnings = 3;
                        break;
                    }
                }
            }
            mState = STATE_SCRAMBLE;
            mFlipTime = 0;
            mFireSoundPlayed = false;
            MarkDirty();
        }

        /// <summary>State 7 (in 0x9f68): the board-wide flip runs (0.01 a frame); done, the new letters show.</summary>
        private void UpdateScramble()
        {
            mFlipTime += 0.01;
            if (mFlipTime >= 1.0)
            {
                foreach (Piece aPiece in mGrid)
                {
                    if (aPiece != null)
                    {
                        aPiece.mLetter = aPiece.mNextLetter;
                    }
                }
                mFlipTime = 0;
                mState = STATE_PLAYING;
            }
            MarkDirty();
        }
    }
}
