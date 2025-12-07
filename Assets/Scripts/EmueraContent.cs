using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using MinorShift.Emuera;
using MinorShift.Emuera.GameView;
using Unity.Mathematics;

/// <summary>
/// Main content display manager for the Emuera console.
/// Handles text and image rendering, scrolling, and user input.
/// Optimized with Unity.Mathematics for better performance.
/// </summary>
public class EmueraContent : MonoBehaviour
{
    /// <summary>
    /// Gets the singleton instance of EmueraContent.
    /// </summary>
    public static EmueraContent instance { get { return instance_; } }
    static EmueraContent instance_ = null;

    /// <summary>
    /// Default font name used for text rendering.
    /// </summary>
    [Tooltip("Default font name for text rendering")]
    public string default_fontname;
    
    /// <summary>
    /// Template text component for creating new text lines.
    /// </summary>
    [Tooltip("Template text component for creating new text lines")]
    public Text template_text;
    
    /// <summary>
    /// Template image component for block elements.
    /// </summary>
    [Tooltip("Template image component for block elements")]
    public Image template_block;
    
    /// <summary>
    /// Template RectTransform for image containers.
    /// </summary>
    [Tooltip("Template for image containers")]
    public RectTransform template_images;
    
    /// <summary>
    /// Container for displaying images.
    /// </summary>
    [Tooltip("Container for displaying images")]
    public RectTransform image_content;
    
    /// <summary>
    /// Container for displaying text content.
    /// </summary>
    [Tooltip("Container for displaying text content")]
    public RectTransform text_content;
    
    /// <summary>
    /// Container for cached images.
    /// </summary>
    [Tooltip("Container for cached images")]
    public RectTransform cache_images;
    
    /// <summary>
    /// Reference to the option window.
    /// </summary>
    [Tooltip("Reference to the option window")]
    public OptionWindow option_window;

    Camera main_camere;
    Image background;
    uEmuera.Drawing.Color background_color;

    public RectTransform rect_transform { get { return (RectTransform)transform; } }
    RectMask2D mask2d;

    void Awake()
    {
        FontUtils.SetDefaultFont(default_fontname);
        main_camere = Object.FindFirstObjectByType<Camera>();
    }

    void Start()
    {
        instance_ = this;
        background = GetComponent<Image>();
        mask2d = GetComponent<RectMask2D>();

        GenericUtils.SetListenerOnBeginDrag(gameObject, OnBeginDrag);
        GenericUtils.SetListenerOnDrag(gameObject, OnDrag);
        GenericUtils.SetListenerOnEndDrag(gameObject, OnEndDrag);
        GenericUtils.SetListenerOnClick(gameObject, OnClick);

        SetIntentBox(PlayerPrefs.GetInt("IntentBox_L", 0),
                    PlayerPrefs.GetInt("IntentBox_R", 0));
    }

    public void SetIntentBox(int left, int right)
    {
        if(left == 0 && right == 0)
            mask2d.enabled = false;
        else
            mask2d.enabled = true;
        rect_transform.anchoredPosition = new Vector2((left - right) / 2.0f, 0);
        rect_transform.sizeDelta = new Vector2(-right - left, 0);
    }

    int GetLineNoIndex(int lineno)
    {
        int high = end_index - 1;
        int low = begin_index;
        int mid = 0;
        int found = -1;

        while(low <= high)
        {
            mid = (low + high) / 2;
            int k = console_lines_[mid % max_log_count].LineNo;
            if(k > lineno)
                high = mid - 1;
            else if(k < lineno)
                low = mid + 1;
            else
            {
                found = mid;
                break;
            }
        }

        if(found < 0)
            return -1;
        return found;
    }

    int GetLineNoIndexForPosY(float y)
    {
        int high = end_index - 1;
        int low = begin_index;
        int mid = 0;
        while(low <= high)
        {
            mid = (low + high) / 2;
            var l = console_lines_[mid % max_log_count];
            float top = l.position_y;
            float bottom = l.position_y + l.height;
            if(y < top)
                high = mid - 1;
            else if(y > bottom)
                low = mid + 1;
            else
                return mid;
        }
        if(high <= begin_index)
            return begin_index;
        else if(low >= end_index - 1)
            return end_index - 1;
        return -1;
    }

