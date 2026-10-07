# Money Rank — Unity

Painel digital 3D para o jogo físico-digital Money Rank, voltado à educação
financeira e fiscal.

## Requisitos

- Unity `6000.6.3f1`
- Universal Render Pipeline
- Windows x86_64 como primeiro alvo
- Unity CLI com o pacote `com.unity.pipeline`

## Estado atual

O projeto concluiu a **Fase 0 — Foundation** e a base funcional da **Fase 1 —
Board Vertical Slice**:

- domínio independente de `UnityEngine`;
- máquina de estados do fluxo-base;
- sessão configurável para jogadores e ordem de turnos;
- comandos explícitos e persistidos para início e encerramento de turno;
- camada de aplicação e interface de persistência;
- definição configurável do tabuleiro externo;
- cálculo de movimento, wrap e voltas independente da apresentação;
- câmera do sandbox com estados de visão geral, acompanhamento e foco;
- sandbox de dois jogadores integrado ao ciclo de sessão, movimento e turnos;
- configuração transacional de novas sessões locais para 2–4 jogadores;
- testes automatizados em EditMode e PlayMode.

A **Fase 2 — Event Engine** possui definições e assets versionados, catálogo e
tabelas validados, requisitos e efeitos extensíveis, preview projetado sem
mutação, confirmação transacional, seleção ativa persistida, histórico auditável
e painel reutilizável com opções indisponíveis explicadas e cancelamento de
preview.
A **Fase 3 — Progression** possui atributos, tabuleiro interno, checkpoints,
captura e vitória configuráveis por políticas, com resolução atômica integrada à
  sessão. A fundação da **Fase 4 — Economy** já inclui contas separadas, transações
  balanceadas por unidade contábil, recursos econômicos e operações institucionais
  data-driven, cálculo tributário, ciclo anual configurável e Hospital resolvido
  por políticas. A
  **Fase 5 — Persistence / Master UX**
começou com mementos versionados, log central de ações, resultado coerente,
undo auditável e saves JSON locais gravados de forma atômica com backup da
versão anterior, além de exportação de playtest em JSON detalhado e CSV resumido.
O painel do sandbox permite consultar ações recentes, desfazer a última ação
reversível e exportar o playtest.

O conjunto atual possui 187 testes EditMode e 1 teste PlayMode. A integração do
conteúdo V3, decisões residuais de gameplay e a UX final ainda não estão
concluídas.

Consulte `AGENTS.md`, `Docs/MONEY_RANK_UNITY_SPEC.md`, `Docs/GAME_RULES.md`,
`Docs/OPEN_DECISIONS.md`, `Docs/ARCHITECTURE.md` e
`Docs/DEVELOPMENT_HANDOFF.md` antes de mudanças estruturais. O handoff contém o
estado entregue, a diferença entre o sandbox e as regras V3, o plano priorizado
e o guia completo para preparar outra máquina.

## Validação pelo Unity CLI

```powershell
unity status
unity commands --format json
unity --format json test . --mode EditMode --output Logs/editmode-results.xml --timeout 300
unity --format json test . --mode PlayMode --output Logs/playmode-results.xml --timeout 300
```

Quando o Editor estiver aberto com o servidor do pacote Pipeline ativo, use
`unity pipeline list` para descobrir a instância antes dos comandos remotos.

## Build local

O primeiro alvo configurado é Windows x86_64 e a cena inicial é
`Assets/_MoneyRank/Scenes/DevSandbox.unity`.

```powershell
unity build --target StandaloneWindows64 --output-path Builds/Windows/MoneyRank.exe --allow-dirty-build .
```

O build validado localmente é gerado em `Builds/Windows/` (diretório ignorado
pelo Git). Saves do player ficam no diretório gravável retornado por
`Application.persistentDataPath`.
