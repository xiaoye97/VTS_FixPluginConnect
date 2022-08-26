using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using BepInEx;
using HarmonyLib;
using WebSocketSharp;
using WebSocketSharp.Server;
using static VTubeStudioAPI;

namespace VTS_FixPluginConnect
{
    [BepInPlugin("me.xiaoye97.plugin.VTubeStudio.FixPluginConnect", "FixPluginConnect", "1.0.0")]
    public class FixPluginConnect : BaseUnityPlugin
    {
        public static List<IPAddress> LocalIP = new List<IPAddress>();

        void Awake()
        {
            Logger.LogInfo("FixPluginConnect Awake");
            Harmony.CreateAndPatchAll(typeof(FixPluginConnect));
        }

        [HarmonyPrefix, HarmonyPatch(typeof(VTSWebSocketReceivedMessageDispatcher), "OnOpen")]
        public static bool VTSWebSocketReceivedMessageDispatcher_OnOpen_Patch(VTSWebSocketReceivedMessageDispatcher __instance)
        {
            // bool flag = base.Context.UserEndPoint.Address.IsLocal();
            // IsLocal is use hostname
            bool flag = LocalIP.Contains(__instance.Context.UserEndPoint.Address);
            Console.WriteLine($"{__instance.Context.UserEndPoint.Address} 是否本地{flag}");
            string origin = __instance.Context.Origin;
            VTubeStudioAPI.APIDebug(string.Concat(new string[]
            {
            "A ",
            flag ? "local" : "remote",
            " plugin has connected to VTube Studio API. Origin is \"",
            origin,
            "\" (sessionID=",
            __instance.ID,
            ")"
            }), true);
            var justOpenedSessionIDs = Traverse.Create(typeof(VTubeStudioAPI)).Field("justOpenedSessionIDs").GetValue<ConcurrentQueue<string>>();
            justOpenedSessionIDs.Enqueue(__instance.ID);
            Traverse.Create(typeof(VTubeStudioAPI)).Field("justOpenedSessionIDs").SetValue(justOpenedSessionIDs);
            var sessions1 = Traverse.Create(typeof(VTubeStudioAPI)).Field("sessions").GetValue<ConcurrentDictionary<string, WebSocket>>();
            var sessions2 = Traverse.Create(__instance).Field("_sessions").GetValue<WebSocketSessionManager>();
            if (!sessions1.TryAdd(__instance.ID, sessions2[__instance.ID].Context.WebSocket))
            {
                VTubeStudioAPI.APIDebug("Failed to open session. (sessionID=" + __instance.ID + ")", true);
            }
            return false;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(APIInfoBroadcast), "getBroadcastIPs")]
        public static bool APIInfoBroadcast_getBroadcastIPs_Patch(APIInfoBroadcast __instance, ref List<IPAddress> __result)
        {
            List<IPAddress> list = new List<IPAddress>();
            List<IPAddress> list2 = new List<IPAddress>();
            // Dns.GetHostEntry(Dns.GetHostName()) throw Exception
            var myIPArray = GetByNetworkInterface();
            foreach (IPAddress ipaddress in myIPArray)
            {
                if (ipaddress.AddressFamily == AddressFamily.InterNetwork)
                {
                    list2.Add(ipaddress);
                }
            }
            foreach (IPAddress ipaddress2 in list2)
            {
                IPAddress hostMask = getHostMask(ipaddress2);
                if (hostMask != null && ipaddress2 != null)
                {
                    byte[] array = new byte[4];
                    byte[] array2 = new byte[4];
                    for (int j = 0; j < 4; j++)
                    {
                        byte a = hostMask.GetAddressBytes().ElementAt(j);
                        array[j] = (byte)~a;
                        byte b = ipaddress2.GetAddressBytes().ElementAt(j);
                        array2[j] = (byte)(b | array[j]);
                    }
                    IPAddress ip = new IPAddress(array2);
                    list.Add(ip);
                    UnityEngine.Debug.Log($"APIInfoBroadcast.getBroadcastIPs :{ip}");
                }
            }
            __result = list;
            return false;
        }


        public static List<IPAddress> GetByNetworkInterface()
        {
            try
            {
                NetworkInterface[] intf = NetworkInterface.GetAllNetworkInterfaces();
                List<IPAddress> ls = new List<IPAddress>();
                foreach (var item in intf)
                {
                    IPInterfaceProperties adapterPropertis = item.GetIPProperties();
                    UnicastIPAddressInformationCollection coll = adapterPropertis.UnicastAddresses;
                    foreach (var col in coll)
                    {
                        if (!ls.Contains(col.Address))
                        {
                            ls.Add(col.Address);
                        }
                    }
                }
                LocalIP = ls;
                return ls;
            }
            catch (Exception)
            {
                UnityEngine.Debug.LogError($"APIInfoBroadcast.getBroadcastIPs.GetByNetworkInterface无法获取到本机IP，返回空");
                LocalIP = new List<IPAddress>();
                return new List<IPAddress>();
            }
        }

        private static IPAddress getHostMask(IPAddress ip)
        {
            NetworkInterface[] allNetworkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
            for (int i = 0; i < allNetworkInterfaces.Length; i++)
            {
                foreach (UnicastIPAddressInformation unicastIPAddressInformation in allNetworkInterfaces[i].GetIPProperties().UnicastAddresses)
                {
                    if (unicastIPAddressInformation.Address.ToString() == ip.ToString())
                    {
                        return unicastIPAddressInformation.IPv4Mask;
                    }
                }
            }
            return null;
        }
    }
}
