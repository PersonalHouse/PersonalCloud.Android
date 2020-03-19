using System;
using System.IO;
using System.Threading.Tasks;

using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Views;

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

        private string inviteCode;

        protected override void OnCreate(Android.OS.Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.dashboard);
            R = new dashboard(this);
            DeviceCell = new key_value_cell(R.device_cell);
            DeviceCell.title_label.Text = "设备名称";
            DeviceCell.detail_label.Text = Globals.CloudManager.PersonalClouds[0].NodeDisplayName;
            CloudCell = new key_value_cell(R.cloud_cell);
            CloudCell.title_label.Text = "云名称";
            CloudCell.detail_label.Text = Globals.CloudManager.PersonalClouds[0].DisplayName;
            InviteCell = new key_value_cell(R.invite_cell);
            InviteCell.title_label.Text = "邀请码";
            InviteCell.detail_label.Text = "正在生成";
            FileSharingCell = new switch_cell(R.file_sharing_cell);
            FileSharingCell.title_label.Text = "允许此设备分享文件";
            FileSharingCell.switch_button.Checked = Globals.Database.CheckSetting(UserSettings.EnableSharing, "1");
            R.file_sharing_root.Enabled = FileSharingCell.switch_button.Checked;

            R.device_cell.Click += ChangeDeviceName;
            R.toggle_invite.Click += ToggleInvitation;
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

        private void ToggleInvitation(object sender, EventArgs e)
        {
            if (inviteCode == null)
            {
                R.toggle_invite.SetText(Resource.String.void_invites);
                R.invite_cell.Visibility = ViewStates.Visible;
                InviteCell.detail_label.Text = "请稍侯";
                Task.Run(async () => {
                    try
                    {
                        inviteCode = await Globals.CloudManager.SharePersonalCloud(Globals.CloudManager.PersonalClouds[0]).ConfigureAwait(false);
                        RunOnUiThread(() => {
                            InviteCell.detail_label.Text = inviteCode;
                        });
                    }
                    catch
                    {
                        inviteCode = "生成失败";
                        RunOnUiThread(() => {
                            InviteCell.detail_label.Text = inviteCode;
                        });
                    }
                });
            }
            else
            {
                R.toggle_invite.SetText(Resource.String.invite_others);
                R.invite_cell.Visibility = ViewStates.Gone;
                inviteCode = null;
                Task.Run(async () => {
                    await Globals.CloudManager.StopSharePersonalCloud(Globals.CloudManager.PersonalClouds[0]).ConfigureAwait(false);
                });
            }
        }

        private void ToggleFileSharing(object sender, Android.Widget.CompoundButton.CheckedChangeEventArgs e)
        {
            if (e.IsChecked)
            {
#pragma warning disable CS0618 // Type or member is obsolete
                var storageRoot = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath;
#pragma warning restore CS0618 // Type or member is obsolete
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
            this.ShowAlert("从个人云中移除此设备？", "当前设备将离开个人云，本地保存的相关信息也将删除。", "取消", null, "确认", () => {
                Globals.CloudManager.ExitFromCloud(Globals.CloudManager.PersonalClouds[0]);
                Globals.Database.DeleteAll<CloudModel>();
                Globals.DiscoverySubscribed = false;
                Finish();
            }, true);
        }
    }
}
