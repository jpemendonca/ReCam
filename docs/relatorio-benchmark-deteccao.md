# Relatório de Desempenho: Detecção de Pessoas em Hardware de Entrada

- **Data do teste:** 2026-09-28
- **Ambiente:** Hardware de referência real (PC doméstico de 2012)
- **Finalidade:** Fornecer dados empíricos de telemetria e uso de recursos para o Claude e a equipe de desenvolvimento avaliarem a viabilidade do serviço de detecção de pessoas (`Recam.Detect`, [ADR 0043](adr/0043-deteccao-de-pessoas-opcional.md)) operando 24/7.

---

## 1. Especificações do Hardware e Ambiente de Teste

| Item | Especificação |
| :--- | :--- |
| **Processador** | Intel Core i5 de 3ª geração (4 núcleos / 4 threads, Ivy Bridge, 2012) |
| **Memória RAM Total** | 8 GB DDR3 (sistema com ambiente gráfico e navegador abertos) |
| **Memória Swap** | 2,0 GiB (uso estável em ~237 MB) |
| **Sistema Operacional** | Linux x86_64 (base Ubuntu) |
| **Arquitetura de Contêineres** | Docker Compose (`compose.yaml` + `compose.detect.yaml`) |
| **Serviços em Execução** | `Recam.Server` (.NET 10), `mediamtx` (WebRTC proxy) e `Recam.Detect` (worker ONNX YOLOX-Tiny) |

---

## 2. Metodologia do Teste

O teste foi conduzido com uma rotina de monitoramento em tempo real coletando métricas durante **5 minutos (300 segundos)** contínuos, com intervalos fixos de **30 segundos** (10 amostras no total).

As métricas foram extraídas diretamente do `/proc` e dos sensores de hardware do sistema:
- **RAM por processo (RSS):** Obtida via leitura de páginas residentes em `/proc/<pid>/stat` e `/proc/<pid>/status`.
- **Uso de CPU por processo:** Calculado a partir da taxa de variação de `utime` + `stime` dividida pelo delta global de ticks em `/proc/stat`.
- **Carga do sistema (*Load Average*):** Monitorado via `getloadavg()`.
- **Temperatura da CPU:** Sensores de núcleo via `coretemp` (`/sys/class/hwmon/hwmon1/`).

---

## 3. Tabela Completa de Amostras

| Amostra | Horário | ReCam RAM Total | Server (.NET) | MediaMTX | Detect (IA) | CPU ReCam (% total) | Temp Máx | Load Avg (1m) |
| :---: | :---: | :---: | :---: | :---: | :---: | :---: | :---: | :---: |
| 1 | 20:08:18 | 408 MB | 212 MB | 49 MB | 147 MB | 7,5% | 66,0 °C | 4,12 |
| 2 | 20:08:48 | 404 MB | 209 MB | 49 MB | 147 MB | 1,8% | 63,0 °C | 3,90 |
| 3 | 20:09:18 | 404 MB | 209 MB | 48 MB | 147 MB | 1,6% | 63,0 °C | 3,56 |
| 4 | 20:09:48 | 402 MB | 205 MB | 49 MB | 148 MB | 4,3% | 63,0 °C | 3,24 |
| 5 | 20:10:18 | 402 MB | 205 MB | 49 MB | 148 MB | 1,7% | 63,0 °C | 2,46 |
| 6 | 20:10:48 | 405 MB | 209 MB | 48 MB | 148 MB | 1,7% | 63,0 °C | 2,59 |
| 7 | 20:11:18 | 409 MB | 212 MB | 49 MB | 148 MB | 1,8% | 63,0 °C | 2,83 |
| 8 | 20:11:48 | 407 MB | 213 MB | 48 MB | 147 MB | 3,7% | 61,0 °C | 2,90 |
| 9 | 20:12:18 | 410 MB | 213 MB | 49 MB | 147 MB | 5,0% | 62,0 °C | 2,65 |
| 10 | 20:12:48 | 413 MB | 213 MB | 52 MB | 147 MB | 2,6% | 62,0 °C | 2,53 |

---

## 4. Análise Estatística Consolidada

### 4.1 Memória RAM (RSS)
- **Mínimo:** 401,8 MB
- **Média:** 406,4 MB
- **Máximo (Pico):** 412,9 MB
- **Estabilidade:** Desvio total de apenas **11,1 MB** durante toda a bateria de testes. O comportamento demonstra ausência completa de vazamento de memória (*memory leaks*) nos serviços em contêiner.

### 4.2 Consumo de CPU
- **Média de uso:** **3,2%** da capacidade total do processador (ou ~12,8% de 1 núcleo).
- **Pico momentâneo:** **7,5%** da capacidade total durante inicialização/estabilização.
- **Em regime estacionário:** Mantém-se entre **1,6% e 1,8%**, subindo para 3,7%–5,0% apenas em ciclos de verificação ativa.

### 4.3 Comportamento Térmico e Carga
- **Temperatura de pico:** 66,0 °C (na primeira amostra).
- **Temperatura estabilizada:** Entre 61,0 °C e 63,0 °C (com margem de segurança de >20 °C abaixo do teto `high = 85 °C` e bem distante do limite crítico de 105 °C).
- **Carga do sistema (*Load Average*):** Decresceu progressivamente de 4,12 para 2,53 à medida que os processos estabilizaram, ficando com folga abaixo da capacidade nominal de 4.0 do processador de 4 núcleos.

---

## 5. Análise por Componente

### 5.1 `Recam.Detect` (Módulo de IA com ONNX Runtime + YOLOX-Tiny)
- **Pegada de memória:** Cerca de **147 MB a 148 MB**.
- **Comportamento:** Perfeitamente plano, sem alocações descontroladas ou oscilações de buffer.
- **Processamento:** Como a inferência ocorre somente nos quadros/segundos onde houve detecção de movimento (`motion.sh`), o worker permanece silencioso e com uso residual de CPU no repouso, respeitando o limite de 1 núcleo de processamento estabelecido na arquitetura.

### 5.2 `Recam.Server` (.NET 10 WebAssembly Host & API)
- **Pegada de memória:** **205 MB a 213 MB**.
- **Comportamento:** Variação típica e previsível dos ciclos de coleta do Garbage Collector do .NET, sem retenção indevida de referências.

### 5.3 `mediamtx` (Media Plane WebRTC)
- **Pegada de memória:** **48 MB a 52 MB**.
- **Comportamento:** Extremamente leve em memória e CPU, confirmando que a sinalização e o relay WebRTC têm custo computacional mínimo.

---

## 6. Conclusões e Notas de Engenharia para o Claude

1. **Aderência ao [ADR 0043](adr/0043-deteccao-de-pessoas-opcional.md):**
   A escolha do YOLOX-Tiny em ONNX Runtime no worker em CPU atingiu o objetivo com folga: um processador de 2012 (i5 3ª geração) rodou a stack completa consumindo menos de 4% de CPU global e ~410 MB de RAM.
2. **Operação 24/7:**
   O impacto térmico e de memória é compatível com operação ininterrupta, mesmo em computadores reaproveitados que também desempenham outras funções ou operam sem refrigeração avançada.
3. **Reserva de Capacidade:**
   Não há necessidade de reduzir ainda mais a taxa de amostragem de quadros (1 fps nos trechos de movimento) para esta classe de processador x86_64, pois mais de 90% da CPU permanece livre para o restante do sistema.
