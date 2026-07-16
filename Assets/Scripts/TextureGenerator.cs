using UnityEngine;

public static class TextureGenerator
{
    public static Sprite CreateCircleSprite(int size, Color color)
    {
        Texture2D tex = new Texture2D(size, size);
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dx = x - size / 2f;
            float dy = y - size / 2f;
            float dist = Mathf.Sqrt(dx * dx + dy * dy) / (size / 2f);
            float alpha = Mathf.Clamp01(1f - dist);
            tex.SetPixel(x, y, new Color(color.r, color.g, color.b, color.a * alpha));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    public static Sprite CreateRingSprite(int size, float innerRadius, Color color)
    {
        Texture2D tex = new Texture2D(size, size);
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dx = x - size / 2f;
            float dy = y - size / 2f;
            float dist = Mathf.Sqrt(dx * dx + dy * dy) / (size / 2f);
            float alpha = dist < innerRadius ? 0f : Mathf.Clamp01(1f - (dist - innerRadius) / (1f - innerRadius));
            tex.SetPixel(x, y, new Color(color.r, color.g, color.b, color.a * alpha));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    public static Sprite CreateRadialGradientSprite(int size, Color center, Color edge)
    {
        Texture2D tex = new Texture2D(size, size);
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dx = x - size / 2f;
            float dy = y - size / 2f;
            float dist = Mathf.Sqrt(dx * dx + dy * dy) / (size / 2f);
            Color c = Color.Lerp(center, edge, dist);
            tex.SetPixel(x, y, c);
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    public static Sprite CreateCrackSprite(int size, Color color)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
            tex.SetPixel(x, y, Color.clear);

        int cracks = 8;
        for (int c = 0; c < cracks; c++)
        {
            Vector2 pos = new Vector2(size / 2, size / 2);
            Vector2 dir = Random.insideUnitCircle.normalized;
            int length = Random.Range(size / 3, size / 2);
            for (int i = 0; i < length; i++)
            {
                pos += dir * 1.5f;
                dir = (dir + Random.insideUnitCircle * 0.4f).normalized;
                for (int r = -1; r <= 1; r++)
                for (int s = -1; s <= 1; s++)
                {
                    int px = Mathf.Clamp((int)pos.x + r, 0, size - 1);
                    int py = Mathf.Clamp((int)pos.y + s, 0, size - 1);
                    tex.SetPixel(px, py, color);
                }
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }
}
