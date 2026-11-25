# Money Rank — Arquitetura

## Direção

O código usa uma arquitetura em camadas. Dependências apontam para dentro:

```text
Presentation -> Application -> Domain
Infrastructure -> Application/Domain
Content -> Application/Domain
```

## Assemblies atuais

### `MoneyRank.Domain`

- Estado e invariantes da sessão.
- IDs estáveis.
- Jogadores e ordem de turnos.
- Máquina de estados explícita.
- Não referencia `UnityEngine` (`noEngineReferences: true`).

### `MoneyRank.Application`

- Casos de uso que coordenam o domínio.
- Interfaces de persistência.
- Não contém UI nem referências ao Unity.

### `MoneyRank.Domain.EditModeTests`

- Testes rápidos de regras e transições.
- Executados no Unity Test Framework em EditMode.

### `MoneyRank.Content`

- ScriptableObjects usados apenas como definições editáveis.
- Converte dados serializados para modelos imutáveis do domínio.

### `MoneyRank.Presentation`

- Montagem visual do tabuleiro e animação da peça.
- Recebe resultados já calculados pelo domínio; não decide regras.

### `MoneyRank.Infrastructure`

- Implementa adapters locais de arquivo e serialização.
- Pode depender do Unity, mas não é referenciada por Domain/Application.
- Persiste mementos; não decide regras nem reconstrói definições de conteúdo.

## Decisões da Fase 0

- `GameSession` é o agregado raiz inicial.
- `GameFlowStateMachine` é a única fonte das transições permitidas.
- Falhas esperadas retornam `OperationResult` com código estável para futura UI.
- `SessionRules` recebe os limites de jogadores por configuração.
- Persistência concreta, eventos de domínio e snapshots entram em fases futuras.

## Limites da fundação

As decisões finais de balanceamento, movimento, economia, atributos, Hospital,
tributação, captura e vitória continuam fora do código. Os módulos já existentes
expõem definições e políticas substituíveis para que essas regras sejam
aprovadas como conteúdo antes de serem ativadas.

## Fase 2 — fundação do Event Engine

- Definições de evento, opção, requisito e efeito vivem em
  `MoneyRank.Domain.Events` e não dependem de `UnityEngine`.
- Eventos e opções preservam IDs estáveis, versão e conteúdo educacional.
- Requisitos são descritos por `typeId` e parâmetros; efeitos por IDs de alvo,
  operação, duração e origem, evitando enums vinculados às decisões abertas.
- O preview é estritamente somente leitura; a confirmação aplica efeitos ao
  estado da sessão de forma transacional.
- `RequirementEvaluator` despacha requisitos por `typeId`; handlers duplicados
  são rejeitados e tipos sem handler produzem diagnóstico estável.
- `EventPreviewService` avalia condições e requisitos e devolve os efeitos
  planejados e o snapshot projetado sem mutar a sessão. A confirmação e a
  aplicação ficam separadas.
- `EffectExecutor` despacha operações por `operationId`; a confirmação aplica os
  efeitos primeiro sobre uma cópia do estado e só efetiva o conjunto completo.
- Confirmações bem-sucedidas geram `EventResolutionRecord` versionado com estado
  anterior/posterior e entram em um histórico append-only.
- Cada jogador possui um `EventState` genérico dentro de `GameSession`; a sessão
  também é dona do histórico de resoluções.
- `GameEventService` restringe preview a `ResolveSpace`/`AwaitChoice` e confirmação
  a `AwaitChoice`, rejeitando previews de outra sessão, jogador ou estado antigo.
- `EventContentCatalog` indexa definições pela combinação estável `eventId@version`
  e valida todas as referências das tabelas antes de disponibilizar o conteúdo.
- `EventSelectionService` delega a escolha a `IEventSelectionPolicy` e valida que
  a chave retornada pertence à tabela solicitada. O motor não assume sorteio,
  pesos, descarte ou repetição; uma política rejeitada não recebe fallback oculto.
