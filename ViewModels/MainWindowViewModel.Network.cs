using Open.Nat;
using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DMINLauncher.ViewModels;

public partial class MainWindowViewModel
{
    #region IP Detection

    public async Task GetLocalIpAddress()
    {
        try
        {
            await Task.Run(() =>
            {
                string? localIp = null;

                try
                {
                    using var socket = new System.Net.Sockets.Socket(
                        System.Net.Sockets.AddressFamily.InterNetwork,
                        System.Net.Sockets.SocketType.Dgram,
                        System.Net.Sockets.ProtocolType.Udp);

                    socket.Connect("8.8.8.8", 80);
                    localIp = (socket.LocalEndPoint as System.Net.IPEndPoint)?.Address.ToString();
                }
                catch
                {
                    var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
                    foreach (var ip in host.AddressList)
                    {
                        if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        {
                            localIp = ip.ToString();
                            break;
                        }
                    }
                }

                if (!string.IsNullOrEmpty(localIp))
                {
                    Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        LocalIpAddress = localIp;
                        IpAddress      = localIp;
                    });
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GetLocalIpAddress: {ex.Message}");
        }
    }

    public async Task GetPublicIpAddress()
    {
        try
        {
            using var client = new HttpClient();
            var ip = await client.GetStringAsync("https://api.ipify.org");
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                PublicIpAddress = ip;
                IpAddress       = ip;
            });
        }
        catch { }
    }

    private async void InitializeNetworkInfo()
    {
        switch (NetworkMode)
        {
            case Enums.NetworkMode.HostLAN:
                await GetLocalIpAddress();
                break;
            case Enums.NetworkMode.HostInternet:
                await GetLocalIpAddress();
                await GetPublicIpAddress();
                break;
        }
    }

    #endregion

    #region Port Forwarding

    private const int DoomPort = 5029;

    private async Task TestPortForwarding()
    {
        try
        {
            IsTestingPort  = true;
            PortTestResult = "🔧 Checking UPnP support and UDP port 5029...";

            var sb          = new StringBuilder();
            NatDevice? nat  = null;
            bool upnp       = false;
            Mapping? existing = null;

            try
            {
                PortTestResult = "🔍 Discovering UPnP-enabled router...";
                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                nat     = await new NatDiscoverer().DiscoverDeviceAsync(PortMapper.Upnp, cts);

                if (nat != null)
                {
                    upnp           = true;
                    PortTestResult = "✅ UPnP router found! Checking port mappings...";

                    var externalIp = await nat.GetExternalIPAsync();
                    if (externalIp != null) PublicIpAddress = externalIp.ToString();

                    try { existing = await nat.GetSpecificMappingAsync(Protocol.Udp, DoomPort); } catch { }
                }
            }
            catch (NatDeviceNotFoundException) { upnp = false; }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"UPnP: {ex.Message}"); }

            if (string.IsNullOrEmpty(PublicIpAddress))
                await GetPublicIpAddress();

            sb.AppendLine($"🌐 Public IP: {(string.IsNullOrEmpty(PublicIpAddress) ? "Unknown" : PublicIpAddress)}");
            sb.AppendLine($"🏠 Local IP: {LocalIpAddress}");
            sb.AppendLine();

            if (upnp && nat != null)
            {
                sb.AppendLine("📡 UPnP: ✅ Supported by your router!");
                sb.AppendLine();

                if (existing != null)
                {
                    sb.AppendLine($"✅ UDP port {DoomPort} is already forwarded!");
                    sb.AppendLine($"   → {existing.PrivateIP}:{existing.PrivatePort}");
                    sb.AppendLine($"   Description: {existing.Description}");
                }
                else
                {
                    sb.AppendLine($"ℹ️ UDP port {DoomPort} is NOT currently forwarded.");
                    sb.AppendLine();
                    sb.AppendLine("Click 'Auto-Configure Port' to set up UPnP forwarding.");
                }
            }
            else
            {
                sb.AppendLine("📡 UPnP: ❌ Not available or disabled");
                sb.AppendLine();
                sb.AppendLine("Manual port forwarding steps:");
                sb.AppendLine("1. Log into your router (usually http://192.168.1.1)");
                sb.AppendLine("2. Find 'Port Forwarding' or 'NAT' settings");
                sb.AppendLine($"3. Forward UDP port {DoomPort} → {LocalIpAddress}:{DoomPort}");
                sb.AppendLine("4. Save and test again");
            }

            PortTestResult = sb.ToString();
        }
        catch (Exception ex)
        {
            PortTestResult = $"❌ Test error: {ex.Message}";
        }
        finally
        {
            IsTestingPort = false;
        }
    }

    private async Task AutoConfigurePort()
    {
        try
        {
            IsTestingPort  = true;
            PortTestResult = "🔧 Configuring UPnP port forwarding...";

            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var nat = await new NatDiscoverer().DiscoverDeviceAsync(PortMapper.Upnp, cts);

            if (nat == null)
            {
                PortTestResult = "❌ Could not find UPnP-enabled router.\n\nMake sure UPnP is enabled in your router settings.";
                return;
            }

            PortTestResult = "📡 Creating UDP port mapping...";

            await nat.CreatePortMapAsync(new Mapping(Protocol.Udp, DoomPort, DoomPort, "Doom Multiplayer (DMINLauncher)"));

            var verified   = await nat.GetSpecificMappingAsync(Protocol.Udp, DoomPort);
            var externalIp = await nat.GetExternalIPAsync();

            PortTestResult = verified != null
                ? $"✅ SUCCESS! UDP port {DoomPort} is now forwarded!\n\n" +
                  $"🌐 External: {externalIp}:{DoomPort}\n" +
                  $"🏠 Internal: {verified.PrivateIP}:{verified.PrivatePort}\n\n" +
                  "Note: This mapping may expire when your router restarts."
                : "⚠️ Port mapping created but could not be verified.";
        }
        catch (MappingException ex)
        {
            PortTestResult = $"❌ Could not create port mapping.\n\nError: {ex.Message}";
        }
        catch (Exception ex)
        {
            PortTestResult = $"❌ Auto-configure failed: {ex.Message}";
        }
        finally
        {
            IsTestingPort = false;
        }
    }

    #endregion
}
