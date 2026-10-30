using UnityEngine;

// Ajustes de accesibilidad del jugador (RNF-07): volumen de la música y de los efectos y modo sentado.
// Se guardan entre sesiones (PlayerPrefs). Los cambia el panel ☰ (MenuAjustes).
public static class AjustesNanoLab
{
    const string ClaveMusica = "NanoLab.VolumenMusica", ClaveEfectos = "NanoLab.VolumenEfectos";
    const string ClaveSentado = "NanoLab.ModoSentado", ClaveAltura = "NanoLab.AlturaSentado";

    static bool cargado;
    static float musica = 1f, efectos = 1f, alturaSentado;
    static bool sentado;

    public static event System.Action Cambiado;

    public static float VolumenMusica { get { Cargar(); return musica; } set { Cargar(); musica = Paso(value); Guardar(); } }
    public static float VolumenEfectos { get { Cargar(); return efectos; } set { Cargar(); efectos = Paso(value); Guardar(); } }
    public static bool ModoSentado { get { Cargar(); return sentado; } set { Cargar(); sentado = value; Guardar(); } }
    // Cuánto se sube la vista en modo sentado (m), calibrado por JugadorNanoLab.
    public static float AlturaSentado { get { Cargar(); return alturaSentado; } set { Cargar(); alturaSentado = Mathf.Clamp(value, 0f, 0.9f); Guardar(); } }

    static float Paso(float v) => Mathf.Round(Mathf.Clamp01(v) * 10f) / 10f;

    static void Cargar()
    {
        if (cargado) return;
        cargado = true;
        try
        {
            musica = PlayerPrefs.GetFloat(ClaveMusica, 1f);
            efectos = PlayerPrefs.GetFloat(ClaveEfectos, 1f);
            sentado = PlayerPrefs.GetInt(ClaveSentado, 0) == 1;
            alturaSentado = PlayerPrefs.GetFloat(ClaveAltura, 0f);
        }
        catch (System.Exception e) { Debug.LogWarning("[Ajustes] No se pudieron leer: " + e.Message); }
    }

    static void Guardar()
    {
        try
        {
            PlayerPrefs.SetFloat(ClaveMusica, musica);
            PlayerPrefs.SetFloat(ClaveEfectos, efectos);
            PlayerPrefs.SetInt(ClaveSentado, sentado ? 1 : 0);
            PlayerPrefs.SetFloat(ClaveAltura, alturaSentado);
            PlayerPrefs.Save();
        }
        catch (System.Exception e) { Debug.LogWarning("[Ajustes] No se pudieron guardar: " + e.Message); }
        Cambiado?.Invoke();
    }

    // Para las pruebas: vuelve a los valores por defecto sin tocar lo guardado.
    public static void Restablecer()
    {
        cargado = true;
        musica = 1f; efectos = 1f; sentado = false; alturaSentado = 0f;
        Cambiado?.Invoke();
    }
}
