using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
namespace CToool
{

    public static class UIExpansion
    {
        public static void SetBtnIcon(this Button btn, string icon)
        {
            var image = btn.transform.Find("icon")?.GetComponent<Image>();
            if (image)
                image.sprite = Resources.Load<Sprite>($"UI/Icon2/{icon}@2x");
            else
                btn.GetComponent<Image>().sprite = Resources.Load<Sprite>($"UI/Icon2/{icon}@2x");
        }
        public static void SetNormalAndSelectColor(this Button b, Color color)
        {
            var cb = b.colors;
            cb.normalColor = color;
            cb.selectedColor = color;
            b.colors = cb;
        }
        public static void SetFullScreen(this RectTransform rts)
        {
            rts.anchorMin = Vector2.zero;
            rts.anchorMax = Vector2.one;
            var op = rts.parent;
            var sl = rts.GetSiblingIndex();
            rts.SetParent(rts.root);
            rts.offsetMax = Vector2.zero;
            rts.offsetMin = Vector2.zero;
            rts.SetParent(op);
            rts.SetSiblingIndex(sl);
        }


        public static void Addlistener(this ToggleGroup group, UnityAction<int> action)
        {
            var togs = group.ActiveToggles();
            int i = 0;
            foreach (var item in togs)
            {
                int index = i;
                item.onValueChanged.AddListener((isOn) =>
                {
                    if (isOn)
                        action(index);
                });
                i++;
            }
        }
        public static void SetTogOn(this ToggleGroup group, int index)
        {
            var togs = group.ActiveToggles();
            int i = 0;
            foreach (var item in togs)
            {
                if (i == index)
                {
                    item.isOn = true;
                    return;
                }
                i++;
            }
        }

    } 
}
