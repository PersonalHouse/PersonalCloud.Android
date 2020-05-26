using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

using Android.App;
using Android.Content.PM;
using Android.Views;

using AndroidX.RecyclerView.Widget;

using Binding;

using DavideSteduto.FlexibleAdapter;
using DavideSteduto.FlexibleAdapter.Common;
using DavideSteduto.FlexibleAdapter.Helpers;
using DavideSteduto.FlexibleAdapter.Items;

using NSPersonalCloud.Interfaces.FileSystem;
using NSPersonalCloud.RootFS;

using NSPersonalCloud.Common;
using NSPersonalCloud.DevolMobile.Items;

namespace NSPersonalCloud.DevolMobile.Activities
{
    [Activity(Name = "com.daoyehuo.UnishareLollipop.ChooseBackupDeviceActivity", Label = "@string/app_name", Theme = "@style/AppTheme", ScreenOrientation = ScreenOrientation.Portrait)]
    public class ChooseBackupDeviceActivity : NavigableActivity, FlexibleAdapter.IOnItemClickListener
    {
        internal cloud_browser R { get; private set; }

        private FlexibleAdapter adapter;
        private RecyclerView.LayoutManager layoutManager;

        private RootFileSystem FileSystem { get; set; }
        private string WorkingPath { get; set; }

        private List<FileSystemEntry> items;

        protected override void OnCreate(Android.OS.Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.cloud_browser);
            SupportActionBar.Title = GetString(Resource.String.choose_photos_backup_location);
            R = new cloud_browser(this);

            FileSystem = Globals.CloudManager.PersonalClouds[0].RootFS;
            if (FileSystem == null) throw new InvalidOperationException("Internal error: RootFileSystem or CurrentDevice is null.");

            WorkingPath = "/";

            adapter = new FlexibleAdapter(null, this);
            adapter.SetAnimationOnForwardScrolling(true);
            layoutManager = new SmoothScrollLinearLayoutManager(this);
            R.list_recycler.SetLayoutManager(layoutManager);
            R.list_recycler.SetAdapter(adapter);
            R.list_recycler.AddItemDecoration(new FlexibleItemDecoration(this).WithDefaultDivider());
            R.list_reloader.SetColorSchemeResources(Resource.Color.colorAccent);
            R.list_reloader.Refresh += RefreshDirectory;
            EmptyViewHelper.Create(adapter, R.list_empty);

            RefreshDirectory(this, EventArgs.Empty);
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.confirm, menu);
            return base.OnCreateOptionsMenu(menu);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            if (item.ItemId == Resource.Id.action_confirm)
            {
                Globals.Database.SaveSetting(UserSettings.PhotoBackupPrefix, WorkingPath);
                Finish();
                return true;
            }

            return base.OnOptionsItemSelected(item);
        }

        public override void OnBackPressed()
        {
            if (WorkingPath.Length != 1)
            {
                WorkingPath = Path.GetDirectoryName(WorkingPath.TrimEnd(Path.AltDirectorySeparatorChar));
                RefreshDirectory(this, EventArgs.Empty);
                return;
            }
            base.OnBackPressed();
        }

        private void RefreshDirectory(object sender, EventArgs e)
        {
            if (!R.list_reloader.Refreshing) R.list_reloader.Refreshing = true;

            Task.Run(async () => {
                var models = new List<IFlexible>();
                if (WorkingPath != "/")
                {
                    var parentPath = Path.GetFileName(Path.GetDirectoryName(WorkingPath.TrimEnd(Path.AltDirectorySeparatorChar)).TrimEnd(Path.AltDirectorySeparatorChar));
                    models.Add(new FolderGoBack(parentPath));
                }

                try
                {
                    var files = await FileSystem.EnumerateChildrenAsync(WorkingPath).ConfigureAwait(false);
                    items = files.Where(x => x.IsDirectory && !x.Attributes.HasFlag(FileAttributes.Hidden) && !x.Attributes.HasFlag(FileAttributes.System))
                                 .OrderBy(x => x.Name).ToList();

                    models.AddRange(items.Select(x => new FileFolder(x)));
                }
                catch (HttpRequestException exception)
                {
                    RunOnUiThread(() => this.ShowAlert(GetString(Resource.String.error_remote), exception.Message));
                }
                catch (Exception exception)
                {
                    RunOnUiThread(() => this.ShowAlert(GetString(Resource.String.error_folder_title), exception.GetType().Name));
                }

                RunOnUiThread(() => {
                    adapter.UpdateDataSet(models, true);
                    if (R.list_reloader.Refreshing) R.list_reloader.Refreshing = false;
                });
            });
        }

        public bool OnItemClick(View view, int position)
        {
            if (position == 0 && WorkingPath != "/")
            {
                WorkingPath = Path.GetDirectoryName(WorkingPath.TrimEnd(Path.AltDirectorySeparatorChar));
                RefreshDirectory(this, EventArgs.Empty);
                return false;
            }

            var item = items[WorkingPath == "/" ? position : (position - 1)];
            WorkingPath = Path.Combine(WorkingPath, item.Name);
            RefreshDirectory(this, EventArgs.Empty);
            return false;
        }
    }
}
