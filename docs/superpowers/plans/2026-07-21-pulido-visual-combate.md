# Pulido Visual de Combate — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Añadir feedback visual de combate a "Candle Fury": bola de fuego pixelart animada con luz y estela para el proyectil del jugador, brazo overlay procedural en los 3 ataques, y rediseño visual + animación procedural del Royal Pyromancer.

**Architecture:** Todo el arte es PNG pixelart generado por script Python/PIL y cableado por scripts de editor idempotentes (menú "Candle Fury"), siguiendo la convención existente (`SpriteAssigner`). La animación es 100% procedural por código (corrutinas), sin Animator. Spec aprobada: `docs/superpowers/specs/2026-07-21-pulido-visual-combate-design.md`.

**Tech Stack:** Unity 6000.4.0f1 (instalado en `/Applications/Unity/Hub/Editor/6000.4.0f1/`), URP 17.4 con Renderer2D/Light2D, C#, Python 3 + Pillow para generar arte.

## Global Constraints

- **Proyecto:** `/Users/isaac_rs/Desktop/ProyectoUnity`, rama `lab10`. Todos los paths de abajo son relativos a esa raíz salvo que empiecen con `/`.
- **Sin tests unitarios:** el proyecto no tiene Unity Test Framework y la spec aprobada define verificación por compilación batch + ejecución batch de menús de editor + checklist manual en Play. El ciclo de cada task es: escribir código → compilar en batch (0 errores `error CS`) → ejecutar método batch cuando aplique → verificar con grep/inspección → commit.
- **Unity batch:** `UNITY="/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity"`. Si un comando batch falla con "It looks like another Unity instance is running with this project open", el editor Unity está abierto con este proyecto: ciérralo (o pide al usuario cerrarlo) y reintenta. No uses la versión 6000.0.42f1.
- **Logs batch:** escribir siempre a `/private/tmp/claude-501/-Users-isaac-rs-Desktop-ProyectoUnity/657453b5-4d48-4157-9b05-f9439642064f/scratchpad/` (referido abajo como `$SCRATCH`).
- **Commits:** Conventional Commits, un commit por task, incluyendo siempre los `.meta` generados por Unity (`git add -A Assets` tras cada batch).
- **Convención de importación pixelart:** Point filter, sin compresión, Sprite/Single, PPU = `Round(alto_px / alto_mundo)`. Personajes usan material `Sprite-Lit-Default`; el proyectil usa `Sprite-Unlit-Default` (auto-emisivo).
- **Flip por escala:** Player y enemigos voltean negando `localScale.x`; todo hijo visual y toda animación de escala debe preservar el **signo** de `localScale.x`.
- **Colores clave:** cian proyectil/llama jugador `#4AD4FF` = `Color(0.290f, 0.831f, 1f)`; magenta boss `#FF4080` = `Color(1f, 0.251f, 0.502f)`.

---

### Task 1: Generar el arte pixelart (8 PNGs)

**Files:**
- Create: `$SCRATCH/gen_art.py` (generador; NO entra al repo)
- Create: `Assets/Sprites/Effects/fireball_0.png` … `fireball_3.png` (16×16)
- Create: `Assets/Sprites/Effects/arm_sword.png` (24×10), `Assets/Sprites/Effects/arm_cast.png` (16×10)
- Create: `Assets/Sprites/Characters/royal_pyromancer_idle.png`, `Assets/Sprites/Characters/royal_pyromancer_cast.png` (84×158)

**Interfaces:**
- Consumes: nada.
- Produces: los 8 PNGs con esos nombres y dimensiones exactas — Tasks 3, 4 y 5 los cargan por path exacto.

- [ ] **Step 1: Crear venv con Pillow**

```bash
SCRATCH="/private/tmp/claude-501/-Users-isaac-rs-Desktop-ProyectoUnity/657453b5-4d48-4157-9b05-f9439642064f/scratchpad"
python3 -m venv "$SCRATCH/.venv" && "$SCRATCH/.venv/bin/pip" -q install pillow
```

Expected: instala Pillow sin errores. (Fallback si venv falla: `pip3 install --user pillow` y usar `python3` directo.)

- [ ] **Step 2: Escribir el generador `$SCRATCH/gen_art.py`**

Contenido completo:

