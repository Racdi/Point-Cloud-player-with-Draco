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
using TMPro;
using Best.HTTP;
using Best.HTTP.Request;
using Best.HTTP.Response;
using Best.HTTP.Shared.PlatformSupport.Memory;

public class DracoBestHTTP : MonoBehaviour
{
    [HideInInspector]
    public UnityEvent OnObsoleteDataReceived = new UnityEvent();

    public enum DataReadModes { Remote, Local, StreamingAssets }
    public DataReadModes ReadMode;

    /// <summary>
    /// Path to a remote http server directory (i.e. http://localhost:8000/Recording_1/ , served by 'python -m http.server')
    /// </summary>
    public string fullHostPath;
    public string hostPath;
    private string _http = "https://";
    private string _port = ":443";
    private string _files = "/draco/";

    [SerializeField]
    private string[] sliceAddressList;
    private float[] sliceTimestampList;

    private int currentSlice=0;

    private bool CheckSlicesTimestampEnabled = false;

    [SerializeField]
    private SliceGraphicsChanger _changer;

    [SerializeField]
    private float _TimestampThreshold = 1.0f;

    public float FPS = 30;
    private float inverseFPS;
    public int bufferSize = 30;
    public bool isLoop = true;

    private float[] startBufferingTime;
    private float[] endBufferingTime;

    private int playIndex, lastPlayedIndex;
    private string[] dracoFiles;

    public StatusMonitor monitor;
    public AnimationFPSCounter counter;
    public TextMeshProUGUI downloadTimerText;

    private Mesh currentMesh;

