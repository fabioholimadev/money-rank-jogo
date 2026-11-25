using System;
using System.Collections.Generic;
using MoneyRank.Content;
using MoneyRank.Domain;
using UnityEngine;

namespace MoneyRank.Presentation.Board
{
    public sealed class BoardLayoutView : MonoBehaviour
    {
        private const int OuterSlotCount = 36;
        private const int EventSlotCount = 32;
        private const int InnerGridSize = 15;
        private const int LudoCenterSize = 3;
        private const int LudoCenterStart = (InnerGridSize - LudoCenterSize) / 2;
        private const int LudoCenterEnd = LudoCenterStart + LudoCenterSize - 1;
        private const int LudoCenterLine = InnerGridSize / 2;

        private static readonly Color Green = HexColor("009E3D");
        private static readonly Color Red = HexColor("F21820");
        private static readonly Color Yellow = HexColor("FFE000");
        private static readonly Color Blue = HexColor("08A9DF");
        private static readonly Color BoardNavy = HexColor("111111");
        private static readonly Color TileWhite = HexColor("FFFFFF");
        private static readonly Color SpaceGray = HexColor("B8B8B8");
        private static readonly Color[] CornerColors =
        {
            HexColor("A0A0A0"), HexColor("A0A0A0"), HexColor("A0A0A0"), HexColor("A0A0A0")
        };

        [SerializeField] private BoardDefinitionAsset definition;
        [SerializeField] private Vector2 boardSize = new Vector2(12f, 12f);
        [SerializeField] private Vector3 spaceScale = new Vector3(1.5f, 0.35f, 1.5f);
        [SerializeField] private Color startColor = new Color(0.1f, 0.75f, 0.4f);
        [SerializeField] private Color eventColor = new Color(0.15f, 0.4f, 0.85f);
        [SerializeField] private Vector2 innerBoardSize = new Vector2(6.6f, 6.6f);
        [SerializeField, Range(1, 4)] private int activePlayerCount = 4;

        private readonly List<Transform> _spaceAnchors = new List<Transform>();
        private readonly Transform[] _outerSlots = new Transform[OuterSlotCount];
        private readonly Dictionary<Color, Material> _colorMaterials = new Dictionary<Color, Material>();
        private readonly Dictionary<Texture2D, Material> _atlasMaterials = new Dictionary<Texture2D, Material>();
        private Transform _generatedRoot;
        private Mesh _homePawnMesh;

        public BoardDefinitionAsset Definition => definition;
        public IReadOnlyList<Transform> SpaceAnchors => _spaceAnchors;

        public void SetActivePlayerCount(int playerCount)
        {
            var clampedCount = Mathf.Clamp(playerCount, 1, 4);
            if (activePlayerCount == clampedCount && _generatedRoot != null)
                return;

            activePlayerCount = clampedCount;
            Rebuild();
        }

        private void Awake()
        {
            Rebuild();
        }

        public void Initialize(BoardDefinitionAsset boardDefinition)
        {
            definition = boardDefinition != null
                ? boardDefinition
                : throw new ArgumentNullException(nameof(boardDefinition));
            Rebuild();
        }

        [ContextMenu("Rebuild Board")]
        public void Rebuild()
        {
            if (definition == null)
                throw new InvalidOperationException("A BoardDefinitionAsset is required.");

            boardSize = Vector2.Max(boardSize, new Vector2(14.4f, 14.4f));
            innerBoardSize = Vector2.Max(innerBoardSize, new Vector2(8f, 8f));
            ClearGeneratedObjects();
            ClearGeneratedMaterials();
            _generatedRoot = new GameObject("Generated Board").transform;
            _generatedRoot.SetParent(transform, false);
            Array.Clear(_outerSlots, 0, _outerSlots.Length);

            CreateBoardBase();
            CreateOuterTrack();
            CreateInnerLudoBoard();
            BuildLogicalAnchors();
        }

        private void CreateBoardBase()
        {
            CreateBox(
                "Board Base",
                _generatedRoot,
                new Vector3(0f, -0.24f, 0f),
                new Vector3(boardSize.x + 0.7f, 0.48f, boardSize.y + 0.7f),
                BoardNavy);
        }

