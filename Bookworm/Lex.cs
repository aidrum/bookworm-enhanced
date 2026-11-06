using System;
using Microsoft.Xna.Framework;

namespace Sexy
{
    /// <summary>
    /// Sexy::Lex (ctor 0x1df28, screen 18, BookWormApp + 0xda8/0xdbc): the bookworm. He plays frame
    /// animations from a table of (image id, duration) pairs, blinks and fidgets at random, and moves between
    /// his three places (0 away, 1 on the menu's pedestal, 2 beside the board) along a Timeline.
    /// </summary>
    public class Lex : InterfaceWidget
    {
        /// <summary>His usual frame, and the one while greyed (0x279b, 0x27a6).</summary>
        public const int IMAGE_IDLE = 0x279b;

        public const int IMAGE_BLINK = 0x279c;

        public const int IMAGE_IDLE_GREY = 0x27a6;

        public const int IMAGE_GREY_FIDGET = 0x27a7;

        public const int ANIM_NONE = -1;

        public const int ANIM_WELCOME = 6;

        /// <summary>The animation the sparkles show with (0x13).</summary>
        public const int ANIM_SPARKLE = 0x13;

        public const int ANIM_GREY_FIDGET = 0x19;

        public const int ANIM_BLINK = 0x1b;

        /// <summary>
        /// The animations (0xcd81c, 0x80 bytes each): up to 16 (atlas image id, duration in half-ticks... x2
        /// frames) pairs, ending with -1.
        /// </summary>
        private static readonly int[][] ANIMS =
        {
            new[] { 10139, 1, 10144, 8, 10140, 6, 10139, 3, 10140, 3, 10139, 2, -1, 0 },
            new[] { 10139, 1, 10144, 3, 10139, 3, 10144, 4, 10145, 2, 10139, 2, 10144, 3, 10146, 9, 10139, 2, -1, 0 },
            new[] { 10139, 1, 10144, 8, 10139, 6, 10144, 8, 10147, 8, 10148, 8, 10147, 8, 10139, 2, -1, 0 },
            new[] { 10139, 1, 10144, 5, 10139, 6, 10144, 8, 10139, 2, -1, 0 },
            new[] { 10139, 1, 10144, 4, 10139, 5, 10146, 6, 10144, 4, 10139, 2, -1, 0 },
            new[] { 10139, 1, 10144, 3, 10139, 4, 10146, 5, 10144, 3, 10139, 2, -1, 0 },
            new[] { 10145, 3, 10144, 4, 10147, 4, 10144, 4, 10139, 4, 10144, 4, 10139, 3, 10144, 7, 10147, 6, 10139, 8, -1, 0 },
            new[] { 10147, 3, 10146, 7, 10145, 4, 10144, 7, 10139, 5, -1, 0 },
            new[] { 10144, 3, 10139, 3, 10147, 3, 10144, 3, 10139, 2, 10144, 2, 10139, 2, 10144, 3, 10139, 3, 10140, 3, 10139, 3, -1, 0 },
            new[] { 10144, 2, 10139, 2, 10147, 2, 10144, 2, 10139, 2, 10144, 2, 10139, 1, 10146, 3, 10139, 3, 10140, 3, 10139, 3, -1, 0 },
            new[] { 10139, 1, 10144, 3, 10139, 3, 10146, 2, 10139, 3, 10144, 3, 10139, 2, 10144, 2, 10139, 3, -1, 0 },
            new[] { 10139, 1, 10140, 4, 10142, 4, 10143, 1, 10139, 2, 10146, 8, 10140, 3, 10144, 8, 10139, 4, 10142, 2, 10139, 2, 10144, 4, 10139, 3, -1, 0 },
            new[] { 10139, 1, 10140, 2, 10141, 2, 10140, 2, 10139, 1, 10144, 4, 10139, 3, 10144, 4, 10139, 3, 10146, 6, 10139, 2, 10144, 3, 10139, 3, -1, 0 },
            new[] { 10140, 2, 10142, 2, 10141, 1, 10146, 3, 10144, 3, 10139, 3, 10144, 2, 10139, 2, 10144, 2, 10139, 2, 10144, 3, 10139, 4, 10144, 3, 10139, 3, -1, 0 },
            new[] { 10144, 4, 10139, 4, 10144, 4, 10139, 4, 10144, 3, 10146, 2, 10139, 3, 10139, 3, -1, 0 },
            new[] { 10142, 1, 10141, 1, 10140, 1, 10144, 4, 10145, 4, 10144, 5, 10139, 4, 10145, 2, 10144, 5, 10139, 5, -1, 0 },
            new[] { 10139, 1, 10145, 3, 10144, 19, 10147, 1, 10146, 10, 10144, 5, 10139, 5, -1, 0 },
            new[] { 10144, 5, 10139, 3, 10140, 3, 10144, 7, 10139, 3, 10142, 2, 10144, 3, 10139, 3, -1, 0 },
            new[] { 10144, 4, 10139, 5, 10144, 5, 10139, 4, 10144, 4, 10139, 5, -1, 0 },
            new[] { 10148, 50, 10149, 10, -1, 0 },
            new[] { 10146, 9, 10144, 3, -1, 0 },
            new[] { 10152, 100000, -1, 0 },
            new[] { 10152, 1, 10147, 5, 10139, 1, -1, 0 },
            new[] { 10152, 100000, -1, 0 },
            new[] { 10150, 1, -1, 0 },
            new[] { 10151, 5, 10150, 1, -1, 0 },
            new[] { 10139, 1, -1, 0 },
            new[] { 10140, 5, 10139, 1, -1, 0 }
        };

