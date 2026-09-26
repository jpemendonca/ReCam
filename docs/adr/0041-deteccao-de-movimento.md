# ADR 0041: Detecção de movimento pelas notas do serviço motion

- Data: 2026-09-26
- Estado: aceita
- Origem: revisão no `SPECS.md` 12

## Contexto

O autor quer achar os momentos importantes das gravações sem assistir horas. A regra do projeto
diz que o .NET não processa vídeo, e o celular câmera é fraco demais para comparar quadros. A
primeira ideia era usar a nota `scene` do FFmpeg, mas ela é feita para achar cortes de cena: numa
cena de teste, uma forma do tamanho de uma pessoa andando deu a mesma nota que a cena parada.

## Decisão

Um container `motion`, com a mesma imagem FFmpeg dos testes, sem rede e com o usuário do servidor,
roda `deploy/motion.sh`. Para cada segmento fechado, grava ao lado um arquivo com, a cada meio
segundo, a fração da imagem que mudou mais de 30 de 255 níveis. O servidor lê essas notas e monta os
eventos com a sensibilidade da câmera (baixa 3%, média 1%, alta 0,3%), emenda trechos a menos de
10 s e ignora os segundos depois de a lanterna mudar.

## Consequências

- O .NET continua sem processar vídeo; ele só lê números.
- Mudar a sensibilidade vale para o que já foi gravado, sem processar o vídeo de novo.
- O evento aparece quando o segmento de 60 s fecha, não na hora; serve para a linha do tempo, não
  para aviso imediato.
- Só funciona com "Gravar sempre" ligado.
- Movimento simples ainda dispara com mudança de luz, sombras e folhas; pessoas e carros com IA
  ficam para depois, opcionais na instalação.