    int GetPrevLineNoIndex(int index)
    {
        if(index > max_index || index < 0)
            return -1;

        var lineno = 0;
        var cindex = index;
        var zero = begin_index;

        if(!console_lines_[index % max_log_count].IsLogicalLine)
        {
            lineno = console_lines_[index % max_log_count].LineNo;
            for(; cindex >= zero; --cindex)
            {
                if(console_lines_[cindex % max_log_count].LineNo != lineno)
                    break;
            }
            return cindex + 1;
        }
        else
            return cindex;
    }

    int GetNextLineNoIndex(int index)
    {
        if(index > max_index || index < 0)
            return -1;

        var lineno = console_lines_[index % max_log_count].LineNo;
        var cindex = index;
        for(; cindex < max_index; ++cindex)
        {
            if(console_lines_[cindex % max_log_count].LineNo != lineno)
                break;
        }
        return cindex - 1;
    }

    public void Update()
    {
        if(!dirty_ && math.all(drag_delta == float2.zero))
            return;
        dirty_ = false;

        float display_width = DISPLAY_WIDTH;
        float display_height = DISPLAY_HEIGHT;

        if(math.any(drag_delta != float2.zero))
        {
            float t = math.length(drag_delta);
            drag_delta = MathUtilities.ApplyDragDamping(drag_delta, 300.0f, Time.deltaTime);
            
            local_position = GetLimitPosition(local_position + drag_delta,
                                            display_width, display_height);
            if((local_position.x <= display_width - content_width && drag_delta.x < 0) ||
                (local_position.x >= 0 && drag_delta.x > 0))
                drag_delta.x = 0;
            if((local_position.y >= content_height - display_height && drag_delta.y > 0) ||
                (local_position.y <= offset_height && drag_delta.y < 0))
                drag_delta.y = 0;
        }

        var pos = local_position + (drag_curr_position - drag_begin_position);
        pos = GetLimitPosition(pos, display_width, display_height);

        int remove_count = 0;
        int count = display_lines_.Count;
        int max_line_no = -1;
        int min_line_no = int.MaxValue;
        for(int i = 0; i < count - remove_count; ++i)
        {
            var line = display_lines_[i];
            if(line.logic_y > pos.y + display_height ||
                line.logic_y + line.logic_height < pos.y)
            {
                display_lines_[i] = display_lines_[count - remove_count - 1];
                PushLine(line);
                ++remove_count;
                --i;
            }
            else
            {
                line.SetPosition(pos.x + line.unit_desc.posx, pos.y - line.logic_y);
                max_line_no = System.Math.Max(line.LineNo, max_line_no);
                min_line_no = System.Math.Min(line.LineNo, min_line_no);
            }
        }
        if(remove_count > 0)
            display_lines_.RemoveRange(count - remove_count, remove_count);

        List<EmueraImage> image_removelist = null;
        var display_iter = display_images_.GetEnumerator();
        while(display_iter.MoveNext())
        {
            var image = display_iter.Current.Value;
            if(image.logic_y > pos.y + display_height ||
                image.logic_y + image.logic_height < pos.y)
            {
                if(image_removelist == null)
                    image_removelist = new List<EmueraImage>();
                image_removelist.Add(image);
            }
            else
                image.SetPosition(pos.x + image.unit_desc.posx, pos.y - image.logic_y);
        }
        if(image_removelist != null)
        {
            var listcount = image_removelist.Count;
            EmueraImage image = null;
            for(int i=0; i<listcount; ++i)
            {
                image = image_removelist[i];
                PushImageContainer(image);
                display_images_.Remove(image.LineNo * 1000 + image.UnitIdx);
            }
        }

        var index = GetLineNoIndex(min_line_no - 1);
        index = GetPrevLineNoIndex(index);
        if(index >= 0)
        {
            UpdateLine(pos, display_height, index, -1);
        }
        index = GetLineNoIndex(max_line_no + 1);
        index = GetNextLineNoIndex(index);
        if(index >= 0)
        {
            UpdateLine(pos, display_height, index, +1);
        }
        if(display_lines_.Count == 0 &&
            console_lines_ != null && console_lines_.Count > 0)
        {
            index = GetLineNoIndexForPosY(pos.y);
            UpdateLine(pos, display_height, index, -1);
            UpdateLine(pos, display_height, index + 1, +1);
        }
    }
    void UpdateLine(float2 local, float display_height, int index, int delta)
    {
        var zero = begin_index;
        while(zero <= index && index < end_index)
        {
            var l = console_lines_[index % max_log_count];
            if(l.position_y > local.y + display_height ||
                l.position_y + l.height < local.y)
                break;

            for(int li = 0; li < l.units.Count; ++li)
            {
                var unit = l.units[li];
                if(!unit.empty)
                {
                    var lc = PullLine();
                    lc.line_desc = l;
                    lc.UnitIdx = li;
                    lc.Width = unit.width;
                    lc.UpdateContent();
                    lc.SetPosition(unit.posx + local.x, local.y - lc.logic_y);
                    display_lines_.Add(lc);
                }
                if(unit.image_indices != null && unit.image_indices.Count > 0)
                {
                    var hash = l.LineNo * 1000 + li;
                    EmueraImage ic = null;
                    if(!display_images_.TryGetValue(hash, out ic))
                    {
                        ic = PullImageContainer();
                        display_images_.Add(l.LineNo * 1000 + li, ic);
                    }
                    else
                        ic.Clear();
                    ic.line_desc = l;
                    ic.UnitIdx = li;
                    ic.Width = unit.width;
                    ic.UpdateContent();
                    ic.SetPosition(unit.posx + local.x, local.y - ic.logic_y);
                }
            }
            index += delta;
        }
    }
    /// <summary>
    /// Limits the scroll position within valid bounds.
    /// Optimized with Unity.Mathematics for better performance.
    /// </summary>
    float2 GetLimitPosition(float2 local,
        float display_width, float display_height)
    {
        float2 displaySize = new float2(display_width, display_height);
        float2 contentSize = new float2(content_width, content_height);
        
        return MathUtilities.LimitPosition(local, displaySize, contentSize, offset_height);
    }
    /// <summary>
    /// Marks the content as needing update.
    /// </summary>
    public void SetDirty()
    {
        SetDirtyInternal(true);
    }
    
