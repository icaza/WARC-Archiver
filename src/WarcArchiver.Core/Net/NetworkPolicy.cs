using System.Net;
using System.Net.Sockets;

namespace WarcArchiver.Core.Net;

/// <summary>
/// Decides whether an IP address may be contacted. By default only globally routable addresses are allowed, so a
/// page (or a redirect) can never make the application probe the user's own machine or local network.
/// </summary>
public static class NetworkPolicy
{
    public static bool IsPubliclyRoutable(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            return IsPublicV4(address.GetAddressBytes());
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            return IsPublicV6(address);
        }

        return false;
    }

    static bool IsPublicV4(byte[] b)
    {
        byte a = b[0];
        byte c = b[1];
        return !(a == 0                                   // "this" network
            || a == 10                                    // private
            || a == 127                                   // loopback
            || (a == 100 && c >= 64 && c <= 127)          // carrier-grade NAT
            || (a == 169 && c == 254)                     // link-local, cloud metadata endpoints
            || (a == 172 && c >= 16 && c <= 31)           // private
            || (a == 192 && c == 168)                     // private
            || (a == 192 && c == 0 && b[2] == 0)          // IETF protocol assignments
            || (a == 192 && c == 0 && b[2] == 2)          // documentation
            || (a == 198 && (c == 18 || c == 19))         // benchmarking
            || (a == 198 && c == 51 && b[2] == 100)       // documentation
            || (a == 203 && c == 0 && b[2] == 113)        // documentation
            || a >= 224);                                 // multicast, reserved, broadcast
    }

    static bool IsPublicV6(IPAddress address)
    {
        byte[] b = address.GetAddressBytes();

        if (address.Equals(IPAddress.IPv6Loopback) || address.Equals(IPAddress.IPv6None) || address.Equals(IPAddress.IPv6Any))
        {
            return false;
        }
        if (address.IsIPv6LinkLocal || address.IsIPv6SiteLocal || address.IsIPv6Multicast)
        {
            return false;
        }
        if ((b[0] & 0xFE) == 0xFC)                         // fc00::/7 unique local
        {
            return false;
        }
        if (b[0] == 0x20 && b[1] == 0x01 && b[2] == 0x0D && b[3] == 0xB8)  // 2001:db8::/32 documentation
        {
            return false;
        }
        if (b[0] == 0x20 && b[1] == 0x02)                  // 6to4: embedded IPv4 in bytes 2..5
        {
            return IsPublicV4([b[2], b[3], b[4], b[5]]);
        }
        if (b[0] == 0x00 && b[1] == 0x64 && b[2] == 0xFF && b[3] == 0x9B)  // 64:ff9b::/96 NAT64
        {
            return IsPublicV4([b[12], b[13], b[14], b[15]]);
        }
        return true;
    }
}
