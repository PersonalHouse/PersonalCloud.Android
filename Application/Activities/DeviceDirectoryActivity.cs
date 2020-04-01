using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Views;
using Android.Webkit;

using AndroidX.AppCompat.View.Menu;
using AndroidX.AppCompat.Widget;
using AndroidX.Core.Content;
using AndroidX.RecyclerView.Widget;

using Binding;

using DavideSteduto.FlexibleAdapter;
using DavideSteduto.FlexibleAdapter.Common;
using DavideSteduto.FlexibleAdapter.Helpers;
using DavideSteduto.FlexibleAdapter.Items;

using NSPersonalCloud.Interfaces.FileSystem;
using NSPersonalCloud.RootFS;

using Unishare.Apps.DevolMobile.Activities;
using Unishare.Apps.DevolMobile.Items;

namespace Unishare.Apps.DevolMobile
{
    [Activity(Name = "com.daoyehuo.UnishareLollipop.DeviceDirectoryActivity", Label = "@string/app_name", Theme = "@style/AppTheme", ScreenOrientation = ScreenOrientation.Portrait)]
    public class DeviceDirectoryActivity : NavigableActivity, FlexibleAdapter.IOnItemClickListener, FlexibleAdapter.IOnItemLongClickListener
    {
        public const string ExtraDeviceName = "DeviceDirectoryActivity.DeviceName";

        private const int CallbackUpload = 10000;
        private const int CallbackDownload = 10001;

        internal cloud_browser R { get; private set; }

        private FlexibleAdapter adapter;
        private RecyclerView.LayoutManager layoutManager;

        private RootFileSystem FileSystem { get; set; }
        private string WorkingPath { get; set; }

        private string deviceName;
        private List<FileSystemEntry> items;
        private int depth;

        private string moveSource;
        private FileSystemEntry downloadSource;
        private bool refreshNow;

        protected override void OnCreate(Android.OS.Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.cloud_browser);
            R = new cloud_browser(this);

            FileSystem = Globals.CloudManager.PersonalClouds[0].RootFS;
            deviceName = Intent.GetStringExtra(ExtraDeviceName);
            if (FileSystem == null || deviceName == null) throw new InvalidOperationException("Internal error: RootFileSystem or CurrentDevice is null.");

            SupportActionBar.Title = deviceName;
            WorkingPath = Path.AltDirectorySeparatorChar + deviceName;

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

        protected override void OnResume()
        {
            base.OnResume();
            if (refreshNow) RefreshDirectory(this, EventArgs.Empty);
            refreshNow = false;
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.directory_actions, menu);
            if (moveSource == null) menu.FindItem(Resource.Id.action_paste).SetVisible(false);

            return base.OnCreateOptionsMenu(menu);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.action_new_folder:
                {
                    this.ShowEditorAlert("输入文件夹名称", "新建文件夹", null, "创建", text => {
                        if (string.IsNullOrWhiteSpace(text))
                        {
                            this.ShowAlert("文件夹名称无效", null);
                            return;
                        }

#pragma warning disable 0618
                        var progress = new ProgressDialog(this);
                        progress.SetCancelable(false);
                        progress.SetMessage("正在创建……");
                        progress.Show();
#pragma warning restore 0618
                        Task.Run(async () => {
                            try
                            {
                                var path = Path.Combine(WorkingPath, text);
                                await FileSystem.CreateDirectoryAsync(path).ConfigureAwait(false);

                                RunOnUiThread(() => {
                                    progress.Dismiss();
                                    RefreshDirectory(this, EventArgs.Empty);
                                });
                            }
                            catch (HttpRequestException exception)
                            {
                                RunOnUiThread(() => {
                                    progress.Dismiss();
                                    this.ShowAlert("与远程设备通讯时遇到问题", exception.Message);
                                });
                            }
                            catch (Exception exception)
                            {
                                RunOnUiThread(() => {
                                    progress.Dismiss();
                                    this.ShowAlert("无法创建文件夹", exception.GetType().Name);
                                });
                            }
                        });
                    }, "取消", null);
                    return true;
                }

                case Resource.Id.action_upload_file:
                {
                    StartActivityForResult(typeof(UploadFilesActivity), CallbackUpload);
                    return true;
                }