    /// <summary>
    /// Internal method to set dirty flag and optionally notify the render manager.
    /// </summary>
    /// <param name="value">The dirty state value.</param>
    private void SetDirtyInternal(bool value)
    {
        dirty_ = value;
        
        // Notify on-demand rendering manager when content becomes dirty
        if (value && OnDemandRenderManager.instance != null)
        {
            OnDemandRenderManager.instance.SetContentDirty();
        }
    }

    bool dirty_ = false;
    uint last_click_tic = 0;
    void OnBeginDrag(PointerEventData e)
    {
        drag_begin_position = new float2(e.position.x, e.position.y);
        drag_curr_position = drag_begin_position;
        drag_delta = float2.zero;
    }
    void OnDrag(PointerEventData e)
    {
        SetDirtyInternal(true);
        drag_curr_position = new float2(e.position.x, e.position.y);
        drag_delta = float2.zero;
    }
    void OnEndDrag(PointerEventData e)
    {
        SetDirtyInternal(true);
        float display_width = DISPLAY_WIDTH;
        float display_height = DISPLAY_HEIGHT;
        
        float2 endPos = new float2(e.position.x, e.position.y);
        local_position = GetLimitPosition(
            local_position + (endPos - drag_begin_position),
            display_width, display_height);

        drag_delta = endPos - drag_curr_position;
        drag_begin_position = float2.zero;
        drag_curr_position = float2.zero;
    }
    void OnClick()
    {
        var nowtick = MinorShift._Library.WinmmTimer.TickCount;
        var skipflag = (nowtick - last_click_tic < 200);
        EmueraThread.instance.Input("", false, skipflag);
        last_click_tic = nowtick;
    }
    float2 drag_begin_position = float2.zero;
    float2 drag_curr_position = float2.zero;
    float2 drag_delta = float2.zero;

