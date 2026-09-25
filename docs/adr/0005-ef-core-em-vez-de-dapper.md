# ADR 0005: EF Core em vez de Dapper

- Data: 2026-09-24
- Estado: aceita
- Origem: decisão inicial, `SPECS.md` 12

## Contexto

O banco é pequeno: alguns aparelhos, tokens de pareamento e configurações. O usuário atualiza o
servidor trocando a imagem Docker, sem rodar script de banco à mão.

## Decisão

O acesso a dados usa EF Core, com migrações versionadas em
`Infrastructure/Persistence/Migrations` e aplicadas automaticamente no startup.

## Consequências

- O ganho de desempenho do Dapper não apareceria num banco deste tamanho.
- Toda mudança de modelo vira uma migração criada com `dotnet ef migrations add`.
- O domínio fica com entidades ricas mapeadas pelo EF, sem camada de repositório.
