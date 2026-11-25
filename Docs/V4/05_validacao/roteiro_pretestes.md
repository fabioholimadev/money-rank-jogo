# Roteiro de pré-testes
Bateria 1: execute três partidas separadas, Fácil/Médio/Difícil. Use B1 em dificuldades.csv; mesma composição de jogadores e cenário quando possível; alterne ordem e profissões nas repetições para reduzir viés. Registre o tempo, saldo mínimo, tributos, hospitalizações, recuperação, capturas, progressão, quantidade de rodadas, vencedor, problemas físicos e UX. Não modifique parâmetros dentro de uma sessão.

Bateria 2: use B2 com Fácil +2,5/0,25 e Médio/Difícil inicialmente iguais. Compare resultados B1; se alterar valores, versionar configuração e justificar antes de jogar. Não trate apenas seis partidas como evidência estatística de aprendizagem. O modo local é a prioridade; online e hardware exigem homologação própria.

As linhas em `cenarios_aceite.csv` são critérios de teste, não resultados obtidos. Rodar EditMode, PlayMode, build Windows, testes reais no kit físico e, quando implementado, rede e Android. Manter logs, hash da build e evidência. Registrar liquidez/trânsito de moedas antes e depois; não imprimir moedas extras sem registrar a mudança de inventário.
