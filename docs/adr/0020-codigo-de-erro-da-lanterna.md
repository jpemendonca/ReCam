# ADR 0020: Códigos de erro da lanterna

- Data: 2026-09-25
- Estado: aceita
- Origem: revisão no `SPECS.md` 12

## Contexto

A tabela do protocolo dizia `camera-not-publishing` para `SetTorch` com a câmera parada, fora do
formato `área.nome` dos outros erros de domínio.

## Decisão

O código é `media.camera_not_publishing`. Câmera inexistente devolve `media.camera_not_found`.

## Consequências

- Todos os erros seguem o mesmo formato, e o app trata por código.
