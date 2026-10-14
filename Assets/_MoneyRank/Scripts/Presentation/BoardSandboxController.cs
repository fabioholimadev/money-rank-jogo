using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MoneyRank.Application;
using MoneyRank.Content;
using MoneyRank.Domain;
using MoneyRank.Domain.Events;
using MoneyRank.Infrastructure.Export;
using MoneyRank.Infrastructure.Persistence;
using MoneyRank.Presentation.CameraSystem;
using MoneyRank.Presentation.Events;
using UnityEngine;

namespace MoneyRank.Presentation.Board
{
    public sealed class BoardSandboxController : MonoBehaviour
    {
        private static readonly SessionId SandboxSessionId = new SessionId("dev-sandbox-session-v2");
        private static readonly PlayerId[] SandboxPlayerIds =
        {
            new PlayerId("sandbox-player-1"),
            new PlayerId("sandbox-player-2"),
            new PlayerId("sandbox-player-3"),
            new PlayerId("sandbox-player-4")
        };
        private static readonly Vector3[] PlayerBoardOffsets =
        {
            new Vector3(-0.22f, 0f, -0.22f),
            new Vector3(0.22f, 0f, -0.22f),
            new Vector3(-0.22f, 0f, 0.22f),
            new Vector3(0.22f, 0f, 0.22f)
        };
        private static readonly Color[] PlayerColors =
        {
            new Color(0.62f, 0.62f, 0.62f),
            new Color(0.62f, 0.62f, 0.62f),
            new Color(0.62f, 0.62f, 0.62f),
            new Color(0.62f, 0.62f, 0.62f)
        };

        [SerializeField] private BoardDefinitionAsset definition;
        [SerializeField] private EventContentCatalogAsset eventCatalog;
        [SerializeField] private string eventSelectionPolicyId = "sandbox-table-index";
        [SerializeField] private InnerBoardDefinitionAsset innerBoardDefinition;
        [SerializeField] private List<AttributeDefinitionAsset> attributeDefinitionAssets =
            new List<AttributeDefinitionAsset>();
        [SerializeField] private List<CharacterDefinitionAsset> characterDefinitionAssets =
            new List<CharacterDefinitionAsset>();
        [SerializeField] private BoardLayoutView boardView;
        [SerializeField] private PlayerPieceView playerPiece;
        [SerializeField] private BoardCameraController cameraController;
        [SerializeField, Range(2, 4)] private int configuredPlayerCount = 2;
        [SerializeField, Min(0)] private int demonstrationSteps = 3;
        [SerializeField] private bool usePersistentSave = true;
        [SerializeField] private bool restoreExistingSave;
        [SerializeField] private string saveFolderName = "MoneyRank/Saves";

        private readonly Dictionary<PlayerId, PlayerPieceView> _playerPieces =
            new Dictionary<PlayerId, PlayerPieceView>();
        private GameSessionService _sessionService;
        private SessionActionRecorder _actionRecorder;
        private GameEventSelectionService _eventSelectionService;
        private GameEventService _eventService;
        private GameProgressionService _progressionService;
        private EventDefinition _activeEventDefinition;
        private EventResolutionPreview _activePreview;
        private InnerBoardResolutionResult _lastProgressionResult;
        private string _saveRootOverride;
        private readonly List<string> _undoCheckpointIds = new List<string>();
        private SessionUndoJournal _undoJournal;
        private GameUndoService _undoService;
        private DateTimeOffset _sessionStartedAt;

        public GameSession Session { get; private set; }
        public int PlayerPieceCount => _playerPieces.Count;
        public EventPanelView EventPanel { get; private set; }
        public SandboxControlPanelView ControlPanel { get; private set; }
        public string LastErrorCode { get; private set; } = string.Empty;
        public string ActiveSaveRoot { get; private set; } = string.Empty;
        public bool RestoredExistingSave { get; private set; }
        public bool IsAnimating => _playerPieces.Values.Any(piece => piece.IsAnimating);
        public EventDefinition ActiveEventDefinition => _activeEventDefinition;
        public EventResolutionPreview ActivePreview => _activePreview;
        public InnerBoardResolutionResult LastProgressionResult => _lastProgressionResult;
        public bool CanUndo => _undoCheckpointIds.Count > 0;
        public string LastExportDirectory { get; private set; } = string.Empty;
        public SessionUndoJournal UndoJournal => _undoJournal;