```python
#!/usr/bin/env python3
"""Genera el arte pixelart de Candle Fury: fireball (4 frames), brazos (2), boss (2 poses)."""
import math, random, os
from PIL import Image, ImageDraw

ROOT = "/Users/isaac_rs/Desktop/ProyectoUnity/Assets/Sprites"

# ── Paleta ────────────────────────────────────────────────────────────────
T    = (0, 0, 0, 0)          # transparente
OUT  = (26, 22, 38, 255)     # contorno
WAX  = (242, 230, 200, 255)  # cera clara (cuerpo del príncipe)
WAXS = (196, 178, 142, 255)  # cera sombra
STL  = (214, 226, 240, 255)  # acero claro
STLD = (130, 142, 170, 255)  # acero oscuro
GOLD = (224, 176, 64, 255)   # oro
CYAN = (74, 212, 255, 255)   # #4AD4FF llama del jugador
CYNL = (191, 247, 255, 255)  # cian claro
CYND = (30, 120, 200, 255)   # cian oscuro
CORE = (255, 255, 255, 255)  # núcleo blanco

def save(img, rel):
    path = os.path.join(ROOT, rel)
    os.makedirs(os.path.dirname(path), exist_ok=True)
    img.save(path)
    print(f"  {rel}: {img.size[0]}x{img.size[1]}")

# ── 1. Fireball: 16x16, 4 frames, bola a la derecha + cola a la izquierda ─
def fireball(frame):
    S = 16
    img = Image.new("RGBA", (S, S), T)
    rnd = random.Random(100 + frame)
    cx, cy = 10.5, 7.5
    r = 4.6 + 0.35 * math.sin(frame * math.pi / 2)   # radio "respira" por frame
    for y in range(S):
        for x in range(S):
            d = math.hypot(x - cx, y - cy)
            if   d < r * 0.38: img.putpixel((x, y), CORE)
            elif d < r * 0.65: img.putpixel((x, y), CYNL)
            elif d < r * 0.90: img.putpixel((x, y), CYAN)
            elif d < r:        img.putpixel((x, y), CYND)
    # cola: se estrecha hacia la izquierda, con flicker por frame
    for x in range(0, 9):
        half = (x / 9) * 3.4 - rnd.uniform(0.0, 1.1)
        for y in range(S):
            dy = abs(y - cy)
            if dy < half and img.getpixel((x, y))[3] == 0:
                col = CYAN if dy < half * 0.5 else CYND
                if rnd.random() < 0.12: col = CYNL   # chispas
                img.putpixel((x, y), col)
    return img

# ── 2. Brazos: grids ASCII exactos ────────────────────────────────────────
LEGEND = {".": T, "O": OUT, "W": WAX, "w": WAXS, "S": STL, "s": STLD,
          "G": GOLD, "C": CYAN, "c": CYNL}

ARM_SWORD = [  # 24x10 — hombro a la izq., espada hacia la derecha
    "........................",
    "..OOO...................",
    ".OWWWO..................",
    ".OWWWWOO...OGO..........",
    "..OwWWWWOO.OGOSSSSSCSSO.",
    "..OwWWWWWOOOGOSSSSSSSSSO",
    "...OwwWWO..OGOssssssssO.",
    "....OOOO....OGO.........",
    ".............O..........",
    "........................",
]

ARM_CAST = [  # 16x10 — palma abierta con brasa cian
    "................",
    "..OOO...........",
    ".OWWWO..........",
    ".OWWWWOO...CC...",
    "..OwWWWWOOCccC..",
    "..OwWWWWOOCccC..",
    "...OwwWWO..CC...",
    "....OOOO........",
    "................",
    "................",
]

def from_grid(rows):
    w, h = len(rows[0]), len(rows)
    for r in rows:
        assert len(r) == w, f"fila con ancho {len(r)} != {w}: {r!r}"
    img = Image.new("RGBA", (w, h), T)
    for y, row in enumerate(rows):
        for x, ch in enumerate(row):
            img.putpixel((x, y), LEGEND[ch])
    return img

# ── 3. Boss: dibujado a 42x79 y escalado x2 (nearest) a 84x158 ───────────
def boss(pose):  # pose: "idle" | "cast"
    W, H = 42, 79
    img = Image.new("RGBA", (W, H), T)
    d = ImageDraw.Draw(img)
    ROBE  = (92, 48, 110, 255)
    ROBED = (58, 32, 72, 255)
    TRIM  = (255, 64, 128, 255)   # #FF4080
    SKIN  = (34, 22, 46, 255)
    EYE   = (255, 120, 180, 255)
    WOOD  = (96, 64, 48, 255)
    ORB   = (255, 190, 220, 255)
    rnd = random.Random(7)

    # túnica: trapecio de hombros (y=18) a base ancha (y=H)
    for y in range(18, H):
        half = int(6 + (y - 18) * 10 / (H - 18))
        d.line([(21 - half, y), (21 + half, y)], fill=ROBE)
    for fx in (-9, -3, 3, 9):                      # pliegues verticales
        for y in range(30, H):
            if 0 <= 21 + fx < W: img.putpixel((21 + fx, y), ROBED)
    for x in range(4, 38):                          # dobladillo con trim
        img.putpixel((x, H - 1), OUT)
        if x % 4 < 2: img.putpixel((x, H - 2), TRIM)
    for i in range(14):                             # brasas en la túnica
        x, y = rnd.randrange(8, 34), rnd.randrange(34, H - 4)
        img.putpixel((x, y), TRIM if i % 2 else EYE)

    d.rectangle([12, 16, 30, 26], fill=ROBE, outline=OUT)   # torso
    d.rectangle([14, 6, 28, 17], fill=ROBED, outline=OUT)   # capucha
    d.rectangle([16, 10, 26, 16], fill=SKIN)                # rostro en sombra
    for ex in (18, 19, 23, 24): img.putpixel((ex, 12), EYE) # ojos brillantes
    for x in range(15, 28, 3): d.line([(x, 3), (x, 5)], fill=GOLD)  # corona
    d.line([(14, 5), (27, 5)], fill=GOLD)

    if pose == "idle":                              # bastón plantado
        d.line([(35, 12), (35, H - 4)], fill=WOOD, width=2)
        ox, oy = 35, 9
    else:                                           # bastón alzado + brazo
        d.line([(30, 20), (39, 4)], fill=WOOD, width=2)
        d.line([(24, 22), (31, 19)], fill=ROBE, width=3)
        ox, oy = 39, 3
    d.ellipse([ox - 3, oy - 3, ox + 3, oy + 3], fill=TRIM)  # orbe
    d.ellipse([ox - 1, oy - 1, ox + 1, oy + 1], fill=CORE)
    if pose == "cast":                              # destellos radiales
        for ang in range(0, 360, 45):
            px = ox + int(5 * math.cos(math.radians(ang)))
            py = oy + int(5 * math.sin(math.radians(ang)))
            if 0 <= px < W and 0 <= py < H: img.putpixel((px, py), ORB)

    return img.resize((W * 2, H * 2), Image.NEAREST)

if __name__ == "__main__":
    print("Generando arte pixelart:")
    for f in range(4):
        save(fireball(f), f"Effects/fireball_{f}.png")
    save(from_grid(ARM_SWORD), "Effects/arm_sword.png")
    save(from_grid(ARM_CAST),  "Effects/arm_cast.png")
    save(boss("idle"), "Characters/royal_pyromancer_idle.png")
    save(boss("cast"), "Characters/royal_pyromancer_cast.png")
    print("OK")
```

- [ ] **Step 3: Ejecutarlo**

