using NSPersonalCloud;

using SQLite;

namespace Unishare.Apps.DevolMobile
{
    public static class Globals
    {
        public static bool DiscoverySubscribed { get; internal set; }
        public static SQLiteConnection Database { get; internal set; }
        public static IConfigStorage Storage { get; internal set; }
        public static PCLocalService CloudManager { get; internal set; }
        public static VirtualFileSystem FileSystem { get; internal set; }
    }
}
