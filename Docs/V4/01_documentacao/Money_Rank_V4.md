# MONEY RANK | V4
## Especificação de regras e manual de referência
**25 de novembro de 2026 | Versão de pré-testes | Educação financeira, fiscal e cidadania**

Este documento consolida as decisões da revisão V4 e os dados necessários à partida físico-digital e online. As regras aprovadas prevalecem sobre o handoff AS-IS. Valores ainda não testados são parâmetros da primeira bateria, não resultados de validação. Os complementos de implementação explicitados na seção 15 não são aprovações expressas; foram escolhidos para tornar o protótipo executável e podem ser alterados após os pré-testes.

## 1. Objetivo e componentes
O Money Rank integra um percurso externo de decisões econômicas e um Ludo interno de progressão por atributos. A capivara representa a profissão no percurso externo; os quatro peões de uma mesma cor representam seu avanço interno. O objetivo é concluir os quatro peões sequencialmente, e não acumular a maior quantidade de dinheiro. As decisões de consumo, patrimônio, formação, saúde e tributação afetam a trajetória.

Kit: tabuleiro quadrado com 36 casas externas (4 extremidades e quatro setores de 8), Ludo central, até 4 capivaras externas, 16 peões internos de quatro cores (verde, vermelho, amarelo, azul), dados, moeda fiscal de duas faces, 360 moedas físicas e computador/dispositivo do mestre. São seis profissões disponíveis. A referência física é a foto e o layout em 04_referencias. A versão normal admite 2–4 jogadores; modo solo é apenas demonstração/teste, como complemento técnico.

## 2. Modalidades
**Local físico-digital:** o mestre registra resultados reais de dados, moeda, escolhas e correções. O aplicativo é a fonte oficial dos saldos e regras; as peças e moedas físicas refletem esse estado. O mestre move/valida as peças, e o app apresenta animação, próximos passos e consequências. A conta do cofre público é enviada ao Arduino exclusivamente para exibir o saldo. A partida continua sem o display se houver desconexão. O mestre vê as pendências fiscais; o painel coletivo não as divulga.

**Online:** sala por código/QR, jogadores controlam seus personagens, motor calcula resultados e sincroniza o estado. Não existe mestre humano obrigatório. Todos podem visualizar os indicadores e situação fiscal dos demais. Um anfitrião técnico não equivale ao mestre pedagógico. Dados, moeda e decisões são validados pela autoridade da sessão.

## 3. Preparação, turno, rodada e ano
Escolher quantidade de jogadores, modalidade, dificuldade, bateria de configuração, profissão (sem repetição por padrão), cor e ordem. Todos começam na casa H01, com 500 UMR, seis atributos zerados, quatro peões na base, salário da profissão no nível inicial, nenhum imóvel/veículo/reserva, sem pendências. Banco recebe o restante das moedas; cofre começa em zero como valor de referência. As cores são independentes das profissões.

Turno: verificar hospitalização ou habilidade alternativa; lançar um dado de seis faces (1–6; regra operacional proposta); avançar no percurso circular externo; executar a casa de chegada; confirmar escolha e dinheiro; aplicar atributos; resolver progressão interna, captura e internação; verificar vitória; avançar ordem. Passar por uma casa não dispara efeitos. A chegada ao hospital comum é neutra para quem não está internado; Direto para o Hospital interna imediatamente. Passar pelo início não paga salário nem inicia um ano.

Rodada é o ciclo de um turno para cada participante ativo, incluindo turnos perdidos por internação. Três rodadas completas constituem um ano global. Após a terceira, pagar todos os salários, resolver IR anual de cada jogador, processar efeitos de fechamento (reserva, parcerias, expiração de veículo), renovar habilidades e iniciar o ano seguinte. Internados continuam recebendo salário. Eliminados deixam de receber. Não há custo de vida automático: despesas surgem apenas nas casas e eventos. Não há pagamento por volta do percurso.

## 4. Atributos, multiplicadores e movimento
Positivos: Conhecimento, Investimento (inclui casa, carro e reserva), Cidadania. Negativos: Toxina, Cortisol, Dívidas. Não existe sétimo atributo de Reserva ou pendência fiscal. Todos possuem barras de três pontos. Ao completar barra positiva: avanço de uma casa interna; ao completar barra negativa: recuo de uma casa. O contador de ciclos é separado do restante da barra. Cada três pontos consumidos gera um ciclo; excedentes de ganhos são preservados para a próxima barra. Reduções operam apenas na barra atual, até zero, sem desfazer ciclos nem guardar crédito. Valores-base de redução não ultrapassam três; ganhos superiores (ex.: quatro de Investimento do carro) são exceções aprovadas e processam os ciclos naturalmente.

A quantidade de ganho é multiplicada pelo fator da profissão e da dificuldade. Usar decimal exato, sem arredondar atributo a cada ação; mostrar a fração na interface. Complemento: reduções não recebem multiplicador, para evitar inverter o significado de dificuldades e garantir que descanso reduza exatamente um ponto. Bônus de deslocamento alteram passos, não pontos. Dívidas é apenas um marcador: não existem contratos de empréstimo, juros de dívida, parcelas, saldo financeiro de dívida nem vencimentos de crédito. Pendências fiscais são exceção administrativa, separadas e com valor fiscal original registrado.

Internação: completar três ciclos de Toxina OU três ciclos de Cortisol desde a última alta (não somar os dois atributos; dois ciclos de cada não internam). Cada ciclo negativo já gera recuo, inclusive o terceiro; não há recuo adicional pela internação. Dívidas nunca interna. Recuperação comum reduz só a barra atual, não o contador de ciclos para hospitalização. Alta zera barras e ciclos de Toxina e Cortisol; preserva Dívidas, atributos positivos, dinheiro e patrimônio.

