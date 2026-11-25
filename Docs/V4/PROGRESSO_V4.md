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
| 5 | Save atômico, export, undo, protocolo do cofre | **feito**; falta transporte serial Windows (P/Invoke) e sketch Arduino |
| 6 | Design/UI, visual, cena real | **primeira versão feita e validada em Play** (setup, tabuleiro, HUD, modais, fiscal/histórico, fim). Falta polimento (ver abaixo) |
| 7 | Baterias 1/2, cenários de aceite, relatório final | cenários automatizados no motor; relatório final pendente |

## Como validar a UI (Editor aberto + capturas)
1. `unity open .` e esperar `unity pipeline list` mostrar `Servidor acessível`.
2. `unity cmd menu --path "MoneyRank/V4/Create Scene"` (só se a cena não existir). Cena: `Assets/_MoneyRank/Scenes/MoneyRankV4.unity` (1ª no build).
3. `unity cmd editor_play`. Focar a janela do Editor antes de cada comando (PowerShell: ShowWindow+SetForegroundWindow), senão o main thread
   do Editor não responde ("Main thread operation timed out").
4. Clicar botões da UI por texto com `eval_file` (script em `Logs/shots/click.cs`) e capturar com `capture_game_view --save_path Logs/shots/x.png`
   (o arquivo cai em `Assets/Logs/shots/`; está no `.git/info/exclude`; apagar `Assets/Logs` ao fim).

## Pendências / próximos passos exatos
1. Transporte serial Windows `WindowsSerialCofferTransport : ICofferDisplayTransport` (P/Invoke kernel32: CreateFile/SetCommState/WriteFile/ReadFile em thread própria,
   115200 8N1, reconexão) + seleção de porta COM na UI + sketch `Docs/Hardware/cofre_display/cofre_display.ino` (LCD I2C ou OLED, parser `COFRE|seq|saldo`, responde `ACK|seq`/`ERR|seq`).
2. Corrigir B10: `BoardPresentationTests` (2) e `DevSandboxSceneTests` (5) — testes do sandbox antigo que falham no HEAD original.
3. Polimento visual: luz/sombras, UI em 1024x768/1280x720/1920x1080 sem clipping (testar), cards com ícone da casa, animação de dado, painel de moedas físico.
4. Relatório final `Docs/V4/RELATORIO_ENTREGA_V4.md`: arquivos alterados, comandos, resultados reais dos testes, limitações, hash da build, capturas.
5. Build Windows: `unity build --target StandaloneWindows64 --output-path Builds/Windows/MoneyRank.exe --allow-dirty-build .` (Editor fechado) e smoke test.
6. Modo online (P2): não iniciado; manter lobby existente sem prometer jogo online. O motor já é autoritativo (comandos com `playerId`, idempotência por `commandId`).
7. Cenários sem teste automatizado: UI01 (resoluções), HW real, NET01/02 com rede real.

## Registro de sessões
- 2026-11-25: pacote extraído, repositório real identificado, branch criada; Etapa 1 e motor V4 implementados; 28/28 testes V4 (conteúdo+núcleo) passando.
- 2026-11-25 (cont.): cena V4 criada e validada em Play (setup→partida→jogada→modal de decisão→confirmação); commits: docs, conteúdo, motor, undo/cofre/save, cena+HUD. 75/76→76/76 testes V4 (após correção de teste).