- Handlers genéricos `state.equals`, `state.exists`, `state.set` e `state.remove`
  permitem que conteúdo provisório exercite requisitos e efeitos sem criar
  enums de atributos ou regras financeiras no Event Engine.
- `EventDefinitionAsset`, `EventTableDefinitionAsset` e
  `EventContentCatalogAsset` são adapters de authoring: convertem dados do
  Inspector em modelos imutáveis e não armazenam estado de sessão.
- Tabelas preservam uma lista ordenada de referências, sem definir sorteio,
  pesos ou política de repetição enquanto essas regras não forem aprovadas.
- `EventPanelViewModel` transforma definições e previews em opções, diagnósticos
  e linhas ordenadas de antes/depois; `EventPanelView` apenas renderiza e emite
  pedidos de escolha/confirmação, sem executar regras.
- A seleção exata do evento ativo (jogador, casa, tabela, política, evento,
  versão e índice) pertence à sessão e integra o save; uma retomada em
  `AwaitChoice` reabre o mesmo evento, sem novo sorteio implícito.
- O `DevSandbox` contém um catálogo temporário explícito com oito eventos e
  quatro tabelas. A política `sandbox-table-index` é determinística e serve
  somente ao vertical slice, sem aprovar sorteio ou repetição da V3.
- Ao abrir o modal, todas as opções são avaliadas sem mutação. Opções bloqueadas
  ficam desabilitadas com diagnóstico; o preview pode ser cancelado para voltar
  à seleção sem alterar o estado.
- Cada classe de `ScriptableObject` pública fica em arquivo homônimo, garantindo
  importação e referências YAML corretas pelo Unity.

## Fase 1 — núcleo do tabuleiro externo

- `BoardDefinition` contém uma lista ordenada de casas e não pressupõe 28 posições.
- `startIndex` é configurável para que o layout não determine a regra.
- `BoardMovementCalculator` calcula destino e voltas sem depender de animação.
- `ExternalBoardState` mantém posição e voltas por jogador.
- `GameSession.RegisterMove` valida a fase, atualiza apenas o estado do jogador
  atual e avança de `AwaitMove` para `ResolveSpace`.
- `GameSession.BeginTurn` é o comando explícito que abre a janela de movimento;
  `GameSession.CompleteTurn` rotaciona o jogador somente após `EndTurn`.
- `GameSessionService` persiste os comandos de início e encerramento apenas
  quando aceitos pelo domínio.
- `GameSessionService.RegisterMove` persiste somente movimentos aceitos.
- `BoardDefinitionAsset` contém as 36 posições temporárias do perímetro visual
  (quatro cantos e oito casas entre eles) e quatro
  categorias, mantendo todo o conteúdo editável no Inspector.
- `BoardLayoutView` monta o perímetro de forma reproduzível a partir da definição.
- `PlayerPieceView` somente representa um `BoardMoveResult`; posição e voltas são
  atualizadas primeiro em `ExternalBoardState`.
- `BoardCameraController` mantém estados visuais explícitos, preserva a pose de
  visão geral e exige um alvo nos estados de foco.
- O `DevSandbox` acompanha a peça durante a animação e termina focando a casa de
  destino, sem transferir lógica de movimento para a câmera.
- O `DevSandbox` cria uma sessão temporária de dois jogadores; cada jogador
  mantém estado e peça visual próprios. `LocalSessionSetupService` fornece o
  caso de uso transacional reutilizável para criar e iniciar sessões locais de
  2–4 jogadores com nomes, ordem e personagens validados antes do primeiro save.
- Enquanto o sandbox não possui conteúdo de evento associado às casas, o comando
  de desenvolvimento para concluir o turno percorre apenas as transições-base
  aprovadas e não se torna regra da aplicação final.

## Fase 3 — fundação de atributos e progressão

- `AttributeDefinition` usa IDs configuráveis para tipo e comportamento de
  overflow; não existe enum com a lista provisória de atributos da V3.
