using UnityEngine;
using UnityEngine.VFX;

namespace MyakuMyakuAR
{
    [RequireComponent(typeof(Yolo11SegARController))]
    sealed class MainController : MonoBehaviour
    {
        [SerializeField]
        VisualEffect vfx;

        readonly int _SegmentationTex = Shader.PropertyToID("_SegmentationTex");
        readonly int _ARRgbDTex = Shader.PropertyToID("_ARRgbDTex");

        void OnEnable()
        {
            if (TryGetComponent(out Yolo11SegARController yolo11Seg))
            {
                yolo11Seg.OnDetect += OnDetect;
            }
        }

        void OnDisable()
        {
            if (TryGetComponent(out Yolo11SegARController yolo11Seg))
            {
                yolo11Seg.OnDetect -= OnDetect;
            }
        }

        void OnDetect(Yolo11SegARController yolo11Seg)
        {
            vfx.SetTexture(_SegmentationTex, yolo11Seg.SegmentationTexture);
            vfx.SetTexture(_ARRgbDTex, yolo11Seg.ARCameraTexture);
        }
    }
}
