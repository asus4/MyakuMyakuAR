using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace MyakuMyakuAR
{
    /// <summary>
    /// Syncs the AR camera projection matrix to the current camera.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    sealed class ARSyncedCamera : MonoBehaviour
    {
        [SerializeField]
        ARCameraManager cameraManager;

        Camera syncedCamera;

        void OnEnable()
        {
            syncedCamera = GetComponent<Camera>();
            cameraManager.frameReceived += OnCameraFrameReceived;
        }

        void OnDisable()
        {
            cameraManager.frameReceived -= OnCameraFrameReceived;
        }

        void OnCameraFrameReceived(ARCameraFrameEventArgs args)
        {
            if (args.projectionMatrix.HasValue)
            {
                syncedCamera.projectionMatrix = args.projectionMatrix.Value;
            }
        }
    }
}