        private void CreateOuterTrack()
        {
            // Each side has eight regular spaces between its two larger corner spaces.
            var stepX = boardSize.x / 10f;
            var stepZ = boardSize.y / 10f;
            var eventOrdinal = 0;

            for (var slot = 0; slot < OuterSlotCount; slot++)
            {
                var position = GetOuterSlotPosition(slot, stepX, stepZ);
                if (IsCornerSlot(slot))
                {
                    CreateCorner(slot, position, stepX, stepZ);
                    continue;
                }

                var tileSize = new Vector3(stepX * 0.80f, 0.20f, stepZ * 0.80f);
                CreateBox(
                    $"Event Space Border {eventOrdinal + 1:00}",
                    _generatedRoot,
                    position + Vector3.up * 0.10f,
                    new Vector3(stepX * 0.90f, 0.20f, stepZ * 0.90f),
                    HexColor("4A4A4A"));
                CreateBox(
                    $"Event Space {eventOrdinal + 1:00}",
                    _generatedRoot,
                    position + Vector3.up * 0.22f,
                    tileSize,
                    SpaceGray);

                _outerSlots[slot] = CreateAnchor(
                    $"Anchor {slot:00}",
                    _generatedRoot,
                    position,
                    0.05f);
                eventOrdinal++;
            }
        }

        private void CreateCorner(int slot, Vector3 position, float stepX, float stepZ)
        {
            var names = new[] { "Start", "Hospital", "Bank Credit", "Public Treasury" };
            var cornerIndex = slot / 9;
            var outwardX = cornerIndex == 0 || cornerIndex == 3 ? -1f : 1f;
            var outwardZ = cornerIndex < 2 ? 1f : -1f;
            var cornerPosition = position + new Vector3(outwardX * stepX * 0.08f, 0f, outwardZ * stepZ * 0.08f);
            var size = new Vector3(stepX * 1.10f, 0.58f, stepZ * 1.10f);
            var top = 0.05f + size.y;

            CreateBox(
                $"{names[cornerIndex]} Corner",
                _generatedRoot,
                cornerPosition + Vector3.up * (0.05f + size.y * 0.5f),
                size,
                CornerColors[cornerIndex]);
            _outerSlots[slot] = CreateAnchor(
                $"Anchor {slot:00}",
                _generatedRoot,
                cornerPosition,
                top - 0.20f);
        }

        private void CreateCornerIcon(int cornerIndex, Vector3 origin)
        {
            switch (cornerIndex)
            {
                case 0:
                    CreateBox("Start Flag Pole", _generatedRoot, origin + new Vector3(-0.30f, 0.30f, 0f),
                        new Vector3(0.09f, 0.08f, 0.62f), TileWhite);
                    CreateBox("Start Flag", _generatedRoot, origin + new Vector3(-0.08f, 0.51f, 0f),
                        new Vector3(0.42f, 0.08f, 0.22f), Yellow);
                    break;
                case 1:
                    CreateBox("Hospital Cross Vertical", _generatedRoot, origin + Vector3.up * 0.08f,
                        new Vector3(0.22f, 0.08f, 0.82f), TileWhite);
                    CreateBox("Hospital Cross Horizontal", _generatedRoot, origin + Vector3.up * 0.08f,
                        new Vector3(0.82f, 0.08f, 0.22f), TileWhite);
                    break;
                case 2:
                    CreateBox("Vault Roof", _generatedRoot, origin + Vector3.up * 0.08f,
                        new Vector3(0.94f, 0.08f, 0.24f), Yellow);
                    for (var pillar = -1; pillar <= 1; pillar++)
                    {
                        CreateBox($"Vault Pillar {pillar + 2}", _generatedRoot,
                            origin + new Vector3(pillar * 0.30f, 0.08f, 0f),
                            new Vector3(0.13f, 0.08f, 0.54f), TileWhite);
                    }
                    break;
                default:
                    CreateBox("Virada Dice", _generatedRoot, origin + Vector3.up * 0.34f,
                        new Vector3(0.56f, 0.56f, 0.56f), TileWhite);
                    CreateDisc("Virada Dice Pip", _generatedRoot, origin + new Vector3(0f, 0.64f, 0f),
                        0.12f, 0.045f, BoardNavy);
                    break;
            }
        }