        /// <summary>The slide that ends a move between places, in ticks (the event that frees the interface).</summary>
        private const int MOVE_TICKS = 0x32;

        private static readonly Random gRandom = new Random();

        public int mAnim; // 0x90

        public int mAnimFrame; // 0x94

        public int mAnimTicks; // 0x98

        public int mImageId; // 0x9c

        public bool mAnimLoop; // 0xa0

        /// <summary>Greyed out (game over): drawn over the grey Lex, with grey frames (0xa1).</summary>
        public bool mGrey; // 0xa1

        /// <summary>Shows the sparkle cels during ANIM_SPARKLE (0xa2).</summary>
        public bool mSparkle; // 0xa2

        public int mSparkleCel; // 0xa4

        public int mListenerId; // 0xa8

        public ButtonListener mListener; // 0xac

        public Timeline<Lex> mTimeline; // 0xb0

        /// <summary>Where the timeline puts him (0xec).</summary>
        public TPoint mTimelinePos;

        /// <summary>His opacity, below 255 while fading (0xf4).</summary>
        public int mAlpha = 0xff;

        /// <summary>What he will say once the word balloon is free (0xf8, 0xfc).</summary>
        public string mPendingSpeech = "";

        public string mPendingSpeech2 = "";

        public Lex(BookWormApp theApp) : base(theApp)
        {
            mTimeline = new Timeline<Lex>(this);
            mGrey = false;
            ResetAnim();
            mHasAlpha = true;
            mMouseInsets.mLeft = 0x1c;
            mMouseInsets.mRight = 0x12;
            Image anImage = AtlasResourceBase.IMAGE_GROUP1_06;
            Resize(theApp.mScreenPos.At(0x88), theApp.mScreenPos.At(0x8c), anImage.mWidth, anImage.mHeight);
        }

        /// <summary>Port (dynamic aspect): straight to his place for the current target (1 menu, 2 board, else off the left).</summary>
        public override void Relayout()
        {
            base.Relayout();
            ScreenPos aPos = mApp.mScreenPos;
            int anOffset = mInterfaceTarget == 1 ? 0x80 : mInterfaceTarget == 2 ? 0x90 : 0x88;
            mTimelinePos = new TPoint(aPos.At(anOffset), aPos.At(anOffset + 4) - (mInterfaceTarget == 2 ? mApp.GetLexLift(mInterfaceState) : 0));
            Move(mTimelinePos.mX, mTimelinePos.mY);
        }

        /// <summary>FUN_0001f0bc: back to standing still.</summary>
        public void ResetAnim()
        {
            mAnim = ANIM_NONE;
            mAnimFrame = 0;
            mAnimTicks = 0;
            mImageId = IMAGE_IDLE;
            mAnimLoop = false;
            mSparkle = false;
            mSparkleCel = 0;
        }

        /// <summary>FUN_0001f0f0: plays an animation (looping or once).</summary>
        public void PlayAnim(int theAnim, bool theLoop)
        {
            mAnim = theAnim;
            mAnimFrame = 0;
            mAnimTicks = 0;
            mImageId = ANIMS[theAnim][0];
            mAnimLoop = theLoop;
            MarkDirty();
        }

