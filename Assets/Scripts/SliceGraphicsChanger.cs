using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SliceGraphicsChanger : MonoBehaviour
{
    int currentSlice = 0;

    public GameObject[] sliceGraphics; 

    public void ChangeSlice(int slice)
    {
        if (slice < sliceGraphics.Length)
        {
            currentSlice = slice;
            foreach (GameObject graphic in sliceGraphics)
            {
                graphic.SetActive(false);
            }
            sliceGraphics[slice].SetActive(true);
        }
    }
}