- `AttributeState` separa o progresso atual do total histórico de ciclos.
- `AttributeProgressionService` delega cálculo de limite, overflow e regressão a
  `IAttributeOverflowHandler`, registrado por ID. Uma política rejeitada ou
  ausente não altera o estado.
- Efeitos de conclusão são apenas planejados pelo resultado e continuam sendo
  aplicados pelo motor transacional de efeitos.
- `AttributeDefinitionAsset` é somente authoring e converte os dados do Inspector
  em uma definição imutável.
- `InnerBoardDefinition` mantém posições próprias, separadas dos índices do
  tabuleiro externo, e configura a política de movimento por ID.
- Checkpoint, proteção e objetivo são propriedades de cada posição autorada; a
  localização de nenhuma delas é presumida pelo domínio.
- `InnerBoardProgressionService` valida a saída de
  `IInnerBoardMovementPolicy` antes de alterar `InnerBoardState`. A política
  futura decide avanço, regressão e limites.
- Captura e vitória usam políticas próprias (`IInnerBoardCapturePolicy` e
  `IInnerBoardVictoryPolicy`), também selecionadas por IDs da definição.
- Relocações de captura são validadas integralmente antes de qualquer mutação;
  uma saída inválida descarta a resolução completa.
- `GameSession` recebe opcionalmente definições de atributos e tabuleiro interno;
  ao adicionar um jogador, cria estados independentes para cada definição sem
  afetar sessões que usam apenas o tabuleiro externo.
- `GameProgressionService` restringe alterações à fase `CheckProgress`. A
  resolução interna roda movimento, captura e vitória sobre cópias; somente
  depois de todas passarem os estados são efetivados, persistidos e a fase muda
  para `EndTurn` ou `GameEnd`.
- `CharacterDefinition` mantém regras salariais, efeitos iniciais, habilidades,
  desvantagens, trilha e referências visuais como dados identificados. Não há
  enum de profissão ou habilidade concreta no domínio.
- `GameSession.AssignCharacter` associa uma definição existente ao jogador
  somente durante `Setup`; personagens não são forçados a ser únicos porque a
  política de sorteio/distribuição ainda não foi aprovada.
- A associação integra o memento versionado, o JSON local e os participantes
  da exportação de playtest.
- O conteúdo temporário do vertical slice inclui dois personagens genéricos,
  três atributos e um tabuleiro interno de seis posições. As políticas
  `carry`, movimento linear limitado, captura por espaço compartilhado e vitória
  no objetivo são selecionadas pelos IDs dos assets e não representam aprovação
  das regras finais.

## Fase 4 — fundação econômica

- `EconomyAccountDefinition` separa contas por `ownerId` e `resourceTypeId`; o
  tipo preserva o significado de Caixa, Reserva, Dívida, Banco, Cofre Público ou
  outro recurso. `unitId` identifica a unidade contábil, permitindo que contas
  semanticamente diferentes movimentem a mesma moeda sem misturar moeda e ativo.
- Limites mínimo/máximo são opcionais e configuráveis por conta.
- Toda `EconomyTransactionRequest` possui lançamentos que somam zero globalmente
  e também dentro de cada `unitId`, além de origem, operador, instante e descrição.
- `EconomyTransactionService` aplica a transação sobre uma cópia, rejeita o
  conjunto inteiro se qualquer conta violar limites e só então registra snapshots
  anterior/posterior em histórico append-only.
- `TaxRuleDefinition` mantém taxa, conta pagadora, destinos, proporções,
  isenções e política de arredondamento como dados.
- `TaxCalculator` gera lançamentos balanceados; a política registrada decide o
  arredondamento e o último destino recebe apenas o restante calculado.
- `EconomicResourceDefinition` descreve recursos como casas, carros, reservas,
  dívidas e investimentos sem criar tipos concretos no domínio: categoria,
  unidade, limite por proprietário, benefícios, efeitos anuais e apresentação
  permanecem dados. `IEconomicResourceTransactionPolicy` calcula a operação.