    public void SetBackgroundColor(uEmuera.Drawing.Color color)
    {
        if(background_color == color)
            return;
        background.color = GenericUtils.ToUnityColor(color);
        background_color = color;
        main_camere.backgroundColor = background.color;
        
        // Notify on-demand rendering manager of visual change
        if (OnDemandRenderManager.instance != null)
        {
            OnDemandRenderManager.instance.SetContentDirty();
        }
    }
    public void Ready()
    {
        EmueraBehaviour.Ready();
        option_window.Ready();
        option_window.gameObject.SetActive(true);

        background.color = GenericUtils.ToUnityColor(Config.BackColor);
        background_color = Config.BackColor;
        content_width = Config.WindowX;

        FontUtils.SetDefaultFont(Config.FontName);

        template_text.color = EmueraBehaviour.FontColor;
        template_text.font = FontUtils.default_font;
        if(template_text.font == null)
            template_text.font = FontUtils.default_font;
        template_text.fontSize = EmueraBehaviour.FontSize;
        template_text.rectTransform.sizeDelta =
            new Vector2(template_text.rectTransform.sizeDelta.x, 0);
        template_text.gameObject.SetActive(false);

        console_lines_ = new List<EmueraBehaviour.LineDesc>(max_log_count);
        while(console_lines_.Count < max_log_count)
            console_lines_.Add(null);
        invalid_count = max_log_count;
    }
    public void SetNoReady() { ready_ = false; }
    bool ready_ = false;

    public void Clear()
    {
        invalid_count = max_log_count;
        max_index = 0;
        for(int i = 0; i < console_lines_.Count; ++i)
            console_lines_[i] = null;

        for(int i = 0; i < display_lines_.Count; ++i)
        {
            PushLine(display_lines_[i]);
        }

        display_lines_.Clear();

        var iter = cache_lines_.GetEnumerator();
        while(iter.MoveNext())
            GameObject.Destroy(iter.Current.gameObject);
        var iter2 = cache_images_.GetEnumerator();
        while(iter2.MoveNext())
            GameObject.Destroy(iter2.Current.gameObject);

        cache_lines_.Clear();
        cache_images_.Clear();

        content_height = 0;
        offset_height = 0;
        local_position = float2.zero;
        drag_delta = float2.zero;
        SetDirtyInternal(true);
    }
    /// <summary>
    /// Adds a new line to the console display.
    /// </summary>
    /// <param name="line">The line object to add.</param>
    /// <param name="roll_to_bottom">Whether to scroll to bottom after adding.</param>
    public void AddLine(object line, bool roll_to_bottom = false)
    {
        if(line == null)
            return;
        if(!ready_)
        {
            Ready();
            ready_ = true;
        }
        var ld = new EmueraBehaviour.LineDesc(line, content_height, Config.LineHeight);

        console_lines_[max_index % max_log_count] = ld;
        if(invalid_count > 0)
            invalid_count -= 1;
        // Add offset height when log is full
        if(valid_count >= max_log_count)
            offset_height += Config.LineHeight;
        max_index += 1;

        ld.Update();
        
        // Add content height
        content_height += Config.LineHeight;
        if(roll_to_bottom)
        {
            local_position.y = content_height - rect_transform.rect.height;
            drag_delta.y = 0;
        }
        else
        {
            drag_delta.y += Config.LineHeight * 1.5f;
        }
        SetDirtyInternal(true);
    }
    /// <summary>
    /// Gets a line by index.
    /// </summary>
    /// <param name="index">The line index.</param>
    /// <returns>The line object or null if not found.</returns>
    public object GetLine(int index)
    {
        if(index < begin_index || index >= end_index)
            return null;
        return console_lines_[index % max_log_count].console_line;
    }
    /// <summary>
    /// Gets the total count of lines.
    /// </summary>
    public int GetLineCount()
    {
        return valid_count;
    }
    /// <summary>
    /// Gets the minimum line number.
    /// </summary>
    public int GetMinLineNo()
    {
        return begin_index;
    }
    /// <summary>
    /// Gets the maximum line number.
    /// </summary>
    public int GetMaxLineNo()
    {
        if(valid_count == 0)
            return -1;
        return max_index;
    }
    /// <summary>
    /// Removes lines from the display.
    /// </summary>
    /// <param name="count">Number of lines to remove.</param>
    public void RemoveLine(int count)
    {
        if(!ready_)
        {
            Ready();
            ready_ = true;
        }
        if(count > valid_count)
            count = valid_count;
        if(count == 0)
            return;

        var lineno = console_lines_[(max_index - count) % max_log_count].LineNo;
        display_lines_.Sort((l, r) =>
        {
            return (l.LineNo * 10 + l.UnitIdx) -
                (r.LineNo * 10 + r.UnitIdx);
        });

        var i = 0;
        for(; i < display_lines_.Count; ++i)
        {
            if(display_lines_[i].LineNo >= lineno)
                break;
        }
        List<int> imageremove = new List<int>();

        var iter = display_images_.GetEnumerator();
        while(iter.MoveNext())
        {
            var image = iter.Current;
            if(image.Key / 1000 >= lineno)
            {
                PushImageContainer(image.Value);
                imageremove.Add(image.Key);
            }
        }
        var remove = imageremove.Count;
        for(var j=0; j<remove; ++j)
        {
            display_images_.Remove(imageremove[j]);
        }

        remove = 0;
        for(; i < display_lines_.Count; ++i, ++remove)
        {
            PushLine(display_lines_[i]);
        }
        if(remove > 0)
        {
            if(remove >= display_lines_.Count)
                display_lines_.Clear();
            else
                display_lines_.RemoveRange(display_lines_.Count - remove, remove);
        }

        var eidx = end_index - 1;
        var bidx = end_index - count ;
        for(i = eidx; i >= bidx; --i)
            console_lines_[i % max_log_count] = null;

        content_height -= Config.LineHeight * count;
        if(count >= valid_count)
        {
            offset_height = 0;
            content_height = 0;
        }
        else if(max_index > max_log_count)
        {
            if(max_index - count <= max_log_count)
            {
                //offset_height -= Config.LineHeight * (max_index - max_log_count);
                //offset_height = Config.LineHeight * (max_index - max_log_count);
                var l = console_lines_[max_index - count - 1];
                offset_height = l.position_y + l.height;
            }
        }

        invalid_count += count;
        max_index -= count;
        SetDirtyInternal(true);
    }
    public void ToBottom()
    {
        local_position.y = content_height - rect_transform.rect.height;
        drag_delta = float2.zero;
        SetDirtyInternal(true);
        Update();
    }
    public void ShowIsInProcess(bool value)
    {
        option_window.inprogress.SetActive(value);
    }
    public void SetLastButtonGeneration(int generation)
    {
        last_button_generation = generation;

        var quick_buttons = option_window.quick_buttons;
        if(quick_buttons.IsShow)
        {
            quick_buttons.Clear();
            if(last_button_generation < 0)
                return;

            for(int i = end_index - 1; i >= begin_index; --i)
            {
                var cl = console_lines_[i%max_log_count];
                if(cl.units == null)
                    continue;
                var bl = 0;
                for(int j = 0; j < cl.units.Count; ++j)
                {
                    var cu = cl.units[j];
                    if(cu.isbutton)
                    {
                        if(cu.generation != last_button_generation)
                            return;
                        if(cu.empty)
                            continue;
                        quick_buttons.AddButton(cu.content, cu.color, cu.code);
                        bl += 1;
                    }
                }
                if(bl > 0)
                    quick_buttons.ShiftLine();    
            }
        }
    }

