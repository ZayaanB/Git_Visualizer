using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;
using GitVisualizer.Models;
using GitVisualizer.Services;

namespace GitVisualizer.Core
{
    [RequireComponent(typeof(NetworkObject))]
    public class GraphRenderer : NetworkBehaviour
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

        /// <summary>
        /// Loads repo data, computes graph, spawns locally (Host only), and syncs to clients.
        /// Call this to trigger graph load. Only the Host performs fetch and computation.
        /// In Solo mode (!IsSpawned), fetches and spawns locally.
        /// </summary>
        public async void SpawnGraphFromRepo(string owner, string repoName, string personalAccessToken = "")
        {
            if (IsServer || !IsSpawned)
            {
                var data = await new GitHubService().FetchRepoData(owner, repoName, personalAccessToken);
                if (data != null)
                {
                    if (IsServer)
                        SpawnGraphAndSyncToClients(data);
                    else
                        SpawnGraph(data);
                }
            }
        }

        /// <summary>
        /// Direct spawn from RepoDataResult. Host syncs to clients; Solo spawns locally.
        /// </summary>
        public void SpawnGraph(GitHubService.RepoDataResult data)
        {
            if (data?.Branches == null)
            {
                Debug.LogError("[GraphRenderer] RepoDataResult or Branches is null.");
                return;
            }
            if (IsServer)
            {
                SpawnGraphAndSyncToClients(data);
            }
            else if (!IsSpawned)
            {
                var commitsByBranch = data.CommitsByBranch ?? new Dictionary<string, Commit[]>();
                var payload = BuildVisualizationPayload(data.Branches.ToList(), commitsByBranch);
                SpawnGraphLocal(payload);
            }
        }

        /// <summary>
        /// Legacy overload. Host computes and syncs; Solo spawns locally.
        /// </summary>
        public void SpawnGraph(List<Branch> branches, IReadOnlyDictionary<string, Commit[]> commitsByBranch)
        {
            if (branches == null || branches.Count == 0)
            {
                Debug.LogWarning("[GraphRenderer] No branches to render.");
                return;
            }
            if (IsServer)
            {
                var payload = BuildVisualizationPayload(branches, commitsByBranch);
                SpawnGraphLocal(payload);
                SyncGraphToClientsClientRpc(payload);
            }
            else if (!IsSpawned)
            {
                var payload = BuildVisualizationPayload(branches, commitsByBranch);
                SpawnGraphLocal(payload);
            }
        }

        private void SpawnGraphAndSyncToClients(GitHubService.RepoDataResult data)
        {
            var commitsByBranch = data.CommitsByBranch ?? new Dictionary<string, Commit[]>();
            var branches = data.Branches.ToList();
            if (branches == null || branches.Count == 0)
            {
                Debug.LogWarning("[GraphRenderer] No branches to render.");
                return;
            }
            var payload = BuildVisualizationPayload(branches, commitsByBranch);
            SpawnGraphLocal(payload);
            SyncGraphToClientsClientRpc(payload);
        }

        private GraphVisualizationPayload BuildVisualizationPayload(List<Branch> branches, IReadOnlyDictionary<string, Commit[]> commitsByBranch)
        {
            var payload = new GraphVisualizationPayload { Branches = new List<BranchVisualizationData>() };

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

                var branchData = new BranchVisualizationData
                {
                    BranchName = Truncate64(branchName),
                    ColorRgb = new Vector3(branchColor.r, branchColor.g, branchColor.b),
                    Nodes = new List<GraphNodeData>()
                };

                for (int j = 0; j < commitsList.Count; j++)
                {
                    var commit = commitsList[j];
                    var pos = new Vector3(xOffset, 0f, j * _commitSpacing);
                    branchData.Nodes.Add(new GraphNodeData
                    {
                        Position = pos,
                        Sha = Truncate64(commit.sha ?? ""),
                        Message = Truncate512(commit.commit?.message?.Trim() ?? ""),
                        AuthorName = Truncate128(commit.commit?.author?.name ?? commit.author?.login ?? ""),
                        Date = Truncate64(commit.commit?.committer?.date ?? commit.commit?.author?.date ?? ""),
                        IndexInBranch = j
                    });
                }

                branchData.NodeCount = branchData.Nodes.Count;
                payload.Branches.Add(branchData);
            }

