# Relatório de entrega — integração Money Rank V4 (25/11/2026)

> Este relatório informa **resultados reais** e **limitações**. Nada aqui afirma homologação do jogo, do equilíbrio de regras
> ou do hardware. Os valores da V4 são parâmetros de pré-teste.

## 1. O que foi entregue
Repositório de trabalho: `C:\Arquivos_Fabio\06_Projetos\01_Ativos\Money Rank Jogo`, branch `feature/v4-integration` (a partir de
`feature/board-visual-event-close` @ `777e366`; alterações locais não rastreadas do usuário foram preservadas). Nada foi enviado ao GitHub.

| Camada | O que existe | Onde |
|---|---|---|
| Conteúdo | CSV (fonte) carregado e validado em tempo de execução: 36 casas, 115 opções, 24 progressões, 6 profissões, 6 dificuldades, 9 eventos, moedas, 228 posições de rota; hash do conteúdo gravado nos saves | `Assets/_MoneyRank/Resources/V4/Data`, `Scripts/Domain/V4/V4Content.cs` |
| Motor de regras | C# puro, comandos atômicos (tudo-ou-nada), idempotência por `commandId`, ledger com `banco + cofre + carteiras = 28.260` verificado a cada comando, atributos fracionários exatos (decimal), Ludo real (4 peões, captura, checkpoints), 36 handlers, impostos/fiscalização, hospital, progressão, reserva, veículo/imóvel, parcerias, habilidades das 6 profissões, fechamento anual com cursor persistido, undo auditável | `Scripts/Domain/V4/V4Engine.*.cs`, `V4Rules.cs`, `V4State.cs` |
| Persistência | save atômico (tmp + replace + `.bak`) com recuperação do backup, arquivamento de saves incompatíveis, exportação analítica (CSV por tabela + snapshot JSON) | `Scripts/Infrastructure/V4/V4Persistence.cs` |
| Cofre/Arduino | protocolo `COFRE|seq|saldo` → `ACK|seq`/`ERR|seq`, sincronização idempotente (ACK perdido, reconexão, troca de porta), transporte serial Windows (P/Invoke), seleção de COM na UI, sketch | `V4Coffer.cs`, `WindowsSerialCofferTransport.cs`, `Docs/Hardware/cofre_display/cofre_display.ino` |
| Apresentação | cena `MoneyRankV4` (1ª no build): tabuleiro 3D com os 36 ícones oficiais, Ludo 15×15 gerado das rotas, capivaras (modelo CC0 já existente) com acessório por profissão, peões de xadrez nas 4 cores, hospital e cofre com display, cenário; HUD do mestre (setup, cartões com 6 barras fracionárias, dado, modal de decisão com prévia sem mutação, painel fiscal, histórico, desfazer/salvar/exportar, tela final) | `Scripts/PresentationV4/*`, `Assets/Editor/V4SceneBuilder.cs`, `V4RenderFix.cs` |

Decisão arquitetural (registrada em `PROGRESSO_V4.md`): o `GameSession` antigo é um framework genérico (3 atributos sandbox, trilha de 6 posições)
cuja semântica difere da V4; foi criado um motor paralelo com testes por cenário, sem apagar o legado. O código antigo permanece intacto.

Tamanho: ~2.540 linhas de domínio, ~350 de infraestrutura, ~1.740 de apresentação e ~1.530 de testes V4.

## 2. Comandos e resultados reais
```powershell
# Editor fechado, projeto na raiz do repositório
unity test . --mode EditMode --output Logs/full-results.xml --timeout 2400
unity build --target StandaloneWindows64 --output-path Builds/Windows/MoneyRank.exe --allow-dirty-build .
```
- **EditMode completo:** 268 testes — **263 passaram, 0 falharam, 5 ignorados** (os 5 são testes do fluxo legado do DevSandbox, desatualizados pelo
  commit `777e366` e deixados em quarentena com motivo explícito — pendência B10; 2 outros testes legados desatualizados foram corrigidos).
- **PlayMode V4 (`unity test . --mode PlayMode --filter V4SceneUiTests`):** 2/2 passando.
- **Testes V4:** 78 + 3 de simulação, todos passando (conteúdo, motor, economia, impostos, hospital, habilidades, save, export, protocolo do cofre).
- **Simulações de partidas completas** (`V4SimulationTests`, resultados em `05_validacao/resultado_simulacao*.csv`): 36 partidas aleatórias
  cobrindo B1/B2 × Fácil/Médio/Difícil, 2–4 jogadores, mix de profissões + 5 partidas de bots gananciosos. Em **todos os passos** valeram:
  identidade de caixa 28.260, nenhum saldo negativo, nenhum erro de comando, ida-e-volta do save sem perda. Menor saldo do banco observado: 24 UMR
  (B1 Difícil) — pressão de liquidez real a observar nos pré-testes.
