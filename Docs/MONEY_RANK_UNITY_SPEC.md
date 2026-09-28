# MONEY RANK — UNITY IMPLEMENTATION SPECIFICATION

**Arquivo canônico para Codex e equipe de desenvolvimento**  
**Versão:** 0.1 — 28/09/2026  
**Status:** especificação técnica de implementação / pré-V3  
**Engine-alvo:** Unity 6+  
**Render Pipeline:** URP (Universal Render Pipeline)  
**Plataforma inicial:** Windows (notebook/desktop escolar)  
**Modo principal:** jogo de tabuleiro físico-digital, operado por um mestre/facilitador  

---

## 0. COMO O CODEX DEVE USAR ESTE DOCUMENTO

Este documento é a especificação de contexto do projeto **Money Rank** para desenvolvimento no Unity. Ele deve ser lido antes de qualquer alteração estrutural no projeto.

### 0.1 Regras de precedência

Quando houver conflito entre fontes, seguir esta ordem:

1. **Regras V3 aprovadas pela equipe**, quando existirem e forem explicitamente marcadas como aprovadas.
2. **Este SPEC**, para arquitetura, organização do código, fluxo digital e limites de implementação.
3. **Money Rank — Documentação V2**, para regras de jogo ainda não substituídas.
4. **Diagnóstico de Integração Físico-Digital MoneyRank**, para comportamento esperado do painel digital, persistência, fluxo do mestre e critérios do MVP.
5. Documentos de inscrição, pesquisa e comunicação do projeto, para objetivo pedagógico e identidade do produto.
6. Ideias, rascunhos, protótipos e mensagens anteriores não formalizadas.

### 0.2 Regra fundamental para ambiguidades

O Codex **não deve inventar regras de gameplay para fechar lacunas**.

Se uma regra estiver indefinida, contraditória ou marcada como provisória:

- criar abstração/configuração que permita alterá-la depois;
- adicionar comentário `DECISION_REQUIRED` ou registrar em `Docs/OPEN_DECISIONS.md`;
- evitar hardcode;
- implementar apenas o comportamento mínimo seguro para testar a arquitetura;
- informar a equipe no resumo da tarefa.

### 0.3 O que o Codex pode decidir sozinho

O Codex pode decidir detalhes técnicos que não alterem as regras do jogo, por exemplo:

- nomes internos de classes e namespaces;
- composição de serviços e interfaces;
- divisão de assemblies;
- padrões de teste;
- cache e pooling;
- organização de prefabs;
- implementação de animações de transição;
- refatorações que preservem comportamento;
- ergonomia técnica de ferramentas de Editor.

Não pode decidir sozinho:

- valores de economia;
- atributos oficiais;
- quantidade final de casas;
- efeitos de cartas;
- regras de vitória/desempate;
- buffs/debuffs definitivos de personagens;
- regras fiscais;
- critérios pedagógicos;
- mudanças de escopo do produto.

---

# 1. VISÃO GERAL DO MONEY RANK

Money Rank é um projeto educacional de **Educação Financeira e Educação Fiscal**, desenvolvido como uma experiência integrada entre:

1. ações pedagógicas presenciais;
2. plataforma web gamificada;
3. jogo de tabuleiro físico;
4. sistema digital de apoio ao tabuleiro;
5. fabricação digital (MDF, impressão 3D, eletrônica, LEDs e áudio).

O projeto foi concebido para aproximar conceitos financeiros e fiscais de decisões reais de jovens, conectando orçamento, consumo, saúde, crédito, dívida, tributos, serviços públicos, transparência e cidadania.

O jogo **não deve ensinar que “vencer financeiramente” significa apenas acumular dinheiro**. O sistema deve valorizar decisões equilibradas, conhecimento, participação cidadã, saúde/qualidade de vida e sustentabilidade financeira.

---

# 2. CONTEXTO PEDAGÓGICO

## 2.1 Público inicial

- estudantes do Ensino Médio Técnico;
- contexto escolar;
- uso mediado por professor, facilitador ou aluno designado como mestre;
- sessões presenciais e coletivas.

## 2.2 Metodologia do projeto

O Money Rank nasce de uma pesquisa-ação com diagnóstico e intervenções pedagógicas. Entre os temas já trabalhados estão:

- educação financeira;
- educação fiscal;
- função social dos tributos;
- consumo de ultraprocessados;
- saúde pública e gastos do SUS;
- álcool, cigarro, vapes e práticas de risco;
- apostas digitais;
- publicidade e persuasão;
- consumo consciente;
- fiscalização e transparência.

A plataforma web possui trilhas e atividades, enquanto o tabuleiro físico-digital expande esses conteúdos para decisões simuladas da vida adulta.

## 2.3 Objetivos pedagógicos relevantes ao software

O sistema deve ajudar o estudante a:

- perceber relação entre decisão individual e efeito coletivo;
- compreender trade-offs;
- visualizar consequências financeiras e não financeiras;
- analisar risco, crédito e dívida;
- entender que tributos possuem bases e finalidades diferentes;
- relacionar arrecadação a serviços públicos e cidadania;
- justificar escolhas;
- aprender com a trajetória da partida, não apenas com o resultado final.

O software deve registrar dados úteis para avaliação pedagógica sem exigir dados pessoais desnecessários.

---

# 3. ESCOPO DO PRODUTO UNITY

## 3.1 Objetivo atual

O Unity substituirá a antiga direção de interface em Pygame para criar uma experiência 3D mais rica, mantendo o princípio **physical-first**.

O aplicativo Unity deve funcionar inicialmente como **console/painel do mestre** com representação 3D do tabuleiro.

O jogador continua jogando principalmente no tabuleiro físico.

### Responsabilidades do sistema Unity

- criar/configurar a sessão;
- cadastrar 2 a 4 jogadores;
- definir/sortear personagens;
- controlar ordem de turnos;
- representar o tabuleiro em 3D;
- registrar dado/destino definido no físico;
- mover a peça virtual correspondente;
- identificar a casa correta;
- abrir o evento correto;
- validar requisitos e efeitos;
- mostrar estado antes/depois;
- controlar atributos, dinheiro, dívida, ativos e ciclos;
- registrar histórico auditável;
- salvar e retomar partidas;
- permitir desfazer de forma segura;
- controlar progressão do tabuleiro interno;
- apresentar encerramento/pódio;
- exportar dados de playtest.

