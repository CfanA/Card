using UnityEngine;

using CardGame.Map;
using CardGame.Battle;
using CardGame.Content;
using CardGame.Run;
namespace CardGame.Map
{
    /// <summary>
    /// Camera fitting strategy.
    /// </summary>
    public enum MapCameraFitMode
    {
        FitAll,
        FitHeightOnly,
        FitWidthOnly
    }

    /// <summary>
    /// Fits camera framing to fully include a target hierarchy in world space.
    /// </summary>
    public class MapCameraFitter : MonoBehaviour
    {
        [Tooltip("Camera to fit. If null, MainCamera is used.")]
        public Camera TargetCamera;

        [Tooltip("Target root to calculate bounds from. Usually MapViewRoot.")]
        public Transform TargetRoot;

        [Range(0f, 1f)]
        [Tooltip("Extra framing margin ratio. 0.1 means 10% padding.")]
        public float Padding = 0.1f;

        [Tooltip("How orthographic size is computed from bounds.")]
        public MapCameraFitMode FitMode = MapCameraFitMode.FitAll;

        [Tooltip("Minimum orthographic size after clamping (FitAll/FitHeightOnly).")]
        public float MinOrthoSize = 3f;

        [Tooltip("Maximum orthographic size after clamping (FitAll/FitHeightOnly).")]
        public float MaxOrthoSize = 7f;

        [Tooltip("If true, Fit() only applies once until ForceFit() is called.")]
        public bool FitOnStartOnly = true;

        private bool _hasFitted;

        /// <summary>
        /// Fits camera to the currently assigned TargetRoot.
        /// </summary>
        public bool Fit()
        {
            if (FitOnStartOnly && _hasFitted)
            {
                return false;
            }

            return FitToTarget(TargetRoot);
        }

        /// <summary>
        /// Forces a fit regardless of FitOnStartOnly state.
        /// </summary>
        public bool ForceFit()
        {
            return FitToTarget(TargetRoot, true);
        }

        /// <summary>
        /// Fits camera to a given target hierarchy using renderer bounds.
        /// </summary>
        public bool FitToTarget(Transform target)
        {
            return FitToTarget(target, false);
        }

        private bool FitToTarget(Transform target, bool force)
        {
            if (FitOnStartOnly && _hasFitted && !force)
            {
                return false;
            }

            var cam = TargetCamera != null ? TargetCamera : Camera.main;
            if (cam == null || target == null)
            {
                return false;
            }

            var renderers = target.GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0)
            {
                return false;
            }

            bool hasBounds = false;
            Bounds bounds = default;
            for (int i = 0; i < renderers.Length; i++)
            {
                var r = renderers[i];
                if (r == null || !r.enabled)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = r.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(r.bounds);
                }
            }

            if (!hasBounds)
            {
                return false;
            }

            var center = bounds.center;
            var camPos = cam.transform.position;
            cam.transform.position = new Vector3(center.x, center.y, camPos.z);

            if (cam.orthographic)
            {
                float aspect = Mathf.Max(0.0001f, cam.aspect);
                float sizeY = bounds.extents.y;
                float sizeX = bounds.extents.x / aspect;
                float safePadding = 1f + Mathf.Max(0f, Padding);
                float requiredAll = Mathf.Max(sizeY, sizeX) * safePadding;
                float requiredHeight = sizeY * safePadding;
                float requiredWidth = sizeX * safePadding;

                float orthoSize = requiredAll;
                switch (FitMode)
                {
                    case MapCameraFitMode.FitHeightOnly:
                        orthoSize = requiredHeight;
                        break;
                    case MapCameraFitMode.FitWidthOnly:
                        orthoSize = requiredWidth;
                        break;
                    default:
                        orthoSize = requiredAll;
                        break;
                }

                // Only clamp for FitAll/FitHeightOnly so we can intentionally keep a zoomed-in view.
                if (FitMode == MapCameraFitMode.FitAll || FitMode == MapCameraFitMode.FitHeightOnly)
                {
                    float min = Mathf.Max(0.01f, MinOrthoSize);
                    float max = Mathf.Max(min, MaxOrthoSize);
                    orthoSize = Mathf.Clamp(orthoSize, min, max);
                }
                else
                {
                    orthoSize = Mathf.Max(0.01f, orthoSize);
                }

                cam.orthographicSize = orthoSize;
            }

            _hasFitted = true;
            return true;
        }
    }
}


