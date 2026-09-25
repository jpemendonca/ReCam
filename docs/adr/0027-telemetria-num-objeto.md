# ADR 0027: Telemetria num objeto só, com temperatura

- Data: 2026-09-25
- Estado: aceita
- Origem: revisão no `SPECS.md` 12

## Contexto

A câmera precisava mandar a temperatura (bullet 2.2), que nem todo aparelho informa. O cliente
SignalR do Dart não manda argumento nulo, então um parâmetro `double?` a mais quebraria a chamada.

## Decisão

`ReportTelemetry` recebe um objeto só, `{ batteryLevel, isCharging, temperatureC, supportsH264 }`,
com `temperatureC` em °C ou `null`. `Device` e `CameraStatus` ganharam `temperatureC`, validada
entre -40 e 120 °C (`device.invalid_temperature`). A câmera manda telemetria também quando a
temperatura muda um grau inteiro.

## Consequências

- Campos novos de telemetria entram no objeto sem mudar a assinatura do método do hub.
- A lista de câmeras mostra a temperatura ao lado da bateria.
