using UnityEngine;
using System;
using System.IO;
using System.Diagnostics;



public class AppLauncher : MonoBehaviour
{
    Process process = null;
    StreamWriter messageStream;

    public DracoCurl DracoCurl;
    //public UnityEngine.Events.UnityEvent<string> onDataReceived;
    //public DracoQUIC dracoQUIC;
    public void StartProcess(string AppName, string AppArgs)
    {
        try
        {
            process = new Process();
            process.EnableRaisingEvents = false;
            process.StartInfo.FileName = Application.dataPath + "/../Executables/" + AppName;
            process.StartInfo.Arguments = AppArgs;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;
            process.OutputDataReceived += new DataReceivedEventHandler(DataReceived);
            process.ErrorDataReceived += new DataReceivedEventHandler(ErrorReceived);
            process.Start();
            process.BeginOutputReadLine();
            messageStream = process.StandardInput;

            //UnityEngine.Debug.Log("Successfully launched app");
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError("Unable to launch app: " + e.Message);
        }
    }


    void DataReceived(object sender, DataReceivedEventArgs eventArgs)
    {
        // Handle it
        //UnityEngine.Debug.Log(eventArgs.Data);
        //onDataReceived?.Invoke(eventArgs.Data);
        DracoCurl.AdvanceBatch();
    }


    void ErrorReceived(object sender, DataReceivedEventArgs eventArgs)
    {
        UnityEngine.Debug.LogError(eventArgs.Data);
    }

    public void KillProcess()
    {
        if (process != null || !process.HasExited)
        {
            process.Kill();
        }
    }

    private void OnDestroy()
    {
        KillProcess();
    }
}