```bash
"$SCRATCH/.venv/bin/python" "$SCRATCH/gen_art.py"
```

Expected: imprime los 8 archivos con sus dimensiones (`fireball_N: 16x16`, `arm_sword: 24x10`, `arm_cast: 16x10`, `royal_pyromancer_*: 84x158`) y `OK`.

- [ ] **Step 4: Inspección visual**

Abrir con la herramienta Read cada PNG generado (`Assets/Sprites/Effects/fireball_0.png`, `arm_sword.png`, `arm_cast.png`, `Assets/Sprites/Characters/royal_pyromancer_idle.png`, `royal_pyromancer_cast.png`) y confirmar: fireball = bola cian con núcleo blanco y cola a la izquierda; brazos = brazo crema con espada/palma; boss = figura encapuchada púrpura con corona, ojos rosa y bastón (idle vertical, cast en diagonal con orbe destellando). Si algo se ve roto (p. ej. filas desalineadas), ajustar el generador y repetir Step 3.

- [ ] **Step 5: Importar en Unity (batch) para generar los .meta**

```bash
UNITY="/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity"
"$UNITY" -batchmode -quit -projectPath /Users/isaac_rs/Desktop/ProyectoUnity -logFile "$SCRATCH/unity_import.log"; echo "exit=$?"
grep -c "error CS" "$SCRATCH/unity_import.log" || true
ls Assets/Sprites/Effects/*.meta | wc -l
```

Expected: `exit=0`, `0` errores CS, y `6` metas en Effects (más los 2 nuevos en Characters).

- [ ] **Step 6: Commit**

```bash
git add -A Assets/Sprites
git commit -m "feat(art): add pixelart fireball, brazos del jugador y boss redisenado"
```

---

### Task 2: Runtime del proyectil — flipbook, flicker y glow parametrizado

**Files:**
- Create: `Assets/Scripts/SpriteFlipbook.cs`
- Create: `Assets/Scripts/LightFlicker2D.cs`
- Modify: `Assets/Scripts/ProjectileGlow.cs` (firma de `Attach`)
- Modify: `Assets/Scripts/FlameProjectile.cs:20` (nueva llamada)

**Interfaces:**
- Consumes: patrón de parpadeo de `FlameLightFlicker` (dos senos desfasados).
- Produces: `SpriteFlipbook` con campo serializado `Sprite[] frames` y `float frameTime` (Task 3 los asigna por `SerializedObject` con esos nombres exactos); `ProjectileGlow.Attach(MonoBehaviour host, Color color, float radius = 1f, float intensity = 1.2f, bool flicker = false)`; `LightFlicker2D` componente que requiere `Light2D`.

- [ ] **Step 1: Crear `Assets/Scripts/SpriteFlipbook.cs`**

```csharp
using UnityEngine;

/// <summary>
/// Flipbook mínimo por código (sin Animator): cicla un array de sprites
/// en el SpriteRenderer. Usado por la bola de fuego del jugador.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteFlipbook : MonoBehaviour
{
    [SerializeField] Sprite[] frames;
    [SerializeField] float    frameTime = 0.08f;

    SpriteRenderer sr;
    float timer;
    int   index;

    void Awake() => sr = GetComponent<SpriteRenderer>();

    void Update()
    {
        if (frames == null || frames.Length == 0) return;
        timer += Time.deltaTime;
        if (timer >= frameTime)
        {
            timer -= frameTime;
            index = (index + 1) % frames.Length;
            sr.sprite = frames[index];
        }
    }
}
```

- [ ] **Step 2: Crear `Assets/Scripts/LightFlicker2D.cs`**

```csharp
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Parpadeo genérico de una Light2D (mismo patrón de dos senos que
/// FlameLightFlicker, pero SIN dependencia de WaxSystem — sirve para
/// proyectiles y luces de ambiente).
/// </summary>
[RequireComponent(typeof(Light2D))]
public class LightFlicker2D : MonoBehaviour
{
    [SerializeField] float flickerAmount = 0.25f;  // ±25% de intensidad
    [SerializeField] float speed         = 9f;

    Light2D light2d;
    float   baseIntensity;
    float   baseRadius;
    float   offset;

    void Awake()
    {
        light2d       = GetComponent<Light2D>();
        baseIntensity = light2d.intensity;
        baseRadius    = light2d.pointLightOuterRadius;
        offset        = Random.Range(0f, 100f);
    }

    void Update()
    {
        float t = Time.time * speed + offset;
        float f = 1f + (Mathf.Sin(t) * 0.6f + Mathf.Sin(t * 2.7f) * 0.4f) * flickerAmount;
        light2d.intensity             = baseIntensity * f;
        light2d.pointLightOuterRadius = baseRadius * (1f + (f - 1f) * 0.5f);
    }
}
```

- [ ] **Step 3: Modificar `Assets/Scripts/ProjectileGlow.cs`**

Reemplazar el método `Attach` completo (líneas 13-28) por:

```csharp
    public static void Attach(MonoBehaviour host, Color color, float radius = 1f,
                              float intensity = 1.2f, bool flicker = false)
    {
        // Si el prefab ya trae una luz configurada a mano, respetarla
        if (host.GetComponentInChildren<Light2D>() != null) return;

        GameObject go = new GameObject("Glow");
        go.transform.SetParent(host.transform, false);

        Light2D l = go.AddComponent<Light2D>();
        l.lightType             = Light2D.LightType.Point;
        l.color                 = color;
        l.pointLightOuterRadius = radius;
        l.pointLightInnerRadius = 0.1f;
        l.falloffIntensity      = 0.7f;
        l.intensity             = intensity;

        if (flicker) go.AddComponent<LightFlicker2D>();
    }
```

Los call-sites existentes de `EnemyFlameProjectile` y `MagicOrb` no se tocan: los defaults reproducen el comportamiento actual exacto.

- [ ] **Step 4: Modificar `Assets/Scripts/FlameProjectile.cs` línea 20**