        private void CreateInnerLudoBoard()
        {
            var width = Mathf.Min(innerBoardSize.x, boardSize.x - 4.8f);
            var depth = Mathf.Min(innerBoardSize.y, boardSize.y - 4.8f);
            var cell = Mathf.Min(width, depth) / InnerGridSize;
            var gridWidth = cell * InnerGridSize;
            CreateBox(
                "Ludo Board Frame",
                _generatedRoot,
                new Vector3(0f, 0.15f, 0f),
                new Vector3(gridWidth + 0.62f, 0.28f, gridWidth + 0.62f),
                TileWhite);
            CreateBox(
                "Ludo Board Backing",
                _generatedRoot,
                new Vector3(0f, 0.30f, 0f),
                new Vector3(gridWidth + 0.43f, 0.12f, gridWidth + 0.43f),
                BoardNavy);

            CreateHomeZone("Red Home", -1, 1, cell, Red);
            CreateHomeZone("Green Home", 1, 1, cell, Green);
            CreateHomeZone("Blue Home", -1, -1, cell, Blue);
            CreateHomeZone("Yellow Home", 1, -1, cell, Yellow);

            for (var row = 0; row < InnerGridSize; row++)
            {
                for (var column = 0; column < InnerGridSize; column++)
                {
                    if (!IsLudoPathCell(row, column))
                        continue;
                    if (IsLudoCenterCell(row, column))
                        continue;

                    var center = GetGridCellPosition(row, column, cell);
                    var color = GetPathColor(row, column);
                    CreateBox(
                        $"Ludo Path Cell {row:00}-{column:00} Border",
                        _generatedRoot,
                        center + Vector3.up * 0.40f,
                        new Vector3(cell * 0.99f, 0.075f, cell * 0.99f),
                        BoardNavy);
                    CreateBox(
                        $"Ludo Path Cell {row:00}-{column:00}",
                        _generatedRoot,
                        center + Vector3.up * 0.445f,
                        new Vector3(cell * 0.96f, 0.055f, cell * 0.96f),
                        color);
                }
            }

            CreateLudoCenter(cell);
            CreateHomeTokenSpots("Red", -1, 1, cell, Red, activePlayerCount >= 1);
            CreateHomeTokenSpots("Green", 1, 1, cell, Green, activePlayerCount >= 2);
            CreateHomeTokenSpots("Blue", -1, -1, cell, Blue, activePlayerCount >= 3);
            CreateHomeTokenSpots("Yellow", 1, -1, cell, Yellow, activePlayerCount >= 4);

        }

        private void CreateHomeZone(string name, int sideX, int sideZ, float cell, Color color)
        {
            var homeCenter = LudoCenterLine - (LudoCenterStart - 1) * 0.5f;
            var center = new Vector3(sideX * cell * homeCenter, 0.34f, sideZ * cell * homeCenter);
            CreateBox(
                name,
                _generatedRoot,
                center,
                new Vector3(cell * (LudoCenterStart), 0.12f, cell * (LudoCenterStart)),
                color);
        }

        private void CreateHomeTokenSpots(
            string name,
            int sideX,
            int sideZ,
            float cell,
            Color color,
            bool includePlayerPieces)
        {
            var homeCenter = LudoCenterLine - (LudoCenterStart - 1) * 0.5f;
            var centerX = sideX * cell * homeCenter;
            var centerZ = sideZ * cell * homeCenter;
            CreateBox(
                $"{name} Home Token Tray Border",
                _generatedRoot,
                new Vector3(centerX, 0.405f, centerZ),
                new Vector3(cell * 4.28f, 0.10f, cell * 4.28f),
                BoardNavy);
            CreateBox(
                $"{name} Home Token Tray",
                _generatedRoot,
                new Vector3(centerX, 0.418f, centerZ),
                new Vector3(cell * 4.2f, 0.08f, cell * 4.2f),
                TileWhite);

            var offset = cell * 0.78f;
            var spots = new[]
            {
                new Vector3(-offset, 0f, -offset),
                new Vector3(offset, 0f, -offset),
                new Vector3(-offset, 0f, offset),
                new Vector3(offset, 0f, offset)
            };
            for (var index = 0; index < spots.Length; index++)
            {
                if (!includePlayerPieces)
                    continue;

                var center = new Vector3(centerX, 0.493f, centerZ) + spots[index];
                CreateDisc($"{name} Home Token {index + 1} Border", _generatedRoot, center, cell * 0.74f, 0.10f, BoardNavy);
                CreateDisc($"{name} Home Token {index + 1}", _generatedRoot, center, cell * 0.68f, 0.10f, color);
                CreateChessPawn($"{name} Home Pawn {index + 1}", center + Vector3.up * 0.05f, cell, color);
            }
        }

