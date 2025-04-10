using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class TextCanvasScript : MonoBehaviour
{
    public Controls controls;

    [SerializeField]
    private GameObject XRCanvas;

    void Awake()
    {
        controls.XR.EnableUI.performed += _ => SwitchUI();
    }

    void OnEnable()
    {
        //controls.Enable();
    }

    void OnDisable()
    {
        //controls.Disable();
    }
    void SwitchUI()
    {
        if (XRCanvas.activeInHierarchy)
        {
            HideUI();
        }
        else
        {
            ShowUI();
        }
    }
    void HideUI()
    {
        XRCanvas.SetActive(false);
    }

    void ShowUI()
    {
        XRCanvas.SetActive(true);
    }

}
