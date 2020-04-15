using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Webkit;

using AndroidX.AppCompat.View.Menu;
using AndroidX.AppCompat.Widget;
using AndroidX.Core.Content;
using AndroidX.Fragment.App;
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

using static DavideSteduto.FlexibleAdapter.FlexibleAdapter;

namespace Unishare.Apps.DevolMobile.Fragments
{
    [Register("com.daoyehuo.UnishareLollipop.FinderFragment")]
    public class FinderFragment : Fragment, IOnItemClickListener, IOnItemLongClickListener
    {
        private const int CallbackUpload = 10000;
        private const int CallbackDownload = 10001;
        private const int CallbackMove = 10002;

        internal fragment_finder R { get; private set; }

        private FlexibleAdapter adapter;
        private RecyclerView.LayoutManager layoutManager;

        private RootFileSystem fileSystem;
        private string workingPath;
        private List<FileSystemEntry> items;

        private FileSystemEntry opSource;

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Android.OS.Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.fragment_finder, container, false);
            R = new fragment_finder(view);

            adapter = new FlexibleAdapter(null, this);
            adapter.SetAnimationOnForwardScrolling(true);
            layoutManager = new SmoothScrollLinearLayoutManager(Context);
            R.list_recycler.SetLayoutManager(layoutManager);
            R.list_recycler.SetAdapter(adapter);
            R.list_recycler.AddItemDecoration(new FlexibleItemDecoration(Context).WithDefaultDivider());
            R.list_reloader.SetColorSchemeResources(Resource.Color.colorAccent);
            R.list_reloader.Refresh += RefreshDirectory;
            EmptyViewHelper.Create(adapter, R.list_empty);

            workingPath = "/";
            fileSystem = Globals.CloudManager.PersonalClouds[0].RootFS;