- **Build Windows x64:** gerada a partir de `9ab7b44` (árvore com alterações não commitadas apenas em arquivos de log/config do Unity).
  Teste de fumaça: executável abre, permanece vivo por 25 s, `Player.log` com 34 linhas e **0 exceções** (uma versão anterior da build lançava exceção a
  cada quadro no pipeline URP — corrigido, ver §4).
  Hashes SHA-256: `MoneyRank.exe` `96B492CB271111251FE42B8646E65370A1B7B566773A1E35B34C3F2D1AE70873`; `globalgamemanagers`
  `93C070A4EC67A03278E0B4FD6DA0CECC1FD54D8EAB14C4F5819C1934307890D9`; `level0` `5EE33DBEF14FD65BF9CD617DEA088381E252B5DD56D3ECD887EF65F864EF62A5`;
  `resources.assets` `CC3DAEF67BDCCA05A07D0AD6BC0ADED1414FB7F3C98F5B08DB6C41BC114622E3`. Proveniência: `Builds/Windows/MoneyRank.provenance.json`.
- **Validação visual (Play mode no Editor):** setup → partida → dado → modal da casa → seleção com prévia → confirmação → próxima vez, com capturas
  (1280×720). Mostra carteira 440 após uma despesa de 60, banco e conferência de caixa atualizados, tokens animados nas casas, hospital e cofre.

## 3. Matriz de aceite (`05_validacao/cenarios_aceite.csv`)
Os 61 cenários agora têm status e evidência na própria planilha:
- **60 cobertos por testes automatizados do motor/serviços e passando** (SET, MOV, ANO, ATR, LUD, INV, FIS, HOS, CAS, PRO, HAB, ECO, SAVE, HW, NET, DATA).
- **UI01: parcial** — verificada apenas em 1280×720 por captura; 1024×768, tablet e 1920×1080 pendentes (o HUD agora usa escala `Expand`, que mantém toda a área 1920×1080 visível em qualquer proporção; em 4:3 os textos ficam pequenos — layout compacto pendente).
- **Ressalvas importantes:** HW01/HW02 foram testados com transporte simulado, **não** com o Arduino/display físico; NET01/NET02 foram testados no motor
  (autoridade, rejeição fora da vez, idempotência), **não** com dois clientes em rede real; o modo online (P2) **não foi implementado**.

## 4. Problemas encontrados e corrigidos no caminho
- Teste de fumaça da build: o pipeline URP quebrava a cada quadro (feature SSAO inativa tinha os recursos removidos do build) e `Shader.Find` retornava nulo no
  Player. Corrigido removendo a feature SSAO inativa e usando materiais como assets (`MoneyRank/V4/Fix Render Setup`).
- Disco C: cheio travava o Editor com um diálogo modal oculto — documentado em `PROGRESSO_V4.md` e na memória do projeto.
- `Application` resolvia para o namespace `MoneyRank.Application` nos testes; `System.IO.Ports`/`Registry` não existem no perfil .NET Standard 2.1 — usado P/Invoke (`kernel32`).

## 5. Limitações e pendências
1. **Hardware real não testado** (LCD I2C/OLED, porta COM, driver). O sketch assume LCD 16×2 I2C `0x27`: confirmar o módulo e o endereço antes de gravar.
2. **Online (P2) não iniciado.** O motor já é autoritativo (comandos por `playerId`, idempotência, undo bloqueado online), mas não há rede nem lobby V4.
3. **Ritmo do jogo — `DECISION_REQUIRED`** (`Docs/OPEN_DECISIONS.md`): bots gananciosos concluíram 1–2 dos 4 peões em 71–86 rodadas; extrapolação 150–250 rodadas.
   Nenhum número foi alterado; decidir após a bateria 1 com pessoas.
4. **Visual:** não foram baixados modelos 3D de terceiros (sem autorização para download); peões, hospital, cofre e prédios são geometria própria
   (placeholders, `THIRD_PARTY_ASSETS.md`). Falta polimento: ícone da casa nos cartões, animação do dado, painel de moedas, teste de resoluções/touch.
5. Undo não sobrevive a recarregar o aplicativo (pilha em memória).
6. PlayMode: 2 testes passando operam a cena V4 pelos próprios botões (iniciar, dado, selecionar, confirmar, voltar sem recusar, salvar/retomar no meio da decisão). Cobertura de UI ainda estreita (modais fiscais/hospital/habilidades só testados no motor).
7. 5 testes legados do DevSandbox em quarentena (B10 parcial).

## 6. Como executar
- Editor: abrir o projeto no Unity 6000.6.3f1, cena `Assets/_MoneyRank/Scenes/MoneyRankV4.unity`, Play. (Recriar a cena: menu `MoneyRank/V4/Create Scene`.)
- Build: `Builds/Windows/MoneyRank.exe`. Saves e exportações em `%USERPROFILE%\AppData\LocalLow\DefaultCompany\Money Rank Jogo\MoneyRankV4\{Saves,Exports}`.
- Roteiro humano de pré-testes: `Docs/V4/05_validacao/roteiro_pretestes.md` (bateria 1 e 2). Nada disso substitui as partidas físicas.