        private void Start()
        {
            var startMenu = GetComponent<BoardStartMenuView>();
            if (startMenu == null)
                startMenu = gameObject.AddComponent<BoardStartMenuView>();
            startMenu.Bind(this);
        }

        public bool HasSavedGame
        {
            get
            {
                if (!usePersistentSave)
                    return false;

                try
                {
                    var root = string.IsNullOrWhiteSpace(_saveRootOverride)
                        ? ResolveConfiguredSaveRoot()
                        : _saveRootOverride;
                    var store = new FileGameSessionSaveStore(root);
                    return File.Exists(store.GetSavePath(SandboxSessionId));
                }
                catch (Exception exception)
                {
                    LastErrorCode = $"SAVE_CHECK_FAILED: {exception.Message}";
                    return false;
                }
            }
        }

        public bool StartNewGame()
        {
            return StartNewGame(configuredPlayerCount);
        }

        public bool StartNewGame(int playerCount)
        {
            if (Session != null)
                return false;
            if (playerCount < 2 || playerCount > SandboxPlayerIds.Length)
            {
                LastErrorCode = "PLAYER_COUNT_OUT_OF_RANGE";
                return false;
            }

            configuredPlayerCount = playerCount;
            restoreExistingSave = false;
            RestoredExistingSave = false;
            Initialize();
            return Session != null;
        }

        public bool ContinueSavedGame()
        {
            if (Session != null || !HasSavedGame)
                return false;

            restoreExistingSave = true;
            Initialize();
            return RestoredExistingSave;
        }

