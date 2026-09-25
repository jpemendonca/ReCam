# ADR 0031: Cota e limpeza das gravações

- Data: 2026-09-25
- Estado: aceita
- Origem: revisão no `SPECS.md` 12

## Contexto

Gravar sem limite enche o disco do servidor.

## Decisão

Tabela `RecordingQuota` (linha única), padrão 2048 MB, mínimo 100 MB. `GET` e `PUT
/api/recordings/quota` para Monitores; a cota não pode passar de uso mais espaço livre. A cada 60 s
um `BackgroundService` aplica `RecordingQuota.PlanCleanup`: apaga tudo de câmeras removidas e depois
os segmentos mais antigos até caber, sem apagar o segmento mais novo de cada câmera.

## Consequências

- O MediaMTX nunca perde o arquivo em que está escrevendo.
- A página do servidor mostra o uso ("1,2 de 2 GB em uso").
- O app tem a tela Gravações com a cota em horas aproximadas.
