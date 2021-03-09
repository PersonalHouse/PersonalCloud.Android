using System;
using System.Threading.Tasks;

using Android.App;
using Android.Content.PM;
using Android.OS;

using Binding;

using NSPersonalCloud;
using NSPersonalCloud.Interfaces.Errors;

using NSPersonalCloud.Common;

namespace NSPersonalCloud.DevolMobile
{
    [Activity(Name = "com.daoyehuo.UnishareLollipop.JoinCloudActivity", Label = "@string/app_name", Theme = "@style/AppTheme", ScreenOrientation = ScreenOrientation.Portrait)]
    public class JoinCloudActivity : NavigableActivity
    {
        internal join_cloud_dialog R { get; private set; }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.join_cloud_dialog);
            R = new join_cloud_dialog(this);
            R.join_cloud_device_name.EditText.Text = Build.Model;

            R.join_cloud_device_name.EditText.TextChanged += (o, e) => {
                R.join_cloud_device_name.Error = null;
            };
            R.join_cloud_invite.EditText.TextChanged += (o, e) => {
                R.join_cloud_invite.Error = null;
            };
            R.join_cloud_invite.EditText.EditorAction += (o, e) => {
                R.join_cloud_invite.Error = null;
                JoinCloud(o, e);
            };
            R.join_cloud_button.Click += JoinCloud;
        }

        private void JoinCloud(object sender, EventArgs e)
        {
            var deviceName = R.join_cloud_device_name.EditText.Text;
            var inviteCode = R.join_cloud_invite.EditText.Text;

            var invalidCharHit = false;
            foreach (var character in Consts.InvalidCharacters)
            {
                if (deviceName?.Contains(character) == true) invalidCharHit = true;
            }
            if (string.IsNullOrWhiteSpace(deviceName) || invalidCharHit)
            {
                R.join_cloud_device_name.Error = GetString(Resource.String.invalid_device_name);
                return;
            }

            if (inviteCode?.Length != 4)
            {
                R.join_cloud_invite.Error = GetString(Resource.String.invalid_invite_code);
                return;
            }


#pragma warning disable 0618
            var progress = new ProgressDialog(this);
            progress.SetCancelable(false);
            progress.SetMessage(GetString(Resource.String.joining_cloud));
            progress.Show();
#pragma warning restore 0618

            Task.Run(async () => {
                try
                {
                    var result = await Globals.CloudManager.JoinPersonalCloud(int.Parse(inviteCode), deviceName).ConfigureAwait(false);
                    Globals.Database.SaveSetting(UserSettings.DeviceName, deviceName);
                    RunOnUiThread(() => {
                        progress.Dismiss();
                        this.ShowAlert(GetString(Resource.String.cloud_joined_title), GetString(Resource.String.cloud_joined_message, result.DisplayName), () => {
                            SetResult(Result.Ok);
                            Finish();
                        });
                    });
                }
                catch (NoDeviceResponseException)
                {
                    RunOnUiThread(() => {
                        progress.Dismiss();
                        this.ShowAlert(GetString(Resource.String.error_join_cloud), GetString(Resource.String.error_no_cloud_message));

                    });
                }
                catch (InviteNotAcceptedException)
                {
                    RunOnUiThread(() => {
                        progress.Dismiss();
                        this.ShowAlert(GetString(Resource.String.invalid_invite_code), GetString(Resource.String.error_incorrect_invitation_message));
                    });
                }
                catch
                {
                    RunOnUiThread(() => {
                        progress.Dismiss();
                        this.ShowAlert(GetString(Resource.String.error_join_cloud), GetString(Resource.String.error_internal_message));
                    });
                }
            });
        }
    }
}
