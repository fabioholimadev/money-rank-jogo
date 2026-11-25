# Progresso da integração V4 (arquivo de retomada)

> Este arquivo é a memória do trabalho autônomo. Quem retomar (inclusive uma sessão agendada) deve lê-lo primeiro,
> seguir `Docs/V4/01_documentacao/HANDOFF_CLAUDE_CODE.md` (contrato) e `Docs/V4/01_documentacao/Money_Rank_V4.md` (regras),
> e atualizar este arquivo a cada etapa concluída.

- Repositório de trabalho: `C:\Arquivos_Fabio\06_Projetos\01_Ativos\Money Rank Jogo` (NÃO o clone "… - Claude", que é o `main` antigo).
- Branch de trabalho: `feature/v4-integration` (criada a partir de `feature/board-visual-event-close` @ 777e366).
- Preservar arquivos locais não rastreados: `Assets/Resources/GDKEditionAutoGen*`, `Docs/Hardware/`, `MONEY_RANK_HANDOFF_AS_IS.md`.
- Commits pequenos e focados. Nunca apagar trabalho. Regras/valores vêm só do pacote V4 (CSV é a fonte; JSON é espelho).
- Testes: `unity cmd run_tests --mode EditMode` (Editor aberto) ou `unity test . --mode EditMode` (Editor fechado). Ver `unity list`.
- Disco: C: tinha pouco espaço; evitar gerar lixo (Library/Temp ficam no projeto).
- Sem downloads de assets 3D de terceiros sem autorização explícita: usar placeholders originais e registrar em `THIRD_PARTY_ASSETS.md`.

## Etapas (HANDOFF §15)
| # | Etapa | Estado |
|---|---|---|
| 0 | Inventário, branch, pacote V4 em Docs/V4 | feito |
| 1 | Congelar schema/IDs, importar CSV/JSON, validar integridade, corrigir B01/B02 | pendente |
| 2 | Contas, salário/rodada, seis atributos e multiplicadores; bootstrap real | pendente |
| 3 | Rota interna real (Ludo), quatro peões, captura/checkpoints | pendente |
| 4 | Handlers das 36 casas, impostos, hospital, progressão, investimentos, habilidades, liquidez | pendente |
| 5 | Save/restore/undo, exports e serial (Arduino) | pendente |
| 6 | Design/UI/visual assets, testes em tela | pendente |
| 7 | Baterias 1/2, cenários de aceite (`Docs/V4/05_validacao/cenarios_aceite.csv`), relatório final | pendente |

## Registro de sessões
- 2026-11-25: pacote extraído, repositório real identificado, branch criada.
