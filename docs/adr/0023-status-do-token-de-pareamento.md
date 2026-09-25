# ADR 0023: Consulta de uso do token de pareamento

- Data: 2026-09-25
- Estado: aceita
- Origem: revisão no `SPECS.md` 12

## Contexto

A tela do QR de "Adicionar câmera" ficava aberta depois que a câmera pareava. O Monitor não
sabia que o token tinha sido usado.

## Decisão

`POST /api/pairing-tokens` devolve `{ id, qrUri, expiresAt }`. Nova rota
`GET /api/pairing-tokens/{id}` devolve `{ used }` só para o aparelho que criou o token; para
qualquer outro, e para id inexistente, 404 `pairing.token_not_found`, igual nos dois casos. A
regra é `PairingToken.UsageFor(deviceId)`.

## Consequências

- O app consulta a cada 2 s e fecha a tela quando o token é usado.
- A resposta igual para "não é seu" e "não existe" não revela tokens de outros aparelhos.
