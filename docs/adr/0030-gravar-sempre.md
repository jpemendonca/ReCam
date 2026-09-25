# ADR 0030: "Gravar sempre" por câmera

- Data: 2026-09-25
- Estado: aceita
- Origem: revisão no `SPECS.md` 12

## Contexto

A gravação precisa de uma chave por câmera, e a câmera só pode gravar em H.264.

## Decisão

`Device` ganhou `RecordingEnabled` e `SupportsH264`. A regra é `Device.SetRecording(requester,
enabled)`: só Monitor ativo muda, só numa câmera, e ligar é recusado sem H.264
(`media.recording_needs_h264`). Hub: `SetRecording` para Monitores e `RecordingChanged` para a
câmera. Câmera gravando recebe `StartPublishing` ao conectar e não recebe `StopPublishing` quando o
último Monitor sai. O proxy manda para `rec-{id}` com gravação e para `cam-{id}` sem ela, e o
`Location` leva o prefixo (`cam-` ou `rec-`) antes da sessão.

## Consequências

- Mudar a gravação com a câmera transmitindo reinicia a publicação no outro path.
- Sessão sem prefixo conhecido devolve 404 `media.session_not_found`.
