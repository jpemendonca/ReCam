# ADR 0015: Padrão do servidor antes do loop

- Data: 2026-09-24
- Estado: aceita
- Origem: decisão inicial, `SPECS.md` 12

## Contexto

O servidor seria escrito em grande parte por um agente em loop. Sem um padrão firme no começo,
cada slice poderia sair num estilo diferente.

## Decisão

Os primeiros slices do servidor foram feitos junto com o autor para fixar o padrão. Depois, os
bullets podem seguir em loop com um agente.

## Consequências

- Os bullets marcados `[junto]` existiram enquanto o padrão se firmava.
- Desde 2026-09-25 o autor não precisa mais estar na conversa nesses bullets (`AGENTS.md`).
- A Fase 5 tem bullets de vitrine (CI, observabilidade, ADRs, README).
