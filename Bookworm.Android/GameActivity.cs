using System;
using System.IO;
using Android.App;
using Android.Hardware;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Microsoft.Xna.Framework.Input;
using Sexy;

namespace Bookworm.Android
{
    // Declared in AndroidManifest.xml (not with [Activity]) under this Java name; the label is the app's
    // (ApplicationTitle).
    [Register("bookworm.android.GameActivity")]
    public class GameActivity : Microsoft.Xna.Framework.AndroidGameActivity, ISensorEventListener
    {
        private Sexy.Main mGame;

        private SensorManager mSensorManager;

        private Sensor mAccelerometer;

        /// <summary>The first sample's sensor time (ns), so the game gets small times (it keeps them as floats).</summary>
        private long mSensorTimeBase = -1;

        /// <summary>Samples waiting past this many are dropped (the game is not updating).</summary>
        private const int MAX_PENDING_SAMPLES = 64;

        private const double STANDARD_GRAVITY = 9.80665;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            Window.AddFlags(WindowManagerFlags.KeepScreenOn);
            // Let the game draw around the camera cutout too (the notch shows over it); by default the window
            // (and so the game's surface) stops short of it, leaving a black band.
            if (Build.VERSION.SdkInt >= BuildVersionCodes.R)
            {
                Window.Attributes.LayoutInDisplayCutoutMode = LayoutInDisplayCutoutMode.Always;
            }
            else if (Build.VERSION.SdkInt >= BuildVersionCodes.P)
            {
                Window.Attributes.LayoutInDisplayCutoutMode = LayoutInDisplayCutoutMode.ShortEdges;
            }
            Window.Attributes = Window.Attributes;

            // The game data is read straight from the APK's assets; resolved paths are then the asset
            // paths ("Content/...").
            ContentFS.Root = ApkContentSource.AssetRoot;
            ContentFS.Source = new ApkContentSource(this);

            // Saves live in the app's data folder (Android/data/<package>/files), falling back to
            // internal storage if shared storage is unavailable.
            string saveDir = GetExternalFilesDir(null)?.AbsolutePath ?? FilesDir.AbsolutePath;
            Directory.CreateDirectory(saveDir);

            // Text fields open the system keyboard prompt, as iOS showed its keyboard.
            EditManager.TextPromptHandler = (theText, theDone) =>
                KeyboardInput.Show("Bookworm", "", theText).ContinueWith(t => theDone(t.IsCompletedSuccessfully ? t.Result : null));

            ImageFileLoader.PlatformDecoder = DecodeImage;
            // The layout follows the screen's shape (Constants.ChooseLayout), and again when it turns.
            Sexy.Main.NativeScreenSize = GetScreenSize();
            Sexy.Main.AppFactory = game => new BookWormApp(game);
            // Quitting closes the app for real (the game has already saved), so reopening it starts fresh.
            Sexy.Main.ExitHandler = () => RunOnUiThread(() =>
            {
                FinishAndRemoveTask();
                global::Android.OS.Process.KillProcess(global::Android.OS.Process.MyPid());
            });
            // Links (More Games) open in the browser or, for Google Play links, the Play Store app.
            Sexy.Main.OpenUrlHandler = theUrl => RunOnUiThread(() =>
            {
                try
                {
                    Intent intent = new Intent(Intent.ActionView, global::Android.Net.Uri.Parse(theUrl));
                    intent.AddFlags(ActivityFlags.NewTask);
                    StartActivity(intent);
                }
                catch (ActivityNotFoundException)
                {
                    Toast.MakeText(this, "No app can open " + theUrl, ToastLength.Long).Show();
                }
            });
            // Shaking the device scrambles, as on iOS: the accelerometer feeds BookWormApp's shake detector.
            mSensorManager = (SensorManager)GetSystemService(SensorService);
            mAccelerometer = mSensorManager?.GetDefaultSensor(SensorType.Accelerometer);
            // The kept texture set is read before the layout is chosen (Main's constructor).
            Sexy.Constants.StorageDir = saveDir;
            mGame = new Sexy.Main();
            mGame.applicationStoragePath = saveDir;
            View view = (View)mGame.Services.GetService(typeof(View));
            SetContentView(view);
            HideSystemUi();
            // The back button goes to the game instead of closing the activity.
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
            {
                OnBackInvokedDispatcher.RegisterOnBackInvokedCallback(0, new BackCallback());
            }
            mGame.Run();
        }

#pragma warning disable CA1422, CS0672
        public override void OnBackPressed()
        {
            Sexy.Main.BackRequested = true;
        }
#pragma warning restore CA1422, CS0672

        private class BackCallback : Java.Lang.Object, global::Android.Window.IOnBackInvokedCallback
        {
            public void OnBackInvoked()
            {
                Sexy.Main.BackRequested = true;
            }
        }

        protected override void OnResume()
        {
            base.OnResume();
            if (mAccelerometer != null)
            {
                mSensorManager.RegisterListener(this, mAccelerometer, SensorDelay.Game);
            }
        }

        protected override void OnPause()
        {
            mSensorManager?.UnregisterListener(this);
            base.OnPause();
        }

        public void OnAccuracyChanged(Sensor sensor, [GeneratedEnum] SensorStatus accuracy)
        {
        }

