using UnityEngine;
using UnityEngine.VFX;

namespace MyakuMyakuAR
{
    [RequireComponent(typeof(Yolo11SegARController))]
    sealed class MainController : MonoBehaviour
    {
        [SerializeField]
        VisualEffect vfx;

        [SerializeField]
        Material postVfxMaterial;

        readonly int _SegmentationTex = Shader.PropertyToID("_SegmentationTex");
        readonly int _ARRgbDTex = Shader.PropertyToID("_ARRgbDTex");
        readonly int _SpawnUvMinMax = Shader.PropertyToID("_SpawnUvMinMax");
        readonly int _SpawnRate = Shader.PropertyToID("_SpawnRate");

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
            postVfxMaterial.SetTexture(_ARRgbDTex, yolo11Seg.ARCameraTexture);
            vfx.SetTexture(_SegmentationTex, yolo11Seg.SegmentationTexture);
            vfx.SetTexture(_ARRgbDTex, yolo11Seg.ARCameraTexture);

            var detections = yolo11Seg.Detections;
            if (detections.Length == 0)
            {
                vfx.SetFloat(_SpawnRate, 0);
            }
            else
            {
                var detection = detections[0];
                Rect r = yolo11Seg.ConvertToViewport(detection.rect);
                float area = r.width * r.height;
                vfx.SetFloat(_SpawnRate, area);
                vfx.SetVector4(_SpawnUvMinMax, new Vector4(r.xMin, r.yMin, r.xMax, r.yMax));
            }
        }
    }
}
