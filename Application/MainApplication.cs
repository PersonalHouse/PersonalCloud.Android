using System;
using System.IO;

using Android.App;

using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Microsoft.Extensions.Logging;

using Sentry;
using Sentry.Protocol;

using SQLite;

using Unishare.Apps.Common;
using Unishare.Apps.Common.Models;

namespace Unishare.Apps.DevolMobile
{
    [Application(Name = "com.daoyehuo.UnishareLollipop.MainApplication", Icon = "@mipmap/ic_launcher", RoundIcon = "@mipmap/ic_launcher_round", Label = "@string/app_name",
        SupportsRtl = false, Theme = "@style/AppTheme", AllowBackup = true)]
    public class MainApplication : Application
    {
        public MainApplication(IntPtr reference, Android.Runtime.JniHandleOwnership transfer) : base(reference, transfer)
        {
        }

        private IDisposable sentry;

        public override void OnCreate()
        {
            base.OnCreate();

            SQLitePCL.Batteries_V2.Init();

            AppCenter.Start("2069a171-32ec-4432-b8d5-31d5ce74fb82", typeof(Analytics), typeof(Crashes));
            sentry = SentrySdk.Init(options => {
                options.Dsn = new Dsn("https://d0a8d714e2984642a530aa7deaca3498@sentry.io/5174354");
                options.Environment = "Android";
                options.Release = this.GetPackageVersion();
            });
            SentrySdk.ConfigureScope(scope => {
                scope.SetTag("manufacturer", Android.OS.Build.Manufacturer);
                scope.SetTag("model", Android.OS.Build.Model);

                var deviceId = Globals.Database.LoadSetting(UserSettings.DeviceId);
                if (string.IsNullOrEmpty(deviceId)) return;
                scope.User = new User {
                    Id = deviceId
                };
            });
            Globals.Loggers = new LoggerFactory().AddSentry();

            var databasePath = Path.Combine(Context.FilesDir.AbsolutePath, "Preferences.sqlite3");
            Globals.Database = new SQLiteConnection(databasePath, SQLiteOpenFlags.Create | SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.FullMutex);
            Globals.Database.CreateTable<KeyValueModel>();
            Globals.Database.CreateTable<CloudModel>();
            Globals.Database.CreateTable<NodeModel>();

            if (Globals.Database.Find<KeyValueModel>(UserSettings.EnableSharing) is null)
            {
                Globals.Database.SaveSetting(UserSettings.EnableSharing, "1");
            }

            Globals.Storage = new AndroidDataStorage();
        }

        // This method may not be called.
        public override void OnTerminate()
        {
            base.OnTerminate();
            sentry?.Dispose();
        }
    }
}
