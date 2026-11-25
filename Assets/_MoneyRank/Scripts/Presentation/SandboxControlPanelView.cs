using System;
using MoneyRank.Domain;
using MoneyRank.Presentation.Board;
using UnityEngine;

namespace MoneyRank.Presentation
{
    [DisallowMultipleComponent]
    public sealed class SandboxControlPanelView : MonoBehaviour
    {
        private static int _nextWindowId;

        [SerializeField] private Rect panelRect = new Rect(24f, 24f, 340f, 430f);
        [SerializeField] private string stepInput = "3";
        [SerializeField] private string progressionStepInput = "1";

        private BoardSandboxController _controller;
        private int _windowId;
        private Vector2 _scrollPosition;
        private bool _showTools;
        private GUIStyle _playerStyle;
        private GUIStyle _detailStyle;

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
            if (_controller?.Session == null || _controller.IsPaused ||
                _controller.EventPanel?.Model.IsVisible == true)
                return;

            var scale = Mathf.Clamp(Mathf.Min(Screen.width / 1280f, Screen.height / 720f), 0.72f, 1f);
            if (UnityEngine.Application.isMobilePlatform)
            {
                var safeArea = Screen.safeArea;
                panelRect.width = Mathf.Min(340f * scale, safeArea.width - 24f);
                panelRect.height = Mathf.Min(430f * scale, safeArea.height - 24f);
                panelRect.x = safeArea.x + 12f;
                panelRect.y = Screen.height - safeArea.yMax + 58f * scale;
            }
            else
            {
                panelRect.width = Mathf.Max(panelRect.width, 330f * scale);
                panelRect.height = Mathf.Min(panelRect.height, Screen.height - 24f);
                panelRect.y = Mathf.Max(panelRect.y, 58f * scale);
            }
            EnsureStyles(scale);
            panelRect = GUI.Window(_windowId, panelRect, DrawWindow, "PAINEL DO MESTRE");
        }

        private void DrawWindow(int windowId)
        {
            var session = _controller.Session;
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, false, true,
                GUILayout.Height(Mathf.Max(80f, panelRect.height - 38f)));
            GUILayout.BeginVertical(GUI.skin.box);
            var activePlayer = session.CurrentPlayer;
            GUILayout.Label(activePlayer?.Name ?? "Partida concluída", _playerStyle);
            if (activePlayer != null)
            {
                var character = session.TryGetCharacter(activePlayer.Id, out var assignedCharacter)
                    ? assignedCharacter.DisplayName
                    : "Personagem não atribuído";
                var space = session.TryGetBoardState(activePlayer.Id, out var state)
                    ? $"Casa {state.Position + 1}"
                    : string.Empty;
                GUILayout.Label($"{character}{(string.IsNullOrEmpty(space) ? string.Empty : $"  •  {space}")}", _detailStyle);
            }
            GUILayout.Label(GetPhaseLabel(session.Phase), _detailStyle);
            GUILayout.EndVertical();
            GUILayout.Space(10f);

            if (session.Phase == GamePhase.AwaitChoice && !_controller.EventPanel.Model.IsVisible)
            {
                if (GUILayout.Button("Reabrir evento pendente", GUILayout.Height(40f)) &&
                    !_controller.ReopenPendingEvent())
                {
                    StatusMessage = $"Não foi possível reabrir o evento: {_controller.LastErrorCode}.";
                }
            }

            if (session.Phase == GamePhase.AwaitMove)
            {
                GUILayout.Label("Quantas casas a capivara vai andar?");
                GUILayout.BeginHorizontal();
                stepInput = GUILayout.TextField(stepInput, 8, GUILayout.Height(38f));
                GUI.enabled = !_controller.IsAnimating;
                if (GUILayout.Button("MOVER", GUILayout.Height(38f), GUILayout.Width(105f)))
                    SubmitMove();
                GUILayout.EndHorizontal();
            }

            GUI.enabled = !_controller.IsAnimating;
            if (session.Phase == GamePhase.ResolveSpace && GUILayout.Button("CONCLUIR CASA", GUILayout.Height(42f)))
                AdvanceTurn();

            if (session.Phase == GamePhase.ApplyEffects || session.Phase == GamePhase.CheckProgress)
            {
                GUILayout.Label("Passos no tabuleiro interno");
                GUILayout.BeginHorizontal();
                progressionStepInput = GUILayout.TextField(progressionStepInput, 8, GUILayout.Height(38f));
                if (GUILayout.Button("CONCLUIR TURNO", GUILayout.Height(38f), GUILayout.Width(140f)))
                    SubmitProgression();
                GUILayout.EndHorizontal();
            }
            GUI.enabled = true;

            if (!string.IsNullOrEmpty(StatusMessage))
            {
                GUILayout.Space(7f);
                GUILayout.Label(StatusMessage, _detailStyle);
            }

            GUILayout.FlexibleSpace();
            _showTools = GUILayout.Toggle(_showTools, _showTools ? "Ocultar ferramentas" : "Ferramentas do mestre");
            if (_showTools)
            {
                GUILayout.BeginHorizontal();
                GUI.enabled = _controller.CanUndo && !_controller.IsAnimating;
                if (GUILayout.Button("Desfazer ação", GUILayout.Height(32f))) UndoLastAction();
                GUI.enabled = true;
                if (GUILayout.Button("Exportar playtest", GUILayout.Height(32f))) ExportPlaytest();
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
            GUI.DragWindow(new Rect(0f, 0f, panelRect.width, 28f));
        }

        private void EnsureStyles(float scale)
        {
            if (_playerStyle != null) return;
            _playerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(22f * scale),
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color32(245, 197, 66, 255) }
            };
            _detailStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(14f * scale),
                wordWrap = true,
                normal = { textColor = new Color32(230, 230, 230, 255) }
            };
        }

        private static string GetPhaseLabel(GamePhase phase) => phase switch
        {
            GamePhase.AwaitMove => "Sua vez: informe o movimento.",
            GamePhase.ResolveSpace => "Casa alcançada.",
            GamePhase.AwaitChoice => "Aguardando escolha da carta.",
            GamePhase.ApplyEffects => "Resolva a casa e conclua o turno.",
            GamePhase.CheckProgress => "Informe a progressão interna.",
            GamePhase.GameEnd => "Partida encerrada.",
            GamePhase.Results => "Resultado da partida.",
            _ => "Preparando turno..."
        };
    }
}
