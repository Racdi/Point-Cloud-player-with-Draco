using UnityEngine;
using System;
using System.IO;
using System.Diagnostics;
using System.Collections;
using System.Text;

/// <summary>
/// Optimized bulk QUIC downloader using existing picoquicdemo.exe
/// Downloads ALL files in a SINGLE process for maximum efficiency
/// Replaces AppLauncher.cs with 20-50x better performance
/// </summary>
public class OptimizedQUICDownloader : MonoBehaviour
{
    [Header("Server Configuration")]
    public string serverHost = "ateixs.me";
    public int serverPort = 443;
    
    [Header("Download Configuration")]
    public string filePattern = "draco/{0:D4}.drc";  // e.g., 1000.drc
    public int startIndex = 1000;
    public int endIndex = 1299;
    public string outputDirectory = "Downloads";
    
    [Header("Performance Settings")]
    [Tooltip("Max concurrent QUIC streams (adjust based on network)")]
    public int maxConcurrentStreams = 10;
    [Tooltip("Batch size for very large downloads")]
    public int batchSize = 100; // 0 = download all at once
    
    [Header("Events")]
    public UnityEngine.Events.UnityEvent<int, int, float> OnProgressUpdate; // completed, total, percentage
    public UnityEngine.Events.UnityEvent<bool, string, int, int> OnDownloadComplete; // success, message, completed, failed
    public UnityEngine.Events.UnityEvent<string> OnFileDownloaded; // individual file completed
    
    // Private fields
    private Process downloadProcess;
    private bool isDownloading = false;
    private int totalFiles;
    private int completedFiles;
    private int failedFiles;
    private string outputPath;
    private Coroutine progressMonitor;
    private DateTime downloadStartTime;
    
    // Public properties
    public bool IsDownloading => isDownloading;
    public float Progress => totalFiles > 0 ? (float)completedFiles / totalFiles : 0f;
    public int CompletedFiles => completedFiles;
    public int TotalFiles => totalFiles;
    public int FailedFiles => failedFiles;
    public double DownloadSpeedFilesPerSecond 
    { 
        get 
        {
            var elapsed = DateTime.Now - downloadStartTime;
            return elapsed.TotalSeconds > 0 ? completedFiles / elapsed.TotalSeconds : 0;
        }
    }
    
    /// <summary>
    /// Start downloading all files in a single efficient process
    /// </summary>
    public void StartBulkDownload()
    {
        if (isDownloading)
        {
            UnityEngine.Debug.LogWarning("Download already in progress");
            return;
        }
        
        StartCoroutine(ExecuteBulkDownload());
    }
    
    /// <summary>
    /// Start download with custom parameters
    /// </summary>
    public void StartBulkDownload(string host, int port, string pattern, int start, int end)
    {
        if (isDownloading)
        {
            UnityEngine.Debug.LogWarning("Download already in progress");
            return;
        }
        
        serverHost = host;
        serverPort = port;
        filePattern = pattern;
        startIndex = start;
        endIndex = end;
        
        StartCoroutine(ExecuteBulkDownload());
    }
    
    /// <summary>
    /// Generate scenario string for picoquicdemo.exe (QUIC multiplexing format)
    /// Format: "0:file1.pcl;4:file2.pcl;8:file3.pcl;..."
    /// </summary>
    private string GenerateScenario()
    {
        StringBuilder scenario = new StringBuilder();
        
        for (int i = startIndex; i <= endIndex; i++)
        {
            string filename = string.Format(filePattern, i);
            
            // QUIC stream IDs for client-initiated bidirectional streams: 0, 4, 8, 12...
            int streamId = (i - startIndex) * 4;
            
            scenario.Append($"{streamId}:{filename};");
        }
        
        return scenario.ToString().TrimEnd(';'); // Remove trailing semicolon
    }
    public void SetHostAndPort(string newHostname, int newPort)
    {
        serverHost = newHostname;
        serverPort = newPort;
    }