## 5. Ludo e captura
Quatro peões por jogador, estritamente em ordem. Só um peão em percurso por vez; o primeiro avanço positivo retira o peão ativo da base. Quando ele chega ao objetivo, fica concluído e o próximo fica elegível a partir de outra concessão de movimento. Excedente do avanço que conclui um peão é descartado, mesmo com carro; não passa ao próximo. Se vários ciclos positivos ocorrerem no mesmo evento, processá-los como concessões distintas, em ordem definida no handoff. Recuo nunca reabre peão concluído, nem reduz posição abaixo da base.

O percurso interno utiliza a grade física de Ludo, não a trilha experimental de seis posições existente no Unity. Proposta técnica de geometria: 52 casas compartilhadas em sentido horário, corredores finais privativos de cada cor e caminho até o centro; definir rotas a partir da arte física, com coordenadas explicitadas no handoff. A entrada é específica da cor. Não é necessário tirar seis no dado para sair da base: o movimento vem de atributos. A chegada exata não é obrigatória; sobras são perdidas. Ao parar em casa comum ocupada por adversário, captura. Sem checkpoint alcançado, o adversário volta à base; com checkpoints próprios, retorna ao mais avançado que já atingiu (não pode avançar por captura). Peões concluídos e corredores finais privativos não são capturáveis. Casas protegidas impressas com estrela são zonas seguras por proposta operacional. Captura não é regressão normal; o checkpoint só protege do retorno por captura, não de recuo de atributos. Checkpoints são direitos pessoais, permanentes e reutilizáveis, comprados por imóvel; não protegem outros jogadores. O peão capturado continua ativo; outros não saem até ele concluir. Vence quem concluir os quatro peões. No difícil, se restar um único participante não eliminado, ele vence por sobrevivência como regra complementar; se nenhum restar, encerra sem vencedor.

## 6. Economia física e circulação
UMR é a moeda fictícia. Estoque confirmado: 60 moedas de 1, 80 de 5, 80 de 10, 60 de 50, 40 de 100, 40 de 500. Total: 360 moedas e 28.260 UMR. Moeda de 20 foi mencionada como hipótese, mas não consta no inventário; não incluída. Todos os valores monetários fracionários devem ser arredondados para cima em UMR inteiras, usando ceil. Não há criação de moeda durante a partida.

Banco: paga salários, retornos de parcerias, trabalho extra e bolsas, recebe despesas, compras, progressões e tratamento. Cofre público: recebe impostos e multas, financia exclusivamente o hospital público, repassando o custo ao banco. Jogador pode manter carteira e reserva depositada sob custódia bancária; a reserva é passivo do banco, não dinheiro adicional em circulação. O aplicativo deve acompanhar saldo, troco e compromissos. Resgates transferem dinheiro do banco à carteira e reduzem o saldo de reserva. Nenhum saldo monetário fica negativo; não criar moedas, nem lançar créditos fictícios para encobrir insuficiência do banco.

Salários anuais-base: Professora 250; Médica 700; Economista 500; Atleta 300; Empreendedora 350; Motorista 400 UMR. Com quatro profissões de maior salário, o banco inicia com 26.260 UMR (cofre zero, 500 por pessoa), folha 1.950 UMR, 13 pagamentos anuais completos sem receitas ou outras saídas. Isso não prova sustentabilidade com bônus e progressões. Com todos esses quatro no nível 4, folha de 3.510 UMR. Testar cenários de acúmulo, reserva e bônus, além da disponibilidade de trocos; não prometer liquidez infinita.

## 7. Tributação, moeda e pendências
IR anual: 10% no Fácil, 20% no Médio, 25% no Difícil; base = salário bruto anual vigente. Recebe salário, depois decide pagar ou sonegar. Casas de tributo fixas: IPTU 50 UMR por imóvel permanente; IPVA 40 UMR apenas com carro ativo; ISS 30 UMR e ICMS 40 UMR para qualquer jogador que cair. Essas são simplificações pedagógicas, não incidências ou alíquotas reais brasileiras. IOF e IR deixam de ser casas; a fiscalização ocorre nas casas de imposto, preservando a mecânica anterior.

Pagar voluntariamente: transfere o tributo ao cofre (concessão proposta de +1 Cidadania por pagamento voluntário, configurável). Tentar sonegar: moeda equilibrada, capivara feliz não paga agora e registra pendência fiscal; capivara triste paga 2 vezes o tributo e ganha +1 Cortisol. Quando houver pendência anterior e o jogador cair em casa de imposto, realiza fiscalização com moeda antes de resolver o novo tributo. Se feliz, mantém pendências; se triste, paga dobro dos principais ainda não autuados, +1 Cortisol e regulariza os registros pagos. Não há juros automáticos. Pendências anteriores são rastreáveis no local apenas pelo mestre e visíveis no online. Se não tiver recursos (incluindo possibilidade de resgate), +3 pontos-base de Dívidas e registra pendência; sem obrigar a sonegar. Se não conseguir pagar uma multa, preservar obrigação autuada sem multiplicá-la indefinidamente; regra complementar detalhada no handoff. O dinheiro efetivamente arrecadado entra apenas no cofre.

## 8. Hospital
Há um único hospital físico. A internação ocorre por H19 ou três ciclos de um atributo de saúde individual. Tratamento público custa 150 UMR do cofre, com dois turnos de afastamento. Particular custa 250 UMR da carteira do jogador (reserva pode ser resgatada), com um turno de afastamento. Médica: uma vez por ano, particular por 125 UMR; redução de um turno, mínimo um. Pagar o tratamento transfere para o banco, não imobiliza moedas no hospital. Internado perde as jogadas de movimento, mas recebe salário anual e participa da tributação. Ao consumir todos os turnos perdidos, recebe alta e zera Toxina e Cortisol.