### O Unity não deve exigir no MVP

- que cada jogador use um celular;
- internet externa;
- conta de usuário;
- nuvem;
- matchmaking;
- multiplayer online;
- ranking global;
- microserviços;
- integração física automática por sensores;
- movimentação automática obrigatória do tabuleiro físico.

---

# 4. PRINCÍPIOS DE DESIGN

## 4.1 Physical-first

O tabuleiro físico é o centro social, tátil e estratégico da experiência.

O digital deve:

- reduzir trabalho do mestre;
- impedir erros previsíveis;
- registrar estado;
- apresentar informação;
- ajudar na visualização;
- não duplicar desnecessariamente ações físicas.

## 4.2 Uma decisão principal por vez

O painel do mestre deve minimizar carga cognitiva.

Evitar telas em que o operador precise interpretar várias ações simultaneamente.

## 4.3 O mestre controla, o sistema valida

O mestre registra o ocorrido no físico. O sistema valida se a ação é legal.

Exemplo:

1. jogador lança dado físico;
2. mestre informa o resultado ou a casa de destino;
3. sistema calcula/valida destino;
4. sistema move peça virtual;
5. sistema abre evento;
6. escolha é tomada;
7. sistema apresenta preview;
8. mestre confirma;
9. estado é persistido.

## 4.4 Data-driven

Eventos, personagens, casas, parâmetros e balanceamento devem ser definidos por dados sempre que possível.

Evitar regras espalhadas em `if/else` dentro de MonoBehaviours.

## 4.5 Offline-first

A partida principal precisa funcionar sem internet.

---

# 5. ESTRUTURA DO JOGO DE TABULEIRO

O Money Rank combina elementos de:

- Banco Imobiliário/Monopoly: percurso externo, economia, ativos, despesas, tributos e eventos;
- Ludo: progressão interna, captura e objetivo central;
- jogos educacionais: cartas, situações-problema, escolhas e feedback;
- Kahoot apenas como referência de entrada por código, sincronização e apresentação coletiva — **não** como regra principal de pontuação por velocidade.

---

# 6. TABULEIRO EXTERNO

## 6.1 Baseline V2

A referência atual define um tabuleiro externo 8 × 8 considerando o contorno total.

O perímetro possui **28 posições**:

- 4 extremidades;
- 24 casas de evento.

Essa quantidade ainda deve permanecer configurável porque há uma inconsistência antiga entre “24 casas / 8 categorias × 3” e a proposta de quatro casas de imposto.

### Regra de implementação

Não hardcodar `28` em sistemas de domínio.

Usar `BoardDefinition` contendo uma lista ordenada de `BoardSpaceDefinition`.

O prefab/visual pode inicialmente representar 28 casas, mas a lógica deve funcionar com qualquer número coerente de posições.

---

# 7. QUATRO EXTREMIDADES DO TABULEIRO

Baseline V2:

### 7.1 Início / Novo Ano

- marca começo do percurso;
- registra passagem completa;
- participa do fechamento de ciclo anual;
- baseline: 3 voltas completas = 1 ano;
- quantidade de voltas por ano deve ser configurável.

### 7.2 Hospital

Representa crise de saúde e recuperação.

Baseline V2:

- pode ser acionado ao cair na extremidade ou quando a condição de saúde exigir;
- jogador pode perder próximo movimento;
- saída por pagamento ou rolagem específica;
- jogador não é eliminado por falta de dinheiro.

### 7.3 Banco e Crédito

Responsável por:

- empréstimos;
- dívidas;
- renegociação;
- quitação;
- operações financeiras individuais.

### 7.4 Assembleia / Cofre Público

Responsável por:

- decisões coletivas;
- uso de recursos públicos;
- votação;
- ajuda emergencial conforme regras;
- visualização de cidadania fiscal.

---

# 8. CATEGORIAS DE CASAS DE EVENTO

Baseline V2 possui oito categorias:

1. Despesa não essencial;
2. Despesa essencial;
3. Investimento;
4. Imposto;
5. Dívida;
6. Progresso de Vida;
7. Lazer;
8. Oportunidade.

Cada `BoardSpaceDefinition` deve referenciar uma categoria e, quando aplicável, uma tabela/conjunto de eventos.

---

# 9. EVENTOS E CARTAS

## 9.1 Estrutura mínima de um evento

Todo evento deve possuir ID estável.

Modelo conceitual:

```text
EventDefinition
- id
- version
- category
- title
- educationalText
- tags
- conditions
- options[]
- duration
- presentation
```

Cada opção:

```text
EventOptionDefinition
- id
- label
- description
- requirements[]
- effects[]
- visibility
```

Cada efeito:

```text
GameEffect
- target
- operation
- value
- duration
- source
```

## 9.2 Informações educacionais

Cartas não são apenas comandos mecânicos.

Devem permitir:

- título curto;
- explicação factual;
- situação/contexto;
- escolhas comparáveis;
- consequências claras;
- feedback após resolução.

## 9.3 Versionamento

Cada resolução deve registrar:

- ID do evento;
- versão do evento;
- opção escolhida;
- estado antes;
- estado depois.

Alterar um evento no futuro não pode reescrever a interpretação de partidas antigas.

---

# 10. FLUXO DE TURNO

Fluxo funcional desejado:

1. identificar jogador atual;
2. aplicar pendências do início do turno;
3. tratar perda de turno/internação quando necessário;
4. aguardar movimento físico;
5. mestre registra dado ou destino;
6. sistema valida e move a peça virtual;
7. detectar passagem por Início;
8. resolver casa de destino;
9. abrir evento/ação;
10. receber escolha;
11. mostrar preview de efeitos;
12. mestre confirma;
13. aplicar efeitos;
14. verificar progressão/regressão;
15. verificar ciclo anual;
16. verificar captura;
17. verificar condição de fim;
18. salvar snapshot;
19. passar ao próximo jogador.

