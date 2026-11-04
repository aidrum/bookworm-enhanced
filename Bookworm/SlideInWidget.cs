namespace Sexy
{
    /// <summary>
    /// Sexy::SlideInWidget (0x33d7c): a screen that slides in from an edge when its interface state shows it
    /// and back out when it hides it, one step a frame along a KeyInterpolator. While it moves it holds the
    /// app's interface lock, so the screens are told their move is done only once all have stopped.
    /// </summary>
    public class SlideInWidget : InterfaceWidget
    {
        public const int SLIDE_FROM_RIGHT = 0;

        public const int SLIDE_FROM_LEFT = 1;

        public const int SLIDE_FROM_TOP = 2;

        public const int SLIDE_FROM_BOTTOM = 3;

        /// <summary>Steps (frames) per second of slide time (0x34080).</summary>
        private const float SLIDE_STEPS_PER_SECOND = 100f;

        public bool mSlidingIn; // 0x88

        public int mSlideSteps; // 0x8c

        public int mSlideStep; // 0x90

        public KeyInterpolatorTPoint mSlidePath = KeyInterpolatorTPoint.GetNewKeyInterpolatorTPoint(); // 0x94

        public SlideInWidget(BookWormApp theApp) : base(theApp)
        {
            mSlideSteps = 0;
            mSlideStep = 0;
        }

        /// <summary>
        /// vf87 (0x33f90): slides between the widget's position and just past an edge (its parent's, or its own
        /// size away when it has none), taking theSeconds.
        /// </summary>
        public virtual void SetupSlide(int theDirection, float theSeconds)
        {
            TPoint aTo = new TPoint(mX, mY);
            TPoint aFrom = aTo;
            if (mParent == null)
            {
                switch (theDirection)
                {
                case SLIDE_FROM_RIGHT:
                    aFrom.mX = mWidth + mX;
                    break;
                case SLIDE_FROM_LEFT:
                    aFrom.mX = mX - mWidth;
                    break;
                case SLIDE_FROM_TOP:
                    aFrom.mY = mY - mHeight;
                    break;
                case SLIDE_FROM_BOTTOM:
                    aFrom.mY = mHeight + mY;
                    break;
                }
            }
            else
            {
                switch (theDirection)
                {
                case SLIDE_FROM_RIGHT:
                    aFrom.mX = mParent.mWidth;
                    break;
                case SLIDE_FROM_LEFT:
                    aFrom.mX = -mWidth;
                    break;
                case SLIDE_FROM_TOP:
                    aFrom.mY = -mHeight;
                    break;
                case SLIDE_FROM_BOTTOM:
                    aFrom.mY = mParent.mHeight;
                    break;
                }
            }
            SetupSlide(aFrom, aTo, theSeconds);
        }

        /// <summary>vf88 (0x3400c): an eased slide from theFrom to theTo; the widget starts at theFrom.</summary>
        public virtual void SetupSlide(TPoint theFrom, TPoint theTo, float theSeconds)
        {
            mSlideSteps = (int)(theSeconds * SLIDE_STEPS_PER_SECOND);
            mSlideStep = 0;
            mSlidePath.Clear();
            mSlidePath.SetKey(0, theFrom, false, false);
            mSlidePath.SetKey(mSlideSteps, theTo, true, true);
            Move(theFrom.mX, theFrom.mY);
        }

        /// <summary>Port: attached again (InterfaceWidget.Relayout) and put straight at the end of its slide in.</summary>
        public override void Relayout()
        {
            bool anUp = mInterfaceTarget != 0 && mParent != null;
            base.Relayout();
            if (anUp && mSlideSteps > 0)
            {
                mSlidingIn = true;
                mSlideStep = mSlideSteps;
                TPoint aPos = mSlidePath.Tick(mSlideStep);
                Move(aPos.mX, aPos.mY);
            }
        }

        /// <summary>vf83 (0x34084): coming in. With the state's own parameter it slides, else it jumps in place.</summary>
        public override void InterfaceStateIn(int theState, int theTarget, int theParam)
        {
            mSlidingIn = true;
            SetVisible(true);
            if (mInterfaceParam == theParam)
            {
                if (mSlideStep == 0)
                {
                    SlideInStarted();
                    mApp.mInterfaceLock++;
                }
                return;
            }
            mSlideStep = mSlideSteps;
            TPoint aPos = mSlidePath.Tick(mSlideStep);
            Move(aPos.mX, aPos.mY);
        }

        /// <summary>vf84 (0x34104): going away, sliding back out or jumping to the start.</summary>
        public override void InterfaceStateOut(int theState, int theTarget, int theParam)
        {
            mSlidingIn = false;
            if (mInterfaceParam == theParam)
            {
                if (mSlideStep == mSlideSteps)
                {
                    SlideOutStarted();
                    mApp.mInterfaceLock++;
                }
                return;
            }
            mSlideStep = 0;
            TPoint aPos = mSlidePath.Tick(0);
            Move(aPos.mX, aPos.mY);
        }

        /// <summary>0x34160: one step of the slide; the last one ends it and releases the interface lock.</summary>
        public override void Update()
        {
            base.Update();
            if (!mVisible)
            {
                return;
            }
            if (mSlidingIn)
            {
                if (mSlideStep >= mSlideSteps)
                {
                    return;
                }
                mSlideStep++;
                TPoint aPos = mSlidePath.Tick(mSlideStep);
                Move(aPos.mX, aPos.mY);
                SlideStep();
                if (mSlideStep != mSlideSteps)
                {
                    return;
                }
                SlideInDone();
            }
            else
            {
                if (mSlideStep < 1)
                {
                    return;
                }
                mSlideStep--;
                TPoint aPos = mSlidePath.Tick(mSlideStep);
                Move(aPos.mX, aPos.mY);
                SlideStep();
                if (mSlideStep != 0)
                {
                    return;
                }
                SlideOutDone();
            }
            mApp.ReleaseInterfaceLock();
        }

        /// <summary>vf89.</summary>
        public virtual void SlideInStarted()
        {
        }

        /// <summary>vf90.</summary>
        public virtual void SlideOutStarted()
        {
        }

        /// <summary>vf91: after each step.</summary>
        public virtual void SlideStep()
        {
        }

        /// <summary>vf92.</summary>
        public virtual void SlideInDone()
        {
        }

        /// <summary>vf93.</summary>
        public virtual void SlideOutDone()
        {
        }
    }
}
