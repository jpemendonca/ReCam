# ADR 0033: Remover aparelhos

- Data: 2026-09-25
- Estado: aceita
- Origem: revisão no `SPECS.md` 12

## Contexto

Não havia como tirar do servidor um celular perdido ou trocado.

## Decisão

`GET /api/devices` lista os aparelhos e `DELETE /api/devices/{id}` remove, pela regra
`Device.RevokeBy(requester, now)`: só Monitor ativo remove, nunca a si mesmo. Remover derruba as
conexões do hub do aparelho e avisa os Monitores com `CameraRemoved`. A feature `Realtime`
implementa isso atrás de `IDeviceRemovals`, em `Infrastructure/Realtime`.

## Consequências

- `Devices` não depende de `Realtime` (inversão de dependência).
- A câmera removida recebe 401 ao reconectar e fecha o modo câmera.
