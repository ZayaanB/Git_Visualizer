using System.Collections;
using UnityEngine;
using GitVisualizer.Models;

namespace GitVisualizer.Core
{
    [RequireComponent(typeof(Collider))]
    public class NodeInteractable : MonoBehaviour
    {
        private const float ResolveHoldDuration = 3f;

        [SerializeField] private AudioClip _clickSfx;

        private Commit _commit;
        private string _branchName;
        private int _indexInBranch;
        private int _globalIndex = -1;

        private bool _isConflictNode;
        private bool _isResolved;
        private bool _isHolding;
        private float _holdStartTime;
        private MaterialPropertyBlock _propertyBlock;
        private Renderer _renderer;

        public int GlobalIndex => _globalIndex;

        public void SetCommit(Commit commit) => _commit = commit;

        public void SetBranchInfo(string branchName, int indexInBranch)
        {
            _branchName = branchName ?? "";
            _indexInBranch = indexInBranch;
        }

        public void SetGlobalIndex(int globalIndex) => _globalIndex = globalIndex;

        private void Start()
        {
            _renderer = GetComponent<Renderer>();
            RefreshConflictState();
        }

        private void Update()
        {
            if (_isConflictNode && !_isResolved && _renderer != null)
            {
                _propertyBlock ??= new MaterialPropertyBlock();
                float pulse = 0.7f + 0.3f * Mathf.Sin(Time.time * 4f);
                var c = new Color(1f, pulse * 0.2f, pulse * 0.2f, 1f);
                _propertyBlock.SetColor("_BaseColor", c);
                _propertyBlock.SetColor("_Color", c);
                _renderer.SetPropertyBlock(_propertyBlock);
            }

            if (_isHolding && Input.GetMouseButton(0) == false)
            {
                _isHolding = false;
                return;
            }

            if (_isHolding && Time.time - _holdStartTime >= ResolveHoldDuration)
            {
                _isHolding = false;
                TryResolveConflict();
            }
        }

        public void RefreshConflictState()
        {
            var manager = GameLoopManager.Instance;
            if (manager == null || _globalIndex < 0)
            {
                _isConflictNode = false;
                RestoreOriginalMaterial();
                return;
            }

            _isConflictNode = manager.IsConflictNode(_globalIndex);
            if (_isConflictNode && !_isResolved)
                ApplyConflictMaterial();
            else
                RestoreOriginalMaterial();
        }

        public void SetResolved()
        {
            _isResolved = true;
            RestoreOriginalMaterial();
        }

        private void ApplyConflictMaterial()
        {
            if (_renderer == null) return;
            _propertyBlock ??= new MaterialPropertyBlock();
            var c = new Color(1f, 0.2f, 0.2f, 1f);
            _propertyBlock.SetColor("_BaseColor", c);
            _propertyBlock.SetColor("_Color", c);
            _renderer.SetPropertyBlock(_propertyBlock);
        }

        private void RestoreOriginalMaterial()
        {
            if (_renderer == null) return;
            _renderer.SetPropertyBlock(null);
        }

        private void OnMouseDown()
        {
            if (_commit == null) return;

            var manager = GameLoopManager.Instance;
            if (manager != null && _globalIndex >= 0 && manager.IsConflictNode(_globalIndex) && !_isResolved)
            {
                _isHolding = true;
                _holdStartTime = Time.time;
            }
        }

        private void OnMouseUp()
        {
            if (_isHolding)
            {
                if (Time.time - _holdStartTime >= ResolveHoldDuration)
                    TryResolveConflict();
                _isHolding = false;
            }
        }

        private void OnMouseUpAsButton()
        {
            if (_commit == null) return;

            var manager = GameLoopManager.Instance;
            if (manager != null && _globalIndex >= 0 && manager.IsConflictNode(_globalIndex) && !_isResolved)
                return;

            GetComponent<NodeClickEffects>()?.PlayClickEffect();
            if (_clickSfx != null)
                AudioManager.Instance?.PlaySFX(_clickSfx);
            GitVisualizer.UI.CommitDetailsUI.Instance?.ShowCommit(_commit);

            var avatar = AvatarController.LocalInstance ?? FindFirstObjectByType<AvatarController>();
            if (avatar != null)
                avatar.NavigateTo(transform, _branchName, _indexInBranch);
        }

        private void TryResolveConflict()
        {
            var manager = GameLoopManager.Instance;
            if (manager == null || _globalIndex < 0 || !manager.IsConflictNode(_globalIndex) || _isResolved)
                return;

            manager.ResolveConflictServerRpc(_globalIndex);
        }

        private void OnDestroy()
        {
            if (_renderer != null)
                _renderer.SetPropertyBlock(null);
        }
    }
}
