# Sistema de manutenção (empresa)

Arquitetura em **3 frentes**:

| Canal | Stack | Função |
|-------|--------|--------|
| App **Cliente** | Kotlin / Android | Abrir chamado, **rastrear o técnico** no mapa, ver prova do reparo e **enviar feedback** |
| **Web central** | **ASP.NET Core Razor Pages** (.NET 10) | Aprovar, despachar, rastrear visitas, ver foto do reparo e avaliação |
| App **Técnico** | Kotlin / Android | Intranet VPN, **abrir GPS**, enviar posição, **fotografar o reparo** ao concluir |

Backend REST: **ASP.NET Core Web API** (`src/Manutencao.Api`).

## Fluxo

1. Cliente envia requisição (`POST /api/client/requests`) com GPS do aparelho.
2. Operador aprova no painel (`/`) e destina o técnico com ranking de sugestões.
3. Técnico toca **Abrir GPS** — abre a navegação e um serviço em primeiro plano envia a posição (`/track`) mesmo com o Maps aberto.
4. Cliente acompanha o técnico em tempo real (`GET /api/client/requests/{id}/tracking`).
5. Ao terminar, o técnico **fotografa o reparo** (anexo obrigatório) e conclui.
6. O cliente vê a foto e avalia o atendimento (`POST /api/client/requests/{id}/feedback`).

## Executar localmente (.NET)

```powershell
cd C:\Users\devfa\Projects\manutencao-sistema
dotnet restore
dotnet run --project src/Manutencao.Api/Manutencao.Api.csproj --urls http://localhost:5080
dotnet run --project src/Manutencao.Web/Manutencao.Web.csproj --urls http://localhost:5081
```

Swagger: http://localhost:5080/swagger  
Painel ASP.NET: http://localhost:5081 (login operador)

### Usuários demo (seed)

| Perfil | E-mail | Senha |
|--------|--------|-------|
| Operador | operador@empresa.local | Operador@123 |
| Cliente | cliente@demo.local | Cliente@123 |
| Técnico | tecnico@empresa.local | Tecnico@123 |

## VPN intranet

- Stack: `vpn/docker-compose.yml` (WireGuard + API).
- Middleware `VpnIntranetMiddleware` bloqueia `/api/intranet/*` se o IP não estiver na faixa (`Vpn:AllowedCidrs`).
- Dev: `Vpn:RequireVpn=false` em `src/Manutencao.Api/appsettings.json`.

## Apps Android

- `mobile/cliente` — chamados, rastreio OSM e feedback (emulador: ajuste `API_BASE_URL` em `build.gradle.kts`).
- `mobile/tecnico` — visitas, abrir GPS, rastreio e foto de conclusão.
"# pimIV" 
