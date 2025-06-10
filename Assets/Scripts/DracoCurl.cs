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

public class DracoCurl : MonoBehaviour
{
    public string fullPath;
    public string HostPath;
    private string _http = "https://";
    private string _port = "443";
    private string _files = "draco/";

    [SerializeField]
    private int[] sliceAddressList;
    //private float[] sliceTimestampList;

    private int currentSlice = 0;

    //private bool CheckSlicesTimestampEnabled = false;

    //[SerializeField]
    //private SliceGraphicsChanger _changer;

    //[SerializeField]
    //private float _TimestampThreshold = 1.0f;

    public float FPS = 30;
    private float inverseFPS;
    public bool isLoop = true;

    public int batchSize = 30;

    private int playIndex, lastPlayedIndex;
    private string[] dracoFiles;

    //public StatusMonitor monitor;
    //public AnimationFPSCounter counter;
    //public TextMeshProUGUI downloadTimerText;

    public AppLauncher appLauncher;

    private Queue<Mesh> LoadedMeshes;
    private Mesh currentMesh;

    private bool haltDownloading = true;
    private bool advanceBatch = false;
    private bool readingFiles = false;
    private bool playerReady = true;


    public int PlayIndex { get => playIndex; set => playIndex = value; }

    public DracoToParticles particlesScript;

    private void OnEnable()
    {
        //sliceTimestampList = new float[sliceAddressList.Count()];
        LoadedMeshes = new Queue<Mesh>();
        PlayIndex = 0;
        inverseFPS = 1000 / FPS;
        currentMesh = new Mesh();
        //ResetTimestamps();
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
    

    async Task PlaySingleFile()
    {        
        playerReady = false;
        float startTime = Time.realtimeSinceStartup;
        particlesScript.Set(currentMesh);
        

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
        //counter.Tick();
        startTime = Time.realtimeSinceStartup;

       
        playerReady = true;
    }


    async void ReadMeshFromFile(string fileName, Mesh.MeshDataArray meshDataArray)
    {
        byte[] stream = ReadStreamFromDownloadedFile(fileName, "C:/Users/Rafael/AppData/LocalLow/Smartness/Draco-RTC/Downloads/");

        if (stream != null)
        {
            await DracoDecoder.DecodeMesh(meshDataArray[0], stream);
            
            Mesh tempMesh = new Mesh();

            Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, tempMesh);

            LoadedMeshes.Enqueue(tempMesh);
        }
    }
    public async void ReceiveRequestToReadFile(string fileName)
    {
        Debug.Log("Request to read file received");
        while (readingFiles)
        {
            await Task.Delay(1);
        }

        var meshDataArray = Mesh.AllocateWritableMeshData(1);
        ReadMeshFromFile(fileName, meshDataArray);

    }


    byte[] ReadStreamFromDownloadedFile(string fileName, string filePath)
    {
        byte[] buffer;
        if (filePath == null)
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
        //_changer.ChangeSlice(currentSlice);
    }

    private void ResetHostPath()
    {
        fullPath = _http + HostPath + ":" + _port + "/" + _files;
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
        //CheckSlicesTimestampEnabled = !CheckSlicesTimestampEnabled;
    }

    /*
    public void ResetTimestamps()
    {
        for (int i = 0; i < sliceTimestampList.Length; i++)
        {
            sliceTimestampList[i] = -1;
        }
    }
    */

    public void AdvanceBatch()
    {
        advanceBatch = true;
    }

    

    private void Update()
    {
        if (dracoFiles == null)
        {
            return;
        }

        //Starts the requests to download files
        if (haltDownloading == false && LoadedMeshes.Count < 30)
        {
            //Debug.Log("Begin haltDownloading buffer");

            haltDownloading = true;
            string appArgs = "--http3 --parallel";
            for (int i = 0; i < batchSize; i++ )
            {
                string filepath = dracoFiles[i + PlayIndex];

                 appArgs += " -o " + Application.persistentDataPath + "/Downloads/" + filepath + " " + fullPath + "/" + filepath;

                //Debug.Log(appArgs);
            }

            appLauncher.StartProcess("curl.exe", appArgs);
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

        if (advanceBatch)
        {
            for (int i = playIndex; i < playIndex + batchSize; i++)
            {
                var meshDataArray = Mesh.AllocateWritableMeshData(1);
                ReadMeshFromFile(dracoFiles[i], meshDataArray);
            }
            playIndex += batchSize;
            if (playIndex >= dracoFiles.Length && isLoop)
            {
                playIndex = 0;
            }
            haltDownloading = false;
            advanceBatch = false;
        }
        //Play the read files to the user
        if (playerReady && LoadedMeshes.Count > 0)
        {
            LoadedMeshes.TryDequeue(out currentMesh);
            Debug.Log(LoadedMeshes.Count);
            //tempMesh.SetVertices(LoadedMeshes.Peek().vertices);
            PlaySingleFile();

        }
    }
}
