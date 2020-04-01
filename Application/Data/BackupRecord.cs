using System;

using SQLite;

namespace Unishare.Apps.DevolMobile.Data
{
    [Table("Backup")]
    public class BackupRecord
    {
        [PrimaryKey]
        public string LocalPath { get; set; }

        public string RemotePath { get; set; }

        public DateTime Timestamp { get; set; }

        public Guid WorkId { get; set; }
    }
}
