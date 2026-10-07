using System;
using System.Linq;
using MoneyRank.Domain;
using MoneyRank.Presentation.Board;
using UnityEngine;

namespace MoneyRank.Presentation
{
    [DisallowMultipleComponent]
    public sealed class SandboxControlPanelView : MonoBehaviour
    {
        private static int _nextWindowId;

        [SerializeField] private Rect panelRect = new Rect(24f, 24f, 380f, 680f);
        [SerializeField] private string stepInput = "3";
        [SerializeField] private string progressionStepInput = "1";

        private BoardSandboxController _controller;
        private int _windowId;
        private Vector2 _scrollPosition;

        public string StepInput => stepInput;
        public string ProgressionStepInput => progressionStepInput;
        public string StatusMessage { get; private set; } = string.Empty;
        public bool IsBound => _controller != null;

        private void OnEnable()
        {
            if (_windowId == 0)
                _windowId = ++_nextWindowId;
        }

        public void Bind(BoardSandboxController controller)
        {
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));
            StatusMessage = controller.RestoredExistingSave
                ? "Sessão local restaurada."
                : "Nova sessão iniciada.";
        }

        public void SetStepInput(string value)
        {
            stepInput = value ?? string.Empty;
        }

        public void SetProgressionStepInput(string value)
        {
            progressionStepInput = value ?? string.Empty;
        }

        public void ReportStatus(string message)
        {
            StatusMessage = message ?? string.Empty;
        }

        public bool SubmitMove()
        {
            if (_controller == null)
                throw new InvalidOperationException("The sandbox control panel is not bound.");
            if (!int.TryParse(stepInput, out var steps) || steps < 0)
            {
                StatusMessage = "Informe uma quantidade inteira de passos igual ou maior que zero.";
                return false;
            }
            if (_controller.Session.Phase != GamePhase.AwaitMove)
            {
                StatusMessage = $"Movimento indisponível durante {_controller.Session.Phase}.";
                return false;
            }

            var moved = _controller.Move(steps);
            StatusMessage = moved
                ? $"Movimento de {steps} passo(s) registrado."
                : $"Movimento rejeitado: {_controller.LastErrorCode}.";
            return moved;
        }

        public bool AdvanceTurn()
        {
            if (_controller == null)
                throw new InvalidOperationException("The sandbox control panel is not bound.");

            var advanced = _controller.CompleteDemonstrationTurn();
            StatusMessage = advanced
                ? $"Turno concluído. Agora joga {_controller.Session.CurrentPlayer.Name}."
                : $"Não foi possível concluir o turno: {_controller.LastErrorCode}.";
            return advanced;
        }

        public bool SubmitProgression()
        {
            if (_controller == null)
                throw new InvalidOperationException("The sandbox control panel is not bound.");
            if (!int.TryParse(progressionStepInput, out var steps) || steps < 0)
            {
                StatusMessage = "Informe uma quantidade inteira de passos internos igual ou maior que zero.";
                return false;
            }

            var progressed = _controller.ResolveDemonstrationProgress(steps);
            if (!progressed)
            {
                StatusMessage = $"Progressão rejeitada: {_controller.LastErrorCode}.";
                return false;
            }

            StatusMessage = _controller.Session.Phase == GamePhase.Results
                ? "Condição de vitória alcançada."
                : $"Progressão registrada. Agora joga {_controller.Session.CurrentPlayer.Name}.";
            return true;
        }

        public bool UndoLastAction()
        {
            if (_controller == null)
                throw new InvalidOperationException("The sandbox control panel is not bound.");

            var undone = _controller.UndoLastAction();
            StatusMessage = undone
                ? "Última ação reversível desfeita; a auditoria foi preservada."
                : $"Undo indisponível: {_controller.LastErrorCode}.";
            return undone;
        }

        public bool ExportPlaytest()
        {
            if (_controller == null)
                throw new InvalidOperationException("The sandbox control panel is not bound.");

            var exported = _controller.ExportPlaytest();
            StatusMessage = exported
                ? $"Playtest exportado em {_controller.LastExportDirectory}."
                : $"Falha na exportação: {_controller.LastErrorCode}.";
            return exported;
        }

        private void OnGUI()
        {
            if (_controller?.Session == null) return;
            panelRect = GUI.Window(_windowId, panelRect, DrawWindow, "Money Rank — Painel do Mestre");
        }

        private void DrawWindow(int windowId)
        {
            var session = _controller.Session;
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);
            GUILayout.Label($"Fase: {session.Phase}");
            GUILayout.Label($"Jogador atual: {session.CurrentPlayer?.Name ?? "—"}");
            if (session.Result != null)
            {
                var winner = session.Players.FirstOrDefault(player => player.Id == session.Result.WinnerId);
                GUILayout.Label($"Vencedor: {winner?.Name ?? session.Result.WinnerId.Value}");
                GUILayout.Label($"Motivo: {session.Result.ReasonCode}");
            }
            GUILayout.Space(8f);

            foreach (var player in session.Players.OrderBy(item => item.TurnOrder))
            {
                var position = session.TryGetBoardState(player.Id, out var state)
                    ? state.Position.ToString()
                    : "—";
                var innerPosition = session.TryGetInnerBoardState(player.Id, out var innerState)
                    ? innerState.Position.ToString()
                    : "—";
                var character = session.TryGetCharacter(player.Id, out var assignedCharacter)
                    ? assignedCharacter.DisplayName
                    : "sem personagem";
                GUILayout.Label($"{player.Name}: externa {position} | interna {innerPosition}");
                GUILayout.Label($"  {character}");
            }

            GUILayout.Space(12f);
            GUILayout.Label("Passos informados pelo mestre");
            stepInput = GUILayout.TextField(stepInput, 8, GUILayout.Height(32f));

            GUI.enabled = session.Phase == GamePhase.AwaitMove && !_controller.IsAnimating;
            if (GUILayout.Button("Registrar movimento", GUILayout.Height(44f)))
                SubmitMove();

            GUI.enabled = session.Phase == GamePhase.ResolveSpace && !_controller.IsAnimating;
            if (GUILayout.Button("Concluir casa sem evento", GUILayout.Height(40f)))
                AdvanceTurn();

            GUILayout.Space(8f);
            GUILayout.Label("Passos no tabuleiro interno");
            progressionStepInput = GUILayout.TextField(progressionStepInput, 8, GUILayout.Height(32f));
            GUI.enabled = (session.Phase == GamePhase.ApplyEffects || session.Phase == GamePhase.CheckProgress) &&
                          !_controller.IsAnimating;
            if (GUILayout.Button("Registrar progressão e concluir turno", GUILayout.Height(44f)))
                SubmitProgression();
            GUI.enabled = true;

            GUILayout.Space(10f);
            GUILayout.Label($"Ações auditadas: {session.ActionHistory.Records.Count}");
            foreach (var action in session.ActionHistory.Records.Reverse().Take(4).Reverse())
                GUILayout.Label($"• {action.TypeId}");

            GUI.enabled = _controller.CanUndo && !_controller.IsAnimating;
            if (GUILayout.Button("Desfazer última ação", GUILayout.Height(38f)))
                UndoLastAction();
            GUI.enabled = true;
            if (GUILayout.Button("Exportar playtest", GUILayout.Height(38f)))
                ExportPlaytest();

            GUILayout.Space(10f);
            GUILayout.Label(StatusMessage);
            if (!string.IsNullOrEmpty(_controller.ActiveSaveRoot))
                GUILayout.Label("Autosave local ativo.");

            GUILayout.EndScrollView();
            GUI.DragWindow(new Rect(0f, 0f, panelRect.width, 28f));
        }
    }
}
