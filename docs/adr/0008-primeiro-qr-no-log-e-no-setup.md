# ADR 0008: Primeiro QR no log e em /setup

- Data: 2026-09-24
- Estado: aceita
- Origem: decisão inicial, `SPECS.md` 12

## Contexto

O primeiro celular precisa de um jeito de parear sem que exista ainda nenhum aparelho de
confiança. Um painel web de gestão resolveria, mas seria outra interface para manter e proteger.

## Decisão

Enquanto ninguém é dono, o servidor imprime o QR do primeiro pareamento no log e o mostra em
`/setup`, só para a rede local. O celular que lê esse QR vira o dono e passa a gerar os QRs das
câmeras. Esse QR no log é a única exceção à regra de nunca registrar token em log.

## Consequências

- Não existe dashboard web de gestão.
- A página `/setup` depois virou um painel só leitura (ADR 0022).
- A regra do "primeiro QR" foi ampliada para "sempre que não houver Monitor ativo" (ADR 0024).
