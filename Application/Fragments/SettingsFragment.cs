using System;
using System.IO;
using System.Threading.Tasks;

using Android.Content;
using Android.Runtime;
using Android.Views;

using AndroidX.Fragment.App;
using AndroidX.Work;

using Binding;

using NSPersonalCloud;

using NSPersonalCloud.Common;
using NSPersonalCloud.Common.Models;
using NSPersonalCloud.DevolMobile.Activities;
using NSPersonalCloud.DevolMobile.Workers;

namespace NSPersonalCloud.DevolMobile.Fragments
{
    [Register("com.daoyehuo.UnishareLollipop.SettingsFragment")]
    public class SettingsFragment : Fragment
    {
        private const int CallbackSharingRoot = 10000;

        internal fragment_settings R { get; private set; }

        internal key_value_cell DeviceCell { get; private set; }
        internal key_value_cell CloudCell { get; private set; }
        internal switch_cell FileSharingCell { get; private set; }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Android.OS.Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.fragment_settings, container, false);
            R = new fragment_settings(view);

            var privacyCell = new basic_cell(R.about_privacy_cell);
            privacyCell.title_label.Text = GetString(Resource.String.about_privacy);
            R.about_privacy_cell.Click += (o, e) => {
                var intent = new Intent(Intent.ActionView, Android.Net.Uri.Parse("https://daoyehuo.com/privacy.txt"));
                if (intent.ResolveActivity(Context.PackageManager) != null) StartActivity(intent);
                else
                {
                    Activity.ShowAlert(GetString(Resource.String.error_web_browser),
                        GetString(Resource.String.cannot_open_browser, "https://daoyehuo.com/privacy.txt"));
                }
            };
            var contactCell = new basic_cell(R.about_contact_cell);
            contactCell.title_label.Text = GetString(Resource.String.about_contact);
            R.about_contact_cell.Click += (o, e) => {
                var intent = new Intent(Intent.ActionSendto)
                             .SetData(Android.Net.Uri.Parse("mailto:appstore@daoyehuo.com"))
                             .PutExtra(Intent.ExtraEmail, new[] { "appstore@daoyehuo.com" })
                             .PutExtra(Intent.ExtraSubject, "Personal Cloud Feedback: " + Context.GetPackageVersion());
                if (intent.ResolveActivity(Context.PackageManager) != null)
                {
                    StartActivity(intent);
                    return;
                }

                intent = new Intent(Intent.ActionSend).SetType("text/plain")
                             .PutExtra(Intent.ExtraEmail, new[] { "appstore@daoyehuo.com" })
                             .PutExtra(Intent.ExtraSubject, "Personal Cloud Feedback: " + Context.GetPackageVersion());
                if (intent.ResolveActivity(Context.PackageManager) != null) StartActivity(intent);
                else
                {
                    Activity.ShowAlert(GetString(Resource.String.error_email),
                        GetString(Resource.String.cannot_send_email, "appstore@daoyehuo.com"));
                }
            };

            DeviceCell = new key_value_cell(R.device_cell);
            DeviceCell.title_label.Text = GetString(Resource.String.device_name);
            DeviceCell.detail_label.Text = Globals.CloudManager.PersonalClouds[0].NodeDisplayName;
            CloudCell = new key_value_cell(R.cloud_cell);
            CloudCell.title_label.Text = GetString(Resource.String.cloud_name);
            CloudCell.detail_label.Text = Globals.CloudManager.PersonalClouds[0].DisplayName;
            FileSharingCell = new switch_cell(R.file_sharing_cell);
            FileSharingCell.title_label.Text = GetString(Resource.String.enable_file_sharing);
            FileSharingCell.switch_button.Checked = Globals.Database.CheckSetting(UserSettings.EnableSharing, "1");
            R.file_sharing_root.Enabled = FileSharingCell.switch_button.Checked;

            R.device_cell.Click += ChangeDeviceName;
            R.toggle_invite.Click += InviteDevices;
            FileSharingCell.switch_button.CheckedChange += ToggleFileSharing;
            R.file_sharing_root.Click += ChangeSharingRoot;
            R.leave_cloud.Click += LeaveCloud;