```csharp
        // Luz azul parpadeante para verse volar en la oscuridad de Level2
        ProjectileGlow.Attach(this, new Color(0.290f, 0.831f, 1f),
                              radius: 1.6f, intensity: 2f, flicker: true);
```

- [ ] **Step 5: Compilar en batch**

```bash
"$UNITY" -batchmode -quit -projectPath /Users/isaac_rs/Desktop/ProyectoUnity -logFile "$SCRATCH/unity_t2.log"; echo "exit=$?"
grep "error CS" "$SCRATCH/unity_t2.log" || echo "SIN ERRORES"
```

Expected: `exit=0`, `SIN ERRORES`.

- [ ] **Step 6: Commit**

```bash
git add -A Assets/Scripts
git commit -m "feat(projectile): flipbook, glow parametrizado y flicker de Light2D"
```

---

### Task 3: Editor — importador pixel, ProjectileDresser y prefab del proyectil

**Files:**
- Create: `Assets/Editor/PixelSpriteImport.cs`
- Create: `Assets/Editor/ProjectileDresser.cs`
- Modify (vía batch): `Assets/Prefabs/Proyectil.prefab`

**Interfaces:**
- Consumes: PNGs `Assets/Sprites/Effects/fireball_0..3.png` (Task 1); `SpriteFlipbook` con campo `frames` (Task 2).
- Produces: `PixelSpriteImport.Import(string assetPath, float worldHeight, Vector2? customPivot = null) → Sprite` (Task 4 y este task la usan con esa firma exacta); `Proyectil.prefab` vestido (sprite fireball, unlit, flipbook, estela).

- [ ] **Step 1: Crear `Assets/Editor/PixelSpriteImport.cs`**

```csharp
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// CANDLE FURY — Importador compartido de sprites pixelart:
/// Point filter, sin compresión, Sprite/Single, PPU derivado del alto
/// deseado en unidades de mundo, y pivote custom opcional.
/// (Misma convención que SpriteAssigner, reutilizable desde otros menús.)
/// </summary>
public static class PixelSpriteImport
{
    public static Sprite Import(string assetPath, float worldHeight, Vector2? customPivot = null)
    {
        var imp = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (imp == null) return null;

        bool dirty = false;
        if (imp.textureType        != TextureImporterType.Sprite)  { imp.textureType      = TextureImporterType.Sprite;  dirty = true; }
        if (imp.spriteImportMode   != SpriteImportMode.Single)     { imp.spriteImportMode = SpriteImportMode.Single;     dirty = true; }
        if (imp.filterMode         != FilterMode.Point)            { imp.filterMode       = FilterMode.Point;            dirty = true; }
        if (imp.textureCompression != TextureImporterCompression.Uncompressed)
                                                                   { imp.textureCompression = TextureImporterCompression.Uncompressed; dirty = true; }
        if (dirty) { imp.SaveAndReimport(); dirty = false; }

        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        if (tex == null) return null;

        float ppu = Mathf.Round(tex.height / worldHeight);
        if (Mathf.Abs(imp.spritePixelsPerUnit - ppu) > 0.5f) { imp.spritePixelsPerUnit = ppu; dirty = true; }

        if (customPivot.HasValue)
        {
            var ts = new TextureImporterSettings();
            imp.ReadTextureSettings(ts);
            if (ts.spriteAlignment != (int)SpriteAlignment.Custom || ts.spritePivot != customPivot.Value)
            {
                ts.spriteAlignment = (int)SpriteAlignment.Custom;
                ts.spritePivot     = customPivot.Value;
                imp.SetTextureSettings(ts);
                dirty = true;
            }
        }
        if (dirty) imp.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
    }
}
#endif
```

- [ ] **Step 2: Crear `Assets/Editor/ProjectileDresser.cs`**

```csharp
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// CANDLE FURY — Viste Proyectil.prefab con la bola de fuego pixelart:
/// sprite frame 0 + SpriteFlipbook (4 frames), material Sprite-Unlit-Default
/// (el fuego es auto-emisivo: brilla igual en la oscuridad de Level2)
/// y TrailRenderer con estela cian. Idempotente.
/// Entrada batch: -executeMethod ProjectileDresser.Dress
/// </summary>
public static class ProjectileDresser
{
    const string SpriteDir  = "Assets/Sprites/Effects/";
    const string PrefabPath = "Assets/Prefabs/Proyectil.prefab";
    const string UnlitMaterialPath =
        "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";
    const float WorldHeight = 0.9f;   // 16px → PPU 18 ≈ diámetro de colisión (1u)

    [MenuItem("Candle Fury/Vestir Proyectil")]
    public static void Dress()
    {
        var sprites = new Sprite[4];
        for (int i = 0; i < 4; i++)
        {
            sprites[i] = PixelSpriteImport.Import($"{SpriteDir}fireball_{i}.png", WorldHeight);
            if (sprites[i] == null)
            {
                Debug.LogError($"[ProjectileDresser] FALTA {SpriteDir}fireball_{i}.png");
                return;
            }
        }
        var unlit = AssetDatabase.LoadAssetAtPath<Material>(UnlitMaterialPath);

        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var sr = root.GetComponent<SpriteRenderer>();
            sr.sprite = sprites[0];
            sr.color  = Color.white;              // el color ya va en el sprite
            if (unlit != null) sr.sharedMaterial = unlit;

            var flip = root.GetComponent<SpriteFlipbook>();
            if (flip == null) flip = root.AddComponent<SpriteFlipbook>();
            var so  = new SerializedObject(flip);
            var arr = so.FindProperty("frames");
            arr.arraySize = sprites.Length;
            for (int i = 0; i < sprites.Length; i++)
                arr.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
            so.ApplyModifiedPropertiesWithoutUndo();

            var trail = root.GetComponent<TrailRenderer>();
            if (trail == null) trail = root.AddComponent<TrailRenderer>();
            trail.time              = 0.15f;
            trail.startWidth        = 0.25f;
            trail.endWidth          = 0f;
            trail.minVertexDistance = 0.05f;
            if (unlit != null) trail.sharedMaterial = unlit;
            trail.sortingLayerID = sr.sortingLayerID;
            trail.sortingOrder   = sr.sortingOrder - 1;   // detrás de la bola
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(new Color(0.290f, 0.831f, 1f),   0f),
                        new GradientColorKey(new Color(0.118f, 0.470f, 0.784f), 1f) },
                new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0f, 1f) });
            trail.colorGradient = grad;

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Debug.Log("✔ Proyectil vestido: fireball flipbook + unlit + estela");
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
}
#endif
```

