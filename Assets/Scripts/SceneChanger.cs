using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChanger : MonoBehaviour
{

    public void ChangeSceneButton(string sceneName)
    {
        StartCoroutine(ChangeScene(sceneName));
    }
    public void ChangeSceneButton(int sceneID)
    {
        StartCoroutine(ChangeScene(sceneID));
    }

    IEnumerator ChangeScene(string sceneName)
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);

        // Wait until the asynchronous scene fully loads
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
    }

    IEnumerator ChangeScene(int sceneID)
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneID);

        // Wait until the asynchronous scene fully loads
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
    }

}
