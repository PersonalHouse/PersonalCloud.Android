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
                this.ShowAlert("服务无名称", "您必须填写“服务名称”才能将此服务添加到个人云。", () => {
                    R.aliyun_name.EditText.RequestFocus();
                });
                return;
            }

            var endpoint = R.aliyun_endpoint.EditText.Text;
            if (string.IsNullOrEmpty(endpoint))
            {
                this.ShowAlert("帐户信息不完整", "您必须填写“域名”（也称“访问域名”、Endpoint 或 Extranet Endpoint）才能连接到阿里云。", () => {
                    R.aliyun_endpoint.EditText.RequestFocus();
                });
                return;
            }

            var bucket = R.aliyun_bucket.EditText.Text;
            if (string.IsNullOrEmpty(bucket))
            {
                this.ShowAlert("帐户信息不完整", "您必须填写“存储空间”（也称 Bucket）才能连接到阿里云。", () => {
                    R.aliyun_bucket.EditText.RequestFocus();
                });
                return;
            }

            var accessId = R.aliyun_access_id.EditText.Text;
            if (string.IsNullOrEmpty(accessId))
            {
                this.ShowAlert("帐户信息不完整", "您必须填写“用户 ID”（也称 Access Key ID）才能连接到阿里云。", () => {
                    R.aliyun_access_id.EditText.RequestFocus();
                });
                return;
            }

            var accessSecret = R.aliyun_access_secret.EditText.Text;
            if (string.IsNullOrEmpty(accessSecret))
            {
                this.ShowAlert("帐户信息不完整", "您必须填写“访问密钥”（也称 Access Key Secret）才能连接到阿里云。", () => {
                    R.aliyun_access_id.EditText.RequestFocus();
                });
                return;
            }

            if (Globals.Database.Find<AliYunOSS>(x => x.Name == name) != null)
            {
                this.ShowAlert("同名服务已存在", "为避免数据冲突，请为此服务指定不同的名称。", () => {
                    R.aliyun_name.EditText.RequestFocus();
                });
                return;
            }

#pragma warning disable 0618
            var progress = new ProgressDialog(this);
            progress.SetCancelable(false);
            progress.SetMessage("正在验证……");
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
                        this.ShowAlert("认证失败", "您提供的帐户信息无法用来访问阿里云对象存储服务。请检查后重试。");
                    });
                }
            });
        }
    }
}