Sem recursos em nenhuma modalidade: Fácil permite recuperação emergencial em 3 turnos sem custo; Médio em 4 turnos e +2 Dívidas base; Difícil elimina. Valores e turnos são configurações de pré-teste; eliminação no Difícil foi aprovada como princípio. Para o protótipo, avaliar capacidade financeira no momento da internação após oferecer resgate; se optar por público com cofre insuficiente, apresentar particular se acessível; só usar emergência quando nenhuma modalidade for acessível.

## 9. Investimentos e patrimônio
**Reserva de emergência (H05):** depósito de 50/100/200 UMR, respectivamente +1/+2/+3 Investimento base, desde que caiba no teto igual ao salário atual. Entrega moedas ao banco. Rendimento de 10% composto a cada fechamento anual; ceil do rendimento; saldo final limitado ao salário atual. Quando atingir o teto, não rende mais até que o salário aumente ou haja resgate. Resgatar parte/tudo no próprio turno sem custo de tempo, para qualquer finalidade. Complemento antiabuso: registrar principal líquido depositado e maior principal líquido histórico; juros nunca contam para recorde. Pontos de um depósito são concedidos apenas proporcionalmente à parte que exceder o recorde (pontos base × parcela nova / valor depositado). Ao resgatar, abater principal primeiro (sem tornar negativo). Isso evita reciclar moedas e preserva benefício de novos depósitos.

**Veículo (H14):** compra opcional de 400 UMR, +4 Investimento e benefício de dobrar avanço por barra positiva durante três anos (9 rodadas completas). Recuos permanecem unitários. IPVA enquanto ativo. Se já possui carro ativo, oferecer manutenção preventiva por 150 UMR, +1 Investimento e prorrogação de 1 ano; não acumular multiplicadores. Se vencido, oferecer nova compra com os efeitos iniciais. Pode recusar. Prazo medido em rodadas completas a partir da aquisição, não pela quantidade de passagens em H01; complementar: armazenar rodadas restantes e não decrementar no fechamento da rodada de compra. Não há revenda prevista. A compra vale para todos os peões. A ordem de ativação do bônus antes do ganho de Investimento é uma escolha de execução explicitada no handoff.

**Imóvel (H20):** compra opcional de 500 UMR, +4 Investimento, cada compra concede um direito de instalar checkpoint permanente. Pode adquirir vários imóveis. O direito é guardado e pode ser usado uma única vez na posição atual válida do peão ativo, durante seu turno; outros direitos continuam disponíveis. Marcar cada checkpoint para a cor/jogador e aplicar o mais avançado já atingido em futuras capturas. IPTU 50 por imóvel. Não expira nem pode ser vendido nesta versão.

**Investimento Livre (H34):** substitui Aplicação Financeira. Selecionar Reserva, Veículo ou Imóvel; delegar às mesmas opções e regras, permitir apenas uma operação ou recusar. Não existe uma aplicação financeira adicional com prazo e risco.

## 10. Progressão de Vida
As quatro casas de Progressão (H08, H16, H24, H29) têm ícones temáticos distintos, porém ofertam a mesma próxima carta profissional sequencial. Cada uma das seis profissões tem quatro evoluções, totalizando 24 cartas, cinco níveis incluindo inicial. Custos: 50%, 75%, 100%, 125% do salário inicial, arredondados para cima. Cada evolução acrescenta 20% do salário inicial (não composto). Após quatro, salário = 180% do inicial. Pontos positivos por etapa: 2, 3, 4, 5 distribuídos, no máximo três por atributo por carta antes dos multiplicadores. Não é exigido nível de Conhecimento além do custo nesta versão. Recusar ou não ter dinheiro mantém a mesma oferta até próxima casa. Depois da quarta, casa é neutra.

Salários finais: Professora 450; Médica 1.260; Economista 900; Atleta 540; Empreendedora 630; Motorista 720 UMR. Os títulos e a distribuição aprovados constam nas 24 cartas de `progressao.csv`, reproduzidos no anexo. A narrativa representa desenvolvimento da profissão, não títulos ou rendimentos reais garantidos. Progressão profissional não equivale à compra de patrimônio: a melhoria de veículo narrada na carreira do motorista não ativa carro nem IPVA.

## 11. Categorias de casas
Essenciais: três alternativas pagas fixas; escolha obrigatória entre as acessíveis; se nenhuma for acessível, +3 Dívidas base, sem benefício. Não essenciais: três pagas com benefícios; recusar livremente sem penalidade. Dívidas: cartão, crédito simbólico, conta atrasada e renegociação; mudanças de atributo, sem contrato de empréstimo. Renegociação com barra atual de Dívidas zero é neutra. Lazer: descanso automático gratuito reduz 1 Toxina e 1 Cortisol; outras casas apresentam três opções pagas e recusa livre, sem mensalidade. Férias concede Conhecimento a partir da escolha intermediária. Oportunidades: três opções fixas, sem sorteio, podem combinar renda e custos em atributos. Parceria exige custo inicial e garante retorno total no próximo fechamento anual, com Cortisol/Toxina imediatos; não tem sorte nem multiplicador de retorno. Trabalho Extra paga 25/50/75% do salário vigente imediatamente, sem mudar salário. Bolsa Pública paga 50/100/150% do salário atual e efeitos descritos nas cartas. Todos podem recusar oportunidades sem penalidade. Não há novas casas aleatórias por categoria: cada casa mantém seu tema, exceto as quatro de Progressão, que compartilham a função de carreira.

