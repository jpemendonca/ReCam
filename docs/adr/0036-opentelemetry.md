# ADR 0036: Observabilidade com OpenTelemetry

- Data: 2026-09-25
- Estado: aceita
- Origem: revisão no `SPECS.md` 12

## Contexto

O projeto é vitrine .NET e precisava mostrar traces e métricas, sem mandar nada para terceiros.

## Decisão

Traces e métricas de ASP.NET Core e HttpClient, e três medidores no meter `Recam.Server` lidos do
`DevicePresence`: `recam.cameras.online`, `recam.cameras.publishing` e `recam.views.active`. OTLP só
com `OTEL_EXPORTER_OTLP_ENDPOINT` definida. Aspire Dashboard opcional em
`deploy/compose.observability.yaml`, preso ao loopback.

## Consequências

- Por padrão nada sai do servidor.
- A query string sai redigida nos traces, então o `access_token` do SignalR não vaza.
- As portas do dashboard só existem com o compose opcional.
