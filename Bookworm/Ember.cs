using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace Sexy
{
    /// <summary>
    /// Sexy::Ember (0x58 bytes, made by FUN_0001a398): one particle of the board's fire and sparkle effects. An
    /// animation strip (by mType) drawn additive and centred on its position; it falls under mGravity, fades by
    /// mDecay a frame, and dies when faded out or gone off screen.
    /// </summary>
    public class Ember
    {
        /// <summary>The strips by type: 0-2 run down (square cels), 3-9 run across; 10 is the soft glow.</summary>
        public const int TYPE_GLOW = 10;

        public EmberListMgr mMgr; // 0x00

        public int mAge; // 0x04

        public bool mDead; // 0x08

        /// <summary>Port (wide game layout): placed outside the board's own space (the score's sparkles, a
        /// flying fire tile's embers), so drawn over the scaled board rather than with it (Board.Draw).</summary>
        public bool mUnscaled;

        public double mX; // 0x0c

        public double mY; // 0x14

        public double mVelX; // 0x1c

        public double mVelY; // 0x24

        public double mGravity = 0.01; // 0x2c

        /// <summary>Life left, 1 to 0; also the glow's alpha (0x34).</summary>
        public double mLife; // 0x34

        public double mDecay; // 0x3c

        /// <summary>Frames before it starts moving (0x44).</summary>
        public int mDelay;

        /// <summary>Cycles its cels every 4th frame instead of running through them over its life (0x48).</summary>
        public bool mLoop;

        public int mFrame; // 0x4c

        public int mFrameCount; // 0x50

        public int mType; // 0x54

        private static Image GetImage(int theType)
        {
            switch (theType)
            {
            case 0: return AtlasResourceBase.IMAGE_GROUP5_16;
            case 1: return AtlasResourceBase.IMAGE_GROUP5_14;
            case 2: return AtlasResourceBase.IMAGE_GROUP5_13;
            case 3: return AtlasResourceBase.IMAGE_GROUP5_10;
            case 4: return AtlasResourceBase.IMAGE_GROUP5_06;
            case 5: return AtlasResourceBase.IMAGE_GROUP5_03;
            case 6: return AtlasResourceBase.IMAGE_GROUP5_09;
            case 7: return AtlasResourceBase.IMAGE_GROUP5_05;
            case 8: return AtlasResourceBase.IMAGE_GROUP5_02;
            case 9: return AtlasResourceBase.IMAGE_GROUP5_00;
            }
            return null;
        }

        /// <summary>FUN_0001a9f4.</summary>
        public void Update()
        {
            if (mFrameCount == 0)
            {
                if (mType == TYPE_GLOW)
                {
                    mFrameCount = 1;
                }
                else if (mType >= 0 && mType < TYPE_GLOW)
                {
                    Image anImage = GetImage(mType);
                    mFrameCount = mType < 3 ? anImage.mHeight / anImage.mWidth : anImage.mWidth / anImage.mHeight;
                }
            }
            if (mDelay > 0)
            {
                mDelay--;
                return;
            }
            if (mType == TYPE_GLOW)
            {
                mFrame = 0;
            }
            else if (mType >= 0 && mType < TYPE_GLOW)
            {
                if (!mLoop)
                {
                    double aCel = System.Math.Min(mLife * mFrameCount - 1.0, mFrameCount - 1);
                    mFrame = mFrameCount - 1 - (int)aCel;
                }
                else if ((mAge & 3) == 0)
                {
                    mFrame = (mFrame + 1) % mFrameCount;
                }
            }
            mX += mVelX;
            mVelY += mGravity;
            mY += mVelY;
            mLife -= mDecay;
            int aWidth = mMgr.mApp.mWidth;
            int aHeight = mMgr.mApp.mHeight;
            bool aGone = mLife < 0
                || (mX < 0 && mVelX <= 0) || (mX >= aWidth && mVelX >= 0)
                || (mY < 0 && mVelY <= 0) || (mY > aHeight && mVelY >= 0);
            if (aGone)
            {
                mDead = true;
            }
        }

        /// <summary>FUN_0001abd0.</summary>
        public void Draw(Graphics g)
        {
            int aX = (int)mX;
            int aY = (int)mY;
            if (mType == TYPE_GLOW)
            {
                g.SetColorizeImages(true);
                g.SetColor(new Color(255, 255, 255, (int)(mLife * 255.0)));
                g.DrawImage(AtlasResourceBase.IMAGE_GROUP5_15, aX - 0xf, aY - 0xf);
                g.SetColorizeImages(false);
                return;
            }
            if (mType == 4)
            {
                // The big flame, then its core over it at 0xb0 alpha.
                DrawCel(g, AtlasResourceBase.IMAGE_GROUP5_06, aX, aY);
                g.SetColorizeImages(true);
                g.SetColor(new Color(255, 255, 255, 0xb0));
                DrawCel(g, AtlasResourceBase.IMAGE_GROUP5_05, aX, aY, false);
                g.SetColorizeImages(false);
                return;
            }
            Image anImage = GetImage(mType);
            if (anImage != null)
            {
                DrawCel(g, anImage, aX, aY, mType < 3);
            }
        }

        private void DrawCel(Graphics g, Image theImage, int theX, int theY, bool theDown = false)
        {
            int aSize = theDown ? theImage.mWidth : theImage.mHeight;
            TRect aSrc = theDown ? new TRect(0, aSize * mFrame, aSize, aSize) : new TRect(aSize * mFrame, 0, aSize, aSize);
            g.SetDrawMode(1);
            g.DrawImage(theImage, theX - aSize / 2, theY - aSize / 2, aSrc);
            g.SetDrawMode(0);
        }
    }

    /// <summary>
    /// Sexy::EmberListMgr (board + 0xf4): the live embers, and a pool of dead ones to reuse.
    /// </summary>
    public class EmberListMgr
    {
        public BookWormApp mApp; // 0x04

        public List<Ember> mEmbers = new List<Ember>(); // 0x0c

        private List<Ember> mFree = new List<Ember>(); // 0x18

        public EmberListMgr(BookWormApp theApp)
        {
            mApp = theApp;
        }

        /// <summary>FUN_0001a398: a fresh ember (gravity 0.01, everything else zero), already live.</summary>
        public Ember Add()
        {
            Ember anEmber;
            if (mFree.Count == 0)
            {
                anEmber = new Ember();
            }
            else
            {
                anEmber = mFree[mFree.Count - 1];
                mFree.RemoveAt(mFree.Count - 1);
                anEmber.mAge = 0;
                anEmber.mDead = false;
                anEmber.mX = anEmber.mY = anEmber.mVelX = anEmber.mVelY = 0;
                anEmber.mGravity = 0.01;
                anEmber.mLife = anEmber.mDecay = 0;
                anEmber.mDelay = 0;
                anEmber.mLoop = false;
                anEmber.mUnscaled = false;
                anEmber.mFrame = anEmber.mFrameCount = anEmber.mType = 0;
            }
            anEmber.mMgr = this;
            mEmbers.Add(anEmber);
            return anEmber;
        }

        /// <summary>FUN_0001b658: moves them all; the dead go back to the pool. True if any were live.</summary>
        public bool Update()
        {
            bool anyLive = mEmbers.Count > 0;
            for (int i = 0; i < mEmbers.Count; i++)
            {
                Ember anEmber = mEmbers[i];
                anEmber.Update();
                if (anEmber.mDead)
                {
                    mFree.Add(anEmber);
                    mEmbers[i] = mEmbers[mEmbers.Count - 1];
                    mEmbers.RemoveAt(mEmbers.Count - 1);
                    i--;
                }
            }
            return anyLive;
        }

        /// <summary>FUN_0001b630.</summary>
        public void Draw(Graphics g)
        {
            foreach (Ember anEmber in mEmbers)
            {
                anEmber.Draw(g);
            }
        }

        /// <summary>Port (wide game layout): only the embers with (or without) Ember.mUnscaled.</summary>
        public void Draw(Graphics g, bool theUnscaled)
        {
            foreach (Ember anEmber in mEmbers)
            {
                if (anEmber.mUnscaled == theUnscaled)
                {
                    anEmber.Draw(g);
                }
            }
        }

        public void Clear()
        {
            mFree.AddRange(mEmbers);
            mEmbers.Clear();
        }
    }
}
