# ADR 0013: Vertical slices num projeto só

- Data: 2026-09-24
- Estado: aceita
- Origem: decisão inicial, `SPECS.md` 12

## Contexto

O domínio é pequeno. Clean Architecture em vários projetos (Domain, Application,
Infrastructure, Api) multiplicaria arquivos e indireções sem ganho proporcional.

## Decisão

Um projeto só, `Recam.Server`, organizado em `Domain/`, `Infrastructure/` e `Features/<Feature>/`.
As fronteiras são garantidas por teste de arquitetura (NetArchTest): `Domain` não depende de
fora, `Infrastructure` não depende de `Features`, e uma feature não depende de outra.

## Consequências

- O que duas features compartilham vai para `Domain` ou `Infrastructure`
  (exemplos: `DevicePresence`, `IDeviceRemovals`).
- Os endpoints e o hub ficam finos: orquestram e delegam a regra à entidade.
