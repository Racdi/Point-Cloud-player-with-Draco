using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class ParticlesFromData : MonoBehaviour{
    public VisualEffect VFX;

    public float particleScale = 1;
    public float particleSize = 0.02f;


    // Use this for initialization

    private void Start()
    {
        VFX.SetFloat("Scale", particleScale);
        VFX.SetFloat("ParticleSize", particleSize);
        VFX.Play();
    }

    public void Set(List<Vector3> vertices, List<Color32> colors)
    {
        //Taken from PCX importer
        var _pointCount = vertices.Count;

        var width = Mathf.CeilToInt(Mathf.Sqrt(_pointCount));

        Texture2D _positionMap = new Texture2D(width, width, TextureFormat.RGBAHalf, false);
        _positionMap.name = "Position Map";
        _positionMap.filterMode = FilterMode.Point;

        Texture2D _colorMap = new Texture2D(width, width, TextureFormat.RGBA32, false);
        _colorMap.name = "Color Map";
        _colorMap.filterMode = FilterMode.Point;

        var i1 = 0;
        var i2 = 0U;

        for (var y = 0; y < width; y++){
            for (var x = 0; x < width; x++){
                var i = i1 < _pointCount ? i1 : (int)(i2 % _pointCount);
                var p = vertices[i];

                _positionMap.SetPixel(x, y, new Color(p.x, p.y, p.z));
                _colorMap.SetPixel(x, y, colors[i]);

                i1 ++;
                i2 += 132049U; // prime
            }
        }

        _positionMap.Apply(false, true);
        _colorMap.Apply(false, true);

        VFX.SetTexture("PositionMap", _positionMap);
        VFX.SetTexture("ColorMap", _colorMap);

        VFX.Reinit();
    }
}