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
        public static VirtualFileSystem FileSystem { get; internal set; }
    }
}
