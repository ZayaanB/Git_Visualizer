using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace GitVisualizer.Core
{
    [RequireComponent(typeof(NetworkObject))]
    public class AvatarController : NetworkBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float _moveSpeed = 4f;
        [SerializeField] private float _rotationSpeed = 360f;
        [SerializeField] private float _heightOffset = 0.3f;

        [Header("References")]
        [SerializeField] private Transform _graphContainer;
        [SerializeField] private string _linesContainerName = "Lines";

        private string _currentBranchName = "";
        private int _currentIndexInBranch = -1;
        private Coroutine _moveCoroutine;

        /// <summary>
        /// The local player's avatar (for Solo: any avatar; for Co-op: the one we own).
        /// </summary>
        public static AvatarController LocalInstance { get; private set; }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsOwner)
            {
                LocalInstance = this;
                SetCameraToFollowThis();
            }
        }

        private void Start()
        {
            if (!IsSpawned)
            {
                LocalInstance = this;
                SetCameraToFollowThis();
            }
        }

        private void SetCameraToFollowThis()
        {
            var cam = FindObjectOfType<OrbitCamera>();
            if (cam != null)
                cam.SetTarget(transform);
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            if (LocalInstance == this)
                LocalInstance = null;
        }

        private void EnsureGraphContainer()
        {
            if (_graphContainer != null) return;
            var graph = FindObjectOfType<GraphRenderer>();
            if (graph != null)
                _graphContainer = graph.transform.Find("GraphContainer");
        }

        private bool IsLocalPlayer => !IsSpawned || IsOwner;

        public void NavigateTo(Transform targetNode, string branchName, int indexInBranch)
        {
            if (!IsLocalPlayer) return;
            if (targetNode == null) return;
            if (_moveCoroutine != null) StopCoroutine(_moveCoroutine);

            var path = GetPathToNode(branchName, indexInBranch);
            _moveCoroutine = StartCoroutine(MoveAlongPathCoroutine(path, targetNode.position));
            _currentBranchName = branchName;
            _currentIndexInBranch = indexInBranch;
        }

        public void NavigateToPosition(Vector3 worldPosition)
        {
            if (!IsLocalPlayer) return;
            if (_moveCoroutine != null) StopCoroutine(_moveCoroutine);
            _moveCoroutine = StartCoroutine(MoveToPositionCoroutine(worldPosition));
        }

        private Vector3[] GetPathToNode(string branchName, int indexInBranch)
        {
            EnsureGraphContainer();
            if (_graphContainer == null || _currentIndexInBranch < 0 || string.IsNullOrEmpty(_currentBranchName))
                return null;
            if (_currentBranchName != branchName) return null;

            var lines = _graphContainer.Find(_linesContainerName);
            var lineObj = lines?.Find($"Line_{branchName}");
            var lineRenderer = lineObj?.GetComponent<LineRenderer>();
            if (lineRenderer == null || lineRenderer.positionCount == 0) return null;

            int startIndex = Mathf.Clamp(_currentIndexInBranch, 0, lineRenderer.positionCount - 1);
            int endIndex = Mathf.Clamp(indexInBranch, 0, lineRenderer.positionCount - 1);

            if (startIndex == endIndex)
                return new[] { transform.position, lineRenderer.GetPosition(endIndex) + Vector3.up * _heightOffset };

            int step = startIndex < endIndex ? 1 : -1;
            var path = new List<Vector3>();
            for (int i = startIndex; i != endIndex + step; i += step)
                path.Add(lineRenderer.GetPosition(i) + Vector3.up * _heightOffset);

            return path.Count > 0 ? path.ToArray() : null;
        }

        private IEnumerator MoveAlongPathCoroutine(Vector3[] path, Vector3 finalPosition)
        {
            if (path != null && path.Length > 1)
            {
                for (int i = 1; i < path.Length; i++)
                    yield return MoveToPosition(path[i]);
            }
            else
            {
                yield return MoveToPosition(finalPosition + Vector3.up * _heightOffset);
            }
            _moveCoroutine = null;
        }

        private IEnumerator MoveToPositionCoroutine(Vector3 target)
        {
            yield return MoveToPosition(target + Vector3.up * _heightOffset);
            _moveCoroutine = null;
        }

        private IEnumerator MoveToPosition(Vector3 target)
        {
            var start = transform.position;
            float duration = Vector3.Distance(start, target) / _moveSpeed;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                t = t * t * (3f - 2f * t); // smoothstep

                transform.position = Vector3.Lerp(start, target, t);

                var dir = (target - transform.position).normalized;
                if (dir.sqrMagnitude > 0.01f)
                {
                    var lookRot = Quaternion.LookRotation(dir);
                    transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, _rotationSpeed * Time.deltaTime * Mathf.Deg2Rad);
                }
                yield return null;
            }

            transform.position = target;
        }
    }
}
