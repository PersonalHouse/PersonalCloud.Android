using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Views;

using AndroidX.AppCompat.App;
using AndroidX.RecyclerView.Widget;

using Binding;

using DavideSteduto.FlexibleAdapter;
using DavideSteduto.FlexibleAdapter.Common;
using DavideSteduto.FlexibleAdapter.Helpers;

using Microsoft.Extensions.Logging;

using NSPersonalCloud;
using NSPersonalCloud.Interfaces.FileSystem;
using NSPersonalCloud.RootFS;

using Unishare.Apps.Common;
using Unishare.Apps.Common.Data;
using Unishare.Apps.DevolMobile.Items;

using static DavideSteduto.FlexibleAdapter.FlexibleAdapter;

namespace Unishare.Apps.DevolMobile
{
    [Activity(Name = "com.daoyehuo.UnishareLollipop.MainActivity", Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true,  ScreenOrientation = ScreenOrientation.Portrait)]
    public class MainActivity : AppCompatActivity, IOnItemClickListener
    {
        private const int RequestAccess = 10000;

        internal cloud_browser R { get; private set; }

        private FlexibleAdapter adapter;
        private RecyclerView.LayoutManager layoutManager;

        private RootFileSystem fileSystem;
        private List<FileSystemEntry> devices;

        protected override void OnCreate(Android.OS.Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.cloud_browser);
            R = new cloud_browser(this);
            Window.AddFlags(WindowManagerFlags.KeepScreenOn);

            adapter = new FlexibleAdapter(null, this);
            adapter.SetAnimationOnForwardScrolling(true);
            layoutManager = new SmoothScrollLinearLayoutManager(this);
            R.list_recycler.SetLayoutManager(layoutManager);
            R.list_recycler.SetAdapter(adapter);
            R.list_recycler.AddItemDecoration(new FlexibleItemDecoration(this).WithDefaultDivider());
            R.list_reloader.SetColorSchemeResources(Resource.Color.colorAccent);
            R.list_reloader.Refresh += RefreshDevices;
            EmptyViewHelper.Create(adapter, R.list_empty);

            RequestPermissions(new string[] { Manifest.Permission.ReadExternalStorage, Manifest.Permission.WriteExternalStorage }, RequestAccess);
        }

        protected override void OnResume()
        {
            base.OnResume();
            InvalidateOptionsMenu();

            if (fileSystem == null)
            {
                RefreshDevices(this, EventArgs.Empty);
            }

            if (!Globals.DiscoverySubscribed)
            {
                var cloud = Globals.CloudManager?.PersonalClouds?.FirstOrDefault();
                if (cloud == null) return;
                cloud.OnNodeChangedEvent += (o, e) => RunOnUiThread(() => RefreshDevices(this, EventArgs.Empty));
            }
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            if (Globals.Database.Table<CloudModel>().Count() == 0) MenuInflater.Inflate(Resource.Menu.join_cloud, menu);
            else MenuInflater.Inflate(Resource.Menu.settings, menu);
            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.create_new_cloud:
                {
                    StartActivity(typeof(CreateCloudActivity));
                    break;
                }

                case Resource.Id.join_new_cloud:
                {
                    StartActivity(typeof(JoinCloudActivity));
                    break;
                }

                case Resource.Id.app_settings:
                {
                    StartActivity(typeof(SettingsActivity));
                    break;
                }
            }
            return true;
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            if (requestCode == RequestAccess)
            {
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
                    Globals.CloudManager = new PCLocalService(Globals.Storage, new LoggerFactory(), Globals.FileSystem);
                    Task.Run(() => Globals.CloudManager.StartService());
                }
                else
                {
                    this.ShowAlert("个人云需要访问存储空间", "个人云是一款文件管理 App，用于在同一网络多设备间共享文件。"
                                   + Environment.NewLine + Environment.NewLine
                                   + "您必须授权访问存储空间才能使用个人云。"
                                   + Environment.NewLine + Environment.NewLine
                                   + "请在系统设置中调整个人云权限后重新打开 App。");
                    return;
                }
            }

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        private void RefreshDevices(object sender, EventArgs e)
        {
            fileSystem = Globals.CloudManager?.PersonalClouds?.FirstOrDefault()?.RootFS;
            if (fileSystem == null)
            {
                R.empty_text.Text = GetString(Resource.String.no_personal_cloud);
                if (R.list_reloader.Refreshing) R.list_reloader.Refreshing = false;
                return;
            }

            if (!R.list_reloader.Refreshing) R.list_reloader.Refreshing = true;
            Task.Run(async () => {
                devices = await fileSystem.EnumerateChildrenAsync("/").ConfigureAwait(false);
                var models = devices.Select(x => new Device(x)).ToList();

                RunOnUiThread(() => {
                    if (R.list_reloader.Refreshing) R.list_reloader.Refreshing = false;

                    if (devices.Count == 0)
                    {
                        R.empty_text.Text = GetString(Resource.String.no_active_device);
                        return;
                    }

                    adapter.UpdateDataSet(models, true);
                });
            });
        }

        public bool OnItemClick(View view, int position)
        {
            var device = devices[position];
            var intent = new Intent(this, typeof(DeviceDirectoryActivity)).PutExtra(DeviceDirectoryActivity.ExtraDeviceName, device.Name);
            StartActivity(intent);

            return false;
        }
    }
}
