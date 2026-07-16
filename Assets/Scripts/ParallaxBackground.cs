using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ParallaxBackground : MonoBehaviour
{
    [System.Serializable]
    public class Layer
    {
        public string name;
        public Texture2D texture;
        public float speed = 10f;
        public Color color = Color.white;
    }

    public Canvas canvas;
    public bool autoLoad = true;
    public string resourcesPath = "Background/Forest";
    public Layer[] layers;
    public float globalSpeed = 6f;
    public bool useUnscaledTime = false;

    private RectTransform root;
    private List<LayerInstance> instances = new List<LayerInstance>();
    private float canvasWidth;

    class LayerInstance
    {
        public float speed;
        public RectTransform[] tiles;
    }

    void Start()
    {
        if (canvas == null) canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        Build();
    }

    void Build()
    {
        if (root != null) return;

        GameObject rootGO = new GameObject("ParallaxBackground", typeof(RectTransform));
        root = rootGO.GetComponent<RectTransform>();
        root.SetParent(canvas.transform, false);
        root.SetAsFirstSibling();
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;
        root.localScale = Vector3.one;

        RectTransform canvasRT = canvas.GetComponent<RectTransform>();
        canvasWidth = canvasRT.rect.width;

        if (autoLoad && (layers == null || layers.Length == 0))
            AutoLoadLayers();

        if (layers == null) return;

        for (int i = 0; i < layers.Length; i++)
        {
            if (layers[i].texture == null) continue;
            CreateLayerTiles(layers[i]);
        }
    }

    void AutoLoadLayers()
    {
        Texture2D[] textures = Resources.LoadAll<Texture2D>(resourcesPath);
        if (textures == null || textures.Length == 0)
        {
            Sprite[] sprites = Resources.LoadAll<Sprite>(resourcesPath);
            if (sprites != null && sprites.Length > 0)
            {
                textures = new Texture2D[sprites.Length];
                for (int i = 0; i < sprites.Length; i++)
                    textures[i] = sprites[i].texture;
            }
        }
        if (textures == null || textures.Length == 0) return;
        System.Array.Sort(textures, (a, b) => b.name.CompareTo(a.name));

        float baseSpeed = 25f;
        layers = new Layer[textures.Length];
        for (int i = 0; i < textures.Length; i++)
        {
            layers[i] = new Layer
            {
                name = textures[i].name,
                texture = textures[i],
                speed = baseSpeed * (i + 1) / textures.Length,
                color = Color.white
            };
        }
    }

    void CreateLayerTiles(Layer layer)
    {
        float aspect = (float)layer.texture.width / layer.texture.height;
        float width = canvasWidth;
        float height = width / aspect;

        LayerInstance inst = new LayerInstance
        {
            speed = layer.speed,
            tiles = new RectTransform[2]
        };

        for (int i = 0; i < 2; i++)
        {
            GameObject tile = new GameObject(layer.name + "_Tile" + i, typeof(RectTransform), typeof(RawImage));
            tile.transform.SetParent(root, false);

            RectTransform rt = tile.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(width, height);
            rt.anchoredPosition = new Vector2(i * width, 0f);

            RawImage img = tile.GetComponent<RawImage>();
            img.texture = layer.texture;
            img.color = layer.color;
            img.raycastTarget = false;

            inst.tiles[i] = rt;
        }

        instances.Add(inst);
    }

    void Update()
    {
        if (instances.Count == 0) return;
        if (canvas == null || root == null || instances[0].tiles[0] == null)
        {
            Rebuild();
            return;
        }

        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        foreach (var inst in instances)
        {
            float move = inst.speed * globalSpeed * dt;
            foreach (var tile in inst.tiles)
                tile.anchoredPosition += new Vector2(-move, 0f);

            foreach (var tile in inst.tiles)
            {
                if (tile.anchoredPosition.x <= -canvasWidth)
                    tile.anchoredPosition += new Vector2(2 * canvasWidth, 0f);
            }
        }
    }

    void Rebuild()
    {
        if (root != null)
        {
            try { Destroy(root.gameObject); }
            catch { }
        }
        root = null;
        instances.Clear();
        canvas = null;
        Start();
    }
}
