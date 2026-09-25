# ADR 0021: Mensagem WatchersChanged para a câmera

- Data: 2026-09-25
- Estado: aceita
- Origem: revisão no `SPECS.md` 12

## Contexto

O celular câmera precisava mostrar quantas pessoas estão assistindo (bullet 1.12.7), e o servidor
não mandava essa informação.

## Decisão

Nova mensagem servidor → câmera, `WatchersChanged(int count)`, com quantas conexões de Monitor têm
lease aberto na câmera. Vai a cada mudança (lease aberto, fechado ou perdido por desconexão) e ao
conectar a câmera. `UnwatchCamera` espera esse aviso antes de responder.

## Consequências

- O modo câmera mostra "N assistindo" sem consultar o servidor.
- A contagem também passou a ficar no `DevicePresence`, onde outras features a leem.