    public void SetIndexes(int newStartIndex, int newEndIndex)
    {
        startIndex = newStartIndex;
        endIndex = newEndIndex;
    }
    private IEnumerator ExecuteBulkDownload()
    {
        isDownloading = true;
        totalFiles = endIndex - startIndex + 1;
        completedFiles = 0;
        failedFiles = 0;
        downloadStartTime = DateTime.Now;
        
        UnityEngine.Debug.Log($"Starting optimized bulk download of {totalFiles} files");
        UnityEngine.Debug.Log($"Server: {serverHost}:{serverPort}");
        UnityEngine.Debug.Log($"Pattern: {filePattern}");
        UnityEngine.Debug.Log($"Range: {startIndex} to {endIndex}");
        
        // Setup output directory
        outputPath = Path.Combine(Application.persistentDataPath, outputDirectory);
        Directory.CreateDirectory(outputPath);
        
        // Clear any existing files from previous downloads
        ClearExistingFiles();
        
        try
        {
            if (batchSize > 0 && totalFiles > batchSize)
            {
                // Download in batches for very large downloads
                StartCoroutine(ExecuteBatchedDownload());
            }
            else
            {
                // Download all files in single batch
                StartCoroutine(ExecuteSingleBatch(startIndex, endIndex));
            }
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"Exception during bulk download: {ex.Message}");
            OnDownloadComplete?.Invoke(false, ex.Message, completedFiles, failedFiles);
        }
        finally
        {
            CleanupProcess();
            isDownloading = false;
            
        }
        yield return null;
    }
    
    private IEnumerator ExecuteBatchedDownload()
    {
        UnityEngine.Debug.Log($"Using batched download: {batchSize} files per batch");
        
        for (int batchStart = startIndex; batchStart <= endIndex; batchStart += batchSize)
        {
            int batchEnd = Mathf.Min(batchStart + batchSize - 1, endIndex);
            
            UnityEngine.Debug.Log($"Downloading batch: files {batchStart} to {batchEnd}");
            
            yield return StartCoroutine(ExecuteSingleBatch(batchStart, batchEnd));
            
            // Brief pause between batches
            if (batchEnd < endIndex)
            {
                yield return new WaitForSeconds(1f);
            }
        }
        
        // Final completion callback
        bool success = failedFiles == 0 && completedFiles == totalFiles;
        string message = success ? 
            $"Successfully downloaded all {completedFiles}/{totalFiles} files" :
            $"Completed with {failedFiles} failures. Downloaded: {completedFiles}/{totalFiles}";
        
        OnDownloadComplete?.Invoke(success, message, completedFiles, failedFiles);
    }
    
    private IEnumerator ExecuteSingleBatch(int batchStart, int batchEnd)
    {
        // Generate scenario for this batch
        string scenario = GenerateBatchScenario(batchStart, batchEnd);
        int batchSize = batchEnd - batchStart + 1;
        
        UnityEngine.Debug.Log($"Generated scenario for {batchSize} files");
        UnityEngine.Debug.Log($"Scenario preview: {scenario.Substring(0, Math.Min(200, scenario.Length))}...");
        
        // Start single process for this batch
        bool processStarted = StartDownloadProcess(scenario);
        if (!processStarted)
        {
            string errorMsg = "Failed to start download process";
            if (batchStart == startIndex && batchEnd == endIndex)
            {
                // Single batch failure
                OnDownloadComplete?.Invoke(false, errorMsg, 0, 0);
            }
            yield break;
        }
        
        // Start monitoring progress for this batch
        progressMonitor = StartCoroutine(MonitorDownloadProgress());
        
        // Wait for process to complete
        while (downloadProcess != null && !downloadProcess.HasExited)
        {
            yield return new WaitForSeconds(0.1f);
        }
        
        // Stop progress monitoring
        if (progressMonitor != null)
        {
            StopCoroutine(progressMonitor);
            progressMonitor = null;
        }
        
        // Final progress update for this batch
        yield return StartCoroutine(UpdateProgressFromFiles());
        
        // Check batch results
        int exitCode = downloadProcess?.ExitCode ?? -1;
        bool batchSuccess = exitCode == 0;
        
        UnityEngine.Debug.Log($"Batch completed with exit code: {exitCode}");
        
        if (!batchSuccess)
        {
            failedFiles += batchSize - (CountDownloadedFiles() - completedFiles);
        }
        
        CleanupProcess();
    }
    
    private string GenerateBatchScenario(int batchStart, int batchEnd)
    {
        StringBuilder scenario = new StringBuilder();
        
        for (int i = batchStart; i <= batchEnd; i++)
        {
            string filename = string.Format(filePattern, i);
            
            // QUIC stream IDs for client-initiated bidirectional streams: 0, 4, 8, 12...
            int streamId = (i - batchStart) * 4;
            
            scenario.Append($"{streamId}:{filename};");
        }
        
        return scenario.ToString().TrimEnd(';');
    }

    private bool StartDownloadProcess(string scenario)
    {
        try
        {
            downloadProcess = new Process();
            downloadProcess.EnableRaisingEvents = false;
            downloadProcess.StartInfo.FileName = Application.dataPath + "/../Executables/picoquicdemo.exe";
            downloadProcess.StartInfo.Arguments = $"{serverHost} {serverPort} \"{scenario}\"";
            downloadProcess.StartInfo.UseShellExecute = false;
            downloadProcess.StartInfo.RedirectStandardOutput = true;
            downloadProcess.StartInfo.RedirectStandardError = true;
            downloadProcess.StartInfo.CreateNoWindow = true;
            downloadProcess.StartInfo.WorkingDirectory = outputPath;

            // Event handlers for process output
            downloadProcess.OutputDataReceived += OnProcessOutputReceived;
            downloadProcess.ErrorDataReceived += OnProcessErrorReceived;

            UnityEngine.Debug.Log($"Starting process: {downloadProcess.StartInfo.FileName}");
            UnityEngine.Debug.Log($"Arguments: {downloadProcess.StartInfo.Arguments}");
            UnityEngine.Debug.Log($"Working directory: {outputPath}");

            downloadProcess.Start();
            downloadProcess.BeginOutputReadLine();
            downloadProcess.BeginErrorReadLine();

            UnityEngine.Debug.Log("Successfully launched bulk download process");
            return true;
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError($"Unable to launch download process: {e.Message}");
            return false;
        }
    }

    private void OnProcessOutputReceived(object sender, DataReceivedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.Data)) return;

        UnityEngine.Debug.Log($"picoquicdemo: {e.Data}");

        // Parse useful information from picoquicdemo output
        if (e.Data.Contains("Connection established"))
        {
            UnityEngine.Debug.Log("QUIC connection established successfully");
        }
        else if (e.Data.Contains("Received") && e.Data.Contains("bytes"))
        {
            UnityEngine.Debug.Log($"Transfer info: {e.Data}");
        }
        else if (e.Data.Contains("Client exit with code = 0"))
        {
            UnityEngine.Debug.Log("picoquicdemo completed successfully");
        }
        else if (e.Data.Contains("Client exit with code = -1"))
        {
            UnityEngine.Debug.LogWarning("picoquicdemo completed with errors");
        }
    }

    private void OnProcessErrorReceived(object sender, DataReceivedEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.Data))
        {
            UnityEngine.Debug.LogError($"picoquicdemo error: {e.Data}");
        }
    }

    private IEnumerator MonitorDownloadProgress()
    {
        int lastCompletedCount = completedFiles;

        while (isDownloading && (downloadProcess == null || !downloadProcess.HasExited))
        {
            yield return new WaitForSeconds(0.5f); // Update twice per second
            yield return StartCoroutine(UpdateProgressFromFiles());

            // Check for new files completed
            if (completedFiles > lastCompletedCount)
            {
                // Trigger individual file completion events
                for (int i = lastCompletedCount; i < completedFiles; i++)
                {
                    string completedFile = string.Format(filePattern, startIndex + i);
                    UnityEngine.Debug.Log("Sending an Invoke for file: " + completedFile);
                    OnFileDownloaded?.Invoke(completedFile);
                }
                lastCompletedCount = completedFiles;
            }
        }
    }

    private IEnumerator UpdateProgressFromFiles()
    {
        // Count downloaded files in background to avoid frame drops
        yield return new WaitForEndOfFrame();

        int currentCompleted = CountDownloadedFiles();

        if (currentCompleted != completedFiles)
        {
            completedFiles = currentCompleted;

            float percentage = Progress * 100f;

            //UnityEngine.Debug.Log("Sending an Invoke for files: " + completedFiles);
            OnProgressUpdate?.Invoke(completedFiles, totalFiles, percentage);

            UnityEngine.Debug.Log($"Progress: {completedFiles}/{totalFiles} files ({percentage:F1}%) - " +
                                $"Speed: {DownloadSpeedFilesPerSecond:F1} files/sec");
        }
    }

    private int CountDownloadedFiles()
    {
        if (!Directory.Exists(outputPath)) return 0;

        try
        {
            // Count files matching the expected pattern
            string[] files = Directory.GetFiles(outputPath, "*.drc"); // Adjust extension as needed

            // You might want to validate that files are actually complete
            // For now, just count existing files
            return files.Length;
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogWarning($"Error counting files: {ex.Message}");
            return 0;
        }
    }

    private void ClearExistingFiles()
    {
        try
        {
            if (Directory.Exists(outputPath))
            {
                string[] existingFiles = Directory.GetFiles(outputPath, "*.drc");
                foreach (string file in existingFiles)
                {
                    File.Delete(file);
                }
                if (existingFiles.Length > 0)
                {
                    UnityEngine.Debug.Log($"Cleared {existingFiles.Length} existing files");
                }
            }
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogWarning($"Error clearing existing files: {ex.Message}");
        }
    }

    public void CancelDownload()
    {
        if (downloadProcess != null && !downloadProcess.HasExited)
        {
            try
            {
                downloadProcess.Kill();
                UnityEngine.Debug.Log("Download cancelled by user");
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"Failed to cancel download: {ex.Message}");
            }
        }
    }

    private void CleanupProcess()
    {
        if (downloadProcess != null)
        {
            try
            {
                // Unsubscribe from events
                downloadProcess.OutputDataReceived -= OnProcessOutputReceived;
                downloadProcess.ErrorDataReceived -= OnProcessErrorReceived;

                if (!downloadProcess.HasExited)
                {
                    downloadProcess.Kill();
                }
                downloadProcess.Dispose();
                downloadProcess = null;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"Error cleaning up process: {ex.Message}");
            }
        }
    }

    private void OnDestroy()
    {
        CancelDownload();
        CleanupProcess();
    }

    // Public utility methods
    public void SetDownloadRange(int start, int end)
    {
        if (!isDownloading)
        {
            startIndex = start;
            endIndex = end;
        }
        else
        {
            UnityEngine.Debug.LogWarning("Cannot change range while downloading");
        }
    }

    public void SetFilePattern(string pattern)
    {
        if (!isDownloading)
        {
            filePattern = pattern;
        }
        else
        {
            UnityEngine.Debug.LogWarning("Cannot change pattern while downloading");
        }
    }

    public void SetServer(string host, int port)
    {
        if (!isDownloading)
        {
            serverHost = host;
            serverPort = port;
        }
        else
        {
            UnityEngine.Debug.LogWarning("Cannot change server while downloading");
        }
    }

    public string GetOutputPath()
    {
        return outputPath;
    }

    public string[] GetDownloadedFiles()
    {
        if (!Directory.Exists(outputPath)) return new string[0];

        try
        {
            return Directory.GetFiles(outputPath, "*.drc");
        }
        catch
        {
            return new string[0];
        }
    }
}