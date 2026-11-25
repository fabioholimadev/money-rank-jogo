using System;
using System.Collections;
using UnityEngine;

namespace MoneyRank.V4.Presentation
{
    /// <summary>Dice-throw and coin-flip animations. They only visualise a result decided by the engine/RNG: never the source of rules.</summary>
    public sealed partial class V4BoardView
    {
        private GameObject[] _dice;
        private GameObject _coin;
        private Coroutine _diceRoutine, _coinRoutine;
        private static Texture2D[] _pipTextures;
        private static Texture2D _happyTexture, _sadTexture;

        public bool Animating => _diceRoutine != null || _coinRoutine != null;

        // ------------------------------------------------------------------ dice

        // opposite faces sum to 7: +Y=1 / -Y=6, +Z=2 / -Z=5, +X=3 / -X=4
        private static Vector3 NormalOfFace(int value)
        {
            switch (value)
            {
                case 1: return Vector3.up;
                case 6: return Vector3.down;
                case 2: return Vector3.forward;
                case 5: return Vector3.back;
                case 3: return Vector3.right;
                default: return Vector3.left;
            }
        }

        private static Texture2D PipTexture(int value)
        {
            if (_pipTextures == null) _pipTextures = new Texture2D[7];
            if (_pipTextures[value] != null) return _pipTextures[value];
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            var bg = new Color32(247, 245, 236, 255);
            for (var i = 0; i < px.Length; i++) px[i] = bg;
            var cells = new[] { 0.22f, 0.5f, 0.78f };
            int[][] layout;
            switch (value)
            {
                case 1: layout = new[] { new[] { 1, 1 } }; break;
                case 2: layout = new[] { new[] { 0, 0 }, new[] { 2, 2 } }; break;
                case 3: layout = new[] { new[] { 0, 0 }, new[] { 1, 1 }, new[] { 2, 2 } }; break;
                case 4: layout = new[] { new[] { 0, 0 }, new[] { 0, 2 }, new[] { 2, 0 }, new[] { 2, 2 } }; break;
                case 5: layout = new[] { new[] { 0, 0 }, new[] { 0, 2 }, new[] { 1, 1 }, new[] { 2, 0 }, new[] { 2, 2 } }; break;
                default: layout = new[] { new[] { 0, 0 }, new[] { 0, 2 }, new[] { 1, 0 }, new[] { 1, 2 }, new[] { 2, 0 }, new[] { 2, 2 } }; break;
            }
            var pip = value == 1 ? new Color32(214, 40, 48, 255) : new Color32(28, 30, 40, 255);
            var radius = value == 1 ? 0.15f : 0.105f;
            foreach (var cell in layout)
            {
                var cx = cells[cell[1]] * size; var cy = cells[cell[0]] * size;
                for (var y = 0; y < size; y++)
                    for (var x = 0; x < size; x++)
                    {
                        var dx = x + 0.5f - cx; var dy = y + 0.5f - cy;
                        if (dx * dx + dy * dy <= radius * size * radius * size) px[y * size + x] = pip;
                    }
            }
            for (var i = 0; i < size; i++) { px[i] = px[i * size] = px[(size - 1) * size + i] = px[i * size + size - 1] = new Color32(205, 200, 188, 255); }
            tex.SetPixels32(px); tex.Apply(true);
            _pipTextures[value] = tex;
            return tex;
        }

        private GameObject MakeDie(string name)
        {
            const float s = 0.85f;
            var die = new GameObject(name);
            die.transform.SetParent(transform, false);
            Prim(PrimitiveType.Cube, "Body", die.transform, Vector3.zero, Vector3.one * s, Lit(new Color(0.96f, 0.95f, 0.9f), 0.5f));
            for (var v = 1; v <= 6; v++)
            {
                var n = NormalOfFace(v);
                var q = Prim(PrimitiveType.Quad, "Face" + v, die.transform, n * (s * 0.5f + 0.002f), Vector3.one * (s * 0.94f), Tex(PipTexture(v)));
                var up = Mathf.Abs(n.y) > 0.9f ? Vector3.forward : Vector3.up;
                q.transform.localRotation = Quaternion.LookRotation(-n, up);   // Quad's visible side faces -forward
            }
            die.SetActive(false);
            return die;
        }

        private static Quaternion RestRotation(int value, float yaw)
            => Quaternion.AngleAxis(yaw, Vector3.up) * Quaternion.FromToRotation(NormalOfFace(value), Vector3.up);

