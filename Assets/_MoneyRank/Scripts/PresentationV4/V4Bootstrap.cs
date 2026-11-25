using MoneyRank.Domain.V4;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MoneyRank.V4.Presentation
{
    /// <summary>Entry point of the V4 scene: setup -> match. Builds the board and HUD at runtime from the shipped content.</summary>
    public sealed class V4Bootstrap : MonoBehaviour
    {
        private V4Hud _hud;
        private V4BoardView _board;
        private V4Session _session;
        public V4Session Session => _session;
        public V4Hud Hud => _hud;
        private float _yaw, _pitch = 49f, _dist = 27f;
        private Vector3 _target = new Vector3(0f, 0f, -0.9f);

        private void Awake()
        {
            Application.targetFrameRate = 60;
            var hudGo = new GameObject("V4 HUD");
            hudGo.transform.SetParent(transform, false);
            _hud = hudGo.AddComponent<V4Hud>();
            _hud.Init();
            _hud.SessionStarted = StartSession;
            _hud.BackToSetup = ReturnToSetup;
            _hud.AnimateDice = (a, b, done) => { if (_board != null) _board.RollDice(a, b, done); else done?.Invoke(); };
            _hud.AnimateCoin = (happy, done) => { if (_board != null) _board.FlipCoin(happy, done); else done?.Invoke(); };
            ShowcaseBoard();
            _hud.ShowSetup();
        }

        private void ShowcaseBoard()
        {
            RebuildBoard(new V4State());
        }

        private void RebuildBoard(V4State state)
        {
            if (_board != null) Destroy(_board.gameObject);
            var go = new GameObject("V4 Board");
            go.transform.SetParent(transform, false);
            _board = go.AddComponent<V4BoardView>();
            _board.Build(V4Assets.Content, state);
        }

        private void StartSession(V4Session session)
        {
            if (_session != null) _session.Changed -= OnChanged;
            _session = session;
            RebuildBoard(session.S);
            _session.Changed += OnChanged;
            _hud.Bind(session);
        }

        /// <summary>Camera pose used by tests/cinematics; the user can still orbit (right mouse) and zoom (wheel).</summary>
        public void ResetView() { SetView(0f, 49f, 27f, new Vector3(0f, 0f, -0.9f)); }

        public void SetView(float yaw, float pitch, float dist, Vector3 target)
        {
            _yaw = yaw; _pitch = pitch; _dist = dist; _target = target;
        }

        private void OnChanged()
        {
            if (_session != null && _board != null) _board.Refresh(_session.S);
        }

        private void ReturnToSetup()
        {
            if (_session != null) { _session.Persist(); _session.Changed -= OnChanged; _session = null; }
            ShowcaseBoard();
            _hud.ShowSetup();
        }

        private void Update()
        {
            if (_session != null) _session.Tick();
        }

        private void LateUpdate()
        {
            var mouse = Mouse.current;
            if (mouse != null)
            {
                if (mouse.rightButton.isPressed)
                {
                    var d = mouse.delta.ReadValue();
                    _yaw += d.x * 0.2f;
                    _pitch = Mathf.Clamp(_pitch - d.y * 0.2f, 25f, 82f);
                }
                var scroll = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f) _dist = Mathf.Clamp(_dist - scroll * 0.012f, 10f, 32f);
            }
            var cam = Camera.main;
            if (cam == null) return;
            var rot = Quaternion.Euler(_pitch, _yaw, 0f);
            cam.transform.position = _target + rot * new Vector3(0f, 0f, -_dist);
            cam.transform.rotation = rot;
            cam.fieldOfView = 38f;
        }
    }
}
