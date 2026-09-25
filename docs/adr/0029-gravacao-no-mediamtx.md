# ADR 0029: Gravação no MediaMTX

- Data: 2026-09-25
- Estado: aceita
- Origem: revisão no `SPECS.md` 12

## Contexto

A gravação entrou no escopo. O MediaMTX já grava em fMP4, e o servidor precisa listar, servir e
apagar os arquivos.

## Decisão

Paths `rec-<id>` gravam em fMP4, em segmentos de 60 s, no volume `recam-recordings`; paths
`cam-<id>` só repassam. O MediaMTX roda com o usuário do servidor (`1654:1654`) para o servidor
poder apagar os segmentos. VP8 não é gravado.

## Consequências

- O `SPECS.md` ganhou a seção 2.4, e o risco de VP8 foi para a seção 11.
- Os dois composes montam o mesmo volume nos dois containers.