            HasOptionsMenu = true;
            return view;
        }

        public override void OnStart()
        {
            base.OnStart();
            RefreshDirectory(this, EventArgs.Empty);
            Globals.CloudManager.PersonalClouds[0].OnNodeChangedEvent += RefreshDevices;
        }

        public override void OnStop()
        {
            Globals.CloudManager.PersonalClouds[0].OnNodeChangedEvent -= RefreshDevices;
            base.OnStop();
        }

        public override void OnCreateOptionsMenu(IMenu menu, MenuInflater inflater)
        {
            base.OnCreateOptionsMenu(menu, inflater);
            inflater.Inflate(Resource.Menu.finder_options, menu);
            if (workingPath.Length == 1)
            {
                menu.FindItem(Resource.Id.finder_home).SetVisible(false);
                menu.FindItem(Resource.Id.finder_upload).SetVisible(false);
            }
            else menu.FindItem(Resource.Id.finder_connect).SetVisible(false);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.finder_home:
                {
                    workingPath = "/";
                    RefreshDirectory(this, EventArgs.Empty);
                    return true;
                }

                case Resource.Id.finder_connect:
                {
                    this.StartActivity(typeof(ManageConnectionsActivity));
                    return true;
                }

                case Resource.Id.action_new_folder:
                {
                    Activity.ShowEditorAlert("输入文件夹名称", "新建文件夹", null, "创建", text => {
                        if (string.IsNullOrWhiteSpace(text))
                        {
                            Activity.ShowAlert("文件夹名称无效", null);
                            return;
                        }

#pragma warning disable 0618
                        var progress = new Android.App.ProgressDialog(Context);
                        progress.SetCancelable(false);
                        progress.SetMessage("正在创建……");
                        progress.Show();
#pragma warning restore 0618
                        Task.Run(async () => {
                            try
                            {
                                var path = Path.Combine(workingPath, text);
                                await fileSystem.CreateDirectoryAsync(path).ConfigureAwait(false);

                                Activity.RunOnUiThread(() => {
                                    progress.Dismiss();
                                    RefreshDirectory(this, EventArgs.Empty);
                                });
                            }
                            catch (HttpRequestException exception)
                            {
                                Activity.RunOnUiThread(() => {
                                    progress.Dismiss();
                                    Activity.ShowAlert("与远程设备通讯时遇到问题", exception.Message);
                                });
                            }
                            catch (Exception exception)
                            {
                                Activity.RunOnUiThread(() => {
                                    progress.Dismiss();
                                    Activity.ShowAlert("无法创建文件夹", exception.GetType().Name);
                                });
                            }
                        });
                    }, "取消", null);
                    return true;
                }

                case Resource.Id.action_upload_file:
                {
                    this.StartActivityForResult(typeof(UploadFilesActivity), CallbackUpload);
                    return true;
                }

                default:
                {
                    return base.OnOptionsItemSelected(item);
                }
            }
        }

        public override void OnActivityResult(int requestCode, int resultCode, Intent data)
        {
            switch (requestCode)
            {
                case CallbackUpload:
                {
                    if ((Android.App.Result) resultCode != Android.App.Result.Ok) return;
                    var path = data.GetStringExtra(UploadFilesActivity.ResultPath);
                    if (string.IsNullOrEmpty(path)) throw new InvalidOperationException();

#pragma warning disable 0618
                    var progress = new Android.App.ProgressDialog(Context);
                    progress.SetCancelable(false);
                    progress.SetMessage("正在上传……");
                    progress.Show();
#pragma warning restore 0618
                    Task.Run(async () => {
                        try
                        {
                            var fileName = Path.GetFileName(path);
                            var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                            var remotePath = Path.Combine(workingPath, fileName);
                            await fileSystem.WriteFileAsync(remotePath, stream).ConfigureAwait(false);

                            Activity.RunOnUiThread(() => {
                                progress.Dismiss();
                                RefreshDirectory(this, EventArgs.Empty);
                            });
                        }
                        catch (HttpRequestException exception)
                        {
                            Activity.RunOnUiThread(() => {
                                progress.Dismiss();
                                Activity.ShowAlert("与远程设备通讯时遇到问题", exception.Message);
                            });
                        }
                        catch (Exception exception)
                        {
                            Activity.RunOnUiThread(() => {
                                progress.Dismiss();
                                Activity.ShowAlert("无法上传此文件", exception.GetType().Name);
                            });
                        }
                    });
                    return;
                }

                case CallbackDownload:
                {
                    if ((Android.App.Result) resultCode != Android.App.Result.Ok || opSource is null) return;
                    var path = data.GetStringExtra(ChooseFolderActivity.ResultPath);
                    if (string.IsNullOrEmpty(path)) return;

                    path = Path.Combine(path, opSource.Name);
                    PreparePlaceholder(opSource, path, () => {
                        Activity.ShowAlert("文件已下载", $"“{opSource.Name}”已下载为 {path}");
                        opSource = null;
                    }, exception => {
                        if (exception is HttpRequestException http) Activity.ShowAlert("与远程设备通讯时遇到问题", http.Message);
                        else Activity.ShowAlert("无法下载文件", exception.GetType().Name);
                        opSource = null;
                    });
                    return;
                }

                case CallbackMove:
                {
                    if ((Android.App.Result) resultCode != Android.App.Result.Ok || opSource is null) return;
                    var path = data.GetStringExtra(ChooseDestinationActivity.ResultPath);
                    if (string.IsNullOrEmpty(path)) return;

#pragma warning disable 0618
                    var progress = new Android.App.ProgressDialog(Context);
                    progress.SetCancelable(false);
                    progress.SetMessage("正在移动……");
                    progress.Show();
#pragma warning restore 0618

                    Task.Run(async () => {
                        try
                        {
                            var sourcePath = Path.Combine(workingPath, opSource.Name);
                            path = Path.Combine(path, opSource.Name);
                            await fileSystem.RenameAsync(sourcePath, path).ConfigureAwait(false);
                            opSource = null;

                            Activity.RunOnUiThread(() => {
                                progress.Dismiss();
                                RefreshDirectory(this, EventArgs.Empty);
                            });
                        }
                        catch (HttpRequestException exception)
                        {
                            Activity.RunOnUiThread(() => {
                                progress.Dismiss();
                                Activity.ShowAlert("与远程设备通讯时遇到问题", exception.Message);
                            });
                        }
                        catch (Exception exception)
                        {
                            Activity.RunOnUiThread(() => {
                                progress.Dismiss();
                                Activity.ShowAlert("无法移动至指定文件夹", exception.GetType().Name);
                            });
                        }
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

        #region Click Listeners

        public bool OnItemClick(View view, int position)
        {
            if (position == 0 && workingPath.Length != 1)
            {
                workingPath = Path.GetDirectoryName(workingPath.TrimEnd(Path.AltDirectorySeparatorChar));
                RefreshDirectory(this, EventArgs.Empty);
                return false;
            }

            var item = items[workingPath.Length == 1 ? position : (position - 1)];
            if (item.IsDirectory)
            {
                workingPath = Path.Combine(workingPath, item.Name);
                RefreshDirectory(this, EventArgs.Empty);
            }
            else
            {
                var filePath = Path.Combine(Context.ExternalCacheDir.AbsolutePath, item.Name);
                if (File.Exists(filePath))
                {
                    var fileInfo = new FileInfo(filePath);
                    if (fileInfo.Length == item.Size)
                    {
                        // Open file.
                        var extension = Path.GetExtension(item.Name)?.TrimStart('.');
                        var mime = string.IsNullOrEmpty(extension) ? null : MimeTypeMap.Singleton.GetMimeTypeFromExtension(extension);
                        var intent = new Intent(Intent.ActionSend);
                        var file = new Java.IO.File(Context.ExternalCacheDir, item.Name);
                        var fileUri = FileProvider.GetUriForFile(Context, "com.daoyehuo.Unishare.FileProvider", file);
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
                    var file = new Java.IO.File(Context.ExternalCacheDir, item.Name);
                    var fileUri = FileProvider.GetUriForFile(Context, "com.daoyehuo.Unishare.FileProvider", file);
                    intent.PutExtra(Intent.ExtraStream, fileUri);
                    intent.SetData(fileUri);
                    intent.SetType(mime);
                    intent.AddFlags(ActivityFlags.GrantReadUriPermission | ActivityFlags.NewTask);
                    var chooser = Intent.CreateChooser(intent, "打开方式…");
                    chooser.AddFlags(ActivityFlags.GrantReadUriPermission | ActivityFlags.NewTask);
                    StartActivity(chooser);
                }, exception => {
                    if (exception is HttpRequestException http) Activity.ShowAlert("与远程设备通讯时遇到问题", http.Message);
                    else Activity.ShowAlert("无法下载文件", exception.GetType().Name);
                });
            }

            return false;
        }

        public void OnItemLongClick(int position)
        {
            if (position == 0 && workingPath.Length != 1) return;

            var item = items[workingPath.Length == 1 ? position : (position - 1)];
            var view = layoutManager.FindViewByPosition(position);
            var popup = new PopupMenu(Context, view);
            popup.MenuInflater.Inflate(Resource.Menu.file_actions, popup.Menu);

            if (item.IsDirectory) popup.Menu.FindItem(Resource.Id.action_download).SetVisible(false);
            if (item.IsReadOnly)
            {
                popup.Menu.FindItem(Resource.Id.action_rename).SetVisible(false);
                popup.Menu.FindItem(Resource.Id.action_move).SetVisible(false);
                popup.Menu.FindItem(Resource.Id.action_delete).SetVisible(false);
            }
            if (item.Attributes.HasFlag(FileAttributes.Device))
            {
                popup.Menu.FindItem(Resource.Id.action_rename).SetVisible(false);
                popup.Menu.FindItem(Resource.Id.action_move).SetVisible(false);
            }

            popup.MenuItemClick += (o, e) => {
                switch (e.Item.ItemId)
                {
                    case Resource.Id.action_download:
                    {
                        opSource = item;
                        this.StartActivityForResult(typeof(ChooseFolderActivity), CallbackDownload);
                        return;
                    }

                    case Resource.Id.action_move:
                    {
                        opSource = item;
                        var intent = new Intent(Context, typeof(ChooseDestinationActivity));
                        var deviceRoot = workingPath.Substring(1);
                    var nextSeparator = deviceRoot.IndexOf(Path.AltDirectorySeparatorChar);
                    if (nextSeparator == -1) deviceRoot = "/" + deviceRoot;
                    else deviceRoot = "/" + deviceRoot.Substring(0, nextSeparator);
                        intent.PutExtra(ChooseDestinationActivity.ExtraRootPath, deviceRoot);
                        StartActivityForResult(intent, CallbackMove);
                        return;
                    }

                    case Resource.Id.action_rename:
                    {
                        Activity.ShowEditorAlert("输入新名称", item.Name, item.Name, "保存", text => {
                            if (string.IsNullOrWhiteSpace(text))
                            {
                                Activity.ShowAlert("新名称无效", null);
                                return;
                            }

                            if (text == item.Name) return;

#pragma warning disable 0618
                            var progress = new Android.App.ProgressDialog(Context);
                            progress.SetCancelable(false);
                            progress.SetMessage("正在重命名……");
                            progress.Show();
#pragma warning restore 0618
                            Task.Run(async () => {
                                try
                                {
                                    var path = Path.Combine(workingPath, item.Name);
                                    await fileSystem.RenameAsync(path, text).ConfigureAwait(false);

                                    Activity.RunOnUiThread(() => {
                                        progress.Dismiss();
                                        RefreshDirectory(this, EventArgs.Empty);
                                    });
                                }
                                catch (HttpRequestException exception)
                                {
                                    Activity.RunOnUiThread(() => {
                                        progress.Dismiss();
                                        Activity.ShowAlert("与远程设备通讯时遇到问题", exception.Message);
                                    });
                                }
                                catch (Exception exception)
                                {
                                    Activity.RunOnUiThread(() => {
                                        progress.Dismiss();
                                        Activity.ShowAlert("无法重命名此项目", exception.GetType().Name);
                                    });
                                }
                            });
                        }, "取消", null);
                        return;
                    }

                    case Resource.Id.action_delete:
                    {
                        Activity.ShowAlert("删除此项目？", $"将从远程设备上删除“{item.Name}”。" + Environment.NewLine + Environment.NewLine + "如果此项目是文件夹或包，其中的内容将被一同删除。", "取消", null, "删除", () => {
#pragma warning disable 0618
                            var progress = new Android.App.ProgressDialog(Context);
                            progress.SetCancelable(false);
                            progress.SetMessage("正在删除……");
                            progress.Show();
#pragma warning restore 0618

                            Task.Run(async () => {
                                try
                                {
                                    var path = Path.Combine(workingPath, item.Name);
                                    if (item.IsDirectory) path += Path.AltDirectorySeparatorChar;
                                    await fileSystem.DeleteAsync(path).ConfigureAwait(false);

                                    Activity.RunOnUiThread(() => {
                                        progress.Dismiss();
                                        RefreshDirectory(this, EventArgs.Empty);
                                    });
                                }
                                catch (HttpRequestException exception)
                                {
                                    Activity.RunOnUiThread(() => {
                                        progress.Dismiss();
                                        Activity.ShowAlert("与远程设备通讯时遇到问题", exception.Message);
                                    });
                                }
                                catch (Exception exception)
                                {
                                    Activity.RunOnUiThread(() => {
                                        progress.Dismiss();
                                        Activity.ShowAlert("无法删除此项目", exception.GetType().Name);
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
                var helper = new MenuPopupHelper(Context, (MenuBuilder) popup.Menu, view);
                helper.SetForceShowIcon(true);
                helper.Show();
            }
        }

        #endregion Click Listeners

        #region Utility: Refresh

        public void RefreshDevices(object sender, EventArgs e)
        {
            if (workingPath.Length != 1) return;
            Activity.RunOnUiThread(() => RefreshDirectory(this, EventArgs.Empty));
        }

        private void RefreshDirectory(object sender, EventArgs e)
        {
            if (!R.list_reloader.Refreshing) R.list_reloader.Refreshing = true;

            if (!workingPath.EndsWith(Path.AltDirectorySeparatorChar)) workingPath += Path.AltDirectorySeparatorChar;

            if (workingPath.Length == 1) Activity.Title = "个人云";

            Task.Run(async () => {
                string title = null;
                if (workingPath.Length != 1 && !string.IsNullOrEmpty(Path.GetDirectoryName(workingPath.TrimEnd(Path.AltDirectorySeparatorChar))))
                {
                    title = workingPath.Substring(1, workingPath.IndexOf(Path.AltDirectorySeparatorChar));
                }

                var models = new List<IFlexible>();
                if (workingPath.Length != 1)
                {
                    var parentPath = Path.GetFileName(Path.GetDirectoryName(workingPath.TrimEnd(Path.AltDirectorySeparatorChar)).TrimEnd(Path.AltDirectorySeparatorChar));
                    models.Add(new FolderGoBack(parentPath));
                }

                try
                {
                    var files = await fileSystem.EnumerateChildrenAsync(workingPath).ConfigureAwait(false);
                    items = files.Where(x => !x.Attributes.HasFlag(FileAttributes.Hidden) && !x.Attributes.HasFlag(FileAttributes.System)).ToList();
                    models.AddRange(items.Select(x => new FileFolder(x)));
                }
                catch (HttpRequestException exception)
                {
                    items = null;
                    Activity.RunOnUiThread(() => {
                        Activity.ShowAlert("与远程设备通讯时遇到问题", exception.Message);
                    });
                }
                catch (Exception exception)
                {
                    Activity.RunOnUiThread(() => {
                        Activity.ShowAlert("无法打开文件夹", exception.GetType().Name);
                    });
                }

                Activity.RunOnUiThread(() => {
                    adapter.UpdateDataSet(models, true);
                    if (!string.IsNullOrEmpty(title)) Activity.Title = title;
                    if (R.list_reloader.Refreshing) R.list_reloader.Refreshing = false;
                    Activity.InvalidateOptionsMenu();
                });
            });
        }

        #endregion Utility: Refresh

        #region Utility: Download

        private void PreparePlaceholder(FileSystemEntry item, string cachePath, Action onCompletion, Action<Exception> onError)
        {
            if (File.Exists(cachePath))
            {
                Activity.ShowAlert("替换本地同名文件？", $"下载目录中已存在同名文件“{item.Name}”，收藏新文件将替换旧文件。" +
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
                Activity.ShowAlert("立即下载此文件？", "此文件尚未下载并且大小可能超过 100 MB，下载将需要一段时间。", "开始下载", () => {
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
                Activity.ShowAlert("无法下载文件", "文件访问冲突，请重试。");
                return;
            }

#pragma warning disable 0618
            var progress = new Android.App.ProgressDialog(Context);
            progress.SetCancelable(false);
            progress.SetMessage("正在下载……");
            progress.Show();
#pragma warning restore 0618
            Task.Run(async () => {
                try
                {
                    var source = Path.Combine(workingPath, item.Name);
                    var target = new FileStream(cachePath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None);
                    await (await fileSystem.ReadFileAsync(source).ConfigureAwait(false)).CopyToAsync(target).ConfigureAwait(false);
                    await target.DisposeAsync().ConfigureAwait(false);

                    Activity.RunOnUiThread(() => {
                        progress.Dismiss();
                        onCompletion?.Invoke();
                    });
                }
                catch (Exception exception)
                {
                    try { File.Delete(cachePath); }
                    catch { }

                    Activity.RunOnUiThread(() => {
                        progress.Dismiss();
                        onError?.Invoke(exception);
                    });
                }
            });
        }

        #endregion Utility: Download
    }
}
