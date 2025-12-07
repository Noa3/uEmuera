using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MinorShift.Emuera.GameView;
using MinorShift.Emuera.Content;

public class EmueraImage : EmueraBehaviour
{
    class ImageInfo : MonoBehaviour
    {
        static void OnLoadImageCallback(object obj, SpriteManager.SpriteInfo spriteinfo)
        {
            if(obj == null)
            {
                SpriteManager.GivebackSpriteInfo(spriteinfo);
                return;
            }
            var c = obj as ImageInfo;
            if(!c.gameObject.activeSelf)
            {
                SpriteManager.GivebackSpriteInfo(spriteinfo);
                return;
            }
            c.SetSprite(spriteinfo);
        }
        public void Load(ASprite src)
        {
            SpriteManager.GetSprite(src, this, ImageInfo.OnLoadImageCallback);
        }
        void SetSprite(SpriteManager.SpriteInfo spriteinfo)
        {
            if(spriteinfo == null)
            {
                image.sprite = null;
                image.color = kTransparent;
            }
            else
            {
                image.sprite = spriteinfo.sprite;
                image.color = Color.white;
            }
            this.spriteinfo = spriteinfo;
        }
        public void Clear()
        {
            SpriteManager.GivebackSpriteInfo(spriteinfo);
            EmueraContent.instance.PushImage(image);
        }
        
        public SpriteManager.SpriteInfo spriteinfo = null;
        UnityEngine.UI.Image image
        {
            get
            {
                if(image_ == null)
                    image_ = GetComponent<UnityEngine.UI.Image>();
                return image_;
            }
        }
        UnityEngine.UI.Image image_ = null;
    }

    static readonly Color kTransparent = new Color(0, 0, 0, 0);
    GenericUtils.PointerClickListener click_handler_ = null;

    void Awake()
    {
        GenericUtils.SetListenerOnClick(gameObject, OnClick);
        click_handler_ = GetComponent<GenericUtils.PointerClickListener>();
    }

    public override void UpdateContent()
    {
        var ud = unit_desc;
        var image_indices = ud.image_indices;
        var ld = line_desc;
        var consoleline = ld.console_line as ConsoleDisplayLine;
        var cb = consoleline.Buttons[UnitIdx];
        
        if(ud.isbutton && ud.generation >= EmueraContent.instance.button_generation)
        {
            image.enabled = true;
            click_handler_.enabled = true;
#if UNITY_EDITOR
            code = ud.code;
            generation = ud.generation;
#endif
        }
        else
        {
            image.enabled = false;
            click_handler_.enabled = false;
        }

        int miny = int.MaxValue;
        int maxy = int.MinValue;
        for(int i = 0; i < image_indices.Count; ++i)
        {
            var str_index = image_indices[i];
            var image_part = cb.StrArray[str_index] as ConsoleImagePart;
            miny = System.Math.Min(miny, image_part.Top);
            maxy = System.Math.Max(maxy, image_part.Bottom);
        }
        logic_y = line_desc.position_y + miny;
        logic_height = maxy - miny;

        var prt = rect_transform;
        int width = 0;
        for(int i = 0; i < image_indices.Count; ++i)
        {
            var image = EmueraContent.instance.PullImage();
            var imageinfo = image.gameObject.GetComponent<ImageInfo>();
            if(imageinfo == null)
                imageinfo = image.gameObject.AddComponent<ImageInfo>();
            image.raycastTarget = false;

            var rt = image.gameObject.transform as RectTransform;
            rt.SetParent(prt);

            var str_index = image_indices[i];
            var image_part = cb.StrArray[str_index] as ConsoleImagePart;
#if UNITY_EDITOR
            image.name = image_part.Image.Name;
#endif
            imageinfo.Load(image_part.Image);
            image_infos_.Add(imageinfo);

            var image_rect = image_part.dest_rect;
            
            // Calculate scaled sprite offset based on display size vs source size
            // Matches the original GDI drawing logic where offsets are scaled proportionally
            int sprite_x_offset = 0;
            int sprite_y_offset = 0;
            if (image_part.Image != null && !image_part.Image.DestBasePosition.IsEmpty)
            {
                var sprite = image_part.Image;
                var src_rect = sprite.Rectangle;
                
                // Scale the offset by the ratio of destination size to source size
                // This ensures proper layering when sprites are scaled
                if (src_rect.Width > 0)
                    sprite_x_offset = sprite.DestBasePosition.X * image_rect.Width / src_rect.Width;
                if (src_rect.Height > 0)
                    sprite_y_offset = sprite.DestBasePosition.Y * image_rect.Height / src_rect.Height;
            }
            
            // Position calculation:
            // - X: horizontal position from button start + scaled sprite offset
            // - Y: In console coordinates (Y down), image_part.Top is the ypos offset from baseline
            //   - miny is the minimum Top value, used as the container's top edge
            //   - Position relative to container top: (image_part.Top - miny)
            //   - Unity uses bottom-up Y, and anchors are at bottom-left by default
            //   - Container height is (maxy - miny)
            //   - Y position from bottom = container_height - (Top - miny) - image_height - scaled_sprite_y_offset
            //   - Simplified: maxy - image_part.Top - image_rect.Height - sprite_y_offset
            rt.anchoredPosition = new Vector2(
                image_part.PointX - ud.posx + sprite_x_offset, 
                maxy - image_part.Top - image_rect.Height - sprite_y_offset);
            rt.sizeDelta = new Vector2(image_rect.Width, image_rect.Height);
            rt.localScale = Vector3.one;

            width = Mathf.Max(image_part.PointX - ud.posx + sprite_x_offset + image_rect.Width, width);
        }

        prt.sizeDelta = new Vector2(width, logic_height);
#if UNITY_EDITOR
        name = string.Format("image:{0}:{1}", LineNo, UnitIdx);
#endif
    }
    public void Clear()
    {
        var count = image_infos_.Count;
        for(var i=0; i<count; ++i)
        {
            var image = image_infos_[i];
            image.Clear();
        }
        image_infos_.Clear();
    }

    UnityEngine.UI.Image image
    {
        get
        {
            if(image_ == null)
                image_ = GetComponent<UnityEngine.UI.Image>();
            return image_;
        }
    }
    UnityEngine.UI.Image image_ = null;
    List<ImageInfo> image_infos_ = new List<ImageInfo>();
}
