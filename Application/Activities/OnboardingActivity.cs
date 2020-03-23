using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Views;

using AndroidX.AppCompat.App;

using Binding;

using Microsoft.Extensions.Logging;

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

            R.welcome_create_button.Click += (o, e) => {
                StartActivityForResult(typeof(CreateCloudActivity), CloudManagement);
            };
            R.welcome_join_button.Click += (o, e) => {
                StartActivityForResult(typeof(JoinCloudActivity), CloudManagement);
            };

            RequestPermissions(new string[] { Manifest.Permission.ReadExternalStorage, Manifest.Permission.WriteExternalStorage }, RequestAccess);
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
                R.welcome_create_hint.Visibility = ViewStates.Visible;
                R.welcome_create_button.Visibility = ViewStates.Visible;
                R.welcome_join_hint.Visibility = ViewStates.Visible;
                R.welcome_join_button.Visibility = ViewStates.Visible;

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
                Globals.CloudManager = new PCLocalService(Globals.Storage, new LoggerFactory(), Globals.FileSystem);
                Task.Run(() => Globals.CloudManager.StartService());

                if (Globals.Database.Table<CloudModel>().Count() != 0)
                {
                    StartActivity(typeof(MainActivity));
                    Finish();
                }
            }
            else
            {
                this.ShowFatalAlert("个人云需要访问存储空间", "个人云是一款文件管理 App，用于在同一网络多设备间共享文件。"
                                    + Environment.NewLine + Environment.NewLine
                                    + "您必须授权访问存储空间才能使用个人云。请在系统设置中调整个人云权限后重新打开 App。");
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
