using UnityEngine;
using GitVisualizer.Models;

namespace GitVisualizer.Core
{
    [RequireComponent(typeof(Collider))]
    public class NodeInteractable : MonoBehaviour
    {
        private Commit _commit;
        private string _branchName;
        private int _indexInBranch;

        public void SetCommit(Commit commit) => _commit = commit;

        public void SetBranchInfo(string branchName, int indexInBranch)
        {
            _branchName = branchName ?? "";
            _indexInBranch = indexInBranch;
        }

        private void OnMouseUpAsButton()
        {
            if (_commit == null) return;

            GetComponent<NodeClickEffects>()?.PlayClickEffect();
            GitVisualizer.UI.CommitDetailsUI.Instance?.ShowCommit(_commit);

            var avatar = AvatarController.LocalInstance ?? FindObjectOfType<AvatarController>();
            if (avatar != null)
                avatar.NavigateTo(transform, _branchName, _indexInBranch);
        }
    }
}