---

# 11. MÁQUINA DE ESTADOS

Usar uma máquina de estados explícita.

Baseline:

```text
SETUP
  -> START_TURN
  -> AWAIT_MOVE
  -> RESOLVE_SPACE
  -> AWAIT_CHOICE (quando necessário)
  -> APPLY_EFFECTS
  -> CHECK_PROGRESS
  -> END_TURN
  -> START_TURN

GAME_END -> RESULTS
```

Estados adicionais são permitidos se melhorarem clareza, por exemplo:

```text
ANNUAL_CYCLE
HOSPITAL_RESOLUTION
ASSEMBLY_VOTE
PAUSED
RESTORING_SESSION
```

### Regra

A interface só oferece comandos legais para o estado atual.

---

# 12. TABULEIRO INTERNO

O tabuleiro interno representa progressão de vida e não deve ser tratado como simples placar monetário.

Baseline V2:

- percurso curto;
- aproximadamente 8 a 10 posições até o centro;
- pontos de salvamento;
- progressão por completar atributos positivos;
- possibilidade de regressão;
- captura inspirada no Ludo;
- objetivo central representa conclusão do percurso.

### Implementação

Criar um `InnerBoardDefinition` separado do tabuleiro externo.

Não misturar índices dos dois percursos.

---

# 13. ATRIBUTOS — ÁREA EM TRANSIÇÃO PARA V3

Há evolução de design entre os documentos.

## 13.1 Baseline V2 documentado

- Saúde — positivo;
- Conhecimento — positivo;
- Cidadania — positivo;
- Reserva — proteção/recurso;
- Dívida — regressivo.

Na V2, ao preencher 3 marcas de atributo positivo, o jogador avança no tabuleiro interno e o marcador é zerado.

## 13.2 Direção de design posterior da equipe

Discussões posteriores trabalharam seis atributos divididos em positivos e negativos:

**Negativos**
- Cortisol;
- Toxina;
- Dívida.

**Positivos**
- Conhecimento;
- Cidadania;
- Investimento.

Também foi discutida a lógica de três blocos `0, 1, 2` para completar um ciclo.

## 13.3 Regra técnica obrigatória

Como a V3 ainda precisa congelar a regra final, o código **não pode depender de enum fixo com esses atributos**.

Implementar atributos de forma data-driven:

```text
AttributeDefinition
- id
- displayName
- type: Positive | Negative | Protective | Neutral
- threshold
- overflowBehavior
- completionEffects[]
- icon
- presentation
```

O jogador deve possuir um mapa/dicionário de estados por `AttributeId`.

Assim, V3 poderá substituir Saúde por Cortisol/Toxina ou alterar Investimento sem refazer o motor.

---

# 14. PERSONAGENS

O jogo trabalha com personagens capivaras representando perfis/profissões.

Seis perfis já discutidos:

1. Capivara Professora;
2. Capivara Médica;
3. Capivara Economista;
4. Capivara Atleta;
5. Capivara Empreendedora;
6. Capivara Motorista de Aplicativo.

Baseline de design: cada personagem deve ter **vantagem e limitação/desvantagem**, evitando que o sorteio sozinho determine o resultado.

## 14.1 Exemplos V2 — tratar como dados provisórios

### Professora
- salário menor;
- afinidade com Conhecimento;
- possibilidade de bônus periódico em Conhecimento.

### Médica
- salário maior;
- redução de custo no Hospital;
- penalidade quando ignora crise.

### Economista
- salário maior;
- afinidade com Investimentos;
- exposição a revés econômico.

### Atleta
- afinidade com Saúde/Lazer;
- risco de lesão.

### Empreendedora
- renda variável;
- mais escolhas em Oportunidade;
- risco maior em revés.

### Motorista de Aplicativo
- renda instável;
- interação especial com Carro;
- maior vulnerabilidade sem reserva/proteção.

## 14.2 Estrutura técnica

```text
CharacterDefinition
- id
- displayName
- description
- salaryRules
- startingResources
- startingAttributes
- passiveAbilities[]
- disadvantages[]
- progressionTrack
- visualPrefab
- portrait
```

Nenhuma habilidade deve existir somente em código sem uma definição identificável.

---

# 15. ECONOMIA E RECURSOS

Recursos devem ser separados semanticamente.

Possíveis estados:

- Caixa;
- Reserva;
- Dívida;
- salário;
- ativos;
- custos anuais;
- risco fiscal;
- saldo do Cofre Público.

Não misturar `Caixa`, `Reserva`, `Banco` e `Cofre Público` em uma variável genérica de dinheiro.

---

# 16. INVESTIMENTOS / ATIVOS — BASELINE V2

## 16.1 Casa

Baseline:

- custo inicial: 8 unidades;
- pode permitir novo ponto de salvamento no tabuleiro interno;
- custo anual de IPTU;
- limite provisório: 2.

## 16.2 Carro

Baseline:

- custo inicial: 6 unidades;
- benefício de movimento/bloqueio conforme regra vigente;
- custo anual de IPVA;
- limite provisório: 2.

## 16.3 Reserva

Baseline:

- recurso de proteção;
- cobre despesas antes da dívida em situações definidas;
- não deve ser tratada como investimento especulativo;
- limite provisório V2: 10 unidades.

Todos esses números são **configuração**, não constantes espalhadas no código.

---

# 17. SISTEMA DE TRIBUTOS E ANO FISCAL

## 17.1 Ciclo anual

Baseline V2:

- três voltas completas equivalem a um ano;
- ao fechar o ano, processar salário, custos e imposto;
- quantidade de voltas deve ser configurável para playtest.

## 17.2 Tributos V2

- IPTU — associado a Casas;
- IPVA — associado a Carros;
- IOF — associado a crédito/endividamento;
- Fiscalização/Taxa pública — obrigação genérica de serviço público na V2.

A quantidade exata de casas de imposto no perímetro é uma decisão pendente.

## 17.3 Imposto de Renda — baseline provisório

V2 usa 20% do salário, com regra de arredondamento ainda não congelada.

