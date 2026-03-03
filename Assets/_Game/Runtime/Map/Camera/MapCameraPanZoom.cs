using UnityEngine;

using CardGame.Map;
using CardGame.Battle;
using CardGame.Content;
using CardGame.Run;
namespace CardGame.Map
{
    /// <summary>
    /// Runtime camera pan/zoom controller constrained by target bounds.
    /// </summary>
    public class MapCameraPanZoom : MonoBehaviour
    {
        [Tooltip("Controlled camera. If null, MainCamera is used.")]
        public Camera TargetCamera;

        [Tooltip("Map root used to compute movement limits.")]
        public Transform TargetRoot;

        [Header("Input")]
        [Tooltip("If false, wheel pans vertically; if true, wheel zooms.")]
        public bool MouseWheelZoom = false;
        public bool DragWithMiddleMouse = true;
        public bool DragWithRightMouse = true;

        [Header("Speed")]
        public float PanSpeed = 6f;
        public float ZoomSpeed = 6f;

        [Header("Zoom Limits")]
        public float MinOrtho = 3f;
        public float MaxOrtho = 12f;

        [Header("Bounds")]
        public float ViewMarginWorld = 0.8f;

        private bool _hasBounds;
        private Bounds _targetBounds;
        private bool _isDragging;
        private Vector3 _lastMousePosition;

        private void Update()
        {
            var cam = TargetCamera != null ? TargetCamera : Camera.main;
            if (cam == null || TargetRoot == null)
            {
                return;
            }

            RefreshBoundsIfNeeded();
            if (!_hasBounds)
            {
                return;
            }

            HandleWheel(cam);
            HandleDrag(cam);
            ClampCameraToBounds(cam);
        }

        /// <summary>
        /// Rebuilds cached world bounds from target renderers.
        /// </summary>
        public bool RefreshBounds()
        {
            if (TargetRoot == null)
            {
                _hasBounds = false;
                return false;
            }

            var renderers = TargetRoot.GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0)
            {
                _hasBounds = false;
                return false;
            }

            bool has = false;
            Bounds merged = default;
            for (int i = 0; i < renderers.Length; i++)
            {
                var r = renderers[i];
                if (r == null || !r.enabled)
                {
                    continue;
                }

                if (!has)
                {
                    merged = r.bounds;
                    has = true;
                }
                else
                {
                    merged.Encapsulate(r.bounds);
                }
            }

            _targetBounds = merged;
            _hasBounds = has;
            return _hasBounds;
        }

        private void RefreshBoundsIfNeeded()
        {
            if (!_hasBounds)
            {
                RefreshBounds();
            }
        }

        private void HandleWheel(Camera cam)
        {
            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) < 0.0001f)
            {
                return;
            }

            if (MouseWheelZoom && cam.orthographic)
            {
                float zoomDelta = scroll * ZoomSpeed * Time.unscaledDeltaTime;
                float next = cam.orthographicSize - zoomDelta;
                float min = Mathf.Max(0.01f, MinOrtho);
                float max = Mathf.Max(min, MaxOrtho);
                cam.orthographicSize = Mathf.Clamp(next, min, max);
                return;
            }

            float panUnits = scroll * PanSpeed * Mathf.Max(0.25f, cam.orthographicSize * 0.18f);
            cam.transform.position += new Vector3(0f, panUnits, 0f);
        }

        private void HandleDrag(Camera cam)
        {
            bool dragDown = (DragWithMiddleMouse && Input.GetMouseButtonDown(2))
                            || (DragWithRightMouse && Input.GetMouseButtonDown(1));
            bool dragHeld = (DragWithMiddleMouse && Input.GetMouseButton(2))
                            || (DragWithRightMouse && Input.GetMouseButton(1));
            bool dragUp = (DragWithMiddleMouse && Input.GetMouseButtonUp(2))
                          || (DragWithRightMouse && Input.GetMouseButtonUp(1));

            if (dragDown)
            {
                _isDragging = true;
                _lastMousePosition = Input.mousePosition;
            }

            if (_isDragging && dragHeld)
            {
                Vector3 currentMouse = Input.mousePosition;
                Vector3 worldPrev = cam.ScreenToWorldPoint(new Vector3(_lastMousePosition.x, _lastMousePosition.y, -cam.transform.position.z));
                Vector3 worldCurrent = cam.ScreenToWorldPoint(new Vector3(currentMouse.x, currentMouse.y, -cam.transform.position.z));
                Vector3 delta = worldPrev - worldCurrent;
                cam.transform.position += new Vector3(delta.x, delta.y, 0f);
                _lastMousePosition = currentMouse;
            }

            if (dragUp)
            {
                _isDragging = false;
            }
        }

        private void ClampCameraToBounds(Camera cam)
        {
            float margin = Mathf.Max(0f, ViewMarginWorld);
            if (!cam.orthographic)
            {
                var p = cam.transform.position;
                cam.transform.position = new Vector3(
                    Mathf.Clamp(p.x, _targetBounds.min.x - margin, _targetBounds.max.x + margin),
                    Mathf.Clamp(p.y, _targetBounds.min.y - margin, _targetBounds.max.y + margin),
                    p.z);
                return;
            }

            float aspect = Mathf.Max(0.0001f, cam.aspect);
            float halfH = cam.orthographicSize;
            float halfW = cam.orthographicSize * aspect;
            float mapMinX = _targetBounds.min.x;
            float mapMaxX = _targetBounds.max.x;
            float mapMinY = _targetBounds.min.y;
            float mapMaxY = _targetBounds.max.y;
            var pos = cam.transform.position;
            float minX = mapMinX + halfW - margin;
            float maxX = mapMaxX - halfW + margin;
            float minY = mapMinY + halfH - margin;
            float maxY = mapMaxY - halfH + margin;

            float targetX = minX > maxX
                ? _targetBounds.center.x
                : Mathf.Clamp(pos.x, minX, maxX);

            float targetY = minY > maxY
                ? _targetBounds.center.y
                : Mathf.Clamp(pos.y, minY, maxY);

            cam.transform.position = new Vector3(targetX, targetY, pos.z);
        }
    }
}


