using System;
using System.Linq;
using System.Net.NetworkInformation;

public static class DockerUtility
{
    public static int GetAvailablePort()
    {
        var properties = IPGlobalProperties.GetIPGlobalProperties();
        var usedPorts = properties.GetActiveTcpConnections()
            .Select(t => t.LocalEndPoint.Port)
            .Concat(properties.GetActiveTcpListeners().Select(l => l.Port))
            .Concat(properties.GetActiveUdpListeners().Select(u => u.Port))
            .ToHashSet();

        for (int port = 8000; port < 9000; port++)
        {
            if (!usedPorts.Contains(port))
            {
                return port;
            }
        }

        throw new Exception("No available ports found.");
    }
}