        /// <summary>FUN_0001f120: greys him out, playing the grey fidget unless something else is playing.</summary>
        public void SetGrey()
        {
            mGrey = true;
            if ((mAnim == ANIM_NONE && !mTimeline.IsRunning()) || mAnim == ANIM_SPARKLE)
            {
                mAnim = ANIM_GREY_FIDGET;
                mAnimFrame = 0;
                mAnimTicks = 0;
                mImageId = IMAGE_GREY_FIDGET;
                mAnimLoop = false;
                MarkDirty();
            }
        }

        /// <summary>No animation and no move (the board's "anim == -1 and timeline done" checks).</summary>
        public bool IsIdle()
        {
            return mAnim == ANIM_NONE && !mTimeline.IsRunning();
        }

        /// <summary>Idle, or in the panicky sparkle loop (the board's game-over checks).</summary>
        public bool IsIdleOrSparkling()
        {
            return IsIdle() || mAnim == ANIM_SPARKLE;
        }

        /// <summary>FUN_0001e1b8.</summary>
        public void SetSparkle(bool theSparkle)
        {
            if (theSparkle != mSparkle)
            {
                mSparkleCel = 0;
            }
            mSparkle = theSparkle;
        }

        /// <summary>FUN_0001f180: something to say once the word balloon is free.</summary>
        public void SetPendingSpeech(string theSpeech, string theSpeech2)
        {
            mPendingSpeech = theSpeech;
            mPendingSpeech2 = theSpeech2;
        }

        /// <summary>
        /// 0x1e464 (vf80): moving between places (old place * 3 + new place) runs a four-key move over
        /// ScreenPos points (0x80 menu spot, 0x88 off the left edge, 0x90 board spot, 0x98 board entry) with a
        /// fade, holding the interface lock until tick 50. With another state parameter he just jumps there.
        /// </summary>
        public override void SetInterfaceState(int theState, int theTarget, int theParam)
        {
            int anOldTarget = mInterfaceTarget;
            int anOldParam = mInterfaceParam;
            base.SetInterfaceState(theState, theTarget, theParam);
            mTimeline.Clear();
            ScreenPos aPos = mApp.mScreenPos;
            TPoint aMenu = new TPoint(aPos.At(0x80), aPos.At(0x84));
            TPoint anOffLeft = new TPoint(aPos.At(0x88), aPos.At(0x8c));
            // Port: on the menu pages past 16:9, above the page (BookWormApp.GetLexLift).
            int aLift = mApp.GetLexLift(theState);
            TPoint aBoard = new TPoint(aPos.At(0x90), aPos.At(0x94) - aLift);
            TPoint aBoardEntry = new TPoint(aPos.At(0x98), aPos.At(0x9c) - aLift);
            TPoint[] aKeys = new TPoint[4];
            int[] anAlphas = new int[4];
            int aDuration = 0;
            bool aMove;
            switch (anOldTarget * 3 + theTarget)
            {
            case 1: // away -> menu: in from the left
                aKeys = new[] { anOffLeft, anOffLeft, anOffLeft, aMenu };
                anAlphas = new[] { 0xff, 0xff, 0xff, 0xff };
                aDuration = 10;
                aMove = true;
                break;
            case 2: // away -> board: fades in
                aKeys = new[] { aBoardEntry, aBoardEntry, aBoardEntry, aBoard };
                anAlphas = new[] { 0, 0, 0, 0xff };
                aDuration = 0x14;
                aMove = true;
                break;
            case 3: // menu -> away
                aKeys = new[] { aMenu, anOffLeft, anOffLeft, anOffLeft };
                anAlphas = new[] { 0xff, 0xff, 0xff, 0xff };
                aDuration = 0x28;
                aMove = true;
                break;
            case 5: // menu -> board
                aKeys = new[] { aMenu, aMenu, aBoardEntry, aBoard };
                anAlphas = new[] { 0xff, 0, 0, 0xff };
                aDuration = 0x14;
                aMove = true;
                break;
            case 6: // board -> away: fades out
                aKeys = new[] { aBoard, aBoardEntry, aBoardEntry, aBoardEntry };
                anAlphas = new[] { 0xff, 0, 0, 0 };
                aDuration = 0x14;
                aMove = true;
                break;
            case 7: // board -> menu
                aKeys = new[] { aBoard, aBoardEntry, anOffLeft, aMenu };
                anAlphas = new[] { 0xff, 0, 0xff, 0xff };
                aDuration = 0x14;
                aMove = true;
                break;
            case 8: // board -> board
                aKeys = new[] { aBoard, aBoardEntry, aBoardEntry, aBoard };
                anAlphas = new[] { 0xff, 0, 0, 0xff };
                aDuration = 0x19;
                aMove = false;
                if (mGrey && mInterfaceState == 3)
                {
                    // Not greyed any more once the move starts.
                    Timeline<Lex>.IntChannel aGrey = mTimeline.AddIntChannel((theLex, theValue) => theLex.mGrey = theValue != 0);
                    aGrey.mKeys.SetKey(0, 1, false, false);
                    aGrey.mKeys.SetKey(0x19, 0, false, false);
                    aMove = true;
                }
                break;
            default:
                aMove = false;
                break;
            }
            WordBalloon aBalloon = mApp.mWordBalloon;
            if (anOldParam == theParam)
            {
                if (aMove)
                {
                    Timeline<Lex>.PointChannel aPath = mTimeline.AddPointChannel((theLex, thePos) => theLex.mTimelinePos = thePos);
                    // SetKey(tick, value, ease, tween), as in the original's calls.
                    aPath.mKeys.SetKey(0, aKeys[0], true, false);
                    aPath.mKeys.SetKey(aDuration, aKeys[1], false, true);
                    aPath.mKeys.SetKey(aDuration + 1, aKeys[2], false, false);
                    aPath.mKeys.SetKey(MOVE_TICKS, aKeys[3], true, true);
                    Timeline<Lex>.IntChannel aFade = mTimeline.AddIntChannel((theLex, theValue) => theLex.mAlpha = theValue);
                    aFade.mKeys.SetKey(0, anAlphas[0], false, false);
                    aFade.mKeys.SetKey(aDuration, anAlphas[1], false, true);
                    aFade.mKeys.SetKey(aDuration + 1, anAlphas[2], false, false);
                    aFade.mKeys.SetKey(MOVE_TICKS, anAlphas[3], false, true);
                    mTimeline.AddEvent(MOVE_TICKS, theLex => theLex.mApp.ReleaseInterfaceLock());
                    mTimeline.Start();
                    mApp.mInterfaceLock++;
                }
                aBalloon?.Hide();
            }
            else
            {
                Move(aKeys[3].mX, aKeys[3].mY);
                mAlpha = anAlphas[3];
                aBalloon?.HideNow();
            }
            aBalloon?.SetListener(0, null);
        }

