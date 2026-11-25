# Configuração necessária para testar o online

## Projeto Unity

1. Abra `Edit > Project Settings > Services` e confirme que o projeto continua
   vinculado ao Cloud Project usado pela equipe.
2. No Unity Dashboard, abra esse mesmo projeto e habilite Authentication,
   Multiplayer Sessions/Lobby e Relay no ambiente usado para testes.
3. Use builds de Windows e Android ligadas ao mesmo Cloud Project e ambiente.
   O PC cria a sala; o celular entra pelo código ou pelo QR mostrado pelo PC.
4. O primeiro acesso autentica anonimamente. O nickname é salvo localmente e
   enviado como nome de exibição, nunca como credencial.

## Estado da implementação

- A sala usa Relay; não depende de os dispositivos estarem na mesma rede nem de
  encaminhar portas no roteador.
- O QR é produzido localmente e codifica somente o código da sessão.
- `MaxPlayers` conta o mestre. O limite é cinco membros no total: o mestre do PC
  e até quatro participantes; os celulares entram por código ou QR.
- O código de sala é obtido diretamente de `ISession.Code` e a entrada usa
  `JoinSessionByCodeAsync`. O cliente remove espaços externos e converte para
  maiúsculas antes da chamada do SDK.
- Mensagens de sessão usam `SessionException.Error` quando o SDK fornece um
  código estruturado. Código não encontrado, sala encerrada, limite excedido e
  falha de rede não devem compartilhar a mesma mensagem.
- Android inicia em orientação horizontal. A interface IMGUI redimensiona os
  painéis pela resolução real e mantém os elementos dentro da área segura; o
  nickname e o código abrem o teclado nativo no celular.
- Ainda não há uma versão completa da partida sincronizada. A sala e a lista de
  membros funcionam como primeira etapa; sincronização de comandos/estado do
  tabuleiro e validação com dois dispositivos continuam pendentes.
- Android Build Support, SDK e NDK não estavam instalados junto ao Editor
  `6000.6.3f1` verificado nesta etapa. Instale o módulo pelo Unity Hub para criar
  um APK; não aceite ou configure uma keystore de publicação para o protótipo.

## Dependência de QR

O leitor/gerador usa a distribuição ZXing.Net para Unity. Os avisos de licença
Apache 2.0 e MIT estão em `Assets/_MoneyRank/ThirdParty/ZXing/`.