## 12. Profissões e habilidades
A tabela completa de multiplicadores está em `personagens.csv`. Todas as habilidades especiais são anuais, com no máximo um uso por ano (regras automáticas de primeira barra contam como uso). Professora: Domínio do Conhecimento, primeira barra de Conhecimento do ano avança duas casas. Médica: Atendimento Especializado, tratamento particular pela metade e redução de turno (mínimo um). Economista: Planejamento Estratégico, primeira barra de Investimento do ano avança duas casas. Atleta: Fôlego Extra, uma vez ao completar barra positiva, escolhe duas casas em vez de uma ou reduzir um ponto-base de atributo negativo. Empreendedora: Visão de Negócios, uma vez por ano evita um evento financeiro desfavorável elegível, sem afetar impostos ou penalidades já adquiridas. Como as cartas agora são fixas, o complemento de execução troca a antiga instrução de sortear outra carta pela escolha de outra opção válida da mesma casa, antes de confirmar, sem repetir o benefício nem escapar de obrigação (ver handoff; precisa de pré-teste). Motorista: Flexibilidade de Jornada, uma vez por ano troca a jogada por redução de dois pontos-base de Cortisol ou Toxina, preservando salário. Benefício do carro e habilidades que dobram passos não se multiplicam entre si no protótipo: maior avanço permitido é dois por ciclo; essa escolha de compatibilidade deve ser testada.

## 13. Dificuldades e baterias de pré-testes
| Bateria | Fácil positivo / negativo | Médio | Difícil positivo / negativo |
|---|---|---|---|
| 1 | 2 / 0,5 | 1 / 1 | 0,75 / 1,5 |
| 2 | 2,5 / 0,25 | 1 / 1 | 0,75 / 1,5 |

Cada bateria inclui três partidas de pré-teste (Fácil, Médio e Difícil), não três dificuldades diferentes em uma mesma partida. A primeira é a linha de base; a segunda é derivada dos dados obtidos na primeira. A configuração da segunda acima é hipótese pré-planejada; pode ser ajustada com registro de versão e justificativa antes da execução. Nunca alterar a dificuldade no meio de uma partida. IR 10/20/25%, hospital conforme seção 8. Registrar quantidade de jogadores, profissões, tempo, turnos, ciclos, capturas, progressões, patrimônio, saúde, saldo mínimo do banco/cofre, ocorrência de falta de troco, impostos pagos/sonegados, multas, resultado e ocorrências técnicas. Comparar a duração e a taxa de vitória por profissão, não declarar impacto educacional com seis partidas. Para evidências de aprendizagem, usar instrumentos pré/pós separados, consentimento conforme protocolo aplicável e dados pseudonimizados.

## 14. Implementação e interfaces
O mestre não calcula efeitos manualmente. Ele registra escolhas e resultados físicos, visualiza prévia e confirma; o sistema aplica automaticamente. A interface deve mostrar jogador atual, rodada/ano, saldos, profissão, nível, seis barras e contadores, patrimônio, peão ativo, pendências (restritas localmente), histórico e ferramentas de correção. O modo online compartilha o motor, sem painel administrativo. O Arduino apenas exibe o saldo do cofre; não calcula regras. Visual: ambientação tridimensional sem fundo cinza genérico, capivaras melhores se houver modelo licenciado, quatro peões de xadrez estilizados por cor, ícones alinhados, modelos de hospital e cofre, cartas legíveis e identidade consistente. A IA desenvolvedora pode decidir estética e buscar recursos licenciados, não modificar regras aprovadas.

## 15. Decisões complementares e limites da versão
As seguintes decisões são defaults operacionais, não aprovações individuais: jogo de 2–4 com solo de demonstração; um D6; cofre inicial zero; visita comum ao hospital neutra; contribuição voluntária de 50 sem pontos; desempate por ordem de resolução, vitória por sobrevivência e ausência de vencedor se todos eliminados; redução sem multiplicadores; pontos fracionários preservados; regra antiabuso da reserva proporcional ao novo principal; proteção de estrelas e mapeamento de Ludo; não empilhar carro com habilidade de avanço; prazo do carro contado por rodada; ordem simultânea de efeitos; tratamento imediato no ingresso; ajustes da habilidade da Empreendedora a casas fixas; prêmio de Cidadania por tributo voluntário; lidar com autuação sem fundos; imposto anual apenas sobre salário; pagamento de parceria e rendimento no fechamento; posição H25 ISS no antigo IOF, H32 ICMS no antigo IR (escolha de mapeamento); detalhes de preços das despesas essenciais são propostas da conversa para pré-teste. O Economista foi recuperado de proposta anterior, sem confirmação individual explícita no trecho disponível; aplicou-se a regra aprovada de periodicidade anual. O arquivo `casas.csv` e os anexos são a referência de conteúdo; o projeto Unity não foi alterado nem testado nesta entrega. A V3 integral do Drive não foi reaberta nesta execução; a revisão se baseou nas decisões registradas, foto, faixas e auditoria AS-IS anexada. Não presumir que esta versão foi homologada ou que modelos 3D externos foram obtidos.

## 16. Mapa das casas e cartas
O mapa abaixo segue o layout de referência, com Início no canto superior esquerdo e sentido horário; na foto física o tabuleiro está girado. Os cantos são H01, H10, H19 e H28. Os quatro ícones substitutos estão no pacote visual. A posição de cada casa é numerada de 1 a 36; no Unity o índice é posição menos um. Não confundir cores das categorias com cores dos peões.

