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
