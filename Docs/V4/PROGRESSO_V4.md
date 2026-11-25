# Progresso da integração V4 (arquivo de retomada)

> Este arquivo é a memória do trabalho autônomo. Quem retomar (inclusive uma sessão agendada) deve lê-lo primeiro,
> seguir `Docs/V4/01_documentacao/HANDOFF_CLAUDE_CODE.md` (contrato) e `Docs/V4/01_documentacao/Money_Rank_V4.md` (regras),
> e atualizar este arquivo a cada etapa concluída.

- Repositório de trabalho: `C:\Arquivos_Fabio\06_Projetos\01_Ativos\Money Rank Jogo` (NÃO o clone "… - Claude", que é o `main` antigo).
- Branch de trabalho: `feature/v4-integration` (criada a partir de `feature/board-visual-event-close` @ 777e366).
- Preservar arquivos locais não rastreados: `Assets/Resources/GDKEditionAutoGen*`, `Docs/Hardware/`, `MONEY_RANK_HANDOFF_AS_IS.md`.
- Commits pequenos e focados. Nunca apagar trabalho. Regras/valores vêm só do pacote V4 (CSV é a fonte; JSON é espelho).
- Testes (Editor fechado): `unity test . --mode EditMode --filter V4 --output Logs/v4-results.xml --timeout 1500` (≈3 min por rodada, compila tudo).
  Com Editor aberto: `unity cmd run_tests --mode EditMode --timeout 900 --result-only`. Parsear o XML em `Logs/`.
- Disco: manter C: com folga; um diálogo modal oculto (pouco espaço) trava o Editor.
- Sem downloads de assets 3D de terceiros sem autorização explícita: usar placeholders originais e registrar em `THIRD_PARTY_ASSETS.md`.
- Heartbeat da sessão ativa: `Docs/V4/.heartbeat` (ignorado pelo git). A tarefa agendada `money-rank-v4-retomar` roda de hora em hora e só assume se o heartbeat tiver >25 min.

## Decisão arquitetural (registrada)
O `GameSession` antigo é um framework genérico (3 atributos sandbox, trilha de 6 posições, saves schema 7) cuja semântica difere do V4
(cartas fixas, 6 atributos fracionários, Ludo real, ledger, fiscal). Em vez de remendar 700+ linhas e 187 testes, foi criado o motor
**`MoneyRank.Domain.V4`** (C# puro, sem UnityEngine) com comandos atômicos e testes por cenário de aceite. O conteúdo vem do CSV via
`V4ContentSet`. O código antigo permanece intacto (compatibilidade, testes antigos). A apresentação nova (cena real) consome o motor V4.
Reuso: asmdefs, padrão de save atômico, pacotes UGS/Netcode, exportação analítica, ScriptableObjects antigos só como legado.

## Etapas (HANDOFF §15)
| # | Etapa | Estado |
|---|---|---|
| 0 | Inventário, branch, pacote V4 em Docs/V4 | feito |
| 1 | Importar CSV/JSON, validar integridade, hash de conteúdo | **feito** (8 testes). Pendente: B10 (7 testes antigos desatualizados) |
| 2 | Contas/ledger, salário/rodada, 6 atributos e multiplicadores | **feito** no motor e ligado à cena V4 |
| 3 | Ludo real, 4 peões, captura/checkpoints | **feito** (motor + tabuleiro 3D gerado das rotas) |
| 4 | 36 casas, impostos, hospital, progressão, investimentos, habilidades, liquidez | **feito** no motor + modais na UI |
| 5 | Save atômico, export, undo, protocolo do cofre, serial | **feito** (serial P/Invoke + sketch; hardware real NÃO testado) |
| 6 | Design/UI, visual, cena real | **primeira versão feita e validada em Play** (setup, tabuleiro, HUD, modais, fiscal/histórico, fim). Falta polimento (ver abaixo) |
| 7 | Baterias 1/2, cenários de aceite, relatório final | **60/61 cenários automatizados passando**; relatório final em `RELATORIO_ENTREGA_V4.md`; baterias humanas pendentes |

## Como validar a UI (Editor aberto + capturas)
1. `unity open .` e esperar `unity pipeline list` mostrar `Servidor acessível`.
2. `unity cmd menu --path "MoneyRank/V4/Create Scene"` (só se a cena não existir). Cena: `Assets/_MoneyRank/Scenes/MoneyRankV4.unity` (1ª no build).
3. `unity cmd editor_play`. Focar a janela do Editor antes de cada comando (PowerShell: ShowWindow+SetForegroundWindow), senão o main thread
   do Editor não responde ("Main thread operation timed out").
4. Clicar botões da UI por texto com `eval_file` (script em `Logs/shots/click.cs`) e capturar com `capture_game_view --save_path Logs/shots/x.png`
   (o arquivo cai em `Assets/Logs/shots/`; está no `.git/info/exclude`; apagar `Assets/Logs` ao fim).

## Pendências / próximos passos exatos (após o relatório de entrega)
1. Testar o display físico real (confirmar módulo/endereço I2C, porta COM) e ajustar o sketch.
2. Polimento visual e de UI: ícone da casa nos cartões, animação do dado, painel de moedas, verificar 1024x768, 1280x720, 1920x1080, tablet/touch; modelos 3D licenciados (precisa autorização do usuário para baixar) ou manter placeholders.
3. Ampliar PlayMode da cena V4 (modais fiscal/hospital/habilidades, fim de partida, undo). Já existem 2 testes em `Tests/PlayMode/V4`.
4. Online autoritativo (P2): lobby V4, cliente envia intenção, host aplica comando, RNG autoritativo persistido; reutilizar `V4Engine`.
5. Reabrir os 5 testes legados em quarentena (DevSandbox) ou removê-los junto com o legado quando o usuário decidir aposentar o sandbox.
6. Decidir o ritmo do Ludo (`DECISION_REQUIRED` em OPEN_DECISIONS.md) após a bateria 1 humana; só então ajustar parâmetros (CSV/`parametros.json`) com versão.
7. Se tudo acima estiver feito: escrever CONCLUIDO em `Docs/V4/.heartbeat` e desabilitar a tarefa agendada `money-rank-v4-retomar`.

## Registro de sessões
- 2026-11-25: pacote extraído, repositório real identificado, branch criada; Etapa 1 e motor V4 implementados; 28/28 testes V4 (conteúdo+núcleo) passando.
- 2026-11-25 (cont.): cena V4 criada e validada em Play (setup→partida→jogada→modal de decisão→confirmação); commits: docs, conteúdo, motor, undo/cofre/save, cena+HUD. 75/76→76/76 testes V4 (após correção de teste).
- 2026-11-25 (fim da 1ª sessão longa): serial+sketch, simulações (36 aleatórias + 5 bots), quarentena B10, correção do pipeline URP no Player, build Windows com fumaça OK, matriz de aceite atualizada, relatório de entrega escrito.
