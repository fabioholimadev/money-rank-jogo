# Money Rank — Playtest

O formato inicial já possui exportação versionada. O formato definitivo será
expandido junto à UX do mestre e aos módulos de gameplay ainda pendentes.

## Diretrizes já aprovadas

- Não coletar dados pessoais desnecessários.
- Registrar IDs e versões do conteúdo utilizado.
- Preservar estado antes/depois de ações confirmadas.
- Registrar duração da sessão e dos turnos.
- Exportação inicial prevista em JSON completo e CSV resumido.

## Exportação atual

`FilePlaytestExporter` grava dois arquivos por sessão, usando hash do `sessionId`
no nome físico e mantendo `.bak` ao sobrescrever:

- JSON com versão do formato, horário de exportação, janela opcional da sessão,
  participantes e seus personagens associados, metadados de undo e o memento
  completo da sessão;
- CSV com `sessionId`, fase, jogadores, resoluções de evento, transações
  econômicas, total de ações, ciclos anuais processados, jogadores internados,
  undos, duração e horário de exportação.

Campos cuja origem ainda não existe no domínio não são estimados. Eles serão
adicionados ao JSON/CSV quando os respectivos módulos auditáveis forem
integrados.

## Persistência no sandbox

O `DevSandbox` grava automaticamente a sessão local após comandos aceitos e tenta
restaurá-la ao abrir novamente. Os arquivos ficam sob
`Application.persistentDataPath/MoneyRank/Saves`; uma falha de compatibilidade ou
arquivo corrompido é exibida como erro e não é sobrescrita silenciosamente.

O painel do mestre no sandbox exibe as ações mais recentes, cria checkpoints
antes de movimento, confirmação de evento e progressão, permite desfazer a
última ação reversível sem apagar a auditoria e exporta JSON/CSV para
`Application.persistentDataPath/MoneyRank/Exports`. Quando um diretório de teste
é injetado, a exportação fica isolada sob esse diretório.

## Validação automatizada do fluxo

O teste PlayMode carrega `DevSandbox`, usa persistência isolada, registra um
movimento animado, aguarda a peça terminar, confirma o evento, aplica progressão
e verifica a troca de jogador. Testes de cena em EditMode cobrem retomada de
save, evento ativo, resultado, Undo com auditoria preservada e exportação.