    public int button_generation { get { return last_button_generation; } }
#if UNITY_EDITOR
    public
#endif
    int last_button_generation = 0;
    int max_index = 0;
    int invalid_count = 0;
    int begin_index
    {
        get
        {
            return System.Math.Max(0, max_index - valid_count);
        }
    }
    int end_index
    {
        get
        {
            return max_index;
        }
    }
    int valid_count
    {
        get
        {
            return max_log_count - invalid_count;
        }
    }
    /// <summary>
    /// Maximum number of log lines to keep.
    /// </summary>
    public int max_log_count { get { return MinorShift.Emuera.Config.MaxLog; } }
    List<EmueraBehaviour.LineDesc> console_lines_;

    //RectTransform parent
    //{
    //    get
    //    {
    //        if(parent_ == null)
    //        {
    //            parent_ = transform.parent as RectTransform;
    //            while(parent_.parent != null)
    //            {
    //                parent_ = parent_.parent as RectTransform; ;
    //            }
    //        }
    //        return parent_;
    //    }
    //}
    //RectTransform parent_;
    //float DISPLAY_WIDTH { get { return parent.sizeDelta.x; } }
    //float DISPLAY_HEIGHT { get { return parent.sizeDelta.y; } }
    float DISPLAY_WIDTH { get { return rect_transform.rect.width; } }
    float DISPLAY_HEIGHT { get { return rect_transform.rect.height; } }

