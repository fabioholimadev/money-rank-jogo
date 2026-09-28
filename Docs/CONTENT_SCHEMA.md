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