        private void CreateChessPawn(string name, Vector3 basePosition, float cell, Color color)
        {
            if (_homePawnMesh == null)
                _homePawnMesh = CreateChessPawnMesh();

            var pawn = new GameObject(name);
            pawn.transform.SetParent(_generatedRoot, false);
            pawn.transform.localPosition = basePosition;
            pawn.transform.localScale = new Vector3(cell * 0.58f, cell * 0.82f, cell * 0.58f);
            pawn.AddComponent<MeshFilter>().sharedMesh = _homePawnMesh;
            ApplyColor(pawn.AddComponent<MeshRenderer>(), color);
        }

        private static Mesh CreateChessPawnMesh()
        {
            // Lathed profile gives the token the familiar base, stem, collar and head of a chess pawn.
            var profile = new[]
            {
                new Vector2(0.025f, 0.30f), new Vector2(0.045f, 0.39f),
                new Vector2(0.10f, 0.43f), new Vector2(0.16f, 0.38f),
                new Vector2(0.20f, 0.25f), new Vector2(0.28f, 0.17f),
                new Vector2(0.43f, 0.16f), new Vector2(0.50f, 0.23f),
                new Vector2(0.56f, 0.35f), new Vector2(0.63f, 0.38f),
                new Vector2(0.70f, 0.31f), new Vector2(0.75f, 0.19f),
                new Vector2(0.81f, 0.16f), new Vector2(0.86f, 0.21f),
                new Vector2(0.92f, 0.22f), new Vector2(0.97f, 0.15f),
                new Vector2(1.00f, 0.00f)
            };
            const int segments = 24;
            var vertices = new Vector3[profile.Length * segments + segments + 1];
            var triangles = new int[(profile.Length - 1) * segments * 6 + segments * 3];

            for (var row = 0; row < profile.Length; row++)
            {
                for (var segment = 0; segment < segments; segment++)
                {
                    var angle = segment * Mathf.PI * 2f / segments;
                    var radius = profile[row].y;
                    vertices[row * segments + segment] = new Vector3(
                        Mathf.Cos(angle) * radius,
                        profile[row].x,
                        Mathf.Sin(angle) * radius);
                }
            }

            var triangleIndex = 0;
            for (var row = 0; row < profile.Length - 1; row++)
            {
                for (var segment = 0; segment < segments; segment++)
                {
                    var next = (segment + 1) % segments;
                    var lower = row * segments + segment;
                    var lowerNext = row * segments + next;
                    var upper = (row + 1) * segments + segment;
                    var upperNext = (row + 1) * segments + next;
                    triangles[triangleIndex++] = lower;
                    triangles[triangleIndex++] = upper;
                    triangles[triangleIndex++] = upperNext;
                    triangles[triangleIndex++] = lower;
                    triangles[triangleIndex++] = upperNext;
                    triangles[triangleIndex++] = lowerNext;
                }
            }

            var bottomCenter = profile.Length * segments;
            vertices[bottomCenter] = Vector3.zero;
            for (var segment = 0; segment < segments; segment++)
            {
                var next = (segment + 1) % segments;
                triangles[triangleIndex++] = bottomCenter;
                triangles[triangleIndex++] = segment;
                triangles[triangleIndex++] = next;
            }

            var mesh = new Mesh { name = "Chess Pawn Mesh" };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private void CreateLudoCenter(float cell)
        {
            var size = cell * 3f;
            CreateBox("Ludo Center Border", _generatedRoot, new Vector3(0f, 0.40f, 0f),
                new Vector3(size, 0.075f, size), BoardNavy);
            CreateBox("Ludo Center Surface", _generatedRoot, new Vector3(0f, 0.445f, 0f),
                new Vector3(size * 0.96f, 0.055f, size * 0.96f), TileWhite);

            var half = size * 0.48f;
            var centerY = 0.482f;
            CreateFlatTriangle("Ludo Center Green Wedge", _generatedRoot,
                new Vector3(-half, centerY, half), new Vector3(half, centerY, half),
                new Vector3(0f, centerY, 0f), Green);
            CreateFlatTriangle("Ludo Center Yellow Wedge", _generatedRoot,
                new Vector3(half, centerY, half), new Vector3(half, centerY, -half),
                new Vector3(0f, centerY, 0f), Yellow);
            CreateFlatTriangle("Ludo Center Blue Wedge", _generatedRoot,
                new Vector3(half, centerY, -half), new Vector3(-half, centerY, -half),
                new Vector3(0f, centerY, 0f), Blue);
            CreateFlatTriangle("Ludo Center Red Wedge", _generatedRoot,
                new Vector3(-half, centerY, -half), new Vector3(-half, centerY, half),
                new Vector3(0f, centerY, 0f), Red);

            var corners = new[]
            {
                new Vector2(-half, -half), new Vector2(-half, half),
                new Vector2(half, half), new Vector2(half, -half)
            };
            for (var index = 0; index < corners.Length; index++)
            {
                var corner = corners[index];
                var direction = new Vector3(corner.x, 0f, corner.y);
                var divider = CreateBox(
                    $"Ludo Center Divider {index + 1}",
                    _generatedRoot,
                    new Vector3(corner.x * 0.5f, centerY + 0.006f, corner.y * 0.5f),
                    new Vector3(cell * 0.035f, 0.012f, direction.magnitude),
                    BoardNavy);
                divider.transform.localRotation = Quaternion.Euler(
                    0f, Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg, 0f);
            }
        }

        private GameObject CreateFlatTriangle(string name, Transform parent, Vector3 first, Vector3 second,
            Vector3 tip, Color color)
        {
            var gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, false);
            var mesh = new Mesh { name = name + " Mesh" };
            mesh.vertices = new[] { first, second, tip };
            var normal = Vector3.Cross(second - first, tip - first);
            mesh.triangles = normal.y >= 0f ? new[] { 0, 1, 2 } : new[] { 0, 2, 1 };
            mesh.RecalculateNormals();
            gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            ApplyColor(gameObject.AddComponent<MeshRenderer>(), color);
            return gameObject;
        }

