using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace CToool
{
    public class CoroutineTool
    {
        public static IEnumerator WaitXSecAndDO(float x,Action action)
        {
            yield return new WaitForSeconds(x);
            action();
        }

        public static IEnumerator LoadTexture(RawImage rawImage, string downloadPath, Action<bool> onDownloaded)
        {
            var unityWebRequest = UnityWebRequestTexture.GetTexture(downloadPath);
            yield return unityWebRequest.SendWebRequest();
            if (unityWebRequest.isDone)
            {
                rawImage.texture = DownloadHandlerTexture.GetContent(unityWebRequest);
                onDownloaded(true);
            }else
            {
                Debug.LogError("无法下载图片:"+ downloadPath);
                onDownloaded(false);
            }
        }

        internal static IEnumerator DoitBeforeAfter(Action action1, Action action2)
        {
            action1();
            yield return null;
            action2();
        }
    }
}
