using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;

using AndroidX.Core.App;
using AndroidX.Core.Content;
using AndroidX.Work;

using NSPersonalCloud;

using Sentry;
using Sentry.Protocol;

using SQLite;

using Unishare.Apps.Common;
using Unishare.Apps.Common.Models;
using Unishare.Apps.DevolMobile.Data;

namespace Unishare.Apps.DevolMobile.Workers
{
    public class PhotosBackupWorker : Worker
    {
        private const string NotificationChannelId = "Unishare.Apps.DevolMobile.ForegroundWorkers";

        private Context Context { get; }
        private WorkerParameters Parameters { get; }

        public PhotosBackupWorker(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        public PhotosBackupWorker(Context context, WorkerParameters workerParams) : base(context, workerParams)
        {
            Context = context;
            Parameters = workerParams;
        }

        public override Result DoWork()
        {
            SentrySdk.AddBreadcrumb("Backup worker started.");

            if (Globals.Database == null)
            {
                var databasePath = Path.Combine(Context.FilesDir.AbsolutePath, "Preferences.sqlite3");
                Globals.Database = new SQLiteConnection(databasePath, SQLiteOpenFlags.Create | SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.FullMutex);
                Globals.Database.CreateTable<KeyValueModel>();
                Globals.Database.CreateTable<CloudModel>();
                Globals.Database.CreateTable<AlibabaOSS>();
                Globals.Database.CreateTable<AzureBlob>();
                Globals.Database.CreateTable<Launcher>();
                Globals.Database.CreateTable<WebApp>();
                Globals.Database.CreateTable<BackupRecord>();
            }

            if (!Globals.Database.CheckSetting(UserSettings.AutoBackupPhotos, "1")) return Result.InvokeSuccess();

            var workId = new Guid(Id.ToString());
            Globals.Database.SaveSetting(UserSettings.PhotoBackupTask, workId.ToString("N"));

            var path = Globals.Database.LoadSetting(UserSettings.PhotoBackupPrefix);
            if (string.IsNullOrEmpty(path)) return Result.InvokeFailure();

            if (ContextCompat.CheckSelfPermission(Context, Manifest.Permission.ReadExternalStorage) != Permission.Granted)
            {
                SentrySdk.AddBreadcrumb("Backup worker failed: Permission not granted.", level: BreadcrumbLevel.Error);
                return Result.InvokeFailure();
            }

            if (Globals.Storage == null) Globals.Storage = new AndroidDataStorage();
            if (Globals.FileSystem == null) Globals.FileSystem = new VirtualFileSystem(null);

            var appsPath = Path.Combine(Context.FilesDir.AbsolutePath, "Static");
                Directory.CreateDirectory(appsPath);
            if (Globals.CloudManager == null) Globals.CloudManager = new PCLocalService(Globals.Storage, Globals.Loggers, Globals.FileSystem, appsPath);
            if (Globals.CloudManager.PersonalClouds.Count < 1) return Result.InvokeFailure();
            Globals.CloudManager.NetworkRefeshNodes();
            Task.Delay(TimeSpan.FromSeconds(5)).Wait();

            var fileSystem = Globals.CloudManager.PersonalClouds[0].RootFS;
            var dcimPath = Path.Combine(path, Globals.Database.LoadSetting(UserSettings.DeviceName), "DCIM/");
            try
            {
                fileSystem.CreateDirectoryAsync(dcimPath).AsTask().Wait();
            }
            catch
            {
                SentrySdk.AddBreadcrumb("Remote directory is inaccessible or already exists.");
            }

            try
            {
                fileSystem.EnumerateChildrenAsync(dcimPath).AsTask().Wait();
            }
            catch (Exception exception)
            {
                SentrySdk.AddBreadcrumb("Remote directory is inaccessible. Backup failed.", level: BreadcrumbLevel.Error);
                SentrySdk.CaptureException(exception);
                return Result.InvokeFailure();
            }

#if !DEBUG
            SetForegroundAsync(PrepareForForeground());
#endif

            var failures = 0;
            var directory = new DirectoryInfo(Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDcim).Path);
            if (directory.Exists)
            {
                foreach (var file in directory.EnumerateFileSystemInfos("*", SearchOption.AllDirectories))
                {
                    if (file is DirectoryInfo) continue;
                    if (file.Attributes.HasFlag(FileAttributes.Hidden) || file.Attributes.HasFlag(FileAttributes.System)) continue;
                    if (Globals.Database.Find<BackupRecord>(file.FullName) != null) continue;

                    try
                    {
                        var remoteName = file.FullName.Substring(directory.FullName.Length + 1);
                        var remotePath = Path.Combine(dcimPath, remoteName);
                        using var stream = ((FileInfo) file).OpenRead();
                        fileSystem.WriteFileAsync(remotePath, stream).AsTask().Wait();
                        Globals.Database.InsertOrReplace(new BackupRecord {
                            LocalPath = file.FullName,
                            RemotePath = remotePath,
                            Timestamp = DateTime.Now,
                            WorkId = workId
                        });
                    }
                    catch (Exception exception)
                    {
                        SentrySdk.AddBreadcrumb($"Cannot backup item in DCIM: {file.FullName}", level: BreadcrumbLevel.Warning);
                        SentrySdk.CaptureException(exception);

                        failures += 1;
                    }
                }
            }

            var picsPath = Path.Combine(path, Globals.Database.LoadSetting(UserSettings.DeviceName), "Pictures/");
            try
            {
                fileSystem.CreateDirectoryAsync(picsPath).AsTask().Wait();
            }
            catch
            {
                SentrySdk.AddBreadcrumb("Remote directory is inaccessible or already exists.");
            }

            try
            {
                fileSystem.EnumerateChildrenAsync(picsPath).AsTask().Wait();
            }
            catch (Exception exception)
            {
                SentrySdk.AddBreadcrumb("Remote directory is inaccessible. Backup failed.", level: BreadcrumbLevel.Error);
                SentrySdk.CaptureException(exception);
                return Result.InvokeFailure();
            }

            directory = new DirectoryInfo(Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryPictures).Path);
            if (directory.Exists)
            {
                foreach (var file in directory.EnumerateFileSystemInfos("*", SearchOption.AllDirectories))
                {
                    if (file is DirectoryInfo) continue;
                    if (file.Attributes.HasFlag(FileAttributes.Hidden) || file.Attributes.HasFlag(FileAttributes.System)) continue;
                    if (Globals.Database.Find<BackupRecord>(file.FullName) != null) continue;

                    try
                    {
                        var remoteName = file.FullName.Substring(directory.FullName.Length + 1);
                        var remotePath = Path.Combine(picsPath, remoteName);
                        using var stream = ((FileInfo) file).OpenRead();
                        fileSystem.WriteFileAsync(remotePath, stream).AsTask().Wait();
                        Globals.Database.InsertOrReplace(new BackupRecord {
                            LocalPath = file.FullName,
                            RemotePath = remotePath,
                            Timestamp = DateTime.Now,
                            WorkId = workId
                        });
                    }
                    catch (Exception exception)
                    {
                        SentrySdk.AddBreadcrumb($"Cannot backup item in Pictures: {file.FullName}", level: BreadcrumbLevel.Warning);
                        SentrySdk.CaptureException(exception);

                        failures += 1;
                    }
                }
            }