            return view;
        }

        public override void OnResume()
        {
            base.OnResume();
            DeviceCell.detail_label.Text = Globals.CloudManager.PersonalClouds[0].NodeDisplayName;
            CloudCell.detail_label.Text = Globals.CloudManager.PersonalClouds[0].DisplayName;
        }

        public override void OnActivityResult(int requestCode, int resultCode, Intent data)
        {
            switch (requestCode)
            {
                case CallbackSharingRoot:
                {
                    if ((Android.App.Result) resultCode != Android.App.Result.Ok) return;
                    var path = data.GetStringExtra(ChooseFolderActivity.ResultPath);
                    if (string.IsNullOrEmpty(path)) throw new InvalidOperationException();
                    Globals.SetupFS(path);
                    Globals.Database.SaveSetting(UserSettings.SharingRoot, path);
                    Activity.ShowAlert(GetString(Resource.String.shared_folder_set), path);
                    return;
                }

                default:
                {
                    base.OnActivityResult(requestCode, resultCode, data);
                    return;
                }
            }
        }

        private void ChangeDeviceName(object sender, EventArgs e)
        {
            Activity.ShowEditorAlert(GetString(Resource.String.new_device_name), DeviceCell.detail_label.Text, null, GetString(Resource.String.action_save), deviceName => {
                var invalidCharHit = false;
                foreach (var character in Consts.InvalidCharacters)
                {
                    if (deviceName?.Contains(character) == true) invalidCharHit = true;
                }
                if (string.IsNullOrWhiteSpace(deviceName) || invalidCharHit)
                {
                    Activity.ShowAlert(GetString(Resource.String.invalid_device_name), GetString(Resource.String.invalid_device_name_message));
                    return;
                }

                var cloud = Globals.CloudManager.PersonalClouds[0];
                cloud.NodeDisplayName = deviceName;
                try { Globals.CloudManager.NetworkMayChanged(false); } catch { }
                Globals.Database.SaveSetting(UserSettings.DeviceName, deviceName);
                DeviceCell.detail_label.Text = deviceName;
            }, GetString(Resource.String.action_cancel), null);
        }

        private void InviteDevices(object sender, EventArgs e)
        {
#pragma warning disable 0618
            var progress = new Android.App.ProgressDialog(Context);
            progress.SetCancelable(false);
            progress.SetMessage(GetString(Resource.String.sending_invites));
            progress.Show();
#pragma warning restore 0618

            Task.Run(async () => {
                try
                {
                    var inviteCode = Globals.CloudManager.SharePersonalCloud(Globals.CloudManager.PersonalClouds[0]);
                    Activity?.RunOnUiThread(() => {
                        progress.Dismiss();
                        var dialog = new AndroidX.AppCompat.App.AlertDialog.Builder(Context, Resource.Style.AlertDialogTheme)
                        .SetIcon(Resource.Mipmap.ic_launcher_round).SetCancelable(false)
                        .SetTitle(Resource.String.invited_title)
                        .SetMessage(GetString(Resource.String.invited_message, inviteCode))
                        .SetPositiveButton(Resource.String.void_invites, (o, e) => {
                            try { Globals.CloudManager.StopSharePersonalCloud(Globals.CloudManager.PersonalClouds[0]); }
                            catch { }
                        }).Show();
                    });
                }
                catch
                {
                    Activity.RunOnUiThread(() => {
                        progress.Dismiss();
                        Activity.ShowAlert(GetString(Resource.String.error_invite), GetString(Resource.String.cannot_send_invites));
                    });
                }
            });
        }

        private void ToggleFileSharing(object sender, Android.Widget.CompoundButton.CheckedChangeEventArgs e)
        {
            if (e.IsChecked)
            {
                var storageRoot = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath;
                var sharingRoot = Globals.Database.LoadSetting(UserSettings.SharingRoot);
                if (string.IsNullOrEmpty(sharingRoot) || !Directory.Exists(sharingRoot))
                {
                    sharingRoot = storageRoot;
                    Globals.Database.Delete<KeyValueModel>(UserSettings.SharingRoot);
                }

                Globals.Database.SaveSetting(UserSettings.EnableSharing, "1");
                Globals.SetupFS(sharingRoot);
                R.file_sharing_root.Enabled = true;
            }
            else
            {
                Globals.Database.SaveSetting(UserSettings.EnableSharing, "0");
                Globals.SetupFS(null);
                R.file_sharing_root.Enabled = false;
            }
        }

        private void ChangeSharingRoot(object sender, EventArgs e)
        {
            this.StartActivityForResult(typeof(ChooseFolderActivity), CallbackSharingRoot);
        }

        private void LeaveCloud(object sender, EventArgs e)
        {
            new AndroidX.AppCompat.App.AlertDialog.Builder(Context, Resource.Style.AlertDialogTheme)
                .SetIcon(Resource.Mipmap.ic_launcher_round)
                .SetTitle(Resource.String.remove_from_cloud)
                .SetMessage(Resource.String.remove_deletes_local_config)
                .SetPositiveButton(Resource.String.leave_cloud_now, (o, e) => {
                    Globals.CloudManager.ExitFromCloud(Globals.CloudManager.PersonalClouds[0]);
                    Globals.Database.DeleteAll<CloudModel>();
                    Activity.Finish();
                })
                .SetNeutralButton(Resource.String.action_cancel, (EventHandler<DialogClickEventArgs>) null).Show();
        }
    }
}