- [ ] **Step 3: Ejecutar en batch**

```bash
"$UNITY" -batchmode -quit -projectPath /Users/isaac_rs/Desktop/ProyectoUnity \
  -executeMethod ProjectileDresser.Dress -logFile "$SCRATCH/unity_t3.log"; echo "exit=$?"
grep -E "error CS|Proyectil vestido" "$SCRATCH/unity_t3.log"
```

Expected: `exit=0` y la línea `✔ Proyectil vestido: fireball flipbook + unlit + estela`, sin `error CS`.

- [ ] **Step 4: Verificar el prefab por guid**

```bash
FLIP_GUID=$(grep -o 'guid: [a-f0-9]*' Assets/Scripts/SpriteFlipbook.cs.meta | cut -d' ' -f2)
FIRE_GUID=$(grep -o 'guid: [a-f0-9]*' Assets/Sprites/Effects/fireball_0.png.meta | cut -d' ' -f2)
grep -c "$FLIP_GUID" Assets/Prefabs/Proyectil.prefab   # espera 1
grep -c "$FIRE_GUID" Assets/Prefabs/Proyectil.prefab   # espera >=1
grep -c "TrailRenderer" Assets/Prefabs/Proyectil.prefab # espera >=1
```

Expected: los tres greps encuentran al menos 1 coincidencia.

- [ ] **Step 5: Commit**

```bash
git add -A Assets/Editor Assets/Prefabs Assets/Sprites
git commit -m "feat(projectile): viste el prefab con fireball animada, unlit y estela"
```

---

### Task 4: Brazo overlay procedural del jugador

**Files:**
- Create: `Assets/Scripts/PlayerAttackArm.cs`
- Create: `Assets/Editor/PlayerArmSetup.cs`
- Modify: `Assets/Scripts/PlayerCombat.cs` (campo + Awake + 3 llamadas en líneas 80, 102, 135)
- Modify (vía batch): `Assets/Scenes/Level1.unity`, `Assets/Scenes/Level2.unity`

**Interfaces:**
- Consumes: `PixelSpriteImport.Import(path, worldHeight, pivot)` (Task 3); PNGs `arm_sword.png`/`arm_cast.png` (Task 1); `PlayerController` (existente, para localizar al Player por componente).
- Produces: `PlayerAttackArm` con métodos públicos `PlaySword()`, `PlayShoot()`, `PlayPulse()` y campos serializados `swordSprite`/`castSprite`; hijo "Arm" activo (SpriteRenderer deshabilitado en reposo) bajo el Player de ambas escenas.

- [ ] **Step 1: Crear `Assets/Scripts/PlayerAttackArm.cs`**

```csharp
using System.Collections;
using UnityEngine;

/// <summary>
/// Brazo overlay procedural del Príncipe Vela: vive en el hijo "Arm"
/// (siempre activo, con su SpriteRenderer apagado en reposo) y se anima
/// por corrutinas — sin Animator. El flip del jugador (localScale.x
/// negativo) voltea el brazo automáticamente.
/// Configurado por el menú "Candle Fury/Configurar Brazo Jugador".
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerAttackArm : MonoBehaviour
{
    [SerializeField] Sprite swordSprite;
    [SerializeField] Sprite castSprite;
    [SerializeField] float  swingTime = 0.18f;
    [SerializeField] float  shootTime = 0.15f;
    [SerializeField] float  pulseTime = 0.25f;

    SpriteRenderer sr;
    Vector3   basePos;
    Transform body;      // raíz del Player (scale-punch del pulso)
    Coroutine current;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        sr.enabled = false;
        basePos = transform.localPosition;
        body    = transform.parent;
    }

    public void PlaySword() => Play(SwordSwing());
    public void PlayShoot() => Play(ShootPunch());
    public void PlayPulse() => Play(PulseRaise());

    void Play(IEnumerator routine)
    {
        if (current != null) StopCoroutine(current);   // no acumular brazos
        ResetVisual();
        current = StartCoroutine(routine);
    }

    void ResetVisual()
    {
        transform.localPosition = basePos;
        transform.localRotation = Quaternion.identity;
        sr.enabled = false;
    }

    IEnumerator SwordSwing()
    {
        sr.sprite  = swordSprite;
        sr.enabled = true;
        float t = 0f;
        while (t < swingTime)
        {
            t += Time.deltaTime;
            float k = Mathf.Sin(Mathf.Clamp01(t / swingTime) * Mathf.PI * 0.5f); // ease-out
            transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(70f, -50f, k));
            yield return null;
        }
        ResetVisual();
        current = null;
    }

    IEnumerator ShootPunch()
    {
        sr.sprite  = castSprite;
        sr.enabled = true;
        float t = 0f;
        while (t < shootTime)
        {
            t += Time.deltaTime;
            float k = Mathf.Sin(Mathf.Clamp01(t / shootTime) * Mathf.PI);        // ida y vuelta
            transform.localPosition = basePos + new Vector3(0.20f * k, 0f, 0f);
            yield return null;
        }
        ResetVisual();
        current = null;
    }

    IEnumerator PulseRaise()
    {
        sr.sprite  = castSprite;
        sr.enabled = true;
        transform.localRotation = Quaternion.Euler(0f, 0f, 90f);                 // brazo alzado
        float t = 0f;
        while (t < pulseTime)
        {
            t += Time.deltaTime;
            float k = Mathf.Sin(Mathf.Clamp01(t / pulseTime) * Mathf.PI);
            if (body != null)
            {
                float sign = Mathf.Sign(body.localScale.x);   // preservar flip
                float mag  = 1f + 0.08f * k;
                body.localScale = new Vector3(sign * mag, mag, 1f);
            }
            yield return null;
        }
        if (body != null)
            body.localScale = new Vector3(Mathf.Sign(body.localScale.x), 1f, 1f);
        ResetVisual();
        current = null;
    }
}
```

