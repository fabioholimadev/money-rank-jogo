# Money Rank — Decisões abertas

O documento V3 fechou várias decisões que antes estavam pendentes. A
implementação deve seguir `GAME_RULES.md`; os itens abaixo continuam proibidos
de virar regra rígida sem aprovação.

## Conteúdo e layout

1. Compatibilizar o mapa externo: a V3 aprovada define 28 posições (24 eventos), enquanto o documento de distribuição recebido em 04/09/2026 lista 36 posições (32 eventos e quatro cantos). O sandbox foi atualizado para o mapa de 36 posições solicitado; confirmar futuramente qual versão será a regra oficial.
2. Posições exatas dos dois pontos de salvamento e das casas protegidas no
   tabuleiro interno.
3. Quantidade e representação visual das peças internas por jogador.
4. Três exemplos finais de carta para cada uma das oito categorias.
5. Assets finais: identidade visual, ícones, fontes, materiais, personagens,
   animações, áudio e licenças.
- Definir e cadastrar o conteúdo/efeito da carta Moradia; até lá, não existe
  cobrança automática por casas possuídas.

### Cartas de Oportunidade

- Definir os efeitos aprovados para cada carta; as duas cartas atuais do
  sandbox são protótipos sem efeito de jogo.
- Confirmar se o descarte de carta profissional vale para todos os jogadores
  daquela profissão ou somente para quem confirmou o uso. O sandbox deixa esse
  comportamento configurável e usa o escopo por jogador enquanto a decisão não
  for tomada.
- Criar conteúdo de cartas gerais e específicas por profissão. A interface
  atualmente oferece somente as cartas cadastradas na tabela de Oportunidade
  da casa e mostra até três.

## Configuração da partida

6. Personagens podem se repetir ou devem ser exclusivos por sessão?
7. Quais atributos, habilidades e contrapesos oficiais serão cadastrados nos
   seis perfis de personagem? No protótipo, o sorteio é feito fora do jogo e o
   mestre registra manualmente o resultado na tela de seleção. O protótipo
   permite personagens repetidos; a regra definitiva de exclusividade continua
   pendente.
8. Quais permissões distinguem mestre/facilitador e operador/aluno?

## Regras que exigem validação

9. Morte/eliminação individual no modo Difícil e adequação à faixa etária.
10. Nome editorial definitivo do quarto canto: `Virada e Consequência` ou
    `Mega da Virada`.
11. Limiar e efeito exatos de prevenção coletiva e rede de apoio do Cofre.
12. Resultado exato da opção de espera no Hospital por dificuldade.
13. Transcrição e validação final dos atributos iniciais, habilidade e
    contrapeso dos seis personagens em assets oficiais.

## Produto futuro — fora do MVP inicial

14. Protocolo do modo físico-digital e eventual hardware.
15. Suporte Android/participação por celular — em implementação conforme a
    solicitação da Fase 2; faltam build e testes em aparelho real.
16. Multiplayer online — em implementação com Unity Multiplayer Services,
    Relay e Netcode for GameObjects. O MVP técnico de sala e código está iniciado;
    sincronização autoritativa de gameplay, reconexão e configuração operacional
    dos serviços ainda precisam ser concluídas e testadas.
17. O dado de inflação não aparece como regra ativa na V3; qualquer retorno
    precisa de aprovação explícita.

## Decisões fechadas pela V3

- 2–4 jogadores mais mestre.
- Tabuleiro externo com 28 posições e oito categorias de evento.
- Tabuleiro interno com oito casas, dois salvamentos e áreas protegidas.
- Seis atributos oficiais e suas consequências nos níveis 3 e 6.
- Captura perde somente posição.
- Seis personagens e salários-base.
- Ciclo de salário e alíquota de IR por dificuldade; IR arredondado para cima e
  destinado ao Cofre.
- Sonegação, risco fiscal e auditoria.
- Banco e Cofre separados.
- Rotas privada, pública e espera no Hospital, ressalvada a validação do modo
  Difícil.
- Regra da Virada e Consequência.
- Vitória, rodada final e critérios de desempate.

## Registro da implementação atual

- A máquina de estados implementa somente transições legais do fluxo-base.
- `AwaitChoice` é opcional entre `ResolveSpace` e `ApplyEffects`.
- Limites de jogadores são fornecidos por `SessionRules`.
- A fundação de atributos, tabuleiro interno, captura, vitória, economia e
  tributação usa políticas e IDs configuráveis.
