using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using MinorShift.Emuera;

/// <summary>
/// Represents a single line of text in the Emuera console.
/// Handles text rendering, styling, and user interaction for console output.
/// </summary>
public class EmueraLine : EmueraBehaviour
{
    /// <summary>
    /// Initializes the line component and sets up event handlers.
    /// </summary>
    void Awake()
    {
        GenericUtils.SetListenerOnClick(gameObject, OnClick);
        monospaced_ = GetComponent<UnityEngine.UI.TMPMonospaced>();
        click_handler_ = GetComponent<GenericUtils.PointerClickListener>();
    }

    /// <summary>
    /// Gets the TextMeshProUGUI component for text rendering.
    /// </summary>
    public TextMeshProUGUI text
    {
        get
        {
            if (text_ == null)
            {
                text_ = GetComponent<TextMeshProUGUI>();
                text_.maskable = false;
            }
            return text_;
        }
    }
    TextMeshProUGUI text_ = null;
    
    /// <summary>
    /// Gets the monospaced text modifier component.
    /// </summary>
    public UnityEngine.UI.TMPMonospaced monospaced
    {
        get
        {
            if(monospaced_ == null)
                monospaced_ = GetComponent<UnityEngine.UI.TMPMonospaced>();
            return monospaced_;
        }
    }
    UnityEngine.UI.TMPMonospaced monospaced_ = null;
    
    /// <summary>
    /// Gets the content size fitter component for automatic sizing.
    /// </summary>
    public UnityEngine.UI.ContentSizeFitter size_fitter
    {
        get
        {
            if(size_fitter_ == null)
            {
                size_fitter_ = GetComponent<UnityEngine.UI.ContentSizeFitter>();
                size_fitter_.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
            }
            return size_fitter_;
        }
    }
    UnityEngine.UI.ContentSizeFitter size_fitter_ = null;
    GenericUtils.PointerClickListener click_handler_ = null;

#if UNITY_STANDALONE
    /// <summary>
    /// Gets the button component for standalone platforms.
    /// </summary>
    public UnityEngine.UI.Button button
    {
        get
        {
            if (button_ == null)
            {
                button_ = GetComponent<UnityEngine.UI.Button>();
                if (button_ == null)
                    button_ = gameObject.AddComponent<UnityEngine.UI.Button>();
                var colors = button_.colors;
                colors.highlightedColor = Config.FocusColor.ToUnityColor();
                button_.colors = colors;
            }
            return button_;
        }
    }
    UnityEngine.UI.Button button_ = null;
#endif

    /// <summary>
    /// Updates the content of this line with the current unit description.
    /// </summary>
    public override void UpdateContent()
    {
        var ud = unit_desc;

        text.text = ud.content;
        text.color = ud.color;
        text.richText = ud.richedit;

        if(ud.isbutton && ud.generation >= EmueraContent.instance.button_generation)
        {
            click_handler_.enabled = true;
            text.raycastTarget = true;
#if UNITY_STANDALONE
            button.enabled = true;
#endif
#if UNITY_EDITOR
            code = ud.code;
            generation = ud.generation;
#endif
        }
        else
        {
            click_handler_.enabled = false;
            text.raycastTarget = false;
#if UNITY_STANDALONE
            button.enabled = false;
#endif
        }

        var font = FontUtils.default_tmp_font;
        if(ud.fontname != null)
            font = FontUtils.GetTMPFont(ud.fontname);
        if(text.font != font)
            text.font = font;

        if(monospaced_ != null)
            monospaced_.enabled = ud.monospaced;

        logic_y = line_desc.position_y;
        logic_height = line_desc.height;

        var sizefitter = false;
        if(ud.isbutton || line_desc.units.Count > 1)
            sizefitter = true;
        else
            sizefitter = false;
        if(sizefitter)
        {
            size_fitter.horizontalFit =
                UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
        }
        else
        {
            size_fitter.horizontalFit =
                UnityEngine.UI.ContentSizeFitter.FitMode.Unconstrained;
            text_.rectTransform.sizeDelta = new Vector2(Width, 0);
        }

        var fontSize = text.fontSize;
        if(unit_desc.underline)
        {
            if(underline_ == null)
            {
                var obj = GameObject.Instantiate(EmueraContent.instance.template_block.gameObject);
#if UNITY_EDITOR
                obj.name = "underline";
#endif
                underline_ = obj.GetComponent<RectTransform>();
                underline_.transform.SetParent(this.transform);
                underline_.anchorMin = new Vector2(0, 1);
                underline_.anchorMax = new Vector2(0, 1);
                underline_.localScale = Vector3.one;
                underline_.anchoredPosition = new Vector2(0, - fontSize - 1);
            }
            underline_.sizeDelta = new Vector2(Width, 1);
            underline_.GetComponent<UnityEngine.UI.Image>().color = unit_desc.color;
            underline_.gameObject.SetActive(true);
        }
        else if(underline_ != null)
            underline_.gameObject.SetActive(false);

        if(unit_desc.strickout)
        {
            if(strickout_ == null)
            {
                var obj = GameObject.Instantiate(EmueraContent.instance.template_block.gameObject);
#if UNITY_EDITOR
                obj.name = "strickout";
#endif
                strickout_ = obj.GetComponent<RectTransform>();
                strickout_.transform.SetParent(this.transform);
                strickout_.anchorMin = new Vector2(0, 1);
                strickout_.anchorMax = new Vector2(0, 1);
                strickout_.localScale = Vector3.one;
                strickout_.anchoredPosition = new Vector2(0, - fontSize / 2.0f);
            }
            strickout_.sizeDelta = new Vector2(Width, 1);
            strickout_.GetComponent<UnityEngine.UI.Image>().color = unit_desc.color;
            strickout_.gameObject.SetActive(true);
        }
        else if(strickout_ != null)
            strickout_.gameObject.SetActive(false);

#if UNITY_EDITOR
        gameObject.name = string.Format("line:{0}:{1}", LineNo, UnitIdx);
#endif
        gameObject.SetActive(true);
    }
    
    /// <summary>
    /// Clears the line content and hides decorations.
    /// </summary>
    public void Clear()
    {
        line_desc = null;
        UnitIdx = -1;
        text.text = "";
        if(underline_ != null)
            underline_.gameObject.SetActive(false);
        if(strickout_ != null)
            strickout_.gameObject.SetActive(false);
    }
    RectTransform underline_ = null;
    RectTransform strickout_ = null;
}
