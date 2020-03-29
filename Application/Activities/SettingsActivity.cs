using System;
using System.IO;
using System.Threading.Tasks;

using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Widget;

using Binding;

using Unishare.Apps.Common;
using Unishare.Apps.Common.Models;
using Unishare.Apps.DevolMobile.Activities;

namespace Unishare.Apps.DevolMobile
{
    [Activity(Name = "com.daoyehuo.UnishareLollipop.SettingsActivity", Label = "@string/settings", Theme = "@style/AppTheme", ScreenOrientation = ScreenOrientation.Portrait)]
    public class SettingsActivity : NavigableActivity
    {
        private const int CallbackSharingRoot = 10000;

        internal dashboard R { get; private set; }
        internal key_value_cell DeviceCell { get; private set; }
        internal key_value_cell CloudCell { get; private set; }
        internal key_value_cell InviteCell { get; private set; }
        internal switch_cell FileSharingCell { get; private set; }

        protected override void OnCreate(Android.OS.Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.dashboard);
            R = new dashboard(this);

            var privacyCell = new basic_cell(R.about_privacy_cell);
            privacyCell.title_label.Text = GetString(Resource.String.about_privacy);
            R.about_privacy_cell.Click += (o, e) => {
                var intent = new Intent(Intent.ActionView, Android.Net.Uri.Parse(@"https://daoyehuo.com/privacy.txt"));
                if (intent.ResolveActivity(PackageManager) != null) StartActivity(intent);
                else Toast.MakeText(this, "无法打开浏览器", ToastLength.Long).Show();
            };
            var contactCell = new basic_cell(R.about_contact_cell);
            contactCell.title_label.Text = GetString(Resource.String.about_contact_us);
            R.about_contact_cell.Click += (o, e) => {
                var intent = new Intent(Intent.ActionSendto).SetType(@"text/plain")
                                                            .PutExtra(Intent.ExtraEmail, @"appstore@daoyehuo.com")
                                                            .PutExtra(Intent.ExtraSubject, @"个人云 (2.0.2) 反馈");
                if (intent.ResolveActivity(PackageManager) != null) StartActivity(intent);
                else Toast.MakeText(this, "无法发送电子邮件", ToastLength.Long).Show();
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
        }

        protected override void OnResume()
        {
            base.OnResume();
            DeviceCell.detail_label.Text = Globals.CloudManager.PersonalClouds[0].NodeDisplayName;
            CloudCell.detail_label.Text = Globals.CloudManager.PersonalClouds[0].DisplayName;
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            switch (requestCode)
            {
                case CallbackSharingRoot:
                {
                    if (resultCode != Result.Ok) return;
                    var path = data.GetStringExtra(ChooseFolderActivity.ResultPath);
                    if (string.IsNullOrEmpty(path)) throw new InvalidOperationException();
                    Globals.FileSystem.RootPath = path;
                    Globals.Database.SaveSetting(UserSettings.SharingRoot, path);
                    this.ShowAlert("已设置分享文件夹", path);
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
            this.ShowEditorAlert("输入设备新名称", DeviceCell.detail_label.Text, null, "保存", deviceName => {
                var invalidCharHit = false;
                foreach (var character in Path.GetInvalidFileNameChars())
                {
                    if (deviceName?.Contains(character) == true) invalidCharHit = true;
                }
                if (string.IsNullOrWhiteSpace(deviceName) || invalidCharHit)
                {
                    this.ShowAlert("设备名称无效", "请使用简短、尽量不包含特殊字符、尽量不与其它设备重复的名称。");
                    return;
                }

                var cloud = Globals.CloudManager.PersonalClouds[0];
                cloud.NodeDisplayName = deviceName;
                try { Globals.CloudManager.StartNetwork(false); } catch { }
                Globals.Database.SaveSetting(UserSettings.DeviceName, deviceName);
                DeviceCell.detail_label.Text = deviceName;
            }, "取消", null);
        }

        private void InviteDevices(object sender, EventArgs e)
        {
#pragma warning disable 0618
            var progress = new ProgressDialog(this);
            progress.SetCancelable(false);
            progress.SetMessage("正在生成……");
            progress.Show();
#pragma warning restore 0618

            Task.Run(async () => {
                try
                {
                    var inviteCode = await Globals.CloudManager.SharePersonalCloud(Globals.CloudManager.PersonalClouds[0]).ConfigureAwait(false);
                    RunOnUiThread(() => {
                        progress.Dismiss();
                        var dialog = new AndroidX.AppCompat.App.AlertDialog.Builder(this, Resource.Style.AlertDialogTheme)
                        .SetIcon(Resource.Mipmap.ic_launcher_round).SetCancelable(false)
                        .SetTitle("已生成邀请码")
                        .SetMessage("请在其它设备输入邀请码：" + Environment.NewLine + Environment.NewLine +
                        inviteCode + Environment.NewLine + Environment.NewLine +
                        "离开此界面邀请码将失效。")
                        .SetPositiveButton("停止邀请", (o, e) => {
                            try { _ = Globals.CloudManager.StopSharePersonalCloud(Globals.CloudManager.PersonalClouds[0]); }
                            catch { }
                        }).Show();
                    });
                }
                catch
                {
                    RunOnUiThread(() => {
                        progress.Dismiss();
                        this.ShowAlert("无法邀请其它设备", "邀请码生成失败，请稍后重试。");
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
                Globals.FileSystem.RootPath = sharingRoot;
                R.file_sharing_root.Enabled = true;
            }
            else
            {
                Globals.Database.SaveSetting(UserSettings.EnableSharing, "0");
                Globals.FileSystem.RootPath = null;
                R.file_sharing_root.Enabled = false;
            }
        }

        private void ChangeSharingRoot(object sender, EventArgs e)
        {
            StartActivityForResult(typeof(ChooseFolderActivity), CallbackSharingRoot);
        }

        private void LeaveCloud(object sender, EventArgs e)
        {
            new AndroidX.AppCompat.App.AlertDialog.Builder(this, Resource.Style.AlertDialogTheme)
                .SetIcon(Resource.Mipmap.ic_launcher_round)
                .SetTitle("从个人云中移除此设备？")
                .SetMessage("当前设备将离开个人云，本地保存的相关信息也将删除。")
                .SetPositiveButton("离开", (o, e) => {
                    Globals.CloudManager.ExitFromCloud(Globals.CloudManager.PersonalClouds[0]);
                    Globals.Database.DeleteAll<CloudModel>();
                    Finish();
                })
                .SetNeutralButton("返回", (EventHandler<DialogClickEventArgs>) null).Show();
        }
    }
}
