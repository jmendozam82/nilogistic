using System.Net;
using System.Net.Sockets;

namespace Nilogistic.Utility.Seguridad;

public static class ClienteIp
{
    public static string Clave(IPAddress? ip)
    {
        if (ip is null)
        {
            return "desconocido";
        }

        if (ip.IsIPv4MappedToIPv6)
        {
            ip = ip.MapToIPv4();
        }

        if (ip.AddressFamily == AddressFamily.InterNetwork)
        {
            return ip.ToString();
        }

        if (ip.AddressFamily == AddressFamily.InterNetworkV6)
        {
            var bytes = ip.GetAddressBytes();
            if (bytes.Length == 16)
            {
                var grupo1 = (ushort)((bytes[0] << 8) | bytes[1]);
                var grupo2 = (ushort)((bytes[2] << 8) | bytes[3]);
                var grupo3 = (ushort)((bytes[4] << 8) | bytes[5]);
                var grupo4 = (ushort)((bytes[6] << 8) | bytes[7]);
                return $"v6:{grupo1:x4}:{grupo2:x4}:{grupo3:x4}:{grupo4:x4}";
            }
        }

        return ip.ToString();
    }
}
