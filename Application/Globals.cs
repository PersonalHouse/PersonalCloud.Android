using Microsoft.Extensions.Logging;

using NSPersonalCloud;

using SQLite;

namespace NSPersonalCloud.DevolMobile
{
    public static class Globals
    {
        public static ILoggerFactory Loggers { get; internal set; }
        public static SQLiteConnection Database { get; internal set; }
        public static IConfigStorage Storage { get; internal set; }
        public static PCLocalService CloudManager { get; internal set; }
        public static Zio.IFileSystem FileSystem { get; internal set; }

        public static void SetupFS(string sharingRoot)
        {
            var rootfs = new Zio.FileSystems.PhysicalFileSystem();
            Zio.IFileSystem fsfav;
            if (!string.IsNullOrWhiteSpace(sharingRoot))
            {
                fsfav = new Zio.FileSystems.SubFileSystem(rootfs, sharingRoot);
            }
            else
            {
                fsfav = new Zio.FileSystems.MemoryFileSystem();
            }
            Globals.FileSystem = fsfav;
            try { Globals.CloudManager.FileSystem = fsfav; } catch { }
            try { Globals.CloudManager?.StopNetwork(); } catch { }
            try { Globals.CloudManager?.StartNetwork(true); } catch { }
        }
    }
}