        public void Initialize()
        {
            if (Session != null) return;

            if (definition == null || boardView == null || playerPiece == null || cameraController == null)
                throw new InvalidOperationException("The board definition, board view, player piece and camera controller are required.");

            EventPanel = GetComponent<EventPanelView>();
            if (EventPanel == null)
                EventPanel = gameObject.AddComponent<EventPanelView>();
            EventPanel.OptionRequested += HandleOptionRequested;
            EventPanel.ConfirmationRequested += HandleConfirmationRequested;
            EventPanel.CancellationRequested += HandleCancellationRequested;
            EventPanel.CloseRequested += HandleEventCloseRequested;
            ControlPanel = GetComponent<SandboxControlPanelView>();
            if (ControlPanel == null)
                ControlPanel = gameObject.AddComponent<SandboxControlPanelView>();

            var board = definition.ToDomain();
            var innerBoard = innerBoardDefinition == null ? null : innerBoardDefinition.ToDomain();
            var attributes = attributeDefinitionAssets
                .Select(asset => asset != null
                    ? asset.ToDomain()
                    : throw new InvalidOperationException("The sandbox contains a missing attribute definition asset."))
                .ToList();
            var characters = characterDefinitionAssets
                .Select(asset => asset != null
                    ? asset.ToDomain()
                    : throw new InvalidOperationException("The sandbox contains a missing character definition asset."))
                .ToList();
            if (boardView.SpaceAnchors.Count != board.SpaceCount)
            {
                boardView.Rebuild();
            }

            var playerCount = ResolvePlayerCount();
            boardView.SetActivePlayerCount(playerCount);
            playerPiece.name = "PlayerPiece 1";
            var pieces = new PlayerPieceView[playerCount];
            pieces[0] = playerPiece;
            for (var index = 1; index < pieces.Length; index++)
                pieces[index] = Instantiate(playerPiece, playerPiece.transform.parent);

            for (var index = 0; index < playerCount; index++)
            {
                var piece = pieces[index];
                piece.name = $"PlayerPiece {index + 1}";
                piece.SetBoardOffset(PlayerBoardOffsets[index]);
                piece.SetPlayerColor(PlayerColors[index]);
                _playerPieces.Add(SandboxPlayerIds[index], piece);
            }

            var repository = CreateRepository();
            _actionRecorder = new SessionActionRecorder("sandbox-ui", "sandbox-facilitator");
            _sessionService = new GameSessionService(repository, _actionRecorder);
            _undoJournal = new SessionUndoJournal();
            _undoService = new GameUndoService(repository, _undoJournal, _actionRecorder);
            _sessionStartedAt = DateTimeOffset.UtcNow;
            Session = new GameSession(
                SandboxSessionId,
                new SessionRules(2, 4),
                new GameFlowStateMachine(),
                board,
                innerBoard,
                attributes,
                characterDefinitions: characters);
            for (var index = 0; index < playerCount; index++)
            {
                EnsureSucceeded(
                    Session.AddPlayer(
                        new Player(SandboxPlayerIds[index], $"Sandbox Player {index + 1}", index)),
                    "configure a sandbox player");
            }
            if (characters.Count > 0)
            {
                var playersWithCharacters = Math.Min(characters.Count, playerCount);
                for (var index = 0; index < playersWithCharacters; index++)
                {
                    EnsureSucceeded(
                        Session.AssignCharacter(SandboxPlayerIds[index], characters[index].Id),
                        "assign a sandbox character");
                }
            }

            if (repository is FileBackedGameSessionRepository fileRepository && restoreExistingSave)
            {
                var restore = fileRepository.Attach(Session, true);
                if (restore.Succeeded)
                {
                    RestoredExistingSave = true;
                }
                else if (!string.Equals(restore.ErrorCode, "SESSION_SAVE_NOT_FOUND", StringComparison.Ordinal))
                {
                    LastErrorCode = restore.ErrorCode;
                    throw new InvalidOperationException(
                        $"Could not restore the sandbox session: {restore.ErrorCode} - {restore.Message}");
                }
            }

            if (!RestoredExistingSave)
            {
                AttachFreshSession(repository);
                RecordInitialSetupActions();
                EnsureSucceeded(_sessionService.Start(SandboxSessionId), "start the sandbox session");
                EnsureSucceeded(_sessionService.BeginTurn(SandboxSessionId), "begin the first sandbox turn");
            }

            ConfigureEventFlow(repository);
            ConfigureProgressionFlow(repository);

            foreach (var binding in _playerPieces)
            {
                if (!Session.TryGetBoardState(binding.Key, out var boardState))
                    throw new InvalidOperationException($"Board state for '{binding.Key}' was not created.");

                binding.Value.SnapTo(boardState.Position, boardView.SpaceAnchors);
            }

            cameraController.ReturnToOverview();
            ControlPanel.Bind(this);
            ResumeOrOpenEvent();
        }

        public void ConfigurePersistence(string saveRoot, bool restoreExisting)
        {
            if (Session != null)
                throw new InvalidOperationException("Persistence must be configured before sandbox initialization.");
            if (string.IsNullOrWhiteSpace(saveRoot))
                throw new ArgumentException("Save root cannot be empty.", nameof(saveRoot));

            usePersistentSave = true;
            restoreExistingSave = restoreExisting;
            _saveRootOverride = Path.GetFullPath(saveRoot);
        }

        [ContextMenu("Move Demonstration Steps")]
        public void MoveDemonstrationSteps()
        {
            Move(demonstrationSteps);
        }

        public bool Move(int steps)
        {
            if (Session == null || IsAnimating) return false;

            var checkpointId = CreateUndoCheckpoint("Before board movement");
            if (checkpointId == null)
                return false;
            var currentPlayerId = Session.CurrentPlayer.Id;
            var result = _sessionService.RegisterMove(SandboxSessionId, steps);
            if (!result.Succeeded)
            {
                LastErrorCode = result.ErrorCode;
                return false;
            }

            _undoCheckpointIds.Add(checkpointId);

            LastErrorCode = string.Empty;
            var currentPiece = _playerPieces[currentPlayerId];
            if (UnityEngine.Application.isPlaying)
            {
                StartCoroutine(AnimateMove(currentPiece, result.Value));
            }
            else
            {
                currentPiece.SnapTo(result.Value.Destination, boardView.SpaceAnchors);
                cameraController.SetState(BoardCameraState.FocusSpace, currentPiece.transform);
                ShowLandingEvent();
            }

            return true;
        }

