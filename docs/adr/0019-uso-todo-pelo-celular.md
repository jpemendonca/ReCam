# ADR 0019: Uso todo pelo celular, papel decidido pelo QR

- Data: 2026-09-25
- Estado: aceita
- Origem: revisão no `SPECS.md` 12

## Contexto

No teste com dois celulares o uso ficou confuso: não dava para inverter os papéis, e só o dono
gerava QRs, e só de câmera.

## Decisão

Novo desenho, combinado com o autor: o PC serve para instalar e acompanhar, e todo o resto é pelo
celular.
- Um só "Ler QR": o papel do QR (`r=`) decide se o celular vira câmera ou Monitor.
- Qualquer Monitor gera QR de câmera e de Monitor.
- A primeira abertura pergunta para que o celular vai ser usado; o app não mostra o conceito de
  dono.
- O `/setup` vira painel de acompanhamento só leitura na rede local.

## Consequências

- O dono continua existindo no servidor, mas não aparece nas telas.
- Revogar e outras ações seguem no celular (depois, a tela Aparelhos, ADR 0033).
- A página do servidor mudou (ADRs 0022 e 0025).