- `EconomicResourceService` valida propriedade da conta, tipo do recurso, limites
  e metadados de auditoria; somente então aplica a transação balanceada. O serviço
  de aplicação persiste apenas alterações aceitas.
- `InstitutionOperationDefinition` identifica categoria, proprietário institucional
  e política sem acoplar o domínio a uma modalidade concreta. Banco e Cofre Público
  usam o mesmo contrato, mas permanecem instituições distintas por `ownerId`.
- `InstitutionOperationService` delega empréstimo, pagamento, renegociação, auxílio
  ou outra operação à política registrada; valida participantes e preserva ID,
  solicitante, operador, instante e motivo antes de aplicar a transação no ledger.
  `GameInstitutionOperationService` persiste somente operações aceitas.
- `AnnualCycleDefinition` configura quantas voltas completas formam um ciclo e
  seleciona uma política por ID. `AnnualCycleService` resolve somente o próximo
  ciclo pendente, aplica uma única transação auditável e só então marca o ciclo
  processado; falhas deixam economia e estado anual inalterados.
- Cada jogador recebe `AnnualCycleState` quando a sessão possui definição anual.
  O total processado é independente do total de voltas e nunca pode ultrapassar
  os ciclos efetivamente habilitados pelo tabuleiro externo.
- Hospital é um módulo próprio em `MoneyRank.Domain.Hospital`. Admissão registra
  motivo e perdas de movimento; `IHospitalResolutionPolicy` decide recuperação,
  permanência e efeitos planejados sem embutir pagamento, dado ou custo.
- `HospitalService` rejeita políticas ausentes/falhas sem mutação e nunca oferece
  eliminação do jogador como resultado. Cada sessão configurada mantém um
  `HospitalState` independente por jogador.
- `GameAnnualCycleService` restringe o fechamento anual a `CheckProgress`, usa o
  jogador atual e persiste somente uma resolução aceita. `GameHospitalService`
  coordena internação, consumo explícito de perda de movimento e recuperação,
  persistindo apenas comandos que realmente alteram estado.

## Fase 5 — fundação de persistência e UX do mestre

- `GameSessionMemento` possui `schemaVersion` explícita (schema atual 7) e captura
  fase, jogador atual, evento ativo, resultado, estados externo/interno,
  atributos, ciclo anual, Hospital, valores de evento, saldos e históricos de
  evento, economia e ações.
- `GameSession.Restore` valida integralmente compatibilidade de sessão,
  jogadores, definições, posições, checkpoints e histórico antes de alterar o
  agregado.
- O mesmo memento serve como fronteira para save/load e como snapshot de undo;
  serialização em arquivo e política de retenção permanecem na infraestrutura.
- `GameUndoService` restaura um checkpoint, preserva o histórico de resoluções
  já ocorrido e acrescenta um `SessionUndoRecord` com snapshots antes/depois,
  operador e instante. Assim, desfazer não apaga a trilha de auditoria.
- `JsonGameSessionMementoSerializer` converte o memento para um DTO explícito;
  IDs, enum, datas ISO 8601, estados opcionais e histórico não dependem da
  serialização de propriedades imutáveis do domínio.
- `FileGameSessionSaveStore` grava primeiro em arquivo temporário e substitui o
  save apenas depois da escrita completa, preservando `.bak`. O nome físico usa
  SHA-256 do ID da sessão para impedir interpretação de caracteres de caminho.
- `FileBackedGameSessionRepository` mantém a sessão configurada em memória,
  salva a cada chamada dos serviços existentes e pode anexar uma nova instância
  com as mesmas definições restaurando o memento local.
- O `DevSandbox` usa esse repositório em `Application.persistentDataPath`, com
  autosave após comandos aceitos e autoload na próxima inicialização. Antes do
  restore, o bootstrap recria tabuleiro, regras e jogadores; o memento só repõe o
  estado dinâmico depois que a compatibilidade estrutural é validada.
