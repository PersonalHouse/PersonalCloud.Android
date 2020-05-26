using System;

using Android.Content;
using Android.Net.Wifi;

namespace NSPersonalCloud.DevolMobile.BroadcastReceivers
{
    public class WiFiStateReceiver : BroadcastReceiver
    {
        private DateTime lastUpdate;

        public override void OnReceive(Context context, Intent intent)
        {
            if (intent.Action != WifiManager.NetworkStateChangedAction || DateTime.Now - lastUpdate < TimeSpan.FromSeconds(5)) return;

            try
            {
                Globals.CloudManager?.StartNetwork(false);
                lastUpdate = DateTime.Now;
            }
            catch
            {
                // Ignored.
            }
        }
    }
}
