using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace Modules.Communication
{
    public static class NetUtil
    {
        public static IPAddress GetIPAddres(bool isAp = true)
        {
            NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
            foreach (var adater in adapters)
            {
                if (adater.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                    continue;
#if UNITY_STANDALONE
                if (adater.OperationalStatus != OperationalStatus.Up)
                    continue; 
#else
                if (adater.Speed<=0)
                    continue;
#endif
                if (adater.Supports(NetworkInterfaceComponent.IPv4))
                {
                    UnicastIPAddressInformationCollection UniCast = adater.GetIPProperties().UnicastAddresses;
                    if (UniCast.Count > 0)
                    {
                        foreach (var uni in UniCast)
                        {
                            if (uni.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                            {
                                if (isAp)
                                    return uni.Address;
                                else if (uni.Address.GetAddressBytes()[2] == 100)
                                {
                                    return uni.Address;
                                }
                            }
                        }
                    }
                }
            }
            return null;
        }
        public static bool FindRouteDrone() 
        {
            NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
            foreach (var adater in adapters)
            {
                if (adater.OperationalStatus != OperationalStatus.Up)
                    continue;
                if (adater.Supports(NetworkInterfaceComponent.IPv4))
                {
                    UnicastIPAddressInformationCollection UniCast = adater.GetIPProperties().UnicastAddresses;
                    if (UniCast.Count > 0)
                    {
                        foreach (var uni in UniCast)
                        {
                            if (uni.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                            {
                                if (uni.Address.GetAddressBytes()[2] == 100)
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }
    }
}
