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
        controls.XR.HideUI.performed += _ => HideUI();
        controls.XR.ShowUI.performed += _ => ShowUI();
    }

    void OnEnable()
    {
        controls.Enable();
    }

    void OnDisable()
    {
        controls.Disable();
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