O Codex deve implementar cálculo por estratégia/configuração:

```text
TaxRule
- base
- rate
- roundingMode
- destinations[]
- exemptions[]
```

## 17.4 Sonegação e risco fiscal — conteúdo pedagógico

A V2 possui uma mecânica opcional de sonegação associada a risco, multa, dívida e auditoria.

Ela deve ser implementada como **regra configurável**, não embutida na lógica do ano fiscal.

O sistema deve evitar apresentar sonegação como estratégia recomendável; a mecânica existe para demonstrar consequências e fiscalização.

---

# 18. BANCO E COFRE PÚBLICO

São entidades diferentes.

## Banco

Pode:

- pagar salários;
- receber compras;
- conceder empréstimos;
- cobrar dívida;
- administrar operações individuais.

## Cofre Público

Pode:

- receber parcela definida de impostos/multas;
- financiar ajuda emergencial permitida;
- financiar efeitos coletivos aprovados.

Não pode ser tratado como extensão do Banco.

Toda movimentação do Cofre Público deve possuir origem e destino auditáveis.

---

# 19. HOSPITAL

O Hospital deve ser um módulo próprio de resolução.

Requisitos:

- não eliminar permanentemente jogador;
- registrar internação;
- controlar perda de turno;
- permitir recuperação segundo regra configurada;
- aplicar custo/alternativa válida;
- exibir motivo e consequência.

Na V2, tentar sair por dado substitui o movimento do turno — decisão que deve ficar configurável até V3.

---

# 20. PROGRESSÃO, CAPTURA E VITÓRIA

## 20.1 Progressão

Completar ciclos de atributos positivos pode mover o jogador no tabuleiro interno.

Guardar dois valores:

- progresso atual do atributo;
- total histórico de ciclos concluídos.

Isso permite zerar o marcador sem perder informação de trajetória.

## 20.2 Regressão

Atributos negativos podem gerar regressão.

O efeito exato da V3 ainda precisa ser confirmado.

Implementar regressão por `ProgressionEffect`, não diretamente dentro do atributo.

## 20.3 Captura

Baseline V2:

- quando termina em posição ocupada por adversário, pode capturar;
- casas protegidas não permitem captura;
- captura afeta posição, não apaga dinheiro/atributos/ativos;
- baseline: uma peça por jogador no protótipo.

## 20.4 Vitória

Baseline atual:

- vence quem alcança o objetivo central do tabuleiro interno;
- demais jogadores terminam o ciclo atual quando a regra exigir.

## 20.5 Pódio/desempate provisório

Diagnóstico recomendou, caso ninguém/mais de um precise ser ordenado:

1. conclusão do objetivo central / ordem coerente de conclusão;
2. maior avanço interno;
3. maior quantidade histórica de ciclos positivos;
4. menor dívida;
5. maior reserva;
6. caixa apenas como desempate posterior.

Esse ranking é **provisório** até aprovação da V3.

Nunca usar tempo de resposta de quiz como condição principal de vitória.

---

# 21. PAINEL DO MESTRE — UX FUNCIONAL

## 21.1 Tela de configuração

Deve permitir:

- criar nova sessão;
- definir número de jogadores;
- nomes/apelidos locais;
- cor/equipe;
- personagem sorteado ou atribuído;
- ordem de turno;
- modo de playtest;
- configurações de regras;
- carregar sessão salva.

## 21.2 Tela principal da partida

Layout recomendado:

- tabuleiro 3D como elemento central;
- painel lateral com jogador atual;
- recursos essenciais;
- atributos;
- estado/pendências;
- ação principal;
- histórico/desfazer acessível;
- indicador da fase da máquina de estados.

## 21.3 Tela/modal de evento

Mostrar:

- categoria;
- título;
- texto educacional;
- contexto;
- opções;
- requisitos;
- opções indisponíveis com motivo;
- preview de consequência;
- confirmar/cancelar.

## 21.4 Administração

Ajuste manual deve:

- ser restrito;
- pedir motivo;
- registrar valor anterior e novo;
- registrar operador e data/hora local;
- nunca apagar histórico.

Prever futuramente dois níveis:

- Facilitador;
- Operador/aluno.

## 21.5 Encerramento

Apresentar:

- resultado/pódio;
- progresso interno;
- resumo da trajetória;
- decisões relevantes;
- indicadores pedagógicos;
- exportação.

---

# 22. EXPERIÊNCIA 3D

## 22.1 Direção visual

A versão Unity deve parecer um **jogo de tabuleiro físico virtual**, não um menu 2D com tabuleiro decorativo.

Referência conceitual:

- mesa física virtual;
- tabuleiro com profundidade;
- peças 3D;
- construções nas extremidades;
- câmera isométrica/perspectiva inclinada;
- animações suaves;
- aproximações durante ações importantes.

## 22.2 Relação com o tabuleiro físico real

O projeto físico trabalha com:

- base de MDF cortada/gravada a laser;
- profundidade em camadas;
- quatro extremidades preparadas para estruturas 3D;
- centro rebaixado;
- espaço inferior para eletrônica;
- LEDs e áudio com Arduino;
- personagens capivaras impressos em 3D;
- moedas, dados, peões, hospital, cofres/prédios e outras peças.

O modelo virtual deve poder reproduzir a linguagem visual do físico sem depender de medidas finais para funcionar.

---

# 23. CÂMERA

Criar `BoardCameraController` ou sistema equivalente.

Estados sugeridos:

```text
Overview
FocusCurrentPlayer
FocusDice
FollowPiece
FocusSpace
FocusSpecialBuilding
Results
```

Fluxo visual típico:

```text
START_TURN -> FocusCurrentPlayer
AWAIT_MOVE -> FocusDice ou Overview
MOVEMENT -> FollowPiece
RESOLVE_SPACE -> FocusSpace
EVENT -> câmera estável + UI
END_TURN -> Overview
RESULTS -> composição de encerramento
```

Usar Cinemachine se fizer sentido no projeto e estiver disponível/compatível, mas evitar dependência desnecessária para lógica de jogo.

---

# 24. DADO