        /// <summary>0x1ec88 (vf81): once in place he announces some menus.</summary>
        public override void InterfaceStateDone()
        {
            base.InterfaceStateDone();
            WordBalloon aBalloon = mApp.mWordBalloon;
            switch (mInterfaceState)
            {
            case 3:
                aBalloon.Say(SexyAppBase.Loc("PERSONAL BESTS!"));
                break;
            case 7:
                aBalloon.Say(SexyAppBase.Loc("OPTIONS!"));
                break;
            case 10:
                // Back on the board, a word being spelled shows again.
                if (mApp.mBoard != null && mApp.mBoard.mWordScore.mWord.Length != 0)
                {
                    aBalloon.ShowWord(mApp.mBoard.mWordScore);
                }
                break;
            case 11:
                aBalloon.Say(mApp.mBoard != null && mApp.mBoard.mState == Board.STATE_GAME_OVER ? SexyAppBase.Loc("GAME OVER!") : SexyAppBase.Loc("LEVEL UP!"));
                break;
            }
        }

        /// <summary>0x1f1a0.</summary>
        public override void Draw(Graphics g)
        {
            if (mAlpha < 0xff)
            {
                g.SetColor(new Color(255, 255, 255, mAlpha));
                g.SetColorizeImages(true);
            }
            if (mGrey)
            {
                g.DrawImage(AtlasResourceBase.IMAGE_GROUP1_04, 0, 0);
            }
            if (mImageId == IMAGE_BLINK)
            {
                g.DrawImage(AtlasResourceBase.GetImageById(IMAGE_IDLE), 0, 0);
            }
            Image anImage = AtlasResourceBase.GetImageById(mImageId);
            if (anImage != null)
            {
                g.DrawImage(anImage, 0, 0);
            }
            if (mSparkle && mAnim == ANIM_SPARKLE && !mGrey)
            {
                g.DrawImageCel(AtlasResourceBase.IMAGE_GROUP5_07, 0, 0x14, mSparkleCel);
            }
        }

