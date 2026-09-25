# ADR 0038: Navegador como primeiro Monitor e primeiro celular como câmera

- Data: 2026-09-25
- Estado: aceita
- Origem: revisão no `SPECS.md` 12

## Contexto

No desenho anterior, o primeiro Monitor era um celular que lia o QR do log ou do `/setup`, e só
depois outro celular virava câmera. Para testar o produto a pessoa precisava de dois celulares, e
o PC servia só para instalar. O autor queria desde o começo que alguém pudesse usar o ReCam com um
celular e um computador.

## Decisão

O navegador do computador é sempre o primeiro Monitor. Ele entra com um código de primeira
abertura que o servidor imprime no log, aceito só da rede local e só enquanto não há Monitor
ativo. O navegador mostra o QR de "Adicionar câmera", e o primeiro celular pareado é sempre uma
câmera. O navegador é um Monitor completo. Outros celulares entram depois pelo navegador, como
câmera ou como Monitor. O token do dono, o QR no log e a página `/setup` saem; `/setup` redireciona
para a raiz. A credencial do navegador fica num cookie `HttpOnly`, `Secure` e `SameSite=Strict`, e
os métodos que mudam estado exigem o cabeçalho `X-Recam-Web: 1`. Um navegador novo, com Monitor já
existente, entra pelo "Conectar navegador", aprovado num celular Monitor.

## Consequências

- Um celular e um computador bastam. A pessoa vê a própria câmera funcionando no primeiro minuto.
- Decisões antigas caem: "não existe dashboard web" (ADR 0008) e o painel só leitura (ADRs 0022 e
  0025).
- O log deixa de ter QR com token; a exceção da regra de log passa a ser o código, que não é
  credencial.
- O navegador mostra aviso de certificado autoassinado na primeira abertura.
- Quem abrir primeiro na rede local com o código vira dono; o `reset-owner` continua sendo a saída.
