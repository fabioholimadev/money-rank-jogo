using System.Collections.Generic;
using System.Linq;
using MoneyRank.Domain.V4;
using UnityEngine;

namespace MoneyRank.V4.Presentation
{
    /// <summary>Walks a token along waypoints with a small hop. Animation only represents confirmed state.</summary>
    public sealed class V4Token : MonoBehaviour
    {
        public float Speed = 7f;
        public float Hop = 0.18f;
        private readonly Queue<Vector3> _path = new Queue<Vector3>();
        private Vector3 _from, _to;
        private float _t = 1f, _len = 1f;
        public Vector3 Rest { get; private set; }
        public bool Busy => _t < 1f || _path.Count > 0;

        public void Snap(Vector3 p) { _path.Clear(); transform.position = p; Rest = p; _t = 1f; }

        public void MoveAlong(IEnumerable<Vector3> points)
        {
            foreach (var p in points) _path.Enqueue(p);
            if (_path.Count > 0) Rest = _path.Last();
        }

        private void Update()
        {
            if (_t >= 1f)
            {
                if (_path.Count == 0) return;
                _from = transform.position;
                _to = _path.Dequeue();
                _len = Mathf.Max(0.05f, Vector3.Distance(_from, _to));
                _t = 0f;
                var dir = _to - _from; dir.y = 0f;
                if (dir.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
            }
            _t = Mathf.Min(1f, _t + Time.deltaTime * Speed / _len);
            var pos = Vector3.Lerp(_from, _to, _t);
            pos.y += Mathf.Sin(_t * Mathf.PI) * Hop * Mathf.Min(1f, _len);
            transform.position = pos;
        }
    }

    /// <summary>
    /// Builds the 3D board from the V4 content: 36 houses with their official tiles, the 15x15 Ludo plate driven by the route data,
    /// capybaras (external track), chess-style pawns (internal track), hospital and coffer props and a stylised environment.
    /// </summary>
    public sealed partial class V4BoardView : MonoBehaviour
    {
        public const float Tile = 1f;
        public const float PlateSize = 8.4f;
        private const float Cell = PlateSize / 15f;

        private V4ContentSet _c;
        private Material _lit, _unlit;
        private readonly Vector3[] _ring = new Vector3[36];
        private readonly Dictionary<string, V4Token> _capys = new Dictionary<string, V4Token>();
        private readonly Dictionary<string, V4Token[]> _pawns = new Dictionary<string, V4Token[]>();
        private readonly Dictionary<string, int> _lastPos = new Dictionary<string, int>();
        private GameObject _highlight;
        private Renderer _highlightRenderer;
        private TextMesh _cofferText;
        private float _highlightBase = 1.08f;
        private Font _font;

        public Vector3 RingPosition(int index) => _ring[index];
        public Vector3 HouseWorld(int unityIndex) => _ring[unityIndex];

        public const float CornerSize = 2.4f;
        public const float Half = 6.4f;
        private const float CornerCenter = Half - CornerSize / 2f;   // 5.2

        public static bool IsCorner(int i) => i % 9 == 0;

        /// <summary>Corners are big squares (CornerSize); the 8 houses of each side sit on the corner centre line.</summary>
        public static Vector3 RingCell(int i)
        {
            const float c = CornerCenter;
            if (i == 0) return new Vector3(-c, 0f, c);
            if (i == 9) return new Vector3(c, 0f, c);
            if (i == 18) return new Vector3(c, 0f, -c);
            if (i == 27) return new Vector3(-c, 0f, -c);
            if (i < 9) return new Vector3(-3.5f + (i - 1), 0f, c);
            if (i < 18) return new Vector3(c, 0f, 3.5f - (i - 10));
            if (i < 27) return new Vector3(3.5f - (i - 19), 0f, -c);
            return new Vector3(-c, 0f, -3.5f + (i - 28));
        }

        private static Vector3 LudoWorld(float row, float col, float y)
            => new Vector3((col - 7.5f) * Cell, y, (7.5f - row) * Cell);

        // ------------------------------------------------------------------ build

