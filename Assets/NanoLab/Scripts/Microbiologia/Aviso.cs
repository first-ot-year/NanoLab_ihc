using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Mensajes cortos que flotan junto a la acción (feedback textual inmediato) y se desvanecen.
// Color + prefijo de texto, para no depender solo del color.
public static class Aviso
{
    public enum Tipo { Info, Ok, Error, Pista }

    static readonly List<Item> pool = new List<Item>();
    static Transform raiz;

    public static void Mostrar(string texto, Vector3 pos, Tipo tipo = Tipo.Info, float duracion = 5f)
    {
        if (raiz == null)
        {
            raiz = new GameObject("Avisos").transform;
            pool.Clear();
        }
        // Reutiliza el aviso que ya está en ese sitio (no se apilan) o el más antiguo.
        Item elegido = null;
        foreach (var it in pool) if (it != null && it.Activo && (it.transform.position - pos).sqrMagnitude < 0.09f) { elegido = it; break; }
        if (elegido == null) foreach (var it in pool) if (it != null && !it.Activo) { elegido = it; break; }
        if (elegido == null && pool.Count < 5) { elegido = Crear(); pool.Add(elegido); }
        if (elegido == null) { elegido = pool[0]; foreach (var it in pool) if (it.Inicio < elegido.Inicio) elegido = it; }
        elegido.Mostrar(texto, pos, tipo, duracion);
    }

    static Item Crear()
    {
        var go = new GameObject("Aviso");
        go.transform.SetParent(raiz, false);
        var t = go.AddComponent<TextMeshPro>();
        t.fontSize = 0.3f;
        t.alignment = TextAlignmentOptions.Center;
        t.enableWordWrapping = true;
        t.rectTransform.sizeDelta = new Vector2(0.7f, 0.2f);
        t.overflowMode = TextOverflowModes.Overflow;
        t.fontStyle = FontStyles.Bold;
        t.outlineWidth = 0.25f;
        t.outlineColor = new Color32(0, 0, 0, 230);
        var item = go.AddComponent<Item>();
        item.texto = t;
        go.SetActive(false);
        return item;
    }

    public class Item : MonoBehaviour
    {
        public TextMeshPro texto;
        public float Inicio { get; private set; }
        public bool Activo => gameObject.activeSelf;
        float fin;
        Color color;

        public void Mostrar(string s, Vector3 pos, Tipo tipo, float duracion)
        {
            string prefijo;
            switch (tipo)
            {
                case Tipo.Ok: color = new Color(0.35f, 1f, 0.55f); prefijo = "BIEN · "; break;
                case Tipo.Error: color = new Color(1f, 0.5f, 0.4f); prefijo = "ATENCIÓN · "; break;
                case Tipo.Pista: color = new Color(1f, 0.85f, 0.35f); prefijo = "PISTA · "; break;
                default: color = new Color(0.6f, 0.9f, 1f); prefijo = ""; break;
            }
            texto.text = prefijo + s;
            texto.color = color;
            transform.position = pos;
            Inicio = Time.time;
            fin = Time.time + duracion;
            gameObject.SetActive(true);
            Orientar();
        }

        void LateUpdate()
        {
            if (Time.time > fin) { gameObject.SetActive(false); return; }
            float a = Mathf.Clamp01((fin - Time.time) / 0.6f) * Mathf.Clamp01((Time.time - Inicio) / 0.15f);
            var c = color; c.a = a;
            texto.color = c;
            transform.position += Vector3.up * Time.deltaTime * 0.01f;
            Orientar();
        }

        void Orientar()
        {
            var cab = ManosRig.Cabeza;
            if (cab == null) return;
            var d = transform.position - cab.position;
            if (d.sqrMagnitude > 1e-4f) transform.rotation = Quaternion.LookRotation(d);
        }
    }
}