        private static bool IsLudoCenterCell(int row, int column) =>
            row >= LudoCenterStart && row <= LudoCenterEnd &&
            column >= LudoCenterStart && column <= LudoCenterEnd;

        private static Rect GetEventIconRect(int ordinal)
        {
            // The atlas's lower-left panel contains the first 21 event icons in a 7-column grid.
            var grid = new[,]
            {
                { 5, 0 }, { 2, 1 }, { 4, 2 },
                { 2, 0 }, { 0, 1 }, { 3, 1 },
                { 1, 2 }, { 0, 3 }, { 2, 3 },
                { 4, 0 }, { 3, 2 }, { 6, 2 },
                { 1, 0 }, { 5, 1 }, { 6, 1 },
                { 0, 0 }, { 4, 1 }, { 0, 2 },
                { 3, 0 }, { 1, 1 }, { 2, 2 }
            };
            if (ordinal < 21)
            {
                var column = grid[ordinal, 0];
                var row = grid[ordinal, 1];
                return PixelRect(235f + column * 107f, 966f + row * 128f, 92f, 90f, 1600f, 1600f);
            }

            // The opportunity icons are in the right-hand atlas panel: public program, extra work, partnership.
            switch (ordinal - 21)
            {
                case 0: return PixelRect(1415f, 652f, 96f, 84f, 1600f, 1600f);
                case 1: return PixelRect(1199f, 752f, 96f, 82f, 1600f, 1600f);
                default: return PixelRect(1307f, 752f, 96f, 82f, 1600f, 1600f);
            }
        }

