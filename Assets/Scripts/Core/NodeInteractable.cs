using UnityEngine;
using GitVisualizer.Models;

namespace GitVisualizer.Core
{
    /// <summary>Detects clicks on commit nodes and shows details.</summary>
    [RequireComponent(typeof(Collider))]
    public class NodeInteractable : MonoBehaviour
    {
        private Commit _commit;
        private string _branchName;
        private int _indexInBranch;

        public void SetCommit(Commit commit)
        {
            _commit = commit;
        }

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
