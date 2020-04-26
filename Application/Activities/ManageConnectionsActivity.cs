using System;
using System.Threading.Tasks;

using Android.App;
using Android.Content.PM;
using Android.OS;

using Binding;

using NSPersonalCloud;
using NSPersonalCloud.FileSharing.Aliyun;

using Unishare.Apps.Common.Models;

namespace Unishare.Apps.DevolMobile.Activities
{
    [Activity(Name = "com.daoyehuo.UnishareLollipop.ManageConnectionsActivity", Label = "@string/app_name", Theme = "@style/AppTheme", ScreenOrientation = ScreenOrientation.Portrait)]
    public class ManageConnectionsActivity : NavigableActivity
    {
        internal add_a_connection R { get; private set; }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.add_a_connection);
            SupportActionBar.Title = GetString(Resource.String.connections);
            R = new add_a_connection(this);
            R.connection_save.Click += SaveCredentials;
            R.aliyun_endpoint.EditText.RequestFocus();
        }

        private void SaveCredentials(object sender, EventArgs e)
        {
            var name = R.aliyun_name.EditText.Text;
            var invalidCharHit = false;
            foreach (var character in VirtualFileSystem.InvalidCharacters)
            {
                if (name?.Contains(character) == true) invalidCharHit = true;
            }
            if (string.IsNullOrEmpty(name) || invalidCharHit)
            {
                this.ShowAlert(GetString(Resource.String.connection_bad_name), GetString(Resource.String.connection_name_cannot_be_empty), () => {
                    R.aliyun_name.EditText.RequestFocus();
                });
                return;
            }

            var endpoint = R.aliyun_endpoint.EditText.Text;
            if (string.IsNullOrEmpty(endpoint))
            {
                this.ShowAlert(GetString(Resource.String.connection_invalid_account), GetString(Resource.String.aliyun_bad_endpoint), () => {
                    R.aliyun_endpoint.EditText.RequestFocus();
                });
                return;
            }

            var bucket = R.aliyun_bucket.EditText.Text;
            if (string.IsNullOrEmpty(bucket))
            {
                this.ShowAlert(GetString(Resource.String.connection_invalid_account), GetString(Resource.String.aliyun_bad_bucket), () => {
                    R.aliyun_bucket.EditText.RequestFocus();
                });
                return;
            }

            var accessId = R.aliyun_access_id.EditText.Text;
            if (string.IsNullOrEmpty(accessId))
            {
                this.ShowAlert(GetString(Resource.String.connection_invalid_account), GetString(Resource.String.aliyun_bad_user_id), () => {
                    R.aliyun_access_id.EditText.RequestFocus();
                });
                return;
            }

            var accessSecret = R.aliyun_access_secret.EditText.Text;
            if (string.IsNullOrEmpty(accessSecret))
            {
                this.ShowAlert(GetString(Resource.String.connection_invalid_account), GetString(Resource.String.aliyun_bad_secret), () => {
                    R.aliyun_access_id.EditText.RequestFocus();
                });
                return;
            }

            if (Globals.Database.Find<AliYunOSS>(x => x.Name == name) != null)
            {
                this.ShowAlert(GetString(Resource.String.connection_name_exists), GetString(Resource.String.connection_use_different_name), () => {
                    R.aliyun_name.EditText.RequestFocus();
                });
                return;
            }

#pragma warning disable 0618
            var progress = new ProgressDialog(this);
            progress.SetCancelable(false);
            progress.SetMessage(GetString(Resource.String.connection_verifying));
            progress.Show();
#pragma warning restore 0618

            Task.Run(() => {
                var config = new OssConfig {
                    OssEndpoint = endpoint,
                    BucketName = bucket,
                    AccessKeyId = accessId,
                    AccessKeySecret = accessSecret
                };

                if (config.Verify())
                {
                    Globals.CloudManager.AddStorageProvider(Globals.CloudManager.PersonalClouds[0].Id, name, config, StorageProviderVisibility.Private);
                    RunOnUiThread(() => {
                        progress.Dismiss();
                        Finish();
                    });
                }
                else
                {
                    RunOnUiThread(() => {
                        progress.Dismiss();
                        this.ShowAlert(GetString(Resource.String.connection_bad_account), GetString(Resource.String.connection_bad_aliyun_account));
                    });
                }
            });
        }
    }
}
