using System.Security.Claims;
using Manutencao.Application.Dto;
using Manutencao.Application.Services;
using Manutencao.Domain.Enums;
using Manutencao.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Manutencao.Web.Pages;

public class IndexModel(AppDbContext db, DispatchRecommendationService dispatch, ServiceRequestWorkflow workflow) : PageModel
{
    public List<ServiceRequestSummaryDto> Requests { get; private set; } = [];
    public ServiceRequestSummaryDto? Selected { get; private set; }
    public IReadOnlyList<DispatchRecommendationDto> Recommendations { get; private set; } = [];

    public async Task OnGetAsync(Guid? id)
    {
        Requests = await db.ServiceRequests.AsNoTracking()
            .Where(r => r.Status == ServiceRequestStatus.PendenteAprovacao
                        || r.Status == ServiceRequestStatus.AguardandoAtribuicao)
            .OrderByDescending(r => r.Priority)
            .ThenBy(r => r.CreatedAt)
            .Select(r => new ServiceRequestSummaryDto(
                r.Id, r.Protocol, r.Title, r.Description, r.Status, r.Priority,
                r.AddressLine, r.Latitude, r.Longitude, r.CreatedAt))
            .ToListAsync();

        Selected = id is Guid selectedId ? Requests.FirstOrDefault(r => r.Id == selectedId) : Requests.FirstOrDefault();
        if (Selected is { Status: ServiceRequestStatus.AguardandoAtribuicao })
            Recommendations = await dispatch.GetRecommendationsAsync(Selected.Id);
    }

    public async Task<IActionResult> OnPostApproveAsync(Guid id)
    {
        await workflow.ApproveAsync(id, OperatorId());
        return RedirectToPage("/Index", new { id });
    }

    public async Task<IActionResult> OnPostAssignAsync(Guid id, Guid technicianId)
    {
        await workflow.AssignAsync(id, technicianId, OperatorId(), "Destino via painel ASP.NET");
        return RedirectToPage("/Visitas");
    }

    private Guid OperatorId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
