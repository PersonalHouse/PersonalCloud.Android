using System;

using Android.App;
using Android.Content;
using Android.Util;
using Android.Widget;

using AndroidX.Work;

using NSPersonalCloud.Common;
using NSPersonalCloud.DevolMobile.Workers;

namespace NSPersonalCloud.DevolMobile.BroadcastReceivers
{
    [BroadcastReceiver(Enabled = true, Exported = true, DirectBootAware = true, Name = "com.daoyehuo.PersonalCloud.Android.BootReceiver")]
    [IntentFilter(new[]
    {
        Intent.ActionBootCompleted,
        Intent.ActionLockedBootCompleted,
        Intent.ActionMyPackageReplaced,
        Intent.ActionUserInitialize,
        "android.intent.action.QUICKBOOT_POWERON",
        "com.htc.intent.action.QUICKBOOT_POWERON"
    }, Categories = new[] { Intent.CategoryDefault })]
    public class BootReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            //Log.Info($"MyBootReceiver.OnReceive ({intent.Action})", "Info");
            //Toast.MakeText(context, "BootCompleted broadcast message is received", ToastLength.Long).Show();
            if (intent.Action.Equals(Intent.ActionBootCompleted) ||
                intent.Action.Equals(Intent.ActionLockedBootCompleted))
            {
                //Log.Info($"MyBootReceiver.OnReceive ({intent.Action}) Accepted", "Info");
                if (Globals.Database.CheckSetting(UserSettings.AutoBackupPhotos, "1") &&
                    !string.IsNullOrEmpty(Globals.Database.LoadSetting(UserSettings.PhotoBackupPrefix)) &&
                    int.TryParse(Globals.Database.LoadSetting(UserSettings.PhotoBackupInterval), out var workInterval))
                {
                    var workConstraints = new Constraints.Builder()
                        .SetRequiredNetworkType(NetworkType.NotRequired).SetRequiresBatteryNotLow(true)
                        .SetRequiresCharging(false).Build();
                    var workRequest = new PeriodicWorkRequest.Builder(typeof(PhotosBackupWorker), TimeSpan.FromHours(workInterval))
                        .SetConstraints(workConstraints).Build();
                    WorkManager.GetInstance(context).Enqueue(workRequest);
                    Globals.Database.SaveSetting(AndroidUserSettings.BackupScheduleId, workRequest.Id.ToString());
                    //Log.Info($"MyBootReceiver.OnReceive Create BackupScheduleId={workRequest.Id}", "Info");
                }
                else
                {
                    //Log.Info($"MyBootReceiver: Autobackup={Globals.Database.CheckSetting(UserSettings.AutoBackupPhotos, "1")}", "Info");
                    //Log.Info($"MyBootReceiver: PhotoBackupPrefix={Globals.Database.LoadSetting(UserSettings.PhotoBackupPrefix)}", "Info");
                    //Log.Info($"MyBootReceiver: PhotoBackupInterval={Globals.Database.LoadSetting(UserSettings.PhotoBackupInterval)}", "Info");
                }
            }
        }
    }
}