        public void Build(V4ContentSet content, V4State state)
        {
            _c = content;
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _lit = Resources.Load<Material>("V4/Materials/V4_Lit") ?? MakeBase("Universal Render Pipeline/Lit", "Standard");
            _unlit = Resources.Load<Material>("V4/Materials/V4_Unlit") ?? MakeBase("Universal Render Pipeline/Unlit", "Unlit/Texture");
            for (var i = 0; i < 36; i++) _ring[i] = RingCell(i);

            BuildEnvironment();
            BuildFrame();
            BuildTiles();
            BuildLudoPlate();
            BuildProps();
            BuildTokens(state);
            BuildHighlight();
            Refresh(state, true);
        }

        private static Material MakeBase(string shader, string fallback)
        {
            var s = Shader.Find(shader) ?? Shader.Find(fallback);
            if (s == null) throw new System.InvalidOperationException("No usable shader found (" + shader + "). Run MoneyRank/V4/Fix Render Setup in the Editor.");
            return new Material(s);
        }

        private Material Lit(Color color, float smooth = 0.25f)
        {
            var m = new Material(_lit) { color = color };
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smooth);
            return m;
        }

        private Material Tex(Texture tex)
        {
            var m = new Material(_unlit);
            m.mainTexture = tex;
            return m;
        }

        private GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        private GameObject FlatQuad(string name, Transform parent, Vector3 pos, float size, Material mat)
        {
            var q = Prim(PrimitiveType.Quad, name, parent, pos, new Vector3(size, size, 1f), mat);
            q.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            return q;
        }

