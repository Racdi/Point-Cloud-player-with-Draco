using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System.IO;
using System;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine.Networking;
using UnityEngine.Events;
using PCP;
using PCP.Utils;
using Draco;

public class DracoWebRequest : MonoBehaviour
{
    [HideInInspector]
    public UnityEvent OnObsoleteDataReceived = new UnityEvent();

    public enum DataReadModes { Remote, Local, StreamingAssets }
    public DataReadModes ReadMode;

    /// <summary>
    /// Path to a remote http server directory (i.e. http://localhost:8000/Recording_1/ , served by 'python -m http.server')
    /// </summary>
    public string RemoteHostPath;
    public string ipHostPath;
    private string _http = "http://";
    private string _port0 = ":8080";
    private string _port1 = ":8081";
    private string _files = "/dracoSimple/";
    private bool _port = false;

    public int FPS = 30;
    public int bufferSize = 30;
    public bool isLoop = true;

    private float t;
    private int playIndex, lastPlayedIndex;
    private string[] dracoFiles;

    public StatusMonitor monitor;
    public AnimationFPSCounter counter;

    private Mesh currentMesh;

    private Mesh[] buffer; //Stores the files as they are received
    private Mesh[] playBuffer; //Used by Play so no files are overwritten during play
    private bool bufferLoaded;
    private bool playBufferReady;
    private int requestsCounter = 0;

    public int PlayIndex { get => playIndex; set => playIndex = value; }

    private DracoToParticles particlesScript;

    private void OnEnable()
    {
        currentMesh = new Mesh();
        buffer = new Mesh[bufferSize];
        playBuffer = new Mesh[bufferSize];
        for (int i = 0; i < bufferSize; i++)
        {
            buffer[i] = new Mesh();
            playBuffer[i] = new Mesh();
        }
        bufferLoaded = false;
        playBufferReady = true;
        particlesScript = gameObject.GetComponent<DracoToParticles>();
        PlayIndex = 0;
        //UpdateDracoFiles();
    }

    private void UpdateDracoFiles()
    {
        StartCoroutine(GetFilesFromHTTP(RemoteHostPath, (val) => { dracoFiles = val; }));
    }

    private IEnumerator GetFilesFromHTTP(string url, Action<string[]> callback)
    {
        List<string> paths = new List<string>();
        UnityWebRequest webRequest = UnityWebRequest.Get(url);
        yield return webRequest.SendWebRequest();

        if (webRequest.result == UnityWebRequest.Result.Success)
        {
            using (StreamReader reader = new StreamReader(webRequest.downloadHandler.text.GenerateStream()))
            {
                string html = reader.ReadToEnd();
                Regex regex = new Regex(GetDirectoryListingRegexForUrl());
                MatchCollection matches = regex.Matches(html);
                if (matches.Count > 0)
                {
                    foreach (Match match in matches)
                    {
                        if (match.Success)
                        {
                            string value = match.Groups["name"].Value;
                            string path = Path.Combine(url, value);
                            paths.Add(path);
                            //Debug.Log(path);
                            
                        }
                    }
                }
                else
                {
                    monitor.SetText("No matches found!");
                }
                callback(paths.ToArray());
            }
        }
        else
        {
            monitor.SetText("Web request failed\nURL is:" + url);
        }
    }

    public static string GetDirectoryListingRegexForUrl()
    {
        return "<a href=\"(?<name>.+drc)\">.+drc</a>";
    }
    /*
    private async Task Play(int index)
    {
        string filepath = dracoFiles[PlayIndex];
        if (ReadMode == DataReadModes.Remote)
        {
            StartCoroutine(getRequest(filepath, OnRequestComplete, index));
        }

        bool dropFrames = false;
        if (currentMesh != null)
        {
            if (lastPlayedIndex > index && index != 0)
            {
                print("Obsolete data received");
                OnObsoleteDataReceived.Invoke();
                dropFrames = true;
            }

            if (!dropFrames)
            {
                var verticesList = new List<Vector3>(currentMesh.vertices);
                var colorsList = new List<Color32>(currentMesh.colors32);

                await particlesScript.Set(verticesList, colorsList);
                lastPlayedIndex = index;
                //currentMesh.Clear();
            }
        }
    }
    */
    async Task PlayBuffer()
    {
        for (int i = 0; i < bufferSize; i++)
        {
            var verticesList = new List<Vector3>(playBuffer[i].vertices);
            var colorsList = new List<Color32>(playBuffer[i].colors32);

            await particlesScript.Set(verticesList, colorsList);
            await Task.Delay(1000 / FPS);
            //Debug.Log("Playing on " + i);
        }
        playBufferReady = true;
    }

    IEnumerator getRequest(string uri, System.Action<byte[], int> callbackOnFinish, int bufferIndex)
    {
        UnityWebRequest uwr = UnityWebRequest.Get(uri);
        uwr.downloadHandler = new DownloadHandlerBuffer();
        yield return uwr.SendWebRequest();

        if (uwr.result == UnityWebRequest.Result.ConnectionError)
        {
            Debug.Log("Error While Sending: " + uwr.error);
            if (monitor != null)
            {
                monitor.SetText("Web Request error: " + uwr.error);
            }
        }
        else
        {
            byte[] results = uwr.downloadHandler.data;
            callbackOnFinish(results, bufferIndex);
        }
        uwr.Dispose();
    }

    async void OnRequestComplete(byte[] stream, int bufferIndex)
    {
        // Async decoding has to start on the main thread and spawns multiple C# jobs.
        //currentMesh = new Mesh();
        var meshDataArray = Mesh.AllocateWritableMeshData(1);
        var result = await DracoDecoder.DecodeMesh(meshDataArray[0], stream);
        //currentMesh = await DracoDecoder.DecodeMesh(stream);
        Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, buffer[bufferIndex]);
        requestsCounter = requestsCounter + 1;
    }

    public void SetNewAddress(string newAddress)
    {
        RemoteHostPath = newAddress;
    }

    public void SetNewIP(string newIP)
    {
        ipHostPath = newIP;
        RemoteHostPath = _http + ipHostPath + _port0 + _files;
    }

    public void ChangePort()
    {
        if(_port == true)
        {
            RemoteHostPath = _http + ipHostPath + _port0 + _files;
            _port = false;
            Reconnect();
        }
        else
        {
            RemoteHostPath = _http + ipHostPath + _port1 + _files;
            _port = true;
            Reconnect();
        }
    }

    public void Reconnect()
    {
        ReadMode = DataReadModes.Remote;
        UpdateDracoFiles();
    }

    public void ChangeFramerate(string newFramerate)
    {
        int checkOutput;
        if (Int32.TryParse(newFramerate, out checkOutput))
        {
            FPS = checkOutput;
        }
    }

    private void Update()
    {
        if (dracoFiles == null)
        {
            return;
        }
        else
        {
            if (bufferLoaded == false)
            {
                bufferLoaded = true;
                if (PlayIndex >= dracoFiles.Length)
                {
                    PlayIndex = 0;
                }
                
                for (int i = 0; i < bufferSize; i++)
                {
                    string filepath = dracoFiles[i + PlayIndex];
                    StartCoroutine(getRequest(filepath, OnRequestComplete, i));
                }              
                PlayIndex = PlayIndex + bufferSize;
            }

            if (requestsCounter >= bufferSize && playBufferReady)
            {
                Debug.Log("Calling play (play index is:" +PlayIndex +")");
                playBufferReady = false;
                Array.Copy(buffer, playBuffer, bufferSize);
                requestsCounter = 0;
                bufferLoaded = false;
                PlayBuffer();
            }
        }
    }
}
