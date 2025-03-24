using System;
using System.Text;
using System.Threading;
using Microsoft.ML.OnnxRuntime.Examples;
using TextureSource;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MyakuMyakuAR
{
    [RequireComponent(typeof(VirtualTextureSource))]
    public sealed class Yolo11SegARController : MonoBehaviour
    {
        [SerializeField]
        RemoteFile modelFile = new("https://github.com/asus4/onnxruntime-unity-examples/releases/download/v0.2.7/yolo11n-seg-dynamic.onnx");

        [SerializeField]
        Yolo11Seg.Options options;

        [Header("Debug Visualization Options")]
        [SerializeField]
        TMPro.TMP_Text detectionBoxPrefab;

        [SerializeField]
        RectTransform detectionContainer;

        [SerializeField]
        RawImage segmentationImage;

        AspectRatioFitter segmentationImageAspectRatioFitter;

        [SerializeField]
        int maxDetections = 20;

        [SerializeField]
        bool showDebugUI = true;

        VirtualTextureSource textureSource;
        Yolo11Seg inference;
        TMPro.TMP_Text[] detectionBoxes;
        Image[] detectionBoxOutline;
        readonly StringBuilder sb = new();
        Awaitable currentTask = null;

        public event Action<Yolo11SegARController> OnDetect;
        public ReadOnlySpan<Yolo11Seg.Detection> Detections => inference.Detections;
        public Texture SegmentationTexture => inference.SegmentationTexture;
        public Texture ARCameraTexture => textureSource.Texture;

        public bool ShowDebugUI
        {
            get => showDebugUI;
            set
            {
                showDebugUI = value;
                detectionContainer.gameObject.SetActive(value);
            }
        }

        async void Start()
        {
            byte[] onnxFile = await modelFile.Load(destroyCancellationToken);
            inference = new Yolo11Seg(onnxFile, options)
            {
                SegmentationFilterDelegate = SegmentationFilter,
            };

            detectionBoxes = new TMPro.TMP_Text[maxDetections];
            detectionBoxOutline = new Image[maxDetections];
            for (int i = 0; i < maxDetections; i++)
            {
                var box = Instantiate(detectionBoxPrefab, detectionContainer);
                box.name = $"Detection {i}";
                box.gameObject.SetActive(false);
                detectionBoxes[i] = box;
                detectionBoxOutline[i] = box.transform.GetChild(0).GetComponent<Image>();
            }

            if (TryGetComponent(out textureSource))
            {
                textureSource.OnTexture.AddListener(OnTexture);
            }

            if (!segmentationImage.TryGetComponent(out segmentationImageAspectRatioFitter))
            {
                Debug.LogWarning($"No {nameof(AspectRatioFitter)} found in {segmentationImage.name}");
            }

            ShowDebugUI = showDebugUI;
        }

        void OnDestroy()
        {
            if (TryGetComponent(out textureSource))
            {
                textureSource.OnTexture.RemoveListener(OnTexture);
            }
            inference?.Dispose();
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (Application.isPlaying)
            {
                ShowDebugUI = showDebugUI;
            }
        }
#endif // UNITY_EDITOR

        public void OnTexture(Texture texture)
        {
            if (inference == null)
            {
                return;
            }

            // Async
            bool isNextAvailable = currentTask == null || currentTask.IsCompleted;
            if (isNextAvailable)
            {
                currentTask = RunAsync(texture, destroyCancellationToken);
            }
        }

        public Rect ConvertToViewport(Rect rect)
        {
            return inference.ConvertToViewport(rect);
        }

        async Awaitable RunAsync(Texture texture, CancellationToken cancellationToken)
        {
            try
            {
                await inference.RunAsync(texture, cancellationToken);
            }
            catch (OperationCanceledException e)
            {
                Debug.LogWarning(e);
                return;
            }
            await Awaitable.MainThreadAsync();

            OnDetect?.Invoke(this);

            // Invoke events when the segmentation texture is updated
            if (ShowDebugUI)
            {
                UpdateDetectionBox(inference.Detections);
                var segTex = inference.SegmentationTexture;
                segmentationImage.texture = segTex;
                if (segmentationImageAspectRatioFitter != null)
                {
                    segmentationImageAspectRatioFitter.aspectRatio = (float)segTex.width / segTex.height;
                }
            }
        }

        void UpdateDetectionBox(ReadOnlySpan<Yolo11Seg.Detection> detections)
        {
            var labels = inference.labelNames;
            Vector2 viewportSize = detectionContainer.rect.size;

            int i;
            int length = Math.Min(detections.Length, maxDetections);
            for (i = 0; i < length; i++)
            {
                var detection = detections[i];

                var color = detection.GetColor();

                var box = detectionBoxes[i];
                box.gameObject.SetActive(true);

                // Using StringBuilder to reduce GC
                sb.Clear();
                sb.Append(labels[detection.label]);
                sb.Append(": ");
                sb.Append((int)(detection.probability * 100));
                sb.Append('%');
                box.SetText(sb);
                box.color = color;

                // The detection rect is model space
                // Needs to be converted to viewport space
                RectTransform rt = box.rectTransform;
                Rect rect = inference.ConvertToViewport(detection.rect);
                rt.anchoredPosition = rect.min * viewportSize;
                rt.sizeDelta = rect.size * viewportSize;

                detectionBoxOutline[i].color = color;
            }

            // Hide unused boxes
            for (; i < maxDetections; i++)
            {
                detectionBoxes[i].gameObject.SetActive(false);
            }
        }

        int SegmentationFilter(NativeArray<Yolo11Seg.Detection>.ReadOnly detections)
        {
            return 0;
        }
    }
}
