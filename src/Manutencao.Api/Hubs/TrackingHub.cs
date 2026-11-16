using System.Security.Claims;
using Manutencao.Domain.Enums;
using Manutencao.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Manutencao.Api.Hubs;

[Authorize]
public class TrackingHub(AppDbContext db) : Hub
{
    public async Task JoinVisit(string visitAssignmentId)
    {
        if (!Guid.TryParse(visitAssignmentId, out var id))
            throw new HubException("Visita inválida.");

        var role = Context.User?.FindFirstValue(ClaimTypes.Role);
        var userIdValue = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdValue, out var userId))
            throw new HubException("Não autenticado.");

        if (role == nameof(UserRole.Cliente))
        {
            var owns = await db.VisitAssignments
                .AnyAsync(v => v.Id == id && v.ServiceRequest.ClientId == userId);
            if (!owns)
                throw new HubException("Não autorizado.");
        }
        else if (role == nameof(UserRole.Tecnico))
        {
            var owns = await db.VisitAssignments
                .AnyAsync(v => v.Id == id && v.Technician.UserId == userId);
            if (!owns)
                throw new HubException("Não autorizado.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, visitAssignmentId);
    }
}
