namespace Sexy
{
    /// <summary>
    /// Sexy::InterfaceWidget (0x33ab4): base of Bookworm's screens. BookWormApp keeps 19 of them
    /// (app + 0xd60) and moves them all through its interface states together: each gets
    /// <see cref="SetInterfaceState"/> with its own target for the new state (0 = not shown).
    /// </summary>
    public class InterfaceWidget : Widget
    {
        public BookWormApp mApp; // 0x78

        public int mInterfaceState; // 0x7c

        /// <summary>Where the widget goes in this state (0: away, removed when the move ends).</summary>
        public int mInterfaceTarget; // 0x80

        public int mInterfaceParam; // 0x84

        public InterfaceWidget(BookWormApp theApp)
        {
            mApp = theApp;
            mInterfaceState = 0;
            mInterfaceTarget = 0;
            mInterfaceParam = 0;
        }

        /// <summary>
        /// vf80 (0x33ae0): disabled while it moves; coming in, it is added to the widget manager
        /// (<see cref="AttachToManager"/>) then <see cref="InterfaceStateIn"/> starts the move; going away,
        /// <see cref="InterfaceStateOut"/>.
        /// </summary>
        public virtual void SetInterfaceState(int theState, int theTarget, int theParam)
        {
            SetDisabled(true);
            if ((mInterfaceTarget == 0) != (theTarget == 0))
            {
                if (theTarget == 0)
                {
                    InterfaceStateOut(theState, 0, theParam);
                }
                else
                {
                    AttachToManager(theState, theTarget, theParam);
                    InterfaceStateIn(theState, theTarget, theParam);
                }
            }
            mInterfaceState = theState;
            mInterfaceTarget = theTarget;
            mInterfaceParam = theParam;
        }

        /// <summary>vf81 (0x33b5c): the move has ended; a widget sent away leaves its parent, else it is enabled again.</summary>
        public virtual void InterfaceStateDone()
        {
            if (mInterfaceTarget == -1)
            {
                return;
            }
            if (mInterfaceTarget == 0)
            {
                mParent?.RemoveWidget(this);
            }
            else
            {
                SetDisabled(false);
            }
        }

        /// <summary>vf82 (0x33b8c): moves the widget from its parent to the top of the widget manager.</summary>
        public virtual void AttachToManager(int theState, int theTarget, int theParam)
        {
            mParent?.RemoveWidget(this);
            mApp.mWidgetManager.AddWidget(this);
            SetVisible(true);
        }

        /// <summary>vf83: the widget is coming in (subclasses start their slide here).</summary>
        /// <summary>
        /// Port addition (dynamic aspect): the screen changed shape (BookWormApp.ApplyLayout, ScreenPos laid out
        /// again). A screen that is up is attached again, which re-reads its places from ScreenPos.
        /// </summary>
        public virtual void Relayout()
        {
            if (mInterfaceTarget != 0 && mParent != null)
            {
                AttachToManager(mInterfaceState, mInterfaceTarget, mInterfaceParam);
            }
        }

        public virtual void InterfaceStateIn(int theState, int theTarget, int theParam)
        {
        }

        /// <summary>vf84: the widget is going away.</summary>
        public virtual void InterfaceStateOut(int theState, int theTarget, int theParam)
        {
        }

        /// <summary>vf85: no-op hook.</summary>
        public virtual void InterfaceVf85()
        {
        }

        /// <summary>vf86: no-op hook.</summary>
        public virtual void InterfaceVf86()
        {
        }

        /// <summary>vf44 (0x33bc8): disabling a screen disables its children too.</summary>
        public override void SetDisabled(bool isDisabled)
        {
            base.SetDisabled(isDisabled);
            foreach (Widget aChild in mWidgets)
            {
                aChild.SetDisabled(isDisabled);
            }
        }
    }
}