        private static Rect GetCornerIconRect(int cornerIndex)
        {
            switch (cornerIndex)
            {
                case 0: return PixelRect(707f, 565f, 367f, 352f, 1600f, 1600f);
                case 1: return PixelRect(1118f, 565f, 367f, 352f, 1600f, 1600f);
                case 2: return PixelRect(707f, 936f, 367f, 352f, 1600f, 1600f);
                default: return PixelRect(1118f, 936f, 367f, 352f, 1600f, 1600f);
            }
        }

        private static Rect PixelRect(float x, float top, float width, float height, float textureWidth, float textureHeight)
        {
            // Keep bilinear sampling inside each atlas cell so neighboring artwork cannot bleed in.
            const float insetPixels = 1.5f;
            x += insetPixels;
            top += insetPixels;
            width -= insetPixels * 2f;
            height -= insetPixels * 2f;
            return new Rect(x / textureWidth, 1f - (top + height) / textureHeight,
                width / textureWidth, height / textureHeight);
        }

        private void CreateAtlasQuad(string name, Transform parent, Vector3 position, float width, float depth,
            Texture2D atlas, Rect uvRect, float yaw)
        {
            var gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, false);
            gameObject.transform.localPosition = position;
            gameObject.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            var mesh = new Mesh { name = name + " Mesh" };
            mesh.vertices = new[]
            {
                new Vector3(-width * 0.5f, 0f, -depth * 0.5f),
                new Vector3(-width * 0.5f, 0f, depth * 0.5f),
                new Vector3(width * 0.5f, 0f, depth * 0.5f),
                new Vector3(width * 0.5f, 0f, -depth * 0.5f)
            };
            mesh.uv = new[]
            {
                new Vector2(uvRect.xMin, uvRect.yMin), new Vector2(uvRect.xMin, uvRect.yMax),
                new Vector2(uvRect.xMax, uvRect.yMax), new Vector2(uvRect.xMax, uvRect.yMin)
            };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mesh.RecalculateNormals();
            gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = GetAtlasMaterial(atlas);
            DisableBoardSurfaceShadows(renderer);
        }

