using System;
using System.Collections.Generic;
using System.Linq;

namespace SuperUI.Components.Network.Models;

public enum NetworkLayerType
{
    Ethernet,
    IPv4,
    TCP,
    UDP,
    Payload
}

public class SgNetworkPacket
{
    public EthernetHeader Ethernet { get; set; } = new();
    public IPv4Header IP { get; set; } = new();
    public L4Header L4 { get; set; } = new TcpHeader();
    public string Payload { get; set; } = "";

    public byte[] ToBytes()
    {
        var bytes = new List<byte>();
        bytes.AddRange(Ethernet.ToBytes());
        bytes.AddRange(IP.ToBytes());
        bytes.AddRange(L4.ToBytes());
        bytes.AddRange(System.Text.Encoding.UTF8.GetBytes(Payload));
        return bytes.ToArray();
    }
}

public class EthernetHeader
{
    public string DestMac { get; set; } = "FF:FF:FF:FF:FF:FF";
    public string SrcMac { get; set; } = "00:00:00:00:00:00";
    public ushort EtherType { get; set; } = 0x0800; // IPv4

    public byte[] ToBytes()
    {
        var bytes = new byte[14];
        ParseMac(DestMac).CopyTo(bytes, 0);
        ParseMac(SrcMac).CopyTo(bytes, 6);
        BitConverter.GetBytes(EtherType).Reverse().ToArray().CopyTo(bytes, 12);
        return bytes;
    }

    private byte[] ParseMac(string mac) => 
        mac.Split(':').Select(x => Convert.ToByte(x, 16)).ToArray();
}

public class IPv4Header
{
    public byte Version { get; set; } = 4;
    public byte IHL { get; set; } = 5;
    public byte DSCP { get; set; } = 0;
    public byte ECN { get; set; } = 0;
    public ushort TotalLength { get; set; } = 20;
    public ushort Identification { get; set; } = 0x1234;
    public ushort FlagsAndOffset { get; set; } = 0;
    public byte TTL { get; set; } = 64;
    public byte Protocol { get; set; } = 6; // TCP
    public ushort Checksum { get; set; } = 0;
    public string SrcIp { get; set; } = "192.168.1.10";
    public string DestIp { get; set; } = "8.8.8.8";

    public byte[] ToBytes()
    {
        var bytes = new byte[20];
        bytes[0] = (byte)((Version << 4) | IHL);
        bytes[1] = (byte)((DSCP << 2) | ECN);
        BitConverter.GetBytes(TotalLength).Reverse().ToArray().CopyTo(bytes, 2);
        BitConverter.GetBytes(Identification).Reverse().ToArray().CopyTo(bytes, 4);
        BitConverter.GetBytes(FlagsAndOffset).Reverse().ToArray().CopyTo(bytes, 6);
        bytes[8] = TTL;
        bytes[9] = Protocol;
        BitConverter.GetBytes(Checksum).Reverse().ToArray().CopyTo(bytes, 10);
        System.Net.IPAddress.Parse(SrcIp).GetAddressBytes().CopyTo(bytes, 12);
        System.Net.IPAddress.Parse(DestIp).GetAddressBytes().CopyTo(bytes, 16);
        return bytes;
    }
}

public abstract class L4Header
{
    public abstract byte[] ToBytes();
}

public class TcpHeader : L4Header
{
    public ushort SrcPort { get; set; } = 44332;
    public ushort DestPort { get; set; } = 80;
    public uint SeqNumber { get; set; } = 0;
    public uint AckNumber { get; set; } = 0;
    public byte DataOffset { get; set; } = 5;
    public bool NS { get; set; }
    public bool CWR { get; set; }
    public bool ECE { get; set; }
    public bool URG { get; set; }
    public bool ACK { get; set; }
    public bool PSH { get; set; }
    public bool RST { get; set; }
    public bool SYN { get; set; } = true;
    public bool FIN { get; set; }
    public ushort WindowSize { get; set; } = 64240;
    public ushort Checksum { get; set; } = 0;
    public ushort UrgentPointer { get; set; } = 0;

    public override byte[] ToBytes()
    {
        var bytes = new byte[20];
        BitConverter.GetBytes(SrcPort).Reverse().ToArray().CopyTo(bytes, 0);
        BitConverter.GetBytes(DestPort).Reverse().ToArray().CopyTo(bytes, 2);
        BitConverter.GetBytes(SeqNumber).Reverse().ToArray().CopyTo(bytes, 4);
        BitConverter.GetBytes(AckNumber).Reverse().ToArray().CopyTo(bytes, 8);
        
        ushort flags = (ushort)((DataOffset << 12) | 
                       (NS ? 1 << 8 : 0) | 
                       (CWR ? 1 << 7 : 0) | 
                       (ECE ? 1 << 6 : 0) | 
                       (URG ? 1 << 5 : 0) | 
                       (ACK ? 1 << 4 : 0) | 
                       (PSH ? 1 << 3 : 0) | 
                       (RST ? 1 << 2 : 0) | 
                       (SYN ? 1 << 1 : 0) | 
                       (FIN ? 1 : 0));
        
        BitConverter.GetBytes(flags).Reverse().ToArray().CopyTo(bytes, 12);
        BitConverter.GetBytes(WindowSize).Reverse().ToArray().CopyTo(bytes, 14);
        BitConverter.GetBytes(Checksum).Reverse().ToArray().CopyTo(bytes, 16);
        BitConverter.GetBytes(UrgentPointer).Reverse().ToArray().CopyTo(bytes, 18);
        return bytes;
    }
}

public class UdpHeader : L4Header
{
    public ushort SrcPort { get; set; } = 44332;
    public ushort DestPort { get; set; } = 53;
    public ushort Length { get; set; } = 8;
    public ushort Checksum { get; set; } = 0;

    public override byte[] ToBytes()
    {
        var bytes = new byte[8];
        BitConverter.GetBytes(SrcPort).Reverse().ToArray().CopyTo(bytes, 0);
        BitConverter.GetBytes(DestPort).Reverse().ToArray().CopyTo(bytes, 2);
        BitConverter.GetBytes(Length).Reverse().ToArray().CopyTo(bytes, 4);
        BitConverter.GetBytes(Checksum).Reverse().ToArray().CopyTo(bytes, 6);
        return bytes;
    }
}
