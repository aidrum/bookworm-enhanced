using System;
using System.Collections.Generic;

namespace Sexy
{
    /// <summary>
    /// Sexy::AbstractTimeline (0x34c94): a clock in ticks (frames) plus a float time that channels are
    /// evaluated at.
    /// </summary>
    public class AbstractTimeline
    {
        public int mTick; // 0x04

        public float mTime; // 0x08

        /// <summary>1 while running (0x0c).</summary>
        public int mRunState;

        /// <summary>vf2: from the start.</summary>
        public virtual void Start()
        {
            mTick = 0;
            mTime = 0f;
            mRunState = 1;
        }

        /// <summary>vf3.</summary>
        public virtual void Stop()
        {
            mRunState = 0;
        }

        /// <summary>vf4.</summary>
        public virtual void Resume()
        {
            mRunState = 1;
        }

        /// <summary>vf5.</summary>
        public virtual void Clear()
        {
            Stop();
            mTick = 0;
            mTime = 0f;
        }

        /// <summary>vf6.</summary>
        public bool IsActive()
        {
            return mRunState == 1;
        }

        /// <summary>vf8.</summary>
        public virtual void FireEvents()
        {
        }

        /// <summary>vf9.</summary>
        public virtual void Apply()
        {
        }

        /// <summary>FUN_00034d04: a tick; the events due fire, and if one stopped the timeline its channels settle there.</summary>
        public void Tick()
        {
            if (!IsActive())
            {
                return;
            }
            mTick++;
            FireEvents();
            if (IsActive())
            {
                return;
            }
            mTime = mTick;
            Apply();
        }

        /// <summary>FUN_00034d48: advances the channels' time.</summary>
        public void AddTime(float theTime)
        {
            if (!IsActive())
            {
                return;
            }
            mTime += theTime;
            Apply();
        }
    }

    /// <summary>
    /// Sexy::Timeline&lt;T&gt;: keyframed channels that set members of an owner, and events fired at given
    /// ticks (kept sorted, fired once each as the clock passes them).
    /// </summary>
    public class Timeline<T> : AbstractTimeline
    {
        public abstract class Channel
        {
            public abstract void Apply(T theOwner, float theTime);
        }

        /// <summary>MemberDataChannel&lt;TPoint&gt;.</summary>
        public class PointChannel : Channel
        {
            public KeyInterpolatorTPoint mKeys = KeyInterpolatorTPoint.GetNewKeyInterpolatorTPoint();

            public Action<T, TPoint> mSetter;

            public override void Apply(T theOwner, float theTime)
            {
                mSetter(theOwner, mKeys.Tick(theTime));
            }
        }

        /// <summary>MemberDataChannel&lt;int&gt; (and &lt;bool&gt;, as 0/1).</summary>
        public class IntChannel : Channel
        {
            public KeyInterpolatorInt mKeys = KeyInterpolatorInt.GetNewKeyInterpolatorInt();

            public Action<T, int> mSetter;

            public override void Apply(T theOwner, float theTime)
            {
                mSetter(theOwner, mKeys.Tick(theTime));
            }
        }

        /// <summary>MemberDataChannel&lt;float&gt;.</summary>
        public class FloatChannel : Channel
        {
            public KeyInterpolatorFloat mKeys = KeyInterpolatorFloat.GetNewKeyInterpolatorFloat();

            public Action<T, float> mSetter;

            public override void Apply(T theOwner, float theTime)
            {
                mSetter(theOwner, mKeys.Tick(theTime));
            }
        }

        private struct Event
        {
            public int mTick;

            public int mOrder;

            public Action<T> mAction;
        }

        public T mOwner; // 0x10

        private readonly List<Event> mEvents = new List<Event>(); // 0x14 (a set ordered by tick)

        /// <summary>The next event to fire (0x2c); mEvents.Count = at the end.</summary>
        private int mNextEvent;

        public List<Channel> mChannels = new List<Channel>(); // 0x30

        public Timeline(T theOwner)
        {
            mOwner = theOwner;
        }

        public PointChannel AddPointChannel(Action<T, TPoint> theSetter)
        {
            PointChannel aChannel = new PointChannel { mSetter = theSetter };
            mChannels.Add(aChannel);
            return aChannel;
        }

        public IntChannel AddIntChannel(Action<T, int> theSetter)
        {
            IntChannel aChannel = new IntChannel { mSetter = theSetter };
            mChannels.Add(aChannel);
            return aChannel;
        }

        public FloatChannel AddFloatChannel(Action<T, float> theSetter)
        {
            FloatChannel aChannel = new FloatChannel { mSetter = theSetter };
            mChannels.Add(aChannel);
            return aChannel;
        }

        public void AddEvent(int theTick, Action<T> theAction)
        {
            Event anEvent = new Event { mTick = theTick, mOrder = mEvents.Count, mAction = theAction };
            int i = mEvents.Count;
            while (i > 0 && mEvents[i - 1].mTick > theTick)
            {
                i--;
            }
            mEvents.Insert(i, anEvent);
        }

        /// <summary>Timeline::Clear: stops, and drops the events and channels.</summary>
        public override void Clear()
        {
            base.Clear();
            mEvents.Clear();
            mNextEvent = 0;
            mChannels.Clear();
        }

        /// <summary>Timeline::Start.</summary>
        public override void Start()
        {
            base.Start();
            mNextEvent = 0;
        }

        /// <summary>Timeline::IsRunning: running with events still to fire.</summary>
        public bool IsRunning()
        {
            return IsActive() && !AtEnd();
        }

        /// <summary>Timeline::AtEnd.</summary>
        public bool AtEnd()
        {
            return mNextEvent >= mEvents.Count;
        }

        /// <summary>Timeline::FireEvents: every event due by the current tick.</summary>
        public override void FireEvents()
        {
            while (mNextEvent < mEvents.Count && mEvents[mNextEvent].mTick <= mTick)
            {
                Event anEvent = mEvents[mNextEvent++];
                anEvent.mAction(mOwner);
            }
        }

        /// <summary>Timeline::Apply: every channel at the current time.</summary>
        public override void Apply()
        {
            foreach (Channel aChannel in mChannels)
            {
                aChannel.Apply(mOwner, mTime);
            }
        }
    }
}