    /// <summary>
    /// Offset height for scrolling.
    /// </summary>
    float offset_height = 0;
    /// <summary>
    /// Content width.
    /// </summary>
    float content_width = 0;
    /// <summary>
    /// Content height.
    /// </summary>
    float content_height = 0;
    /// <summary>
    /// Current scroll position.
    /// </summary>
    float2 local_position = float2.zero;

    List<EmueraLine> display_lines_ = new List<EmueraLine>();
    Dictionary<int, EmueraImage> display_images_ = new Dictionary<int, EmueraImage>();
    /// <summary>
    /// Gets a text display control from the pool.
    /// </summary>
    /// <returns>An EmueraLine instance.</returns>
    EmueraLine PullLine()
    {
        EmueraLine line = null;
        if(cache_lines_.Count > 0)
            line = cache_lines_.Dequeue();
        else
        {
            var obj = GameObject.Instantiate(template_text.gameObject);
            line = obj.GetComponent<EmueraLine>();
            line.transform.SetParent(text_content);
            line.transform.localScale = Vector3.one;
            line.gameObject.SetActive(true);
        }
        //line.size_fitter.enabled = true;
        //line.monospaced.enabled = true;
        //line.gameObject.SetActive(true);  
        return line;
    }
    /// <summary>
    /// Returns a text display control to the pool.
    /// </summary>
    /// <param name="line">The line to return.</param>
    void PushLine(EmueraLine line)
    {
        line.Clear();
        //line.gameObject.SetActive(false);

        //line.size_fitter.enabled = false;
        //line.monospaced.enabled = false;
        //line.text.text = string.Empty;
        //line.rect_transform.sizeDelta = Vector2.zero;
        line.rect_transform.position = new Vector3(-10000, 0, 0);

#if UNITY_EDITOR
        line.gameObject.name = "unused";
#endif
        cache_lines_.Enqueue(line);
    }
    Queue<EmueraLine> cache_lines_ = new Queue<EmueraLine>();

    /// <summary>
    /// Gets an image display control from the pool.
    /// </summary>
    /// <returns>An EmueraImage instance.</returns>
    EmueraImage PullImageContainer()
    {
        EmueraImage image = null;
        if(cache_image_containers_.Count > 0)
            image = cache_image_containers_.Pop();
        else
        {
            var obj = GameObject.Instantiate(template_images.gameObject);
            image = obj.GetComponent<EmueraImage>(); 
        }
        image.transform.SetParent(image_content);
        image.transform.localScale = Vector3.one;
        image.gameObject.SetActive(true);
        return image;
    }
    /// <summary>
    /// Returns an image display control to the pool.
    /// </summary>
    /// <param name="image">The image to return.</param>
    void PushImageContainer(EmueraImage image)
    {
        image.Clear();
        image.gameObject.SetActive(false);
#if UNITY_EDITOR
        image.gameObject.name = "unused";
#endif
        image.transform.SetParent(cache_images);
        cache_image_containers_.Push(image);
    }
    Stack<EmueraImage> cache_image_containers_ = new Stack<EmueraImage>();

    /// <summary>
    /// Gets an image from the pool.
    /// </summary>
    /// <returns>An Image component.</returns>
    public Image PullImage()
    {
        Image image = null;
        if(cache_images_.Count > 0)
            image = cache_images_.Pop();
        else
        {
            var obj = new GameObject();
            image = obj.AddComponent<Image>();
            image.transform.SetParent(cache_images);
            var rt = image.transform as RectTransform;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.localScale = Vector3.one;
        }
        image.gameObject.SetActive(true);
        return image;
    }
    /// <summary>
    /// Returns an image to the pool.
    /// </summary>
    /// <param name="image">The image to return.</param>
    public void PushImage(Image image)
    {
        image.gameObject.SetActive(false);
#if UNITY_EDITOR
        image.gameObject.name = "unused";
#endif
        image.sprite = null;
        image.transform.SetParent(cache_images);
        cache_images_.Push(image);
    }
    Stack<Image> cache_images_ = new Stack<Image>();
}
