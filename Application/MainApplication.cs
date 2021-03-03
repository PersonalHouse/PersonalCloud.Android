using System;
using System.IO;

using Android.App;
using Android.Content;
using Android.Net.Wifi;

using AndroidX.Lifecycle;

using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Microsoft.Extensions.Logging;

using SQLite;

using NSPersonalCloud.Common;
using NSPersonalCloud.Common.Models;
using NSPersonalCloud.DevolMobile.BroadcastReceivers;
using NSPersonalCloud.DevolMobile.Data;

namespace NSPersonalCloud.DevolMobile
{
    [Application(Name = "com.daoyehuo.UnishareLollipop.MainApplication", Icon = "@mipmap/ic_launcher", RoundIcon = "@mipmap/ic_launcher_round", Label = "@string/app_name",
        SupportsRtl = false, Theme = "@style/AppTheme", AllowBackup = true)]
    public class MainApplication : Application, ILifecycleEventObserver
    {
        public MainApplication(IntPtr reference, Android.Runtime.JniHandleOwnership transfer) : base(reference, transfer) { }

        private bool fromBackground;
        private BroadcastReceiver wifiMonitor;

        public override void OnCreate()
        {
            base.OnCreate();

            SQLitePCL.Batteries_V2.Init();

            AppCenter.Start("2069a171-32ec-4432-b8d5-31d5ce74fb82", typeof(Analytics), typeof(Crashes));
            Globals.Loggers = new LoggerFactory().AddSentry(config => {
                config.Dsn = "https://d0a8d714e2984642a530aa7deaca3498@o209874.ingest.sentry.io/5174354";
                config.Environment = "Android";
                config.Release = this.GetPackageVersion();
            });

#if DEBUG
            Crashes.SetEnabledAsync(false);
#endif

            var databasePath = Path.Combine(Context.FilesDir.AbsolutePath, "Preferences.sqlite3");
            Globals.Database = new SQLiteConnection(databasePath, SQLiteOpenFlags.Create | SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.FullMutex);
            Globals.Database.CreateTable<KeyValueModel>();
            Globals.Database.CreateTable<CloudModel>();
            Globals.Database.CreateTable<AlibabaOSS>();
            Globals.Database.CreateTable<AzureBlob>();
            Globals.Database.CreateTable<WebApp>();
            Globals.Database.CreateTable<BackupRecord>();

            Globals.Database.SaveSetting(UserSettings.PhotoBackupInterval, "1");

            if (Globals.Database.Find<KeyValueModel>(UserSettings.EnableSharing) is null)
            {
                Globals.Database.SaveSetting(UserSettings.EnableSharing, "1");
            }

            Globals.Storage = new AndroidDataStorage();

            wifiMonitor = new WiFiStateReceiver();
            var filter = new IntentFilter();
            filter.AddAction(WifiManager.NetworkStateChangedAction);
            RegisterReceiver(wifiMonitor, filter);
            ProcessLifecycleOwner.Get().Lifecycle.AddObserver(this);
        }

        public void OnStateChanged(ILifecycleOwner sender, Lifecycle.Event args)
        {
            if (args == Lifecycle.Event.OnStart)
            {
                if (wifiMonitor is null)
                {
                    wifiMonitor = new WiFiStateReceiver();
                    var filter = new IntentFilter();
                    filter.AddAction(WifiManager.NetworkStateChangedAction);
                    RegisterReceiver(wifiMonitor, filter);
                }
                if (!fromBackground) return;
                fromBackground = false;
                Globals.CloudManager?.NetworkMayChanged(false);
                return;
            }

            if (args == Lifecycle.Event.OnStop)
            {
                if (wifiMonitor != null)
                {
                    UnregisterReceiver(wifiMonitor);
                    wifiMonitor = null;
                }
                fromBackground = true;
                return;
            }
        }
    }
}