### 16.1. Mapa numerado e alternativas

### H01 — Início
Casa de referência; sem salário por passagem ou chegada.

- **Início** — sem valores fixos adicionais. Casa de referência: não há pagamento extra por passagem ou chegada.

### H02 — Compra por impulso
Apresenta alternativas fixas; aplica exatamente uma; recusa conforme categoria.

- **Pequeno agrado** — custo 30 UMR; Cortisol -1. 
- **Compra de conforto** — custo 60 UMR; Cortisol -2. 
- **Compra útil planejada** — custo 90 UMR; Conhecimento +1; Cortisol -1. 
- **Recusar** — sem valores fixos adicionais. Não contratar ou comprar. Sem efeitos.

### H03 — Curso gratuito
Apresenta alternativas fixas; aplica exatamente uma; recusa conforme categoria.

- **Oficina introdutória** — Conhecimento +1. Capacitação gratuita, sem custo em moedas.
- **Curso de capacitação** — Conhecimento +2; Cortisol +1. Capacitação gratuita, sem custo em moedas.
- **Formação intensiva** — Conhecimento +3; Cidadania +1; Cortisol +2. Capacitação gratuita, sem custo em moedas.
- **Recusar** — sem valores fixos adicionais. Não contratar ou comprar. Sem efeitos.

### H04 — Alimentação
Apresenta alternativas fixas; aplica exatamente uma; recusa conforme categoria.

- **Alimentação econômica** — custo 30 UMR; Toxina +1. 
- **Refeição equilibrada** — custo 50 UMR. 
- **Alimentação saudável** — custo 80 UMR; Toxina -1. 
- **Não consegue pagar** — Dívidas +3. Nenhuma alternativa é acessível. Registra dificuldade financeira.

### H05 — Reserva de emergência
Deposita no banco; teto = salário atual; rendimento anual 10%; resgate imediato.

- **Depositar 50 UMR** — custo 50 UMR; Investimento +1. Entregue moedas ao banco e aumente a reserva. Ganho de Investimento depende do recorde de principal depositado.
- **Depositar 100 UMR** — custo 100 UMR; Investimento +2. Entregue moedas ao banco e aumente a reserva. Ganho de Investimento depende do recorde de principal depositado.
- **Depositar 200 UMR** — custo 200 UMR; Investimento +3. Entregue moedas ao banco e aumente a reserva. Ganho de Investimento depende do recorde de principal depositado.
- **Recusar** — sem valores fixos adicionais. Não contratar ou comprar. Sem efeitos.

### H06 — Descanso
Recupera 1 Toxina e 1 Cortisol sem custo.

- **Descansar em casa** — Toxina -1; Cortisol -1. Recupere um ponto de Toxina e Cortisol, até zero.

### H07 — IPTU
Calcula tributo devido, verifica pendências anteriores e oferece pagar/sonegar; sem tributo se isento.

- **Pagar imposto** — tributo-base 50 UMR por imóvel. Pague o tributo devido ao cofre público.
- **Tentar sonegar** — tributo-base 50 UMR por imóvel. Lance a moeda: feliz registra pendência; triste cobra o dobro e acrescenta Cortisol.
- **Sem recursos** — tributo-base 50 UMR por imóvel; Dívidas +3. Se não puder pagar o tributo, registre pendência e três pontos-base de Dívidas.
- **Isento nesta casa** — sem valores fixos adicionais. Não possui patrimônio tributável correspondente.

### H08 — Qualificação
Exibe próxima carta exclusiva da profissão; 4 etapas; recusa preserva oferta.

- **Próxima evolução profissional** — sem valores fixos adicionais. Consulte a próxima carta da sua profissão; pode comprar ou recusar. A oferta permanece até aquisição.
- **Carreira completa** — sem valores fixos adicionais. Você já concluiu as quatro evoluções.

### H09 — Cartão de crédito
Apresenta alternativas fixas; aplica exatamente uma; recusa conforme categoria.

- **Pagar a fatura integralmente** — custo 100 UMR; Dívidas -2. 
- **Pagar parcialmente** — custo 50 UMR; Dívidas +1. 
- **Não pagar a fatura** — Dívidas +3. 

### H10 — Hospital
Visita neutra se não estiver internado (complemento).

- **Visitar hospital** — sem valores fixos adicionais. Se não estiver internado, siga normalmente.

### H11 — IPVA
Calcula tributo devido, verifica pendências anteriores e oferece pagar/sonegar; sem tributo se isento.

- **Pagar imposto** — tributo-base 40 UMR. Pague o tributo devido ao cofre público.
- **Tentar sonegar** — tributo-base 40 UMR. Lance a moeda: feliz registra pendência; triste cobra o dobro e acrescenta Cortisol.
- **Sem recursos** — tributo-base 40 UMR; Dívidas +3. Se não puder pagar o tributo, registre pendência e três pontos-base de Dívidas.
- **Isento nesta casa** — sem valores fixos adicionais. Não possui patrimônio tributável correspondente.

### H12 — Atividade física
Apresenta alternativas fixas; aplica exatamente uma; recusa conforme categoria.

- **Prática esportiva** — custo 30 UMR; Toxina -2. 
- **Academia** — custo 60 UMR; Toxina -2; Cortisol -2. 
- **Treinamento orientado** — custo 100 UMR; Toxina -3; Cortisol -3. 
- **Recusar** — sem valores fixos adicionais. Não contratar ou comprar. Sem efeitos.

### H13 — Empréstimo
Apresenta alternativas fixas; aplica exatamente uma; recusa conforme categoria.

