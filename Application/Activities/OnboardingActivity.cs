using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Views;

using AndroidX.AppCompat.App;
using AndroidX.Core.App;

using Binding;

using NSPersonalCloud;

using Unishare.Apps.Common;
using Unishare.Apps.Common.Models;

namespace Unishare.Apps.DevolMobile.Activities
{
    [Activity(Name = "com.daoyehuo.UnishareLollipop.OnboardingActivity", Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true, ScreenOrientation = ScreenOrientation.Portrait)]
    public class OnboardingActivity : AppCompatActivity
    {
        internal welcome R { get; private set; }

        private const int RequestAccess = 10000;
        private const int CloudManagement = 10001;

        protected override void OnCreate(Android.OS.Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.welcome);
            R = new welcome(this);

            R.welcome_create_button.Click += (o, e) => {
                StartActivityForResult(typeof(CreateCloudActivity), CloudManagement);
            };
            R.welcome_join_button.Click += (o, e) => {
                StartActivityForResult(typeof(JoinCloudActivity), CloudManagement);
            };

            ActivityCompat.RequestPermissions(this, new string[] { Manifest.Permission.ReadExternalStorage, Manifest.Permission.WriteExternalStorage }, RequestAccess);
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            if (requestCode != RequestAccess)
            {
                base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
                return;
            }
            if (grantResults.All(x => x == Permission.Granted))
            {
#pragma warning disable CS0618 // Type or member is obsolete
                var storageRoot = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath;
#pragma warning restore CS0618 // Type or member is obsolete

                Directory.CreateDirectory(Path.Combine(storageRoot, "Personal Cloud"));

                var sharingRoot = Globals.Database.LoadSetting(UserSettings.SharingRoot);
                if (string.IsNullOrEmpty(sharingRoot) || !Directory.Exists(sharingRoot))
                {
                    sharingRoot = storageRoot;
                    Globals.Database.Delete<KeyValueModel>(UserSettings.SharingRoot);
                }
                if (Globals.Database.LoadSetting(UserSettings.EnableSharing) == "0")
                {
                    sharingRoot = null;
                }

                Globals.FileSystem = new VirtualFileSystem(sharingRoot);

                var appsPath = Path.Combine(FilesDir.AbsolutePath, "Static");
                Directory.CreateDirectory(appsPath);
                Globals.CloudManager = new PCLocalService(Globals.Storage, Globals.Loggers, Globals.FileSystem, appsPath);

                Task.Run(async () => {
                    var appVersion = this.GetPackageVersion();
                    if (!Globals.Database.CheckSetting(UserSettings.LastInstalledVersion, appVersion))
                    {
                        await Globals.CloudManager.InstallApps().ConfigureAwait(false);
                        Globals.Database.SaveSetting(UserSettings.LastInstalledVersion, appVersion);
                    }

                    Globals.CloudManager.StartService();
                });

                if (Globals.Database.Table<CloudModel>().Count() != 0)
                {
                    StartActivity(typeof(MainActivity));
                    Finish();
                }
                else
                {
                    R.welcome_create_hint.Visibility = ViewStates.Visible;
                    R.welcome_create_button.Visibility = ViewStates.Visible;
                    R.welcome_join_hint.Visibility = ViewStates.Visible;
                    R.welcome_join_button.Visibility = ViewStates.Visible;
                }
            }
            else
            {
                this.ShowFatalAlert(GetString(Resource.String.error_storage_permission_title), GetString(Resource.String.error_storage_permission_message));
                return;
            }
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            if (requestCode != CloudManagement)
            {
                base.OnActivityResult(requestCode, resultCode, data);
                return;
            }

            if (resultCode == Result.Ok)
            {
                StartActivity(typeof(MainActivity));
                Finish();
            }
        }
    }
}
