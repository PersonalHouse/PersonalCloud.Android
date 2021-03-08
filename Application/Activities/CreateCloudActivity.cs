using System;
using System.Threading.Tasks;

using Android.App;
using Android.Content.PM;
using Android.OS;

using Binding;

using NSPersonalCloud;

using NSPersonalCloud.Common;

namespace NSPersonalCloud.DevolMobile
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
            R.new_cloud_device_name.EditText.Text = Build.Model;

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
            foreach (var character in Consts.InvalidCharacters)
            {
                if (deviceName?.Contains(character) == true) invalidCharHit = true;
            }
            if (string.IsNullOrWhiteSpace(deviceName) || invalidCharHit)
            {
                R.new_cloud_device_name.Error = GetString(Resource.String.invalid_device_name);
                return;
            }

            if (string.IsNullOrWhiteSpace(cloudName))
            {
                R.new_cloud_name.Error = GetString(Resource.String.invalid_cloud_name);
                return;
            }

#pragma warning disable 0618
            var progress = new ProgressDialog(this);
            progress.SetCancelable(false);
            progress.SetMessage(GetString(Resource.String.creating_cloud));
            progress.Show();
#pragma warning restore 0618

            Task.Run(async () => {
                try
                {
                    Globals.CloudManager.CreatePersonalCloud(cloudName, deviceName);
                    Globals.Database.SaveSetting(UserSettings.DeviceName, deviceName);
                    RunOnUiThread(() => {
                        progress.Dismiss();
                        this.ShowAlert(GetString(Resource.String.cloud_created_title), GetString(Resource.String.cloud_created_message, cloudName), () => {
                            SetResult(Result.Ok);
                            Finish();
                        });
                    });
                }
                catch
                {
                    RunOnUiThread(() => {
                        progress.Dismiss();
                        this.ShowAlert(GetString(Resource.String.error_create_cloud), GetString(Resource.String.error_internal_message));
                    });
                }
            });
        }
    }
}
