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

## Decisões da Fase 0

- `GameSession` é o agregado raiz inicial.
- `GameFlowStateMachine` é a única fonte das transições permitidas.
- Falhas esperadas retornam `OperationResult` com código estável para futura UI.
- `SessionRules` recebe os limites de jogadores por configuração.
- Persistência concreta, eventos de domínio e snapshots entram em fases futuras.

## Limites

Não há, nesta fase, regras de movimento, economia, atributos, Hospital,
tributação, captura ou vitória.

## Fase 1 — núcleo do tabuleiro externo

- `BoardDefinition` contém uma lista ordenada de casas e não pressupõe 28 posições.
- `startIndex` é configurável para que o layout não determine a regra.
- `BoardMovementCalculator` calcula destino e voltas sem depender de animação.
- `ExternalBoardState` mantém posição e voltas por jogador.
- `GameSession.RegisterMove` valida a fase, atualiza apenas o estado do jogador
  atual e avança de `AwaitMove` para `ResolveSpace`.
- `GameSessionService.RegisterMove` persiste somente movimentos aceitos.
- `BoardDefinitionAsset` contém o subset temporário de 20 casas e quatro
  categorias, mantendo todo o conteúdo editável no Inspector.
- `BoardLayoutView` monta o perímetro de forma reproduzível a partir da definição.
- `PlayerPieceView` somente representa um `BoardMoveResult`; posição e voltas são
  atualizadas primeiro em `ExternalBoardState`.

