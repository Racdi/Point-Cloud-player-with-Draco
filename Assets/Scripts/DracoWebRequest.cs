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
using Unity.VisualScripting;

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
    private string _http = "https://";
    private string _port0 = ":443";
    private string _port1 = ":8443";
    private string _files = "/dracoSimple/";
    private bool _port = false;

    public float FPS = 30;
    public int bufferSize = 30;
    public bool isLoop = true;

    private float t;
    private int playIndex, lastPlayedIndex;
    private string[] dracoFiles;

    public StatusMonitor monitor;
    public AnimationFPSCounter counter;

    private Mesh currentMesh;

    private Mesh[] buffer0, buffer1; //Stores the files as they are received, another array is used to play them
    private bool isCurrentBuffer1 = false;
    private bool bufferLoaded;
    private bool playBufferReady;
    private int requestsCounter = 0;

    public int PlayIndex { get => playIndex; set => playIndex = value; }

    private DracoToParticles particlesScript;

    private void OnEnable()
    {
        currentMesh = new Mesh();
        buffer0 = new Mesh[bufferSize];
        for (int i = 0; i < bufferSize; i++)
        {
            buffer0[i] = new Mesh();
        }
        buffer1 = new Mesh[bufferSize];
        for (int i = 0; i < bufferSize; i++)
        {
            buffer1[i] = new Mesh();
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
    public class BypassCertificateHandler : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData)
        {
            return true; // Accept all certificates
        }
    }

    private IEnumerator GetFilesFromHTTP(string url, Action<string[]> callback)
    {
        List<string> paths = new List<string>();
        UnityWebRequest webRequest = UnityWebRequest.Get(url);
        webRequest.certificateHandler = new BypassCertificateHandler();
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

    async Task PlayBuffer(Mesh[] deepCopy)
    {
        for (int i = 0; i < bufferSize; i++)
        {
            //Debug.Log("Showing frame number " + i);
            float startTime = Time.realtimeSinceStartup;
            //var verticesList = new List<Vector3>(deepCopy[i].vertices);
            //var colorsList = new List<Color32>(deepCopy[i].colors32);

            await particlesScript.Set(deepCopy[i]);

            float elapsedS = Time.realtimeSinceStartup - startTime;
            float elapsedMS = elapsedS * 1000;
            float desired = 1000 / FPS;
            //Debug.Log(elapsedMS);
            if (elapsedMS < desired)
            {
                //Debug.Log("Must wait " + (desired - elapsedMS));
                await Task.Delay((int)(desired - elapsedMS));
            }
            else
            {
                await Task.Delay(1);
            }
                counter.Tick();
            
        }
        playBufferReady = true;
        //Debug.Log("Finished playing buffer");
    }

    IEnumerator getRequest(string uri, System.Action<byte[], int> callbackOnFinish, int bufferIndex)
    {
        
        UnityWebRequest uwr = UnityWebRequest.Get(uri);
        uwr.certificateHandler = new BypassCertificateHandler();
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
            //Debug.Log("Loading frame number:" + bufferIndex + " URI is: " + uri);
            byte[] results = uwr.downloadHandler.data;
            callbackOnFinish(results, bufferIndex);
        }
        uwr.Dispose();
    }

    async void OnRequestComplete(byte[] stream, int bufferIndex)
    {
        // Async decoding has to start on the main thread and spawns multiple C# jobs.
        var meshDataArray = Mesh.AllocateWritableMeshData(1);
        var result = await DracoDecoder.DecodeMesh(meshDataArray[0], stream);
        if (isCurrentBuffer1 == false) {
            Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, buffer0[bufferIndex]);
        }
        else
        {
            Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, buffer1[bufferIndex]);
        }
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

    public void SetNewPort(string newPort)
    {
        RemoteHostPath = _http + ipHostPath + ":" + newPort + _files;
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
                //Debug.Log("Begin downloading buffer");

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
                //Debug.Log("Start playing buffer");
                playBufferReady = false;

                /*
                //Array.Copy(buffer, playBuffer, bufferSize);
                Mesh[] deepCopy = new Mesh[bufferSize];
                for (int i = 0; i < bufferSize; i++)
                {
                    deepCopy[i] = new Mesh();
                    deepCopy[i].vertices = buffer[i].vertices;
                    deepCopy[i].colors32 = buffer[i].colors32;
                }
                */
                if(isCurrentBuffer1 == false)
                {
                    PlayBuffer(buffer0);
                    isCurrentBuffer1 = true;
                }
                else
                {
                    PlayBuffer(buffer1);
                    isCurrentBuffer1 = false;
                }
                requestsCounter = 0;
                bufferLoaded = false;
                //PlayBuffer(deepCopy);
            }
        }
    }
}
