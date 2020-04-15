using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;

using Newtonsoft.Json;

using NSPersonalCloud;
using NSPersonalCloud.Config;
using NSPersonalCloud.FileSharing.Aliyun;

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
            return Globals.Database.Table<CloudModel>().Select(x => {
                var providers = Globals.Database.Table<AliYunOSS>().Where(y => y.CloudId == x.Id).Select(y => {
                    var config = new OssConfig {
                        OssEndpoint = y.Endpoint,
                        BucketName = y.Bucket,
                        AccessKeyId = y.AccessID,
                        AccessKeySecret = y.AccessSecret
                    };
                    return new StorageProviderInfo {
                        Type = StorageProviderInstance.TypeAliYun,
                        Name = y.Name,
                        Visibility = (StorageProviderVisibility) y.Visibility,
                        Settings = JsonConvert.SerializeObject(config)
                    };
                }).ToList();
                if (providers is null) providers = new List<StorageProviderInfo>();
                return new PersonalCloudInfo(providers) {
                    Id = x.Id.ToString("N"),
                    DisplayName = x.Name,
                    NodeDisplayName = deviceName,
                    MasterKey = Convert.FromBase64String(x.Key),
                    TimeStamp = x.Version
                };
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
            Globals.Database.DeleteAll<AliYunOSS>();
            foreach (var item in cloud)
            {
                var id = new Guid(item.Id);
                Globals.Database.Insert(new CloudModel {
                    Id = id,
                    Name = item.DisplayName,
                    Key = Convert.ToBase64String(item.MasterKey),
                    Version = item.TimeStamp
                });

                foreach(var provider in item.StorageProviders)
                {
                    switch (provider.Type)
                    {
                        case StorageProviderInstance.TypeAliYun:
                        {
                            var config = JsonConvert.DeserializeObject<OssConfig>(provider.Settings);
                            Globals.Database.Insert(new AliYunOSS {
                                CloudId = id,
                                Name = provider.Name,
                                Visibility = (int) provider.Visibility,
                                Endpoint = config.OssEndpoint,
                                Bucket = config.BucketName,
                                AccessID = config.AccessKeyId,
                                AccessSecret = config.AccessKeySecret
                            });
                            continue;
                        }
                    }
                }
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