    //[SerializeField]
    private int numberOfBuffers = 2;
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
        inverseFPS = 1000 / FPS;
        sliceTimestampList = new float[sliceAddressList.Length];
        startBufferingTime = new float[numberOfBuffers];
        endBufferingTime = new float[numberOfBuffers];
        ResetTimestamps();
        //UpdateDracoFiles();
    }

    void UpdateDracoFiles()
    {
        //StartCoroutine(GetFilesFromHTTP(fullHostPath, (val) => { dracoFiles = val; }));

        dracoFiles = new string[300];
        for (int i = 0; i < 300; i++) //Disgustingly hardcoded solution
        {
            dracoFiles[i] = (1000 + i) + ".drc";
        }
    }
    public class BypassCertificateHandler : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData)
        {
            return true; // Accept all certificates
        }
    }

    /*
    private IEnumerator GetFilesFromHTTP(string url, Action<string[]> callback)
    {
        List<string> paths = new List<string>();
        var webRequest = HTTPRequest.CreateGet(url);
        //webRequest.certificateHandler = new BypassCertificateHandler();
        webRequest.Send();

        yield return webRequest;

        switch (webRequest.State)
        {
            case HTTPRequestStates.Finished:
                if (webRequest.Response.IsSuccess)
                {
                    Debug.Log("Is success");
                    using (StreamReader reader = new StreamReader(webRequest.Response.DownStream))
                    {
                        string html = reader.ReadToEnd();
                        Debug.Log(html);
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
                                    Debug.Log(path);
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
                break;
            default:
                Debug.LogError($"Request finished with error! Request state: {webRequest.State}");
                break;
        }
        
    }
    */
    public static string GetDirectoryListingRegexForUrl()
    {
        return "<a href=\"(?<name>.+drc)\">.+drc</a>";
    }

    async Task PlayBuffer(Mesh[] currentBuffer)
    {
        float startTime = Time.realtimeSinceStartup;
        for (int i = 0; i < bufferSize; i++)
        {
            //Debug.Log("Showing frame number " + i);
            
            //var verticesList = new List<Vector3>(deepCopy[i].vertices);
            //var colorsList = new List<Color32>(deepCopy[i].colors32);

            particlesScript.Set(currentBuffer[i]);

            float elapsedS = Time.realtimeSinceStartup - startTime;
            float elapsedMS = elapsedS * 1000;
            
            //Debug.Log(elapsedMS);
            if (elapsedMS < inverseFPS)
            {
                int delay = (int)Math.Round(inverseFPS - elapsedMS);
                //Debug.Log("Must wait " + delay);
                await Task.Delay(delay);
            }
            else
            {
                await Task.Delay(1);
            }
                counter.Tick();
                startTime = Time.realtimeSinceStartup;
        }
        playBufferReady = true;
        //Debug.Log("Finished playing buffer");
    }

    IEnumerator getRequest(string uri, System.Action<byte[], int> callbackOnFinish, int bufferIndex)
    {
        Debug.Log(uri);
        var wr = HTTPRequest.CreateGet(uri);
        //uwr.certificateHandler = new BypassCertificateHandler();
        //uwr.downloadHandler = new DownloadHandlerBuffer();
        wr.Send();
        yield return wr;

        switch (wr.State)
        {
            case HTTPRequestStates.Finished:
                if (!wr.Response.IsSuccess)
                {
                    Debug.Log("Error While Sending: " + wr.Response.ToString());
                    if (monitor != null)
                    {
                        monitor.SetText("Web Request error: " + wr.Response.ToString());
                    }
                }
                else
                {
                    //Debug.Log("Loading frame number:" + bufferIndex + " URI is: " + uri);
                    byte[] dracoData = wr.Response.Data;
                    if (dracoData != null)
                    {
                        callbackOnFinish(dracoData, bufferIndex);
                    }
                }
                break;
            default:
                Debug.LogError($"Request finished with error! Request state: {wr.State}");
                break;
        }
        
        //wr.dispose?;
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

        //Check if all requests are completed for timestamping

        if (requestsCounter >= bufferSize)
        {
            if (isCurrentBuffer1 == false)
            {
                endBufferingTime[0] = Time.realtimeSinceStartup;
                //CheckSliceTimestamp(0);
                downloadTimerText.text = "Last download took " + ((endBufferingTime[0] - startBufferingTime[0]) * 1000).ToString("F0") + " ms";
            }
            else
            {
                endBufferingTime[1] = Time.realtimeSinceStartup;
                CheckSliceTimestamp(1);
                downloadTimerText.text = "Last download took " + ((endBufferingTime[1] - startBufferingTime[1]) * 1000).ToString("F0") + " ms";
            }
            
        }
    }
    private byte[] Combine(List<byte[]> arrays)
    {
        int size = 0;
        foreach (byte[] array in arrays)
        {
            size += array.Length;
        }
        byte[] rv = new byte[size];
        int offset = 0;
        foreach (byte[] array in arrays)
        {
            System.Buffer.BlockCopy(array, 0, rv, offset, array.Length);
            offset += array.Length;
        }
        return rv;
    }
    public void SetNewAddress(string newAddress)
    {
        fullHostPath = newAddress;
    }

    public void SetNewIP(string newIP)
    {
        hostPath = newIP;
        ResetHostPath();
    }

    public void SetNewPort(string newPort)
    {
        Debug.Log("Not changing slice, undefined behavior");
        _port = ":" + newPort;
        ResetHostPath();
    }

    public void SetPortFromSliceList(int slice)
    {
        currentSlice = slice;
        _port = ":" + sliceAddressList[currentSlice];
        ResetHostPath();
        _changer.ChangeSlice(currentSlice);
    }

    private void ResetHostPath()
    {
        fullHostPath = _http + hostPath + _port + _files;
        //UpdateDracoFiles();
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
    public void SwitchCheckTimestampsFunction()
    {
        CheckSlicesTimestampEnabled = !CheckSlicesTimestampEnabled;
    }

    public void ResetTimestamps()
    {
        for (int i = 0; i < sliceTimestampList.Length; i++)
        {
            sliceTimestampList[i] = -1;
        }
    }
    private void CheckSliceTimestamp(int currentBuffer)
    {
        //Debug.Log("Current slice is: " + currentSlice);
        float currentTimestamp = endBufferingTime[currentBuffer] - startBufferingTime[currentBuffer];
        //Debug.Log(currentTimestamp);
        sliceTimestampList[currentSlice] = currentTimestamp;

        if (!CheckSlicesTimestampEnabled)
        {
            return;
        }

        if (currentTimestamp < _TimestampThreshold)
        {
            Debug.Log("Timestamp is fine");    
        }
        else
        {
            Debug.Log("Timestamp NOT FINE!");
            
            int checkedSlices = 0;
            while(checkedSlices < sliceAddressList.Length){
                if (sliceTimestampList[checkedSlices] < _TimestampThreshold)
                {
                    SetPortFromSliceList(checkedSlices);
                    UpdateDracoFiles();
                    return;
                }
                
                checkedSlices++;
            }
            checkedSlices = 0;
            while (checkedSlices < sliceAddressList.Length)
            {
                if (sliceTimestampList[checkedSlices] < sliceTimestampList[currentSlice])
                {
                    SetPortFromSliceList(checkedSlices);
                    UpdateDracoFiles();
                    return;
                }
                checkedSlices++;
            }

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
                if (PlayIndex >= dracoFiles.Length && isLoop)
                {
                    PlayIndex = 0;
                }
                else if (!isLoop)
                {
                    return ;
                }

                if(isCurrentBuffer1 == false)
                {
                    startBufferingTime[0] = Time.realtimeSinceStartup;
                }
                else
                {
                    startBufferingTime[1] = Time.realtimeSinceStartup;
                }

                for (int i = 0; i < bufferSize; i++)
                {
                    string filepath = dracoFiles[i + PlayIndex];
                    
                        
                    StartCoroutine(getRequest(fullHostPath+filepath, OnRequestComplete, i));
                }              
                PlayIndex += bufferSize;
            }
            if(requestsCounter >= bufferSize && playBufferReady)
            {
                //Debug.Log("Start playing buffer");
                playBufferReady = false;

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
                
            }
            
        }
    }
}
