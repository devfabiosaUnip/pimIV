using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Manutencao.Infrastructure.Vpn;

/// <summary>
/// Restringe rotas de intranet do técnico à rede VPN (WireGuard).
/// Em desenvolvimento, defina Vpn:RequireVpn=false.
/// </summary>
public class VpnIntranetMiddleware(RequestDelegate next, IConfiguration configuration)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var requireVpn = configuration.GetValue("Vpn:RequireVpn", true);
        if (!requireVpn || !context.Request.Path.StartsWithSegments("/api/intranet"))
        {
            await next(context);
            return;
        }

        var allowedCidrs = configuration.GetSection("Vpn:AllowedCidrs").Get<string[]>() ?? ["10.8.0.0/24"];
        var remoteIp = context.Connection.RemoteIpAddress;

        if (remoteIp is null || !IsIpAllowed(remoteIp, allowedCidrs))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Acesso intranet permitido somente via VPN corporativa.",
                hint = "Conecte-se ao WireGuard antes de sincronizar visitas."
            });
            return;
        }

        await next(context);
    }

    private static bool IsIpAllowed(IPAddress ip, IEnumerable<string> cidrs)
    {
        if (IPAddress.IsLoopback(ip)) return true;

        foreach (var cidr in cidrs)
        {
            var parts = cidr.Split('/');
            if (parts.Length != 2 || !IPAddress.TryParse(parts[0], out var network) || !int.TryParse(parts[1], out var prefix))
                continue;

            if (IsInSubnet(ip, network, prefix))
                return true;
        }

        return false;
    }

    private static bool IsInSubnet(IPAddress address, IPAddress network, int prefixLength)
    {
        var addressBytes = address.MapToIPv4().GetAddressBytes();
        var networkBytes = network.MapToIPv4().GetAddressBytes();
        var fullBytes = prefixLength / 8;
        var remainingBits = prefixLength % 8;

        for (var i = 0; i < fullBytes; i++)
        {
            if (addressBytes[i] != networkBytes[i]) return false;
        }

        if (remainingBits == 0) return true;

        var mask = (byte)(0xFF << (8 - remainingBits));
        return (addressBytes[fullBytes] & mask) == (networkBytes[fullBytes] & mask);
    }
}
