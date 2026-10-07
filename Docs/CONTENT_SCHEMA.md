# Money Rank — Schema de conteúdo

Este documento será expandido junto ao vertical slice.

## Princípios

- IDs são estáveis e independentes do texto exibido.
- Definições são imutáveis durante uma sessão.
- Estado de jogador nunca é salvo em ScriptableObject de definição.
- Atributos, tabuleiros, eventos e personagens permanecem configuráveis.

## Schemas previstos

- `BoardDefinition` e `BoardSpaceDefinition`.
- `InnerBoardDefinition`.
- `AttributeDefinition`.
- `CharacterDefinition`.
- `EventDefinition`, opções, requisitos e efeitos.

## Eventos — fundação da Fase 2

`EventDefinition` é imutável durante a sessão e contém:

- `id`, `version`, `categoryId`, `title` e `educationalText` obrigatórios;
- tags, condições e opções copiadas na construção;
- IDs opcionais de duração e apresentação;
- pelo menos uma opção, com IDs únicos dentro do evento.

`EventOptionDefinition` contém texto, `visibilityId`, requisitos e efeitos.

Como atributos, recursos e operações finais ainda não foram congelados,
`RequirementDefinition` usa `typeId` e parâmetros chave/valor, enquanto
`GameEffectDefinition` usa IDs de alvo, operação, duração e origem. O valor do
efeito permanece textual e será interpretado pelo handler registrado para a
operação em uma etapa posterior. Nenhum enum de atributo ou regra econômica é
fixado por esse schema.

Handlers de requisito são registrados por `typeId` e recebem um contexto
imutável de valores identificados por chave. O preview mantém resultados de
condições e requisitos separados, lista os efeitos planejados e informa se a
opção pode ser confirmada, sem alterar o estado da sessão. Os efeitos também são
executados sobre uma cópia para produzir o snapshot projetado que a interface
compara com o estado anterior; falhas de handler bloqueiam a confirmação e
fornecem um diagnóstico estável.

Na confirmação, handlers de efeito são registrados por `operationId`. A
aplicação é transacional: todos os efeitos executam sobre uma cópia e qualquer
falha descarta o conjunto. O registro preserva IDs do evento, versão, jogador,
casa, escolha, origem, operador, instante e snapshots anterior/posterior.
O snapshot usado no preview também é preservado; se o estado mudar antes da
confirmação, a operação é rejeitada e um novo preview deve ser gerado.

O conteúdo autorado no Unity é organizado por três tipos de asset:

- `EventDefinitionAsset`: texto, condições, opções, requisitos e efeitos de uma
  versão de evento;
- `EventTableDefinitionAsset`: lista ordenada de referências `eventId@version`;
- `EventContentCatalogAsset`: conjunto de eventos e tabelas convertido para um
  catálogo imutável no início da sessão.

O catálogo rejeita IDs/versões duplicados, tabelas duplicadas e referências a
versões inexistentes. A ordem da tabela é preservada como dado, mas o método de
seleção ainda é `DECISION_REQUIRED`: nenhum sorteio, peso ou descarte é inferido
pelo motor nesta fase.

Quando a seleção for solicitada, `EventSelectionService` recebe explicitamente o
ID da tabela, o ID da política e um contexto com sessão, jogador, casa, índice de
seleção e parâmetros livres. A política deve devolver uma referência exata que
já pertença à tabela; o serviço rejeita qualquer referência externa. Assim, uma
política manual, sequencial ou aleatória pode ser adicionada depois sem alterar
o catálogo ou o fluxo de confirmação.

O conjunto técnico inicial de handlers reconhece `state.equals`, `state.exists`,
`state.set` e `state.remove`. Esses IDs operam apenas sobre o estado genérico do
evento; efeitos econômicos, atributos oficiais e regras V3 continuam exigindo
handlers próprios e configuração aprovada.

O vertical slice autora oito eventos genéricos, duas versões de teste para cada
uma das quatro categorias, e quatro tabelas. Esse catálogo é conteúdo de
sandbox: serve para validar o fluxo e não substitui o conjunto de cartas V3.
Quando uma opção é aberta, a sessão persiste `ActiveEventSelection` com jogador,
casa, tabela, política, evento, versão e índice. O save em `AwaitChoice` só é
aceito quando essa referência coincide com o estado do tabuleiro.

## Atributos — fundação da Fase 3

`AttributeDefinitionAsset` autoriza no Inspector os campos `id`, `displayName`,
`typeId`, `threshold`, `overflowBehaviorId`, efeitos de conclusão, ícone e
apresentação. Tanto o tipo quanto a política são IDs livres, evitando congelar a
lista de atributos ou uma regra de overflow antes da V3.

O estado guarda `currentProgress` e `completedCycles` separadamente. A política
registrada calcula o novo progresso e quantos ciclos foram concluídos; o serviço
repete os efeitos configurados por ciclo, mas não os aplica diretamente.