- **Não contratar** — sem valores fixos adicionais. Não recebe dinheiro nem altera os atributos.
- **Crédito simbólico pequeno** — recebe 50 UMR; Dívidas +2. Receba 50 UMR; aumente Dívidas. Não há contrato ou parcelas.
- **Crédito simbólico maior** — recebe 100 UMR; Dívidas +4. Receba 100 UMR; aumente Dívidas. Não há contrato ou parcelas.

### H14 — Veículo
Compra se não possui ativo; manutenção se possui; pode recusar.

- **Comprar veículo** — custo 400 UMR; Investimento +4. Adquira um veículo por três anos. Avanços positivos internos dobram enquanto ativo.
- **Manutenção preventiva** — custo 150 UMR; Investimento +1. Prolongue em um ano a validade do veículo ativo.
- **Recusar** — sem valores fixos adicionais. Não contratar ou comprar. Sem efeitos.

### H15 — Moradia
Apresenta alternativas fixas; aplica exatamente uma; recusa conforme categoria.

- **Moradia básica** — custo 50 UMR; Cortisol +1. 
- **Moradia adequada** — custo 80 UMR. 
- **Moradia confortável** — custo 120 UMR; Cortisol -1. 
- **Não consegue pagar** — Dívidas +3. Nenhuma alternativa é acessível. Registra dificuldade financeira.

### H16 — Marco de carreira
Exibe próxima carta exclusiva da profissão; 4 etapas; recusa preserva oferta.

- **Próxima evolução profissional** — sem valores fixos adicionais. Consulte a próxima carta da sua profissão; pode comprar ou recusar. A oferta permanece até aquisição.
- **Carreira completa** — sem valores fixos adicionais. Você já concluiu as quatro evoluções.

### H17 — Assinaturas e aplicativos
Apresenta alternativas fixas; aplica exatamente uma; recusa conforme categoria.

- **Assinatura básica** — custo 30 UMR; Cortisol -1. 
- **Assinatura educacional** — custo 80 UMR; Conhecimento +1; Cortisol -1. 
- **Pacote completo** — custo 150 UMR; Conhecimento +2; Cortisol -2. 
- **Recusar** — sem valores fixos adicionais. Não contratar ou comprar. Sem efeitos.

### H18 — Trabalho extra
Apresenta alternativas fixas; aplica exatamente uma; recusa conforme categoria.

- **Trabalho pontual** — recebe 25% do salário vigente; Cortisol +1. Receba renda extra imediatamente, sem alterar seu salário.
- **Jornada extra** — recebe 50% do salário vigente; Cortisol +2. Receba renda extra imediatamente, sem alterar seu salário.
- **Projeto intensivo** — recebe 75% do salário vigente; Toxina +1; Cortisol +3. Receba renda extra imediatamente, sem alterar seu salário.
- **Recusar** — sem valores fixos adicionais. Não contratar ou comprar. Sem efeitos.

### H19 — Direto para o hospital
Envia ao hospital imediatamente e abre escolha de tratamento.

- **Internação imediata** — sem valores fixos adicionais. Vá diretamente ao hospital e selecione modalidade de atendimento.

### H20 — Imóvel
Compra opcional repetível; cada imóvel concede checkpoint.

- **Comprar imóvel** — custo 500 UMR; Investimento +4. Adquira propriedade permanente e um direito de posicionar checkpoint.
- **Recusar** — sem valores fixos adicionais. Não contratar ou comprar. Sem efeitos.

### H21 — Energia e água
Apresenta alternativas fixas; aplica exatamente uma; recusa conforme categoria.

- **Consumo mínimo** — custo 30 UMR; Cortisol +1. 
- **Consumo regular** — custo 50 UMR. 
- **Consumo eficiente** — custo 80 UMR; Cidadania +1. 
- **Não consegue pagar** — Dívidas +3. Nenhuma alternativa é acessível. Registra dificuldade financeira.

### H22 — Parceria
Apresenta alternativas fixas; aplica exatamente uma; recusa conforme categoria.

- **Parceria local** — custo 50 UMR; retorno anual 100 UMR; Cortisol +1. Pague agora; retorno total garantido no próximo fechamento anual.
- **Sociedade temporária** — custo 100 UMR; retorno anual 200 UMR; Toxina +1; Cortisol +2. Pague agora; retorno total garantido no próximo fechamento anual.
- **Negócio intensivo** — custo 150 UMR; retorno anual 300 UMR; Toxina +2; Cortisol +3. Pague agora; retorno total garantido no próximo fechamento anual.
- **Recusar** — sem valores fixos adicionais. Não contratar ou comprar. Sem efeitos.

### H23 — Tecnologia
Apresenta alternativas fixas; aplica exatamente uma; recusa conforme categoria.

- **Acessório útil** — custo 100 UMR; Conhecimento +1. 
- **Equipamento de estudo** — custo 200 UMR; Conhecimento +2. 
- **Equipamento completo** — custo 300 UMR; Conhecimento +2; Cortisol -1. 
- **Recusar** — sem valores fixos adicionais. Não contratar ou comprar. Sem efeitos.

### H24 — Projeto pessoal
Exibe próxima carta exclusiva da profissão; 4 etapas; recusa preserva oferta.

- **Próxima evolução profissional** — sem valores fixos adicionais. Consulte a próxima carta da sua profissão; pode comprar ou recusar. A oferta permanece até aquisição.
- **Carreira completa** — sem valores fixos adicionais. Você já concluiu as quatro evoluções.

### H25 — ISS
Calcula tributo devido, verifica pendências anteriores e oferece pagar/sonegar; sem tributo se isento.

