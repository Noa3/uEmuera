using TMPro;
using UnityEngine;

namespace UnityEngine.UI
{
    /// <summary>
    /// A TextMeshPro mesh modifier that enforces monospaced character spacing.
    /// This component modifies the text mesh vertices to ensure uniform character width,
    /// supporting both half-width and full-width characters commonly used in Japanese text.
    /// </summary>
    public class TMPMonospaced : MonoBehaviour
    {
        /// <summary>
        /// Unicode character for zero-width space (U+200B).
        /// </summary>
        private const char ZERO_WIDTH_SPACE = (char)8203;
        
        /// <summary>
        /// Array of characters used to detect rich text tags.
        /// </summary>
        static readonly char[] _index_any = new char[] { '<', '\n' };

        /// <summary>
        /// Width size to use for character spacing calculation.
        /// If zero or less than font size, the font size will be used.
        /// </summary>
        [Tooltip("Width size for character spacing calculation. Uses font size if not set.")]
        public float widthsize = 0;
        
        /// <summary>
        /// Flag to track if event is subscribed.
        /// </summary>
        private bool isSubscribed_ = false;

        /// <summary>
        /// Gets the next valid character index, skipping over rich text tags.
        /// </summary>
        /// <param name="content">The text content.</param>
        /// <param name="i">Current index.</param>
        /// <returns>The next valid character index.</returns>
        int GetNextValidIndex(string content, int i)
        {
            if (i >= content.Length)
                return i;

            var c = content[i];
            while (c == '<')
            {
                var t1 = content.IndexOf('>', i + 1);
                var t2 = content.IndexOfAny(_index_any, i + 1);
                if (t2 == -1)
                    t2 = int.MaxValue;
                if (t1 < t2 && t1 - i > 1)
                {
                    var sub = content.Substring(i + 1, t1 - i - 1).ToLower();
                    if (sub == "b" ||
                        sub == "i" ||
                        sub == "/b" ||
                        sub == "/i" ||
                        sub == "/size" ||
                        sub == "/color" ||
                        sub.IndexOf("size") == 0 ||
                        sub.IndexOf("color") == 0)
                    {
                        i = t1 + 1;
                        if (i >= content.Length)
                            return i;
                        c = content[i];
                    }
                    else
                        break;
                }
                else
                    break;
            }
            return i;
        }

        /// <summary>
        /// Applies monospaced character spacing to the TextMeshPro component.
        /// </summary>
        public void ApplyMonospacing()
        {
            if (!enabled || textComponent == null || string.IsNullOrEmpty(textComponent.text))
                return;

            textComponent.ForceMeshUpdate();
            
            var textInfo = textComponent.textInfo;
            if (textInfo == null || textInfo.characterCount == 0)
                return;

            var size = widthsize;
            if (size < textComponent.fontSize)
                size = textComponent.fontSize;

            var content = textComponent.text;
            var length = content.Length;
            var richtext = textComponent.richText;
            if (string.IsNullOrWhiteSpace(content))
                return;

            int i = 0;
            if (richtext)
                i = GetNextValidIndex(content, i);
            
            float s = 0;
            float si = 0;
            float linestart = 0;

            // Vertex index tracker
            int visibleCharIndex = 0;
            char c = '\x0';
            
            for (; i < length && visibleCharIndex < textInfo.characterCount; ++i)
            {
                c = content[i];
                if (c == '\n')
                {
                    if (richtext)
                    {
                        var ni = i + 1;
                        i = GetNextValidIndex(content, ni);
                        if (i >= length)
                            break;
                        else if (i == ni)
                            i -= 1;
                    }
                    s = 0;
                    continue;
                }
                else if (c == ' ')
                {
                    s += size / 2.0f;
                    visibleCharIndex++;
                    continue;
                }
                else if (richtext && c == '<')
                {
                    var nexti = GetNextValidIndex(content, i);
                    if (nexti > i)
                    {
                        i = nexti;
                        if (i >= length)
                            break;
                        i -= 1;
                        continue;
                    }
                }

                if (visibleCharIndex >= textInfo.characterCount)
                    break;

                var charInfo = textInfo.characterInfo[visibleCharIndex];
                if (!charInfo.isVisible)
                {
                    visibleCharIndex++;
                    continue;
                }

                // Calculate character width based on full/half width
                if (uEmuera.Utils.CheckFullSize(c))
                    si = size;
                else if (uEmuera.Utils.CheckHalfSize(c))
                    si = size / 2.0f;
                else if (c == ZERO_WIDTH_SPACE)
                    si = 0;
                else
                    si = size;

                // Get the mesh info for this character
                int materialIndex = charInfo.materialReferenceIndex;
                int vertexIndex = charInfo.vertexIndex;

                var meshInfo = textInfo.meshInfo[materialIndex];
                var vertices = meshInfo.vertices;

                // Calculate character width from vertices
                float charWidth = vertices[vertexIndex + 2].x - vertices[vertexIndex + 0].x;

                // Calculate offset to center character in its slot
                float offset = s + (si - charWidth) / 2.0f;

                // Apply offset to all 4 vertices of this character
                float originalX = vertices[vertexIndex].x;
                float delta = linestart + offset - originalX;

                vertices[vertexIndex + 0].x += delta;
                vertices[vertexIndex + 1].x += delta;
                vertices[vertexIndex + 2].x += delta;
                vertices[vertexIndex + 3].x += delta;

                s += si;
                visibleCharIndex++;
            }

            // Update the mesh with modified vertices
            for (int mi = 0; mi < textInfo.meshInfo.Length; mi++)
            {
                textInfo.meshInfo[mi].mesh.vertices = textInfo.meshInfo[mi].vertices;
                textComponent.UpdateGeometry(textInfo.meshInfo[mi].mesh, mi);
            }
        }

        /// <summary>
        /// Called when the TMP text is changed to re-apply monospacing.
        /// </summary>
        void OnEnable()
        {
            if (!isSubscribed_)
            {
                TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged);
                isSubscribed_ = true;
            }
        }

        /// <summary>
        /// Cleanup event subscription.
        /// </summary>
        void OnDisable()
        {
            if (isSubscribed_)
            {
                TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChanged);
                isSubscribed_ = false;
            }
        }

        /// <summary>
        /// Handles text change events from TextMeshPro.
        /// </summary>
        /// <param name="obj">The object that triggered the event.</param>
        void OnTextChanged(Object obj)
        {
            if (obj == textComponent)
            {
                ApplyMonospacing();
            }
        }

        /// <summary>
        /// Gets the RectTransform of this component.
        /// </summary>
        RectTransform rectTransform => transform as RectTransform;

        /// <summary>
        /// Gets the TextMeshProUGUI component attached to this GameObject.
        /// </summary>
        TextMeshProUGUI textComponent
        {
            get
            {
                if (text_ == null)
                    text_ = GetComponent<TextMeshProUGUI>();
                return text_;
            }
        }
        TextMeshProUGUI text_ = null;
    }
}
