using System;
using Microsoft.ML.OnnxRuntime.Examples;
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

        [SerializeField]
        Vector3 targetViewport = new Vector3(0.5f, 0.5f, 10f);


        readonly int _SegmentationTex = Shader.PropertyToID("_SegmentationTex");
        readonly int _ARRgbDTex = Shader.PropertyToID("_ARRgbDTex");
        readonly int _SpawnUvMinMax = Shader.PropertyToID("_SpawnUvMinMax");
        readonly int _SpawnRate = Shader.PropertyToID("_SpawnRate");
        readonly int _TargetPosition = Shader.PropertyToID("_TargetPosition");

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

            vfx.SetVector3(_TargetPosition, Camera.main.ViewportToWorldPoint(targetViewport));

            var detections = yolo11Seg.Detections;
            if (detections.Length == 0)
            {
                vfx.SetFloat(_SpawnRate, 0);
            }
            else
            {
                // Rect r = detections[0].rect; // v1
                Rect r = GetEncapsulateRect(detections); // v2

                r = yolo11Seg.ConvertToViewport(r);
                float area = r.width * r.height;

                // Debug.Log($"r: {r}, area: {area}");
                vfx.SetFloat(_SpawnRate, area);
                vfx.SetVector4(_SpawnUvMinMax, new Vector4(r.xMin, r.yMin, r.xMax, r.yMax));

            }
        }

        static Rect GetEncapsulateRect(ReadOnlySpan<Yolo11Seg.Detection> detections)
        {
            if (detections.Length == 0)
            {
                return default;
            }

            var detection = detections[0];
            Rect r = detection.rect;
            if (detections.Length == 1)
            {
                return r;
            }

            // Length > 1
            for (int i = 1; i < detections.Length; i++)
            {
                Rect r2 = detections[i].rect;
                r = Rect.MinMaxRect(
                    Mathf.Min(r.xMin, r2.xMin),
                    Mathf.Min(r.yMin, r2.yMin),
                    Mathf.Max(r.xMax, r2.xMax),
                    Mathf.Max(r.yMax, r2.yMax));
            }
            return r;
        }
    }
}