O dado físico é a fonte primária no modo híbrido.

O sistema Unity deve suportar modos:

```text
ManualValue
VirtualRandom
PhysicalIntegration (futuro)
```

`ManualValue` é o padrão do MVP: o mestre informa o resultado obtido no dado físico.

A animação de dado virtual pode representar visualmente o valor confirmado sem substituir a fonte física.

Existe também conceito de **dado de inflação** no projeto físico. Não integrar sua regra antes de a especificação V3 definir seu uso exato.

---

# 25. ARQUITETURA DE SOFTWARE

## 25.1 Regra principal

**Domínio não depende de UnityEngine.**

A lógica central deve ser testável em EditMode sem carregar cena, câmera ou prefab.

## 25.2 Camadas sugeridas

```text
MoneyRank.Domain
MoneyRank.Application
MoneyRank.Content
MoneyRank.Infrastructure
MoneyRank.Presentation
MoneyRank.Editor
MoneyRank.Tests
```

### Domain

Contém:

- entidades;
- value objects;
- regras puras;
- state machine;
- efeitos;
- validações;
- resultado de comandos.

Não deve importar:

- UI;
- Input System;
- GameObject;
- Transform;
- SceneManager.

### Application

Casos de uso:

- CreateGame;
- StartGame;
- StartTurn;
- RegisterMove;
- ResolveSpace;
- ChooseOption;
- ConfirmEffects;
- ApplyAnnualCycle;
- UndoAction;
- SaveGame;
- EndGame.

### Content

Definitions carregadas a partir de:

- ScriptableObjects;
- JSON;
- catálogos versionados.

### Infrastructure

- persistência;
- serialização;
- filesystem;
- exportação CSV/JSON;
- relógio;
- IDs;
- integração futura com rede/Arduino.

### Presentation

- MonoBehaviours;
- Views;
- presenters/controllers;
- animações;
- UI;
- câmera;
- áudio;
- VFX.

---

# 26. ESTRUTURA DE PASTAS UNITY

Estrutura recomendada:

```text
Assets/
  _MoneyRank/
    Art/
      Models/
      Materials/
      Textures/
      Animations/
      VFX/
    Audio/
      Music/
      SFX/
    Prefabs/
      Board/
      Spaces/
      Characters/
      Buildings/
      Dice/
      UI/
    Scenes/
      Bootstrap.unity
      MainMenu.unity
      Game.unity
      Results.unity
      DevSandbox.unity
    Scripts/
      Domain/
      Application/
      Content/
      Infrastructure/
      Presentation/
      Editor/
    Data/
      ScriptableObjects/
      JSON/
    UI/
      UXML-or-UGUI-assets/
    Tests/
      EditMode/
      PlayMode/
Docs/
  MONEY_RANK_UNITY_SPEC.md
  ARCHITECTURE.md
  GAME_RULES.md
  OPEN_DECISIONS.md
  PLAYTEST.md
AGENTS.md
```

Não criar diretórios genéricos como `Scripts/Misc`, `Scripts/Old`, `Scripts/New`.

---

# 27. ENTIDADES PRINCIPAIS

Modelos conceituais mínimos:

## GameSession

```text
id
version
createdAt
updatedAt
phase
round
currentPlayerId
turnIndex
rulesetId
players[]
boardState
innerBoardState
publicFund
history
metadata
```

## Player

```text
id
name
colorId
characterId
turnOrder
```

## PlayerState

```text
externalPosition
internalPosition
laps
cash
reserve
salaryState
attributes{}
assets[]
debts[]
fiscalRisk
statusEffects[]
characterProgress
statistics
```

## BoardSpaceDefinition

```text
id
index
type
category
eventTableId
visualId
tags
```

## EventResolution

```text
id
eventId
eventVersion
playerId
spaceId
choiceId
beforeState
afterState
source
createdAt
operatorId/localRole
```

## Snapshot

```text
schemaVersion
sessionState
historyCursor
checksum(optional)
```

---

# 28. COMANDOS E EVENTOS INTERNOS

Separar intenção de fato ocorrido.

## Commands

Exemplos:

```text
CreateSession
AddPlayer
AssignCharacter
StartGame
RegisterDiceValue
MovePlayer
ChooseEventOption
ConfirmResolution
ApplyAnnualCycle
RequestPublicAid
UndoLastAction
EndTurn
EndGame
```

## Domain Events

Exemplos:

```text
GameStarted
TurnStarted
DiceRegistered
PlayerMoved
SpaceResolved
ChoiceResolved
ResourceChanged
AttributeChanged
AttributeCycleCompleted
PlayerCaptured
AnnualCycleStarted
TaxPaid
DebtChanged
HospitalEntered
HospitalExited
PublicFundChanged
ActionUndone
GameFinished
```

O histórico precisa permitir auditoria e diagnóstico de bugs.

---

# 29. PERSISTÊNCIA E DESFAZER

## 29.1 Salvamento

Salvar automaticamente após cada ação confirmada relevante.

Não esperar somente o fim do turno.

## 29.2 Estratégia inicial

Pode usar:

- JSON versionado para sessão/snapshot;
- log incremental de ações/eventos;
- arquivo local por sessão.

SQLite pode ser introduzido depois se houver necessidade real.

## 29.3 Undo

`Undo` não deve apagar histórico.

Uma reversão deve gerar registro próprio.

Exemplo:

```text
Action 81: PlayerMoved 12 -> 16
Action 82: UndoApplied target=81
```

A UI volta ao estado correto, mas a auditoria mantém o ocorrido.

---

# 30. EXPORTAÇÃO DE PLAYTEST

Exportar pelo menos:

- sessionId;
- duração total;
- duração média do turno;
- jogadores/personagens;
- eventos sorteados;
- escolhas;
- antes/depois;
- compras de ativos;
- dívidas;
- ciclos anuais;
- impostos;
- auditorias fiscais;
- Hospital;
- pedidos ao Cofre Público;
- capturas;
- progressão interna;
- quantidade de undo;
- motivos de ajuste manual;
- resultado final.

Formato inicial:

- JSON completo;
- CSV resumido.

