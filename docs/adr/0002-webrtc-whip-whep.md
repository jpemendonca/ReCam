# ADR 0002: WebRTC com WHIP e WHEP

- Data: 2026-09-24
- Estado: aceita
- Origem: decisão inicial, `SPECS.md` 12

## Contexto

O caminho principal promete vídeo ao vivo com atraso abaixo de 1 segundo na rede local, em
celulares fracos. RTMP entrega entre 1 e 3 segundos. RTSP não tem biblioteca madura em Flutter.

## Decisão

A câmera publica com WebRTC via WHIP e o Monitor assiste via WHEP. A sinalização é HTTP com SDP;
a mídia vai por UDP direto entre o celular e o MediaMTX.

## Consequências

- Atraso abaixo de 1 s e controle de congestionamento embutido no WebRTC.
- O app usa `flutter_webrtc`, que já negocia H.264 e VP8 com o encoder do aparelho.
- A porta UDP 8189 precisa estar alcançável a partir dos celulares; um proxy HTTP não resolve o
  vídeo.
