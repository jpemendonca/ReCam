# ADR 0017: Volume nomeado para /data

- Data: 2026-09-24
- Estado: aceita
- Origem: revisão no `SPECS.md` 12

## Contexto

O `/data` começou como bind mount em `deploy/data/`. No Linux, o Docker cria a pasta do bind mount
como root, e o servidor roda como usuário não-root, sem permissão de escrita nela.

## Decisão

`/data` passou a ser o volume nomeado `recam-data`. O Dockerfile cria `/data` com o dono
não-root, e o Docker copia essa permissão para o volume no primeiro uso.

## Consequências

- O servidor sobe sem ajuste de permissão no host.
- O banco e o certificado não ficam numa pasta visível do repositório; backup é pelo volume.
- As gravações seguiram o mesmo mecanismo no volume `recam-recordings` (ADR 0029).
