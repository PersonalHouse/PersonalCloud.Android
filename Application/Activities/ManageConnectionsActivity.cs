using System;
using System.Threading.Tasks;

using Android.App;
using Android.Content.PM;
using Android.OS;

using Binding;

using NSPersonalCloud;
using NSPersonalCloud.FileSharing.Aliyun;

using Unishare.Apps.Common;

using static Android.Widget.AdapterView;

namespace Unishare.Apps.DevolMobile.Activities
{
    [Activity(Name = "com.daoyehuo.UnishareLollipop.ManageConnectionsActivity", Label = "@string/app_name", Theme = "@style/AppTheme", ScreenOrientation = ScreenOrientation.Portrait)]
    public class ManageConnectionsActivity : NavigableActivity
    {
        internal add_a_connection R { get; private set; }

        private bool initialized;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.add_a_connection);
            SupportActionBar.Title = GetString(Resource.String.connections);
            R = new add_a_connection(this);
            R.connection_save.Click += SaveCredentials;
            R.connection_endpoint.EditText.RequestFocus();
            R.connection_to_spinner.ItemSelected += SwitchConnections;

            initialized = true;
        }

        private void SwitchConnections(object sender, ItemSelectedEventArgs e)
        {
            if (!initialized) return;

            switch (R.connection_to_spinner.SelectedItemPosition)
            {
                case 0: // Alibaba Cloud OSS
                {
                    R.connection_endpoint.Hint = GetString(Resource.String.aliyun_endpoint);
                    R.connection_container.Hint = GetString(Resource.String.aliyun_bucket);
                    R.connection_account.Hint = GetString(Resource.String.aliyun_user);
                    R.connection_secret.Hint = GetString(Resource.String.aliyun_secret);
                    return;
                }
                case 1: // Azure Blob Storage
                {
                    R.connection_endpoint.Hint = GetString(Resource.String.azure_endpoint);
                    R.connection_container.Hint = GetString(Resource.String.azure_container);
                    R.connection_account.Hint = GetString(Resource.String.azure_account);
                    R.connection_secret.Hint = GetString(Resource.String.azure_secret);
                    return;
                }
            }
        }

        private void SaveCredentials(object sender, EventArgs e)
        {
            var name = R.connection_name.EditText.Text;
            var invalidCharHit = false;
            foreach (var character in VirtualFileSystem.InvalidCharacters)
            {
                if (name?.Contains(character) == true) invalidCharHit = true;
            }
            if (string.IsNullOrEmpty(name) || invalidCharHit)
            {
                this.ShowAlert(GetString(Resource.String.connection_bad_name), GetString(Resource.String.connection_name_cannot_be_empty), () => {
                    R.connection_name.EditText.RequestFocus();
                });
                return;
            }

            var selection = R.connection_to_spinner.SelectedItemPosition;
            OssConfig alibaba = null;
            AzureBlobConfig azure = null;
            switch (selection)
            {
                case 0: // Alibaba Cloud OSS
                {
                    var endpoint = R.connection_endpoint.EditText.Text;
                    if (string.IsNullOrEmpty(endpoint))
                    {
                        this.ShowAlert(GetString(Resource.String.connection_invalid_account), GetString(Resource.String.aliyun_bad_endpoint), () => {
                            R.connection_endpoint.EditText.RequestFocus();
                        });
                        return;
                    }

                    var bucket = R.connection_container.EditText.Text;
                    if (string.IsNullOrEmpty(bucket))
                    {
                        this.ShowAlert(GetString(Resource.String.connection_invalid_account), GetString(Resource.String.aliyun_bad_bucket), () => {
                            R.connection_container.EditText.RequestFocus();
                        });
                        return;
                    }

                    var accessId = R.connection_account.EditText.Text;
                    if (string.IsNullOrEmpty(accessId))
                    {
                        this.ShowAlert(GetString(Resource.String.connection_invalid_account), GetString(Resource.String.aliyun_bad_user_id), () => {
                            R.connection_account.EditText.RequestFocus();
                        });
                        return;
                    }

                    var accessSecret = R.connection_secret.EditText.Text;
                    if (string.IsNullOrEmpty(accessSecret))
                    {
                        this.ShowAlert(GetString(Resource.String.connection_invalid_account), GetString(Resource.String.aliyun_bad_secret), () => {
                            R.connection_secret.EditText.RequestFocus();
                        });
                        return;
                    }

                    alibaba = new OssConfig {
                        OssEndpoint = endpoint,
                        BucketName = bucket,
                        AccessKeyId = accessId,
                        AccessKeySecret = accessSecret
                    };

                    break;
                }
                case 1: // Azure Blob Storage
                {
                    var endpoint = R.connection_endpoint.EditText.Text;
                    if (string.IsNullOrEmpty(endpoint))
                    {
                        this.ShowAlert(GetString(Resource.String.connection_invalid_account), GetString(Resource.String.connection_bad_azure_endpoint), () => {
                            R.connection_endpoint.EditText.RequestFocus();
                        });
                        return;
                    }

                    var accountName = R.connection_account.EditText.Text;

                    var accessKey = R.connection_secret.EditText.Text;
                    if (string.IsNullOrEmpty(accessKey))
                    {
                        this.ShowAlert(GetString(Resource.String.connection_invalid_account), GetString(Resource.String.connection_bad_azure_secret), () => {
                            R.connection_secret.EditText.RequestFocus();
                        });
                        return;
                    }

                    var container = R.connection_container.EditText.Text;
                    if (string.IsNullOrEmpty(container))
                    {
                        this.ShowAlert(GetString(Resource.String.connection_invalid_account), GetString(Resource.String.connection_bad_azure_container), () => {
                            R.connection_container.RequestFocus();
                        });
                        return;
                    }

                    string connection;
                    if (endpoint.Contains(accountName, StringComparison.Ordinal)) accountName = null;
                    if (endpoint.StartsWith("http://", StringComparison.Ordinal)) endpoint.Replace("http://", "https://");
                    if (endpoint.StartsWith("https://", StringComparison.Ordinal))
                    {
                        if (string.IsNullOrEmpty(accountName))
                        {
                            if (string.IsNullOrEmpty(accessKey)) connection = endpoint;
                            else connection = $"BlobEndpoint={endpoint};SharedAccessSignature={accessKey}";
                        }
                        else
                        {
                            this.ShowAlert(GetString(Resource.String.connection_invalid_account), GetString(Resource.String.connection_mismatch_azure_endpoint));
                            return;
                        }
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(accountName))
                        {
                            this.ShowAlert(GetString(Resource.String.connection_invalid_account), GetString(Resource.String.connection_bad_azure_id));
                            return;
                        }
                        else connection = $"DefaultEndpointsProtocol=https;AccountName={accountName};AccountKey={accessKey};EndpointSuffix={endpoint}";
                    }

                    azure = new AzureBlobConfig {
                        ConnectionString = connection,
                        BlobName = container
                    };
                    break;
                }
            }

            if (!Globals.Database.IsStorageNameUnique(name))
            {
                this.ShowAlert(GetString(Resource.String.connection_name_exists), GetString(Resource.String.connection_use_different_name), () => {
                    R.connection_name.EditText.RequestFocus();
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
                switch (selection)
                {
                    case 0:
                    {
                        if (alibaba.Verify())
                        {
                            Globals.CloudManager.AddStorageProvider(Globals.CloudManager.PersonalClouds[0].Id, name, alibaba, StorageProviderVisibility.Private);
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
                        return;
                    }

                    case 1:
                    {
                        if (azure.Verify())
                        {
                            Globals.CloudManager.AddStorageProvider(Globals.CloudManager.PersonalClouds[0].Id, name, azure, StorageProviderVisibility.Private);
                            RunOnUiThread(() => {
                                progress.Dismiss();
                                Finish();
                            });
                        }
                        else
                        {
                            RunOnUiThread(() => {
                                progress.Dismiss();
                                this.ShowAlert(GetString(Resource.String.connection_bad_account), GetString(Resource.String.connection_bad_azure_account));
                            });
                        }
                        return;
                    }
                }
            });
        }
    }
}
