using Manutencao.Domain.Enums;

namespace Manutencao.Web;

public static class StatusDisplay
{
    public static string Label(ServiceRequestStatus status) => status switch
    {
        ServiceRequestStatus.PendenteAprovacao => "Aguardando aprovação",
        ServiceRequestStatus.Aprovada => "Aprovada",
        ServiceRequestStatus.AguardandoAtribuicao => "Na fila de despacho",
        ServiceRequestStatus.Atribuida => "Técnico designado",
        ServiceRequestStatus.EmDeslocamento => "Em deslocamento",
        ServiceRequestStatus.EmAtendimento => "Em atendimento",
        ServiceRequestStatus.Concluida => "Concluída",
        ServiceRequestStatus.Cancelada => "Cancelada",
        ServiceRequestStatus.Rejeitada => "Rejeitada",
        _ => status.ToString()
    };
}
