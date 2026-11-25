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
        private float _yaw, _pitch = 54f, _dist = 17.4f;
        private Vector3 _target = new Vector3(0f, 0f, -1.9f);

        private void Awake()
        {
            Application.targetFrameRate = 60;
            var hudGo = new GameObject("V4 HUD");
            hudGo.transform.SetParent(transform, false);
            _hud = hudGo.AddComponent<V4Hud>();
            _hud.Init();
            _hud.SessionStarted = StartSession;
            _hud.BackToSetup = ReturnToSetup;
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
                if (Mathf.Abs(scroll) > 0.01f) _dist = Mathf.Clamp(_dist - scroll * 0.012f, 9f, 26f);
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
