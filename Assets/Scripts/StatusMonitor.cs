using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class StatusMonitor : MonoBehaviour
{
    public TMP_Text screenText;
    
    public void SetText(string textToSet)
    {
        screenText.text = textToSet;
    }
}
