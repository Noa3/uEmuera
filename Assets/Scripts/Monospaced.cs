using System;

namespace UnityEngine.UI
{
    /// <summary>
    /// A mesh effect that modifies text to use monospaced character spacing.
    /// This component adjusts the position of text characters to ensure uniform width,
    /// supporting both half-width (ASCII) and full-width (Japanese) characters.
    /// </summary>
    public class Monospaced : BaseMeshEffect
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
        /// Modifies the mesh vertices to apply monospaced character spacing.
        /// </summary>
        /// <param name="vh">The vertex helper containing the mesh data.</param>
        public override void ModifyMesh(VertexHelper vh)
        {
            if (!enabled || string.IsNullOrEmpty(text.text))
                return;

            var size = widthsize;
            if (size < text.fontSize)
                size = text.fontSize;
            var count = vh.currentVertCount / 4;
            var content = text.text;
            var length = content.Length;
            var richtext = text.supportRichText;
            if (string.IsNullOrWhiteSpace(content))
                return;

            int i = 0;
            if (richtext)
                i = GetNextValidIndex(content, i);
            float a = size / 2.0f * 1.30f;
            float b = size * 1.50f;
            float d = 0;
            float s = 0;
            float si = 0;

            UIVertex v1 = new UIVertex();
            UIVertex v2 = new UIVertex();
            float linestart = -rectTransform.sizeDelta.x * rectTransform.pivot.x;

            // Vertex index tracker
            int vi = 0;
            char c = '\x0';
            for (; i < length && vi < count; ++i)
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
                            // Not a special character
                            i -= 1;
                    }
                    s = 0;
                    continue;
                }
                else if (c == ' ')
                {
                    s += size / 2.0f;
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

                vh.PopulateUIVertex(ref v1, vi * 4 + 0);
                vh.PopulateUIVertex(ref v2, vi * 4 + 2);

                d = v2.position.x - v1.position.x;
                if (d > b)
                    // Glyph size exceeds text size
                    // May be using <size> rich text tag
                    si = d;
                else if (uEmuera.Utils.CheckFullSize(c))
                    si = size;
                else if (uEmuera.Utils.CheckHalfSize(c))
                    si = size / 2.0f;
                else if (c == ' ')
                    si = size / 2.0f;
                else if (c == ZERO_WIDTH_SPACE)
                    si = 0;
                else
                    si = size;

                var o = s + (si - d) / 2.0f;
                v1.position.x = linestart + o;
                v2.position.x = v1.position.x + d;

                vh.SetUIVertex(v1, vi * 4 + 0);
                vh.SetUIVertex(v2, vi * 4 + 2);

                vh.PopulateUIVertex(ref v1, vi * 4 + 3);
                vh.PopulateUIVertex(ref v2, vi * 4 + 1);

                v1.position.x = linestart + o;
                v2.position.x = v1.position.x + d;

                vh.SetUIVertex(v1, vi * 4 + 3);
                vh.SetUIVertex(v2, vi * 4 + 1);
                vi += 1;

                s += si;
            }
        }

        /// <summary>
        /// Width size to use for character spacing calculation.
        /// If zero or less than font size, the font size will be used.
        /// </summary>
        public float widthsize = 0;

        /// <summary>
        /// Gets the RectTransform of this component.
        /// </summary>
        RectTransform rectTransform => transform as RectTransform;
        
        /// <summary>
        /// Gets the Text component attached to this GameObject.
        /// </summary>
        Text text
        {
            get
            {
                if (text_ == null)
                    text_ = GetComponent<Text>();
                return text_;
            }
        }
        Text text_ = null;
    }
}