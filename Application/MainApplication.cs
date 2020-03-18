using System;
using System.IO;

using Android.App;

#if !DEBUG
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
#endif

using SQLite;

using Unishare.Apps.Common;
using Unishare.Apps.Common.Data;

namespace Unishare.Apps.DevolMobile
{
    [Application(Name = "com.daoyehuo.UnishareLollipop.MainApplication", Icon = "@mipmap/ic_launcher", RoundIcon = "@mipmap/ic_launcher_round", Label = "@string/app_name",
        SupportsRtl = false, Theme = "@style/AppTheme", AllowBackup = true)]
    public class MainApplication : Application
    {
        public MainApplication(IntPtr reference, Android.Runtime.JniHandleOwnership transfer) : base(reference, transfer) { }

        public override void OnCreate()
        {
            base.OnCreate();

#if !DEBUG
            AppCenter.Start("2069a171-32ec-4432-b8d5-31d5ce74fb82", typeof(Analytics), typeof(Crashes));
#endif

            SQLitePCL.Batteries_V2.Init();

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
    }
}
