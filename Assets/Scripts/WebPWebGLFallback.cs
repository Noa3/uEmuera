#if UNITY_WEBGL
using UnityEngine;

namespace WebP
{
    /// <summary>
    /// Managed WebGL fallback for the native WebP package.
    /// Browsers may decode WebP through Unity's image loader; if the active
    /// Unity/WebGL runtime does not support it, the caller receives a normal
    /// decoding error instead of a missing native-library failure.
    /// </summary>
    public enum Error
    {
        Success = 0,
        InvalidHeader = 20,
        DecodingError = 30,
    }

    public static class Texture2DExt
    {
        public delegate void ScalingFunction(ref int width, ref int height);

        public static Texture2D CreateTexture2DFromWebP(
            byte[] data,
            bool mipmaps,
            bool linear,
            out Error error,
            ScalingFunction scalingFunction = null)
        {
            error = Error.DecodingError;
            if (data == null || data.Length == 0)
            {
                error = Error.InvalidHeader;
                return null;
            }

            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, mipmaps, linear);
            if (scalingFunction != null)
            {
                // The native implementation can scale during decode. Keep the
                // WebGL fallback explicit rather than returning an incorrectly
                // sized texture.
                Object.Destroy(texture);
                return null;
            }

            if (texture.LoadImage(data, markNonReadable: false))
            {
                error = Error.Success;
                return texture;
            }

            Object.Destroy(texture);
            return null;
        }
    }
}
#endif