        public bool CompleteDemonstrationTurn()
        {
            if (Session == null || IsAnimating) return false;
            if (Session.Phase == GamePhase.AwaitChoice)
            {
                LastErrorCode = "SANDBOX_EVENT_CONFIRMATION_REQUIRED";
                return false;
            }
            if (Session.Phase != GamePhase.ResolveSpace && Session.Phase != GamePhase.ApplyEffects)
            {
                LastErrorCode = "SANDBOX_RESOLUTION_NOT_READY";
                return false;
            }

            if (Session.Phase == GamePhase.ResolveSpace)
                EnsureSucceeded(Session.TryTransition(GamePhase.ApplyEffects), "complete a space without a configured event");
            if (_progressionService != null)
                return ResolveDemonstrationProgress(0);

            EnsureSucceeded(Session.TryTransition(GamePhase.CheckProgress), "skip the unimplemented progress check");
            EnsureSucceeded(Session.TryTransition(GamePhase.EndTurn), "finish the sandbox turn flow");
            EnsureSucceeded(_sessionService.CompleteTurn(SandboxSessionId), "complete the sandbox turn");
            EnsureSucceeded(_sessionService.BeginTurn(SandboxSessionId), "begin the next sandbox turn");

            LastErrorCode = string.Empty;
            _activeEventDefinition = null;
            _activePreview = null;
            EventPanel.Hide();
            cameraController.ReturnToOverview();
            return true;
        }

        public bool ReopenPendingEvent()
        {
            if (Session == null ||
                Session.Phase != GamePhase.AwaitChoice ||
                EventPanel == null ||
                EventPanel.Model.IsVisible)
            {
                return false;
            }

            ResumeOrOpenEvent();
            return EventPanel.Model.IsVisible;
        }

        public bool ResolveDemonstrationProgress(int steps)
        {
            if (Session == null || IsAnimating) return false;
            if (steps < 0)
            {
                LastErrorCode = "INNER_BOARD_STEPS_INVALID";
                return false;
            }
            if (_progressionService == null)
            {
                LastErrorCode = "SANDBOX_PROGRESSION_NOT_CONFIGURED";
                return false;
            }
            var checkpointId = CreateUndoCheckpoint("Before inner-board progression");
            if (checkpointId == null)
                return false;
            if (Session.Phase == GamePhase.ApplyEffects)
                EnsureSucceeded(Session.TryTransition(GamePhase.CheckProgress), "begin the progress check");
            if (Session.Phase != GamePhase.CheckProgress)
            {
                LastErrorCode = "SANDBOX_PROGRESSION_NOT_READY";
                return false;
            }

            _undoCheckpointIds.Add(checkpointId);

            var result = _progressionService.ResolveInnerBoard(SandboxSessionId, steps);
            if (!result.Succeeded)
            {
                LastErrorCode = result.ErrorCode;
                return false;
            }

            LastErrorCode = string.Empty;
            _lastProgressionResult = result.Value;
            _activeEventDefinition = null;
            _activePreview = null;
            EventPanel.Hide();

            if (Session.Phase == GamePhase.EndTurn)
            {
                EnsureSucceeded(_sessionService.CompleteTurn(SandboxSessionId), "complete the sandbox turn");
                EnsureSucceeded(_sessionService.BeginTurn(SandboxSessionId), "begin the next sandbox turn");
                cameraController.ReturnToOverview();
            }
            else if (Session.Phase == GamePhase.GameEnd)
            {
                EnsureSucceeded(_sessionService.ShowResults(SandboxSessionId), "show the sandbox result");
                cameraController.SetState(BoardCameraState.Results);
            }

            return true;
        }

