using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using Unity.Netcode;
using GitVisualizer.Models;
using GitVisualizer.Services;

namespace GitVisualizer.Core
{
    [RequireComponent(typeof(NetworkObject))]
    public class GraphRenderer : NetworkBehaviour
    {
        private const int MaxLineVertices = 128;

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

        public static event Action<int> OnGraphReady;

        private Transform _graphContainer;
        private Transform _nodesContainer;
        private Transform _linesContainer;
        private CommitNodePool _nodePool;
        private Material _lineMaterialInstance;

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

        public void SpawnGraph(GitHubService.RepoDataResult data)
        {
            if (data?.Branches == null)
            {
                Debug.LogError("[GraphRenderer] RepoDataResult or Branches is null.");
                return;
            }
            if (IsServer)
                SpawnGraphAndSyncToClients(data);
            else if (!IsSpawned)
            {
                var commitsByBranch = data.CommitsByBranch ?? new Dictionary<string, Commit[]>();
                var payload = BuildVisualizationPayload(data.Branches.ToList(), commitsByBranch);
                SpawnGraphLocal(payload);
            }
        }

        public void SpawnGraph(List<Branch> branches, IReadOnlyDictionary<string, Commit[]> commitsByBranch)
        {
            if (branches == null || branches.Count == 0)
            {
                Debug.LogWarning("[GraphRenderer] No branches to render.");
                return;
            }
            var payload = BuildVisualizationPayload(branches, commitsByBranch);
            if (IsServer)
            {
                SpawnGraphLocal(payload);
                SyncGraphToClientsClientRpc(payload);
            }
            else if (!IsSpawned)
                SpawnGraphLocal(payload);
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
            _nodePool ??= new CommitNodePool(_commitNodePrefab, _nodesContainer, 64);
            _nodePool.ReturnAll();

            int globalIndex = 0;

            for (int b = 0; b < payload.BranchCount && b < payload.Branches.Count; b++)
            {
                var branchData = payload.Branches[b];
                if (branchData.Nodes == null || branchData.NodeCount == 0) continue;

                var branchName = branchData.BranchName.ToString();
                var color = new Color(branchData.ColorRgb.x, branchData.ColorRgb.y, branchData.ColorRgb.z);
                var xOffset = b * _branchSpacing;

                SpawnBranchNodesFromPayload(branchData, xOffset, branchName, ref globalIndex);
                SpawnBranchLinesFromPayload(branchData, xOffset, color, branchName);
            }

            OnGraphReady?.Invoke(globalIndex);
        }

        private void SpawnBranchNodesFromPayload(BranchVisualizationData branchData, float xOffset, string branchName, ref int globalIndex)
        {
            var nodesToAnimate = new List<Transform>();

            for (int i = 0; i < branchData.NodeCount && i < branchData.Nodes.Count; i++)
            {
                var nodeData = branchData.Nodes[i];
                var position = nodeData.Position;
                var node = CreateCommitNodeFromPayload(nodeData, position, branchName, globalIndex);
                if (node != null)
                {
                    node.SetParent(_nodesContainer);
                    if (_useSpawnAnimation)
                        nodesToAnimate.Add(node);
                }
                globalIndex++;
            }

            if (_useSpawnAnimation && nodesToAnimate.Count > 0 && VFXManager.Instance != null)
                VFXManager.Instance.PlaySpawnAnimationStaggered(nodesToAnimate.ToArray(), Vector3.one * _nodeScale);
        }

        private Transform CreateCommitNodeFromPayload(GraphNodeData nodeData, Vector3 position, string branchName, int globalIndex)
        {
            var nodeObj = _nodePool.Get(position, Quaternion.identity);

            var shortSha = nodeData.Sha.ToString().Length > 7 ? nodeData.Sha.ToString().Substring(0, 7) : nodeData.Sha.ToString();
            nodeObj.name = $"Commit_{shortSha}_{branchName}";
            nodeObj.transform.localScale = _useSpawnAnimation ? Vector3.zero : Vector3.one * _nodeScale;

            var commit = CreateMinimalCommit(nodeData);
            var interactable = nodeObj.GetComponent<NodeInteractable>() ?? nodeObj.AddComponent<NodeInteractable>();
            interactable.SetCommit(commit);
            interactable.SetBranchInfo(branchName, nodeData.IndexInBranch);
            interactable.SetGlobalIndex(globalIndex);

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

            _lineMaterialInstance ??= _lineMaterial != null
                ? _lineMaterial
                : new Material(Shader.Find("Sprites/Default"));
            lineRenderer.sharedMaterial = _lineMaterialInstance;
            lineRenderer.startColor = lineRenderer.endColor = branchColor;

            int vertexCount = Mathf.Min(branchData.NodeCount, MaxLineVertices);
            var positions = new Vector3[vertexCount];

            if (vertexCount == branchData.NodeCount)
            {
                for (int i = 0; i < vertexCount; i++)
                    positions[i] = branchData.Nodes[i].Position;
            }
            else
            {
                for (int i = 0; i < vertexCount; i++)
                {
                    float t = (float)i / (vertexCount - 1);
                    int idx = Mathf.RoundToInt(t * (branchData.NodeCount - 1));
                    positions[i] = branchData.Nodes[idx].Position;
                }
            }

            lineRenderer.positionCount = vertexCount;
            lineRenderer.SetPositions(positions);
            if (vertexCount > 16)
                lineRenderer.Simplify(0.01f);
        }

        public void ClearGraph()
        {
            var existing = transform.Find(GraphContainerName);
            if (existing != null)
            {
                _nodePool?.DestroyAll();
                _nodePool = null;
                Object.Destroy(existing.gameObject);
            }
        }

        private void EnsureGraphContainer()
        {
            var existing = transform.Find(GraphContainerName);
            if (existing != null)
            {
                _nodePool?.DestroyAll();
                _nodePool = null;
                Object.DestroyImmediate(existing.gameObject);
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
            if (string.IsNullOrEmpty(dateStr)) return DateTime.MinValue;
            return DateTime.TryParse(dateStr, out var dt) ? dt : DateTime.MinValue;
        }

        private static Color GetBranchColor(string branchName)
        {
            var hue = (Math.Abs(branchName.GetHashCode()) % 360) / 360f;
            return Color.HSVToRGB(hue, 0.8f, 0.9f);
        }

        private void OnDestroy()
        {
            _nodePool?.DestroyAll();
            if (_lineMaterialInstance != null && _lineMaterial == null)
                Object.Destroy(_lineMaterialInstance);
        }
    }
}
