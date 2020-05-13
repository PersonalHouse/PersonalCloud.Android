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
    public class FinderFragment : Fragment, IOnItemClickListener, IOnItemLongClickListener, IBackButtonHandler
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
                    Activity.ShowEditorAlert(GetString(Resource.String.new_folder_name), GetString(Resource.String.new_folder_placeholder), null, GetString(Resource.String.new_folder_create), text => {
                        if (string.IsNullOrWhiteSpace(text))
                        {
                            Activity.ShowAlert(GetString(Resource.String.bad_folder_name), null);
                            return;
                        }

#pragma warning disable 0618
                        var progress = new Android.App.ProgressDialog(Context);
                        progress.SetCancelable(false);
                        progress.SetMessage(GetString(Resource.String.creating_new_folder));
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
                                    Activity.ShowAlert(GetString(Resource.String.error_remote), exception.Message);
                                });
                            }
                            catch (Exception exception)
                            {
                                Activity.RunOnUiThread(() => {
                                    progress.Dismiss();
                                    Activity.ShowAlert(GetString(Resource.String.error_new_folder), exception.GetType().Name);
                                });
                            }
                        });
                    }, GetString(Resource.String.action_cancel), null);
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
                    progress.SetMessage(GetString(Resource.String.uploading_file));
                    progress.Show();
