# ADR 0016: Gravação e acesso remoto adiados

- Data: 2026-09-24
- Estado: aceita
- Origem: decisão inicial, `SPECS.md` 12

## Contexto

A gravação é a parte mais cara do projeto (disco, cota, player, codec). Acesso de fora da rede
local depende da infraestrutura de cada usuário.

## Decisão

O MVP não grava. Acesso remoto fica por conta do usuário (porta aberta, Tailscale, túnel).

## Consequências

- O MVP pode transmitir só sob demanda (ADR 0004).
- A gravação voltou ao escopo depois (ADRs 0026 e 0029). O acesso remoto segue fora, com o
  modo atrás de proxy reverso como ajuda (ADR 0035).
