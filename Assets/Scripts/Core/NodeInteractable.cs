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

        /// <summary>
        /// Sets the commit data for this node. Call after instantiation.
        /// </summary>
        public void SetCommit(Commit commit)
        {
            _commit = commit;
        }

        private void OnMouseUpAsButton()
        {
            if (_commit == null)
                return;

            var ui = CommitDetailsUI.Instance;
            if (ui != null)
            {
                ui.ShowCommit(_commit);
            }
            else
            {
                Debug.LogWarning("[NodeInteractable] No CommitDetailsUI found. Assign one in the scene.");
            }
        }
    }
}
