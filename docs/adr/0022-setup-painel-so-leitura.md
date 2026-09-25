# ADR 0022: /setup como painel só leitura

- Data: 2026-09-25
- Estado: aceita
- Origem: revisão no `SPECS.md` 12

## Contexto

Depois do primeiro pareamento, o `/setup` mostrava só "already configured". Quem instalou o
servidor no PC não tinha como ver o que estava conectado.

## Decisão

Com um celular já pareado, `GET /setup` vira um painel só leitura, na mesma regra de acesso (só
rede local): nome, tipo, online, transmitindo, quantos assistem e bateria. Recarrega sozinho a
cada 5 s por `<meta http-equiv="refresh">`, sem script e sem ações. A página do QR se recarrega a
cada 30 s para virar o painel depois do primeiro pareamento.

## Consequências

- Continua não existindo dashboard de gestão.
- A contagem de quem assiste foi para o `DevicePresence` (Infrastructure), porque `Features.Setup`
  não pode ler `Features.Realtime`.
