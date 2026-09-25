# ADR 0032: Listar e servir gravações

- Data: 2026-09-25
- Estado: aceita
- Origem: revisão no `SPECS.md` 12

## Contexto

O Monitor precisa achar e tocar as gravações de uma câmera por dia.

## Decisão

`GET /api/cameras/{id}/recordings?day=` devolve os trechos do dia UTC com seus segmentos (folga de
1 s para emendar). `GET /api/cameras/{id}/recording-days` lista os dias com gravação.
`GET /api/recordings/{cameraId}/{segmento}` serve o arquivo com `Range`; só nome no formato do
MediaMTX vira caminho no disco.

## Consequências

- O segmento mais novo fica de fora enquanto a câmera grava.
- O app converte os dias UTC para o dia local, pedindo dois dias UTC por dia local.
- A validação do nome do segmento impede path traversal.
