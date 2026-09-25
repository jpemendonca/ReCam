# ADR 0004: Transmissão sob demanda

- Data: 2026-09-24
- Estado: aceita
- Origem: decisão inicial, `SPECS.md` 12

## Contexto

O celular câmera fica ligado na tomada por dias. Transmitir o tempo todo esquenta o aparelho e
gasta bateria, e, sem gravação no MVP, ninguém aproveita o vídeo que não está sendo assistido.

## Decisão

A câmera só publica enquanto alguém assiste. O servidor controla isso por leases: o primeiro
Monitor que abre a câmera faz o servidor mandar `StartPublishing`; quando o último sai, o servidor
espera um período de carência e manda `StopPublishing`.

## Consequências

- Menos calor e menos bateria no celular fraco.
- Abrir o vídeo ao vivo leva o tempo de a câmera abrir a captura e negociar o WebRTC.
- Quando a gravação entrou (ADR 0030), a regra ganhou uma exceção: câmera com "Gravar sempre"
  transmite mesmo sem ninguém assistindo.
