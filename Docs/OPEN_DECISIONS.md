# Money Rank — Decisões abertas

Itens desta lista não podem ser transformados em regra rígida sem aprovação.

1. Mapa e numeração final do tabuleiro externo.
2. Quantidade final de casas de imposto.
3. Atributos oficiais da V3.
4. Relação entre Saúde, Cortisol e Toxina.
5. Papel de Investimento como atributo ou conjunto de ativos.
6. Regra final de progressão e regressão.
7. Quantidade de peças por jogador no tabuleiro interno.
8. Localização dos pontos de salvamento.
9. Regra final de captura.
10. Regra final do Hospital.
11. Quantidade de voltas por ano.
12. Cálculo e arredondamento do Imposto de Renda.
13. Repasse ao Cofre Público.
14. Regra final de sonegação e auditoria.
15. Ordem padrão de pagamento entre Caixa, Reserva, Cofre, Empréstimo e Dívida.
16. Buffs e debuffs finais dos personagens.
17. Condição oficial de vitória e desempates.
18. Regras do dado de inflação.
19. Conjunto mínimo de cartas e eventos da V3.
20. Decisões que exigirão participação por celular.

## Registro de implementação

- A máquina de estados da Fase 0 implementa somente o fluxo-base aprovado.
- `AwaitChoice` é opcional entre `ResolveSpace` e `ApplyEffects`.
- A quantidade de jogadores é fornecida por `SessionRules`, não hardcoded no
  agregado.