- `PlaytestExportRequest` congela o memento, participantes, janela temporal e
  registros de undo. `FilePlaytestExporter` produz JSON detalhado com o snapshot
  completo e CSV de uma linha com métricas disponíveis, sem inferir métricas que
  ainda não são registradas. Nova exportação preserva backups dos dois arquivos.
- `AdministrativeEconomyAdjustmentService` exige uma política explícita de
  autorização e um motivo não vazio. Ajustes aceitos são transações balanceadas
  comuns, portanto preservam operador, horário, motivo e snapshots antes/depois
  no histórico econômico; tentativas negadas não alteram nem salvam a sessão.
- Os níveis concretos de acesso continuam fora do serviço:
  `IAdministrationAuthorizationPolicy` permitirá congelar Facilitador e
  Operador/aluno quando a V3 aprovar as permissões.
- `SessionActionHistory` é append-only e registra metadados estáveis de ações
  coordenadas pelos serviços: tipo, jogador, fase anterior/posterior, horário,
  operador, origem e parâmetros. Undo preserva os registros anteriores e inclui
  `action.undone`.
- O menu de nova partida coleta 2–4 jogadores e, antes de criar a sessão, recebe
  a atribuição feita pelo mestre após o sorteio físico entre seis perfis de
  personagem. A atribuição é salva pela sessão; perfis repetidos são permitidos
  no protótipo enquanto a decisão de exclusividade estiver pendente.
- O painel do mestre prioriza o jogador, personagem, casa e a ação do turno.
  Undo e exportação local ficam recolhidos em Ferramentas do mestre. Os
  checkpoints de UI permanecem em memória durante a execução atual, enquanto o
  registro da reversão e o estado restaurado são persistidos.
- `GameResult` guarda vencedor e código do motivo. Apenas uma avaliação de
  vitória aceita pode levar a `GameEnd`; `Results` exibe e restaura o resultado.
- O fluxo integrado possui testes de cena em EditMode e um teste PlayMode que
  carrega a cena, anima a peça, abre/confirma evento, progride e troca o jogador.

## Build e execução local

- `ProjectSettings/EditorBuildSettings.asset` usa o `DevSandbox` como primeira
  cena habilitada, removendo a cena de exemplo do fluxo de entrega.
- O alvo inicial validado é `StandaloneWindows64`; a saída local fica em
  `Builds/Windows/` e não é versionada.
- O player usa `Application.persistentDataPath`, portanto não depende de acesso
  de escrita ao diretório de instalação.

## Fase 2 — início do online e Android

- O projeto foi vinculado a um Unity Cloud Project e usa o Multiplayer Services
  SDK com sessões hospedadas pelo PC através do Relay. O nome exibido é salvo
  localmente e também enviado como propriedade pública da sessão; a autenticação
  é anônima e não usa o nickname como credencial.
- `OnlineSessionCoordinator` cria sessões no desktop e permite entrar por código.
  A lista de participantes vem dos eventos e estado atuais da sessão. A tela do
  mestre gera um QR local para o mesmo código; o cliente móvel solicita acesso à
  câmera apenas quando o usuário escolhe ler o QR.
- A build Android não pode criar ou continuar partidas locais como mestre; o
  coordenador também rejeita criação de sala em Android/iOS. O limite de sessão
  é cinco conexões (mestre + até quatro participantes).
- Esta etapa implementa infraestrutura e sala de espera. Sincronização da partida
  Money Rank (comandos e estado autoritativo do tabuleiro), botão do mestre para
  iniciar a partida sincronizada, reconexão após perda de rede e validação em
  dispositivos ainda são pendentes; não declarar multiplayer jogável antes
  dessas etapas.
- QR usa ZXing.Net para Unity (Apache 2.0), empacotado com o aviso de licença em
  `Assets/_MoneyRank/ThirdParty/ZXing/`. O QR contém somente o código da sessão.