- [ ] **Step 2: Modificar `Assets/Scripts/PlayerCombat.cs`**

(a) Junto a los componentes (tras la línea `Animator anim;`, línea 37):

```csharp
    PlayerAttackArm  arm;
```

(b) En `Awake()` (tras `anim = GetComponent<Animator>();`):

```csharp
        arm        = GetComponentInChildren<PlayerAttackArm>();
```

(c) En `SwordAttack()` tras `anim.SetTrigger("Attack");` (línea 80):

```csharp
        arm?.PlaySword();
```

(d) En `FlameCheck()` tras `anim.SetTrigger("Pulse");` (línea 102):

```csharp
        arm?.PlayPulse();
```

(e) En `ShootFlame()` tras `anim.SetTrigger("Shoot");` (línea 135):

```csharp
        arm?.PlayShoot();
```

- [ ] **Step 3: Crear `Assets/Editor/PlayerArmSetup.cs`**

```csharp
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// CANDLE FURY — Crea/configura el hijo "Arm" del Player en la escena:
/// SpriteRenderer (lit, encima del cuerpo, apagado) + PlayerAttackArm con
/// los sprites arm_sword/arm_cast. Idempotente, con Undo.
/// No hay prefab de Player: correr en Level1 y Level2.
/// Entrada batch: -executeMethod PlayerArmSetup.SetupAllScenes
/// </summary>
public static class PlayerArmSetup
{
    const string SwordPath = "Assets/Sprites/Effects/arm_sword.png";
    const string CastPath  = "Assets/Sprites/Effects/arm_cast.png";
    const string LitMaterialPath =
        "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Lit-Default.mat";
    // Alto 10 px al PPU del player (37) → 10/37 u de alto en mundo
    const float ArmWorldHeight = 10f / 37f;

    static readonly string[] ScenePaths =
        { "Assets/Scenes/Level1.unity", "Assets/Scenes/Level2.unity" };

    [MenuItem("Candle Fury/Configurar Brazo Jugador")]
    public static void SetupActiveScene()
    {
        Setup();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("Recuerda GUARDAR la escena (Ctrl+S) y repetir en la otra escena.");
    }

    public static void SetupAllScenes()
    {
        foreach (string path in ScenePaths)
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            Setup();
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"✔ Brazo configurado en {path}");
        }
    }

    static void Setup()
    {
        // pivote en el hombro (extremo izquierdo del sprite)
        Sprite sword = PixelSpriteImport.Import(SwordPath, ArmWorldHeight, new Vector2(0.12f, 0.5f));
        Sprite cast  = PixelSpriteImport.Import(CastPath,  ArmWorldHeight, new Vector2(0.19f, 0.5f));
        if (sword == null || cast == null)
        {
            Debug.LogError("[PlayerArmSetup] Faltan arm_sword.png / arm_cast.png en Assets/Sprites/Effects/");
            return;
        }
        Material lit = AssetDatabase.LoadAssetAtPath<Material>(LitMaterialPath);

        foreach (var pc in Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
        {
            Transform arm = pc.transform.Find("Arm");
            if (arm == null)
            {
                var go = new GameObject("Arm");
                Undo.RegisterCreatedObjectUndo(go, "crear Arm");
                go.transform.SetParent(pc.transform, false);
                arm = go.transform;
            }
            arm.localPosition = new Vector3(0.05f, 0.15f, 0f);   // hombro aprox.

            var sr = arm.GetComponent<SpriteRenderer>();
            if (sr == null) sr = Undo.AddComponent<SpriteRenderer>(arm.gameObject);
            Undo.RecordObject(sr, "configurar Arm");
            sr.sprite  = sword;
            sr.enabled = false;                    // PlayerAttackArm lo enciende al atacar
            if (lit != null) sr.sharedMaterial = lit;
            var bodySr = pc.GetComponent<SpriteRenderer>();
            if (bodySr != null)
            {
                sr.sortingLayerID = bodySr.sortingLayerID;
                sr.sortingOrder   = bodySr.sortingOrder + 1;   // encima del cuerpo
            }

            var attackArm = arm.GetComponent<PlayerAttackArm>();
            if (attackArm == null) attackArm = Undo.AddComponent<PlayerAttackArm>(arm.gameObject);
            var so = new SerializedObject(attackArm);
            so.FindProperty("swordSprite").objectReferenceValue = sword;
            so.FindProperty("castSprite").objectReferenceValue  = cast;
            so.ApplyModifiedProperties();
            Debug.Log($"✔ Arm listo en {pc.gameObject.name}");
        }
    }
}
#endif
```

- [ ] **Step 4: Ejecutar en batch y verificar escenas**

```bash
"$UNITY" -batchmode -quit -projectPath /Users/isaac_rs/Desktop/ProyectoUnity \
  -executeMethod PlayerArmSetup.SetupAllScenes -logFile "$SCRATCH/unity_t4.log"; echo "exit=$?"
grep -E "error CS|Brazo configurado" "$SCRATCH/unity_t4.log"
grep -c "m_Name: Arm" Assets/Scenes/Level1.unity   # espera 1
grep -c "m_Name: Arm" Assets/Scenes/Level2.unity   # espera 1
```

Expected: `exit=0`, dos líneas `✔ Brazo configurado en ...`, y `1` en cada grep de escena.