        /// <summary>Tumbles two dice on the board and ends showing exactly (<paramref name="value1"/>, <paramref name="value2"/>).</summary>
        public void RollDice(int value1, int value2, Action onSettled)
        {
            if (_dice == null) _dice = new[] { MakeDie("Die A"), MakeDie("Die B") };
            if (_diceRoutine != null) StopCoroutine(_diceRoutine);
            _diceRoutine = StartCoroutine(DiceRoutine(new[] { value1, value2 }, onSettled));
        }

        private IEnumerator DiceRoutine(int[] values, Action onSettled)
        {
            const float throwTime = 1.5f;
            var rng = new System.Random(values[0] * 31 + values[1] * 7 + Environment.TickCount);
            var starts = new[] { new Vector3(-8.5f, 3.2f, -3.5f), new Vector3(-8.0f, 3.8f, -5.5f) };
            var ends = new[] { new Vector3(-0.95f, 0.45f, 0.25f), new Vector3(1.0f, 0.45f, -0.2f) };
            var axes = new Vector3[2]; var rots = new Quaternion[2]; var finals = new Quaternion[2]; var speeds = new float[2];
            for (var i = 0; i < 2; i++)
            {
                _dice[i].SetActive(true);
                axes[i] = new Vector3((float)rng.NextDouble() - 0.5f, (float)rng.NextDouble() - 0.5f, (float)rng.NextDouble() - 0.5f).normalized;
                speeds[i] = 620f + (float)rng.NextDouble() * 360f;
                rots[i] = UnityEngine.Random.rotation;
                finals[i] = RestRotation(values[i], (float)rng.NextDouble() * 360f);
                _dice[i].transform.position = starts[i];
            }
            for (var t = 0f; t < throwTime; t += Time.deltaTime)
            {
                var k = Mathf.Clamp01(t / throwTime);
                var ease = 1f - Mathf.Pow(1f - k, 2.2f);
                for (var i = 0; i < 2; i++)
                {
                    var d = _dice[i].transform;
                    var pos = Vector3.Lerp(starts[i], ends[i], ease);
                    var arc = Mathf.Sin(Mathf.Min(1f, k * 1.25f) * Mathf.PI) * 2.2f * (1f - k);
                    var bounce = k > 0.55f ? Mathf.Abs(Mathf.Sin((k - 0.55f) * Mathf.PI * 3.4f)) * 0.45f * Mathf.Pow(1f - k, 1.5f) : 0f;
                    pos.y = ends[i].y + arc + bounce;
                    d.position = pos;
                    if (k < 0.78f) { rots[i] = Quaternion.AngleAxis(speeds[i] * Time.deltaTime * (1.15f - k), axes[i]) * rots[i]; d.rotation = rots[i]; }
                    else d.rotation = Quaternion.Slerp(rots[i], finals[i], Mathf.SmoothStep(0f, 1f, (k - 0.78f) / 0.22f));
                }
                yield return null;
            }
            for (var i = 0; i < 2; i++) { _dice[i].transform.position = ends[i]; _dice[i].transform.rotation = finals[i]; }
            yield return new WaitForSeconds(0.75f);
            _diceRoutine = null;
            onSettled?.Invoke();
            yield return new WaitForSeconds(1.1f);
            if (_diceRoutine == null) foreach (var d in _dice) d.SetActive(false);
        }

        // ------------------------------------------------------------------ coin

        private static Texture2D FaceTexture(bool happy)
        {
            if (happy && _happyTexture != null) return _happyTexture;
            if (!happy && _sadTexture != null) return _sadTexture;
            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            var rim = happy ? new Color32(36, 150, 76, 255) : new Color32(190, 48, 52, 255);
            var fill = happy ? new Color32(88, 214, 128, 255) : new Color32(255, 112, 112, 255);
            var ink = new Color32(20, 36, 30, 255);
            for (var y = 0; y < size; y++)
                for (var x = 0; x < size; x++)
                {
                    var fx = (x + 0.5f) / size - 0.5f; var fy = (y + 0.5f) / size - 0.5f;
                    var r = Mathf.Sqrt(fx * fx + fy * fy);
                    Color32 c = new Color32(0, 0, 0, 0);
                    if (r <= 0.5f) c = r > 0.43f ? rim : fill;
                    // eyes
                    if (Mathf.Abs(Mathf.Abs(fx) - 0.14f) < 0.045f && Mathf.Abs(fy - 0.12f) < 0.06f && (fx * fx / 0.002f + (fy - 0.12f) * (fy - 0.12f) / 0.0036f) < 1.2f) c = ink;
                    // mouth: smile (lower arc) or frown (upper arc)
                    var cy = happy ? 0.02f : -0.30f;
                    var rr = Mathf.Sqrt(fx * fx + (fy - cy) * (fy - cy));
                    var onArc = Mathf.Abs(rr - 0.27f) < 0.032f && (happy ? fy < cy - 0.02f : fy > cy + 0.02f) && Mathf.Abs(fx) < 0.26f;
                    if (onArc) c = ink;
                    px[y * size + x] = c;
                }
            tex.SetPixels32(px); tex.Apply(true);
            if (happy) _happyTexture = tex; else _sadTexture = tex;
            return tex;
        }

