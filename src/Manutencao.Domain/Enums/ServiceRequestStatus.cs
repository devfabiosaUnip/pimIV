namespace Manutencao.Domain.Enums;

public enum ServiceRequestStatus
{
    PendenteAprovacao = 0,
    Aprovada = 1,
    AguardandoAtribuicao = 2,
    Atribuida = 3,
    EmDeslocamento = 4,
    EmAtendimento = 5,
    Concluida = 6,
    Cancelada = 7,
    Rejeitada = 8
}
