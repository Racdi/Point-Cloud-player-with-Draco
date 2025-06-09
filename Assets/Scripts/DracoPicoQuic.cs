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
using TMPro;
using System.Runtime.InteropServices.ComTypes;
using UnityEngine.UIElements;
using System.Linq;

public class DracoQUIC : MonoBehaviour
{
    public string fullPath;
    public string HostPath;
    private string _http = "https://";
    private string _port = "443";
    private string _files = "draco/";

    [SerializeField]
    private int[] sliceAddressList;
    private float[] sliceTimestampList;

    private int currentSlice = 0;

    private bool CheckSlicesTimestampEnabled = false;

    [SerializeField]
    private SliceGraphicsChanger _changer;

    [SerializeField]
    private float _TimestampThreshold = 1.0f;

    public float FPS = 30;
    private float inverseFPS;
    public bool isLoop = true;

    private int playIndex, lastPlayedIndex;
    private string[] dracoFiles;

    public StatusMonitor monitor;
    public AnimationFPSCounter counter;
    public TextMeshProUGUI downloadTimerText;

    public OptimizedQUICDownloader optimizedDownloader;

    private Queue<Mesh> LoadedMeshes;

    private bool haltDownloading = true;
    private bool readingFiles = false;
    private bool playerReady = true;


    public int PlayIndex { get => playIndex; set => playIndex = value; }

    private DracoToParticles particlesScript;

    private void OnEnable()
    {
        sliceTimestampList = new float[sliceAddressList.Count()];
        LoadedMeshes = new Queue<Mesh>();
        PlayIndex = 0;
        inverseFPS = 1000 / FPS;
        ResetTimestamps();
    }

    void UpdateDracoFiles()
    {
        //StartCoroutine(GetFilesFromHTTP(fullPath, (val) => { dracoFiles = val; }));

        dracoFiles = new string[300];
        for (int i = 0; i < 300; i++) //Disgustingly hardcoded solution
        {
            dracoFiles[i] = (1000 + i) + ".drc";
        }

        haltDownloading = false;
    }
    public class BypassCertificateHandler : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData)
        {
            return true; // Accept all certificates
        }
    }
    

    async Task PlaySingleFile(Mesh currentFile)
    {
        playerReady = false;
        float startTime = Time.realtimeSinceStartup;
        
        particlesScript.Set(currentFile);

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

        playerReady = true;
    }


    async void ReadMeshFromFile(string fileName)
    {
        Debug.Log("Begin reading from file");
        readingFiles = true;

        byte[] stream = ReadStreamFromFile(fileName, null);

        if (stream != null)
        {
            var meshDataArray = Mesh.AllocateWritableMeshData(1);
            await DracoDecoder.DecodeMesh(meshDataArray[0], stream);

            Mesh tempMesh = new Mesh();

            Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, tempMesh);

            LoadedMeshes.Enqueue(tempMesh);
        }
        readingFiles = false;
    }
    public async void ReceiveRequestToReadFile(string fileName)
    {
        Debug.Log("Request to read file received");
        while (readingFiles)
        {
            await Task.Delay(1);
        }
        
        ReadMeshFromFile(fileName);

    }
        
    byte[] ReadStreamFromFile(string fileName, string filePath)
    {
        byte[] buffer;
        if(filePath == null)
        {
            filePath = Application.persistentDataPath + "/Downloads/";
        }

        if (File.Exists(filePath+fileName))
        {
            using (FileStream fileStream = File.OpenRead(filePath+fileName))
            {
                int length = (int)fileStream.Length;
                buffer = new byte[length];
                
                // Set the stream position to the beginning of the file.
                fileStream.Seek(0, SeekOrigin.Begin);

                fileStream.Read(buffer, 0 , length);
                
            }
            return buffer;
        }
        else
        {
            return null;
        }
        
    }

    public void SetNewIP(string newIP)
    {
        HostPath = newIP;
        ResetHostPath();
    }

    public void SetNewPort(string newPort)
    {
        UnityEngine.Debug.Log("Not changing slice, undefined behavior");
        _port = newPort;
        ResetHostPath();
    }

    public void SetPortFromSliceList(int slice)
    {
        currentSlice = slice;
        _port = sliceAddressList[currentSlice].ToString();
        ResetHostPath();
        _changer.ChangeSlice(currentSlice);
    }

    private void ResetHostPath()
    {
        fullPath = _http + HostPath + _port + _files;
        //UpdateDracoFiles();
    }

    public void Reconnect()
    {
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
    /*
    private void CheckSliceTimestamp(int currentBuffer)
    {
        UnityEngine.Debug.Log("Current slice is: " + currentSlice);
        float currentTimestamp = endBufferingTime[currentBuffer] - startBufferingTime[currentBuffer];
        UnityEngine.Debug.Log(currentTimestamp);
        sliceTimestampList[currentSlice] = currentTimestamp;

        if (!CheckSlicesTimestampEnabled)
        {
            return;
        }

        if (currentTimestamp < _TimestampThreshold)
        {
            UnityEngine.Debug.Log("Timestamp is fine");
        }
        else
        {
            UnityEngine.Debug.Log("Timestamp NOT FINE!");

            int checkedSlices = 0;
            while (checkedSlices < sliceAddressList.Length)
            {
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
    */
    private void Update()
    {
        if (dracoFiles == null)
        {
            return;
        }

        //Starts the requests to download files
        if (haltDownloading == false)
        {
            //Debug.Log("Begin haltDownloading buffer");

            haltDownloading = true;
            optimizedDownloader.SetHostAndPort(HostPath, sliceAddressList[currentSlice]);
            optimizedDownloader.SetIndexes(1000, 1299);
            optimizedDownloader.StartBulkDownload();
            
            /*
            if (PlayIndex >= dracoFiles.Length && isLoop)
            {
                PlayIndex = 0;
            }
            else if (!isLoop)
            {
                return;
            }

            for (int i = 0; i < bufferSize; i++)
            {
                string filepath = dracoFiles[i + PlayIndex];

                string appArgs = "-o " + Application.dataPath + "/../Downloads " + HostPath + " " + _port + " " + _files + filepath;

                

                //appLauncher.StartProcess("picoquicdemo.exe", appArgs);
                //---------- start picoquic here ------------
                //StartCoroutine(getRequest(filepath, OnRequestComplete, i));
            }
            */
        }


        //Play the read files to the user
        if (playerReady && LoadedMeshes.Count > 0)
        {
            Debug.Log("Start playing");
            PlaySingleFile(LoadedMeshes.Dequeue());

        }
    }
}
