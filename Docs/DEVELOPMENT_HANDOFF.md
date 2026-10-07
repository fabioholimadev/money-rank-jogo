# Money Rank — Entrega técnica e plano de trabalho

Atualizado em 7 de outubro de 2026. Este documento é o ponto de entrada para
outro desenvolvedor ou outra sessão do Codex continuar o projeto com segurança.

## 1. Ordem obrigatória de leitura

1. `AGENTS.md`
2. `Docs/MONEY_RANK_UNITY_SPEC.md`
3. `Docs/GAME_RULES.md`
4. `Docs/OPEN_DECISIONS.md`
5. `Docs/ARCHITECTURE.md`
6. este documento

Regra central: não inventar gameplay. Quando faltar uma decisão, manter o
comportamento configurável, registrar `DECISION_REQUIRED` e atualizar
`OPEN_DECISIONS.md`.

## 2. Estado entregue

### Plataforma

- Unity `6000.6.3f1`.
- Universal Render Pipeline.
- Windows x86_64 como primeiro alvo.
- Modo local e offline-first.
- Cena inicial: `Assets/_MoneyRank/Scenes/DevSandbox.unity`.

### Fundação funcional

- Arquitetura em camadas `Presentation -> Application -> Domain`, com
  `Infrastructure` e `Content` como adapters.
- `Domain` e `Application` independentes de `UnityEngine`.
- Sessão, máquina de estados, turnos, movimento, wrap e voltas.
- Tabuleiro externo visual temporário e câmera com visão geral, acompanhamento
  e foco.
- Configuração transacional de sessões locais para 2–4 jogadores; o sandbox
  visual ainda inicia dois jogadores fixos.
- Event Engine data-driven com versões, catálogo, tabelas, requisitos, efeitos,
  preview sem mutação, confirmação transacional e histórico auditável.
- Progressão com atributos, tabuleiro interno, salvamentos, proteção, captura e
  vitória definidos por políticas.
- Economia com contas separadas, ledger balanceado, recursos, operações de
  Banco/Cofre, tributos, ciclo anual, Hospital e ajuste administrativo.
- Save local JSON atômico com backup, schema atual 7 e leitura a partir do 4.
- Histórico central de ações, undo auditável, autosave/autoload e exportação de
  playtest em JSON e CSV.
- Painel de desenvolvimento com estado, movimento, evento, progressão,
  histórico, undo e exportação.
- 187 testes EditMode e 1 teste PlayMode na última validação completa.
- Build Windows validado localmente em `Builds/Windows/MoneyRank.exe`.

### Conteúdo temporário que precisa ser substituído

| Área | Sandbox atual | Regra V3 |
|---|---:|---:|
| Tabuleiro externo | 20 casas | 28 posições: 4 cantos + 24 eventos |
| Categorias | 4 | 8, com 3 casas por categoria |
| Eventos de exemplo | 8 | mínimo de 24 exemplos finais |
| Atributos | 3 genéricos | 6 oficiais |
| Personagens | 2 genéricos | 6 oficiais |
| Caminho interno | 6 posições | 8 casas até o centro |

O sandbox comprova a arquitetura; ele não é ainda o jogo V3 completo.

## 3. Problema funcional conhecido — prioridade máxima

Depois de confirmar uma opção de evento, o painel continua aberto em estado de
confirmação e o próximo comando fica no painel de desenvolvimento. Em algumas
resoluções parece que a partida travou e as opções podem continuar visíveis.

Correção esperada:

1. tornar explícitos os estados `Selecting`, `Previewing` e `Confirmed` do modal;
2. desabilitar opções após a confirmação;
3. mostrar um botão `Continuar` no estado confirmado;
4. fechar o modal e apresentar a única ação legal da fase seguinte;
5. criar uma barra de ação primária fixa, independente de rolagem;
6. cobrir o fluxo completo com PlayMode tests em 1366×768 e 1920×1080.

Não contornar o problema pulando fases na apresentação. A máquina de estados
continua sendo a autoridade.

## 4. Plano recomendado

### P0 — Destravar e tornar o turno legível

- Corrigir o fluxo pós-evento descrito acima.
- Exibir fase, jogador atual e uma única ação primária sempre visível.
- Integrar o dado digital d6 e registrar origem/resultado.
- Manter entrada manual apenas como futuro adapter físico-digital/dev.
- Adicionar teste PlayMode: iniciar turno -> lançar dado -> mover -> abrir evento
  -> escolher -> confirmar -> continuar -> concluir progressão -> próximo jogador.

Critério de pronto: o usuário conclui vários turnos sem recorrer ao painel de
desenvolvimento nem ficar preso em modal.

### P1 — Menu, configuração e navegação

- Fluxo `Bootstrap -> MainMenu -> Setup -> Game -> Results`.
- Menu com `Iniciar partida digital`, `Continuar`, `Configurações` e botão
  `Online — em breve` desabilitado.
- Setup de 2–4 jogadores: nomes, cores, personagem, ordem, dificuldade,
  facilitador, modo de playtest e resumo antes de iniciar.
- Resolver em `OPEN_DECISIONS.md` repetição e forma de seleção dos personagens.
- Configurações: áudio, tela, escala da UI, alto contraste, redução de animações,
  autosave e informação do diretório de dados.
- Usar UI Toolkit para telas e HUD; manter IMGUI somente como ferramenta dev.

Critério de pronto: uma sessão pode ser configurada e iniciada sem dados
hardcoded no `DevSandbox`.

