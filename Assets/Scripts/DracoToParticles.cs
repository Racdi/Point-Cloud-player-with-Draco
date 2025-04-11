using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine.Events;
using System.Threading.Tasks;
using Unity.Jobs;
using Unity.Collections;
using UnityEngine.Jobs;
using UnityEngine;
using UnityEngine.VFX;

public class DracoToParticles : MonoBehaviour {
    public VisualEffect VFX;

    public float particleScale = 1;
    public float particleSize = 1.7f;
    public float particleDistanceCompensation = 1.7f;

    private float currentDistance;

    public GameObject distanceReference;

    private Texture2D _positionMap;
    private Texture2D _colorMap;

    // Use this for initialization

    private void Start()
    {
        VFX.SetFloat("Scale", particleScale);
        VFX.SetFloat("ParticleSize", particleSize);
        VFX.Play();
    }

    public void Update()
    {
        currentDistance = distanceReference.transform.position.magnitude;
        if(currentDistance > 0)
        {
            particleDistanceCompensation = (Mathf.Pow(currentDistance, 1.5f)) / 4;
        }
        else
        {
            particleDistanceCompensation = 0.25f;
        }

            ResetParticleSize(particleSize / particleDistanceCompensation);
    }

    public void ChangeParticleSize(float newSize)
    {
        particleSize = newSize;
    }

    private void ResetParticleSize(float Size)
    {
        VFX.SetFloat("ParticleSize", Size);
    }

    public async Task Set(List<Vector3> vertices, List<Color32> colors)
    {
        if(_positionMap != null)
        {
            Destroy(_positionMap);
            Destroy(_colorMap);
        }
        //Taken from PCX importer
        var _pointCount = vertices.Count;

        var width = Mathf.CeilToInt(Mathf.Sqrt(_pointCount));

        _positionMap = new Texture2D(width, width, TextureFormat.RGBAHalf, false);
        _positionMap.name = "Position Map";
        _positionMap.filterMode = FilterMode.Point;

        _colorMap = new Texture2D(width, width, TextureFormat.RGBA32, false);
        _colorMap.name = "Color Map";
        _colorMap.filterMode = FilterMode.Point;

        var i1 = 0;
        var i2 = 0U;

        Color[] vertexArray = new Color[width*width];
        Color[] colorArray = new Color[width*width];

        for (var y = 0; y < width; y++) {
            for (var x = 0; x < width; x++) {
                var i = i1 < _pointCount ? i1 : (int)(i2 % _pointCount);
                var p = vertices[i];

                vertexArray[x + (y * width)] = new Color(p.x, p.y, p.z);
                colorArray[x + (y * width)] = colors[i];
                //_positionMap.SetPixel(x, y, new Color(p.x, p.y, p.z));
                //_colorMap.SetPixel(x, y, colors[i]);

                i1++;
                i2 += 132049U; // prime
            }
        }
        _positionMap.SetPixels(vertexArray);
        _colorMap.SetPixels(colorArray);

        _positionMap.Apply(false, true);
        _colorMap.Apply(false, true);

        VFX.SetTexture("PositionMap", _positionMap);
        VFX.SetTexture("ColorMap", _colorMap);

        VFX.Reinit();
    }
    
    public void Set(Mesh pcMesh)
    {
        VFX.SetMesh("PointCloudMesh", pcMesh);

        VFX.Reinit();
    }
}