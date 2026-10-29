using UnityEngine;

namespace MoneyRank.Presentation.Board
{
    [DefaultExecutionOrder(10000)]
    [DisallowMultipleComponent]
    public sealed class PauseMenuView : MonoBehaviour
    {
        private static readonly Color Navy = new Color32(8, 24, 39, 250);
        private static readonly Color Gold = new Color32(245, 197, 66, 255);
        private static readonly Color Yellow = new Color32(255, 217, 102, 255);
        private static readonly Color Orange = new Color32(242, 140, 40, 255);
        private static readonly Color Red = new Color32(190, 38, 48, 255);
        private static readonly Color White = new Color32(255, 255, 255, 255);
        private static readonly Color Muted = new Color32(205, 215, 222, 255);

        private BoardSandboxController _controller;
        private GUIStyle _pauseButtonStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _bodyStyle;
        private GUIStyle _actionStyle;
        private float _styleScale = -1f;
        private string _message = string.Empty;

        public void Bind(BoardSandboxController controller)
        {
            _controller = controller;
        }

        private void OnGUI()
        {
            if (_controller?.Session == null)
                return;

            var safe = UnityEngine.Application.isMobilePlatform
                ? Screen.safeArea
                : new Rect(0f, 0f, Screen.width, Screen.height);
            var top = Screen.height - safe.yMax;
            var scale = Mathf.Clamp(Mathf.Min(safe.width / 1280f, safe.height / 720f), 0.55f, 1.1f);
            EnsureStyles(scale);

            var oldColor = GUI.color;
            var oldBackground = GUI.backgroundColor;
            var oldDepth = GUI.depth;
            GUI.depth = -100;
            GUI.color = Color.white;
            GUI.backgroundColor = Gold;

            var pauseRect = new Rect(safe.x + 14f * scale, top + 12f * scale,
                116f * scale, 42f * scale);
            if (!_controller.IsPaused)
            {
                if (GUI.Button(pauseRect, "Ⅱ  PAUSAR", _pauseButtonStyle))
                {
                    _message = string.Empty;
                    _controller.SetPaused(true);
                }
            }
            else
            {
                DrawPauseOverlay(safe, top, scale);
            }

            GUI.color = oldColor;
            GUI.backgroundColor = oldBackground;
            GUI.depth = oldDepth;
        }

        private void DrawPauseOverlay(Rect safe, float safeTop, float scale)
        {
            GUI.color = new Color(0f, 0f, 0f, 0.68f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            var width = Mathf.Min(520f * scale, safe.width - 28f * scale);
            var height = Mathf.Min(340f * scale, safe.height - 24f * scale);
            var panel = new Rect(safe.x + (safe.width - width) * 0.5f,
                safeTop + (safe.height - height) * 0.5f, width, height);
            DrawFramedPanel(panel, Navy, Gold, Mathf.Max(1f, 2f * scale));

            var padding = 28f * scale;
            GUI.Label(new Rect(panel.x + padding, panel.y + 15f * scale,
                panel.width - padding * 2f, 56f * scale), "JOGO PAUSADO", _titleStyle);
            GUI.Label(new Rect(panel.x + padding, panel.y + 72f * scale,
                panel.width - padding * 2f, 44f * scale),
                "Seu progresso permanece no mesmo ponto.", _bodyStyle);

            var buttonWidth = Mathf.Min(panel.width - padding * 2f, 400f * scale);
            var buttonHeight = 48f * scale;
            var buttonX = panel.center.x - buttonWidth * 0.5f;
            var continueRect = new Rect(buttonX, panel.y + height * 0.46f,
                buttonWidth, buttonHeight);
            GUI.backgroundColor = Gold;
            if (GUI.Button(continueRect, "CONTINUAR", _actionStyle))
                _controller.SetPaused(false);

            var exitRect = new Rect(buttonX, continueRect.yMax + 12f * scale,
                buttonWidth, buttonHeight);
            GUI.backgroundColor = Red;
            if (GUI.Button(exitRect, "SAIR E SALVAR", _actionStyle))
            {
                if (_controller.TrySaveCurrentGame(out var error))
                    _controller.ReturnToMenuAfterSaving();
                else
                    _message = error;
            }

            if (_controller.IsOnlineSession && string.IsNullOrEmpty(_message))
                _message = "A partida online ainda não permite salvar e retomar.";
            if (!string.IsNullOrEmpty(_message))
            {
                GUI.color = Yellow;
                GUI.Label(new Rect(panel.x + padding, exitRect.yMax + 8f * scale,
                    panel.width - padding * 2f, Mathf.Max(28f * scale, panel.yMax - exitRect.yMax - 12f * scale)),
                    _message, _bodyStyle);
                GUI.color = Color.white;
            }

            GUI.backgroundColor = Color.white;
        }

        private void EnsureStyles(float scale)
        {
            if (_titleStyle != null && Mathf.Approximately(_styleScale, scale))
                return;

            _styleScale = scale;
            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(32f * scale),
                fontStyle = FontStyle.Bold,
                normal = { textColor = Gold }
            };
            _bodyStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(15f * scale),
                wordWrap = true,
                normal = { textColor = Muted }
            };
            _pauseButtonStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(16f * scale),
                fontStyle = FontStyle.Bold,
                normal = { textColor = Navy },
                hover = { textColor = Navy },
                active = { textColor = Navy },
                focused = { textColor = Navy }
            };
            _actionStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(17f * scale),
                fontStyle = FontStyle.Bold,
                normal = { textColor = White },
                hover = { textColor = White },
                active = { textColor = White },
                focused = { textColor = White }
            };
            SetFlatBackgrounds(_pauseButtonStyle);
            SetFlatBackgrounds(_actionStyle);
        }

        private static void DrawFramedPanel(Rect rect, Color fill, Color border, float thickness)
        {
            GUI.color = border;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = fill;
            GUI.DrawTexture(new Rect(rect.x + thickness, rect.y + thickness,
                Mathf.Max(1f, rect.width - thickness * 2f),
                Mathf.Max(1f, rect.height - thickness * 2f)), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private static void SetFlatBackgrounds(GUIStyle style)
        {
            style.normal.background = Texture2D.whiteTexture;
            style.hover.background = Texture2D.whiteTexture;
            style.active.background = Texture2D.whiteTexture;
            style.focused.background = Texture2D.whiteTexture;
        }
    }
}