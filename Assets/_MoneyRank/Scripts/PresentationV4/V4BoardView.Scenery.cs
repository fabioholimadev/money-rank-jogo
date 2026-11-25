using UnityEngine;

namespace MoneyRank.V4.Presentation
{
    /// <summary>
    /// Gold-and-black scenery inspired by the supplied reference video (board on an organised desk with notebook, books,
    /// plant, calculator, coins) and by the supplied corner sketch (big corner squares with 3D elements).
    /// Only the look is borrowed: all rules and content come from the V4 data. Everything here is original primitive geometry.
    /// </summary>
    public sealed partial class V4BoardView
    {
        private static readonly Color Gold = new Color(0.86f, 0.66f, 0.2f);
        private static readonly Color GoldDark = new Color(0.55f, 0.40f, 0.10f);
        private static readonly Color Black = new Color(0.035f, 0.035f, 0.04f);
        private Transform _flag;
        private float _flagBaseYaw;

        private Material LitTex(Texture tex, float smooth, Vector2? tiling = null)
        {
            var m = new Material(_lit) { mainTexture = tex };
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smooth);
            if (tiling.HasValue) m.mainTextureScale = tiling.Value;
            return m;
        }

        private void SceneryUpdate()
        {
            if (_flag != null) _flag.localRotation = Quaternion.Euler(0f, _flagBaseYaw + Mathf.Sin(Time.time * 2.4f) * 13f, 0f);
        }

        // ------------------------------------------------------------------ textures

