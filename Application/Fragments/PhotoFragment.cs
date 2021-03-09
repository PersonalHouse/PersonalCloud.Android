using System;
using System.IO;
using System.Threading.Tasks;

using Android.Content;
using Android.Runtime;
using Android.Views;

using AndroidX.AppCompat.App;
using AndroidX.Fragment.App;
using AndroidX.Work;

using Binding;

using Com.Judemanutd.Autostarter;

using NSPersonalCloud;

using NSPersonalCloud.Common;
using NSPersonalCloud.Common.Models;
using NSPersonalCloud.DevolMobile.Activities;
using NSPersonalCloud.DevolMobile.Workers;

namespace NSPersonalCloud.DevolMobile.Fragments
{
    [Register("com.daoyehuo.UnishareLollipop.PhotoFragment")]
    public class PhotoFragment : Fragment
    {
        private const int CallbackSharingRoot = 10000;

        internal fragment_photo R { get; private set; }

        internal key_value_cell DeviceCell { get; private set; }
        internal key_value_cell CloudCell { get; private set; }
        internal switch_cell FileSharingCell { get; private set; }
        internal key_value_cell BackupLocationCell { get; private set; }
        internal switch_cell AutoBackupCell { get; private set; }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Android.OS.Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.fragment_photo, container, false);
            R = new fragment_photo(view);

            BackupLocationCell = new key_value_cell(R.backup_location_cell);
            BackupLocationCell.title_label.Text = GetString(Resource.String.photos_backup_location);
            BackupLocationCell.detail_label.Text = string.IsNullOrEmpty(Globals.Database.LoadSetting(UserSettings.PhotoBackupPrefix)) ? null : GetString(Resource.String.photos_backup_location_set);
            AutoBackupCell = new switch_cell(R.backup_cell);
            AutoBackupCell.title_label.Text = GetString(Resource.String.enable_photos_backup);
            AutoBackupCell.switch_button.Checked = Globals.Database.CheckSetting(UserSettings.AutoBackupPhotos, "1");

            R.backup_location_cell.Click += ChangeBackupDevice;
            AutoBackupCell.switch_button.CheckedChange += ToggleAutoBackup;

            R.backup_now.Click += BackupNow;

            return view;
        }

        public override void OnResume()
        {
            base.OnResume();
            if (string.IsNullOrEmpty(Globals.Database.LoadSetting(UserSettings.PhotoBackupPrefix)))
            {
                BackupLocationCell.detail_label.Text = null;
            }
            else
            {
                BackupLocationCell.detail_label.Text = GetString(Resource.String.photos_backup_location_set);
            }
        }

        private void ChangeBackupDevice(object sender, EventArgs e)
        {
            this.StartActivity(typeof(ChooseBackupDeviceActivity));
        }

        private void ToggleAutoBackup(object sender, Android.Widget.CompoundButton.CheckedChangeEventArgs e)
        {
            if (e.IsChecked)
            {
                if (string.IsNullOrEmpty(Globals.Database.LoadSetting(UserSettings.PhotoBackupPrefix)))
                {
                    AutoBackupCell.switch_button.Checked = false;
                    Activity.ShowAlert(GetString(Resource.String.cannot_set_up_backup), GetString(Resource.String.cannot_set_up_backup_location));
                    return;
                }

                if (!int.TryParse(Globals.Database.LoadSetting(UserSettings.PhotoBackupInterval), out var workInterval))
                {
                    AutoBackupCell.switch_button.Checked = false;
                    Activity.ShowAlert(GetString(Resource.String.cannot_set_up_backup), GetString(Resource.String.cannot_set_up_backup_interval));
                    return;
                }

                new AlertDialog.Builder(Context)
                    .SetTitle(GetString(Resource.String.enable_autostart))
                    .SetMessage(GetString(Resource.String.enable_autostart_desc))
                    .SetNegativeButton("Deny", (s, e) => {
                        AutoBackupCell.switch_button.Checked = false;
                    })
                    .SetPositiveButton("ALLOW", (s, e) => {
                        var autoStartAvailable = AutoStartPermissionHelper.Instance.IsAutoStartPermissionAvailable(Context);
                        var success = AutoStartPermissionHelper.Instance.GetAutoStartPermission(Context);

                        var workConstraints = new Constraints.Builder()
                            .SetRequiredNetworkType(NetworkType.NotRequired).SetRequiresBatteryNotLow(true)
                            .SetRequiresCharging(false).Build();
                        var workRequest = new PeriodicWorkRequest.Builder(typeof(PhotosBackupWorker), TimeSpan.FromHours(workInterval))
                            .SetConstraints(workConstraints).Build();
                        WorkManager.GetInstance(Context).Enqueue(workRequest);
                        Globals.Database.SaveSetting(UserSettings.AutoBackupPhotos, "1");
                        Globals.Database.SaveSetting(AndroidUserSettings.BackupScheduleId, workRequest.Id.ToString());
                    })
                    .Create()
                    .Show();
            }
            else
            {
                Globals.Database.SaveSetting(UserSettings.AutoBackupPhotos, "0");
                var workRequest = Globals.Database.LoadSetting(AndroidUserSettings.BackupScheduleId);
                if (!string.IsNullOrEmpty(workRequest))
                {
                    WorkManager.GetInstance(Context).CancelWorkById(Java.Util.UUID.FromString(workRequest));
                }
            }
        }

        private void BackupNow(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(Globals.Database.LoadSetting(UserSettings.PhotoBackupPrefix)))
            {
                AutoBackupCell.switch_button.Checked = false;
                Activity.ShowAlert(GetString(Resource.String.cannot_backup), GetString(Resource.String.cannot_set_up_backup_location));
                return;
            }
            var workConstraints = new Constraints.Builder()
                .SetRequiredNetworkType(NetworkType.NotRequired).Build();
            AndroidX.Work.Data myData = new AndroidX.Work.Data.Builder().PutBoolean("BackupNow", true).Build();
            var workRequest = new OneTimeWorkRequest.Builder(typeof(PhotosBackupWorker))
                .SetInputData(myData)
                .SetConstraints(workConstraints).Build();
            WorkManager.GetInstance(Context).Enqueue(workRequest);
        }
    }
}
