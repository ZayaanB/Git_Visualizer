using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GitVisualizer.Models;
using GitVisualizer.Services;

namespace GitVisualizer.Core
{
    /// <summary>
    /// Renders commit data as a 3D graph: nodes for commits, LineRenderer for branch connections.
    /// Layout: Z-axis = chronological, X-axis = branch separation.
    /// </summary>
    [RequireComponent(typeof(Transform))]
    public class GraphRenderer : MonoBehaviour
    {
        [Header("Prefabs & References")]
        [SerializeField]
        private GameObject _commitNodePrefab;

        [Header("Layout Settings")]
        [SerializeField]
        private float _branchSpacing = 3f;

        [SerializeField]
        private float _commitSpacing = 1.5f;

        [SerializeField]
        private float _nodeScale = 0.3f;

        [Header("Line Renderer")]
        [SerializeField]
        private float _lineWidth = 0.05f;

        [SerializeField]
        private Material _lineMaterial;

        private const string GraphContainerName = "GraphContainer";
        private const string NodesContainerName = "Nodes";
        private const string LinesContainerName = "Lines";

        private Transform _graphContainer;
        private Transform _nodesContainer;
        private Transform _linesContainer;

        /// <summary>
        /// Spawns the commit graph from repository data.
        /// Clears any existing graph before spawning.
        /// </summary>
        /// <param name="data">Repository data from GitHubService.FetchRepoData.</param>
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

        /// <summary>
        /// Spawns the commit graph from branches and commits.
        /// </summary>
        /// <param name="branches">List of branches.</param>
        /// <param name="commitsByBranch">Commits per branch (branch name -> commits).</param>
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

                var branchColor = GetBranchColor(branchName, i, branches.Count);
                var xOffset = i * _branchSpacing;

                SpawnBranchNodes(commitsList, xOffset, branchName);
                SpawnBranchLines(commitsList, xOffset, branchColor, branchName);
            }
        }

        private void EnsureGraphContainer()
        {
            var existing = transform.Find(GraphContainerName);
            if (existing != null)
            {
                DestroyImmediate(existing.gameObject);
            }

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
            if (string.IsNullOrEmpty(dateStr))
                return DateTime.MinValue;

            return DateTime.TryParse(dateStr, out var dt) ? dt : DateTime.MinValue;
        }

        private void SpawnBranchNodes(List<Commit> commits, float xOffset, string branchName)
        {
            for (int i = 0; i < commits.Count; i++)
            {
                var commit = commits[i];
                var position = new Vector3(xOffset, 0f, i * _commitSpacing);

                var node = CreateCommitNode(commit, position, branchName);
                if (node != null)
                    node.SetParent(_nodesContainer);
            }
        }

        private Transform CreateCommitNode(Commit commit, Vector3 position, string branchName)
        {
            GameObject nodeObj;

            if (_commitNodePrefab != null)
            {
                nodeObj = Instantiate(_commitNodePrefab, position, Quaternion.identity);
            }
            else
            {
                nodeObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                nodeObj.transform.position = position;
            }

            nodeObj.name = $"Commit_{commit.sha?.Substring(0, Math.Min(7, commit.sha?.Length ?? 0)) ?? "unknown"}_{branchName}";
            nodeObj.transform.localScale = Vector3.one * _nodeScale;

            var interactable = nodeObj.GetComponent<NodeInteractable>();
            if (interactable == null)
                interactable = nodeObj.AddComponent<NodeInteractable>();
            interactable.SetCommit(commit);

            return nodeObj.transform;
        }

        private void SpawnBranchLines(List<Commit> commits, float xOffset, Color branchColor, string branchName)
        {
            if (commits.Count < 2)
                return;

            var lineObj = new GameObject($"Line_{branchName}");
            lineObj.transform.SetParent(_linesContainer, false);

            var lineRenderer = lineObj.AddComponent<LineRenderer>();
            ConfigureLineRenderer(lineRenderer, branchColor);

            var positions = new Vector3[commits.Count];
            for (int i = 0; i < commits.Count; i++)
            {
                positions[i] = new Vector3(xOffset, 0f, i * _commitSpacing);
            }

            lineRenderer.positionCount = positions.Length;
            lineRenderer.SetPositions(positions);
        }

        private void ConfigureLineRenderer(LineRenderer lineRenderer, Color color)
        {
            lineRenderer.startWidth = _lineWidth;
            lineRenderer.endWidth = _lineWidth * 0.5f;
            lineRenderer.useWorldSpace = true;
            lineRenderer.loop = false;
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;

            if (_lineMaterial != null)
            {
                lineRenderer.material = _lineMaterial;
            }
            else
            {
                lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
                lineRenderer.material.color = color;
            }
        }

        private Color GetBranchColor(string branchName, int index, int totalBranches)
        {
            var hash = branchName.GetHashCode();
            var hue = (Math.Abs(hash) % 360) / 360f;
            var saturation = 0.8f;
            var value = 0.9f;
            return Color.HSVToRGB(hue, saturation, value);
        }
    }
}
