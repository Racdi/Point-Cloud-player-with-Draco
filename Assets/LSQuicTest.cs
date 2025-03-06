using UnityEngine;
using System;
using System.Runtime.InteropServices;

public class LSQuicTest : MonoBehaviour
{
    // Import the test function from our DLL
    [DllImport("LSQuicUnityPlugin")]
    private static extern int LSQuic_TestInit();

    void Awake()
    {
        Debug.Log("LSQuicTest script is awake!");
    }

    void Start()
    {
        Debug.Log("=== STARTING LSQUIC TEST ===");
        try
        {
            Debug.Log("About to call LSQuic_TestInit...");
            int result = LSQuic_TestInit();
            Debug.Log($"LSQuic_TestInit returned: {result}");

            if (result == 0)
            {
                Debug.Log("SUCCESS: LSQUIC initialized successfully!");
            }
            else
            {
                Debug.LogError($"FAILED: LSQUIC initialization failed with error code: {result}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"EXCEPTION during LSQUIC initialization: {e.GetType().Name}");
            Debug.LogError($"Message: {e.Message}");
            Debug.LogError($"Stack Trace: {e.StackTrace}");
        }
        Debug.Log("=== LSQUIC TEST COMPLETE ===");
    }

    void OnEnable()
    {
        Debug.Log("LSQuicTest script was enabled!");
    }
}