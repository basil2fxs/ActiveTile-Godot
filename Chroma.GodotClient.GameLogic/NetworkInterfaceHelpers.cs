using System.Net.Sockets;
using MysticClue.Chroma.GodotClient.GameLogic.Debugging;

namespace MysticClue.Chroma.GodotClient.GameLogic;

public static class NetworkInterfaceHelpers
{
    public static void DisableUdpConnectionReset(UdpClient client)
    {
        if (Assert.ReportNull(client?.Client, "UdpClient or inner Socket")) { return; }

        // When sending out on a client, if the remote port is not listening, it is considered unreachable
        // and will throw an exception the next time it's used (e.g. to receive).
        // Disable these signals so that we send regardless of whether the other end is present.
        // See https://stackoverflow.com/questions/38191968/c-sharp-udp-an-existing-connection-was-forcibly-closed-by-the-remote-host
        const int SIO_UDP_CONNRESET = -1744830452;
        client.Client.IOControl(
            (IOControlCode)SIO_UDP_CONNRESET,
            [0, 0, 0, 0],
            null
        );
    }
}
