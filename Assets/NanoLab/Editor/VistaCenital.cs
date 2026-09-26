using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Renderiza una vista cenital ortográfica de una escena (Logs/Capturas/cenital_*.png)
// con una rejilla de 1 m (líneas rojas cada 5 m) para planificar la distribución.
// Mapeo: píxel (px, py) -> mundo x = minX + px / PixelesPorMetro, z = maxZ - py / PixelesPorMetro.
public static class VistaCenital
{
    public const int PixelesPorMetro = 60;

    [MenuItem("NanoLab/Vista cenital (Laboratorio)")]
    public static void Laboratorio() => Renderizar("Assets/NanoLab/Scenes/Laboratorio.unity");

    public static void Renderizar(string scenePath)
    {
        EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        var rs = Object.FindObjectsOfType<MeshRenderer>();
        var b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);

        // Oculta el techo (todo lo que esté por encima de 3 m) para ver el interior.
        foreach (var r in rs) if (r.bounds.min.y > 2.6f) r.enabled = false;

        float minX = Mathf.Floor(b.min.x), maxX = Mathf.Ceil(b.max.x);
        float minZ = Mathf.Floor(b.min.z), maxZ = Mathf.Ceil(b.max.z);
        int w = (int)((maxX - minX) * PixelesPorMetro), h = (int)((maxZ - minZ) * PixelesPorMetro);

        var go = new GameObject("CamCenital");
        var cam = go.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = (maxZ - minZ) / 2f;
        cam.aspect = (float)w / h;
        cam.transform.position = new Vector3((minX + maxX) / 2f, 20f, (minZ + maxZ) / 2f);
        cam.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        var rt = new RenderTexture(w, h, 24);
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);

        // Rejilla (en coordenadas de textura: y crece hacia arriba = z crece).
        for (float x = minX; x <= maxX; x += 1f)
        {
            int px = (int)((x - minX) * PixelesPorMetro);
            var c = Mathf.Approximately(x % 5f, 0f) ? Color.red : new Color(1, 1, 0, 1);
            for (int y = 0; y < h; y++) if (px < w) tex.SetPixel(px, y, Color.Lerp(tex.GetPixel(px, y), c, 0.6f));
        }
        for (float z = minZ; z <= maxZ; z += 1f)
        {
            int py = (int)((z - minZ) * PixelesPorMetro);
            var c = Mathf.Approximately(z % 5f, 0f) ? Color.red : new Color(1, 1, 0, 1);
            for (int x = 0; x < w; x++) if (py < h) tex.SetPixel(x, py, Color.Lerp(tex.GetPixel(x, py), c, 0.6f));
        }
        tex.Apply();

        string dir = Path.Combine(Directory.GetCurrentDirectory(), "Logs", "Capturas");
        Directory.CreateDirectory(dir);
        string nombre = "cenital_" + Path.GetFileNameWithoutExtension(scenePath) + ".png";
        File.WriteAllBytes(Path.Combine(dir, nombre), tex.EncodeToPNG());
        RenderTexture.active = null;
        Debug.Log($"[Cenital] {nombre}: x {minX}..{maxX}, z {minZ}..{maxZ}, {w}x{h}px, {PixelesPorMetro} px/m");
    }
}
