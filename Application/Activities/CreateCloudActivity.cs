using System;
using System.IO;
using System.Threading.Tasks;

using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;

using Binding;

using Unishare.Apps.Common;
using Unishare.Apps.Common.Data;

namespace Unishare.Apps.DevolMobile
{
    [Activity(Name = "com.daoyehuo.UnishareLollipop.CreateCloudActivity", Label = "@string/app_name", Theme = "@style/AppTheme",  ScreenOrientation = ScreenOrientation.Portrait)]
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
                R.new_cloud_device_name.Error = Texts.InvalidDeviceName;
                return;
            }

            if (string.IsNullOrWhiteSpace(cloudName))
            {
                R.new_cloud_name.Error = Texts.InvalidCloudName;
                return;
            }

            R.new_cloud_progress.Visibility = ViewStates.Visible;
            R.new_cloud_button.Enabled = false;

            Task.Run(async () => {
                try
                {
                    await Globals.CloudManager.CreatePersonalCloud(cloudName, deviceName).ConfigureAwait(false);
                    Globals.Database.SaveSetting(UserSettings.DeviceName, deviceName);
                    RunOnUiThread(() => {
                        R.new_cloud_progress.Visibility = ViewStates.Gone;
                        this.ShowAlert("已创建", $"您已创建并加入个人云“{cloudName}”。", () => {
                            Finish();
                        });
                    });
                }
                catch
                {
                    RunOnUiThread(() => {
                        R.new_cloud_progress.Visibility = ViewStates.Gone;
                        R.new_cloud_button.Enabled = true;
                        this.ShowAlert("无法创建个人云", "出现 App 内部错误。");
                    });
                }
            });
        }
    }
}
