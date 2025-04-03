using System.ComponentModel;
using System.Net;
using System.Runtime.InteropServices;

namespace MysticClue.Chroma.PortForwarder;

/// <summary>
/// Windows-specific API for getting list of known IPs.
/// 
/// From https://stackoverflow.com/questions/1148778/how-do-i-access-arp-protocol-information-through-net/1148861
/// </summary>
public static class IpNetTable
{
    // The max number of physical addresses.
    const int MAXLEN_PHYSADDR = 8;

    // Define the MIB_IPNETROW structure.
    [StructLayout(LayoutKind.Sequential)]
    struct MIB_IPNETROW
    {
        [MarshalAs(UnmanagedType.U4)]
        public int dwIndex;
        [MarshalAs(UnmanagedType.U4)]
        public int dwPhysAddrLen;
        [MarshalAs(UnmanagedType.U1)]
        public byte mac0;
        [MarshalAs(UnmanagedType.U1)]
        public byte mac1;
        [MarshalAs(UnmanagedType.U1)]
        public byte mac2;
        [MarshalAs(UnmanagedType.U1)]
        public byte mac3;
        [MarshalAs(UnmanagedType.U1)]
        public byte mac4;
        [MarshalAs(UnmanagedType.U1)]
        public byte mac5;
        [MarshalAs(UnmanagedType.U1)]
        public byte mac6;
        [MarshalAs(UnmanagedType.U1)]
        public byte mac7;
        [MarshalAs(UnmanagedType.U4)]
        public int dwAddr;
        [MarshalAs(UnmanagedType.U4)]
        public int dwType;
    }

    // Declare the GetIpNetTable function.
    [DllImport("IpHlpApi.dll")]
    [return: MarshalAs(UnmanagedType.U4)]
    static extern int GetIpNetTable(
        IntPtr pIpNetTable,
        [MarshalAs(UnmanagedType.U4)]
        ref int pdwSize,
        bool bOrder);

    [DllImport("IpHlpApi.dll", SetLastError = true, CharSet = CharSet.Auto)]
    internal static extern int FreeMibTable(IntPtr plpNetTable);

    const int ERROR_INSUFFICIENT_BUFFER = 122;

    public static List<IPAddress> Get()
    {
        int bytesNeeded = 0;

        int result = GetIpNetTable(IntPtr.Zero, ref bytesNeeded, false);

        // Call the function, expecting an insufficient buffer.
        if (result != ERROR_INSUFFICIENT_BUFFER)
        {
            throw new Win32Exception(result);
        }

        // Allocate the memory, do it in a try/finally block, to ensure
        // that it is released.
        IntPtr buffer = IntPtr.Zero;

        List<IPAddress> ret = new();

        try
        {
            buffer = Marshal.AllocCoTaskMem(bytesNeeded);

            // Make the call again. If it did not succeed, then
            // raise an error.
            result = GetIpNetTable(buffer, ref bytesNeeded, false);
            if (result != 0)
            {
                throw new Win32Exception(result);
            }

            // Now we have the buffer, we have to marshal it. We can read
            // the first 4 bytes to get the length of the buffer.
            int entries = Marshal.ReadInt32(buffer);

            // Increment the memory pointer by the size of the int.
            IntPtr currentBuffer = new IntPtr(buffer.ToInt64() +
               Marshal.SizeOf(typeof(int)));

            MIB_IPNETROW[] table = new MIB_IPNETROW[entries];

            // Cycle through the entries.
            for (int index = 0; index < entries; index++)
            {
                // Call PtrToStructure, getting the structure information.
                var ptr = new IntPtr(currentBuffer.ToInt64() + (index * Marshal.SizeOf(typeof(MIB_IPNETROW))));
                table[index] = Marshal.PtrToStructure<MIB_IPNETROW>(ptr);
            }


            for (int index = 0; index < entries; index++)
            {
                MIB_IPNETROW row = table[index];
                ret.Add(new IPAddress(BitConverter.GetBytes(row.dwAddr)));
            }
        }
        finally
        {
            // Release the memory.
            _ = FreeMibTable(buffer);
        }

        return ret;
    }
}
