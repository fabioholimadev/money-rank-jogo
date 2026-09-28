# AGENTS.md — Money Rank Unity

Este arquivo define como agentes de código (especialmente Codex) devem atuar no projeto Money Rank.

## Contexto obrigatório

Antes de uma tarefa estrutural, leia:

1. `Docs/MONEY_RANK_UNITY_SPEC.md`
2. `Docs/GAME_RULES.md`
3. `Docs/OPEN_DECISIONS.md`
4. `Docs/ARCHITECTURE.md`, quando existir

## Regra nº 1

**Não invente regras de gameplay.**

Se a regra estiver ambígua, provisória ou contraditória:

- implemente de forma configurável;
- registre `DECISION_REQUIRED`;
- adicione a pendência em `Docs/OPEN_DECISIONS.md`;
- explique no resumo final.

## Arquitetura

- `Domain` não depende de `UnityEngine`.
- Lógica de regra não deve ficar dentro de View/MonoBehaviour.
- ScriptableObjects são definições, não estado mutável da sessão.
- Eventos, personagens, atributos, espaços e balanceamento devem ser data-driven.
- Animações representam o estado; não são a fonte da regra.
- Não crie Singleton global sem justificativa arquitetural.

## Unity

Projeto alvo:

- Unity 6+
- URP
- Windows primeiro
- offline-first

Ao alterar cenas/prefabs/referências serializadas, prefira operações seguras pelo Editor/Unity Pipeline a editar YAML manualmente.

## Unity CLI / Pipeline

Antes de assumir comandos disponíveis:

```powershell
unity status
unity command
unity list
unity commands --format json
```

Setup típico:

```powershell
unity auth login
unity pipeline install
unity pipeline list
```

**Nunca invente nome ou schema de comando do Pipeline.** Descubra o comando e os parâmetros primeiro.

## Workflow por tarefa

1. entender o pedido;
2. identificar regra/decisão afetada;
3. implementar mudança mínima;
4. adicionar/atualizar testes;
5. compilar;
6. verificar Console/logs;
7. rodar testes relevantes;
8. validar cenas/prefabs quando afetados;
9. atualizar docs se necessário;
10. entregar resumo com arquivos alterados, testes e pendências.

## Definition of Done

Uma tarefa só está pronta quando, quando aplicável:

- compila;
- testes passam;
- não cria nova exceção relevante;
- não hardcoda regra configurável;
- não inventa regra de gameplay;
- mantém save/versionamento coerente;
- documentação permanece consistente.

## Convenções

- código e identificadores em inglês;
- textos de UI podem ser em português;
- IDs de conteúdo são estáveis e não dependem do texto visível;
- commits pequenos e focados;
- não criar pastas `Misc`, `Old`, `New` ou equivalentes.

## Prioridade do MVP

1. GameState / máquina de estados
2. turnos
3. board externo
4. movimento
5. engine de eventos
6. atributos/progressão interna
7. economia
8. save/load/undo
9. UX do mestre
10. polish 3D
11. integrações opcionais (Arduino, QR, celulares)

## Fora do escopo inicial

Não priorizar:

- multiplayer online;
- nuvem;
- autenticação;
- ranking global;
- Steam;
- matchmaking;
- hardware automático sem protocolo aprovado.

