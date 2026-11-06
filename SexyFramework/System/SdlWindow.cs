using System;
using System.Runtime.InteropServices;

namespace Sexy
{
    /// <summary>
    /// Port (desktop): a few SDL window calls made directly. MonoGame's GameWindow.ClientBounds keeps a cached size
    /// that went stale, and its full screen switch (GraphicsDeviceManager.IsFullScreen) left the window wrongly
    /// sized when it was maximized afterwards (taller than the work area, under the taskbar). SDL's own
    /// "fullscreen desktop" mode saves and restores the window's size, place and maximized state itself.
    /// </summary>
    internal static unsafe class SdlWindow
    {
        private static readonly string[] LIBRARY_NAMES = { "SDL2.dll", "SDL2", "libSDL2-2.0.so.0", "libSDL2-2.0.0.dylib", "libSDL2.dylib" };

        /// <summary>SDL_WINDOW_FULLSCREEN_DESKTOP.</summary>
        private const uint FULLSCREEN_DESKTOP = 0x1001;

        /// <summary>SDL_WINDOW_FULLSCREEN (set in both full screen modes).</summary>
        private const uint FULLSCREEN = 0x1;

        private static delegate* unmanaged[Cdecl]<IntPtr, int*, int*, void> sGetWindowSize;

        private static delegate* unmanaged[Cdecl]<IntPtr, uint, int> sSetWindowFullscreen;

        private static delegate* unmanaged[Cdecl]<IntPtr, uint> sGetWindowFlags;

        private static bool sLoaded;

        private static void Load()
        {
            if (sLoaded)
            {
                return;
            }
            sLoaded = true;
            foreach (string aName in LIBRARY_NAMES)
            {
                if (!NativeLibrary.TryLoad(aName, typeof(Microsoft.Xna.Framework.Game).Assembly, null, out IntPtr aLibrary) && !NativeLibrary.TryLoad(aName, out aLibrary))
                {
                    continue;
                }
                if (NativeLibrary.TryGetExport(aLibrary, "SDL_GetWindowSize", out IntPtr aSize))
                {
                    sGetWindowSize = (delegate* unmanaged[Cdecl]<IntPtr, int*, int*, void>)aSize;
                }
                if (NativeLibrary.TryGetExport(aLibrary, "SDL_SetWindowFullscreen", out IntPtr aFullscreen))
                {
                    sSetWindowFullscreen = (delegate* unmanaged[Cdecl]<IntPtr, uint, int>)aFullscreen;
                }
                if (NativeLibrary.TryGetExport(aLibrary, "SDL_GetWindowFlags", out IntPtr aFlags))
                {
                    sGetWindowFlags = (delegate* unmanaged[Cdecl]<IntPtr, uint>)aFlags;
                }
                break;
            }
        }

        /// <summary>The window's size; false when SDL or the window is not there (the caller keeps ClientBounds).</summary>
        public static bool TryGetSize(IntPtr theWindow, out int theWidth, out int theHeight)
        {
            theWidth = theHeight = 0;
            Load();
            if (theWindow == IntPtr.Zero || sGetWindowSize == null)
            {
                return false;
            }
            int aWidth;
            int aHeight;
            sGetWindowSize(theWindow, &aWidth, &aHeight);
            theWidth = aWidth;
            theHeight = aHeight;
            return aWidth > 0 && aHeight > 0;
        }

        /// <summary>Whether the window is full screen now (false when SDL cannot say).</summary>
        public static bool IsFullscreen(IntPtr theWindow)
        {
            Load();
            return theWindow != IntPtr.Zero && sGetWindowFlags != null && (sGetWindowFlags(theWindow) & FULLSCREEN) != 0;
        }

        /// <summary>Into borderless full screen at the desktop's resolution, or back to the window as it was.
        /// False when SDL cannot do it (the caller falls back to MonoGame's switch).</summary>
        public static bool TrySetFullscreen(IntPtr theWindow, bool theFullscreen)
        {
            Load();
            if (theWindow == IntPtr.Zero || sSetWindowFullscreen == null)
            {
                return false;
            }
            return sSetWindowFullscreen(theWindow, theFullscreen ? FULLSCREEN_DESKTOP : 0) == 0;
        }
    }
}
