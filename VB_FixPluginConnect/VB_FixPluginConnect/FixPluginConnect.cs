using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BepInEx;
using HarmonyLib;

namespace VB_FixPluginConnect
{
    [BepInPlugin("me.xiaoye97.plugin.VBridger.FixPluginConnect", "FixPluginConnect", "1.0.0")]
    public class FixPluginConnect : BaseUnityPlugin
    {
        void Awake()
        {
            Logger.LogInfo("FixPluginConnect Awake");
            Harmony.CreateAndPatchAll(typeof(FixPluginConnect));
        }

        [HarmonyPrefix, HarmonyPatch(typeof(TcpClient), "Connect", new Type[] { typeof(string), typeof(int)})]
        public static bool TcpClient_Connect_Patch(TcpClient __instance, string hostname, int port)
        {
            Console.WriteLine($"日志: TcpClient_Connect_Patch hostname:{hostname} port:{port}");
            if (hostname == "localhost")
            {
                bool m_CleanedUp = Traverse.Create(__instance).Field("m_CleanedUp").GetValue<bool>();
                if (m_CleanedUp)
                {
                    throw new ObjectDisposedException(__instance.GetType().FullName);
                }
                if (hostname == null)
                {
                    throw new ArgumentNullException("hostname");
                }
                bool m_Active = Traverse.Create(__instance).Field("m_Active").GetValue<bool>();
                if (m_Active)
                {
                    throw new SocketException((int)SocketError.IsConnected);
                }
                // Delete Dns.GetHostAddresses(hostname);
                var hostAddresses = GetByNetworkInterface();
                Exception ex = null;
                Socket socket = null;
                Socket socket2 = null;
                try
                {
                    Socket m_ClientSocket = Traverse.Create(__instance).Field("m_ClientSocket").GetValue<Socket>();
                    if (m_ClientSocket == null)
                    {
                        if (Socket.OSSupportsIPv4)
                        {
                            socket2 = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        }
                        if (Socket.OSSupportsIPv6)
                        {
                            socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
                        }
                    }
                    foreach (IPAddress ipaddress in hostAddresses)
                    {
                        try
                        {
                            Socket m_ClientSocket2 = Traverse.Create(__instance).Field("m_ClientSocket").GetValue<Socket>();
                            if (m_ClientSocket2 == null)
                            {
                                if (ipaddress.AddressFamily == AddressFamily.InterNetwork && socket2 != null)
                                {
                                    socket2.Connect(ipaddress, port);
                                    Traverse.Create(__instance).Field("m_ClientSocket").SetValue(socket2);
                                    if (socket != null)
                                    {
                                        socket.Close();
                                    }
                                }
                                else if (socket != null)
                                {
                                    socket.Connect(ipaddress, port);
                                    Traverse.Create(__instance).Field("m_ClientSocket").SetValue(socket);
                                    if (socket2 != null)
                                    {
                                        socket2.Close();
                                    }
                                }
                                Traverse.Create(__instance).Field("m_Family").SetValue(ipaddress.AddressFamily);
                                Traverse.Create(__instance).Field("m_Active").SetValue(true);
                                break;
                            }
                            AddressFamily m_Family = Traverse.Create(__instance).Field("m_Family").GetValue<AddressFamily>();
                            if (ipaddress.AddressFamily == m_Family)
                            {
                                __instance.Connect(new IPEndPoint(ipaddress, port));
                                Traverse.Create(__instance).Field("m_Active").SetValue(true);
                                break;
                            }
                        }
                        catch (Exception ex2)
                        {
                            if (ex2 is ThreadAbortException || ex2 is StackOverflowException || ex2 is OutOfMemoryException)
                            {
                                throw;
                            }
                            ex = ex2;
                        }
                    }
                }
                catch (Exception ex3)
                {
                    if (ex3 is ThreadAbortException || ex3 is StackOverflowException || ex3 is OutOfMemoryException)
                    {
                        throw;
                    }
                    ex = ex3;
                }
                finally
                {
                    bool m_Active2 = Traverse.Create(__instance).Field("m_Active").GetValue<bool>();
                    if (!m_Active2)
                    {
                        if (socket != null)
                        {
                            socket.Close();
                        }
                        if (socket2 != null)
                        {
                            socket2.Close();
                        }
                        if (ex != null)
                        {
                            throw ex;
                        }
                        throw new SocketException((int)SocketError.NotConnected);
                    }
                }
                return false;
            }
            return true;
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
                            if (col.Address.ToString().StartsWith("192"))
                            {
                                ls.Add(col.Address);
                                Console.WriteLine($"GetByNetworkInterface :{col.Address} 加入列表");
                            }
                            else
                            {
                                Console.WriteLine($"GetByNetworkInterface :{col.Address} 抛弃");
                            }
                        }
                    }
                }
                return ls;
            }
            catch (Exception)
            {
                Console.WriteLine($"APIInfoBroadcast.getBroadcastIPs.GetByNetworkInterface无法获取到本机IP，返回空");
                return new List<IPAddress>();
            }
        }
    }
}