                case Resource.Id.action_paste:
                {
#pragma warning disable 0618
                    var progress = new ProgressDialog(this);
                    progress.SetCancelable(false);
                    progress.SetMessage("正在移动……");
                    progress.Show();
#pragma warning restore 0618

                    Task.Run(async () => {
                        try
                        {
                            var fileName = Path.GetFileName(moveSource);
                            var path = Path.Combine(WorkingPath, fileName);
                            await FileSystem.RenameAsync(moveSource, path).ConfigureAwait(false);
                            moveSource = null;

                            RunOnUiThread(() => {
                                progress.Dismiss();
                                InvalidateOptionsMenu();
                                RefreshDirectory(this, EventArgs.Empty);
                            });
                        }
                        catch (HttpRequestException exception)
                        {
                            RunOnUiThread(() => {
                                progress.Dismiss();
                                this.ShowAlert("与远程设备通讯时遇到问题", exception.Message);
                            });
                        }
                        catch (Exception exception)
                        {
                            RunOnUiThread(() => {
                                progress.Dismiss();
                                this.ShowAlert("无法移动至当前文件夹", exception.GetType().Name);
                            });
                        }
                    });
                    return true;
                }
            }

            return base.OnOptionsItemSelected(item);
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            switch (requestCode)
            {
                case CallbackUpload:
                {
                    if (resultCode != Result.Ok) return;
                    var path = data.GetStringExtra(UploadFilesActivity.ResultPath);
                    if (string.IsNullOrEmpty(path)) throw new InvalidOperationException();

#pragma warning disable 0618
                    var progress = new ProgressDialog(this);
                    progress.SetCancelable(false);
                    progress.SetMessage("正在上传……");
                    progress.Show();
#pragma warning restore 0618
                    Task.Run(async () => {
                        try
                        {
                            var fileName = Path.GetFileName(path);
                            var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                            var remotePath = Path.Combine(WorkingPath, fileName);
                            await FileSystem.WriteFileAsync(remotePath, stream).ConfigureAwait(false);

                            RunOnUiThread(() => {
                                progress.Dismiss();
                                RefreshDirectory(this, EventArgs.Empty);
                            });
                        }
                        catch (HttpRequestException exception)
                        {
                            RunOnUiThread(() => {
                                progress.Dismiss();
                                this.ShowAlert("与远程设备通讯时遇到问题", exception.Message);
                            });
                        }
                        catch (Exception exception)
                        {
                            RunOnUiThread(() => {
                                progress.Dismiss();
                                this.ShowAlert("无法上传此文件", exception.GetType().Name);
                            });
                        }
                    });
                    return;
                }

                case CallbackDownload:
                {
                    if (resultCode != Result.Ok || downloadSource is null) return;
                    var path = data.GetStringExtra(ChooseFolderActivity.ResultPath);
                    if (string.IsNullOrEmpty(path)) throw new InvalidOperationException();
                    path = Path.Combine(path, downloadSource.Name);
                    PreparePlaceholder(downloadSource, path, () => {
                        this.ShowAlert("文件已下载", $"“{downloadSource.Name}”已下载为 {path}");
                        downloadSource = null;
                    }, exception => {
                        if (exception is HttpRequestException http) this.ShowAlert("与远程设备通讯时遇到问题", http.Message);
                        else this.ShowAlert("无法下载文件", exception.GetType().Name);
                        downloadSource = null;
                    });
                    return;
                }

                default:
                {
                    base.OnActivityResult(requestCode, resultCode, data);
                    return;
                }
            }
        }

        public bool OnItemClick(View view, int position)
        {
            if (position == 0 && depth != 0)
            {
                WorkingPath = Path.GetDirectoryName(WorkingPath.TrimEnd(Path.AltDirectorySeparatorChar));
                depth -= 1;
                RefreshDirectory(this, EventArgs.Empty);
                return false;
            }

            var item = items[depth == 0 ? position : (position - 1)];
            if (item.IsDirectory)
            {
                WorkingPath = Path.Combine(WorkingPath, item.Name);
                depth += 1;
                RefreshDirectory(this, EventArgs.Empty);
            }
            else
            {
                var filePath = Path.Combine(ExternalCacheDir.AbsolutePath, item.Name);
                if (File.Exists(filePath))
                {
                    var fileInfo = new FileInfo(filePath);
                    if (fileInfo.Length == item.Size)
                    {
                        // Open file.
                        var extension = Path.GetExtension(item.Name)?.TrimStart('.');
                        var mime = string.IsNullOrEmpty(extension) ? null : MimeTypeMap.Singleton.GetMimeTypeFromExtension(extension);
                        var intent = new Intent(Intent.ActionSend);
                        var file = new Java.IO.File(ExternalCacheDir, item.Name);
                        var fileUri = FileProvider.GetUriForFile(this, "com.daoyehuo.Unishare.FileProvider", file);
                        intent.PutExtra(Intent.ExtraStream, fileUri);
                        intent.SetData(fileUri);
                        intent.SetType(mime);
                        intent.AddFlags(ActivityFlags.GrantReadUriPermission | ActivityFlags.NewTask);
                        var chooser = Intent.CreateChooser(intent, "打开方式…");
                        chooser.AddFlags(ActivityFlags.GrantReadUriPermission | ActivityFlags.NewTask);
                        StartActivity(chooser);
                        return false;
                    }
                    else
                    {
                        try { fileInfo.Delete(); }
                        catch { }
                    }
                }

                PreparePlaceholder(item, filePath, () => {
                    var extension = Path.GetExtension(item.Name)?.TrimStart('.');
                    var mime = string.IsNullOrEmpty(extension) ? null : MimeTypeMap.Singleton.GetMimeTypeFromExtension(extension);
                    var intent = new Intent(Intent.ActionSend);
                    var file = new Java.IO.File(ExternalCacheDir, item.Name);
                    var fileUri = FileProvider.GetUriForFile(this, "com.daoyehuo.Unishare.FileProvider", file);
                    intent.PutExtra(Intent.ExtraStream, fileUri);
                    intent.SetData(fileUri);
                    intent.SetType(mime);
                    intent.AddFlags(ActivityFlags.GrantReadUriPermission | ActivityFlags.NewTask);
                    var chooser = Intent.CreateChooser(intent, "打开方式…");
                    chooser.AddFlags(ActivityFlags.GrantReadUriPermission | ActivityFlags.NewTask);
                    StartActivity(chooser);
                }, exception => {
                    if (exception is HttpRequestException http) this.ShowAlert("与远程设备通讯时遇到问题", http.Message);
                    else this.ShowAlert("无法下载文件", exception.GetType().Name);
                });
            }

            return false;
        }

        public void OnItemLongClick(int position)
        {
            if (position == 0 && depth != 0) return;

            var item = items[depth == 0 ? position : (position - 1)];
            var view = layoutManager.FindViewByPosition(position);
            var popup = new PopupMenu(this, view);
            popup.MenuInflater.Inflate(Resource.Menu.file_actions, popup.Menu);

            if (item.IsDirectory) popup.Menu.FindItem(Resource.Id.action_download).SetVisible(false);

            popup.MenuItemClick += (o, e) => {
                switch (e.Item.ItemId)
                {
                    case Resource.Id.action_download:
                    {
                        downloadSource = item;
                        StartActivityForResult(typeof(ChooseFolderActivity), CallbackDownload);
                        return;
                    }

                    case Resource.Id.action_move:
                    {
                        moveSource = Path.Combine(WorkingPath, item.Name);
                        InvalidateOptionsMenu();
                        return;
                    }

                    case Resource.Id.action_rename:
                    {
                        this.ShowEditorAlert("输入新名称", item.Name, item.Name, "保存", text => {
                            if (string.IsNullOrWhiteSpace(text))
                            {
                                this.ShowAlert("新名称无效", null);
                                return;
                            }

                            if (text == item.Name) return;

#pragma warning disable 0618
                            var progress = new ProgressDialog(this);
                            progress.SetCancelable(false);
                            progress.SetMessage("正在重命名……");
                            progress.Show();
#pragma warning restore 0618
                            Task.Run(async () => {
                                try
                                {
                                    var path = Path.Combine(WorkingPath, item.Name);
                                    await FileSystem.RenameAsync(path, text).ConfigureAwait(false);

                                    RunOnUiThread(() => {
                                        progress.Dismiss();
                                        RefreshDirectory(this, EventArgs.Empty);
                                    });
                                }
                                catch (HttpRequestException exception)
                                {
                                    RunOnUiThread(() => {
                                        progress.Dismiss();
                                        this.ShowAlert("与远程设备通讯时遇到问题", exception.Message);
                                    });
                                }
                                catch (Exception exception)
                                {
                                    RunOnUiThread(() => {
                                        progress.Dismiss();
                                        this.ShowAlert("无法重命名此项目", exception.GetType().Name);
                                    });
                                }
                            });
                        }, "取消", null);
                        return;
                    }

                    case Resource.Id.action_delete:
                    {
                        this.ShowAlert("删除此项目？", $"将从远程设备上删除“{item.Name}”。" + Environment.NewLine + Environment.NewLine + "如果此项目是文件夹或包，其中的内容将被一同删除。", "取消", null, "删除", () => {
#pragma warning disable 0618
                            var progress = new ProgressDialog(this);
                            progress.SetCancelable(false);
                            progress.SetMessage("正在删除……");
                            progress.Show();
#pragma warning restore 0618

                            Task.Run(async () => {
                                try
                                {
                                    var path = Path.Combine(WorkingPath, item.Name);
                                    if (item.IsDirectory) path += Path.AltDirectorySeparatorChar;
                                    await FileSystem.DeleteAsync(path).ConfigureAwait(false);

                                    RunOnUiThread(() => {
                                        progress.Dismiss();
                                        RefreshDirectory(this, EventArgs.Empty);
                                    });
                                }
                                catch (HttpRequestException exception)
                                {
                                    RunOnUiThread(() => {
                                        progress.Dismiss();
                                        this.ShowAlert("与远程设备通讯时遇到问题", exception.Message);
                                    });
                                }
                                catch (Exception exception)
                                {
                                    RunOnUiThread(() => {
                                        progress.Dismiss();
                                        this.ShowAlert("无法删除此项目", exception.GetType().Name);
                                    });
                                }
                            });
                        });
                        return;
                    }
                }
            };

            if (popup.Menu.Size() != 0)
            {
                var helper = new MenuPopupHelper(this, (MenuBuilder) popup.Menu, view);
                helper.SetForceShowIcon(true);
                helper.Show();
            }
        }

        private void RefreshDirectory(object sender, EventArgs e)
        {
            if (!R.list_reloader.Refreshing) R.list_reloader.Refreshing = true;

            Task.Run(async () => {
                var models = new List<IFlexible>();
                if (depth != 0)
                {
                    var parentPath = Path.GetFileName(Path.GetDirectoryName(WorkingPath.TrimEnd(Path.AltDirectorySeparatorChar)).TrimEnd(Path.AltDirectorySeparatorChar));
                    models.Add(new FolderGoBack(parentPath));
                }

                try
                {
                    var files = await FileSystem.EnumerateChildrenAsync(WorkingPath).ConfigureAwait(false);
                    items = files.Where(x => !x.Attributes.HasFlag(FileAttributes.Hidden) && !x.Attributes.HasFlag(FileAttributes.System))
                                 .ToList();
                    models.AddRange(items.Select(x => new FileFolder(x)));
                }
                catch (HttpRequestException exception)
                {
                    RunOnUiThread(() => this.ShowAlert("与远程设备通讯时遇到问题", exception.Message));
                }
                catch (Exception exception)
                {
                    RunOnUiThread(() => this.ShowAlert("无法打开文件夹", exception.GetType().Name));
                }

                RunOnUiThread(() => {
                    adapter.UpdateDataSet(models, true);
                    if (R.list_reloader.Refreshing) R.list_reloader.Refreshing = false;
                });
            });
        }

        #region Download

        private void PreparePlaceholder(FileSystemEntry item, string cachePath, Action onCompletion, Action<Exception> onError)
        {
            if (File.Exists(cachePath))
            {
                this.ShowAlert("替换本地同名文件？", $"下载目录中已存在同名文件“{item.Name}”，收藏新文件将替换旧文件。" +
                    Environment.NewLine + Environment.NewLine + "如果您想要同时保留新、旧文件，请在系统文件管理器中手动重命名冲突的文件。",
                    "取消", null, "替换", () => {
                        try { File.Delete(cachePath); }
                        catch { }
                        PrepareConnection(item, cachePath, onCompletion, onError);
                    }, true);
                return;
            }

            PrepareConnection(item, cachePath, onCompletion, onError);
        }

        private void PrepareConnection(FileSystemEntry item, string cachePath, Action onCompletion, Action<Exception> onError)
        {
            if (item.Size > 100000000)
            {
                this.ShowAlert("立即下载此文件？", "此文件尚未下载并且大小可能超过 100 MB，下载将需要一段时间。", "开始下载", () => {
                    DownloadFile(item, cachePath, onCompletion, onError);
                }, "取消", null, true);
                return;
            }

            DownloadFile(item, cachePath, onCompletion, onError);
        }

        private void DownloadFile(FileSystemEntry item, string cachePath, Action onCompletion, Action<Exception> onError)
        {
            if (File.Exists(cachePath))
            {
                this.ShowAlert("无法下载文件", "文件访问冲突，请重试。");
                return;
            }

#pragma warning disable 0618
            var progress = new ProgressDialog(this);
            progress.SetCancelable(false);
            progress.SetMessage("正在下载……");
            progress.Show();
#pragma warning restore 0618
            Task.Run(async () => {
                try
                {
                    var source = Path.Combine(WorkingPath, item.Name);
                    var target = new FileStream(cachePath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None);
                    await (await FileSystem.ReadFileAsync(source).ConfigureAwait(false)).CopyToAsync(target).ConfigureAwait(false);
                    await target.DisposeAsync().ConfigureAwait(false);

                    RunOnUiThread(() => {
                        progress.Dismiss();
                        onCompletion?.Invoke();
                    });
                }
                catch (Exception exception)
                {
                    try { File.Delete(cachePath); }
                    catch { }

                    RunOnUiThread(() => {
                        progress.Dismiss();
                        onError?.Invoke(exception);
                    });
                }
            });
        }

        #endregion Download
    }
}
