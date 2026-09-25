# ADR 0006: SQLite como banco

- Data: 2026-09-24
- Estado: aceita
- Origem: decisão inicial, `SPECS.md` 12

## Contexto

O servidor roda num nó só, em casa, muitas vezes numa máquina modesta. Um banco em outro
container seria mais uma peça para instalar, atualizar e fazer backup.

## Decisão

O banco é SQLite, num arquivo dentro de `/data`, no volume `recam-data`.

## Consequências

- Sem terceiro container.
- Backup é copiar o volume.
- Não há escala horizontal do servidor; o projeto não precisa dela.