        public bool UndoLastAction()
        {
            if (Session == null || IsAnimating || _undoService == null || _undoCheckpointIds.Count == 0)
            {
                LastErrorCode = "UNDO_NOT_AVAILABLE";
                return false;
            }

            var checkpointId = _undoCheckpointIds[_undoCheckpointIds.Count - 1];
            var result = _undoService.Undo(
                SandboxSessionId,
                checkpointId,
                Guid.NewGuid().ToString("N"),
                DateTimeOffset.UtcNow,
                "sandbox-facilitator");
            if (!result.Succeeded)
            {
                LastErrorCode = result.ErrorCode;
                return false;
            }

            _undoCheckpointIds.RemoveAt(_undoCheckpointIds.Count - 1);
            LastErrorCode = string.Empty;
            SynchronizePresentationWithSession();
            return true;
        }

        public bool ExportPlaytest()
        {
            if (Session == null || _undoJournal == null)
            {
                LastErrorCode = "PLAYTEST_EXPORT_NOT_AVAILABLE";
                return false;
            }

            var outputDirectory = ResolveExportRoot();
            var result = new FilePlaytestExporter().Export(
                new PlaytestExportRequest(
                    Session,
                    _undoJournal,
                    DateTimeOffset.UtcNow,
                    _sessionStartedAt,
                    Session.Result == null ? (DateTimeOffset?)null : DateTimeOffset.UtcNow),
                outputDirectory);
            if (!result.Succeeded)
            {
                LastErrorCode = result.ErrorCode;
                return false;
            }

            LastErrorCode = string.Empty;
            LastExportDirectory = outputDirectory;
            return true;
        }

        [ContextMenu("Complete Demonstration Turn")]
        private void CompleteDemonstrationTurnFromContextMenu()
        {
            CompleteDemonstrationTurn();
        }

        private IEnumerator AnimateMove(PlayerPieceView currentPiece, BoardMoveResult result)
        {
            cameraController.SetState(BoardCameraState.FollowPiece, currentPiece.transform);
            yield return currentPiece.Animate(result, boardView.SpaceAnchors);
            cameraController.SetState(BoardCameraState.FocusSpace, currentPiece.transform);
            ShowLandingEvent();
        }

        private void ConfigureEventFlow(IGameSessionRepository repository)
        {
            if (eventCatalog == null)
                return;

            var catalog = eventCatalog.ToDomain();
            var effectExecutor = new EffectExecutor(
                new IEffectHandler[]
                {
                    new SetEventStateEffectHandler(),
                    new RemoveEventStateEffectHandler()
                });
            _eventSelectionService = new GameEventSelectionService(
                repository,
                catalog,
                new IEventSelectionPolicy[]
                {
                    new TableIndexEventSelectionPolicy(eventSelectionPolicyId)
                },
                _actionRecorder);
            _eventService = new GameEventService(
                repository,
                new EventPreviewService(
                    new RequirementEvaluator(
                        new IRequirementHandler[]
                        {
                            new StateEqualsRequirementHandler(),
                            new StateExistsRequirementHandler()
                        }),
                    effectExecutor),
                new EventConfirmationService(effectExecutor),
                _actionRecorder);
        }

        private void ConfigureProgressionFlow(IGameSessionRepository repository)
        {
            if (Session.InnerBoard == null)
                return;

            var overflowHandlers = Session.AttributeDefinitions
                .Select(definition => definition.OverflowBehaviorId)
                .Distinct(StringComparer.Ordinal)
                .Select(id => (IAttributeOverflowHandler)new CarryAttributeOverflowHandler(id));
            _progressionService = new GameProgressionService(
                repository,
                new AttributeProgressionService(overflowHandlers),
                new InnerBoardProgressionService(
                    new IInnerBoardMovementPolicy[]
                    {
                        new LinearClampedInnerBoardMovementPolicy(Session.InnerBoard.MovementPolicyId)
                    }),
                new InnerBoardCaptureService(
                    new IInnerBoardCapturePolicy[]
                    {
                        new SharedSpaceCheckpointCapturePolicy(Session.InnerBoard.CapturePolicyId)
                    }),
                new InnerBoardVictoryService(
                    new IInnerBoardVictoryPolicy[]
                    {
                        new GoalSpaceVictoryPolicy(Session.InnerBoard.VictoryPolicyId, "SANDBOX_INNER_GOAL_REACHED")
                    }),
                _actionRecorder);
        }