            payload.BranchCount = payload.Branches.Count;
            return payload;
        }

        private static FixedString64Bytes Truncate64(string s, int maxChars = 60)
        {
            if (string.IsNullOrEmpty(s)) return default;
            return new FixedString64Bytes(s.Length <= maxChars ? s : s.Substring(0, maxChars));
        }
        private static FixedString128Bytes Truncate128(string s, int maxChars = 120)
        {
            if (string.IsNullOrEmpty(s)) return default;
            return new FixedString128Bytes(s.Length <= maxChars ? s : s.Substring(0, maxChars));
        }
        private static FixedString512Bytes Truncate512(string s, int maxChars = 500)
        {
            if (string.IsNullOrEmpty(s)) return default;
            return new FixedString512Bytes(s.Length <= maxChars ? s : s.Substring(0, maxChars));
        }

        [ClientRpc]
        private void SyncGraphToClientsClientRpc(GraphVisualizationPayload payload)
        {
            if (IsServer) return;
            SpawnGraphLocal(payload);
        }

        private void SpawnGraphLocal(GraphVisualizationPayload payload)
        {
            if (payload.Branches == null || payload.BranchCount == 0) return;

            EnsureGraphContainer();

            for (int b = 0; b < payload.BranchCount && b < payload.Branches.Count; b++)
            {
                var branchData = payload.Branches[b];
                if (branchData.Nodes == null || branchData.NodeCount == 0) continue;

                var branchName = branchData.BranchName.ToString();
                var color = new Color(branchData.ColorRgb.x, branchData.ColorRgb.y, branchData.ColorRgb.z);
                var xOffset = b * _branchSpacing;

                SpawnBranchNodesFromPayload(branchData, xOffset, branchName);
                SpawnBranchLinesFromPayload(branchData, xOffset, color, branchName);
            }
        }

        private void SpawnBranchNodesFromPayload(BranchVisualizationData branchData, float xOffset, string branchName)
        {
            var nodesToAnimate = new List<Transform>();

            for (int i = 0; i < branchData.NodeCount && i < branchData.Nodes.Count; i++)
            {
                var nodeData = branchData.Nodes[i];
                var position = nodeData.Position;
                var node = CreateCommitNodeFromPayload(nodeData, position, branchName);
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

        private Transform CreateCommitNodeFromPayload(GraphNodeData nodeData, Vector3 position, string branchName)
        {
            GameObject nodeObj = _commitNodePrefab != null
                ? Instantiate(_commitNodePrefab, position, Quaternion.identity)
                : CreatePrimitiveNode(position);

            var shortSha = nodeData.Sha.ToString().Length > 7 ? nodeData.Sha.ToString().Substring(0, 7) : nodeData.Sha.ToString();
            nodeObj.name = $"Commit_{shortSha}_{branchName}";
            nodeObj.transform.localScale = _useSpawnAnimation ? Vector3.zero : Vector3.one * _nodeScale;

            var commit = CreateMinimalCommit(nodeData);
            var interactable = nodeObj.GetComponent<NodeInteractable>() ?? nodeObj.AddComponent<NodeInteractable>();
            interactable.SetCommit(commit);
            interactable.SetBranchInfo(branchName, nodeData.IndexInBranch);

            if (nodeObj.GetComponent<NodeClickEffects>() == null)
                nodeObj.AddComponent<NodeClickEffects>();

            return nodeObj.transform;
        }

        private static Commit CreateMinimalCommit(GraphNodeData nodeData)
        {
            return new Commit
            {
                sha = nodeData.Sha.ToString(),
                commit = new CommitInfo
                {
                    message = nodeData.Message.ToString(),
                    author = new GitAuthor { name = nodeData.AuthorName.ToString(), date = nodeData.Date.ToString() },
                    committer = new GitAuthor { name = nodeData.AuthorName.ToString(), date = nodeData.Date.ToString() }
                }
            };
        }

        private void SpawnBranchLinesFromPayload(BranchVisualizationData branchData, float xOffset, Color branchColor, string branchName)
        {
            if (branchData.NodeCount < 2) return;

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

            var positions = new Vector3[branchData.NodeCount];
            for (int i = 0; i < branchData.NodeCount; i++)
                positions[i] = branchData.Nodes[i].Position;

            lineRenderer.positionCount = positions.Length;
            lineRenderer.SetPositions(positions);
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