- O sandbox usa 36 posições externas seguindo o documento de distribuição recebido:
  quatro cantos e oito casas em cada setor, com oito categorias repetidas quatro
  vezes. A V3 aprovada ainda descreve 28 posições, portanto a divergência está
  pendente de decisão editorial. As cartas X/Y/Z são somente protótipos sem efeitos.
- O sandbox ainda usa três atributos genéricos, dois personagens genéricos e
  caminho interno de seis posições; esses dados continuam temporários.

## V4 — decisões pendentes registradas pela integração (`DECISION_REQUIRED`)

- **DECISION_REQUIRED — ritmo do Ludo / duração da partida.** Nas simulações (`Docs/V4/05_validacao/resultado_simulacao_bots_gananciosos.csv`),
  após 71–86 rodadas (≈24–29 anos de jogo) o líder de bots gananciosos concluiu 1–2 dos 4 peões (progresso 64–130 de 228 posições).
  Por extrapolação linear a partida levaria 150–250 rodadas, o que é inviável no kit físico. Bots não são jogadores humanos e as regras
  V4 são parâmetros de pré-teste, por isso **nenhum número foi alterado**. Candidatos configuráveis para a bateria 2: limiar da barra (3),
  `max_steps_per_positive_cycle`, custo/benefício dos ganhos-base, número de peões. Decidir após as partidas físicas da bateria 1.
- **Fiscalização e moeda TRISTE na sonegação.** Implementado: +1 Cortisol só quando a multa é efetivamente paga; se não há recursos
  (autuado) aplica-se apenas +3 Dívidas. Na fiscalização com moeda TRISTE o +1 Cortisol é aplicado sempre (uma vez por fiscalização).
- **Atleta (Fôlego Extra):** a escolha é oferecida a cada ciclo positivo enquanto a habilidade do ano não foi usada; "recusar" não consome.
- **Empreendedora (Visão de Negócios):** troca explícita da opção selecionada (comando próprio), só em casas não essenciais, de dívida ou
  oportunidade, nunca em tributos; considera "desfavorável" opção com custo ou ganho de Toxina/Cortisol/Dívidas.
- **Prazo do veículo:** `rodadasCompletas + 9 + 1` na compra e `+3` na manutenção (ver INV05/INV06).
- **Undo local:** pilha em memória (não sobrevive a recarregar o aplicativo); proibido no modo online.

- **DECISION_REQUIRED — contagem das jogadas extras por dados iguais.** Implementado: dados iguais dão até **3 jogadas extras** por turno (no máximo 4 lançamentos); depois, mesmo com novo par, a vez passa. Se a intenção for "3 lançamentos no total", alterar `doubles_extra_rolls_max` para 2 em `parametros.json`. Sem jogada extra após internação/eliminação.

- **DECISION_REQUIRED — símbolo mostrado no LCD do cofre ("R$" x "CRIA").** A moeda do jogo agora se chama **CRIA Coin** (tela, regras e documentos).
  Pelo pedido do usuário, o LCD mostra o valor como `R$0,00` (e `R$28.260,00` etc.). Para trocar o prefixo do display (por exemplo `CRIA 28.260,00`),
  basta mudar `CURRENCY_PREFIX` em `Docs/Hardware/cofre_display/cofre_protocol.h` e `CofferProtocol.DisplayPrefix` em `V4Coffer.cs` (os vetores
  de teste `money` precisam ser regerados com `tests/make_vectors.py`). O valor enviado é sempre o mesmo número inteiro de CRIA Coin × 100.
- **Nomes das colunas dos dados** (`custo_umr`, `base_umr`, `recompensa_imediata_umr`, `retorno_total_umr`, `salario_apos_umr`) foram mantidos: são identificadores
  estáveis do conteúdo, não texto de tela. Só o texto visível trocou de "UMR" para "CRIA Coin". Salvamentos antigos não abrem mais
  (o hash do conteúdo mudou porque os textos das cartas mudaram).
- **DECISION_REQUIRED — hardware do cofre.** Módulo em uso confirmado em 27/11/2026: OLED 0,96" I2C SSD1306 (0x3C) num Arduino Uno
  (clone CH340), testado com o protocolo do cofre pelo `Docs/Hardware/cofre_display/Test-CofreDisplay.ps1`. O sketch agora usa o OLED por
  padrão e mantém o LCD 1602 como opção (`COFRE_DISPLAY_OLED 0`, não testado). Ainda falta: confirmar se o kit final usa OLED ou LCD,
  testar a legibilidade do OLED na mesa e validar o fluxo completo pelo painel do cofre dentro do jogo.

