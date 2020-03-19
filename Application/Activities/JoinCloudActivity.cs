using System;
using System.IO;
using System.Threading.Tasks;

using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;

using Binding;

using NSPersonalCloud.Interfaces.Errors;

using Unishare.Apps.Common;

namespace Unishare.Apps.DevolMobile
{
    [Activity(Name = "com.daoyehuo.UnishareLollipop.JoinCloudActivity", Label = "@string/app_name", Theme = "@style/AppTheme",  ScreenOrientation = ScreenOrientation.Portrait)]
    public class JoinCloudActivity : NavigableActivity
    {
        internal join_cloud_dialog R { get; private set; }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.join_cloud_dialog);
            R = new join_cloud_dialog(this);

            R.join_cloud_device_name.EditText.TextChanged += (o, e) => {
                R.join_cloud_device_name.Error = null;
            };
            R.join_cloud_invite.EditText.TextChanged += (o, e) => {
                R.join_cloud_invite.Error = null;
            };
            R.join_cloud_button.Click += JoinCloud;
        }

        private void JoinCloud(object sender, EventArgs e)
        {
            var deviceName = R.join_cloud_device_name.EditText.Text;
            var inviteCode = R.join_cloud_invite.EditText.Text;

            var invalidCharHit = false;
            foreach (var character in Path.GetInvalidFileNameChars())
            {
                if (deviceName?.Contains(character) == true) invalidCharHit = true;
            }
            if (string.IsNullOrWhiteSpace(deviceName) || invalidCharHit)
            {
                R.join_cloud_device_name.Error = "设备名称无效";
                return;
            }

            if (inviteCode?.Length != 4)
            {
                R.join_cloud_invite.Error = "邀请码错误";
                return;
            }

            R.join_cloud_progress.Visibility = ViewStates.Visible;
            R.join_cloud_button.Enabled = false;
            Task.Run(async () => {
                try
                {
                    var result = await Globals.CloudManager.JoinPersonalCloud(int.Parse(inviteCode), deviceName).ConfigureAwait(false);
                    Globals.Database.SaveSetting(UserSettings.DeviceName, deviceName);
                    RunOnUiThread(() => {
                        this.ShowAlert("已加入", string.Format("您已加入个人云“{0}”。", result.DisplayName), () => {
                            Finish();
                        });
                    });
                }
                catch (NoDeviceResponseException)
                {
                    RunOnUiThread(() => {
                        R.join_cloud_progress.Visibility = ViewStates.Gone;
                        R.join_cloud_button.Enabled = true;
                        this.ShowAlert("无法查询云信息", "当前网络中没有已加入个人云的设备。");

                    });
                }
                catch (InviteNotAcceptedException)
                {
                    RunOnUiThread(() => {
                        R.join_cloud_progress.Visibility = ViewStates.Gone;
                        R.join_cloud_button.Enabled = true;
                        this.ShowAlert("邀请码错误", "请查看其它设备屏幕上显示的邀请码，并核对您的输入。");
                    });
                }
                catch
                {
                    RunOnUiThread(() => {
                        R.join_cloud_progress.Visibility = ViewStates.Gone;
                        R.join_cloud_button.Enabled = true;
                        this.ShowAlert("无法查询云信息", "出现 App 内部错误。");
                    });
                }
            });
        }
    }
}
