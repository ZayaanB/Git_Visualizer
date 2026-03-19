using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GitVisualizer.Models;
using GitVisualizer.Services;

namespace GitVisualizer.Core
{
    [RequireComponent(typeof(Transform))]
    public class GraphRenderer : MonoBehaviour
    {
        [Header("Prefabs & References")]
        [SerializeField] private GameObject _commitNodePrefab;

        [Header("Layout")]
        [SerializeField] private float _branchSpacing = 3f;
        [SerializeField] private float _commitSpacing = 1.5f;
        [SerializeField] private float _nodeScale = 0.3f;

        [Header("Lines")]
        [SerializeField] private float _lineWidth = 0.05f;
        [SerializeField] private Material _lineMaterial;

        [Header("VFX")]
        [SerializeField] private bool _useSpawnAnimation = true;

        private const string GraphContainerName = "GraphContainer";
        private const string NodesContainerName = "Nodes";
        private const string LinesContainerName = "Lines";

        private Transform _graphContainer;
        private Transform _nodesContainer;
        private Transform _linesContainer;

        public void SpawnGraph(GitHubService.RepoDataResult data)
        {
            if (data?.Branches == null)
            {
                Debug.LogError("[GraphRenderer] RepoDataResult or Branches is null.");
                return;
            }
            var commitsByBranch = data.CommitsByBranch ?? new Dictionary<string, Commit[]>();
            SpawnGraph(data.Branches.ToList(), commitsByBranch);
        }

        public void SpawnGraph(List<Branch> branches, IReadOnlyDictionary<string, Commit[]> commitsByBranch)
        {
            if (branches == null || branches.Count == 0)
            {
                Debug.LogWarning("[GraphRenderer] No branches to render.");
                return;
            }

            EnsureGraphContainer();

            for (int i = 0; i < branches.Count; i++)
            {
                var branch = branches[i];
                var branchName = branch?.name ?? $"branch_{i}";
                if (!commitsByBranch.TryGetValue(branchName, out var commits) || commits == null || commits.Length == 0)
                    continue;

                var commitsList = commits.ToList();
                SortCommitsByDate(commitsList);

                var branchColor = GetBranchColor(branchName);
                var xOffset = i * _branchSpacing;

                SpawnBranchNodes(commitsList, xOffset, branchName);
                SpawnBranchLines(commitsList, xOffset, branchColor, branchName);
            }
        }

        private void EnsureGraphContainer()
        {
            var existing = transform.Find(GraphContainerName);
            if (existing != null)
                DestroyImmediate(existing.gameObject);

            _graphContainer = new GameObject(GraphContainerName).transform;
            _graphContainer.SetParent(transform, false);
            _graphContainer.localPosition = Vector3.zero;

            _nodesContainer = new GameObject(NodesContainerName).transform;
            _nodesContainer.SetParent(_graphContainer, false);

            _linesContainer = new GameObject(LinesContainerName).transform;
            _linesContainer.SetParent(_graphContainer, false);
        }

        private void SortCommitsByDate(List<Commit> commits)
        {
            commits.Sort((a, b) =>
            {
                var dateA = ParseCommitDate(a);
                var dateB = ParseCommitDate(b);
                return dateA.CompareTo(dateB);
            });
        }

        private static DateTime ParseCommitDate(Commit commit)
        {
            var dateStr = commit?.commit?.committer?.date ?? commit?.commit?.author?.date;
            if (string.IsNullOrEmpty(dateStr)) return DateTime.MinValue;
            return DateTime.TryParse(dateStr, out var dt) ? dt : DateTime.MinValue;
        }

        private void SpawnBranchNodes(List<Commit> commits, float xOffset, string branchName)
        {
            var nodesToAnimate = new List<Transform>();

            for (int i = 0; i < commits.Count; i++)
            {
                var commit = commits[i];
                var position = new Vector3(xOffset, 0f, i * _commitSpacing);
                var node = CreateCommitNode(commit, position, branchName, i);
                if (node != null)
                {
                    node.SetParent(_nodesContainer);
                    if (_useSpawnAnimation)
                        nodesToAnimate.Add(node);
                }
            }

            if (_useSpawnAnimation && nodesToAnimate.Count > 0 && VFXManager.Instance != null)
                VFXManager.Instance.PlaySpawnAnimationStaggered(nodesToAnimate.ToArray(), Vector3.one * _nodeScale);
        }

        private Transform CreateCommitNode(Commit commit, Vector3 position, string branchName, int indexInBranch)
        {
            GameObject nodeObj = _commitNodePrefab != null
                ? Instantiate(_commitNodePrefab, position, Quaternion.identity)
                : CreatePrimitiveNode(position);

            var shortSha = commit.sha?.Length > 7 ? commit.sha.Substring(0, 7) : commit.sha ?? "unknown";
            nodeObj.name = $"Commit_{shortSha}_{branchName}";
            nodeObj.transform.localScale = _useSpawnAnimation ? Vector3.zero : Vector3.one * _nodeScale;

            var interactable = nodeObj.GetComponent<NodeInteractable>() ?? nodeObj.AddComponent<NodeInteractable>();
            interactable.SetCommit(commit);
            interactable.SetBranchInfo(branchName, indexInBranch);

            if (nodeObj.GetComponent<NodeClickEffects>() == null)
                nodeObj.AddComponent<NodeClickEffects>();

            return nodeObj.transform;
        }

        private static GameObject CreatePrimitiveNode(Vector3 position)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            obj.transform.position = position;
            return obj;
        }

        private void SpawnBranchLines(List<Commit> commits, float xOffset, Color branchColor, string branchName)
        {
            if (commits.Count < 2) return;

            var lineObj = new GameObject($"Line_{branchName}");
            lineObj.transform.SetParent(_linesContainer, false);

            var lineRenderer = lineObj.AddComponent<LineRenderer>();
            lineRenderer.startWidth = _lineWidth;
            lineRenderer.endWidth = _lineWidth * 0.5f;
            lineRenderer.useWorldSpace = true;
            lineRenderer.loop = false;
            lineRenderer.startColor = lineRenderer.endColor = branchColor;
            lineRenderer.material = _lineMaterial != null
                ? _lineMaterial
                : new Material(Shader.Find("Sprites/Default")) { color = branchColor };

            var positions = new Vector3[commits.Count];
            for (int i = 0; i < commits.Count; i++)
                positions[i] = new Vector3(xOffset, 0f, i * _commitSpacing);

            lineRenderer.positionCount = positions.Length;
            lineRenderer.SetPositions(positions);
        }

        private static Color GetBranchColor(string branchName)
        {
            var hue = (Math.Abs(branchName.GetHashCode()) % 360) / 360f;
            return Color.HSVToRGB(hue, 0.8f, 0.9f);
        }
    }
}