        private void RecordInitialSetupActions()
        {
            _actionRecorder.Record(
                Session,
                "session.created",
                null,
                GamePhase.Setup,
                new[]
                {
                    new DefinitionParameter("minimumPlayers", Session.Rules.MinimumPlayers.ToString()),
                    new DefinitionParameter("maximumPlayers", Session.Rules.MaximumPlayers.ToString()),
                    new DefinitionParameter("boardId", Session.Board.Id)
                });
            foreach (var player in Session.Players.OrderBy(item => item.TurnOrder))
            {
                _actionRecorder.Record(
                    Session,
                    "player.added",
                    player.Id,
                    GamePhase.Setup,
                    new[] { new DefinitionParameter("turnOrder", player.TurnOrder.ToString()) });
                if (Session.TryGetCharacter(player.Id, out var character))
                {
                    _actionRecorder.Record(
                        Session,
                        "character.assigned",
                        player.Id,
                        GamePhase.Setup,
                        new[] { new DefinitionParameter("characterId", character.Id) });
                }
            }
        }

        private void ResumeOrOpenEvent()
        {
            if (Session.Phase == GamePhase.Results)
            {
                cameraController.SetState(BoardCameraState.Results);
                return;
            }
            if (Session.Phase == GamePhase.AwaitChoice)
            {
                var skipped = Session.SkipActiveEventChoice();
                if (!skipped.Succeeded)
                {
                    LastErrorCode = skipped.ErrorCode;
                    EventPanel.Hide();
                    return;
                }

                LastErrorCode = string.Empty;
                _activeEventDefinition = null;
                _activePreview = null;
                EventPanel.Hide();
                CompleteDemonstrationTurn();
                return;
            }

            if (Session.Phase == GamePhase.ResolveSpace)
                ShowLandingEvent();
        }

        private bool _isPrototypeEventOpen;

        private void ShowLandingEvent()
        {
            if (Session == null || Session.Phase != GamePhase.ResolveSpace || EventPanel == null)
                return;

            var player = Session.CurrentPlayer;
            if (!Session.TryGetBoardState(player.Id, out var boardState))
                return;
            var space = Session.Board.GetSpace(boardState.Position);
            var categoryId = string.IsNullOrWhiteSpace(space.CategoryId) ? "special-space" : space.CategoryId;
            var categoryName = GetCategoryDisplayName(categoryId);
            var options = new[] { "X", "Y", "Z" }
                .Select(choice => new EventOptionDefinition(
                    choice,
                    choice,
                    "Opção provisória para este protótipo; ainda sem efeito de jogo.",
                    new RequirementDefinition[0],
                    new GameEffectDefinition[0],
                    "public"));
            _activeEventDefinition = new EventDefinition(
                $"prototype-space-{space.Index:00}",
                1,
                categoryId,
                $"Evento de {space.DisplayName}",
                $"Categoria: {categoryName}\nEscolha uma carta para continuar a partida.",
                new[] { "prototype" },
                new RequirementDefinition[0],
                options,
                presentationId: "prototype-cards");
            _activePreview = null;
            _isPrototypeEventOpen = true;
            EventPanel.ShowPrototype(_activeEventDefinition);
        }

        private static string GetCategoryDisplayName(string categoryId)
        {
            switch (categoryId)
            {
                case "essential-expense": return "Despesa essencial";
                case "non-essential-expense": return "Despesa não essencial";
                case "investment": return "Investimento";
                case "tax": return "Imposto";
                case "debt": return "Dívida";
                case "life-progress": return "Progresso de vida";
                case "opportunity": return "Oportunidade";
                case "leisure": return "Lazer";
                case "special-space": return "Casa especial";
                default: return categoryId;
            }
        }

        private bool CompleteLandingWithoutEvent()
        {
            if (Session == null || Session.Phase != GamePhase.ResolveSpace)
                return false;

            LastErrorCode = string.Empty;
            _activeEventDefinition = null;
            _activePreview = null;
            _isPrototypeEventOpen = false;
            EventPanel.Hide();
            var completed = CompleteDemonstrationTurn();
            if (!completed && string.IsNullOrEmpty(LastErrorCode))
                LastErrorCode = "LANDING_TURN_COMPLETION_FAILED";
            return completed;
        }

