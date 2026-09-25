# ADR 0028: Qualidade reduzida no calor

- Data: 2026-09-25
- Estado: aceita
- Origem: revisão no `SPECS.md` 12

## Contexto

O celular fica na tomada por dias e esquenta, principalmente transmitindo. Aparelho quente perde
bateria mais rápido e pode desligar.

## Decisão

Com a bateria a 42 °C ou mais, a câmera passa a mandar 853x480 (escala 1,5 sobre 1280x720), 10 fps
e 400 kbps, trocando os parâmetros do envio sem reabrir a câmera. Volta a 1280x720, 15 fps e
700 kbps abaixo de 38 °C. Entre os dois limites, mantém o que estava.

## Consequências

- A histerese evita alternar a qualidade a cada leitura.
- O modo câmera avisa que a qualidade foi reduzida pelo calor.
