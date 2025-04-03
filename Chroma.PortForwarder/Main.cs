using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace MysticClue.Chroma.PortForwarder;

public partial class Main : Form
{
    UdpPortForwarder? _forwarder;

    public Main()
    {
        InitializeComponent();
        Console.SetOut(new ControlWriter(consoleTextBox));
        GetLocalAddresses();
    }

    private void GetLocalAddresses()
    {
        Dictionary<IPAddress, IPAddress> subnetMaskByNetworkAddress = new();
        foreach (NetworkInterface netInterface in NetworkInterface.GetAllNetworkInterfaces())
        {
            foreach (var addr in netInterface.GetIPProperties().UnicastAddresses)
            {
                if (addr.Address.AddressFamily == AddressFamily.InterNetwork)
                {
                    var networkAddress = addr.Address.GetNetworkAddress(addr.IPv4Mask);
                    subnetMaskByNetworkAddress[networkAddress] = addr.IPv4Mask;
                    localAddressListBox.Items.Add(new IPAddressWithNote(addr.Address, netInterface.Name));
                }
            }
        }

        foreach (var ip in IpNetTable.Get())
        {
            if (ip.AddressFamily != AddressFamily.InterNetwork) { continue; }

            bool isInSameSubnetAsLocal = false;
            foreach (var (networkAddress, subnetMask) in subnetMaskByNetworkAddress)
            {
                if (ip.IsInSameSubnet(networkAddress, subnetMask))
                {
                    isInSameSubnetAsLocal = true;
                    break;
                }
            }
            if (!isInSameSubnetAsLocal) { continue; }
            remoteAddressListBox.Items.Add(ip);
        }
    }

    private record struct IPAddressWithNote(IPAddress Address, string Note)
    {
        public override string ToString() => $"{Address.ToString()} ({Note})";
    }

    private void startButton_Click(object sender, EventArgs e)
    {
        if (localAddressListBox.SelectedItem == null)
        {
            Console.WriteLine("Select a local address.");
            return;
        }
        if (remoteAddressListBox.SelectedItem == null)
        {
            Console.WriteLine("Select a remote address.");
            return;
        }
        if (!int.TryParse(portTextBox.Text, out var port))
        {
            Console.WriteLine("Invalid port.");
            return;
        }

        localAddressListBox.Enabled = false;
        remoteAddressListBox.Enabled = false;
        portTextBox.Enabled = false;
        startButton.Enabled = false;

        var localAddress = ((IPAddressWithNote)localAddressListBox.SelectedItem).Address;
        var remoteAddress = ((IPAddress)remoteAddressListBox.SelectedItem);
        _forwarder = new UdpPortForwarder(localAddress, port, remoteAddress);
    }

    private async void stopButton_Click(object sender, EventArgs e)
    {
        if (_forwarder != null)
        {
            await _forwarder.DisposeAsync();
            _forwarder = null;
        }

        localAddressListBox.Enabled = true;
        remoteAddressListBox.Enabled = true;
        portTextBox.Enabled = true;
        startButton.Enabled = true;
    }

    private sealed class ControlWriter : TextWriter
    {
        private TextBoxBase textbox;
        public ControlWriter(TextBoxBase textbox) => this.textbox = textbox;
        public override void Write(char value) => textbox.AppendText(value.ToString());
        public override void Write(string? value) => textbox.AppendText(value);
        public override Encoding Encoding => Encoding.ASCII;
    }
}
