using UnityEngine;
using GitVisualizer.Models;

namespace GitVisualizer.Core
{
    /// <summary>
    /// Attach to CommitNode prefabs. Detects clicks via physics raycast (OnMouseDown).
    /// Stores commit data and notifies CommitDetailsUI when clicked.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class NodeInteractable : MonoBehaviour
    {
        private Commit _commit;
        private string _branchName;
        private int _indexInBranch;

        /// <summary>
        /// Sets the commit data for this node. Call after instantiation.
        /// </summary>
        public void SetCommit(Commit commit)
        {
            _commit = commit;
        }

        /// <summary>
        /// Sets branch info for avatar path-finding.
        /// </summary>
        public void SetBranchInfo(string branchName, int indexInBranch)
        {
            _branchName = branchName ?? "";
            _indexInBranch = indexInBranch;
        }

        private void OnMouseUpAsButton()
        {
            if (_commit == null)
                return;

            var effects = GetComponent<NodeClickEffects>();
            if (effects != null)
                effects.PlayClickEffect();

            var ui = GitVisualizer.UI.CommitDetailsUI.Instance;
            if (ui != null)
                ui.ShowCommit(_commit);

            var avatar = FindObjectOfType<AvatarController>();
            if (avatar != null)
                avatar.NavigateTo(transform, _branchName, _indexInBranch);
        }
    }
}
