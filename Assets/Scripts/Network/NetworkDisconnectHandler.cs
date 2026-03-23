using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

namespace GitVisualizer.Network
{
    public class NetworkDisconnectHandler : MonoBehaviour
    {
        public static bool ShowConnectionLostMessage { get; private set; }

        public static void ClearConnectionLostFlag() => ShowConnectionLostMessage = false;

        private void Start()
        {
            var nm = NetworkManager.Singleton;
            if (nm != null)
                nm.OnClientDisconnectCallback += OnClientDisconnected;
        }

        private void OnDestroy()
        {
            var nm = NetworkManager.Singleton;
            if (nm != null)
                nm.OnClientDisconnectCallback -= OnClientDisconnected;
        }

        private void OnClientDisconnected(ulong clientId)
        {
            var nm = NetworkManager.Singleton;
            if (nm == null) return;

            ShowConnectionLostMessage = true;
            nm.Shutdown();
            SceneManager.LoadScene("Scene_MainMenu");
        }
    }
}
