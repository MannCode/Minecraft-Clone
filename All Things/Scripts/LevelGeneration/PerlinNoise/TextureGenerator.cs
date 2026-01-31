using UnityEditor;
using UnityEngine;

using Icaria.Engine.Procedural;
using Unity.Mathematics;
using Unity.AppUI.UI;

public class TextureGenerator : MonoBehaviour
{
    public MeshGenerator meshGenerator;
    public int size = 20;
    float scale;
    public bool isColored = true;
    public int TextureWidth;
    public int TextureHeight;

    public bool show_ctnt_noise = false;
    public bool show_ersn_noise = false;
    public bool show_pv_noise = false;

    public bool useCtnt_HeightCurve = false;
    public bool useErsn_HeightCurve = false;
    public bool usePv_HeightCurve = false;

    public float offsetX;
    public float offsetY;

    Texture2D texture;
    Renderer rend;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        scale = meshGenerator.c_size * size;
        offsetX = offsetX + meshGenerator.randomOffset.x;
        offsetY = offsetY + meshGenerator.randomOffset.z;

        rend = GetComponent<Renderer>();
        texture = new Texture2D(TextureWidth, TextureHeight);
        transform.localScale = new Vector3(scale / 10f, 1, scale / 10f);
        // transform.position = new Vector3(TextureWidth / 2f, 0, TextureHeight / 2f);
        rend.material.mainTexture = texture;
        GenerateTexture();
    }

    // Update is called once per frame
    void Update()
    {
        scale = meshGenerator.c_size * size;
        transform.localScale = new Vector3(scale / 10f, 1, scale / 10f);
        // transform.position = new Vector3(TextureWidth / 2f, 0, TextureHeight / 2f);
        GenerateTexture();
    }

    void GenerateTexture()
    {
        for (int x = 0; x < TextureWidth; x++)
        {
            for (int y = 0; y < TextureHeight; y++)
            {
                // make it scale from center
                float xCoord = offsetX + (x - TextureWidth / 2f) * (scale / TextureWidth);
                float yCoord = offsetY + (y - TextureHeight / 2f) * (scale / TextureHeight);

                float ctnt_height = 0;
                float ersn_height = 0;
                float pv_height = 0;
                float _amplitude = 1;
                float _ctnt_frequency = meshGenerator.Continentalnessfrequency;
                float _ersn_frequency = meshGenerator.erosionFrequency;
                float _pv_frequency = meshGenerator.pvFrequency;
                int _octaves = 7;
                for (int i = 0; i < _octaves; i++)
                {
                    ctnt_height += _amplitude * IcariaNoise.GradientNoise(xCoord * _ctnt_frequency, yCoord * _ctnt_frequency, meshGenerator.seed);
                    ersn_height += _amplitude * IcariaNoise.GradientNoise(xCoord * _ersn_frequency + 10000f, yCoord * _ersn_frequency + 10000f, meshGenerator.seed);
                    pv_height += _amplitude * IcariaNoise.GradientNoise(xCoord * _pv_frequency + 20000f, yCoord * _pv_frequency + 20000f, meshGenerator.seed);
                    _amplitude *= 0.5f;
                    _ctnt_frequency *= 2;
                    _ersn_frequency *= 2;
                    _pv_frequency *= 2;
                }

                ctnt_height = (ctnt_height + 1) / 2f;
                ersn_height = (ersn_height + 1) / 2f;
                pv_height = math.abs(pv_height);

                if (useCtnt_HeightCurve)
                    ctnt_height = meshGenerator.ContinentalnessHeightCurve.Evaluate(ctnt_height);
                    // ctnt_height = ctnt_height * 2f - 1f;
                if (useErsn_HeightCurve)
                    ersn_height = meshGenerator.erosionHeightCurve.Evaluate(ersn_height);
                    // ersn_height = ersn_height * 2f - 1f;
                if (usePv_HeightCurve)
                    pv_height = meshGenerator.pvHeightCurve.Evaluate(pv_height);
                    // pv_height = pv_height * 2f - 1f;
                
                float f_height = 0;
                int total = 0;
                if (show_ctnt_noise)
                {
                    f_height += ctnt_height;
                    total++;
                }
                if (show_ersn_noise)
                {
                    f_height += ersn_height;
                    total++;
                }
                if (show_pv_noise)
                {
                    f_height += pv_height;
                    total++;
                }
                if (total == 0) total = 1;
                f_height = f_height / total;

                // Color color = new Color(f_height, f_height, f_height);
                Color color;

                if (!isColored)
                {
                    color = new Color(f_height, f_height, f_height);
                }
                else
                {
                    int height = Mathf.FloorToInt(Mathf.Lerp(meshGenerator.waterLevel - 32, meshGenerator.c_height - 5, f_height));
                    if (height < meshGenerator.waterLevel)
                    {
                        // blue tinted
                        color = Color.Lerp(new Color(0f, 0f, 0.5f), new Color(0.2f, 0.5f, 1f), height * (1f / meshGenerator.waterLevel));
                    }
                    else if (height < meshGenerator.waterLevel + 5f)
                    {
                        // yellow tinted
                        color = Color.Lerp(new Color(0.2f, 0.5f, 1f), new Color(1f, 1f, 0.2f), (height - meshGenerator.waterLevel) * (1f / 5f));
                    }
                    else
                    {
                        // green tinted
                        color = Color.Lerp(new Color(1f, 1f, 0.2f), new Color(0f, 0.5f, 0f), (height - meshGenerator.waterLevel - 5f) * (1f / (meshGenerator.c_height - meshGenerator.waterLevel - 5f)));
                    }
                }
                texture.SetPixel(x, y, color);
            }
        }
        texture.Apply();
    }
}
