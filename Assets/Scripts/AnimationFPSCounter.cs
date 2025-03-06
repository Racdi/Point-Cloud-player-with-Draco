using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AnimationFPSCounter : MonoBehaviour
{
    public TMP_Text Text;

    private Dictionary<int, string> CachedNumberStrings = new();

    private int[] _frameRateSamples;
    private int _cacheNumbersAmount = 300;
    private int _averageFromAmount = 30;
    private int _averageCounter;
    private int _currentAveraged;
    private float timeOfLastTick = 0;

    void Awake()
    {
        // Cache strings and create array
        {
            for (int i = 0; i < _cacheNumbersAmount; i++)
            {
                CachedNumberStrings[i] = i.ToString();
            }

            _frameRateSamples = new int[_averageFromAmount];
        }
    }

    void Update()
    {
        /*
        // Sample
        {
            var currentFrame = (int)Math.Round(1f / DeltaType switch
            {
                DeltaTimeType.Smooth => Time.smoothDeltaTime,
                DeltaTimeType.Unscaled => Time.unscaledDeltaTime,
                _ => Time.unscaledDeltaTime
            });
            _frameRateSamples[_averageCounter] = currentFrame;
        }
        */

        
    }
    public void Iterate(float deltatime)
    {
        var currentFrame = (int)Math.Round(1f / deltatime);
        _frameRateSamples[_averageCounter] = currentFrame;

        //_averageCounter = (_averageCounter + 1) % _averageFromAmount;
        //Text.text = currentFrame.ToString();
        
        // Average
        {
            var average = 0f;

            foreach (var frameRate in _frameRateSamples)
            {
                average += frameRate;
            }

            _currentAveraged = (int)Math.Round(average / _averageFromAmount);
            _currentAveraged += 1;
            _averageCounter = (_averageCounter + 1) % _averageFromAmount;
        }

        // Assign to UI
        {
            Text.text = "Anim FPS: " + _currentAveraged switch
            {
                var x when x >= 0 && x < _cacheNumbersAmount => CachedNumberStrings[x],
                var x when x >= _cacheNumbersAmount => $"> {_cacheNumbersAmount}",
                var x when x < 0 => "< 0",
                _ => "?"
            };
        }
        

    }

    public void Tick()
    {
        float currentTimeFrame = Time.realtimeSinceStartup - timeOfLastTick;
        timeOfLastTick = Time.realtimeSinceStartup;
        _frameRateSamples[_averageCounter] = (int)Math.Round(1f / currentTimeFrame);

        //_averageCounter = (_averageCounter + 1) % _averageFromAmount;
        //Text.text = currentFrame.ToString();

        // Average
        {
            var average = 0f;

            foreach (var frameRate in _frameRateSamples)
            {
                average += frameRate;
            }

            _currentAveraged = (int)Math.Round(average / _averageFromAmount);
            _currentAveraged += 1;
            _averageCounter = (_averageCounter + 1) % _averageFromAmount;
        }

        // Assign to UI
        {
            Text.text = "Anim FPS: " + _currentAveraged switch
            {
                var x when x >= 0 && x < _cacheNumbersAmount => CachedNumberStrings[x],
                var x when x >= _cacheNumbersAmount => $"> {_cacheNumbersAmount}",
                var x when x < 0 => "< 0",
                _ => "?"
            };
        }


    }
}
