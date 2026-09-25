# ADR 0024: Sair do servidor e primeiro QR sem Monitor ativo

- Data: 2026-09-25
- Estado: aceita
- Origem: revisão no `SPECS.md` 12

## Contexto

"Reiniciar o app" apagava os dados locais, mas o aparelho continuava ativo no servidor. E um
servidor cujo dono tinha saído nunca mais mostrava o QR do primeiro celular.

## Decisão

Nova rota `DELETE /api/me` (qualquer aparelho, 204): o aparelho se revoga. "Reiniciar o app" chama
essa rota para cada aba pareada antes de apagar os dados; sem resposta, apaga mesmo assim. O QR
do primeiro celular passa a existir sempre que não há Monitor ativo.

## Consequências

- Um servidor sem Monitor volta a aceitar um primeiro celular, que vira o novo dono.
- Se o dono sai e um Monitor fica, não há QR: esse Monitor adiciona os demais.
