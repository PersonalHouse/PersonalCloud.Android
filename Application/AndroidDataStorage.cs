using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;

using NSPersonalCloud;
using NSPersonalCloud.Config;

using Unishare.Apps.Common;
using Unishare.Apps.Common.Models;

namespace Unishare.Apps.DevolMobile
{
    public class AndroidDataStorage : IConfigStorage
    {
        public event EventHandler CloudSaved;

        public IEnumerable<PersonalCloudInfo> LoadCloud()
        {
            var deviceName = Globals.Database.LoadSetting(UserSettings.DeviceName) ?? "Android";
            return Globals.Database.Table<CloudModel>().Select(x => new PersonalCloudInfo {
                Id = x.Id.ToString("N"),
                DisplayName = x.Name,
                NodeDisplayName = deviceName,
                MasterKey = Convert.FromBase64String(x.Key)
            });
        }

        public ServiceConfiguration LoadConfiguration()
        {
            var id = Globals.Database.LoadSetting(UserSettings.DeviceId);
            if (id == null) return null;

            var port = int.Parse(Globals.Database.LoadSetting(UserSettings.DevicePort));
            if (port <= IPEndPoint.MinPort || port > IPEndPoint.MaxPort) throw new InvalidOperationException();
            return new ServiceConfiguration {
                Id = new Guid(id),
                Port = port
            };
        }

        public void SaveCloud(IEnumerable<PersonalCloudInfo> cloud)
        {
            Globals.Database.DeleteAll<CloudModel>();
            foreach (var item in cloud)
            {
                Globals.Database.Insert(new CloudModel {
                    Id = new Guid(item.Id),
                    Name = item.DisplayName,
                    Key = Convert.ToBase64String(item.MasterKey),
                    Version = Definition.CloudVersion
                });
            }

            CloudSaved?.Invoke(this, EventArgs.Empty);
        }

        public void SaveConfiguration(ServiceConfiguration config)
        {
            Globals.Database.SaveSetting(UserSettings.DeviceId, config.Id.ToString("N"));
            Globals.Database.SaveSetting(UserSettings.DevicePort, config.Port.ToString(CultureInfo.InvariantCulture));
        }
    }
}