        private static Texture2D _white;
        private static Texture2D WhiteTex()
        {
            if (_white == null) { _white = new Texture2D(2, 2); _white.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white }); _white.Apply(); }
            return _white;
        }

        private void BuildFrame()
        {
            var frame = new GameObject("BoardFrame").transform;
            frame.SetParent(transform, false);
            var black = Lit(new Color(0.03f, 0.03f, 0.035f), 0.7f);
            var gold = Lit(new Color(0.86f, 0.66f, 0.2f), 0.9f);
            const float size = Half * 2f;
            Prim(PrimitiveType.Cube, "Base", frame, new Vector3(0f, -0.2f, 0f), new Vector3(size + 0.6f, 0.32f, size + 0.6f), black);
            Prim(PrimitiveType.Cube, "GoldEdge", frame, new Vector3(0f, -0.055f, 0f), new Vector3(size + 0.36f, 0.05f, size + 0.36f), gold);
            Prim(PrimitiveType.Cube, "Felt", frame, new Vector3(0f, -0.005f, 0f), new Vector3(size, 0.02f, size), Lit(new Color(0.04f, 0.045f, 0.055f), 0.15f));
            foreach (var sx in new[] { -1f, 1f })
                foreach (var sz in new[] { -1f, 1f })
                    Prim(PrimitiveType.Cylinder, "Stud", frame, new Vector3(sx * (Half + 0.22f), 0.03f, sz * (Half + 0.22f)), new Vector3(0.4f, 0.07f, 0.4f), gold);
        }

        private void BuildTiles()
        {
            var root = new GameObject("Houses").transform;
            root.SetParent(transform, false);
            var goldMat = Lit(new Color(0.86f, 0.66f, 0.2f), 0.9f);
            for (var i = 0; i < 36; i++)
            {
                var house = _c.HouseAt(i);
                var corner = IsCorner(i);
                FlatQuad("Border " + house.Id, root, _ring[i] + new Vector3(0f, 0.008f, 0f), corner ? CornerSize : 1.0f, goldMat);
                var tex = V4Assets.Icon(house.Id);
                var mat = tex != null ? Tex(tex) : Lit(V4Assets.HouseColor(house.Id));
                var q = FlatQuad(house.Id, root, _ring[i] + new Vector3(0f, 0.02f, 0f), corner ? CornerSize - 0.1f : 0.94f, mat);
                q.name = house.Id + " " + house.Title;
                var labelPos = corner ? new Vector3(-1.02f, 0.045f, Mathf.Sign(_ring[i].z) * 1.0f) : new Vector3(-0.31f, 0.045f, 0.33f);
                AddLabel(q.transform.parent, (i + 1).ToString("00"), _ring[i] + labelPos, corner ? 0.055f : 0.045f, Color.white, corner && _ring[i].z < 0f ? TextAnchor.LowerLeft : TextAnchor.UpperLeft, 90f);
            }
        }

        private TextMesh AddLabel(Transform parent, string text, Vector3 pos, float charSize, Color color, TextAnchor anchor, float pitch)
        {
            var go = new GameObject("Label " + text);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(pitch, pitch == 0f ? 180f : 0f, 0f);
            var tm = go.AddComponent<TextMesh>();
            tm.text = text; tm.characterSize = charSize; tm.fontSize = 64; tm.anchor = anchor; tm.color = color; tm.font = _font;
            go.GetComponent<MeshRenderer>().sharedMaterial = _font.material;
            return tm;
        }

        // ------------------------------------------------------------------ Ludo plate (generated from route data)

        private static readonly Color32 CWhite = new Color32(246, 246, 240, 255);
        private static readonly Color32 CGrid = new Color32(200, 200, 196, 255);

        private void BuildLudoPlate()
        {
            const int px = 48;
            var size = 15 * px;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var data = new Color32[size * size];
            for (var i = 0; i < data.Length; i++) data[i] = new Color32(20, 36, 48, 255);

            System.Action<int, int, Color32> setCell = (r, c, col) =>
            {
                for (var y = 0; y < px; y++)
                    for (var x = 0; x < px; x++)
                    {
                        var edge = x < 2 || y < 2 || x >= px - 2 || y >= px - 2;
                        data[((14 - r) * px + y) * size + c * px + x] = edge ? (Color32)Color.Lerp(col, new Color(0.1f, 0.14f, 0.18f), 0.35f) : col;
                    }
            };
            System.Action<int, int, int, int, Color32> fillBlock = (r0, c0, r1, c1, col) =>
            {
                for (var r = r0; r <= r1; r++) for (var c = c0; c <= c1; c++)
                    for (var y = 0; y < px; y++) for (var x = 0; x < px; x++) data[((14 - r) * px + y) * size + c * px + x] = col;
            };

            var colors = new Dictionary<string, Color32>
            {
                { "verde", V4Assets.PawnColor("verde") }, { "vermelho", V4Assets.PawnColor("vermelho") },
                { "azul", V4Assets.PawnColor("azul") }, { "amarelo", V4Assets.PawnColor("amarelo") }
            };
            // bases
            fillBlock(0, 0, 5, 5, colors["verde"]); fillBlock(0, 9, 5, 14, colors["vermelho"]);
            fillBlock(9, 9, 14, 14, colors["azul"]); fillBlock(9, 0, 14, 5, colors["amarelo"]);
            // base inner panel + dots
            foreach (var kv in new[] { ("verde", 0, 0), ("vermelho", 0, 9), ("azul", 9, 9), ("amarelo", 9, 0) })
            {
                fillBlock(kv.Item2 + 1, kv.Item3 + 1, kv.Item2 + 4, kv.Item3 + 4, CWhite);
                foreach (var d in BaseSlots(kv.Item1))
                    DrawDisc(data, size, px, d.x, d.y, 0.62f, colors[kv.Item1]);
            }
            // paths
            foreach (var color in _c.RouteColors)
            {
                foreach (var cell in _c.RouteOf(color))
                {
                    if (cell.IsGoal) continue;
                    Color32 tint = CWhite;
                    if (cell.Kind == "privado") tint = colors[color];
                    else if (cell.Index == 0) tint = Color.Lerp(CWhite, colors[color], 0.7f);
                    setCell(cell.Row, cell.Col, tint);
                }
            }
            // protected shared cells get a small mark
            foreach (var cell in _c.Routes.Where(r => r.IsShared && r.Protected && r.Index != 0))
                DrawDisc(data, size, px, cell.Col + 0.5f, cell.Row + 0.5f, 0.22f, new Color32(150, 150, 150, 255));
            // centre: four triangles (green left, red top, blue right, yellow bottom)
            for (var r = 6; r <= 8; r++)
                for (var c = 6; c <= 8; c++)
                    for (var y = 0; y < px; y++)
                        for (var x = 0; x < px; x++)
                        {
                            var fx = c + (x + 0.5f) / px - 7.5f;
                            var fy = (r + (px - y - 0.5f) / px) - 7.5f; // +down in row space
                            string owner;
                            if (Mathf.Abs(fx) > Mathf.Abs(fy)) owner = fx < 0 ? "verde" : "azul";
                            else owner = fy < 0 ? "vermelho" : "amarelo";
                            data[((14 - r) * px + y) * size + c * px + x] = colors[owner];
                        }
            tex.SetPixels32(data);
            tex.Apply(true);
            var plate = FlatQuad("LudoPlate", transform, new Vector3(0f, 0.015f, 0f), PlateSize, Tex(tex));
            plate.transform.SetParent(transform, false);
            Prim(PrimitiveType.Cube, "LudoRim", transform, new Vector3(0f, 0.004f, 0f), new Vector3(PlateSize + 0.22f, 0.02f, PlateSize + 0.22f), Lit(new Color(0.95f, 0.95f, 0.92f), 0.4f));
        }

        private static void DrawDisc(Color32[] data, int size, int px, float cx, float cy, float radiusCells, Color32 color)
        {
            var minX = Mathf.Max(0, (int)((cx - radiusCells) * px)); var maxX = Mathf.Min(size - 1, (int)((cx + radiusCells) * px));
            var minY = Mathf.Max(0, (int)((15f - cy - radiusCells) * px)); var maxY = Mathf.Min(size - 1, (int)((15f - cy + radiusCells) * px));
            for (var y = minY; y <= maxY; y++)
                for (var x = minX; x <= maxX; x++)
                {
                    var dx = (x + 0.5f) / px - cx; var dy = (y + 0.5f) / px - (15f - cy);
                    if (dx * dx + dy * dy <= radiusCells * radiusCells) data[y * size + x] = color;
                }
        }

        /// <summary>Four pawn slots inside each colour base in (col,row) continuous cell coordinates.</summary>
        private static IEnumerable<(float x, float y)> BaseSlots(string color)
        {
            float r0 = 0, c0 = 0;
            if (color == "vermelho") { c0 = 9; } else if (color == "azul") { r0 = 9; c0 = 9; } else if (color == "amarelo") { r0 = 9; }
            foreach (var (dr, dc) in new[] { (1.7f, 1.7f), (1.7f, 4.3f), (4.3f, 1.7f), (4.3f, 4.3f) })
                yield return (c0 + dc, r0 + dr);
        }

        // ------------------------------------------------------------------ props

        // ------------------------------------------------------------------ tokens

        private static Color ProfessionAccent(string professionId)
        {
            switch (professionId)
            {
                case "P01": return new Color(0.55f, 0.35f, 0.95f);
                case "P02": return new Color(0.95f, 0.95f, 0.98f);
                case "P03": return new Color(1f, 0.82f, 0.2f);
                case "P04": return new Color(1f, 0.5f, 0.1f);
                case "P05": return new Color(0.2f, 0.85f, 0.75f);
                default: return new Color(0.4f, 0.45f, 0.5f);
            }
        }

        private GameObject BuildCapybara(PlayerState p, int slot)
        {
            var root = new GameObject("Capivara " + p.Name);
            root.transform.SetParent(transform, false);
            var model = Resources.Load<GameObject>("Models/Capybara/capybara");
            var brown = new Color(0.52f, 0.36f, 0.22f);
            if (model != null)
            {
                var inst = Instantiate(model, root.transform, false);
                var renderers = inst.GetComponentsInChildren<Renderer>();
                var b = renderers[0].bounds;
                foreach (var r in renderers) b.Encapsulate(r.bounds);
                var scale = 0.74f / Mathf.Max(0.001f, Mathf.Max(b.size.x, b.size.z));
                inst.transform.localScale = Vector3.one * scale;
                inst.transform.localPosition = new Vector3(0f, -b.min.y * scale, 0f);
                foreach (var r in renderers) { r.material = Lit(brown, 0.15f); }
            }
            else
            {
                Prim(PrimitiveType.Capsule, "Body", root.transform, new Vector3(0f, 0.3f, 0f), new Vector3(0.45f, 0.3f, 0.7f), Lit(brown));
            }
            // colour disc + profession accessory (identifies the profession without relying on colour alone)
            var pawnColor = V4Assets.PawnColor(p.Color);
            Prim(PrimitiveType.Cylinder, "Disc", root.transform, new Vector3(0f, 0.015f, 0f), new Vector3(0.62f, 0.015f, 0.62f), Lit(pawnColor, 0.4f));
            var accent = Lit(ProfessionAccent(p.ProfessionId), 0.5f);
            var top = new Vector3(0f, 0.52f, 0f);
            switch (p.ProfessionId)
            {
                case "P01": Prim(PrimitiveType.Cube, "Book", root.transform, top, new Vector3(0.26f, 0.05f, 0.2f), accent); break;
                case "P02":
                    Prim(PrimitiveType.Cube, "CrossV", root.transform, top, new Vector3(0.08f, 0.26f, 0.05f), accent);
                    Prim(PrimitiveType.Cube, "CrossH", root.transform, top, new Vector3(0.26f, 0.08f, 0.05f), accent); break;
                case "P03": Prim(PrimitiveType.Cylinder, "Coin", root.transform, top, new Vector3(0.24f, 0.02f, 0.24f), accent).transform.localRotation = Quaternion.Euler(90f, 0f, 0f); break;
                case "P04": Prim(PrimitiveType.Sphere, "Ball", root.transform, top, Vector3.one * 0.22f, accent); break;
                case "P05": Prim(PrimitiveType.Cube, "Case", root.transform, top, new Vector3(0.24f, 0.17f, 0.1f), accent); break;
                default: Prim(PrimitiveType.Capsule, "Wheel", root.transform, top, new Vector3(0.1f, 0.14f, 0.1f), accent).transform.localRotation = Quaternion.Euler(0f, 0f, 90f); break;
            }
            root.AddComponent<V4Token>();
            return root;
        }

        private GameObject BuildChessPawn(string colorName, string name)
        {
            var c = V4Assets.PawnColor(colorName);
            var m = Lit(c, 0.55f);
            var root = new GameObject(name);
            root.transform.SetParent(transform, false);
            Prim(PrimitiveType.Cylinder, "Foot", root.transform, new Vector3(0f, 0.03f, 0f), new Vector3(0.34f, 0.03f, 0.34f), m);
            Prim(PrimitiveType.Cylinder, "Stem", root.transform, new Vector3(0f, 0.2f, 0f), new Vector3(0.17f, 0.14f, 0.17f), m);
            Prim(PrimitiveType.Cylinder, "Collar", root.transform, new Vector3(0f, 0.34f, 0f), new Vector3(0.26f, 0.015f, 0.26f), m);
            Prim(PrimitiveType.Sphere, "Head", root.transform, new Vector3(0f, 0.45f, 0f), Vector3.one * 0.22f, m);
            root.AddComponent<V4Token>().Hop = 0.1f;
            return root;
        }

        private void BuildTokens(V4State state)
        {
            for (var i = 0; i < state.Players.Count; i++)
            {
                var p = state.Players[i];
                _capys[p.Id] = BuildCapybara(p, i).GetComponent<V4Token>();
                var pawns = new V4Token[p.Pawns.Length];
                for (var k = 0; k < pawns.Length; k++)
                    pawns[k] = BuildChessPawn(p.Color, "Peao " + p.Name + " " + (k + 1)).GetComponent<V4Token>();
                _pawns[p.Id] = pawns;
                _lastPos[p.Id] = p.Position;
            }
        }

        private void BuildHighlight()
        {
            _highlight = FlatQuad("TurnHighlight", transform, new Vector3(0f, 0.012f, 0f), 1.08f, Tex(WhiteTex()));
            _highlightRenderer = _highlight.GetComponent<Renderer>();
        }

        // ------------------------------------------------------------------ refresh

        private Vector3 TokenSpot(int pos, int slot, int count)
            => _ring[pos] + new Vector3(0f, 0.04f, 0f) + SlotOffset(slot, count) * (IsCorner(pos) ? 2.6f : 1f);

        private static Vector3 SlotOffset(int slot, int count)
        {
            if (count <= 1) return Vector3.zero;
            var offs = new[] { new Vector3(-0.2f, 0f, 0.2f), new Vector3(0.2f, 0f, 0.2f), new Vector3(-0.2f, 0f, -0.2f), new Vector3(0.2f, 0f, -0.2f) };
            return offs[slot % 4];
        }

        public void Refresh(V4State s, bool snap = false)
        {
            if (_c == null) return;
            var current = s.TurnOrder.Count > 0 ? s.Players.First(p => p.Id == s.TurnOrder[s.CurrentIndex]) : null;

            for (var i = 0; i < s.Players.Count; i++)
            {
                var p = s.Players[i];
                var token = _capys[p.Id];
                token.gameObject.SetActive(!p.Eliminated);
                var target = TokenSpot(p.Position, i, s.Players.Count);
                int last;
                if (!_lastPos.TryGetValue(p.Id, out last)) last = p.Position;
                if (snap) token.Snap(target);
                else if (last != p.Position)
                {
                    var forward = (p.Position - last + 36) % 36;
                    if (forward > 0 && forward <= 6)
                    {
                        var pts = new List<Vector3>();
                        for (var step = 1; step <= forward; step++) pts.Add(TokenSpot((last + step) % 36, i, s.Players.Count));
                        token.MoveAlong(pts);
                    }
                    else token.MoveAlong(new[] { target });
                }
                _lastPos[p.Id] = p.Position;

                var route = _c.RouteOf(p.Color);
                var cellUse = new Dictionary<string, int>();
                for (var k = 0; k < p.Pawns.Length; k++)
                {
                    var pawn = p.Pawns[k];
                    Vector3 pos;
                    if (pawn.RouteIndex < 0)
                    {
                        var slot = BaseSlots(p.Color).ElementAt(k);
                        pos = LudoWorld(slot.y, slot.x, 0.03f);
                    }
                    else
                    {
                        var cell = route[Mathf.Min(pawn.RouteIndex, route.Count - 1)];
                        var key = cell.GlobalCell;
                        int used; cellUse.TryGetValue(key, out used); cellUse[key] = used + 1;
                        var off = cell.IsGoal ? new Vector3((k - 1.5f) * 0.12f, 0f, 0f) : SlotOffset(used, 2) * 0.35f;
                        pos = LudoWorld(cell.Row + 0.5f, cell.Col + 0.5f, 0.03f) + off;
                    }
                    var tk = _pawns[p.Id][k];
                    tk.gameObject.SetActive(!p.Eliminated);
                    if (snap || (tk.transform.position - pos).sqrMagnitude > 0.0001f && !tk.Busy) { if (snap) tk.Snap(pos); else tk.MoveAlong(new[] { pos }); }
                    tk.transform.localScale = Vector3.one * (k == p.ActivePawn && !pawn.Completed ? 1.18f : 1f);
                }
            }

            _highlight.SetActive(current != null);
            if (current != null)
            {
                _highlight.transform.position = _ring[current.Position] + new Vector3(0f, 0.012f, 0f);
                _highlightBase = IsCorner(current.Position) ? CornerSize + 0.14f : 1.08f;
                _highlightRenderer.material.color = V4Assets.PawnColor(current.Color);
            }
            if (_cofferText != null) _cofferText.text = CofferProtocol.DisplayText(s.FundCash);
        }

        private void Update()
        {
            SceneryUpdate();
            if (_highlightRenderer != null)
            {
                var pulse = 0.55f + 0.45f * Mathf.Sin(Time.time * 4f);
                var c = _highlightRenderer.material.color; c.a = 1f;
                var hs = _highlightBase + pulse * 0.06f;
                _highlight.transform.localScale = new Vector3(hs, hs, 1f);
            }
        }
    }
}