#pragma warning restore 0618
                    Task.Run(async () => {
                        var shouldDelete = false;
                        var fileName = Path.GetFileName(path);
                        var remotePath = Path.Combine(workingPath, fileName);
                        try
                        {
                            var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                            await fileSystem.WriteFileAsync(remotePath, stream).ConfigureAwait(false);

                            Activity.RunOnUiThread(() => {
                                progress.Dismiss();
                                RefreshDirectory(this, EventArgs.Empty);
                            });
                        }
                        catch (HttpRequestException exception)
                        {
                            shouldDelete = true;
                            Activity.RunOnUiThread(() => {
                                progress.Dismiss();
                                Activity.ShowAlert(GetString(Resource.String.error_remote), exception.Message);
                            });
                        }
                        catch (Exception exception)
                        {
                            shouldDelete = true;
                            Activity.RunOnUiThread(() => {
                                progress.Dismiss();
                                Activity.ShowAlert(GetString(Resource.String.error_upload_file), exception.GetType().Name);
                            });
                        }

                        /*
                        if (shouldDelete)
                        {
                            try { await fileSystem.DeleteAsync(remotePath).ConfigureAwait(false); }
                            catch { } // Ignored.
                        }
                        */
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
                        Activity.ShowAlert(GetString(Resource.String.downloaded), GetString(Resource.String.downloaded_as, opSource.Name, path));
                        opSource = null;
                    }, exception => {
                        if (exception is HttpRequestException http) Activity.ShowAlert(GetString(Resource.String.error_remote), http.Message);
                        else Activity.ShowAlert(GetString(Resource.String.error_download_file), exception.GetType().Name);
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
                    progress.SetMessage(GetString(Resource.String.moving_file));
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
                                Activity.ShowAlert(GetString(Resource.String.error_remote), exception.Message);
                            });
                        }
                        catch (Exception exception)
                        {
                            Activity.RunOnUiThread(() => {
                                progress.Dismiss();
                                Activity.ShowAlert(GetString(Resource.String.error_move_file), exception.GetType().Name);
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
                        var chooser = Intent.CreateChooser(intent, GetString(Resource.String.open_with));
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
                    var chooser = Intent.CreateChooser(intent, GetString(Resource.String.open_with));
                    chooser.AddFlags(ActivityFlags.GrantReadUriPermission | ActivityFlags.NewTask);
                    StartActivity(chooser);
                }, exception => {
                    if (exception is HttpRequestException http) Activity.ShowAlert(GetString(Resource.String.error_remote), http.Message);
                    else Activity.ShowAlert(GetString(Resource.String.error_download_file), exception.GetType().Name);
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
                        Activity.ShowEditorAlert(GetString(Resource.String.new_file_name), item.Name, item.Name, GetString(Resource.String.action_save), text => {
                            if (string.IsNullOrWhiteSpace(text))
                            {
                                Activity.ShowAlert(GetString(Resource.String.bad_file_name), null);
                                return;
                            }

                            if (text == item.Name) return;

#pragma warning disable 0618
                            var progress = new Android.App.ProgressDialog(Context);
                            progress.SetCancelable(false);
                            progress.SetMessage(GetString(Resource.String.renaming));
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
                                        Activity.ShowAlert(GetString(Resource.String.error_remote), exception.Message);
                                    });
                                }
                                catch (Exception exception)
                                {
                                    Activity.RunOnUiThread(() => {
                                        progress.Dismiss();
                                        Activity.ShowAlert(GetString(Resource.String.error_rename_file), exception.GetType().Name);
                                    });
                                }
                            });
                        }, GetString(Resource.String.action_cancel), null);
                        return;
                    }

                    case Resource.Id.action_delete:
                    {
                        Activity.ShowAlert(GetString(Resource.String.delete_permanently),
                            GetString(Resource.String.delete_all_contents, item.Name),
                            GetString(Resource.String.action_cancel), null, GetString(Resource.String.action_delete), () => {
#pragma warning disable 0618
                                var progress = new Android.App.ProgressDialog(Context);
                                progress.SetCancelable(false);
                                progress.SetMessage(GetString(Resource.String.deleting));
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
                                            Activity.ShowAlert(GetString(Resource.String.error_remote), exception.Message);
                                        });
                                    }
                                    catch (Exception exception)
                                    {
                                        Activity.RunOnUiThread(() => {
                                            progress.Dismiss();
                                            Activity.ShowAlert(GetString(Resource.String.error_delete_file), exception.GetType().Name);
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

            var activity = (MainActivity) Activity;
            if (workingPath.Length == 1)
            {
                activity.SetActionBarTitle(Resource.String.personal_cloud);
                try { Globals.CloudManager.StartNetwork(false); }
                catch { } // Ignored.
            }

            Task.Run(async () => {
                string title = null;
                if (workingPath.Length != 1)
                {
                    var deviceNameEnd = workingPath.IndexOf(Path.AltDirectorySeparatorChar, 1);
                    if (deviceNameEnd != -1) title = workingPath.Substring(1, deviceNameEnd).Trim(Path.AltDirectorySeparatorChar);
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
                    items = files.Where(x => !x.Attributes.HasFlag(FileAttributes.Hidden) && !x.Attributes.HasFlag(FileAttributes.System))
                                 .OrderByDescending(x => x.IsDirectory).ThenBy(x => x.Name).ToList();
                    models.AddRange(items.Select(x => new FileFolder(x)));
                }
                catch (HttpRequestException exception)
                {
                    items = null;
                    Activity.RunOnUiThread(() => {
                        Activity.ShowAlert(GetString(Resource.String.error_remote), exception.Message);
                    });
                }
                catch (Exception exception)
                {
                    Activity.RunOnUiThread(() => {
                        Activity.ShowAlert(GetString(Resource.String.error_folder_title), exception.GetType().Name);
                    });
                }

                Activity.RunOnUiThread(() => {
                    adapter.UpdateDataSet(models, true);
                    if (!string.IsNullOrEmpty(title)) activity.SetActionBarTitle(title);
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
                Activity.ShowAlert(GetString(Resource.String.replace_local),
                    GetString(Resource.String.replace_local_resolve_manually, item.Name),
                    GetString(Resource.String.action_cancel), null, GetString(Resource.String.replace_file), () => {
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
                Activity.ShowAlert(GetString(Resource.String.download_oversized_file), GetString(Resource.String.size_over_100MB), GetString(Resource.String.start_downloading), () => {
                    DownloadFile(item, cachePath, onCompletion, onError);
                }, GetString(Resource.String.action_cancel), null, true);
                return;
            }

            DownloadFile(item, cachePath, onCompletion, onError);
        }

        private void DownloadFile(FileSystemEntry item, string cachePath, Action onCompletion, Action<Exception> onError)
        {
            if (File.Exists(cachePath))
            {
                Activity.ShowAlert(GetString(Resource.String.error_download_file), GetString(Resource.String.file_exists));
                return;
            }

#pragma warning disable 0618
            var progress = new Android.App.ProgressDialog(Context);
            progress.SetCancelable(false);
            progress.SetMessage(GetString(Resource.String.downloading));
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

        public bool OnBack()
        {
            if (workingPath.Length != 1)
            {
                workingPath = Path.GetDirectoryName(workingPath.TrimEnd(Path.AltDirectorySeparatorChar));
                RefreshDirectory(this, EventArgs.Empty);
                return true;
            }
            return false;
        }
    }
}