`InnerBoardDefinitionAsset` mantém uma lista independente de posições internas.
Cada posição possui `typeId` e flags configuráveis de checkpoint, proteção e
objetivo. A definição também aponta para `movementPolicyId`; assim, a regra de
avanço/regressão e a localização dos pontos de salvamento continuam
`DECISION_REQUIRED` e podem ser substituídas sem alterar o estado ou a UI.

Os campos `capturePolicyId` e `victoryPolicyId` selecionam estratégias separadas
para colisões e encerramento. O motor apenas valida e aplica o resultado dessas
estratégias; regras finais de captura, término e desempate continuam pendentes.

## Personagens — schema genérico

`CharacterDefinitionAsset` autora ID, nome, descrição, regras salariais,
recursos e atributos iniciais, habilidades passivas, desvantagens, trilha de
progressão, prefab visual e retrato. Regras usam `typeId`, parâmetros e efeitos
genéricos; portanto os seis perfis discutidos continuam sendo conteúdo
provisório, não classes ou enums embutidos no código.

A sessão recebe um catálogo imutável de personagens e guarda apenas o
`characterId` associado a cada jogador. A escolha é permitida durante `Setup` e
é persistida no save e na exportação de playtest. Sorteio, exclusividade e
aplicação dos efeitos iniciais permanecem `DECISION_REQUIRED`.

## Economia — fundação da Fase 4

`EconomyDefinitionAsset` autora contas e saldos iniciais. Cada conta possui ID,
nome, proprietário, tipo de recurso, unidade contábil e limites opcionais. O
`resourceTypeId` descreve o papel semântico da conta; o `unitId` define o que pode
ser somado e deve fechar em zero na mesma transação. Assim, Caixa e Cofre Público
podem usar a unidade `cash`, enquanto unidades de casa ou investimento permanecem
separadas. Valores decimais são
serializados como texto em formato invariável para evitar perda de precisão no
Inspector. A conversão produz `EconomyDefinition`, que cria o estado inicial e
o serviço transacional usando exatamente o mesmo conjunto de contas.

Transferências são lançamentos balanceados entre contas identificadas. Banco e
Cofre Público são proprietários distintos; qualquer repasse permanece explícito
e auditável. Ordem de pagamento, taxas, salário, tributos e limites oficiais
continuam configuráveis e `DECISION_REQUIRED`.

`EconomicResourceDefinitionAsset` autora recursos econômicos genéricos com ID,
nome, categoria, `unitId`, política de transação, limite opcional por proprietário,
benefícios, efeitos anuais e referência visual. Aquisição, venda, criação ou baixa
é sempre calculada por uma política registrada e aplicada como transação auditável.
Os preços, rendimentos, custos, limites e regras específicas de casas, carros,
reserva, dívida e investimentos continuam sendo conteúdo `DECISION_REQUIRED`.

`InstitutionOperationDefinitionAsset` autora uma operação de Banco ou Cofre
Público com ID, nome, categoria, `institutionOwnerId` e `policyId`. A política
recebe o jogador solicitante, parâmetros livres e o snapshot econômico, então
produz uma transação completa. O motor exige que a transação envolva tanto o
proprietário institucional configurado quanto o jogador solicitante e rejeita
alterações nos metadados de auditoria. Empréstimo pode balancear simultaneamente
as unidades `cash` e `debt`; auxílio público aponta para o proprietário do Cofre,
nunca para uma conta bancária implícita. Taxas, prazos, garantias, elegibilidade e
regras de renegociação continuam `DECISION_REQUIRED`.

`TaxRuleDefinitionAsset` serializa taxa e proporções como decimais invariáveis,
além de conta de origem, destinos, isenções e `roundingPolicyId`. Nenhuma taxa ou
modalidade de arredondamento é embutida no motor.

`AnnualCycleDefinitionAsset` contém `id`, `completedLapsPerCycle` e `policyId`.
A quantidade provisória de voltas não aparece como constante: a política recebe
o jogador, número sequencial do ciclo, snapshot econômico, ID da transação,
instante e operador, e deve devolver uma transação balanceada. Cada chamada
processa no máximo um ciclo para preservar auditoria e permitir retomada segura.

`HospitalDefinitionAsset` contém somente o ID do módulo e o
`resolutionPolicyId`. A internação guarda `reasonId` e a quantidade pendente de
movimentos perdidos; comandos de recuperação possuem ID e parâmetros livres. A
política registrada devolve permanência/saída, perdas ainda pendentes e efeitos
planejados. Pagamento, rolagem, custo e consumo do movimento permanecem
`DECISION_REQUIRED`, não valores fixos no domínio.

## Estado persistido e auditoria

O schema atual do memento é `7` e mantém compatibilidade de leitura desde o
schema `4`. Além dos estados dos módulos, ele inclui:

- seleção ativa de evento durante `AwaitChoice`;
- resultado final com vencedor e código de motivo;
- histórico central append-only de ações;
- históricos detalhados de resoluções de evento e transações econômicas.

O histórico de ações guarda `id`, `typeId`, jogador opcional, fase anterior e
posterior, instante, operador, origem e parâmetros únicos. Ele registra fatos
coordenados pela aplicação sem transformar políticas temporárias em regras do
domínio.