### P2 — Migrar conteúdo e regras V3

- Autorizar o tabuleiro externo de 28 posições e oito categorias.
- Criar os seis atributos e seus gatilhos 3/6, incluindo fila de consequência
  pendente com no máximo uma resolução por turno.
- Autorizar o tabuleiro interno de oito casas sem fixar os dois salvamentos até
  a decisão de layout.
- Criar os seis personagens em assets data-driven.
- Integrar salário e periodicidade por dificuldade antes do movimento.
- Integrar IR, arredondamento para cima, Cofre, sonegação e auditoria.
- Implementar os três caminhos do Hospital e deixar eliminação no Difícil
  desativada/configurável até validação.
- Implementar Virada e Consequência.
- Implementar final de rodada e desempates da V3.
- Expandir o save para os novos estados e criar migração de schema.

Critério de pronto: regras fechadas em `GAME_RULES.md` possuem testes de domínio
e execução integrada; decisões abertas não aparecem hardcoded.

### P3 — Conteúdo, visual e acessibilidade

- Substituir os eventos temporários por três cartas validadas para cada uma das
  oito categorias.
- Montar tabuleiro 3D central com HUD fixo, ação de fase na região inferior e
  modal de evento sobre fundo escurecido.
- Mostrar informação por ícone e texto, nunca apenas por cor.
- Validar 1366×768 e 1920×1080.
- Integrar modelos de personagem. Preferir o GLB original entre os formatos já
  avaliados; confirmar rig Humanoid, animações, materiais PBR e licença. Para
  produção, uma malha base com variantes, texturas 1K/2K e LODs evita seis
  modelos pesados duplicados.
- Adicionar logo, paleta, fonte, oito ícones de categoria, materiais, fundo e
  áudio quando os assets finais estiverem disponíveis.

### P4 — Qualidade e entrega

- Testes de regressão para cada regra V3 e migração de save.
- PlayMode tests dos caminhos principais e dos modais.
- Playtests com exportação JSON/CSV e revisão de métricas.
- Build Windows limpo, Console sem exceções e checklist de acessibilidade.
- Só depois avaliar modo físico-digital, celulares ou online.

## 5. Preparação de uma nova máquina Windows

### Instalar

1. Git for Windows.
2. Unity Hub.
3. Unity Editor `6000.6.3f1` com o módulo `Windows Build Support`.
4. Codex, se ele for usado no desenvolvimento.
5. Unity CLI/Pipeline usado pelo projeto. Não presumir comandos: depois de
   instalar, descobrir as capacidades com os comandos da seção 7.

### Clonar

```powershell
git clone https://github.com/fabioholimadev/money-rank-jogo.git
Set-Location money-rank-jogo
git status
```

No Unity Hub, use `Add project from disk` e selecione a pasta clonada. Confirme
que o Hub abrirá com `6000.6.3f1`; não atualize a versão do projeto durante o
primeiro acesso. Aguarde a importação e a restauração dos pacotes.

### Primeira abertura

1. Verifique que não há erros vermelhos no Console.
2. Abra `Assets/_MoneyRank/Scenes/DevSandbox.unity`.
3. Pressione Play.
4. Use o painel do sandbox para executar o vertical slice.
5. Lembre que existe o problema pós-evento registrado na seção 3.

`Library`, `Temp`, `Logs`, `Builds`, `UserSettings`, soluções e projetos de IDE
são locais e não devem ser enviados ao Git.

## 6. Trabalho com Git

Antes de começar:

```powershell
git switch main
git pull --ff-only
git switch -c feature/nome-curto
```

Antes de entregar:

```powershell
git status
git diff --check
git diff --stat
```

Fazer commits pequenos por assunto. Não editar YAML de cenas/prefabs à mão
quando o Unity Editor puder fazer a alteração com segurança. Nunca versionar
builds, logs, saves pessoais ou arquivos gerados.

## 7. Unity CLI, testes e build

Descobrir primeiro o CLI instalado:

```powershell
unity status
unity command
unity list
unity commands --format json
```

Setup típico do Pipeline, somente após confirmar os comandos disponíveis:

```powershell
unity auth login
unity pipeline install
unity pipeline list
```

Executar testes:

```powershell
unity --format json test . --mode EditMode --output Logs/editmode-results.xml --timeout 300
unity --format json test . --mode PlayMode --output Logs/playmode-results.xml --timeout 300
```

Gerar build local:

```powershell
unity build --target StandaloneWindows64 --output-path Builds/Windows/MoneyRank.exe --allow-dirty-build .
```

O build e os logs são ignorados pelo Git. Saves e exports usam o diretório
gravável informado por `Application.persistentDataPath`.

## 8. Como iniciar uma sessão do Codex

Prompt recomendado:

> Leia integralmente `AGENTS.md`, `Docs/MONEY_RANK_UNITY_SPEC.md`,
> `Docs/GAME_RULES.md`, `Docs/OPEN_DECISIONS.md`, `Docs/ARCHITECTURE.md` e
> `Docs/DEVELOPMENT_HANDOFF.md`. Não invente regras. Escolha uma única prioridade
> do plano, preserve a separação Domain/Application/Presentation, implemente a
> mudança mínima com testes, compile e valide a cena afetada. Registre qualquer
> ambiguidade como `DECISION_REQUIRED` antes de continuar.

Ao terminar uma tarefa, o relatório deve listar arquivos alterados, testes,
build, pendências e qualquer decisão de gameplay ainda necessária.
