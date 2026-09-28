# ADR 0043: Detecção de pessoas num container opcional

- Data: 2026-09-28
- Estado: aceita na branch `claude/person-activity-detection-4pq7pg` (pode não ir para a `main`)
- Origem: revisão no `SPECS.md` 12

## Contexto

O autor quer uma linha do tempo que diga "14:02, pessoa no Quarto". O movimento do 7.2 dispara
também com luz, sombra e folha. O projeto precisa continuar leve para quem não quer IA, e nada
pode sair de casa.

## Decisão

Um serviço `detect`, só num compose à parte (`deploy/compose.detect.yaml`), roda um worker .NET
(`Recam.Detect`) com o YOLOX-Tiny em ONNX Runtime, em CPU. Ele olha um quadro por segundo, só nos
segundos que o `motion` marcou, e grava ao lado do segmento um `.people` com as pessoas e as
caixas. O servidor só lê esse arquivo. O container não tem rede nem porta.

O ROADMAP pedia YOLO nano da Ultralytics. Ficou o YOLOX-Tiny: a Ultralytics só publica pesos em
PyTorch, e exportar para ONNX no build exigiria PyTorch; o YOLOX tem o ONNX pronto na release dos
autores e licença Apache-2.0.

## Consequências

- Quem não sobe o compose extra não baixa nada e não perde nada.
- O `Recam.Server` continua sem processar vídeo; a regra passa a dizer isso com todas as letras.
- A cifra das gravações espera o `.people` enquanto o `detect` roda, até 30 minutos. Num PC que
  não dá conta, parte das gravações fica sem análise, e a tela mostra isso como "não analisado".
- A imagem do `detect` fica em cima da mesma `linuxserver/ffmpeg` do `motion`, que já está na
  máquina: a parte nova é o worker, o ONNX Runtime e o modelo (cerca de 260 MB).
- Só pessoas. Carros, animais, zonas e descrição em texto ficam fora.