- **Pagar imposto** — tributo-base 30 UMR. Pague o tributo devido ao cofre público.
- **Tentar sonegar** — tributo-base 30 UMR. Lance a moeda: feliz registra pendência; triste cobra o dobro e acrescenta Cortisol.
- **Sem recursos** — tributo-base 30 UMR; Dívidas +3. Se não puder pagar o tributo, registre pendência e três pontos-base de Dívidas.

### H26 — Cultura
Apresenta alternativas fixas; aplica exatamente uma; recusa conforme categoria.

- **Cinema** — custo 30 UMR; Conhecimento +1; Cortisol -1. 
- **Museu ou exposição** — custo 60 UMR; Conhecimento +2; Cidadania +1. 
- **Experiência cultural completa** — custo 100 UMR; Conhecimento +3; Cidadania +2; Cortisol -1. 
- **Recusar** — sem valores fixos adicionais. Não contratar ou comprar. Sem efeitos.

### H27 — Conta atrasada
Apresenta alternativas fixas; aplica exatamente uma; recusa conforme categoria.

- **Quitar conta atrasada** — custo 60 UMR; Dívidas -1. 
- **Pagamento parcial** — custo 30 UMR. 
- **Não pagar** — Dívidas +3. 

### H28 — Cofre público
Mostra saldo e finalidade exclusiva: tratamento público; contribuição voluntária proposta.

- **Consultar cofre público** — sem valores fixos adicionais. Confira o saldo acumulado, destinado exclusivamente ao hospital.
- **Contribuir voluntariamente** — custo 50 UMR. Transfira 50 UMR ao cofre, sem benefício adicional.
- **Recusar** — sem valores fixos adicionais. Não contratar ou comprar. Sem efeitos.

### H29 — Contribuição comunitária
Exibe próxima carta exclusiva da profissão; 4 etapas; recusa preserva oferta.

- **Próxima evolução profissional** — sem valores fixos adicionais. Consulte a próxima carta da sua profissão; pode comprar ou recusar. A oferta permanece até aquisição.
- **Carreira completa** — sem valores fixos adicionais. Você já concluiu as quatro evoluções.

### H30 — Renegociação
Se barra atual de Dívidas = 0, encerra sem efeito; caso contrário, oferece opções.

- **Renegociar integralmente** — custo 80 UMR; Dívidas -2. 
- **Renegociar parcialmente** — custo 40 UMR; Dívidas -1. 
- **Não renegociar** — Dívidas +2. 
- **Sem dívidas atuais** — sem valores fixos adicionais. Não pague nada e encerre sem efeito.

### H31 — Férias
Apresenta alternativas fixas; aplica exatamente uma; recusa conforme categoria.

- **Viagem regional** — custo 100 UMR; Toxina -1; Cortisol -2. 
- **Viagem nacional** — custo 150 UMR; Conhecimento +1; Toxina -2; Cortisol -3. 
- **Viagem internacional** — custo 200 UMR; Conhecimento +2; Toxina -3; Cortisol -3. 
- **Recusar** — sem valores fixos adicionais. Não contratar ou comprar. Sem efeitos.

### H32 — ICMS
Calcula tributo devido, verifica pendências anteriores e oferece pagar/sonegar; sem tributo se isento.

- **Pagar imposto** — tributo-base 40 UMR. Pague o tributo devido ao cofre público.
- **Tentar sonegar** — tributo-base 40 UMR. Lance a moeda: feliz registra pendência; triste cobra o dobro e acrescenta Cortisol.
- **Sem recursos** — tributo-base 40 UMR; Dívidas +3. Se não puder pagar o tributo, registre pendência e três pontos-base de Dívidas.

### H33 — Lazer premium
Apresenta alternativas fixas; aplica exatamente uma; recusa conforme categoria.

- **Passeio especial** — custo 80 UMR; Cortisol -1. 
- **Experiência premium** — custo 150 UMR; Cortisol -2. 
- **Experiência de bem-estar** — custo 200 UMR; Toxina -1; Cortisol -2. 
- **Recusar** — sem valores fixos adicionais. Não contratar ou comprar. Sem efeitos.

### H34 — Investimento livre
Escolhe uma operação de reserva, veículo ou imóvel; ou recusa.

- **Reserva de emergência** — sem valores fixos adicionais. Escolha uma operação já definida na casa correspondente.
- **Veículo** — sem valores fixos adicionais. Escolha uma operação já definida na casa correspondente.
- **Imóvel** — sem valores fixos adicionais. Escolha uma operação já definida na casa correspondente.
- **Recusar** — sem valores fixos adicionais. Não contratar ou comprar. Sem efeitos.

### H35 — Transporte e saúde
Apresenta alternativas fixas; aplica exatamente uma; recusa conforme categoria.

- **Transporte e cuidado básicos** — custo 30 UMR; Cortisol +1. 
- **Serviço regular** — custo 60 UMR. 
- **Transporte e prevenção** — custo 100 UMR; Toxina -1. 
- **Não consegue pagar** — Dívidas +3. Nenhuma alternativa é acessível. Registra dificuldade financeira.

### H36 — Bolsa pública
Apresenta alternativas fixas; aplica exatamente uma; recusa conforme categoria.

- **Auxílio estudantil** — recebe 50% do salário vigente; Conhecimento +1; Cortisol +1. Receba benefício financeiro imediato, com dedicação e esforço.
- **Bolsa integral** — recebe 100% do salário vigente; Conhecimento +2; Cortisol +2. Receba benefício financeiro imediato, com dedicação e esforço.
- **Edital de projeto** — recebe 150% do salário vigente; Conhecimento +2; Cidadania +1; Cortisol +3. Receba benefício financeiro imediato, com dedicação e esforço.
- **Recusar** — sem valores fixos adicionais. Não contratar ou comprar. Sem efeitos.

