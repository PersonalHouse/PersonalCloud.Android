using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Views;

using AndroidX.AppCompat.App;
using AndroidX.RecyclerView.Widget;
using AndroidX.Work;
using Binding;

using DavideSteduto.FlexibleAdapter;
using DavideSteduto.FlexibleAdapter.Common;
using DavideSteduto.FlexibleAdapter.Helpers;
using DavideSteduto.FlexibleAdapter.Items;

using NSPersonalCloud.Interfaces.FileSystem;
using NSPersonalCloud.RootFS;

using Unishare.Apps.Common.Models;
using Unishare.Apps.DevolMobile.Activities;
using Unishare.Apps.DevolMobile.Items;
using Unishare.Apps.DevolMobile.Workers;
using static DavideSteduto.FlexibleAdapter.FlexibleAdapter;

namespace Unishare.Apps.DevolMobile
{
    [Activity(Name = "com.daoyehuo.UnishareLollipop.MainActivity", Label = "@string/app_name", Theme = "@style/AppTheme", ScreenOrientation = ScreenOrientation.Portrait)]
    public class MainActivity : AppCompatActivity, IOnItemClickListener
    {
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

            Globals.CloudManager.PersonalClouds[0].OnNodeChangedEvent += (o, e) => {
                RunOnUiThread(() => RefreshDevices(this, EventArgs.Empty));
            };

            /*
            var worker = new PhotosBackupWorker(this, new WorkerParameters(Java.Util.UUID.FromString(Guid.NewGuid().ToString()), null, new List<string>(), null, 0, null, null, null, null, null));
            Task.Run(() => {
                worker.DoWork();
            });
            */
        }

        protected override void OnResume()
        {
            base.OnResume();

            if (Globals.Database.Table<CloudModel>().Count() == 0)
            {
                StartActivity(typeof(OnboardingActivity));
                Finish();
                return;
            }

            RefreshDevices(this, EventArgs.Empty);
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.settings, menu);
            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.app_settings:
                {
                    StartActivity(typeof(SettingsActivity));
                    break;
                }
            }
            return true;
        }

        
        private void RefreshDevices(object sender, EventArgs e)
        {
            fileSystem = Globals.CloudManager.PersonalClouds.FirstOrDefault()?.RootFS;
            if (fileSystem == null)
            {
                StartActivity(typeof(OnboardingActivity));
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
