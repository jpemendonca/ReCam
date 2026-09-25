# ADR 0009: Um app com duas abas

- Data: 2026-09-24
- Estado: aceita
- Origem: decisão inicial, `SPECS.md` 12

## Contexto

Os apps parecidos do mercado separam câmera e visualizador em fluxos pesados, com conta e
anúncio. O ReCam quer ser leve e simples, e o mesmo celular pode mudar de papel.

## Decisão

Um app só, com as abas Câmera e Monitor.

## Consequências

- Um APK para instalar em todos os aparelhos.
- A primeira tela pergunta para que o celular vai ser usado (Filmar ou Assistir).
- O código do app separa `camera/` e `viewer/`, que não importam um ao outro.