            return Result.InvokeSuccess();
        }

        private void CreateNotificationChannel(NotificationManager manager)
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.O) return;

            var channel = new NotificationChannel(NotificationChannelId, Context.GetString(Resource.String.notification_channel_backup), NotificationImportance.High) {
                Description = Context.GetString(Resource.String.notification_channel_backup_description)
            };
            manager.CreateNotificationChannel(channel);
        }

        private ForegroundInfo PrepareForForeground()
        {
            var notificationManager = (NotificationManager) Context.GetSystemService(Context.NotificationService);
            CreateNotificationChannel(notificationManager);

            var cancelIntent = WorkManager.GetInstance(Context).CreateCancelPendingIntent(Id);
            var notification = new NotificationCompat.Builder(Context, NotificationChannelId)
                .SetContentTitle(Context.GetString(Resource.String.notification_title))
                .SetTicker(Context.GetString(Resource.String.notification_ticker))
                .SetContentText(Context.GetString(Resource.String.notification_message))
                .SetSmallIcon(Resource.Mipmap.ic_launcher)
                .SetOngoing(true).AddAction(0, Context.GetString(Resource.String.action_stop), cancelIntent)
                .Build();
            var notificationId = (int) DateTime.Now.TimeOfDay.TotalSeconds;
            Globals.Database.SaveSetting(UserSettings.PhotoBackupNotification, notificationId.ToString(CultureInfo.InvariantCulture));
            return new ForegroundInfo(notificationId, notification);
        }
    }
}