Não exportar informação pessoal além do necessário para o playtest.

---

# 31. UI E ACESSIBILIDADE

Requisitos:

- texto legível em notebook/monitor escolar;
- não usar cor como único indicador;
- usar ícones + texto + estado visual;
- botões principais grandes;
- navegação previsível;
- atalhos para operações frequentes podem ser adicionados;
- confirmação para ações destrutivas;
- feedback claro de erro;
- opção indisponível deve explicar o motivo.

Evitar UI mobile-first no painel do mestre. O alvo inicial é tela de computador.

---

# 32. ÁUDIO, LED E INTEGRAÇÃO FÍSICA FUTURA

O projeto físico prevê LEDs, som e Arduino.

A arquitetura deve permitir um adaptador futuro:

```text
IPhysicalBoardBridge
- SetTurnColor(...)
- PlayEventSignal(...)
- TriggerSpecialSpace(...)
- SendGameState(...)
```

Não implementar protocolo definitivo sem hardware validado.

Não permitir que falha no Arduino interrompa o jogo digital.

Toda integração física deve degradar graciosamente para modo manual.

---

# 33. QR CODE / CELULARES — FUTURO OPCIONAL

A ideia de sala por código continua válida apenas onde o celular agrega valor.

Possíveis usos futuros:

- lobby;
- votação de Assembleia;
- escolha privada;
- consulta de resultado pessoal;
- pódio final.

Evitar:

- cada jogador registrar posição;
- cada jogador atualizar dinheiro;
- exigir confirmação duplicada de toda ação;
- tornar celular obrigatório.

A camada de rede deve ser um módulo opcional e não contaminar o domínio.

---

# 34. CENAS UNITY

## Bootstrap

Responsável por:

- inicialização;
- DI/composition root;
- serviços globais;
- carregamento de configurações.

## MainMenu

- nova partida;
- continuar;
- configurações;
- ferramentas de playtest.

## Game

Cena principal do tabuleiro.

Evitar criar uma cena por modal/evento.

Eventos devem ser UI/overlays dentro da sessão.

## Results

Pode ser cena própria ou estado da cena Game, conforme arquitetura.

## DevSandbox

Cena exclusiva para desenvolvimento de:

- animação de peças;
- dado;
- casas;
- câmera;
- UI isolada;
- efeitos.

Não usar `DevSandbox` como dependência do jogo final.

---

# 35. PREFABS E REPRESENTAÇÃO DO TABULEIRO

Prefabs recomendados:

```text
BoardRoot
ExternalBoardSpace
SpecialCornerSpace
InnerBoardSpace
PlayerPiece
Dice
BuildingHospital
BuildingBank
BuildingAssembly
BuildingStart
EventMarker
SavePoint
```

O tabuleiro visual deve ser montado a partir de dados ou ferramenta de Editor, evitando posicionamento manual impossível de reproduzir.

Criar ferramenta `BoardLayoutAuthoring`/Editor Window se necessário.

---

# 36. UNITY CLI + UNITY PIPELINE + CODEX

## 36.1 Contexto

O Unity CLI e o pacote `com.unity.pipeline` devem ser usados como camada de automação para o Codex interagir com o Editor.

O Unity CLI atual é experimental; portanto, scripts devem ser escritos de forma conservadora e comandos disponíveis devem ser descobertos em vez de presumidos.

Requisitos atuais do Pipeline:

- Unity Editor 6.0+;
- Unity CLI instalado;
- projeto aberto no Editor;
- autenticação Unity;
- pacote Unity Pipeline instalado no projeto.

## 36.2 Setup esperado no Windows

Na raiz do projeto:

```powershell
unity auth login
unity pipeline install
unity pipeline list
```

Para descobrir conexão/comandos:

```powershell
unity status
unity command
unity list
unity commands --format json
```

Quando houver mais de um Editor aberto:

```powershell
unity command --project-path="C:\caminho\MoneyRank"
```

## 36.3 Regra para o Codex

**Nunca inventar nomes/parâmetros de comandos do Unity Pipeline.**

Antes de automatizar uma operação de Editor:

1. verificar que o projeto/Editor correto está conectado;
2. executar comando de descoberta (`unity list`, `unity command` ou manifesto equivalente);
3. ler schema/parâmetros;
4. executar a operação;
5. verificar retorno;
6. inspecionar erros/Console;
7. executar testes relevantes.

## 36.4 Arquivos vs Editor

### Editar diretamente no filesystem

Preferível para:

- `.cs`;
- `.asmdef`;
- JSON;
- documentação;
- configurações textuais controladas.

### Preferir Editor/Pipeline

Preferível para:

- criação/manipulação de GameObjects;
- alteração de cenas;
- prefabs;
- referências serializadas;
- transforms;
- operações de asset que dependem de importação Unity.

Evitar editar YAML de `.unity` e `.prefab` manualmente, salvo necessidade clara e validação posterior.

---

# 37. PROTOCOLO DE EXECUÇÃO PARA O CODEX

Para cada tarefa relevante:

## Antes

1. ler `AGENTS.md`;
2. ler este SPEC;
3. identificar arquivos afetados;
4. identificar regra de gameplay envolvida;
5. checar `OPEN_DECISIONS.md`;
6. se for regra indefinida, não assumir solução definitiva.

## Durante

1. fazer mudança mínima coerente;
2. preservar separação Domain/Presentation;
3. adicionar/atualizar testes;
4. manter conteúdo data-driven;
5. não quebrar serialização sem migration/version bump.

## Depois

1. compilar;
2. checar Console/logs;
3. rodar EditMode tests relacionados;
4. rodar PlayMode tests quando houver comportamento de cena;
5. verificar cena/prefab quando alterado;
6. resumir arquivos modificados;
7. listar decisões pendentes;
8. informar riscos ou regressões possíveis.

---

# 38. PADRÕES DE CÓDIGO

## 38.1 Linguagem

- C# compatível com a versão do Unity utilizada;
- nullable/context conforme suportado/configurado;
- nomes de código em inglês;
- textos apresentados ao usuário podem ser em português.

