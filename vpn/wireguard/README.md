# VPN intranet (WireGuard)

1. Suba o stack: `docker compose -f vpn/docker-compose.yml up -d`
2. Copie o QR/conf do peer `tecnico1` em `vpn/wireguard/config/peer_tecnico1/`
3. No app **Técnico**, importe o `.conf` (WireGuard Android) ou use o botão **Conectar VPN**
4. Com `Vpn:RequireVpn=true`, rotas `/api/intranet/*` só respondem com IP da faixa `10.8.0.0/24`

Para desenvolvimento local, mantenha `Vpn:RequireVpn=false` em `appsettings.json`.
