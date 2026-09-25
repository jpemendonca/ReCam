# ADR 0010: Só Android no MVP

- Data: 2026-09-24
- Estado: aceita
- Origem: decisão inicial, `SPECS.md` 12

## Contexto

O desenvolvimento é em Windows, e o iOS exige macOS para compilar. O iOS também não permite que
um app use a câmera em segundo plano, o que inviabiliza o modo câmera.

## Decisão

O MVP é só Android.

## Consequências

- Plugins e código nativo só precisam existir para Android (`MainActivity.kt`).
- iPhone pode vir depois só como Monitor, se houver demanda.
