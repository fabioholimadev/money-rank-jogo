# Money Rank — proposta de montagem do display

Visualização conceitual de 17/11/2026, gerada pela ferramenta integrada de imagens. Não é projeto de fabricação, desenho em escala ou validação de encaixe. Som e LEDs não fazem parte desta proposta.

Imagem: `money-rank-oled-096-montagem-conceito-v1.png`.

## Evidências consultadas

- Arte do tabuleiro enviada pelo usuário: Ludo central e percurso externo preservados como referência visual. A geração de imagem pode apresentar diferenças gráficas; usar a arte original para impressão e os arquivos CAD para fabricação.
- `MoneyRank_ENCAIXE_LATERAIS_MDF_6mm.dxf`: unidade declarada milímetro; quatro contornos na camada CORTE, cada um com envelope de 490 × 50 mm e 21 vértices.
- `MoneyRank_FINAL_LATERAIS_MDF_6mm.dxf`: unidade declarada milímetro; dois retângulos de 498 × 50 mm e dois de 486 × 50 mm na camada CORTE.
- Vídeo `WhatsApp Video 2026-11-17 at 09.19.23.mp4`: cerca de 12,6 segundos; quadros em cinco momentos mostram montagem da moldura de MDF com encaixes.
- Link enviado: https://meli.la/1WD8CJz. O título recuperado identifica OLED 0,96 I2C SSD1306, apesar de também usar a palavra LCD. Não foram confirmadas dimensões mecânicas, tensão permitida ou pinagem desse anúncio.

## Proposta

Suporte pequeno, inclinado e removível sobre a faixa livre próxima ao Cofre, com passagem curta de quatro fios até o controlador sob o tampo. A posição é candidata; não define casa, regra ou função nova de gameplay. O valor 100 na imagem é apenas exemplo visual.

0,96 polegada corresponde a 24,384 mm de diagonal nominal da tela; não é medida da placa nem do recorte. Avaliação de projeto: testar leitura de perto e do outro lado da mesa antes de escolher este tamanho.

## DECISION_REQUIRED — montagem e hardware

- Confirmar qual versão das laterais é a utilizada e medir a estrutura montada.
- Medir altura útil sob o tampo, considerando base, tampo, suportes, conectores e cabos; os 50 mm de altura das laterais não garantem 50 mm livres.
- Confirmar módulo comprado: resolução, dimensões da placa e área ativa, furação, posição dos pinos, controlador, alimentação e níveis lógicos compatíveis com o Arduino escolhido.
- Testar legibilidade e aprovar posição antes de definir recorte e suporte.
- Definir como o controlador recebe o saldo; a ilustração não implementa integração com Unity nem atualização automática.

Os DXFs originais e o projeto Unity não foram alterados.
