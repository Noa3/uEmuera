using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using MinorShift.Emuera;
using MinorShift.Emuera.GameView;
using MinorShift.Emuera.Content;

/// <summary>
/// Renders CBG (Client Background Graphics) entries from EmueraConsole as Unity UI Images.
/// Entries with zdepth &lt; 0 are placed behind text content; zdepth &gt; 0 in front.
/// Assign behindLayer and frontLayer RectTransforms as siblings of the text content container
/// at the appropriate canvas sort order.
/// Call Refresh() each time the display is updated.
/// </summary>
public class EmuleraCbgRenderer : MonoBehaviour
{
    /// <summary>Container for CBG images rendered BEHIND text (zdepth &lt; 0).</summary>
    [Tooltip("RectTransform container drawn behind text content")]
    public RectTransform behindLayer;

    /// <summary>Container for CBG images rendered IN FRONT of text (zdepth &gt; 0).</summary>
    [Tooltip("RectTransform container drawn in front of text content")]
    public RectTransform frontLayer;

    // ---- Internal sprite handle ------------------------------------------------

    sealed class CbgImageHandle
    {
        public GameObject go;
        public Image image;
        public ASprite normalSprite;
        public ASprite hoverSprite;
        public int buttonValue = -1;
        public SpriteManager.SpriteInfo spriteInfo;

        public void GiveBackSprite()
        {
            if (spriteInfo != null)
            {
                SpriteManager.GivebackSpriteInfo(spriteInfo);
                spriteInfo = null;
            }
        }
    }

    // Callback used for async sprite loads
    static void OnSpriteLoaded(object obj, SpriteManager.SpriteInfo info)
    {
        var handle = obj as CbgImageHandle;
        if (handle == null || handle.go == null)
        {
            // GameObject was destroyed before callback arrived
            SpriteManager.GivebackSpriteInfo(info);
            return;
        }
        handle.GiveBackSprite();
        handle.spriteInfo = info;
        if (handle.image != null && info != null)
        {
            handle.image.sprite = info.sprite;
            handle.image.color = Color.white;
        }
    }

    // ---- Pools -----------------------------------------------------------------

    readonly List<CbgImageHandle> activeHandles_ = new List<CbgImageHandle>();
    readonly List<GameObject> imagePool_          = new List<GameObject>();

    // ---- Public API ------------------------------------------------------------

    /// <summary>
    /// Rebuilds the entire CBG visual from the current console state.
    /// Cheap pool-backed: existing GameObjects are reused across calls.
    /// Safe to call every frame / every display refresh.
    /// </summary>
    public void Refresh()
    {
        ClearActive();

        var console = GlobalStatic.Console;
        if (console == null) return;

        var entries = console.GetCbgSnapshot();
        if (entries == null || entries.Count == 0) return;

        foreach (var entry in entries)
        {
            if (entry.Image == null || !entry.Image.IsCreated) continue;

            RectTransform parent = entry.ZDepth < 0 ? behindLayer : frontLayer;
            if (parent == null) continue;

            var handle = CreateImageHandle(parent, entry);
            activeHandles_.Add(handle);
        }
    }

    // ---- Lifecycle -------------------------------------------------------------

    void OnDestroy()
    {
        ClearActive();
        foreach (var go in imagePool_)
        {
            if (go != null) Destroy(go);
        }
        imagePool_.Clear();
    }

    // ---- Helpers ---------------------------------------------------------------

    CbgImageHandle CreateImageHandle(RectTransform parent, EmueraConsole.CbgEntry entry)
    {
        var go = GetPooledGO();
        go.transform.SetParent(parent, false);

        var img = go.GetComponent<Image>();
        img.sprite = null;
        img.color = new Color(0, 0, 0, 0); // transparent until sprite arrives

        var rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot     = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(entry.X, -entry.Y);
        rt.sizeDelta = new Vector2(entry.Image.DestBaseSize.Width,
                                   entry.Image.DestBaseSize.Height);

        var handle = new CbgImageHandle
        {
            go           = go,
            image        = img,
            normalSprite = entry.Image,
            hoverSprite  = entry.ImageB,
            buttonValue  = entry.IsButton ? entry.ButtonValue : -1,
        };

        if (entry.IsButton)
        {
            img.raycastTarget = true;
            int val = entry.ButtonValue; // capture for closures

            // Hover: swap to hoverSprite on enter, back on exit
            GenericUtils.SetListenerOnPointerEnter(go, _ =>
            {
                if (handle.hoverSprite != null)
                    SpriteManager.GetSprite(handle.hoverSprite, handle, OnSpriteLoaded);
            });
            GenericUtils.SetListenerOnPointerExit(go, _ =>
            {
                if (handle.normalSprite != null)
                    SpriteManager.GetSprite(handle.normalSprite, handle, OnSpriteLoaded);
            });

            // Click: send button value as integer input
            GenericUtils.SetListenerOnClick(go, () =>
            {
                var console = GlobalStatic.Console;
                if (console != null)
                    console.OnCBGButtonClick(val);
            });
        }
        else
        {
            img.raycastTarget = false;
        }

        SpriteManager.GetSprite(entry.Image, handle, OnSpriteLoaded);
        return handle;
    }

    void ClearActive()
    {
        foreach (var h in activeHandles_)
        {
            h.GiveBackSprite();
            if (h.go != null)
            {
                h.go.SetActive(false);
                h.go.transform.SetParent(null, false);
                imagePool_.Add(h.go);
            }
        }
        activeHandles_.Clear();
    }

    GameObject GetPooledGO()
    {
        for (int i = imagePool_.Count - 1; i >= 0; i--)
        {
            var go = imagePool_[i];
            imagePool_.RemoveAt(i);
            if (go != null)
            {
                go.SetActive(true);
                return go;
            }
        }
        // Allocate new
        var newGo = new GameObject("cbg_img", typeof(RectTransform), typeof(Image));
        return newGo;
    }
}
