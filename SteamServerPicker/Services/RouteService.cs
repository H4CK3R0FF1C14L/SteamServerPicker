using NetFwTypeLib;
using SteamServerPicker.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;

namespace SteamServerPicker.Services
{
    public class RouteService
    {
        #region Variables

        private readonly string prefix = "ProjectBoost-";
        private readonly string networkConfigUrl = @"https://api.steampowered.com/ISteamApps/GetSDRConfig/v1?appid=730";

        private const string FWPOLICY2 = "HNetCfg.FwPolicy2";
        private const string FWRULE = "HNetCfg.FWRule";

        #endregion


        /// <summary>
        /// Clear all rules in Firewall
        /// </summary>
        public void ClearAllRules()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Type tNetFwPolicy2 = Type.GetTypeFromProgID(FWPOLICY2)!;
                INetFwPolicy2 fwPolicy2 = (INetFwPolicy2)Activator.CreateInstance(tNetFwPolicy2)!;
                foreach (INetFwRule rule in fwPolicy2.Rules)
                {
                    if (rule.Name.Contains(prefix))
                        fwPolicy2.Rules.Remove(rule.Name);
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return;
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD)) return;
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return;
            else return;
        }


        /// <summary>
        /// Get all rules from Firewall
        /// </summary>
        /// <returns>Blocked servers list (ip:port)</returns>
        public List<string> GetAllRules()
        {
            List<string> addresses = new List<string>();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Type tNetFwPolicy2 = Type.GetTypeFromProgID(FWPOLICY2)!;
                INetFwPolicy2 fwPolicy2 = (INetFwPolicy2)Activator.CreateInstance(tNetFwPolicy2)!;

                foreach (INetFwRule rule in fwPolicy2.Rules)
                {
                    if (rule.Name.Contains(prefix))
                    {
                        string address = $"{rule.RemoteAddresses[0]}:{rule.RemotePorts[0]}";
                        addresses.Add(address);
                    }
                }
                return addresses;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return addresses;
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD)) return addresses;
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return addresses;
            else return addresses;
        }


        /// <summary>
        /// Determines whether [is server banned] [the specified name].
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>
        ///   <c>true</c> if [is server banned] [the specified name]; otherwise, <c>false</c>.
        /// </returns>
        public bool IsServerBanned(string name)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Type tNetFwPolicy2 = Type.GetTypeFromProgID(FWPOLICY2)!;
                INetFwPolicy2 fwPolicy2 = (INetFwPolicy2)Activator.CreateInstance(tNetFwPolicy2)!;

                foreach (INetFwRule rule in fwPolicy2.Rules)
                {
                    if (rule.Name == $"{prefix}{name}")
                    {
                        return true;
                    }
                }
                return false;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return false;
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD)) return false;
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return false;
            else return false;
        }


        /// <summary>
        /// Ping the host.
        /// </summary>
        /// <param name="host">The host.</param>
        /// <returns>Ping value</returns>
        public long PingHost(string host)
        {
            try
            {
                using (Ping pingServer = new Ping())
                {
                    PingReply pingReply = pingServer.Send(host);
                    if (pingReply.Status == IPStatus.Success)
                        return pingReply.RoundtripTime;
                    else
                        return -1;
                }
            }
            catch (Exception exception)
            {
                Debug.WriteLine($"Can't ping {host}\nException Message: {exception.Message}");
                return -1;
            }
        }


        /// <summary>
        /// Pings the host asynchronous.
        /// </summary>
        /// <param name="host">The host.</param>
        /// <returns>Ping value</returns>
        public async Task<long> PingHostAsync(string host)
        {
            try
            {
                using (Ping ping = new Ping())
                {
                    PingReply pingreply = await ping.SendPingAsync(host);
                    if (pingreply.Status == IPStatus.Success)
                        return pingreply.RoundtripTime;
                    else
                        return -1;
                }
            }
            catch (Exception exception)
            {
                Debug.WriteLine($"Can't ping {host}\nException Message: {exception.Message}");
                return -1;
            }
        }


        /// <summary>
        /// Populates the routes.
        /// </summary>
        /// <returns>List Routes</returns>
        public List<Route> PopulateRoutes()
        {
            List<Route> routes = new List<Route>();

            string rawData;
            using (HttpClient client = new HttpClient())
            {
                rawData = client.GetStringAsync(networkConfigUrl).Result;
            }

            JsonDocument jsonDocument = JsonDocument.Parse(rawData);
            JsonElement popsElement = jsonDocument.RootElement.GetProperty("pops");

            foreach (JsonProperty property in popsElement.EnumerateObject())
            {
                JsonElement value = property.Value;

                if (value.ToString().Contains("relays") && !value.ToString().Contains("cloud-test"))
                {
                    Route route = new Route();
                    route.Name = property.Name;

                    if (value.TryGetProperty("desc", out JsonElement descriptionElement))
                    {
                        route.Description = descriptionElement.GetString();
                    }

                    route.Ranges = new Dictionary<string, string>();

                    foreach (JsonElement relay in value.GetProperty("relays").EnumerateArray())
                    {
                        route.Ranges.Add(relay.GetProperty("ipv4").GetString()!, relay.GetProperty("port_range").ToString());
                    }

                    if (value.ToString().Contains("partners\":2"))
                        route.Pw = true;
                    else
                        route.Pw = false;

                    routes.Add(route);
                }
            }

            return routes;
        }


        /// <summary>
        /// Sets the rule.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="remoteAddress">The remote address.</param>
        /// <param name="portRange">The port range.</param>
        public void SetRule(string name, string remoteAddress, string portRange)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                string ruleName = $"{prefix}{name}";

                Type tNetFwPolicy2 = Type.GetTypeFromProgID(FWPOLICY2)!;
                INetFwPolicy2 fwPolicy2 = (INetFwPolicy2)Activator.CreateInstance(tNetFwPolicy2)!;
                foreach (INetFwRule rule in fwPolicy2.Rules)
                {
                    if (rule.Name.Contains(ruleName))
                        fwPolicy2.Rules.Remove(rule.Name);
                }

                INetFwRule fwRule = (INetFwRule2)Activator.CreateInstance(Type.GetTypeFromProgID(FWRULE)!)!;

                fwRule.Enabled = true;
                fwRule.Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_OUT;
                fwRule.Action = NET_FW_ACTION_.NET_FW_ACTION_BLOCK;
                fwRule.RemoteAddresses = remoteAddress;
                fwRule.Protocol = 17;
                fwRule.RemotePorts = portRange.Replace(",", "-").Replace("[", "").Replace("]", "");
                fwRule.Name = ruleName;

                fwPolicy2.Rules.Add(fwRule);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return;
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD)) return;
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return;
            else return;
        }


        /// <summary>
        /// Clears the rule.
        /// </summary>
        /// <param name="name">The name.</param>
        public void ClearRule(string name)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                string ruleName = $"{prefix}{name}";

                Type tNetFwPolicy2 = Type.GetTypeFromProgID(FWPOLICY2)!;
                INetFwPolicy2 fwPolicy2 = (INetFwPolicy2)Activator.CreateInstance(tNetFwPolicy2)!;
                foreach (INetFwRule rule in fwPolicy2.Rules)
                {
                    if (rule.Name.Contains(ruleName))
                        fwPolicy2.Rules.Remove(rule.Name);
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return;
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD)) return;
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return;
            else return;
        }
    }
}