        private Material GetAtlasMaterial(Texture2D atlas)
        {
            if (_atlasMaterials.TryGetValue(atlas, out var material))
                return material;

            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Texture") ?? Shader.Find("Standard");
            if (shader == null)
                throw new InvalidOperationException("No compatible shader was found for the board icon atlas.");
            material = new Material(shader) { name = $"Money Rank Atlas - {atlas.name}" };
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", atlas);
            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", atlas);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
            if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);
            _atlasMaterials.Add(atlas, material);
            return material;
        }

        private static float GetSlotRotation(int slot)
        {
            if (slot < 7) return 0f;
            if (slot < 14) return 90f;
            if (slot < 21) return 180f;
            return 270f;
        }

        private static Color HexColor(string hex)
        {
            if (!ColorUtility.TryParseHtmlString("#" + hex, out var color))
                throw new ArgumentException($"Invalid board color #{hex}.", nameof(hex));
            return color;
        }

        private void CreateDecorativeDie()
        {
            var diePosition = new Vector3(0f, 0.43f, -boardSize.y * 0.5f + 2.05f);
            CreateBox("Game Die", _generatedRoot, diePosition, new Vector3(0.58f, 0.58f, 0.58f), TileWhite);
            var pipOffsets = new[]
            {
                new Vector3(-0.13f, 0.30f, -0.13f),
                new Vector3(0.13f, 0.30f, 0.13f),
                new Vector3(0f, 0.30f, 0f)
            };
            for (var index = 0; index < pipOffsets.Length; index++)
                CreateDisc($"Die Pip {index + 1}", _generatedRoot, diePosition + pipOffsets[index], 0.09f, 0.035f, BoardNavy);
        }

        private void BuildLogicalAnchors()
        {
            _spaceAnchors.Clear();
            var ordered = new Transform[definition.Spaces.Count];
            foreach (var data in definition.Spaces)
            {
                if (data.Index < 0 || data.Index >= ordered.Length)
                    throw new InvalidOperationException($"Board space index {data.Index} is outside the generated anchors.");

                var slot = GetOuterSlotForLogicalSpace(data.Index);
                var anchor = _outerSlots[slot];
                if (anchor == null)
                    throw new InvalidOperationException($"No visual anchor was generated for outer slot {slot}.");
                ordered[data.Index] = anchor;
            }

            _spaceAnchors.AddRange(ordered);
        }

        private int GetOuterSlotForLogicalSpace(int spaceIndex)
        {
            if (definition.Spaces.Count == OuterSlotCount)
                return spaceIndex;
            if (spaceIndex == definition.StartIndex)
                return 0;

            var eventCount = definition.Spaces.Count - 1;
            var eventOrdinal = spaceIndex < definition.StartIndex ? spaceIndex : spaceIndex - 1;
            var mappedEventOrdinal = eventCount <= 1
                ? 0
                : Mathf.RoundToInt((float)eventOrdinal * (EventSlotCount - 1) / (eventCount - 1));
            return GetRingSlotForEventOrdinal(mappedEventOrdinal);
        }

        private static int GetRingSlotForEventOrdinal(int eventOrdinal)
        {
            if (eventOrdinal < 8) return 1 + eventOrdinal;
            if (eventOrdinal < 16) return 10 + eventOrdinal - 8;
            if (eventOrdinal < 24) return 19 + eventOrdinal - 16;
            return 28 + eventOrdinal - 24;
        }

        private Vector3 GetOuterSlotPosition(int slot, float stepX, float stepZ)
        {
            var halfX = boardSize.x * 0.5f;
            var halfZ = boardSize.y * 0.5f;
            var cornerX = halfX - stepX * 0.5f;
            var cornerZ = halfZ - stepZ * 0.5f;

            if (slot == 0) return new Vector3(-cornerX, 0f, cornerZ);
            if (slot == 9) return new Vector3(cornerX, 0f, cornerZ);
            if (slot == 18) return new Vector3(cornerX, 0f, -cornerZ);
            if (slot == 27) return new Vector3(-cornerX, 0f, -cornerZ);
            if (slot < 9) return new Vector3(-halfX + stepX * (slot + 0.5f), 0f, halfZ - stepZ * 0.5f);
            if (slot < 18) return new Vector3(halfX - stepX * 0.5f, 0f, halfZ - stepZ * (slot - 9 + 0.5f));
            if (slot < 27) return new Vector3(halfX - stepX * (slot - 18 + 0.5f), 0f, -halfZ + stepZ * 0.5f);
            return new Vector3(-halfX + stepX * 0.5f, 0f, -halfZ + stepZ * (slot - 27 + 0.5f));
        }

        private static bool IsCornerSlot(int slot) => slot == 0 || slot == 9 || slot == 18 || slot == 27;

        private static bool IsLudoPathCell(int row, int column) =>
            (row >= LudoCenterStart && row <= LudoCenterEnd) ||
            (column >= LudoCenterStart && column <= LudoCenterEnd);

        private static Color GetPathColor(int row, int column)
        {
            // Each home stretch has five straight colored cells and one elbow cell,
            // matching the L-shaped turns in the reference board.
            if (row == LudoCenterLine && column > 0 && column < LudoCenterStart) return Red;
            if (row == LudoCenterLine - 1 && column == 1) return Red;

            if (column == LudoCenterLine && row > 0 && row < LudoCenterStart) return Green;
            if (row == 1 && column == LudoCenterLine + 1) return Green;

            if (row == LudoCenterLine && column > LudoCenterEnd && column < InnerGridSize - 1) return Yellow;
            if (row == LudoCenterLine + 1 && column == InnerGridSize - 2) return Yellow;

            if (column == LudoCenterLine && row > LudoCenterEnd && row < InnerGridSize - 1) return Blue;
            if (row == InnerGridSize - 2 && column == LudoCenterLine - 1) return Blue;

            return TileWhite;
        }

        private static Vector3 GetGridCellPosition(int row, int column, float cell)
        {
            var x = (column - (InnerGridSize - 1) * 0.5f) * cell;
            var z = ((InnerGridSize - 1) * 0.5f - row) * cell;
            return new Vector3(x, 0f, z);
        }

        private Transform CreateAnchor(string name, Transform parent, Vector3 position, float y)
        {
            var anchor = new GameObject(name).transform;
            anchor.SetParent(parent, false);
            anchor.localPosition = new Vector3(position.x, y, position.z);
            return anchor;
        }

        private GameObject CreateBox(string name, Transform parent, Vector3 position, Vector3 scale, Color color)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.localPosition = position;
            box.transform.localScale = scale;
            ApplyColor(box.GetComponent<Renderer>(), color);
            RemoveCollider(box);
            return box;
        }

        private GameObject CreateDisc(
            string name,
            Transform parent,
            Vector3 position,
            float diameter,
            float height,
            Color color)
        {
            var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = name;
            disc.transform.SetParent(parent, false);
            disc.transform.localPosition = position;
            disc.transform.localScale = new Vector3(diameter, height * 0.5f, diameter);
            ApplyColor(disc.GetComponent<Renderer>(), color);
            RemoveCollider(disc);
            return disc;
        }

        private void CreateLabel(string name, Transform parent, Vector3 position, string text, float characterSize, Color color)
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
                return;

            var labelObject = new GameObject(name);
            labelObject.transform.SetParent(parent, false);
            labelObject.transform.localPosition = position;
            labelObject.transform.localRotation =
                Quaternion.Euler(-90f, 0f, 0f) * Quaternion.Euler(0f, 0f, 180f);
            var label = labelObject.AddComponent<TextMesh>();
            label.font = font;
            label.text = text;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.characterSize = characterSize;
            label.fontSize = 48;
            label.color = color;
            labelObject.GetComponent<MeshRenderer>().sortingOrder = 2;
        }

        private void ApplyColor(Renderer renderer, Color color)
        {
            if (renderer == null) return;
            DisableBoardSurfaceShadows(renderer);
            if (!_colorMaterials.TryGetValue(color, out var material))
            {
                var shader = Shader.Find("Universal Render Pipeline/Unlit") ??
                             Shader.Find("Universal Render Pipeline/Lit") ??
                             Shader.Find("Unlit/Color") ??
                             Shader.Find("Standard");
                if (shader == null)
                    throw new InvalidOperationException("No compatible shader was found for the board materials.");

                material = new Material(shader)
                {
                    name = $"Money Rank Board - {ColorUtility.ToHtmlStringRGB(color)}"
                };
                if (material.HasProperty("_BaseColor"))
                    material.SetColor("_BaseColor", color);
                if (material.HasProperty("_Color"))
                    material.SetColor("_Color", color);
                _colorMaterials.Add(color, material);
            }

            renderer.sharedMaterial = material;
        }

        private static void DisableBoardSurfaceShadows(Renderer renderer)
        {
            // The generated board is made from many shallow, overlapping slabs. Avoid
            // shadow-map acne and speckled self-shadowing on those large flat surfaces.
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private void OnDestroy()
        {
            ClearGeneratedMaterials();
        }

        private void ClearGeneratedMaterials()
        {
            foreach (var material in _colorMaterials.Values)
            {
                if (material == null) continue;
                if (UnityEngine.Application.isPlaying)
                    Destroy(material);
                else
                    DestroyImmediate(material);
            }
            _colorMaterials.Clear();
            foreach (var material in _atlasMaterials.Values)
            {
                if (material == null) continue;
                if (UnityEngine.Application.isPlaying)
                    Destroy(material);
                else
                    DestroyImmediate(material);
            }
            _atlasMaterials.Clear();
            if (_homePawnMesh != null)
            {
                if (UnityEngine.Application.isPlaying)
                    Destroy(_homePawnMesh);
                else
                    DestroyImmediate(_homePawnMesh);
                _homePawnMesh = null;
            }
        }

        private static void RemoveCollider(GameObject gameObject)
        {
            var collider = gameObject.GetComponent<Collider>();
            if (collider == null) return;
            if (UnityEngine.Application.isPlaying)
                Destroy(collider);
            else
                DestroyImmediate(collider);
        }

        private void ClearGeneratedObjects()
        {
            _spaceAnchors.Clear();
            var generatedBoard = transform.Find("Generated Board");
            var legacyGeneratedSpaces = transform.Find("Generated Spaces");
            DestroyGeneratedObject(generatedBoard);
            DestroyGeneratedObject(legacyGeneratedSpaces);
        }

        private static void DestroyGeneratedObject(Transform generated)
        {
            if (generated == null) return;
            if (UnityEngine.Application.isPlaying)
                Destroy(generated.gameObject);
            else
                DestroyImmediate(generated.gameObject);
        }
    }
}
