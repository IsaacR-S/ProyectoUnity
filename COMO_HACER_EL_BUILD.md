# Cómo hacer el build de Candle Fury (Windows)

> ⚠️ **La razón por la que rechazaron las entregas pasadas:** se envió SOLO el `.exe`.
> Un build de Unity NO funciona sin sus archivos acompañantes. Hay que comprimir
> **la carpeta completa** del build. Sigue estos pasos al pie de la letra.

## 1. Configuración previa (ya está hecha, solo verifica)

1. Abre **File → Build Settings** (o **Build Profiles** en Unity 6).
2. En **Scenes In Build** deben aparecer, en este orden y con el check activado:
   - `Assets/Scenes/Level1.unity` (índice 0)
   - `Assets/Scenes/Level2.unity` (índice 1)
   - Si falta alguna: abre la escena y pulsa **Add Open Scenes**.
3. En **Edit → Project Settings → Player** verifica:
   - **Company Name:** `IsaacDaniel` (nombre del equipo)
   - **Product Name:** `CandleFury`

## 2. Hacer el build

1. En **Build Settings**, plataforma **Windows, Mac, Linux** con:
   - **Target Platform:** Windows
   - **Architecture:** `x86_64` (Intel 64-bit)
2. Pulsa **Build**.
3. Cuando pida carpeta, crea una carpeta **NUEVA y VACÍA** fuera del proyecto,
   por ejemplo: `C:\Builds\CandleFury\`
   (nunca hagas el build dentro de la carpeta `Assets`).
4. Espera a que termine. La carpeta quedará así:

```
CandleFury/
├── CandleFury.exe            ← el ejecutable
├── UnityPlayer.dll           ← SIN esto el exe no abre
├── UnityCrashHandler64.exe
├── CandleFury_Data/          ← TODOS los assets del juego van aquí
│   └── ...
└── MonoBleedingEdge/         ← runtime de C#
    └── ...
```

## 3. Comprimir para entregar (el paso donde nos rechazaron)

1. Ve a la carpeta **PADRE** (ej. `C:\Builds\`).
2. Clic derecho sobre **la carpeta `CandleFury` completa** → *Comprimir en archivo ZIP*
   (o "Enviar a → Carpeta comprimida").
3. El ZIP debe contener la carpeta con **exe + UnityPlayer.dll + CandleFury_Data + MonoBleedingEdge**.

> ❌ **NUNCA** comprimas o envíes solo `CandleFury.exe` — sin `UnityPlayer.dll`,
> la carpeta `CandleFury_Data` y `MonoBleedingEdge` el juego **no abre** en la
> máquina del profesor.

## 4. Probar antes de entregar

1. Descomprime el ZIP en **otra carpeta** (o mejor, en otra PC).
2. Doble clic en `CandleFury.exe`.
3. Verifica: Level1 carga → al llegar a la Meta carga Level2 → al terminar Level2 sale la victoria.
4. Si abre y se juega completo, ese ZIP es el que se entrega.

## Controles (para el README de la entrega)

| Tecla | Acción |
|---|---|
| A/D o ←/→ | Moverse |
| W / ↑ / Espacio | Saltar (doble salto) |
| S / ↓ | Agacharse |
| Shift Izq. | Dash |
| Z | Espadazo (gratis) |
| X | Pulso de llama (cuesta cera) |
| C | Proyectil de llama (cuesta cera) |
