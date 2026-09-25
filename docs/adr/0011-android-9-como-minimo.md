# ADR 0011: Android 9 como mínimo (minSdk 28)

- Data: 2026-09-24
- Estado: aceita
- Origem: decisão inicial, `SPECS.md` 12

## Contexto

Os aparelhos de referência são de 2019, com 2 GB de RAM. Suportar versões mais antigas traria
APIs de câmera e de serviço em primeiro plano mais difíceis.

## Decisão

`minSdk 28` (Android 9).

## Consequências

- Os celulares parados mais comuns de 2019 em diante entram.
- APIs anteriores ao Android 9 não precisam de caminho alternativo.
