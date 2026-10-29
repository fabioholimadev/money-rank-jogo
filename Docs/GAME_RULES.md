# Money Rank — Regras aprovadas

Este arquivo consolida as regras estáveis que podem orientar a implementação.
O documento **Money Rank Documentação V3 — Regras para Protótipo**, de
3 de outubro de 2026, prevalece sobre a documentação V2 nos pontos que trata.
Conteúdo do sandbox é técnico e não substitui estas regras.

Itens ainda sem definição suficiente permanecem em `OPEN_DECISIONS.md`.

## Produto e escopo do protótipo

- Experiência educacional físico-digital de educação financeira e fiscal.
- O aplicativo Unity atua como painel, árbitro, visualização e registro do
  mestre/facilitador.
- O primeiro modo jogável é digital local e offline.
- A sessão possui de 2 a 4 jogadores, além do mestre.
- Multiplayer online, nuvem, autenticação, celulares e automação de hardware
  não pertencem ao escopo inicial.

## Fluxo de sessão e turno

- A ordem de turnos é explícita e determinística.
- A interface só pode oferecer comandos legais para a fase atual.
- O fluxo-base é `Setup -> StartTurn -> AwaitMove -> ResolveSpace ->`
  `AwaitChoice? -> ApplyEffects -> CheckProgress -> EndTurn`.
- Ao concluir `EndTurn`, o próximo jogador passa a ser o jogador atual.
- No modo digital, o movimento usa um dado de seis faces; origem e resultado
  precisam ser registrados para auditoria.

## Tabuleiro externo

- O tabuleiro possui 28 posições: 4 cantos e 24 casas de evento.
- O Banco não é um canto; é uma função administrada pelo mestre.
- Banco e Cofre Público são instituições e contas separadas.
- Os quatro cantos são:
  1. Início/Ciclo;
  2. Hospital;
  3. Cofre Público;
  4. Virada e Consequência, nome ainda sujeito à confirmação editorial.
- As 24 casas se distribuem em oito categorias, com três casas por categoria:
  despesa essencial, despesa não essencial, lazer, oportunidade, progressão,
  investimento, dívida e impostos.
- A posição exata das 24 casas ainda precisa ser aprovada.

### Moradia

- Moradia é uma casa de Despesa Essencial.
- Não há cobrança automática calculada pela quantidade de casas possuídas.
- Qualquer efeito de Moradia ocorre somente quando uma carta correspondente é
  sorteada; o efeito final da carta ainda precisa ser definido.

## Tabuleiro interno e atributos

- O caminho interno possui oito casas até o centro.
- Existem dois pontos de salvamento e casas protegidas; suas posições exatas
  ainda precisam ser aprovadas.
- Captura faz o jogador perder apenas posição interna. Não remove dinheiro,
  ativos ou atributos.
- Cada jogador possui seis atributos, todos na faixa de 0 a 6:
  - positivos: Reserva, Cidadania e Conhecimento;
  - riscos: Toxina, Cortisol e Dívidas.
- Ao chegar a 3 em um atributo positivo, o jogador avança uma casa interna e o
  atributo diminui em 3.
- Ao chegar a 3 em Toxina ou Cortisol, o jogador regride uma casa interna e o
  atributo volta para 1.
- Ao chegar a 3 em Dívidas, o jogador regride uma casa interna. Ao chegar a 6,
  retorna ao último ponto de salvamento.
- Toxina ou Cortisol em 6 levam o jogador ao Hospital.
- Somente uma consequência de atributo é resolvida por turno; consequências
  adicionais permanecem pendentes para turnos seguintes.

## Personagens

O protótipo V3 define seis personagens. Salários-base aprovados:

| Personagem | Salário |
|---|---:|
| Professora | 8 |
| Médica | 16 |
| Economista | 12 |
| Atleta | 9 |
| Empreendedora | 10, com variação definida pela regra da personagem |
| Motorista | 9, com variação definida pela regra da personagem |

Cada personagem possui atributos iniciais, habilidade e contrapeso descritos no
documento V3. Esses dados devem ser transcritos para definições data-driven e
validados antes de substituir os dois personagens genéricos do sandbox.

## Ciclo, salário e Imposto de Renda

- O salário é pago antes do movimento, após o número de turnos próprios
  concluídos definido pela dificuldade:
  - Fácil: a cada 2 turnos;
  - Médio: a cada 3 turnos;
  - Difícil: a cada 4 turnos.
- A alíquota de Imposto de Renda também depende da dificuldade:
  - Fácil: 10%;
  - Médio: 20%;
  - Difícil: 25%.
- O Imposto de Renda é arredondado para cima e transferido diretamente ao
  Cofre Público.

## Sonegação e auditoria

- A tentativa de sonegação é opcional e pode ocorrer uma vez por ciclo.
- Resultado 1 ou 6 no dado: a tentativa tem sucesso e o jogador recebe um risco
  fiscal.
- Resultado 2 a 5: o jogador é pego, paga o Imposto de Renda devido e multa 2.
- Ao acumular dois riscos fiscais, ocorre auditoria na próxima casa de imposto:
  pagamento do IR devido mais 2, perda de 1 Cidadania e zeragem dos riscos.

## Cofre Público

- Ao passar pelo canto do Cofre, a contribuição escolhida é 0, 1 ou 2.
- Quem contribui recebe 1 Cidadania.
- O Cofre financia prevenção coletiva, rede de apoio e Hospital público segundo
  os limiares e custos do ruleset.
- Saldo e movimentações do Cofre devem permanecer transparentes e auditáveis.

## Hospital

- A entrada ocorre pelo canto Hospital ou quando Toxina/Cortisol alcança 6.
- Tratamento privado: custo 6 para o Banco, perda de 1 turno e redução total de
  2 distribuída entre Toxina e Cortisol. A Médica paga 3.
- Tratamento público: custo 4 retirado do Cofre Público, perda de 2 turnos e
  redução de 1.
- Espera: após 3 turnos aplica-se o resultado previsto pelo modo de dificuldade.
- Fácil e Médio não eliminam o jogador em resultado crítico.
- A morte/eliminação individual no modo Difícil ainda exige validação pedagógica
  e não pode ser ativada como regra final sem decisão explícita.

## Virada e Consequência

- O jogador escolhe descansar, reduzindo 1 Cortisol, ou pagar 2 e lançar o dado.
- Com resultado 6, recebe 12 e ganha 2 Cortisol.
- Nos demais resultados, perde o valor pago e ganha 1 Cortisol.

## Vitória

- O primeiro jogador a alcançar o centro aciona o encerramento.
- Todos os demais concluem o turno atual antes do resultado final.
- Desempate: menor soma de Dívidas + Cortisol + Toxina.
- Persistindo o empate, vence quem tiver maior Cidadania.
- Dinheiro não é critério de desempate.

## Regras de implementação

- Regras de domínio não dependem de `UnityEngine`.
- Atributos, casas, personagens, eventos e balanceamento são data-driven.
- ScriptableObjects são definições; nunca estado mutável da sessão.
- Animações representam um resultado já aplicado no domínio.
- IDs internos são estáveis e não dependem de textos de interface.
- Uma regra ambígua deve continuar configurável e ser registrada como
  `DECISION_REQUIRED`, nunca inferida pelo código.
