using System;
using System.Collections.Generic;
using System.Linq;
using SuperUI.Components.Network.Models;

namespace SuperUI.Services.Network;

public class SgFirewallRule
{
    public string Name { get; set; } = "";
    public string? SrcIp { get; set; }
    public string? DestIp { get; set; }
    public ushort? DestPort { get; set; }
    public string? Protocol { get; set; } // TCP, UDP
    public bool Allow { get; set; }
}

public class SgSimulationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public List<string> HopLog { get; set; } = new();
    public SgFirewallRule? MatchedRule { get; set; }
}

public class SgFirewallService
{
    private List<SgFirewallRule> _rules = new()
    {
        new SgFirewallRule { Name = "Allow HTTP/HTTPS", DestPort = 80, Protocol = "TCP", Allow = true },
        new SgFirewallRule { Name = "Allow HTTP/HTTPS", DestPort = 443, Protocol = "TCP", Allow = true },
        new SgFirewallRule { Name = "Allow DNS", DestPort = 53, Protocol = "UDP", Allow = true },
        new SgFirewallRule { Name = "Block Malicious IP", DestIp = "6.6.6.6", Allow = false },
        new SgFirewallRule { Name = "Default Deny", Allow = false }
    };

    public SgSimulationResult SimulatePacket(SgNetworkPacket packet)
    {
        var result = new SgSimulationResult();
        result.HopLog.Add($"Packet received from {packet.IP.SrcIp} (MAC: {packet.Ethernet.SrcMac})");
        
        // Basic Routing Simulation
        if (packet.IP.TTL <= 0)
        {
            result.Success = false;
            result.Message = "Time Exceeded (TTL=0)";
            result.HopLog.Add("Dropped: TTL expired");
            return result;
        }

        result.HopLog.Add($"Routing to {packet.IP.DestIp}...");
        
        // Firewall Simulation
        var protocol = packet.L4 is TcpHeader ? "TCP" : "UDP";
        ushort destPort = 0;
        if (packet.L4 is TcpHeader tcp) destPort = tcp.DestPort;
        if (packet.L4 is UdpHeader udp) destPort = udp.DestPort;

        var matchedRule = _rules.FirstOrDefault(r => 
            (r.SrcIp == null || r.SrcIp == packet.IP.SrcIp) &&
            (r.DestIp == null || r.DestIp == packet.IP.DestIp) &&
            (r.DestPort == null || r.DestPort == destPort) &&
            (r.Protocol == null || r.Protocol == protocol)
        );

        result.MatchedRule = matchedRule;

        if (matchedRule != null && matchedRule.Allow)
        {
            result.Success = true;
            result.Message = $"Accepted by rule: {matchedRule.Name}";
            result.HopLog.Add($"Firewall: ALLOW ({matchedRule.Name})");
            result.HopLog.Add($"Packet delivered to {packet.IP.DestIp}");
        }
        else
        {
            result.Success = false;
            result.Message = matchedRule != null ? $"Dropped by rule: {matchedRule.Name}" : "Dropped by Default Deny";
            result.HopLog.Add($"Firewall: DROP ({(matchedRule?.Name ?? "Default Deny")})");
        }

        return result;
    }
}
