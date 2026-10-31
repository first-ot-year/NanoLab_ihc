using UnityEngine;

// Avatar del jugador que se coloca a partir de la cabeza y las manos (lo usan las capturas y pruebas
// sin visor). Lo implementan AvatarBaymax (piezas rígidas) y AvatarBaymaxBebe (malla con esqueleto).
public interface IAvatarJugador
{
    void Posar(Vector3 cabezaPos, Quaternion cabezaRot, Pose? manoIzqPose, Pose? manoDerPose, float suelo);
}
