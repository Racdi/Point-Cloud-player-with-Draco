using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class StatusMonitor : MonoBehaviour
{
    public TMP_Text screenText;
    public DracoPlayer dracoPlayer;

    public void SetText(string textToSet)
    {
        screenText.text = textToSet;
    }
}
