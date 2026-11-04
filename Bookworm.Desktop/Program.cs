using System;
using System.IO;
using Sexy;

namespace Bookworm.Desktop
{
    public static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            if (OperatingSystem.IsMacOS())
            {
                UseAppBundleContent();
            }
            // Debug aid: SEXY_STORAGE_DIR points saves at another folder (scripted test runs).
            string storage = Environment.GetEnvironmentVariable("SEXY_STORAGE_DIR") ?? GetDefaultStorageDir();
            Directory.CreateDirectory(storage);
            Sexy.Main.AppFactory = game => new BookWormApp(game);
            using (Sexy.Main game = new Sexy.Main())
            {
                game.applicationStoragePath = storage;
                game.Window.Title = "Bookworm";
                game.Run();
            }
        }

        /// <summary>
        /// Where saves live when SEXY_STORAGE_DIR isn't set: next to the executable on Windows; the user's
        /// application data folder on macOS (a .app bundle is read-only) and Linux (an AppImage is too).
        /// </summary>
        private static string GetDefaultStorageDir()
        {
            if (OperatingSystem.IsMacOS())
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Library", "Application Support", "Bookworm Enhanced");
            }
            if (OperatingSystem.IsLinux())
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData,
                    Environment.SpecialFolderOption.Create), "Bookworm Enhanced");
            }
            return AppContext.BaseDirectory;
        }

        /// <summary>In a macOS .app the game data lives in Contents/Resources/Content.</summary>
        private static void UseAppBundleContent()
        {
            string aBundleContent = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "Resources", "Content"));
            if (!Directory.Exists(Path.Combine(AppContext.BaseDirectory, "Content")) && Directory.Exists(aBundleContent))
            {
                ContentFS.Root = aBundleContent;
            }
        }
    }
}
