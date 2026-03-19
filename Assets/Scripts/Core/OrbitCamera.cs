using UnityEngine;

namespace GitVisualizer.Core
{
    /// <summary>
    /// Orbit camera with pan (WASD), zoom (scroll), and orbit (right-click drag) around a target.
    /// Attach to Main Camera.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class OrbitCamera : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField]
        private Transform _target;

        [Header("Orbit (Right-Click Drag)")]
        [SerializeField]
        private float _orbitSpeed = 120f;

        [SerializeField]
        private float _minVerticalAngle = -89f;

        [SerializeField]
        private float _maxVerticalAngle = 89f;

        [Header("Pan (WASD)")]
        [SerializeField]
        private float _panSpeed = 8f;

        [Header("Zoom (Scroll Wheel)")]
        [SerializeField]
        private float _zoomSpeed = 5f;

        [SerializeField]
        private float _minDistance = 2f;

        [SerializeField]
        private float _maxDistance = 100f;

        [Header("Smoothing")]
        [SerializeField]
        private float _smoothTime = 0.15f;

        private float _currentYaw;
        private float _currentPitch;
        private float _currentDistance;
        private Vector3 _panOffset;
        private Vector3 _velocity;

        private void Start()
        {
            var toCam = transform.position - GetTargetPosition();
            _currentDistance = Mathf.Clamp(toCam.magnitude, _minDistance, _maxDistance);

            var dir = toCam.normalized;
            _currentYaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            _currentPitch = Mathf.Asin(Mathf.Clamp(dir.y, -1f, 1f)) * Mathf.Rad2Deg;

            _panOffset = Vector3.zero;
        }

        private void LateUpdate()
        {
            HandleOrbit();
            HandlePan();
            HandleZoom();
            UpdateCameraPosition();
        }

        private void HandleOrbit()
        {
            if (!Input.GetMouseButton(1))
                return;

            var deltaX = Input.GetAxis("Mouse X");
            var deltaY = Input.GetAxis("Mouse Y");

            _currentYaw += deltaX * _orbitSpeed * Time.deltaTime;
            _currentPitch -= deltaY * _orbitSpeed * Time.deltaTime;
            _currentPitch = Mathf.Clamp(_currentPitch, _minVerticalAngle, _maxVerticalAngle);
        }

        private void HandlePan()
        {
            var move = Vector3.zero;
            if (Input.GetKey(KeyCode.W)) move += Vector3.forward;
            if (Input.GetKey(KeyCode.S)) move += Vector3.back;
            if (Input.GetKey(KeyCode.A)) move += Vector3.left;
            if (Input.GetKey(KeyCode.D)) move += Vector3.right;

            if (move.sqrMagnitude > 0.01f)
            {
                var camRight = transform.right;
                var camForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
                _panOffset += (camForward * move.z + camRight * move.x) * _panSpeed * Time.deltaTime;
            }
        }

        private void HandleZoom()
        {
            var scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                _currentDistance -= scroll * _zoomSpeed * _currentDistance;
                _currentDistance = Mathf.Clamp(_currentDistance, _minDistance, _maxDistance);
            }
        }

        private void UpdateCameraPosition()
        {
            var targetPos = GetTargetPosition() + _panOffset;

            var rotation = Quaternion.Euler(_currentPitch, _currentYaw, 0f);
            var offset = rotation * new Vector3(0f, 0f, -_currentDistance);
            var desiredPosition = targetPos + offset;

            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref _velocity, _smoothTime);
            transform.LookAt(targetPos + _panOffset);
        }

        private Vector3 GetTargetPosition()
        {
            return _target != null ? _target.position : Vector3.zero;
        }

        /// <summary>
        /// Sets the orbit target (e.g., graph center).
        /// </summary>
        public void SetTarget(Transform target)
        {
            _target = target;
        }
    }
}
