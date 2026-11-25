using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ZXing;
using ZXing.QrCode;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;

namespace MoneyRank.Presentation.Board
{
    /// <summary>
    /// Owns the UGS session lifecycle for the online waiting room.
    /// Gameplay authority remains on the PC host; mobile builds are clients only.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class OnlineSessionCoordinator : MonoBehaviour
    {
        private const string NicknameProperty = "nickname";
        // MaxPlayers includes the PC host; the four game participants join as clients.
        private const int MaximumSessionPlayers = 5;

        private ISession _session;
        private bool _busy;
        private Texture2D _joinCodeQrTexture;
        private WebCamTexture _qrCamera;
        private float _nextQrScanTime;

        public event Action Changed;

        public ISession Session => _session;
        public string Status { get; private set; } = string.Empty;
        public bool IsBusy => _busy;
        public bool IsInRoom => _session != null;
        public bool IsScanningQr => _qrCamera != null && _qrCamera.isPlaying;
        public Texture2D JoinCodeQrTexture => _joinCodeQrTexture;
        public Texture QrCameraTexture => _qrCamera;
        public bool IsHost => _session?.IsHost ?? false;
        public bool CanHost => UnityEngine.Application.platform != RuntimePlatform.Android &&
                               UnityEngine.Application.platform != RuntimePlatform.IPhonePlayer;
        public string JoinCode => _session?.Code ?? string.Empty;

        public IReadOnlyList<RoomMember> Members => _session == null
            ? Array.Empty<RoomMember>()
            : _session.Players
                .Select(player => new RoomMember(
                    player.Properties.TryGetValue(NicknameProperty, out var value) &&
                    !string.IsNullOrWhiteSpace(value.Value)
                        ? value.Value
                        : "Jogador",
                    player.Id == _session.Host))
                .ToArray();

        private void Awake()
        {
            EnsureNetworkManager();
        }

        public async void CreateRoom(string nickname)
        {
            if (!CanHost)
            {
                SetStatus("Este dispositivo só pode entrar em uma sala criada no PC.");
                return;
            }

            if (_busy || _session != null)
                return;

            _busy = true;
            SetStatus("Conectando aos serviços online...");
            try
            {
                await EnsureSignedIn(nickname);
                var playerProperties = new Dictionary<string, PlayerProperty>
                {
                    [NicknameProperty] = new PlayerProperty(nickname)
                };
                var options = new SessionOptions
                {
                    Name = "Money Rank",
                    MaxPlayers = MaximumSessionPlayers,
                    PlayerProperties = playerProperties
                }.WithRelayNetwork();

                _session = await MultiplayerService.Instance.CreateSessionAsync(options);
                Subscribe(_session);
                _joinCodeQrTexture = GenerateQrTexture(_session.Code);
                SetStatus("Sala criada. Compartilhe o código com os jogadores.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                SetStatus(ToFriendlyError(exception));
            }
            finally
            {
                _busy = false;
                Changed?.Invoke();
            }
        }

        public async void JoinRoom(string nickname, string code)
        {
            if (_busy || _session != null)
                return;
            if (string.IsNullOrWhiteSpace(code))
            {
                SetStatus("Digite o código da sala.");
                return;
            }

            _busy = true;
            SetStatus("Procurando a sala...");
            try
            {
                await EnsureSignedIn(nickname);
                var options = new JoinSessionOptions
                {
                    PlayerProperties = new Dictionary<string, PlayerProperty>
                    {
                        [NicknameProperty] = new PlayerProperty(nickname)
                    }
                };
                _session = await MultiplayerService.Instance.JoinSessionByCodeAsync(
                    code.Trim().ToUpperInvariant(), options);
                Subscribe(_session);
                SetStatus("Conectado. Aguardando o mestre iniciar a partida.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                SetStatus(ToFriendlyError(exception));
            }
            finally
            {
                _busy = false;
                Changed?.Invoke();
            }
        }

        public async void LeaveRoom()
        {
            if (_busy || _session == null)
                return;

            _busy = true;
            try
            {
                Unsubscribe(_session);
                await _session.LeaveAsync();
                _session = null;
                DestroyQrTexture();
                SetStatus("Você saiu da sala.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                SetStatus(ToFriendlyError(exception));
            }
            finally
            {
                _busy = false;
                Changed?.Invoke();
            }
        }

        public void StartQrScan()
        {
            if (UnityEngine.Application.platform != RuntimePlatform.Android &&
                UnityEngine.Application.platform != RuntimePlatform.IPhonePlayer)
            {
                SetStatus("A leitura por câmera está disponível na versão para celular.");
                return;
            }
            if (_busy || IsScanningQr)
                return;
            StartCoroutine(ScanQrRoutine());
        }

        public void StopQrScan()
        {
            if (_qrCamera == null)
                return;
            if (_qrCamera.isPlaying)
                _qrCamera.Stop();
            Destroy(_qrCamera);
            _qrCamera = null;
            Changed?.Invoke();
        }

        private System.Collections.IEnumerator ScanQrRoutine()
        {
            SetStatus("Aguardando permissão da câmera...");
            yield return UnityEngine.Application.RequestUserAuthorization(UserAuthorization.WebCam);
            if (!UnityEngine.Application.HasUserAuthorization(UserAuthorization.WebCam))
            {
                SetStatus("Permissão da câmera negada. Você ainda pode digitar o código da sala.");
                yield break;
            }

            var devices = WebCamTexture.devices;
            if (devices == null || devices.Length == 0)
            {
                SetStatus("Nenhuma câmera foi encontrada. Digite o código da sala.");
                yield break;
            }

            var camera = devices.FirstOrDefault(device => !device.isFrontFacing);
            _qrCamera = new WebCamTexture(camera.name ?? devices[0].name, 640, 480, 15);
            _qrCamera.Play();
            SetStatus("Aponte a câmera para o QR Code da sala.");
            var reader = new BarcodeReader();
            while (_qrCamera != null && _qrCamera.isPlaying)
            {
                if (_qrCamera.didUpdateThisFrame && Time.unscaledTime >= _nextQrScanTime &&
                    _qrCamera.width > 16 && _qrCamera.height > 16)
                {
                    _nextQrScanTime = Time.unscaledTime + 0.35f;
                    var decoded = reader.Decode(_qrCamera.GetPixels32(), _qrCamera.width, _qrCamera.height);
                    if (decoded != null && !string.IsNullOrWhiteSpace(decoded.Text))
                    {
                        var code = ExtractSessionCode(decoded.Text);
                        StopQrScan();
                        if (!string.IsNullOrEmpty(code))
                            JoinRoom(PlayerPrefs.GetString("MoneyRank.PlayerNickname", ""), code);
                        else
                            SetStatus("O QR Code não contém um código válido de sala.");
                        yield break;
                    }
                }
                yield return null;
            }
        }

        private static string ExtractSessionCode(string payload)
        {
            var candidate = payload.Trim();
            if (Uri.TryCreate(candidate, UriKind.Absolute, out var uri))
            {
                var query = uri.Query.TrimStart('?').Split('&');
                foreach (var item in query)
                {
                    var pair = item.Split(new[] { '=' }, 2);
                    if (pair.Length == 2 && string.Equals(pair[0], "code", StringComparison.OrdinalIgnoreCase))
                        candidate = Uri.UnescapeDataString(pair[1]);
                }
            }
            candidate = new string(candidate.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
            return candidate.Length >= 4 && candidate.Length <= 16 ? candidate : string.Empty;
        }

        private static Texture2D GenerateQrTexture(string text)
        {
            var writer = new BarcodeWriter
            {
                Format = BarcodeFormat.QR_CODE,
                Options = new QrCodeEncodingOptions { Width = 256, Height = 256, Margin = 2 }
            };
            var pixels = writer.Write(text);
            var texture = new Texture2D(256, 256, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                name = "MoneyRank Join Code QR"
            };
            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }

        private void DestroyQrTexture()
        {
            if (_joinCodeQrTexture == null)
                return;
            Destroy(_joinCodeQrTexture);
            _joinCodeQrTexture = null;
        }

        private async Task EnsureSignedIn(string nickname)
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
                await UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            await AuthenticationService.Instance.UpdatePlayerNameAsync(nickname);
        }

        private void Subscribe(ISession session)
        {
            session.PlayerJoined += OnPlayerChanged;
            session.PlayerHasLeft += OnPlayerChanged;
            session.PlayerPropertiesChanged += OnPlayersUpdated;
            session.RemovedFromSession += OnRemovedFromSession;
        }

        private void Unsubscribe(ISession session)
        {
            session.PlayerJoined -= OnPlayerChanged;
            session.PlayerHasLeft -= OnPlayerChanged;
            session.PlayerPropertiesChanged -= OnPlayersUpdated;
            session.RemovedFromSession -= OnRemovedFromSession;
        }

        private void OnPlayerChanged(string _) => Changed?.Invoke();
        private void OnPlayersUpdated() => Changed?.Invoke();

        private void OnRemovedFromSession()
        {
            if (_session != null)
                Unsubscribe(_session);
            _session = null;
            SetStatus("A sala foi encerrada ou a conexão foi perdida.");
        }

        private void SetStatus(string message)
        {
            Status = message ?? string.Empty;
            Changed?.Invoke();
        }

        private static string ToFriendlyError(Exception exception)
        {
            if (exception is SessionException sessionException)
            {
                switch (sessionException.Error)
                {
                    case SessionError.SessionNotFound:
                        return "Sala não encontrada. Confira o código e se o mestre ainda está conectado.";
                    case SessionError.SessionDeleted:
                        return "Esta sala foi encerrada pelo mestre.";
                    case SessionError.RateLimitExceeded:
                        return "Muitas tentativas em pouco tempo. Aguarde um pouco e tente novamente.";
                    case SessionError.NetworkSetupFailed:
                    case SessionError.NetworkManagerStartFailed:
                    case SessionError.TransportInvalid:
                    case SessionError.TransportComponentMissing:
                        return "Falha ao preparar a conexão de rede. Verifique a internet e tente novamente.";
                    case SessionError.InvalidPlayerName:
                        return "O nickname não foi aceito. Escolha outro nome e tente novamente.";
                }
            }

            var details = GetExceptionDetails(exception);
            if (details.IndexOf("full", StringComparison.OrdinalIgnoreCase) >= 0)
                return "A sala está cheia.";
            if (details.IndexOf("deleted", StringComparison.OrdinalIgnoreCase) >= 0 ||
                details.IndexOf("closed", StringComparison.OrdinalIgnoreCase) >= 0 ||
                details.IndexOf("expired", StringComparison.OrdinalIgnoreCase) >= 0)
                return "A sala foi encerrada ou expirou. Peça ao mestre para criar outra.";
            if (details.IndexOf("not found", StringComparison.OrdinalIgnoreCase) >= 0 ||
                details.IndexOf("invalid character", StringComparison.OrdinalIgnoreCase) >= 0 ||
                details.IndexOf("code", StringComparison.OrdinalIgnoreCase) >= 0 &&
                details.IndexOf("invalid", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Código não encontrado ou inválido. Confira o código exibido pelo mestre.";
            if (details.IndexOf("timeout", StringComparison.OrdinalIgnoreCase) >= 0)
                return "A conexão demorou demais. Verifique a internet e tente novamente.";
            if (details.IndexOf("network", StringComparison.OrdinalIgnoreCase) >= 0 ||
                details.IndexOf("connection", StringComparison.OrdinalIgnoreCase) >= 0 ||
                details.IndexOf("relay", StringComparison.OrdinalIgnoreCase) >= 0 ||
                details.IndexOf("request failed", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Falha de conexão com os serviços online. Verifique a internet e tente novamente.";
            return $"Não foi possível concluir a operação online. Detalhe: {details}";
        }

        private static string GetExceptionDetails(Exception exception)
        {
            var parts = new List<string>();
            for (var current = exception; current != null; current = current.InnerException)
                if (!string.IsNullOrWhiteSpace(current.Message))
                    parts.Add(current.Message.Trim());
            return string.Join(" | ", parts);
        }

        private static void EnsureNetworkManager()
        {
            if (NetworkManager.Singleton != null)
                return;

            var networkObject = new GameObject("MoneyRank Online Network");
            DontDestroyOnLoad(networkObject);
            var transport = networkObject.AddComponent<UnityTransport>();
            var manager = networkObject.AddComponent<NetworkManager>();
            manager.NetworkConfig = new NetworkConfig
            {
                NetworkTransport = transport,
                PlayerPrefab = null,
                EnableSceneManagement = false
            };
        }

        private void OnDestroy()
        {
            if (_session != null)
                Unsubscribe(_session);
            StopQrScan();
            DestroyQrTexture();
        }

        public readonly struct RoomMember
        {
            public RoomMember(string nickname, bool isHost)
            {
                Nickname = nickname;
                IsHost = isHost;
            }

            public string Nickname { get; }
            public bool IsHost { get; }
        }
    }
}
