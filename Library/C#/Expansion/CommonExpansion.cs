using Modules.Communication;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace CToool
{
    public static class CommonExpansion
    {
        #region [Event]

        public static void ListenTo(this UnityEvent @event, UnityAction call)
        {
            @event.RemoveAllListeners();
            @event.AddListener(call);
        }
        public static void ListenTo<T>(this UnityEvent<T> @event, UnityAction<T> call)
        {
            @event.RemoveAllListeners();
            @event.AddListener(call);
        }

        #endregion

        #region [UI]

        public static void SetText(this Text text, object obj)
        {
            text.text = obj.ToString();
        }
        public static void SetColor(this Image image, float r = -1f, float g = -1f, float b = -1f, float a = -1f)
        {
            var c = image.color;
            if (r != -1f) c.r = r;
            if (g != -1f) c.g = g;
            if (b != -1f) c.b = b;
            if (a != -1f) c.a = a;
            image.color = c;
        }

        public static void LoadTextureAsyn(this RawImage rawImage, string downloadPath, Action<bool> OnDownloaded)
        {
            rawImage.StartCoroutine(CoroutineTool.LoadTexture(rawImage, downloadPath, OnDownloaded));
        }

        public static Vector3 GetMouseRectPostion(this PointerEventData eventData,RectTransform parent)
        {
            //存储当前鼠标所在位置
            Vector3 globalMousePos;
            //UI屏幕坐标转换为世界坐标
            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(parent, eventData.position, eventData.pressEventCamera, out globalMousePos)) 
            {
                return globalMousePos;
            }
            return Vector3.zero;
        }

        #endregion

        #region [Struct]

        /// <summary>
        /// 根据json更新结构体内的变量(会生成新的struct)
        /// </summary>
        public static T UpdateByJson<T>(this T t, JToken jToken) where T : struct
        {
            var fields = (typeof(T)).GetFields();
            var obj = t as object;
            foreach (var item in fields)
            {
                var value = jToken[item.Name];
                if (value != null)
                {
                    var v = value.ToObject(item.FieldType);
                    item.SetValue(obj, v);
                }
            }
            return (T)obj;
        }
        public static bool ByteArrayToStructure<T>(this byte[] bytearray, ref T data) where T : struct
        {
            if (bytearray == null || bytearray.Length <= 0)
                return false;

            var obj = Activator.CreateInstance(typeof(T));
            int len = Marshal.SizeOf(obj);

            IntPtr iptr = IntPtr.Zero;

            try
            {
                iptr = Marshal.AllocHGlobal(len);
                //clear memory
                for (int i = 0; i < len / 8; i++)
                {
                    Marshal.WriteInt64(iptr, i * 8, 0x00);
                }

                for (int i = len - (len % 8); i < len; i++)
                {
                    Marshal.WriteByte(iptr, i, 0x00);
                }

                // copy byte array to ptr
                Marshal.Copy(bytearray, 0, iptr, bytearray.Length);

                data = (T)Marshal.PtrToStructure(iptr, obj.GetType());
            }
            catch
            {
                Debug.LogError($"{typeof(T)}:转化失败");
            }
            finally
            {
                if (iptr != IntPtr.Zero)
                    Marshal.FreeHGlobal(iptr);
            }
            return true;
        }

        #endregion
        public static bool IsEquals(this byte[] a,byte[] b) 
        {
            if (a == null || b == null)
                return false;
            if (a.Length != b.Length)
                return false;
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i])
                    return false;
            }
            return true;
        }
    }
}