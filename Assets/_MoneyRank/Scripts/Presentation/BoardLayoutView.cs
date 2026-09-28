using System;
using System.Collections.Generic;
using MoneyRank.Content;
using UnityEngine;

namespace MoneyRank.Presentation.Board
{
    public sealed class BoardLayoutView : MonoBehaviour
    {
        [SerializeField] private BoardDefinitionAsset definition;
        [SerializeField] private Vector2 boardSize = new Vector2(12f, 12f);
        [SerializeField] private Vector3 spaceScale = new Vector3(1.5f, 0.35f, 1.5f);
        [SerializeField] private Color startColor = new Color(0.1f, 0.75f, 0.4f);
        [SerializeField] private Color eventColor = new Color(0.15f, 0.4f, 0.85f);

        private readonly List<Transform> _spaceAnchors = new List<Transform>();
        private Transform _generatedRoot;

        public BoardDefinitionAsset Definition => definition;
        public IReadOnlyList<Transform> SpaceAnchors => _spaceAnchors;

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

            ClearGeneratedObjects();
            _generatedRoot = new GameObject("Generated Spaces").transform;
            _generatedRoot.SetParent(transform, false);
            var propertyBlock = new MaterialPropertyBlock();

            for (var index = 0; index < definition.Spaces.Count; index++)
            {
                var data = definition.Spaces[index];
                var space = GameObject.CreatePrimitive(PrimitiveType.Cube);
                space.name = $"Space {data.Index:00} - {data.TypeId}";
                space.transform.SetParent(_generatedRoot, false);
                space.transform.localPosition = GetPerimeterPosition(index, definition.Spaces.Count);
                space.transform.localScale = spaceScale;

                var renderer = space.GetComponent<Renderer>();
                var color = data.TypeId == "start" ? startColor : eventColor;
                propertyBlock.Clear();
                propertyBlock.SetColor("_BaseColor", color);
                propertyBlock.SetColor("_Color", color);
                renderer.SetPropertyBlock(propertyBlock);
                _spaceAnchors.Add(space.transform);
            }
        }

        private Vector3 GetPerimeterPosition(int index, int count)
        {
            var perimeter = 2f * (boardSize.x + boardSize.y);
            var distance = perimeter * index / count;
            var halfWidth = boardSize.x * 0.5f;
            var halfDepth = boardSize.y * 0.5f;

            if (distance < boardSize.x)
                return new Vector3(-halfWidth + distance, 0f, -halfDepth);

            distance -= boardSize.x;
            if (distance < boardSize.y)
                return new Vector3(halfWidth, 0f, -halfDepth + distance);

            distance -= boardSize.y;
            if (distance < boardSize.x)
                return new Vector3(halfWidth - distance, 0f, halfDepth);

            distance -= boardSize.x;
            return new Vector3(-halfWidth, 0f, halfDepth - distance);
        }

        private void ClearGeneratedObjects()
        {
            _spaceAnchors.Clear();
            var existingRoot = transform.Find("Generated Spaces");
            if (existingRoot == null) return;

            if (Application.isPlaying)
                Destroy(existingRoot.gameObject);
            else
                DestroyImmediate(existingRoot.gameObject);
        }
    }
}