        private void ShowEventWithAvailability(EventDefinition definitionToShow)
        {
            var previews = new Dictionary<string, EventResolutionPreview>(StringComparer.Ordinal);
            foreach (var option in definitionToShow.Options)
            {
                var preview = _eventService.PreviewOption(
                    SandboxSessionId,
                    definitionToShow,
                    option.Id);
                if (preview.Succeeded)
                    previews.Add(option.Id, preview.Value);
            }
            EventPanel.Show(definitionToShow, previews);
        }

        private void HandleOptionRequested(string optionId)
        {
            if (_isPrototypeEventOpen)
            {
                _isPrototypeEventOpen = false;
                EventPanel.Hide();
                ControlPanel.ReportStatus($"Carta {optionId} selecionada. Vez do próximo jogador.");
                CompleteLandingWithoutEvent();
                return;
            }

            if (_eventService == null || _activeEventDefinition == null)
                return;

            var preview = _eventService.PreviewOption(
                SandboxSessionId,
                _activeEventDefinition,
                optionId);
            if (!preview.Succeeded)
            {
                LastErrorCode = preview.ErrorCode;
                EventPanel.ShowError(preview.Message);
                return;
            }

            LastErrorCode = string.Empty;
            _activePreview = preview.Value;
            EventPanel.ShowPreview(_activePreview);
        }

        private void HandleConfirmationRequested()
        {
            if (_eventService == null || _activePreview == null || Session.ActiveEvent == null)
                return;

            var checkpointId = CreateUndoCheckpoint("Before event confirmation");
            if (checkpointId == null)
                return;
            var confirmation = _eventService.ConfirmOption(
                SandboxSessionId,
                _activePreview,
                new EventConfirmationRequest(
                    Guid.NewGuid().ToString("N"),
                    Session.ActiveEvent.SpaceId,
                    "sandbox-local",
                    DateTimeOffset.UtcNow,
                    "sandbox-facilitator"));
            if (!confirmation.Succeeded)
            {
                LastErrorCode = confirmation.ErrorCode;
                EventPanel.ShowError(confirmation.Message);
                return;
            }

            _undoCheckpointIds.Add(checkpointId);
            LastErrorCode = string.Empty;
            _activeEventDefinition = null;
            _activePreview = null;
            EventPanel.Hide();
            ControlPanel.ReportStatus(
                "Evento confirmado. Informe os passos da progressão para continuar o turno.");
        }

        private void HandleCancellationRequested()
        {
            if (_activePreview == null || _activeEventDefinition == null)
                return;

            _activePreview = null;
            LastErrorCode = string.Empty;
            EventPanel.Model.CancelPreview();
        }

        private void HandleEventCloseRequested()
        {
            if (_isPrototypeEventOpen && Session?.Phase == GamePhase.ResolveSpace)
            {
                _isPrototypeEventOpen = false;
                CompleteLandingWithoutEvent();
                return;
            }

            if (Session?.Phase == GamePhase.AwaitChoice)
            {
                if (UndoLastAction() && Session.Phase == GamePhase.AwaitMove)
                {
                    ControlPanel.ReportStatus(
                        "Evento fechado. Você voltou ao controle do movimento; ajuste os passos e tente novamente.");
                    return;
                }

                EventPanel.ShowError(
                    $"Não foi possível voltar ao controle do movimento: {LastErrorCode}.");
                return;
            }

            _activeEventDefinition = null;
            _activePreview = null;
            _isPrototypeEventOpen = false;
            EventPanel.Hide();
        }

        private IGameSessionRepository CreateRepository()
        {
            if (!usePersistentSave)
                return new InMemorySessionRepository();

            ActiveSaveRoot = string.IsNullOrWhiteSpace(_saveRootOverride)
                ? ResolveConfiguredSaveRoot()
                : _saveRootOverride;
            return new FileBackedGameSessionRepository(new FileGameSessionSaveStore(ActiveSaveRoot));
        }

