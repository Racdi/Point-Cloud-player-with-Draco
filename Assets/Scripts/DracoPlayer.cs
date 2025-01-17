using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using System.Net;
using System.Text.RegularExpressions;
using UnityEngine.Networking;
using UnityEngine.Events;
using PCP;
using PCP.Utils;
using Draco;

    public class DracoPlayer : MonoBehaviour
    {
        [HideInInspector]
        public UnityEvent OnObsoleteDataReceived = new UnityEvent();

        public enum DataReadModes { Remote, Local, StreamingAssets }
        public DataReadModes ReadMode;

        /// <summary>
        /// Path to a remote http server directory (i.e. http://localhost:8000/Recording_1/ , served by 'python -m http.server')
        /// </summary>
        public string RemoteHostPath;

        /// <summary>
        /// Path to a local directory (i.e. C:/../Recording_1/)
        /// </summary>
        public string LocalPath;

        /// <summary>
        /// Path to a streaming asset directory (i.e. Recording_1) <br/>
        /// <b>NOTE:</b> It works in conjunction with AssetIndexerBuildProcessor.cs for accessing files in standalone (non-editor) builds.
        /// </summary>
        public string StreamingAssetsPath;

        public int FPS = 30;
        public bool isLoop = true;

        private float t;
        private int playIndex, lastPlayedIndex;
        private string[] dracoFiles;

    private Mesh currentMesh;

        public int PlayIndex { get => playIndex; set => playIndex = value; }

        private ParticlesFromData particlesScript;

        private void OnEnable()
        {
            particlesScript = gameObject.GetComponent<ParticlesFromData>();
            PlayIndex = 0;
            UpdateDracoFiles();
        }

        private void UpdateDracoFiles()
        {
            if (ReadMode == DataReadModes.Local)
                dracoFiles = Directory.GetFiles(LocalPath, "*.drc");
            else if (ReadMode == DataReadModes.StreamingAssets)
            {
#if UNITY_EDITOR
                dracoFiles = Directory.GetFiles(Path.Combine(Application.streamingAssetsPath, StreamingAssetsPath), "*.drc");
#else
                List<string> dracoFilesList = ReadAssetIndexer();
                dracoFiles = dracoFilesList.ToArray();
#endif
            }
            else if (ReadMode == DataReadModes.Remote)
                StartCoroutine(GetFilesFromHTTP(RemoteHostPath, (val) => { dracoFiles = val; }));
        }

        private List<string> ReadAssetIndexer()
        {
            List<string> plyFilesList = new List<string>();
            TextAsset paths = Resources.Load<TextAsset>(Path.Combine(AssetIndexerConfig.BaseDirectory, StreamingAssetsPath, "paths"));
            string fs = paths.text;
            string[] fLines = Regex.Split(fs, "\n|\r|\r\n");

            foreach (string line in fLines)
            {
                if (line.Length > 0)
                    plyFilesList.Add(line);
            }

            return plyFilesList;
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
                                //print(path);
                            }
                        }
                    }
                    callback(paths.ToArray());
                }
            }
        }

        public static string GetDirectoryListingRegexForUrl()
        {
            return "<a href=\"(?<name>.+drc)\">.+drc</a>";
        }

        private void Play(int index)
        {
            //string[] plypaths = { "https://ateixs.me/ply/simple1.ply", "https://ateixs.me/ply/simple2.ply", "https://ateixs.me/ply/simple3.ply" };
            string filepath = dracoFiles[PlayIndex];
            //Debug.Log(filepath);

            StartCoroutine(getRequest(filepath, OnRequestComplete));          

            bool dropFrames = false;
            if (currentMesh != null)
            {
                if (lastPlayedIndex > index && index != 0)
                {
                    //print("Obsolete data received");
                    OnObsoleteDataReceived.Invoke();
                    dropFrames = true;
                }

                if (!dropFrames)
                {
                    var verticesList = new List<Vector3>(currentMesh.vertices);
                    var colorsList = new List<Color32>(currentMesh.colors32);

                    particlesScript.Set(verticesList, colorsList);
                    lastPlayedIndex = index;
                }
            }

        }
    
    IEnumerator getRequest(string uri, System.Action<Stream> callbackOnFinish)
    {
        UnityWebRequest uwr = UnityWebRequest.Get(uri);
        yield return uwr.SendWebRequest();

        if (uwr.isNetworkError)
        {
            Debug.Log("Error While Sending: " + uwr.error);
        }
        else
        {
            callbackOnFinish(uwr.downloadHandler.data.GenerateStream());
        }
    }

    async void OnRequestComplete(Stream stream)
    {
        var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        currentMesh = await DracoDecoder.DecodeMesh(memoryStream.ToArray());
    }

    private void Update()
        {
            if (dracoFiles == null)
            {
                return;
            }

            FPS = Mathf.Max(1, FPS);

            t += Time.deltaTime;

            if (dracoFiles.Length > 0 && t >= 1f / FPS)
            {
                t = 0;
                if (PlayIndex < dracoFiles.Length) //dracoFiles.Length
                {
                    Play(playIndex);
                    ++PlayIndex;
                }
                else if (isLoop)
                {
                    PlayIndex = 0;
                }
            }

        }
    }