        private static Texture2D WoodTexture()
        {
            const int size = 256;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat };
            var px = new Color32[size * size];
            var dark = new Color(0.07f, 0.045f, 0.03f);
            var light = new Color(0.20f, 0.13f, 0.08f);
            for (var y = 0; y < size; y++)
                for (var x = 0; x < size; x++)
                {
                    var grain = Mathf.PerlinNoise(x * 0.012f, y * 0.22f);
                    var streak = 0.5f + 0.5f * Mathf.Sin(x * 0.11f + grain * 6f);
                    var t = Mathf.Clamp01(grain * 0.65f + streak * 0.35f);
                    px[y * size + x] = Color.Lerp(dark, light, t);
                }
            tex.SetPixels32(px); tex.Apply(true);
            return tex;
        }

        private static Texture2D WallTexture()
        {
            const int w = 256, h = 128;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[w * h];
            for (var y = 0; y < h; y++)
                for (var x = 0; x < w; x++)
                {
                    var d = Vector2.Distance(new Vector2(x / (float)w, y / (float)h), new Vector2(0.5f, 0.35f));
                    var glow = Mathf.Clamp01(1f - d * 1.8f);
                    px[y * w + x] = Color.Lerp(new Color(0.015f, 0.015f, 0.02f), new Color(0.20f, 0.14f, 0.05f), glow * glow);
                }
            tex.SetPixels32(px); tex.Apply();
            return tex;
        }

        private static Texture2D CheckerTexture()
        {
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Point };
            var px = new Color32[size * size];
            for (var y = 0; y < size; y++) for (var x = 0; x < size; x++) px[y * size + x] = ((x / 8) + (y / 8)) % 2 == 0 ? new Color32(250, 250, 245, 255) : new Color32(14, 14, 16, 255);
            tex.SetPixels32(px); tex.Apply();
            return tex;
        }

        private static Texture2D ScreenTexture()
        {
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            for (var y = 0; y < size; y++) for (var x = 0; x < size; x++) px[y * size + x] = Color.Lerp(new Color(0.10f, 0.07f, 0.02f), new Color(0.50f, 0.36f, 0.10f), y / (float)size);
            tex.SetPixels32(px); tex.Apply();
            return tex;
        }

        // ------------------------------------------------------------------ environment

        private void BuildEnvironment()
        {
            var cam = Camera.main;
            cam.backgroundColor = new Color(0.02f, 0.02f, 0.025f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.40f, 0.36f, 0.30f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.03f, 0.027f, 0.025f);
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 34f;
            RenderSettings.fogEndDistance = 80f;

            var keyGo = GameObject.Find("Directional Light");
            var key = keyGo != null ? keyGo.GetComponent<Light>() : null;
            if (key != null)
            {
                key.type = LightType.Directional;
                key.transform.rotation = Quaternion.Euler(52f, -34f, 0f);
                key.color = new Color(1f, 0.93f, 0.80f);
                key.intensity = 1.2f;
                key.shadows = LightShadows.Soft;
            }
            var glowGo = new GameObject("Gold Glow");
            glowGo.transform.SetParent(transform, false);
            glowGo.transform.position = new Vector3(0f, 10f, 1f);
            var glow = glowGo.AddComponent<Light>();
            glow.type = LightType.Point; glow.range = 26f; glow.intensity = 14f; glow.color = new Color(1f, 0.78f, 0.38f);

            var env = new GameObject("Environment").transform;
            env.SetParent(transform, false);

            // dark walnut desk with gold inlay
            Prim(PrimitiveType.Cube, "Desk", env, new Vector3(0f, -0.66f, 6f), new Vector3(70f, 0.6f, 46f), LitTex(WoodTexture(), 0.35f, new Vector2(7f, 4f)));
            var inlay = Lit(Gold, 0.9f);
            const float x0 = 17.5f, z0 = -11f, z1 = 20f;
            Prim(PrimitiveType.Cube, "InlayN", env, new Vector3(0f, -0.345f, z1), new Vector3(x0 * 2f, 0.02f, 0.14f), inlay);
            Prim(PrimitiveType.Cube, "InlayS", env, new Vector3(0f, -0.345f, z0), new Vector3(x0 * 2f, 0.02f, 0.14f), inlay);
            Prim(PrimitiveType.Cube, "InlayW", env, new Vector3(-x0, -0.345f, (z0 + z1) / 2f), new Vector3(0.14f, 0.02f, z1 - z0), inlay);
            Prim(PrimitiveType.Cube, "InlayE", env, new Vector3(x0, -0.345f, (z0 + z1) / 2f), new Vector3(0.14f, 0.02f, z1 - z0), inlay);

            // back wall with art-deco gold bars
            var wall = Prim(PrimitiveType.Quad, "Wall", env, new Vector3(0f, 9f, 29f), new Vector3(90f, 30f, 1f), Tex(WallTexture()));
            wall.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            var rng = new System.Random(2026);
            for (var i = -14; i <= 14; i++)
            {
                var h = 5f + (float)rng.NextDouble() * 9f;
                Prim(PrimitiveType.Cube, "Bar" + i, env, new Vector3(i * 3.1f, h / 2f - 0.3f, 28.6f), new Vector3(0.3f, h, 0.3f), Lit(Color.Lerp(GoldDark, Gold, (float)rng.NextDouble()), 0.8f));
            }
        }

        // ------------------------------------------------------------------ props

        private void BuildProps()
        {
            var props = new GameObject("Props").transform;
            props.SetParent(transform, false);
            BuildCornerProps(props);

            // organised desk behind/around the board (original low-poly stand-ins)
            PropPlant(props, new Vector3(-11.5f, -0.36f, 11.5f), 1.7f);
            PropNotebook(props, new Vector3(-7.4f, -0.36f, 12.2f), 14f);
            PropStickies(props, new Vector3(-9.6f, -0.36f, 8.6f));
            PropMug(props, new Vector3(-3.8f, -0.36f, 10.4f));
            PropLaptop(props, new Vector3(0.4f, -0.36f, 12.6f));
            PropCalculator(props, new Vector3(4.2f, -0.36f, 10.4f), -14f);
            PropCoins(props, new Vector3(6.6f, -0.36f, 9.6f));
            PropBills(props, new Vector3(10.4f, -0.36f, 9.2f));
            PropBooks(props, new Vector3(9.6f, -0.36f, 12.8f));
            PropBooks(props, new Vector3(-13.6f, -0.36f, 6.2f));
        }

        private void PropPlant(Transform parent, Vector3 pos, float s)
        {
            var g = new GameObject("Plant").transform; g.SetParent(parent, false); g.localPosition = pos; g.localScale = Vector3.one * s;
            Prim(PrimitiveType.Cylinder, "Pot", g, new Vector3(0f, 0.38f, 0f), new Vector3(0.9f, 0.38f, 0.9f), Lit(Black, 0.6f));
            Prim(PrimitiveType.Cylinder, "Rim", g, new Vector3(0f, 0.75f, 0f), new Vector3(0.98f, 0.04f, 0.98f), Lit(Gold, 0.9f));
            Prim(PrimitiveType.Cylinder, "Soil", g, new Vector3(0f, 0.77f, 0f), new Vector3(0.82f, 0.02f, 0.82f), Lit(new Color(0.08f, 0.05f, 0.03f), 0.1f));
            var leaf = Lit(new Color(0.10f, 0.42f, 0.20f), 0.35f); var leaf2 = Lit(new Color(0.06f, 0.30f, 0.14f), 0.35f);
            for (var i = 0; i < 9; i++)
            {
                var a = i * 40f;
                var l = Prim(PrimitiveType.Sphere, "Leaf" + i, g, new Vector3(Mathf.Sin(a * Mathf.Deg2Rad) * 0.25f, 1.35f + (i % 3) * 0.14f, Mathf.Cos(a * Mathf.Deg2Rad) * 0.25f), new Vector3(0.20f, 1.05f, 0.5f), i % 2 == 0 ? leaf : leaf2);
                l.transform.localRotation = Quaternion.Euler(Mathf.Lerp(15f, 38f, (i % 3) / 2f), a, 0f);
            }
        }

        private void PropNotebook(Transform parent, Vector3 pos, float yaw)
        {
            var g = new GameObject("Notebook").transform; g.SetParent(parent, false); g.localPosition = pos; g.localRotation = Quaternion.Euler(0f, yaw, 0f);
            Prim(PrimitiveType.Cube, "Cover", g, new Vector3(0f, 0.09f, 0f), new Vector3(2.5f, 0.16f, 3.2f), Lit(Black, 0.55f));
            Prim(PrimitiveType.Cube, "Pages", g, new Vector3(0.04f, 0.19f, 0f), new Vector3(2.3f, 0.06f, 3.0f), Lit(new Color(0.93f, 0.9f, 0.82f), 0.2f));
            for (var i = 0; i < 8; i++)
                Prim(PrimitiveType.Cylinder, "Ring" + i, g, new Vector3(-1.2f, 0.2f, -1.25f + i * 0.36f), new Vector3(0.09f, 0.13f, 0.09f), Lit(Gold, 0.9f)).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Prim(PrimitiveType.Cube, "Band", g, new Vector3(0.9f, 0.19f, 0f), new Vector3(0.08f, 0.2f, 3.25f), Lit(Gold, 0.85f));
            var pen = Prim(PrimitiveType.Cylinder, "Pen", g, new Vector3(0.3f, 0.28f, -0.2f), new Vector3(0.07f, 0.9f, 0.07f), Lit(Gold, 0.95f));
            pen.transform.localRotation = Quaternion.Euler(90f, 28f, 0f);
        }

        private void PropStickies(Transform parent, Vector3 pos)
        {
            var g = new GameObject("Stickies").transform; g.SetParent(parent, false); g.localPosition = pos;
            var cols = new[] { Gold, new Color(0.95f, 0.93f, 0.85f), new Color(0.12f, 0.12f, 0.14f) };
            for (var i = 0; i < 3; i++)
                Prim(PrimitiveType.Cube, "Note" + i, g, new Vector3(i * 0.55f, 0.02f + i * 0.035f, i * 0.35f), new Vector3(1.0f, 0.03f, 1.0f), Lit(cols[i], 0.2f)).transform.localRotation = Quaternion.Euler(0f, 18f - i * 22f, 0f);
        }

        private void PropMug(Transform parent, Vector3 pos)
        {
            var g = new GameObject("Mug").transform; g.SetParent(parent, false); g.localPosition = pos;
            Prim(PrimitiveType.Cylinder, "Cup", g, new Vector3(0f, 0.3f, 0f), new Vector3(0.62f, 0.3f, 0.62f), Lit(new Color(0.95f, 0.92f, 0.84f), 0.7f));
            Prim(PrimitiveType.Cylinder, "Coffee", g, new Vector3(0f, 0.585f, 0f), new Vector3(0.52f, 0.01f, 0.52f), Lit(new Color(0.07f, 0.04f, 0.02f), 0.8f));
            Prim(PrimitiveType.Cube, "Handle", g, new Vector3(0.36f, 0.3f, 0f), new Vector3(0.14f, 0.28f, 0.1f), Lit(new Color(0.95f, 0.92f, 0.84f), 0.7f));
            Prim(PrimitiveType.Cylinder, "Saucer", g, new Vector3(0f, 0.015f, 0f), new Vector3(0.95f, 0.015f, 0.95f), Lit(Gold, 0.9f));
        }

        private void PropLaptop(Transform parent, Vector3 pos)
        {
            var g = new GameObject("Laptop").transform; g.SetParent(parent, false); g.localPosition = pos;
            Prim(PrimitiveType.Cube, "Base", g, new Vector3(0f, 0.08f, 0f), new Vector3(3.4f, 0.14f, 2.3f), Lit(new Color(0.07f, 0.07f, 0.08f), 0.75f));
            Prim(PrimitiveType.Cube, "Keys", g, new Vector3(0f, 0.16f, -0.25f), new Vector3(2.9f, 0.02f, 1.2f), Lit(new Color(0.16f, 0.16f, 0.18f), 0.3f));
            Prim(PrimitiveType.Cube, "Touchpad", g, new Vector3(0f, 0.16f, -0.95f), new Vector3(1.1f, 0.02f, 0.5f), Lit(new Color(0.22f, 0.21f, 0.2f), 0.5f));
            var hinge = new GameObject("Screen").transform; hinge.SetParent(g, false); hinge.localPosition = new Vector3(0f, 0.12f, 1.1f); hinge.localRotation = Quaternion.Euler(14f, 0f, 0f);
            Prim(PrimitiveType.Cube, "Lid", hinge, new Vector3(0f, 1.05f, 0f), new Vector3(3.4f, 2.1f, 0.1f), Lit(new Color(0.07f, 0.07f, 0.08f), 0.75f));
            var face = Prim(PrimitiveType.Quad, "Display", hinge, new Vector3(0f, 1.05f, -0.056f), new Vector3(3.15f, 1.85f, 1f), Tex(ScreenTexture()));
            var tm = AddLabel(hinge, "MONEY RANK", new Vector3(0f, 1.1f, -0.07f), 0.045f, new Color(1f, 0.9f, 0.55f), TextAnchor.MiddleCenter, 0f);
            tm.transform.localRotation = Quaternion.identity;
        }

        private void PropCalculator(Transform parent, Vector3 pos, float yaw)
        {
            var g = new GameObject("Calculator").transform; g.SetParent(parent, false); g.localPosition = pos; g.localRotation = Quaternion.Euler(0f, yaw, 0f);
            Prim(PrimitiveType.Cube, "Body", g, new Vector3(0f, 0.09f, 0f), new Vector3(1.15f, 0.16f, 1.75f), Lit(Black, 0.6f));
            Prim(PrimitiveType.Cube, "Screen", g, new Vector3(0f, 0.18f, 0.6f), new Vector3(0.95f, 0.02f, 0.4f), Lit(new Color(0.2f, 0.5f, 0.3f), 0.2f));
            for (var r = 0; r < 4; r++) for (var c = 0; c < 3; c++)
                    Prim(PrimitiveType.Cube, "Key", g, new Vector3(-0.32f + c * 0.32f, 0.19f, 0.15f - r * 0.3f), new Vector3(0.24f, 0.04f, 0.2f), Lit(c == 2 ? Gold : new Color(0.3f, 0.3f, 0.32f), 0.5f));
        }

        private void PropCoins(Transform parent, Vector3 pos)
        {
            var g = new GameObject("Coins").transform; g.SetParent(parent, false); g.localPosition = pos;
            var gold = Lit(Gold, 0.95f);
            var heights = new[] { 9, 6, 4 };
            for (var s = 0; s < heights.Length; s++)
                for (var i = 0; i < heights[s]; i++)
                    Prim(PrimitiveType.Cylinder, "Coin", g, new Vector3(s * 0.75f, 0.04f + i * 0.075f, (s % 2) * 0.35f), new Vector3(0.62f, 0.036f, 0.62f), gold);
        }

        private void PropBills(Transform parent, Vector3 pos)
        {
            var g = new GameObject("Bills").transform; g.SetParent(parent, false); g.localPosition = pos;
            var green = Lit(new Color(0.20f, 0.52f, 0.30f), 0.3f); var band = Lit(Gold, 0.8f);
            for (var s = 0; s < 3; s++)
            {
                var st = new GameObject("Stack").transform; st.SetParent(g, false); st.localPosition = new Vector3(s * 0.55f, 0f, s * 0.25f); st.localRotation = Quaternion.Euler(0f, 8f - s * 9f, 0f);
                Prim(PrimitiveType.Cube, "Notes", st, new Vector3(0f, 0.19f + s * 0.06f, 0f), new Vector3(1.5f, 0.35f + s * 0.12f, 0.75f), green);
                Prim(PrimitiveType.Cube, "Band", st, new Vector3(0f, 0.19f + s * 0.06f, 0f), new Vector3(0.22f, 0.37f + s * 0.12f, 0.78f), band);
            }
        }

        private void PropBooks(Transform parent, Vector3 pos)
        {
            var g = new GameObject("Books").transform; g.SetParent(parent, false); g.localPosition = pos;
            var cols = new[] { Black, Gold, new Color(0.10f, 0.30f, 0.20f), new Color(0.38f, 0.07f, 0.10f), new Color(0.08f, 0.1f, 0.2f) };
            var y = 0f;
            for (var i = 0; i < cols.Length; i++)
            {
                var h = 0.28f + (i % 2) * 0.08f;
                var b = Prim(PrimitiveType.Cube, "Book" + i, g, new Vector3((i % 2) * 0.09f, y + h / 2f, 0f), new Vector3(2.3f - i * 0.1f, h, 1.7f), Lit(cols[i], 0.4f));
                b.transform.localRotation = Quaternion.Euler(0f, (i % 3 - 1) * 9f, 0f);
                Prim(PrimitiveType.Cube, "Pages", b.transform.parent, new Vector3((i % 2) * 0.09f + 0.02f, y + h / 2f, -0.0f), new Vector3(2.15f - i * 0.1f, h * 0.7f, 1.72f), Lit(new Color(0.93f, 0.9f, 0.82f), 0.2f)).transform.localRotation = Quaternion.Euler(0f, (i % 3 - 1) * 9f, 0f);
                y += h;
            }
        }

        // ------------------------------------------------------------------ corner props (the big squares of the board)

        private void BuildCornerProps(Transform props)
        {
            // H01 start: checkered flag on a gold base
            var c0 = RingCell(0);
            var flag = new GameObject("StartFlag").transform; flag.SetParent(props, false); flag.localPosition = c0 + new Vector3(0.1f, 0.02f, 0.32f);
            Prim(PrimitiveType.Cylinder, "Base", flag, new Vector3(0f, 0.06f, 0f), new Vector3(1.0f, 0.06f, 1.0f), Lit(Gold, 0.9f));
            Prim(PrimitiveType.Cylinder, "Pole", flag, new Vector3(0f, 1.3f, 0f), new Vector3(0.07f, 1.25f, 0.07f), Lit(Gold, 0.95f));
            Prim(PrimitiveType.Sphere, "Finial", flag, new Vector3(0f, 2.58f, 0f), Vector3.one * 0.16f, Lit(Gold, 0.95f));
            var wave = new GameObject("Wave").transform; wave.SetParent(flag, false); wave.localPosition = new Vector3(0f, 2.25f, 0f);
            Prim(PrimitiveType.Quad, "ClothA", wave, new Vector3(0.62f, 0f, 0f), new Vector3(1.2f, 0.8f, 1f), Tex(CheckerTexture()));
            var back = Prim(PrimitiveType.Quad, "ClothB", wave, new Vector3(0.62f, 0f, 0.001f), new Vector3(1.2f, 0.8f, 1f), Tex(CheckerTexture()));
            back.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            back.transform.localPosition = new Vector3(0.62f, 0f, 0.003f);
            _flag = wave; _flagBaseYaw = 0f;
            Caption(props, "INÍCIO", c0, 1);

            // H10 hospital building
            var c9 = RingCell(9);
            var hosp = new GameObject("Hospital").transform; hosp.SetParent(props, false); hosp.localPosition = c9 + new Vector3(0.05f, 0.02f, 0.3f); hosp.localRotation = Quaternion.Euler(0f, 200f, 0f); hosp.localScale = Vector3.one * 0.92f;
            Prim(PrimitiveType.Cube, "Body", hosp, new Vector3(0f, 0.65f, 0f), new Vector3(1.9f, 1.3f, 1.5f), Lit(new Color(0.94f, 0.95f, 0.97f), 0.35f));
            Prim(PrimitiveType.Cube, "Wing", hosp, new Vector3(1.1f, 0.42f, 0f), new Vector3(0.7f, 0.84f, 1.1f), Lit(new Color(0.86f, 0.88f, 0.92f), 0.35f));
            Prim(PrimitiveType.Cube, "Roof", hosp, new Vector3(0f, 1.34f, 0f), new Vector3(2.0f, 0.08f, 1.6f), Lit(Gold, 0.9f));
            Prim(PrimitiveType.Cube, "CrossV", hosp, new Vector3(0f, 0.85f, 0.77f), new Vector3(0.2f, 0.62f, 0.03f), Lit(new Color(0.9f, 0.1f, 0.12f)));
            Prim(PrimitiveType.Cube, "CrossH", hosp, new Vector3(0f, 0.85f, 0.77f), new Vector3(0.62f, 0.2f, 0.03f), Lit(new Color(0.9f, 0.1f, 0.12f)));
            Prim(PrimitiveType.Cube, "Door", hosp, new Vector3(0f, 0.24f, 0.77f), new Vector3(0.42f, 0.48f, 0.03f), Lit(new Color(0.25f, 0.4f, 0.5f)));
            Caption(props, "HOSPITAL", c9, 1);

            // H19 direct to hospital: ambulance + arrow pointing to H10
            var c18 = RingCell(18);
            var amb = new GameObject("Ambulance").transform; amb.SetParent(props, false); amb.localPosition = c18 + new Vector3(0.35f, 0.02f, -0.5f); amb.localRotation = Quaternion.Euler(0f, -12f, 0f); amb.localScale = Vector3.one * 0.78f;
            Prim(PrimitiveType.Cube, "Box", amb, new Vector3(0f, 0.62f, -0.2f), new Vector3(0.95f, 0.85f, 1.5f), Lit(new Color(0.96f, 0.96f, 0.97f), 0.5f));
            Prim(PrimitiveType.Cube, "Cabin", amb, new Vector3(0f, 0.45f, 0.78f), new Vector3(0.95f, 0.55f, 0.6f), Lit(new Color(0.96f, 0.96f, 0.97f), 0.5f));
            Prim(PrimitiveType.Cube, "Glass", amb, new Vector3(0f, 0.55f, 1.09f), new Vector3(0.8f, 0.3f, 0.02f), Lit(new Color(0.15f, 0.25f, 0.3f), 0.9f));
            Prim(PrimitiveType.Cube, "Stripe", amb, new Vector3(0f, 0.5f, -0.2f), new Vector3(0.97f, 0.12f, 1.52f), Lit(Gold, 0.8f));
            Prim(PrimitiveType.Cube, "CrossV", amb, new Vector3(0.485f, 0.78f, -0.2f), new Vector3(0.02f, 0.4f, 0.12f), Lit(new Color(0.9f, 0.1f, 0.12f)));
            Prim(PrimitiveType.Cube, "CrossH", amb, new Vector3(0.485f, 0.78f, -0.2f), new Vector3(0.02f, 0.12f, 0.4f), Lit(new Color(0.9f, 0.1f, 0.12f)));
            Prim(PrimitiveType.Cube, "SirenR", amb, new Vector3(-0.2f, 1.08f, -0.2f), new Vector3(0.28f, 0.14f, 0.2f), Lit(new Color(0.9f, 0.1f, 0.12f), 0.8f));
            Prim(PrimitiveType.Cube, "SirenB", amb, new Vector3(0.2f, 1.08f, -0.2f), new Vector3(0.28f, 0.14f, 0.2f), Lit(new Color(0.15f, 0.4f, 0.95f), 0.8f));
            foreach (var wz in new[] { -0.7f, 0.75f })
                foreach (var wx in new[] { -0.5f, 0.5f })
                    Prim(PrimitiveType.Cylinder, "Wheel", amb, new Vector3(wx, 0.2f, wz), new Vector3(0.38f, 0.07f, 0.38f), Lit(Black, 0.3f)).transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            var arrow = new GameObject("Arrow").transform; arrow.SetParent(props, false); arrow.localPosition = c18 + new Vector3(-0.8f, 0.03f, -0.55f);
            Prim(PrimitiveType.Cube, "Shaft", arrow, new Vector3(0f, 0.02f, -0.2f), new Vector3(0.22f, 0.04f, 0.7f), Lit(Gold, 0.8f));
            var head = Prim(PrimitiveType.Cube, "Head", arrow, new Vector3(0f, 0.02f, 0.27f), new Vector3(0.5f, 0.04f, 0.5f), Lit(Gold, 0.8f));
            head.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
            Caption(props, "DIRETO AO\nHOSPITAL", c18, 2);

            // H28 public coffer: vault with a live display
            var c27 = RingCell(27);
            var coffer = new GameObject("Cofre").transform; coffer.SetParent(props, false); coffer.localPosition = c27 + new Vector3(-0.05f, 0.02f, -0.32f); coffer.localRotation = Quaternion.Euler(0f, 160f, 0f); coffer.localScale = Vector3.one * 0.95f;
            Prim(PrimitiveType.Cube, "Body", coffer, new Vector3(0f, 0.55f, 0f), new Vector3(1.3f, 1.1f, 1.1f), Lit(new Color(0.16f, 0.17f, 0.2f), 0.75f));
            Prim(PrimitiveType.Cube, "Frame", coffer, new Vector3(0f, 0.55f, 0.555f), new Vector3(1.18f, 0.98f, 0.02f), Lit(Gold, 0.9f));
            Prim(PrimitiveType.Cube, "Door", coffer, new Vector3(0f, 0.55f, 0.565f), new Vector3(1.08f, 0.88f, 0.03f), Lit(new Color(0.24f, 0.25f, 0.29f), 0.85f));
            var dial = Prim(PrimitiveType.Cylinder, "Dial", coffer, new Vector3(0.25f, 0.38f, 0.6f), new Vector3(0.3f, 0.03f, 0.3f), Lit(Gold, 0.95f));
            dial.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Prim(PrimitiveType.Cube, "Handle", coffer, new Vector3(-0.25f, 0.38f, 0.61f), new Vector3(0.32f, 0.07f, 0.05f), Lit(Gold, 0.95f));
            Prim(PrimitiveType.Cube, "Screen", coffer, new Vector3(-0.1f, 0.8f, 0.595f), new Vector3(0.7f, 0.22f, 0.02f), Lit(new Color(0.02f, 0.12f, 0.08f), 0.2f));
            _cofferText = AddLabel(coffer, "COFRE 0 UMR", new Vector3(-0.1f, 0.8f, 0.61f), 0.018f, new Color(0.35f, 1f, 0.55f), TextAnchor.MiddleCenter, 0f);
            _cofferText.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            Caption(props, "COFRE PÚBLICO", c27, 1);
        }

        /// <summary>Gold caption on a black plaque lying on the inner edge of a corner square.</summary>
        private void Caption(Transform parent, string text, Vector3 corner, int lines)
        {
            var toCenterZ = -Mathf.Sign(corner.z);
            var depth = lines > 1 ? 0.66f : 0.4f;
            var pos = corner + new Vector3(0f, 0.03f, toCenterZ * (CornerSize / 2f - depth / 2f - 0.06f));
            var plaque = Prim(PrimitiveType.Cube, "Plaque", parent, pos, new Vector3(CornerSize - 0.3f, 0.02f, depth), Lit(new Color(0.03f, 0.03f, 0.035f), 0.8f));
            Prim(PrimitiveType.Cube, "PlaqueGold", parent, pos + new Vector3(0f, -0.004f, 0f), new Vector3(CornerSize - 0.22f, 0.02f, depth + 0.08f), Lit(Gold, 0.9f));
            plaque.transform.position += new Vector3(0f, 0.004f, 0f);
            var tm = AddLabel(parent, text, pos + new Vector3(0f, 0.025f, 0f), lines > 1 ? 0.052f : 0.058f, new Color(1f, 0.88f, 0.5f), TextAnchor.MiddleCenter, 90f);
            tm.fontStyle = FontStyle.Bold;
        }
    }
}
