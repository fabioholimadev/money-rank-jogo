# Money Rank — Unity

Painel digital 3D para o jogo físico-digital Money Rank, voltado à educação
financeira e fiscal.

## Requisitos

- Unity `6000.6.3f1`
- Universal Render Pipeline
- Windows x86_64 como primeiro alvo
- Unity CLI com o pacote `com.unity.pipeline`

## Estado atual

O projeto concluiu a **Fase 0 — Foundation** e iniciou a **Fase 1 — Board
Vertical Slice**:

- domínio independente de `UnityEngine`;
- máquina de estados do fluxo-base;
- sessão configurável para jogadores e ordem de turnos;
- camada de aplicação e interface de persistência;
- definição configurável do tabuleiro externo;
- cálculo de movimento, wrap e voltas independente da apresentação;
- testes automatizados em EditMode.

Consulte `Docs/MONEY_RANK_UNITY_SPEC.md`, `Docs/ARCHITECTURE.md` e
`Docs/OPEN_DECISIONS.md` antes de mudanças estruturais.

## Validação pelo Unity CLI

```powershell
unity pipeline list
unity command recompile
unity command run_tests --mode editor --filter MoneyRank.Domain.EditModeTests --filter_type assembly --async_tests true
unity command test_status
```
