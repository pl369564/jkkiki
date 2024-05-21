using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

public class ShortCutMgr:Singleton<ShortCutMgr>
{
    private readonly Dictionary<int, Dictionary<KeyCode[], Action>> _DICT = new Dictionary<int, Dictionary<KeyCode[], Action>>();
    private readonly List<int> _idList = new List<int>();

    private Dictionary<KeyCode[], Action> _eventDict;

    internal void Update()
    {
        OnUpdate(_eventDict);
    }

    private void OnUpdate(Dictionary<KeyCode[], Action> eventDict)
    {
        if (!Input.anyKeyDown)
            return;
        if (eventDict == null)
            return;

        foreach (var item in eventDict)
        {
            var mainkey = item.Key[0];
            if (Input.GetKeyDown(mainkey))
            {
                bool isHold = true;
                if (item.Key.Length > 1)
                {
                    for (int i = 1; i < item.Key.Length; i++)
                    {
                        var subkey = item.Key[i];
                        if (!Input.GetKey(subkey))
                            isHold = false;
                    }
                }
                if (isHold)
                {
                    item.Value();
                    return;
                }
            }
        }
    }

    private void MyAddListener(MonoBehaviour mono, Action action, KeyCode[] keys)
    {
        var id = mono.GetInstanceID();
        if (!_DICT.TryGetValue(id, out _eventDict))
        {
            Debug.Log($"StartListen at {mono.name}");
            SetCurMono(id);
        }
        else 
        {
            var last = _idList.Last();
            if (last != id)
            {
                _idList.Remove(id);
                _idList.Add(id);
            }
        }
        //防止重复监听
        if (_eventDict.ContainsKey(keys))
            return;
        _eventDict.Add(keys, action);
    }

    private void MyStopListener(MonoBehaviour mono)
    {
        var id = mono.GetInstanceID();
        if (!_DICT.ContainsKey(id))
            return;
        Debug.Log($"StopListener at {mono.name}");
        var last = _idList.Last();
        _idList.Remove(id);
        if (last == id)
        {
            if (_idList.Count == 0)
            {
                _eventDict = null;
            }
            else
            {
                last = _idList.Last();
                _eventDict = _DICT[last];
            }
        }
        _DICT.Remove(id);
    }

    private void MyBlockListener(MonoBehaviour mono)
    {
        var id = mono.GetInstanceID();
        if (_DICT.ContainsKey(id))
        {
            Debug.LogError("不能阻止已有的监听");
            return;
        }
        _eventDict = null;
        _DICT.Add(id, _eventDict);
        _idList.Add(id);
    }

    private void SetCurMono(int monoId)
    {
        _idList.Add(monoId);
        _eventDict = new Dictionary<KeyCode[], Action>();
        _DICT.Add(monoId, _eventDict);
    }



    /// <summary>
    /// 添加按键监听,第一个按键以后的是组合辅助键
    /// </summary>
    public static void AddListener(MonoBehaviour mono,Action action,params KeyCode[] keys)
    {
        Instance.MyAddListener(mono,action,keys);
    }

    /// <summary>
    /// 阻止其他快捷键监听
    /// </summary>
    /// <param name="mono"></param>
    public static void BlockListener(MonoBehaviour mono)
    {
        Instance.MyBlockListener(mono);
    }

    /// <summary>
    /// 停止监听,恢复上次物体快捷键监听
    /// </summary>
    /// <param name="mono"></param>
    public static void StopListener(MonoBehaviour mono)
    {
        Instance.MyStopListener(mono);
    }

}
