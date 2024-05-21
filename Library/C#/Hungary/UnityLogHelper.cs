using System.Threading;
using System.IO;
using UnityEngine;
using System.Collections.Generic;
using System;

public class UnityLogHelper
{
    Queue<string> logList = new Queue<string>();

    Thread task;
    private string logPath;

    public UnityLogHelper()
    {
        Directory.CreateDirectory("./porary/Log");
        logPath = "./porary/Log/" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".log";
        Debug.Log(logPath);

        if (File.Exists(logPath))
        {
            File.Delete(logPath);
        }

        Application.logMessageReceived += LogReceived;
        Application.quitting += OnQuitting;

        task = new Thread(()=> {
            while (true)
            {
                if (logList.Count > 0)
                {
                    var list = new List<string>();
                    for (int i = 0; i < logList.Count; i++)
                    {
                        list.Add(logList.Dequeue());
                    }
                    File.AppendAllLines(logPath, list);
                }
                Thread.Sleep(1000);
            }
        });
        task.Start();
    }

    private void LogReceived(string condition, string stackTrace, LogType type)
    {
        logList.Enqueue($"{type}:{condition}{System.Environment.NewLine}{stackTrace}");
    }
    private void OnQuitting()
    {
        Debug.Log("OnQuitting");
        task.Abort();
        if (logList.Count > 0)
        {
            var list = new List<string>();
            for (int i = 0; i < logList.Count; i++)
            {
                list.Add(logList.Dequeue());
            }
            File.AppendAllLines(logPath, list);
        }
    }

}