        private GameObject MakeDisc(string name, Transform parent, float radius, float y, bool up, Material mat)
        {
            const int seg = 48;
            var verts = new Vector3[seg + 1]; var uvs = new Vector2[seg + 1]; var tris = new int[seg * 3];
            verts[0] = Vector3.zero; uvs[0] = new Vector2(0.5f, 0.5f);
            for (var i = 0; i < seg; i++)
            {
                var th = i / (float)seg * Mathf.PI * 2f;
                verts[i + 1] = new Vector3(Mathf.Sin(th) * radius, 0f, Mathf.Cos(th) * radius);
                uvs[i + 1] = new Vector2(0.5f + 0.5f * Mathf.Sin(th), 0.5f + 0.5f * Mathf.Cos(th));
            }
            for (var i = 0; i < seg; i++)
            {
                var a = i + 1; var b = (i + 1) % seg + 1;
                if (up) { tris[i * 3] = 0; tris[i * 3 + 1] = a; tris[i * 3 + 2] = b; }
                else { tris[i * 3] = 0; tris[i * 3 + 1] = b; tris[i * 3 + 2] = a; }
            }
            var mesh = new Mesh { name = name + " Mesh", vertices = verts, uv = uvs, triangles = tris };
            mesh.RecalculateNormals();
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, y, 0f);
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }

        private GameObject MakeCoin()
        {
            var coin = new GameObject("Coin Fiscal");
            coin.transform.SetParent(transform, false);
            Prim(PrimitiveType.Cylinder, "Edge", coin.transform, Vector3.zero, new Vector3(1.0f, 0.035f, 1.0f), Lit(new Color(0.85f, 0.65f, 0.2f), 0.9f));
            MakeDisc("Happy", coin.transform, 0.5f, 0.0365f, true, Tex(FaceTexture(true)));
            MakeDisc("Sad", coin.transform, 0.5f, -0.0365f, false, Tex(FaceTexture(false)));
            coin.SetActive(false);
            return coin;
        }

        /// <summary>Flips the two-faced fiscal coin and lands showing FELIZ (happy) or TRISTE (sad) facing up.</summary>
        public void FlipCoin(bool happy, Action onSettled)
        {
            if (_coin == null) _coin = MakeCoin();
            if (_coinRoutine != null) StopCoroutine(_coinRoutine);
            _coinRoutine = StartCoroutine(CoinRoutine(happy, onSettled));
        }

        private IEnumerator CoinRoutine(bool happy, Action onSettled)
        {
            const float flipTime = 1.7f;
            _coin.SetActive(true);
            var start = new Vector3(-6.5f, 0.6f, -3.2f);
            var end = new Vector3(0f, 0.09f, 0f);
            var turns = 5;
            var totalAngle = 360f * turns + (happy ? 0f : 180f);   // happy face is on top at rest
            for (var t = 0f; t < flipTime; t += Time.deltaTime)
            {
                var k = Mathf.Clamp01(t / flipTime);
                var ease = 1f - Mathf.Pow(1f - k, 2f);
                var pos = Vector3.Lerp(start, end, ease);
                pos.y = end.y + Mathf.Sin(k * Mathf.PI) * 3.6f * (1f - 0.3f * k) + (k > 0.8f ? Mathf.Abs(Mathf.Sin((k - 0.8f) * Mathf.PI * 4f)) * 0.12f : 0f);
                _coin.transform.position = pos;
                _coin.transform.rotation = Quaternion.AngleAxis(totalAngle * ease, Vector3.right) * Quaternion.AngleAxis(25f * ease, Vector3.up);
                yield return null;
            }
            _coin.transform.position = end;
            _coin.transform.rotation = Quaternion.AngleAxis(totalAngle, Vector3.right) * Quaternion.AngleAxis(25f, Vector3.up);
            yield return new WaitForSeconds(0.85f);
            _coinRoutine = null;
            onSettled?.Invoke();
            yield return new WaitForSeconds(0.9f);
            if (_coinRoutine == null) _coin.SetActive(false);
        }
    }
}
