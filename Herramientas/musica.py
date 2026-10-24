# Música ambiental de laboratorio, en bucle perfecto (todo el procesado es circular).
# Uso: python Herramientas/musica.py Assets/NanoLab/Audio/AmbienteLaboratorio.wav  (requiere numpy)
import numpy as np, wave, sys
sr = 44100
T = 24.0                      # segundos por acorde
acordes = [  # Dmaj9, Bm11, Gmaj7(#11), A6sus  (MIDI)
    [50, 57, 62, 66, 69, 73, 76],
    [47, 54, 62, 66, 69, 74, 76],
    [43, 50, 59, 62, 66, 69, 73],
    [45, 52, 62, 64, 66, 69, 74],
]
N = int(sr * T * len(acordes)); t = np.arange(N) / sr
rng = np.random.default_rng(11)
hz = lambda m: 440.0 * 2 ** ((m - 69) / 12)
L = np.zeros(N); R = np.zeros(N)

# Envolvente circular de cada acorde (fundido de 6 s con el siguiente).
def env_acorde(i, X=6.0):
    c = (i * T + T / 2)            # centro
    d = ((t - c + N / sr / 2) % (N / sr)) - N / sr / 2   # distancia circular
    a = np.clip((T / 2 + X / 2 - np.abs(d)) / X, 0, 1)
    return 0.5 - 0.5 * np.cos(np.pi * a)

for i, ac in enumerate(acordes):
    e = env_acorde(i)
    for k, m in enumerate(ac):
        f = hz(m)
        amp = 0.11 if m < 52 else 0.07 if m < 64 else 0.045
        # Pad: fundamental + armónicos suaves, dos osciladores desafinados (coro) por canal.
        for ch, det in ((L, -0.12), (R, 0.12)):
            for h, ah in ((1, 1.0), (2, 0.28), (3, 0.09)):
                ph = rng.uniform(0, 2 * np.pi)
                trem = 1 + 0.08 * np.sin(2 * np.pi * (0.05 + 0.013 * k) * t * 1 + ph)
                ch += amp * ah * e * trem * np.sin(2 * np.pi * (f * h + det * h) * t + ph)

# Campanas suaves dispersas (pentatónica de re, octava aguda), con colas circulares.
penta = [74, 76, 78, 81, 83, 86, 88, 90]
for _ in range(34):
    t0 = rng.uniform(0, N / sr); m = rng.choice(penta); f = hz(m)
    dur = 4.0; n = int(dur * sr); tt = np.arange(n) / sr
    s = (np.sin(2*np.pi*f*tt) + 0.25*np.sin(2*np.pi*2.76*f*tt)*np.exp(-tt*3)) * np.exp(-tt * 1.3) * np.minimum(1, tt * 200)
    s *= rng.uniform(0.025, 0.05)
    pan = rng.uniform(0.2, 0.8); idx = (int(t0 * sr) + np.arange(n)) % N
    np.add.at(L, idx, s * (1 - pan)); np.add.at(R, idx, s * pan)

# Murmullo de ventilación: ruido rosado filtrado (FFT circular).
def filtrar(x, corte, pend=1.0):
    X = np.fft.rfft(x); f = np.fft.rfftfreq(len(x), 1 / sr)
    g = 1 / np.sqrt(1 + (f / corte) ** (2 * pend)); g[f < 30] = 0
    return np.fft.irfft(X * g / np.sqrt(np.maximum(f, 20) / 20), len(x))
for ch in (L, R):
    ruido = filtrar(rng.standard_normal(N), 380, 2)
    ch += 0.018 * ruido / np.std(ruido)

# Reverb: convolución circular con cola de ruido que decae (sala grande).
def reverb(x, seg=3.2, mezcla=0.35):
    n = int(seg * sr); ir = rng.standard_normal(n) * np.exp(-np.arange(n) / sr * 6.9 / seg)
    ir = filtrar(np.concatenate([ir, np.zeros(N - n)]), 3500, 1)
    ir /= np.sqrt(np.sum(ir ** 2))
    w = np.fft.irfft(np.fft.rfft(x) * np.fft.rfft(ir), N)
    return x * (1 - mezcla) + w * mezcla
L, R = reverb(L), reverb(R)

# Suavizado general (sin agudos ásperos) y normalización.
L, R = filtrar(L, 6000, 1), filtrar(R, 6000, 1)
pico = max(np.max(np.abs(L)), np.max(np.abs(R)))
L, R = L / pico * 0.6, R / pico * 0.6
datos = (np.stack([L, R], 1) * 32767).astype('<i2').tobytes()
with wave.open(sys.argv[1], 'wb') as w:
    w.setnchannels(2); w.setsampwidth(2); w.setframerate(sr); w.writeframes(datos)
print('ok', N / sr, 's', len(datos) / 1e6, 'MB')