## 38.2 Namespaces

Exemplo:

```text
MoneyRank.Domain
MoneyRank.Domain.Events
MoneyRank.Application
MoneyRank.Infrastructure.Persistence
MoneyRank.Presentation.Board
MoneyRank.Presentation.UI
MoneyRank.Editor
```

## 38.3 MonoBehaviour

MonoBehaviour deve funcionar como adapter/view/controller de engine.

Não armazenar regra crítica exclusivamente em MonoBehaviour.

## 38.4 Singletons

Evitar Singleton global indiscriminado.

Se existir `GameBootstrapper` ou composition root, explicitar dependências.

## 38.5 ScriptableObjects

Usar para definições de conteúdo e authoring, não como estado mutável da partida.

Nunca salvar estado de jogador diretamente no asset ScriptableObject de definição.

---

# 39. TESTES

## 39.1 EditMode obrigatórios para domínio

Cobrir:

- ordem de turnos;
- movimento por índice;
- wrap do percurso externo;
- contagem de voltas;
- mudança de fase;
- aplicação de efeitos;
- requisito inválido;
- limites de atributo;
- progressão interna;
- regressão;
- captura;
- ciclo anual;
- tributo;
- dívida;
- Hospital;
- Cofre Público;
- vitória;
- undo;
- save/load round-trip.

## 39.2 PlayMode

Cobrir:

- criação do tabuleiro visual;
- binding de PlayerPiece;
- animação de movimento não duplicar efeito;
- UI reagir ao GameState;
- confirmação de evento;
- troca de jogador;
- carregamento de cena/sessão.

## 39.3 Regra

Uma animação nunca deve ser a fonte da regra.

A regra é aplicada no domínio; a animação representa o resultado.

---

# 40. LOGS E DIAGNÓSTICO

Usar logs estruturados para desenvolvimento.

Categorias sugeridas:

```text
GAME_FLOW
BOARD
EVENT
ECONOMY
ATTRIBUTE
SAVE
UNDO
UI
PHYSICAL_BRIDGE
```

Não poluir Console com log por frame.

Erros de domínio devem retornar resultado explicável para a UI.

Exemplo:

```text
MoveRejected
reason = PlayerIsHospitalized
```

---

# 41. BUILD

Primeiro alvo:

- Windows x86_64;
- execução local;
- sem necessidade de instalar ambiente de desenvolvimento;
- conteúdo externo versionado corretamente;
- diretório de saves em local gravável do usuário.

Antes de considerar build entregável:

- abrir em máquina diferente;
- criar partida;
- salvar;
- fechar;
- reabrir;
- retomar;
- concluir sessão curta.

---

# 42. VERSIONAMENTO

Usar Git/GitHub.

Regras:

- commits pequenos;
- não versionar `Library/`, `Temp/`, `Logs/`, `Obj/`;
- versionar `Packages/manifest.json` e lockfile apropriado;
- versionar `.meta`;
- usar Git LFS para binários grandes quando necessário;
- evitar grandes reimports sem motivo.

Branches sugeridas:

```text
main
feature/*
fix/*
tool/*
```

Não criar workflow Git complexo demais para equipe pequena.

---

# 43. MVP UNITY — CRITÉRIOS DE ACEITE

O MVP digital pode ser considerado funcional quando:

1. cria sessão para 2–4 jogadores;
2. associa um personagem a cada jogador;
3. inicia ordem de turnos;
4. mestre registra um movimento;
5. peça virtual percorre o tabuleiro;
6. sistema detecta a casa correta;
7. evento é carregado por dados;
8. sistema mostra opções/requisitos;
9. sistema mostra preview antes/depois;
10. confirmação altera estado uma única vez;
11. progressão interna funciona;
12. jogo passa ao próximo turno;
13. histórico registra todas as ações;
14. Undo recupera estado sem apagar auditoria;
15. save/load preserva a sessão;
16. encerramento mostra resultado coerente;
17. jogo funciona offline;
18. jogadores não precisam registrar o próprio estado;
19. conteúdo pode ser alterado sem reescrever o motor;
20. fluxo central possui testes automatizados.

---

# 44. PRIMEIRO VERTICAL SLICE

Antes de implementar toda a economia, criar um vertical slice pequeno:

### Conteúdo

- 2 jogadores;
- 2 personagens genéricos;
- 20 casas temporárias ou subset do mapa;
- 4 categorias de teste;
- 8 eventos;
- 3 atributos genéricos configurados por dados;
- um tabuleiro interno curto;
- uma condição simples de vitória.

### Fluxo

```text
Setup
-> StartTurn
-> RegisterMove
-> AnimatePiece
-> ResolveSpace
-> ChooseOption
-> Preview
-> Confirm
-> Progress
-> EndTurn
```

Objetivo: validar arquitetura antes de importar regras completas.

---

# 45. ROADMAP TÉCNICO RECOMENDADO

## Fase 0 — Foundation

- repositório;
- pastas;
- asmdefs;
- Domain/Application;
- GameState;
- state machine;
- IDs;
- testes básicos;
- CLI/Pipeline funcionando.

## Fase 1 — Board Vertical Slice

- board definitions;
- montagem visual;
- PlayerPiece;
- movimento;
- câmera;
- turno;
- sandbox.

## Fase 2 — Event Engine

- EventDefinition;
- requirements;
- effects;
- preview;
- UI de evento;
- histórico.

## Fase 3 — Progression

- attributes genéricos;
- tabuleiro interno;
- salvamentos;
- regressão;
- captura;
- vitória.

## Fase 4 — Economy

- caixa;
- reserva;
- dívida;
- investimentos;
- Banco;
- Cofre Público;
- tributos;
- ano fiscal;
- Hospital.

## Fase 5 — Persistence / Master UX

- autosave;
- load;
- undo;
- administração;
- exportação de playtest.

## Fase 6 — 3D Polish

- modelos finais;
- materiais;
- capivaras;
- animações;
- áudio;
- VFX;
- melhor câmera.

## Fase 7 — Integrações opcionais

- Arduino/LED/som;
- QR code;
- celulares;
- painel web local;
- sincronização com plataforma Money Rank.