        /// <summary>
        /// An accelerometer sample, in m/s² along the device's axes (x right, y up, z out of the screen; gravity
        /// reads +9.8 upwards). iOS gave g with gravity reading -1 downwards, hence the -1/9.8.
        /// </summary>
        public void OnSensorChanged(SensorEvent e)
        {
            if (e?.Sensor?.Type != SensorType.Accelerometer || e.Values == null || e.Values.Count < 3)
            {
                return;
            }
            if (mSensorTimeBase < 0)
            {
                mSensorTimeBase = e.Timestamp;
            }
            if (Sexy.Main.AccelerometerSamples.Count >= MAX_PENDING_SAMPLES)
            {
                return;
            }
            Sexy.Main.AccelerometerSamples.Enqueue(new double[]
            {
                (e.Timestamp - mSensorTimeBase) / 1e9,
                -e.Values[0] / STANDARD_GRAVITY,
                -e.Values[1] / STANDARD_GRAVITY,
                -e.Values[2] / STANDARD_GRAVITY
            });
        }

        public override void OnWindowFocusChanged(bool hasFocus)
        {
            base.OnWindowFocusChanged(hasFocus);
            if (hasFocus)
            {
                HideSystemUi();
                UpdateSafeInsetTop();
            }
        }

        public override void OnConfigurationChanged(global::Android.Content.Res.Configuration newConfig)
        {
            base.OnConfigurationChanged(newConfig);
            // Turned: the notch is at another edge now.
            Window?.DecorView?.Post(UpdateSafeInsetTop);
        }

        /// <summary>How far the camera cutout reaches down from the top as the device is held (Sexy.Main.SafeInsetTop).</summary>
        private void UpdateSafeInsetTop()
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.P)
            {
                return;
            }
            DisplayCutout aCutout = Window?.DecorView?.RootWindowInsets?.DisplayCutout;
            Sexy.Main.SafeInsetTop = aCutout?.SafeInsetTop ?? 0;
        }

        /// <summary>
        /// Decodes an image file's bytes on the CPU with BitmapFactory (as MonoGame's Android FromStream
        /// does, but without uploading to the GPU and reading back). Pixels stay straight alpha, packed as
        /// SurfaceFormat.Color (ABGR in a uint).
        /// </summary>
        private static uint[] DecodeImage(byte[] theFileData, out int theWidth, out int theHeight)
        {
            theWidth = 0;
            theHeight = 0;
            BitmapFactory.Options options = new BitmapFactory.Options
            {
                InScaled = false,
                InDither = false,
                InPremultiplied = false,
                InPreferredConfig = Bitmap.Config.Argb8888
            };
            using (Bitmap bitmap = BitmapFactory.DecodeByteArray(theFileData, 0, theFileData.Length, options))
            {
                if (bitmap == null)
                {
                    return null;
                }
                theWidth = bitmap.Width;
                theHeight = bitmap.Height;
                int[] argb = new int[theWidth * theHeight];
                bitmap.GetPixels(argb, 0, theWidth, 0, 0, theWidth, theHeight);
                uint[] pixels = new uint[argb.Length];
                for (int i = 0; i < argb.Length; i++)
                {
                    uint p = (uint)argb[i];
                    // ARGB -> ABGR: swap red and blue.
                    pixels[i] = (p & 0xFF00FF00u) | ((p >> 16) & 0xFFu) | ((p & 0xFFu) << 16);
                }
                return pixels;
            }
        }

        /// <summary>
        /// The whole display as it is held now, cutout included (the system bars are hidden while playing). Its
        /// shape picks the layout; turning the device lays the game out again (Main.OnResize).
        /// </summary>
        private Microsoft.Xna.Framework.Point GetScreenSize()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.R)
            {
                var bounds = WindowManager.MaximumWindowMetrics.Bounds;
                return new Microsoft.Xna.Framework.Point(bounds.Width(), bounds.Height());
            }
#pragma warning disable CA1422
            var metrics = new global::Android.Util.DisplayMetrics();
            WindowManager.DefaultDisplay.GetRealMetrics(metrics);
#pragma warning restore CA1422
            return new Microsoft.Xna.Framework.Point(metrics.WidthPixels, metrics.HeightPixels);
        }

        private void HideSystemUi()
        {
            // The insets controller needs the window's DecorView; only call this after SetContentView.
            if (Window.DecorView == null)
            {
                return;
            }
            if (Build.VERSION.SdkInt >= BuildVersionCodes.R)
            {
                Window.SetDecorFitsSystemWindows(false);
                IWindowInsetsController controller = Window.InsetsController;
                if (controller != null)
                {
                    controller.Hide(WindowInsets.Type.SystemBars());
                    controller.SystemBarsBehavior = (int)WindowInsetsControllerBehavior.ShowTransientBarsBySwipe;
                }
            }
            else
            {
#pragma warning disable CA1422
                Window.DecorView.SystemUiVisibility = (StatusBarVisibility)(SystemUiFlags.LayoutStable |
                    SystemUiFlags.LayoutHideNavigation | SystemUiFlags.LayoutFullscreen | SystemUiFlags.HideNavigation |
                    SystemUiFlags.Fullscreen | SystemUiFlags.ImmersiveSticky);
#pragma warning restore CA1422
            }
        }
    }
}
