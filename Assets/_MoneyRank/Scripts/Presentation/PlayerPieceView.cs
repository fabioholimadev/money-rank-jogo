using System.Collections;
using System.Collections.Generic;
using System;
using MoneyRank.Domain;
using UnityEngine;

namespace MoneyRank.Presentation.Board
{
    public sealed class PlayerPieceView : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] private float secondsPerSpace = 0.28f;
        [SerializeField, Min(0f)] private float verticalOffset = 0.22f;
        [SerializeField, Min(0f)] private float jumpHeight = 0.38f;
        [SerializeField] private string playerModelResourcePath = "Models/Capybara/capybara";
        [SerializeField, Min(0.1f)] private float playerModelScale = 0.8f;

        private Vector3 _boardOffset;
        private GameObject _playerModel;

        public bool IsAnimating { get; private set; }

        public void SnapTo(int index, IReadOnlyList<Transform> anchors)
        {
            transform.position = GetTargetPosition(index, anchors);
            FaceAlongPath(index, anchors);
        }

        public void SetPlayerColor(Color color)
        {
            if (_playerModel == null)
            {
                var modelPrefab = Resources.Load<GameObject>(playerModelResourcePath);
                if (modelPrefab == null)
                    throw new InvalidOperationException(
                        $"Player model was not found at Resources/{playerModelResourcePath}.");

                _playerModel = Instantiate(modelPrefab, transform, false);
                _playerModel.name = "Capybara Player Model";
                _playerModel.transform.localPosition = Vector3.zero;
                _playerModel.transform.localRotation = Quaternion.identity;
                _playerModel.transform.localScale = Vector3.one * playerModelScale;

                var existingRenderer = GetComponent<Renderer>();
                if (existingRenderer != null)
                    existingRenderer.enabled = false;
                var existingCollider = GetComponent<Collider>();
                if (existingCollider != null)
                    existingCollider.enabled = false;
            }

            var bodyTinted = false;
            foreach (var renderer in _playerModel.GetComponentsInChildren<Renderer>())
            {
                // The high-poly imported mesh was self-shadowing into tiny speckles.
                // Board surfaces do not receive shadows either, so keep these clean.
                renderer.receiveShadows = false;
                var materials = renderer.materials;
                for (var index = 0; index < materials.Length; index++)
                {
                    SetMatteFinish(materials[index]);
                    if (!materials[index].name.Contains("capybara_body") &&
                        !(materials[index].name.Contains("(Instance)") && !bodyTinted))
                        continue;

                    SetMaterialColor(materials[index], color);
                    bodyTinted = true;
                }
            }
        }

        public void SetBoardOffset(Vector3 boardOffset)
        {
            _boardOffset = boardOffset;
        }

        public IEnumerator Animate(BoardMoveResult result, IReadOnlyList<Transform> anchors)
        {
            IsAnimating = true;
            var index = result.Origin;

            for (var step = 0; step < result.Steps; step++)
            {
                index = (index + 1) % anchors.Count;
                FaceAlongPath(index, anchors);
                var origin = transform.position;
                var target = GetTargetPosition(index, anchors);
                var elapsed = 0f;

                while (elapsed < secondsPerSpace)
                {
                    elapsed += Time.deltaTime;
                    var progress = Mathf.Clamp01(elapsed / secondsPerSpace);
                    var hop = Mathf.Sin(progress * Mathf.PI) * jumpHeight;
                    transform.position = Vector3.Lerp(origin, target, progress) + Vector3.up * hop;
                    yield return null;
                }

                transform.position = target;
            }

            IsAnimating = false;
        }

        private Vector3 GetTargetPosition(int index, IReadOnlyList<Transform> anchors)
        {
            return anchors[index].position + Vector3.up * verticalOffset + _boardOffset;
        }

        private void FaceAlongPath(int index, IReadOnlyList<Transform> anchors)
        {
            if (anchors == null || anchors.Count < 2)
                return;

            var nextIndex = (index + 1) % anchors.Count;
            var direction = anchors[nextIndex].position - anchors[index].position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }

        private static void SetMaterialColor(Material material, Color color)
        {
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);
        }

        private static void SetMatteFinish(Material material)
        {
            if (material == null) return;
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.12f);
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0.12f);
            if (material.HasProperty("_SpecularHighlights")) material.SetFloat("_SpecularHighlights", 0f);
            if (material.HasProperty("_EnvironmentReflections")) material.SetFloat("_EnvironmentReflections", 0f);
        }
    }
}
