using UnityEngine;
using TextureSource;

namespace MyakuMyakuAR
{
    [RequireComponent(typeof(VirtualTextureSource))]
    sealed class MainController : MonoBehaviour
    {
        void OnEnable()
        {
            if (TryGetComponent(out VirtualTextureSource source))
            {
                source.OnTexture.AddListener(OnTexture);
            }
        }

        void OnDisable()
        {
            if (TryGetComponent(out VirtualTextureSource source))
            {
                source.OnTexture.RemoveListener(OnTexture);
            }
        }

        void OnTexture(Texture texture)
        {
            Debug.Log($"Texture {texture.width}x{texture.height} received");
        }
    }
}
