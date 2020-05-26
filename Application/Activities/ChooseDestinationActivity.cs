using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Views;

using AndroidX.RecyclerView.Widget;

using Binding;

using DavideSteduto.FlexibleAdapter;
using DavideSteduto.FlexibleAdapter.Common;
using DavideSteduto.FlexibleAdapter.Helpers;
using DavideSteduto.FlexibleAdapter.Items;

using NSPersonalCloud.Interfaces.FileSystem;
using NSPersonalCloud.RootFS;

using NSPersonalCloud.DevolMobile.Items;

using static DavideSteduto.FlexibleAdapter.FlexibleAdapter;

namespace NSPersonalCloud.DevolMobile.Activities
{
    [Activity(Name = "com.daoyehuo.UnishareLollipop.ChooseDestinationActivity", Label = "@string/app_name", Theme = "@style/AppTheme", ScreenOrientation = ScreenOrientation.Portrait)]
    public class ChooseDestinationActivity : NavigableActivity, IOnItemClickListener
    {
        public const string ExtraRootPath = "Container";
        public const string ResultPath = "SelectedDirectory";

        internal cloud_browser R { get; private set; }

        private string RootPath { get; set; }

        private FlexibleAdapter adapter;
        private RecyclerView.LayoutManager layoutManager;

        private RootFileSystem fileSystem;
        private string workingPath;
        private List<FileSystemEntry> items;

        private bool willCancel;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.cloud_browser);
            SupportActionBar.SetTitle(Resource.String.move_to);
            R = new cloud_browser(this);

            fileSystem = Globals.CloudManager.PersonalClouds[0].RootFS;
            RootPath = Intent.GetStringExtra(ExtraRootPath);
            if (string.IsNullOrEmpty(RootPath)) throw new InvalidOperationException("Internal error: IO operation root not provided.");
            RootPath = RootPath.TrimEnd(Path.AltDirectorySeparatorChar);
            workingPath = RootPath;

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
                var intent = new Intent().PutExtra(ResultPath, workingPath);
                SetResult(Result.Ok, intent);
                Finish();
                return true;
            }

            return base.OnOptionsItemSelected(item);
        }

        public override void OnBackPressed()
        {
            if (workingPath.Length != 1 && !willCancel)
            {
                var parent = Path.GetDirectoryName(workingPath.TrimEnd(Path.AltDirectorySeparatorChar));
                if (string.IsNullOrEmpty(parent)) parent = "/";
                if (!parent.StartsWith(RootPath))
                {
                    this.ShowAlert(GetString(Resource.String.destination_restricted), GetString(Resource.String.cannot_go_back));
                    willCancel = true;
                    return;
                }
                workingPath = parent;
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
                if (workingPath.Length != 1)
                {
                    var parentPath = Path.GetFileName(Path.GetDirectoryName(workingPath.TrimEnd(Path.AltDirectorySeparatorChar)).TrimEnd(Path.AltDirectorySeparatorChar));
                    models.Add(new FolderGoBack(parentPath));
                }

                try
                {
                    var files = await fileSystem.EnumerateChildrenAsync(workingPath).ConfigureAwait(false);
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

        #region Click Listeners

        public bool OnItemClick(View view, int position)
        {
            if (position == 0 && workingPath.Length != 1)
            {
                var parent = Path.GetDirectoryName(workingPath.TrimEnd(Path.AltDirectorySeparatorChar));
                if (string.IsNullOrEmpty(parent)) parent = "/";
                if (!parent.StartsWith(RootPath))
                {
                    this.ShowAlert(GetString(Resource.String.destination_restricted), GetString(Resource.String.cannot_go_back));
                    return false;
                }
                workingPath = parent;
                RefreshDirectory(this, EventArgs.Empty);
                return false;
            }

            var item = items[workingPath.Length == 1 ? position : (position - 1)];
            workingPath = Path.Combine(workingPath, item.Name);
            RefreshDirectory(this, EventArgs.Empty);
            willCancel = false;
            return false;
        }

        #endregion Click Listeners
    }
}
