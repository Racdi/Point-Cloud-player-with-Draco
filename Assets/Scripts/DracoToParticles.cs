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
    public float particleSize = 0.02f;


    // Use this for initialization

    private void Start()
    {
        VFX.SetFloat("Scale", particleScale);
        VFX.SetFloat("ParticleSize", particleSize);
        VFX.Play();
    }

    public async Task Set(List<Vector3> vertices, List<Color32> colors)
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

    public void ParallelSet(List<Vector3> vertices, List<Color32> colors)
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

        NativeArray<int> arrayOfI = new NativeArray<int>(width * width, Allocator.Persistent);

        pctextureJob job = new pctextureJob()
        {
            _pointCount = _pointCount,
            width = width,
            i = arrayOfI
        };

        JobHandle jobHandle = job.Schedule(width, 64);

        jobHandle.Complete();

        for (int j = 0; j < width * width; j++)
        {
            var i = arrayOfI[j];
            var p = vertices[i];
            _positionMap.SetPixel(j % width, j / width, new Color(p.x, p.y, p.z));
            _colorMap.SetPixel(j % width, j / width, colors[i]);

        }
        arrayOfI.Dispose();


        _positionMap.Apply(false, true);
        _colorMap.Apply(false, true);

        VFX.SetTexture("PositionMap", _positionMap);
        VFX.SetTexture("ColorMap", _colorMap);

        VFX.Reinit();
    }

    [BurstCompile]
    public struct pctextureJob : IJobParallelFor
    {

        [ReadOnly] public int _pointCount;
        [ReadOnly] public int width;

        public NativeArray<int> i;

        public void Execute(int pos)
        {
            var x = pos % width;
            var y = pos / width;
            var i1 = y * width + x;
            var i2 = i1 * 132049U;
            var auxi = i1 < _pointCount ? i1 : (int)(i2 % _pointCount);

            i[pos] = auxi;
        }
    }
}