        /// <summary>0x1f25c: moves along the timeline, steps the animation, and now and then blinks or fidgets.</summary>
        public override void Update()
        {
            if (mTimeline.IsRunning())
            {
                mTimeline.AddTime(1f);
                mTimeline.Tick();
                Move(mTimelinePos.mX, mTimelinePos.mY);
            }
            // While the board is up and paused, nothing else updates.
            if (mInterfaceState == 10 && mApp.mBoard != null && mApp.mBoard.IsPaused())
            {
                return;
            }
            base.Update();
            if (mSparkle && mUpdateCnt % 6 == 0)
            {
                mSparkleCel = (mSparkleCel + 1) % 8;
                MarkDirty();
            }
            if (mAnim == ANIM_NONE)
            {
                mImageId = mGrey ? IMAGE_IDLE_GREY : IMAGE_IDLE;
            }
            else
            {
                int[] anAnim = ANIMS[mAnim];
                mAnimTicks++;
                if (anAnim[mAnimFrame * 2 + 1] * 2 <= mAnimTicks)
                {
                    mAnimFrame++;
                    mAnimTicks = 0;
                    mImageId = anAnim[mAnimFrame * 2];
                    if (mImageId == -1)
                    {
                        if (!mAnimLoop)
                        {
                            mAnim = ANIM_NONE;
                            mImageId = mGrey ? IMAGE_IDLE_GREY : IMAGE_IDLE;
                        }
                        else
                        {
                            mImageId = anAnim[0];
                            mAnimFrame = 0;
                        }
                    }
                    MarkDirty();
                }
            }
            if (gRandom.Next() % 500 == 0 && mAnim == ANIM_NONE && !mTimeline.IsRunning())
            {
                mAnim = mGrey ? ANIM_GREY_FIDGET : ANIM_BLINK;
                mAnimFrame = 0;
                mAnimTicks = 0;
                mImageId = mGrey ? IMAGE_GREY_FIDGET : IMAGE_BLINK;
                mAnimLoop = false;
                MarkDirty();
            }
            if (gRandom.Next() % 4000 == 0 && mAnim == ANIM_NONE && !mTimeline.IsRunning() && !mGrey)
            {
                PlayAnim(gRandom.Next() % 2 + 0x1a, false);
            }
            if (mPendingSpeech.Length != 0 && mApp.mWordBalloon.IsFree())
            {
                mApp.mWordBalloon.ShowDefinition(mPendingSpeech2, mPendingSpeech);
                mPendingSpeech = "";
                mPendingSpeech2 = "";
            }
        }

        /// <summary>0x1f4e8.</summary>
        public override void MouseDown(int x, int y, int theClickCount)
        {
            base.MouseDown(x, y, theClickCount);
            mListener?.ButtonPress(mListenerId);
        }

        /// <summary>0x1f508: a release over him (while the touch stayed on him) is a click.</summary>
        public override void MouseUp(int x, int y, int theClickCount)
        {
            base.MouseUp(x, y, theClickCount);
            if (mIsOver)
            {
                mListener?.ButtonDepress(mListenerId);
            }
        }

        /// <summary>vf87 (0x1efe4): his animation state, for the saved game.</summary>
        public void SaveAnimState(System.IO.BinaryWriter theWriter)
        {
            theWriter.Write(mAnim);
            theWriter.Write(mAnimFrame);
            theWriter.Write(mAnimTicks);
            theWriter.Write(mImageId);
            theWriter.Write(mAnimLoop);
            theWriter.Write(mSparkle);
            theWriter.Write(mSparkleCel);
        }

        /// <summary>vf88 (0x1f050).</summary>
        public void LoadAnimState(System.IO.BinaryReader theReader)
        {
            mAnim = theReader.ReadInt32();
            mAnimFrame = theReader.ReadInt32();
            mAnimTicks = theReader.ReadInt32();
            mImageId = theReader.ReadInt32();
            mAnimLoop = theReader.ReadBoolean();
            mSparkle = theReader.ReadBoolean();
            mSparkleCel = theReader.ReadInt32();
        }
    }
}
