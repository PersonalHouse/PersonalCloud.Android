using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;

using Newtonsoft.Json;

using NSPersonalCloud;
using NSPersonalCloud.Config;
using NSPersonalCloud.FileSharing.Aliyun;
using NSPersonalCloud.Interfaces.Apps;

using Unishare.Apps.Common;
using Unishare.Apps.Common.Models;

using VaslD.Utility.Cryptography;

namespace Unishare.Apps.DevolMobile
{
    public class AndroidDataStorage : IConfigStorage
    {
        public event EventHandler CloudSaved;

        #region Cloud

        public IEnumerable<PersonalCloudInfo> LoadCloud()
        {
            var deviceName = Globals.Database.LoadSetting(UserSettings.DeviceName) ?? "Android";
            return Globals.Database.Table<CloudModel>().Select(x => {
                var alibaba = Globals.Database.Table<AlibabaOSS>().Where(y => y.Cloud == x.Id).Select(y => {
                    var config = new OssConfig {
                        OssEndpoint = y.Endpoint,
                        BucketName = y.Bucket,
                    };
                    using var cipher = new PasswordCipher(y.Id.ToString("N", CultureInfo.InvariantCulture), x.Key);
                    config.AccessKeyId = cipher.DecryptContinuousText(y.AccessID);
                    config.AccessKeySecret = cipher.DecryptContinuousText(y.AccessSecret);
                    return new StorageProviderInfo {
                        Id = y.Id,
                        Type = StorageProviderInstance.TypeAliYun,
                        Name = y.Name,
                        Visibility = (StorageProviderVisibility) y.Visibility,
                        Settings = JsonConvert.SerializeObject(config)
                    };
                });
                var azure = Globals.Database.Table<AzureBlob>().Where(y => y.Cloud == x.Id).Select(y => {
                    var config = new AzureBlobConfig {
                        BlobName = y.Container
                    };
                    using var cipher = new PasswordCipher(y.Id.ToString("N", CultureInfo.InvariantCulture), x.Key);
                    config.ConnectionString = cipher.DecryptTextOnce(y.Parameters);
                    return new StorageProviderInfo {
                        Id = y.Id,
                        Type = StorageProviderInstance.TypeAzure,
                        Name = y.Name,
                        Visibility = (StorageProviderVisibility) y.Visibility,
                        Settings = JsonConvert.SerializeObject(config)
                    };
                });
                var providers = new List<StorageProviderInfo>();
                providers.AddRange(alibaba);
                providers.AddRange(azure);
                return new PersonalCloudInfo(providers) {
                    Id = x.Id.ToString("N", CultureInfo.InvariantCulture),
                    DisplayName = x.Name,
                    NodeDisplayName = deviceName,
                    MasterKey = Convert.FromBase64String(x.Key),
                    TimeStamp = x.Version,
                    Apps = new List<AppLauncher>(),
                };
            });
        }

        public void SaveCloud(IEnumerable<PersonalCloudInfo> cloud)
        {
            Globals.Database.DeleteAll<CloudModel>();
            Globals.Database.DeleteAll<AlibabaOSS>();
            Globals.Database.DeleteAll<AzureBlob>();
            foreach (var item in cloud)
            {
                var cloudId = new Guid(item.Id);
                Globals.Database.Insert(new CloudModel {
                    Id = cloudId,
                    Name = item.DisplayName,
                    Key = Convert.ToBase64String(item.MasterKey),
                    Version = item.TimeStamp,
                });

                foreach (var provider in item.StorageProviders)
                {
                    switch (provider.Type)
                    {
                        case StorageProviderInstance.TypeAliYun:
                        {
                            var config = JsonConvert.DeserializeObject<OssConfig>(provider.Settings);
                            var model = new AlibabaOSS {
                                Id = provider.Id,
                                Cloud = cloudId,
                                Name = provider.Name,
                                Visibility = (int) provider.Visibility,
                                Endpoint = config.OssEndpoint,
                                Bucket = config.BucketName,
                            };
                            if (model.Id == Guid.Empty) model.Id = Guid.NewGuid();
                            using var cipher = new PasswordCipher(model.Id.ToString("N", CultureInfo.InvariantCulture), item.MasterKey);
                            model.AccessID = cipher.EncryptContinuousText(config.AccessKeyId);
                            model.AccessSecret = cipher.EncryptContinuousText(config.AccessKeySecret);
                            Globals.Database.Insert(model);
                            continue;
                        }

                        case StorageProviderInstance.TypeAzure:
                        {
                            var config = JsonConvert.DeserializeObject<AzureBlobConfig>(provider.Settings);
                            var model = new AzureBlob {
                                Id = provider.Id,
                                Cloud = cloudId,
                                Name = provider.Name,
                                Visibility = (int) provider.Visibility,
                                Container = config.BlobName
                            };
                            if (model.Id == Guid.Empty) model.Id = Guid.NewGuid();
                            using var cipher = new PasswordCipher(model.Id.ToString("N", CultureInfo.InvariantCulture), item.MasterKey);
                            model.Parameters = cipher.EncryptTextOnce(config.ConnectionString);
                            Globals.Database.Insert(model);
                            continue;
                        }
                    }
                }
            }

            CloudSaved?.Invoke(this, EventArgs.Empty);
        }

        #endregion Cloud

        #region Config

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

        public void SaveConfiguration(ServiceConfiguration config)
        {
            Globals.Database.SaveSetting(UserSettings.DeviceId, config.Id.ToString("N"));
            Globals.Database.SaveSetting(UserSettings.DevicePort, config.Port.ToString(CultureInfo.InvariantCulture));
        }

        #endregion Config

        #region Apps

        public List<(string, string)> GetApp(string appId)
        {
            return Globals.Database.Table<WebApp>().Where(x => x.Name == appId)
                                   .Select(x => (x.Cloud.ToString("N", CultureInfo.InvariantCulture), x.Parameters))
                                   .ToList();
        }

        public void SaveApp(string appId, string cloudId, string config)
        {
            var guid = new Guid(cloudId);
            var old = Globals.Database.Find<WebApp>(x => x.Cloud == guid && x.Name == appId);
            if (old != null) Globals.Database.Delete(old);
            Globals.Database.Insert(new WebApp {
                Cloud = new Guid(cloudId),
                Name = appId,
                Parameters = config
            });
        }

        #endregion Apps
    }
}