        private int ResolvePlayerCount()
        {
            if (!restoreExistingSave || !usePersistentSave)
                return configuredPlayerCount;

            var saveRoot = string.IsNullOrWhiteSpace(_saveRootOverride)
                ? ResolveConfiguredSaveRoot()
                : _saveRootOverride;
            var savedSession = new FileGameSessionSaveStore(saveRoot).Load(SandboxSessionId);
            if (!savedSession.Succeeded)
                throw new InvalidOperationException(
                    $"Could not read the saved player count: {savedSession.ErrorCode}.");

            var playerCount = savedSession.Value.Players.Count;
            if (playerCount < 2 || playerCount > SandboxPlayerIds.Length)
                throw new InvalidOperationException(
                    $"The saved session has {playerCount} players; this build supports between 2 and {SandboxPlayerIds.Length}.");
            return playerCount;
        }

        private string ResolveConfiguredSaveRoot()
        {
            if (string.IsNullOrWhiteSpace(saveFolderName))
                throw new InvalidOperationException("The sandbox save folder name cannot be empty.");

            var persistentRoot = Path.GetFullPath(UnityEngine.Application.persistentDataPath);
            var saveRoot = Path.GetFullPath(Path.Combine(persistentRoot, saveFolderName));
            var requiredPrefix = persistentRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
                                 Path.DirectorySeparatorChar;
            if (!saveRoot.StartsWith(requiredPrefix, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("The sandbox save folder must stay inside persistentDataPath.");
            return saveRoot;
        }

        private string ResolveExportRoot()
        {
            if (!string.IsNullOrWhiteSpace(_saveRootOverride))
                return Path.Combine(_saveRootOverride, "Exports");

            return Path.Combine(UnityEngine.Application.persistentDataPath, "MoneyRank", "Exports");
        }

        private string CreateUndoCheckpoint(string label)
        {
            if (_undoService == null)
            {
                LastErrorCode = "UNDO_NOT_CONFIGURED";
                return null;
            }

            var checkpointId = Guid.NewGuid().ToString("N");
            var result = _undoService.CreateCheckpoint(
                SandboxSessionId,
                checkpointId,
                label,
                DateTimeOffset.UtcNow,
                "sandbox-facilitator");
            if (result.Succeeded)
                return checkpointId;

            LastErrorCode = result.ErrorCode;
            return null;
        }

        private void SynchronizePresentationWithSession()
        {
            _activeEventDefinition = null;
            _activePreview = null;
            _lastProgressionResult = null;
            EventPanel.Hide();

            foreach (var binding in _playerPieces)
            {
                if (Session.TryGetBoardState(binding.Key, out var boardState))
                    binding.Value.SnapTo(boardState.Position, boardView.SpaceAnchors);
            }

            if (Session.Phase == GamePhase.Results)
            {
                cameraController.SetState(BoardCameraState.Results);
                return;
            }

            if (Session.Phase == GamePhase.AwaitChoice)
                ResumeOrOpenEvent();
            else if (Session.CurrentPlayer != null)
                cameraController.ReturnToOverview();
        }

        private void AttachFreshSession(IGameSessionRepository repository)
        {
            if (repository is FileBackedGameSessionRepository fileRepository)
            {
                EnsureSucceeded(fileRepository.Attach(Session, false), "attach a fresh sandbox session");
                return;
            }

            repository.Save(Session);
        }

        private static void EnsureSucceeded(OperationResult result, string action)
        {
            if (!result.Succeeded)
                throw new InvalidOperationException($"Could not {action}: {result.ErrorCode} - {result.Message}");
        }

        private sealed class InMemorySessionRepository : IGameSessionRepository
        {
            private readonly Dictionary<SessionId, GameSession> _sessions =
                new Dictionary<SessionId, GameSession>();

            public GameSession Get(SessionId sessionId) =>
                _sessions.TryGetValue(sessionId, out var session) ? session : null;

            public void Save(GameSession session)
            {
                _sessions[session.Id] = session;
            }
        }
    }
}
