using System;
using System.IO;
using System.Threading.Tasks;

using Android.App;
using Android.Content.PM;
using Android.OS;

using Binding;

using Unishare.Apps.Common;

namespace Unishare.Apps.DevolMobile
{
    [Activity(Name = "com.daoyehuo.UnishareLollipop.CreateCloudActivity", Label = "@string/app_name", Theme = "@style/AppTheme", ScreenOrientation = ScreenOrientation.Portrait)]
    public class CreateCloudActivity : NavigableActivity
    {
        internal new_cloud_dialog R { get; private set; }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.new_cloud_dialog);
            R = new new_cloud_dialog(this);

            R.new_cloud_device_name.EditText.TextChanged += (o, e) => {
                R.new_cloud_device_name.Error = null;
            };
            R.new_cloud_name.EditText.TextChanged += (o, e) => {
                R.new_cloud_name.Error = null;
            };
            R.new_cloud_button.Click += CreateNewCloud;
        }

        private void CreateNewCloud(object sender, EventArgs e)
        {
            var deviceName = R.new_cloud_device_name.EditText.Text;
            var cloudName = R.new_cloud_name.EditText.Text;

            var invalidCharHit = false;
            foreach (var character in Path.GetInvalidFileNameChars())
            {
                if (deviceName?.Contains(character) == true) invalidCharHit = true;
            }
            if (string.IsNullOrWhiteSpace(deviceName) || invalidCharHit)
            {
                R.new_cloud_device_name.Error = "设备名称无效";
                return;
            }

            if (string.IsNullOrWhiteSpace(cloudName))
            {
                R.new_cloud_name.Error = "云名称无效";
                return;
            }

#pragma warning disable 0618
            var progress = new ProgressDialog(this);
            progress.SetCancelable(false);
            progress.SetMessage("正在创建……");
            progress.Show();
#pragma warning restore 0618

            Task.Run(async () => {
                try
                {
                    await Globals.CloudManager.CreatePersonalCloud(cloudName, deviceName).ConfigureAwait(false);
                    Globals.Database.SaveSetting(UserSettings.DeviceName, deviceName);
                    RunOnUiThread(() => {
                        progress.Dismiss();
                        this.ShowAlert("已创建", $"您已创建并加入个人云“{cloudName}”。", () => {
                            SetResult(Result.Ok);
                            Finish();
                        });
                    });
                }
                catch
                {
                    RunOnUiThread(() => {
                        progress.Dismiss();
                        this.ShowAlert("无法创建个人云", "出现 App 内部错误。");
                    });
                }
            });
        }
    }
}