- [ ] **Step 5: Idempotencia — repetir el batch y confirmar que no duplica**

```bash
"$UNITY" -batchmode -quit -projectPath /Users/isaac_rs/Desktop/ProyectoUnity \
  -executeMethod PlayerArmSetup.SetupAllScenes -logFile "$SCRATCH/unity_t4b.log"
grep -c "m_Name: Arm" Assets/Scenes/Level1.unity   # sigue siendo 1
```

Expected: sigue habiendo exactamente 1 "Arm" por escena.

- [ ] **Step 6: Commit**

```bash
git add -A Assets/Scripts Assets/Editor Assets/Scenes Assets/Sprites
git commit -m "feat(player): brazo overlay procedural en espadazo, pulso y disparo"
```

---

### Task 5: Rediseño del boss — visuales y cableado

**Files:**
- Create: `Assets/Scripts/RoyalPyromancerVisuals.cs`
- Modify: `Assets/Scripts/RoyalPyromancer.cs` (campo + Awake + llamada en `CastOrbs`, línea 73)
- Modify: `Assets/Editor/SpriteAssigner.cs` (array `Imports` líneas 44-50, asignación línea 126, extras nuevos, entrada batch)
- Delete: `Assets/Sprites/Characters/royal_pyromancer.png` (+ `.meta`)
- Modify (vía batch): `Assets/Scenes/Level1.unity`, `Assets/Scenes/Level2.unity`

**Interfaces:**
- Consumes: PNGs `royal_pyromancer_idle.png` / `royal_pyromancer_cast.png` (Task 1); `EnemyBase.anim` protegido (existente); patrón `SerializedObject` de SpriteAssigner.
- Produces: `RoyalPyromancerVisuals` con método público `OnCastStart()` y campos serializados `castSprite`/`castLight`; `SpriteAssigner.AssignAllScenes()` como entrada batch.

- [ ] **Step 1: Crear `Assets/Scripts/RoyalPyromancerVisuals.cs`**

```csharp
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Visuales del Pirómante Real (SRP: separados de la lógica de combate de
/// RoyalPyromancer). Idle: levitación con bob sinusoidal. Cast: pose de
/// casteo + pulso de luz magenta en el CastPoint + scale-punch que preserva
/// el flip. El flash rojo de daño sigue viviendo en EnemyBase.
/// El menú "Candle Fury/Asignar Sprites" cablea castSprite y castLight.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class RoyalPyromancerVisuals : MonoBehaviour
{
    [Header("Levitación")]
    [SerializeField] float bobAmplitude = 0.08f;
    [SerializeField] float bobPeriod    = 2.5f;

    [Header("Casteo")]
    [SerializeField] Sprite  castSprite;
    [SerializeField] float   castPoseTime  = 0.5f;
    [SerializeField] Light2D castLight;
    [SerializeField] float   castLightPeak = 2f;

    SpriteRenderer sr;
    Sprite    idleSprite;
    float     baseY;
    float     bobT;
    Coroutine castRoutine;

    void Awake()
    {
        sr         = GetComponent<SpriteRenderer>();
        idleSprite = sr.sprite;
        baseY      = transform.position.y;
    }

    void Update()
    {
        // El jefe es estático (moveSpeed 0, Rigidbody2D kinemático):
        // bob directo sobre transform.position.y sin pelear con la física.
        bobT += Time.deltaTime;
        float y = baseY + Mathf.Sin(bobT * 2f * Mathf.PI / bobPeriod) * bobAmplitude;
        transform.position = new Vector3(transform.position.x, y, transform.position.z);
    }

    public void OnCastStart()
    {
        if (castRoutine != null) StopCoroutine(castRoutine);
        castRoutine = StartCoroutine(CastPose());
    }

    IEnumerator CastPose()
    {
        if (castSprite != null) sr.sprite = castSprite;

        float t = 0f;
        while (t < castPoseTime)
        {
            t += Time.deltaTime;
            float k = Mathf.Sin(Mathf.Clamp01(t / castPoseTime) * Mathf.PI);   // 0→1→0
            if (castLight != null) castLight.intensity = castLightPeak * k;
            float sign = Mathf.Sign(transform.localScale.x);                    // preservar flip
            float mag  = 1f + 0.06f * k;
            transform.localScale = new Vector3(sign * mag, mag, 1f);
            yield return null;
        }

        if (castLight != null) castLight.intensity = 0f;
        transform.localScale = new Vector3(Mathf.Sign(transform.localScale.x), 1f, 1f);
        sr.sprite   = idleSprite;
        castRoutine = null;
    }
}
```

- [ ] **Step 2: Modificar `Assets/Scripts/RoyalPyromancer.cs`**

(a) Junto a los campos privados (tras `bool playerDetected;`, línea 22):

```csharp
    RoyalPyromancerVisuals visuals;
```

(b) En `Awake()`, tras `base.Awake();`:

```csharp
        visuals   = GetComponent<RoyalPyromancerVisuals>();
```

(c) En `CastOrbs()`, tras `if (anim != null) anim.SetTrigger("Cast");` (línea 73):

```csharp
        visuals?.OnCastStart();
```

- [ ] **Step 3: Modificar `Assets/Editor/SpriteAssigner.cs`**

(a) En el array `Imports` (líneas 44-50), reemplazar la entrada del pirómante por dos:

```csharp
        ("royal_pyromancer_idle.png", 1.56f),  // jefe: 1.3× el alto del player
        ("royal_pyromancer_cast.png", 1.56f),  // pose de casteo (mismo tamaño)
```

(b) Reemplazar la asignación del boss (líneas 126-127):

```csharp
        count = AssignAll<RoyalPyromancer>(sprites, "royal_pyromancer_idle.png", litMat);
        if (count > 0) creados.Add($"RoyalPyromancer: sprite asignado a {count}");
        SetupBossExtras(sprites, creados);
```

(c) Añadir al final de la clase (antes del cierre `}` de `SpriteAssigner`):

