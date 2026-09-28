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
- A integração com `GameSession` ocorrerá junto ao comando `RegisterMove`, após a
  definição do conteúdo temporário do vertical slice.