---

# 46. NÃO-OBJETIVOS DO PRIMEIRO CICLO

Não priorizar agora:

- multiplayer online;
- backend em nuvem;
- autenticação;
- ranking global;
- loja;
- Steam;
- IA generativa dentro do jogo;
- matchmaking;
- editor completo de cursos da plataforma web dentro do Unity;
- reconstruir toda a plataforma Money Rank no Unity;
- física realista do dado como fonte oficial do resultado;
- integração de hardware sem protocolo definido.

---

# 47. DECISÕES ABERTAS QUE BLOQUEIAM V3

Registrar e manter visível:

1. mapa/numeração final do tabuleiro externo;
2. quantidade final de casas de imposto;
3. atributos oficiais da V3;
4. relação entre Saúde vs Cortisol/Toxina;
5. papel exato de Investimento como atributo vs ativos;
6. regra final de progressão/regressão;
7. quantidade de peças por jogador no interno;
8. localização dos pontos de salvamento;
9. regra final de captura;
10. regra final do Hospital;
11. quantidade de voltas por ano;
12. cálculo/arredondamento do IR;
13. repasse ao Cofre Público;
14. regra final de sonegação/auditoria;
15. ordem padrão de pagamento (Caixa/Reserva/Cofre/Empréstimo/Dívida);
16. buffs/debuffs finais dos seis personagens;
17. condição e desempates oficiais de vitória;
18. regras do dado de inflação;
19. conjunto mínimo de cartas/eventos da V3;
20. quais decisões exigirão participação por celular.

O Codex deve consultar esta lista antes de transformar regra provisória em implementação rígida.

---

# 48. DOCUMENTAÇÃO QUE DEVE EXISTIR NO REPOSITÓRIO

## `Docs/GAME_RULES.md`

Somente regras aprovadas.

## `Docs/OPEN_DECISIONS.md`

Decisões ainda não aprovadas.

## `Docs/ARCHITECTURE.md`

Decisões técnicas e diagramas.

## `Docs/PLAYTEST.md`

Métricas, protocolo e formato de exportação.

## `Docs/CONTENT_SCHEMA.md`

Schema de personagens, eventos, atributos e tabuleiros.

## `AGENTS.md`

Regras operacionais para agentes.

---

# 49. DEFINITION OF DONE PARA TAREFAS DO CODEX

Uma tarefa não está concluída apenas porque o código foi escrito.

Considerar concluída quando aplicável:

- código compila;
- nenhuma exceção nova relevante no Console;
- testes existentes continuam passando;
- novos testes cobrem nova lógica;
- cena/prefab abre corretamente;
- dados não foram hardcodados indevidamente;
- documentação foi atualizada se arquitetura/regra mudou;
- nenhuma decisão de gameplay foi inventada;
- save schema foi versionado se alterado;
- resumo final informa exatamente o que mudou.

---

# 50. MODELO DE PROMPT PARA O CODEX

Use tarefas pequenas e verificáveis.

Exemplo:

```text
Leia AGENTS.md, Docs/MONEY_RANK_UNITY_SPEC.md e Docs/OPEN_DECISIONS.md.

Tarefa: implementar a fundação do Turn System.

Requisitos:
- Domain não pode depender de UnityEngine.
- Implementar GamePhase, TurnState e transições mínimas:
  Setup -> StartTurn -> AwaitMove -> ResolveSpace -> ApplyEffects -> CheckProgress -> EndTurn.
- Não implementar regras fiscais ou Hospital ainda.
- Criar testes EditMode para transições válidas e inválidas.
- Não alterar cenas sem necessidade.
- Ao terminar, compilar, rodar os testes relacionados e listar arquivos alterados.
- Se encontrar uma regra indefinida, registrar em OPEN_DECISIONS.md em vez de inventá-la.
```

---

# 51. REFERÊNCIAS DE PROJETO USADAS NESTE SPEC

Fontes internas principais:

- **Money Rank — Documentação V2 — estrutura de regras para prototipagem física**; versão de trabalho de 21/08/2026.
- **Diagnóstico Técnico e Proposta de Arquitetura — Integração do tabuleiro físico com a experiência digital do MoneyRank**; 04/09/2026.
- **Inscrição Ciência Jovem — Money Rank**; setembro de 2026.
- **Briefing de redesign da landing page Money Rank**; setembro de 2026.
- decisões posteriores de design do projeto sobre atributos, fabricação digital, personagens, tabuleiro em MDF, peças 3D, LEDs e som.

Fontes técnicas externas:

- Unity Docs — Local tools and Unity CLI: https://docs.unity.com/en-us/unity-production-pipeline/local-tools-cli
- Unity Docs — Unity CLI: https://docs.unity.com/en-us/unity-cli
- Unity Docs — Unity Pipeline package: https://docs.unity.com/en-us/unity-cli/unity-pipeline/unity-pipeline-package
- Unity Docs — Unity CLI reference: https://docs.unity.com/en-us/unity-cli/unity-cli-reference

---

# 52. RESUMO EXECUTIVO PARA O AGENTE

Se o contexto for reduzido, preservar estas regras:

1. Money Rank é um jogo educacional físico-digital de educação financeira e fiscal.
2. O físico continua sendo a experiência principal; Unity é o painel/árbitro/registro 3D do mestre.
3. O jogo possui percurso externo de eventos e percurso interno inspirado em Ludo.
4. O sistema deve controlar turnos, eventos, efeitos, economia, atributos, progressão, histórico, save/load, undo e resultado.
5. O domínio não depende de UnityEngine.
6. Conteúdo e balanceamento são data-driven.
7. Não hardcodar atributos porque a V3 ainda está consolidando a mudança de Saúde/Reserva para uma possível estrutura com Cortisol/Toxina/Dívida e Conhecimento/Cidadania/Investimento.
8. Não inventar regra quando houver lacuna.
9. O Unity CLI + Pipeline deve ser usado com descoberta de comandos antes de executar automação de Editor.
10. Toda tarefa precisa ser verificável por compilação/testes e preservar auditabilidade da partida.