```csharp
    // ── Extras del jefe: visuales de casteo y luz en el CastPoint ──────────
    static void SetupBossExtras(Dictionary<string, Sprite> sprites, List<string> creados)
    {
        if (!sprites.TryGetValue("royal_pyromancer_cast.png", out var castSprite)) return;

        foreach (var boss in Object.FindObjectsByType<RoyalPyromancer>(FindObjectsSortMode.None))
        {
            var visuals = boss.GetComponent<RoyalPyromancerVisuals>();
            if (visuals == null)
            {
                visuals = Undo.AddComponent<RoyalPyromancerVisuals>(boss.gameObject);
                creados.Add("RoyalPyromancerVisuals agregado al jefe");
            }

            // Luz de casteo magenta (#FF4080) en el CastPoint, apagada en reposo
            Light2D castLight = null;
            Transform castPoint = boss.transform.Find("CastPoint");
            if (castPoint != null)
            {
                castLight = castPoint.GetComponent<Light2D>();
                if (castLight == null)
                {
                    castLight = Undo.AddComponent<Light2D>(castPoint.gameObject);
                    castLight.lightType             = Light2D.LightType.Point;
                    castLight.color                 = new Color(1f, 0.251f, 0.502f);
                    castLight.pointLightOuterRadius = 1.5f;
                    castLight.intensity             = 0f;
                    creados.Add("Luz de casteo creada en CastPoint");
                }
            }

            var so = new SerializedObject(visuals);
            so.FindProperty("castSprite").objectReferenceValue = castSprite;
            so.FindProperty("castLight").objectReferenceValue  = castLight;
            so.ApplyModifiedProperties();
        }
    }

    // Entrada batch: -executeMethod SpriteAssigner.AssignAllScenes
    public static void AssignAllScenes()
    {
        foreach (string path in new[] { "Assets/Scenes/Level1.unity", "Assets/Scenes/Level2.unity" })
        {
            Scene s = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            Assign();
            EditorSceneManager.SaveScene(s);
            Debug.Log($"✔ Sprites asignados en {path}");
        }
    }
```

- [ ] **Step 4: Ejecutar en batch y verificar**

```bash
"$UNITY" -batchmode -quit -projectPath /Users/isaac_rs/Desktop/ProyectoUnity \
  -executeMethod SpriteAssigner.AssignAllScenes -logFile "$SCRATCH/unity_t5.log"; echo "exit=$?"
grep -E "error CS|Sprites asignados|FALTA" "$SCRATCH/unity_t5.log"
IDLE_GUID=$(grep -o 'guid: [a-f0-9]*' Assets/Sprites/Characters/royal_pyromancer_idle.png.meta | cut -d' ' -f2)
VIS_GUID=$(grep -o 'guid: [a-f0-9]*' Assets/Scripts/RoyalPyromancerVisuals.cs.meta | cut -d' ' -f2)
grep -c "$IDLE_GUID" Assets/Scenes/Level1.unity   # espera >=1
grep -c "$VIS_GUID"  Assets/Scenes/Level1.unity   # espera >=1
```

Expected: `exit=0`, dos `✔ Sprites asignados`, ninguna línea `FALTA`, y ambos guids presentes en Level1. (El boss solo existe en Level1; en Level2 el resumen simplemente no listará pirómantes.)

- [ ] **Step 5: Eliminar el sprite viejo del boss**

```bash
git rm Assets/Sprites/Characters/royal_pyromancer.png Assets/Sprites/Characters/royal_pyromancer.png.meta
OLD_GUID="2e0f5884199024b4ab4a0d14e797ec09"
grep -c "$OLD_GUID" Assets/Scenes/Level1.unity || echo "sin referencias al sprite viejo"
```

Expected: `sin referencias al sprite viejo` (la reasignación del Step 4 ya lo reemplazó; si aún hay referencias, NO borrar — investigar el log del Step 4).

- [ ] **Step 6: Commit**

```bash
git add -A Assets/Scripts Assets/Editor Assets/Scenes Assets/Sprites
git commit -m "feat(boss): rediseno pixelart del Piromante Real con levitacion y pose de casteo"
```

---

### Task 6: Verificación final en Play (manual)

**Files:** ninguno (solo verificación; commit final únicamente si Unity re-serializó escenas).

**Interfaces:**
- Consumes: todo lo anterior.
- Produces: confirmación de la checklist de la spec.

- [ ] **Step 1: Compilación final limpia**

```bash
"$UNITY" -batchmode -quit -projectPath /Users/isaac_rs/Desktop/ProyectoUnity -logFile "$SCRATCH/unity_final.log"; echo "exit=$?"
grep "error CS" "$SCRATCH/unity_final.log" || echo "SIN ERRORES"
```

Expected: `exit=0`, `SIN ERRORES`.

- [ ] **Step 2: Checklist manual en el editor (la hace el usuario)**

Pedir al usuario que abra Unity y verifique en Play, y esperar su confirmación:

1. **Level1** — Z/X/C muestran el brazo (swing de espada, brazo alzado con punch del cuerpo, empuje al disparar), mirando a la derecha Y a la izquierda.
2. El proyectil es la bola de fuego cian animada con estela; al chocar desaparece.
3. El boss (x≈156) levita, y al castear cambia de pose ~0.5s con pulso de luz magenta; su flash rojo al recibir daño sigue; sigue lanzando 2 orbes cada 3s y suelta el FlameCore al morir.
4. **Level2** — en la oscuridad, la bola de fuego se ve brillante (unlit) y su luz parpadea; el brazo también funciona.
5. Menús "Candle Fury/Asignar Sprites" y "Configurar Brazo Jugador" corridos dos veces no duplican nada.

- [ ] **Step 3: Commit final (solo si hay diffs de re-serialización)**

```bash
git status --short
# Si hay cambios en Assets/ generados por la sesión manual del usuario:
git add -A Assets && git commit -m "chore: re-serializacion de escenas tras verificacion en editor"
```