### 16.2. Multiplicadores das profissões
Os fatores incidem nos ganhos-base e são combinados com a dificuldade.

| Profissão | Salário | Conhec. | Invest. | Cidad. | Toxina | Cortisol | Dívidas |
|---|---:|---:|---:|---:|---:|---:|---:|
| Professora | 250 | 1,5 | 1 | 1,25 | 1 | 1 | 1,5 |
| Médica | 700 | 1,25 | 1 | 1 | 0,75 | 1,5 | 1 |
| Economista | 500 | 1 | 1,5 | 1 | 1 | 1,25 | 0,75 |
| Atleta | 300 | 1 | 0,75 | 1 | 0,75 | 0,75 | 1,5 |
| Empreendedora | 350 | 1 | 1,5 | 1,25 | 1 | 1,75 | 1 |
| Motorista | 400 | 1 | 1,25 | 1 | 1,5 | 0,75 | 1,25 |

### 16.3. Cartas de evolução por profissão

### Professora
| Etapa | Evolução | Custo UMR | Salário UMR | Conhec. | Invest. | Cidad. |
|---:|---|---:|---:|---:|---:|---:|
| 1 | Especialização | 125 | 300 | 2 | 0 | 0 |
| 2 | Mestrado | 188 | 350 | 2 | 0 | 1 |
| 3 | Doutorado | 250 | 400 | 3 | 0 | 1 |
| 4 | Referência acadêmica | 313 | 450 | 3 | 0 | 2 |

### Médica
| Etapa | Evolução | Custo UMR | Salário UMR | Conhec. | Invest. | Cidad. |
|---:|---|---:|---:|---:|---:|---:|
| 1 | Residência médica | 350 | 840 | 1 | 0 | 1 |
| 2 | Especialização | 525 | 980 | 2 | 0 | 1 |
| 3 | Subespecialização | 700 | 1120 | 3 | 0 | 1 |
| 4 | Referência médica | 875 | 1260 | 2 | 0 | 3 |

### Economista
| Etapa | Evolução | Custo UMR | Salário UMR | Conhec. | Invest. | Cidad. |
|---:|---|---:|---:|---:|---:|---:|
| 1 | Analista pleno | 250 | 600 | 1 | 1 | 0 |
| 2 | Analista sênior | 375 | 700 | 1 | 2 | 0 |
| 3 | Consultoria econômica | 500 | 800 | 2 | 2 | 0 |
| 4 | Direção estratégica | 625 | 900 | 2 | 3 | 0 |

### Atleta
| Etapa | Evolução | Custo UMR | Salário UMR | Conhec. | Invest. | Cidad. |
|---:|---|---:|---:|---:|---:|---:|
| 1 | Competição regional | 150 | 360 | 1 | 0 | 1 |
| 2 | Competição nacional | 225 | 420 | 0 | 1 | 2 |
| 3 | Competição internacional | 300 | 480 | 0 | 2 | 2 |
| 4 | Seleção nacional | 375 | 540 | 0 | 2 | 3 |

### Empreendedora
| Etapa | Evolução | Custo UMR | Salário UMR | Conhec. | Invest. | Cidad. |
|---:|---|---:|---:|---:|---:|---:|
| 1 | Expansão comercial | 175 | 420 | 0 | 2 | 0 |
| 2 | Consolidação regional | 263 | 490 | 1 | 2 | 0 |
| 3 | Expansão nacional | 350 | 560 | 1 | 3 | 0 |
| 4 | Grande empresa | 438 | 630 | 1 | 3 | 1 |

### Motorista
| Etapa | Evolução | Custo UMR | Salário UMR | Conhec. | Invest. | Cidad. |
|---:|---|---:|---:|---:|---:|---:|
| 1 | Aprimoramento profissional | 200 | 480 | 1 | 1 | 0 |
| 2 | Serviço de conforto | 300 | 560 | 1 | 2 | 0 |
| 3 | Serviço premium | 400 | 640 | 0 | 3 | 1 |
| 4 | Transporte executivo | 500 | 720 | 0 | 3 | 2 |

## 17. Alterações pós-V4 aprovadas pelo usuário (25/11/2026)
Estas regras foram pedidas pelo responsável do produto depois da V4 e **prevalecem sobre o texto anterior** onde houver conflito (seção 3: "lançar um dado de seis faces").
- **Dois dados para a movimentação.** O jogador lança dois dados de seis faces (físicos registrados pelo mestre, ou virtuais com animação no painel); o avanço no percurso externo é a **soma**. Continua valendo: passar por uma casa não dispara efeitos, apenas a de chegada.
- **Dados iguais jogam de novo.** Se os dois dados mostram o mesmo número, o jogador resolve a casa de chegada normalmente (inclusive decisões e efeitos) e **lança novamente**. Limite: **no máximo 3 jogadas extras** por turno (até 4 lançamentos); se vier novo par depois disso, a vez passa.
- A jogada extra **não ocorre** se o turno terminar com internação hospitalar, eliminação ou fim de partida.
- Parâmetros em `02_dados/parametros.json`: `dice_count` = 2, `doubles_extra_rolls_max` = 3 (configuráveis). Versão de regras: `4.1-pretest`.
- Interpretação registrada em `Docs/OPEN_DECISIONS.md`: "limitar a 3 vezes essa dinâmica" foi lido como **3 jogadas extras** (4 lançamentos no máximo); basta mudar `doubles_extra_rolls_max` se a intenção for outra.
