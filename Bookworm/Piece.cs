namespace Sexy
{
    /// <summary>
    /// Sexy::Piece (ctor 0x2620, 0xe0 bytes): one letter tile of the board. Field comments give the original
    /// offsets; the meaning of some is still being worked out from Board::Update.
    /// </summary>
    public class Piece
    {
        public const int TYPE_NORMAL = 0;

        /// <summary>Green bonus tile.</summary>
        public const int TYPE_BONUS = 1;

        /// <summary>Gold tile.</summary>
        public const int TYPE_GOLD = 2;

        public const int TYPE_FIRE = 3;

        /// <summary>Gem tiles (translucent, with a glow).</summary>
        public const int TYPE_SAPPHIRE = 4;

        public const int TYPE_DIAMOND = 5;

        /// <summary>Live pieces (0x11c3f0).</summary>
        public static int gPieceCount;

        public double mX; // 0x04

        public double mY; // 0x0c

        public double mField14; // 0x14

        /// <summary>Fall speed (0x1c).</summary>
        public double mVelY;

        public int mField24; // 0x24

        public int mField28; // 0x28

        public int mCol; // 0x2c

        public int mRow; // 0x30

        public bool mField34; // 0x34

        public int mField38 = 1; // 0x38

        public bool mField3c; // 0x3c

        public bool mField3d; // 0x3d

        public double mField40; // 0x40

        public double mField48; // 0x48

        public int mField50; // 0x50

        public int mField54; // 0x54

        public bool mField58; // 0x58

        public bool mField59; // 0x59

        public int mField5c; // 0x5c

        public int mField60; // 0x60

        public int mField64; // 0x64

        public bool mField68; // 0x68

        public double mField6c; // 0x6c

        public double mField74; // 0x74

        public int mField7c; // 0x7c

        public int mField80; // 0x80

        public int mField84; // 0x84

        public int mField88; // 0x88

        public bool mField8c; // 0x8c

        public int mField90; // 0x90

        public int mField94; // 0x94

        /// <summary>The letter ('A'-'Z'; 'Q' is drawn as "Qu") (0x98).</summary>
        public char mLetter = (char)0xff;

        /// <summary>The letter shown once a flip (scramble) has turned the tile (0x99).</summary>
        public char mNextLetter = (char)0xff;

        /// <summary>Part of the word being spelled (0x9a).</summary>
        public bool mSelected;

        public double mField9c = 1.0; // 0x9c

        /// <summary>Being removed: sinks out of the board (0xa4).</summary>
        public bool mRemoving;

        /// <summary>How far it has sunk (0xa8).</summary>
        public int mSinkOffset;

        /// <summary>Frames before it starts sinking (0xac).</summary>
        public int mSinkDelay;

        /// <summary>Arcade: chosen to burn (0xb0).</summary>
        public bool mArcadeBurn;

        public int mFieldB4; // 0xb4

        /// <summary>Cycles an 8-cel animation (0xb8, cel at 0xbc).</summary>
        public bool mAnimating;

        public int mAnimCel; // 0xbc

        /// <summary>Wobbles (burning above) (0xc0, amount 0xc4).</summary>
        public bool mShaking;

        public double mShakeAmount; // 0xc4

        /// <summary>Flipping to a new letter (0xcc, progress 0xd0).</summary>
        public bool mFlipping;

        public double mFlipTime; // 0xd0

        /// <summary>TYPE_* (0xd8).</summary>
        public int mType;

        /// <summary>Burn stage, set when a fire tile sits on top (0xdc).</summary>
        public int mBurnStage;

        /// <summary>The turns a fire tile takes to burn through a tile of theType (normal 1 ... diamond 5; fire 0).</summary>
        public static int BurnStageFor(int theType)
        {
            switch (theType)
            {
            case TYPE_NORMAL: return 1;
            case TYPE_BONUS: return 2;
            case TYPE_GOLD: return 3;
            case TYPE_SAPPHIRE: return 4;
            case TYPE_DIAMOND: return 5;
            }
            return 0;
        }

        public Piece()
        {
            gPieceCount++;
        }
    }
}
