# Money Rank — Regras aprovadas

Este arquivo contém somente regras consideradas estáveis o suficiente para
orientar a implementação. Regras provisórias permanecem em
`OPEN_DECISIONS.md`.

## Produto

- O jogo é uma experiência educacional físico-digital de educação financeira e
  fiscal.
- O tabuleiro físico é a experiência principal.
- O aplicativo Unity funciona como painel, árbitro, visualização e registro para
  o mestre/facilitador.
- O MVP funciona offline e não exige celular, conta, nuvem ou multiplayer online.

## Sessão e turno

- Uma sessão aceita de 2 a 4 jogadores; os limites são configuração do ruleset.
- A ordem de turnos é explícita e determinística.
- A interface só pode oferecer comandos legais para a fase atual.
- O fluxo-base é `Setup -> StartTurn -> AwaitMove -> ResolveSpace ->`
  `AwaitChoice? -> ApplyEffects -> CheckProgress -> EndTurn`.
- Ao concluir `EndTurn`, o próximo jogador passa a ser o jogador atual.

## Implementação

- Regras de domínio não dependem de `UnityEngine`.
- Atributos, casas, personagens, eventos e balanceamento são data-driven.
- Animações representam um resultado já aplicado no domínio.
- IDs internos são estáveis e não dependem de textos de interface.

