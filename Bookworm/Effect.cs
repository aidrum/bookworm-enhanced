using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace Sexy
{
    /// <summary>
    /// Sexy::Effect (vtable 0x10f718): something the effect manager updates and draws until it is done.
    /// </summary>
    public class Effect
    {
        public EffectMgr mMgr; // 0x04

        public int mAge; // 0x08

        public bool mDone; // 0x0c

        /// <summary>vf3: -1, Sparkle 0, FlyingPieceList 1.</summary>
        public virtual int GetEffectType()
        {
            return -1;
        }

        /// <summary>vf1.</summary>
        public virtual void Update()
        {
            mAge++;
        }

        /// <summary>vf2.</summary>
        public virtual void Draw(Graphics g)
        {
        }
    }

    /// <summary>Sexy::EffectMgr (0x18d90; the board's at + 0xf0, the overlay's at + 0x80).</summary>
    public class EffectMgr
    {
        public BookWormApp mApp; // 0x04

        public MTRand mRand = new MTRand((uint)System.Environment.TickCount); // 0x08

        public int mUpdateCnt; // 0x0c

        public List<Effect> mEffects = new List<Effect>(); // 0x10

        public EffectMgr(BookWormApp theApp)
        {
            mApp = theApp;
        }

        /// <summary>FUN_00019168.</summary>
        public void Add(Effect theEffect)
        {
            theEffect.mMgr = this;
            mEffects.Add(theEffect);
        }

        /// <summary>FUN_000190d4: updates them all and drops the done ones. True if there were any.</summary>
        public bool Update()
        {
            bool anyEffects = mEffects.Count > 0;
            for (int i = 0; i < mEffects.Count; i++)
            {
                Effect anEffect = mEffects[i];
                anEffect.Update();
                if (anEffect.mDone)
                {
                    mEffects.RemoveAt(i--);
                }
            }
            mUpdateCnt++;
            return anyEffects;
        }

        /// <summary>FUN_00019148.</summary>
        public void Draw(Graphics g)
        {
            foreach (Effect anEffect in mEffects)
            {
                anEffect.Draw(g);
            }
        }

        /// <summary>FUN_000191e4.</summary>
        public bool HasType(int theType)
        {
            foreach (Effect anEffect in mEffects)
            {
                if (anEffect.GetEffectType() == theType)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>FUN_0001918c, counted.</summary>
        public int CountType(int theType)
        {
            int aCount = 0;
            foreach (Effect anEffect in mEffects)
            {
                if (anEffect.GetEffectType() == theType)
                {
                    aCount++;
                }
            }
            return aCount;
        }

        /// <summary>FUN_00019088.</summary>
        public void Clear()
        {
            mEffects.Clear();
        }
    }

    /// <summary>
    /// Sexy::Sparkle (0x198c4, 0x48 bytes): a twinkle centred in a tile-sized box: 7 cels of mImageId then 7 of
    /// image 0x27bd, a cel every 8 frames, drawn additive at 200 alpha.
    /// </summary>
    public class Sparkle : Effect
    {
        public double mX; // 0x10

        public double mY; // 0x18

        public double mVelX; // 0x20

        public double mVelY; // 0x28

        public double mGravity; // 0x30

        public int mDelay; // 0x38

        public int mImageId = 0x27bc; // 0x40

        public int mCel; // 0x44

        public override int GetEffectType()
        {
            return 0;
        }

        /// <summary>vf1 (0x1a70c).</summary>
        public override void Update()
        {
            mAge++;
            if (mDelay > 0)
            {
                mDelay--;
                return;
            }
            mX += mVelX;
            mVelY += mGravity;
            mY += mVelY;
            if ((mAge & 7) == 0)
            {
                mCel = (mCel + 1) % 14;
                if (mCel == 0)
                {
                    mDone = true;
                }
                mMgr.mApp.mBoard?.MarkDirty();
            }
        }

        /// <summary>vf2 (0x1a7a0).</summary>
        public override void Draw(Graphics g)
        {
            Image anImage = AtlasResourceBase.GetImageById(mCel < 7 ? mImageId : 0x27bd);
            if (anImage == null)
            {
                return;
            }
            int aCel = mCel < 7 ? mCel : mCel - 7;
            Board aBoard = mMgr.mApp.mBoard;
            g.SetDrawMode(1);
            g.SetColor(new Color(255, 255, 255, 200));
            g.SetColorizeImages(true);
            g.DrawImageCel(anImage, (int)mX + (aBoard.mTileWidth - anImage.GetCelWidth()) / 2, (int)mY + (aBoard.mTileHeight - anImage.GetCelHeight()) / 2, aCel);
            g.SetColorizeImages(false);
            g.SetDrawMode(0);
        }
    }

    /// <summary>
    /// Sexy::FlyingPieceList (0x19934): the tiles of a word, in screen space, flying into Lex's mouth one after
    /// another, shrinking as they go (each chases the one ahead; the first, the mouth at ScreenPos + 0x180 / 0x184
    /// from Lex). Lex opens up once the first is above the board's top row (anim 0x15) and closes when the last
    /// is eaten (0x16). Each bite chomps (at most every 4 frames); a fire tile gives him a burp (board + 0x300).
    /// Bonus and gold tiles sparkle on the way, fire tiles shed embers.
    /// </summary>
    public class FlyingPieceList : Effect
    {
        public List<Piece> mPieces = new List<Piece>(); // 0x10

        /// <summary>The frame of the last chomp (0x18).</summary>
        public int mLastChomp;

        public FlyingPieceList(List<Piece> thePieces)
        {
            mPieces.AddRange(thePieces);
        }

        public override int GetEffectType()
        {
            return 1;
        }

        /// <summary>vf1 (0x19c78).</summary>
        public override void Update()
        {
            BookWormApp anApp = mMgr.mApp;
            Board aBoard = anApp.mBoard;
            Lex aLex = anApp.mLex;
            ScreenPos aPos = anApp.mScreenPos;
            int aTileW = aBoard.mTileWidth;
            int aTileH = aBoard.mTileHeight;
            mAge++;
            if (mPieces.Count == 0)
            {
                mDone = true;
                aLex.PlayAnim(0x16, false);
                return;
            }
            // Port: also once the first tile is as close to his mouth as the top row is in portrait (ScreenPos
            // mLexMouthGap): in the wide game layout he stands beside the board, his mouth below its top row.
            double aMouthDX = mPieces[0].mX - (aPos.At(0x180) + aLex.mX - aTileW / 2);
            double aMouthDY = mPieces[0].mY - (aPos.At(0x184) + aLex.mY);
            bool aNearMouth = aMouthDX * aMouthDX + aMouthDY * aMouthDY < (double)aPos.mLexMouthGap * aPos.mLexMouthGap;
            if ((mPieces[0].mY < aBoard.ToShown(aPos.At(0x17c)) + aBoard.mY || aNearMouth) && aLex.IsIdleOrSparkling())
            {
                aLex.PlayAnim(0x15, false);
            }
            Piece aPrev = null;
            for (int i = 0; i < mPieces.Count; i++)
            {
                Piece aPiece = mPieces[i];
                int aBaseX;
                int aBaseY;
                if (aPrev == null)
                {
                    aBaseX = aPos.At(0x180) + aLex.mX - aTileW / 2;
                    aBaseY = aPos.At(0x184) + aLex.mY - aTileW / 2;
                }
                else
                {
                    aBaseX = (int)aPrev.mX;
                    aBaseY = (int)aPrev.mY;
                }
                aPrev = aPiece;
                aPiece.mField9c *= 0.98;
                double aTargetX = aBaseX + (int)(aTileW * aPiece.mField9c);
                if (aPiece.mX != aTargetX)
                {
                    aPiece.mX += (int)(aTargetX - aPiece.mX) * aPiece.mField14;
                    aPiece.mField14 += 0.003;
                }
                double aTargetY = aBaseY + (int)(aTileH * aPiece.mField9c);
                if (aPiece.mY != aTargetY)
                {
                    aPiece.mY += (int)(aTargetY - aPiece.mY) * aPiece.mVelY;
                    aPiece.mVelY += 0.003;
                }
                if (System.Math.Abs((int)(aPiece.mY - aTargetY)) < 2 && System.Math.Abs((int)(aPiece.mX - aTargetX)) < 2)
                {
                    if (aPiece.mType == Piece.TYPE_FIRE)
                    {
                        aBoard.mBurpTimer = 0x32;
                    }
                    aPiece.mField14 = 0;
                    aPiece.mVelY = 0;
                    mPieces.RemoveAt(i--);
                    if (mAge - mLastChomp > 3)
                    {
                        anApp.PlaySound(Res.SOUND_CHOMP);
                        mLastChomp = mAge;
                    }
                    continue;
                }
                if (aPiece.mType == Piece.TYPE_BONUS || aPiece.mType == Piece.TYPE_GOLD)
                {
                    if (mAge % 6 == 0)
                    {
                        Sparkle aSparkle = new Sparkle();
                        aSparkle.mX = aPiece.mX + mMgr.mRand.Next() % (uint)aTileW - aTileW / 2;
                        aSparkle.mY = aPiece.mY + mMgr.mRand.Next() % (uint)aTileH - aTileH / 2;
                        mMgr.Add(aSparkle);
                    }
                }
                else if (aPiece.mType == Piece.TYPE_FIRE && mAge % 3 == 0)
                {
                    Ember anEmber = aBoard.mEmbers.Add();
                    anEmber.mUnscaled = true;
                    anEmber.mType = 0;
                    anEmber.mX = aPiece.mX + mMgr.mRand.Next() % (uint)aTileW;
                    anEmber.mY = aPiece.mY + mMgr.mRand.Next() % (uint)aTileH;
                    anEmber.mVelX = (mMgr.mRand.Next() % 1000 - 500.0) / 800.0;
                    anEmber.mVelY = (mMgr.mRand.Next() % 1000 - 1000.0) / 1500.0;
                    anEmber.mLife = 0.95;
                    anEmber.mDecay = mMgr.mRand.Next() % 1000 / 1000.0 * 0.01 + 0.006;
                }
            }
        }

        /// <summary>vf2 (0x1a464): the blank tile back, scaled.</summary>
        public override void Draw(Graphics g)
        {
            Image anImage = AtlasResourceBase.IMAGE_GROUP31_17;
            foreach (Piece aPiece in mPieces)
            {
                g.DrawImage(anImage, (int)aPiece.mX, (int)aPiece.mY, (int)(anImage.mWidth * aPiece.mField9c), (int)(anImage.mHeight * aPiece.mField9c));
            }
        }
    }
}
