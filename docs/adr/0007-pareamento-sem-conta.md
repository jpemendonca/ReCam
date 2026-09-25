# ADR 0007: Pareamento sem conta

- Data: 2026-09-24
- Estado: aceita
- Origem: decisão inicial, `SPECS.md` 12

## Contexto

O ReCam não tem nuvem nem cadastro. Pedir e-mail e senha num servidor caseiro seria atrito e mais
um segredo para guardar.

## Decisão

Cada celular é um dispositivo com credencial própria, obtida ao ler um QR de pareamento de uso
único. O primeiro celular vira o dono e pareia os demais. No banco fica só o hash da credencial.

## Consequências

- Não existe login, recuperação de senha nem conta.
- Revogar um aparelho é apagar a validade da credencial dele.
- Perder o único Monitor exige uma saída no servidor, que veio no `reset-owner` (ADR 0034).
