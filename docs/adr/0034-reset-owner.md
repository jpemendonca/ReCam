# ADR 0034: Comando reset-owner

- Data: 2026-09-25
- Estado: aceita
- Origem: revisão no `SPECS.md` 12

## Contexto

Se o único Monitor quebrar ou sumir, ninguém mais consegue gerenciar o servidor.

## Decisão

`docker compose exec server ./Recam.Server reset-owner` revoga todos os Monitores ativos e deixa as
câmeras pareadas. O servidor volta a gerar o QR do primeiro Monitor em até 30 s.

## Consequências

- A recuperação é feita no PC, por quem tem acesso ao Docker.
- As conexões do hub desses Monitores só caem na próxima reconexão, porque o comando roda em
  outro processo.
