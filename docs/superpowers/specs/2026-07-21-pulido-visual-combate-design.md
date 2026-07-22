# Diseño: Pulido visual de combate — "Candle Fury"

**Fecha:** 2026-07-21
**Rama:** lab10
**Estado:** Aprobado por el usuario

## Contexto

Juego de plataformas 2D pixelart en Unity 6 (6000.4.0f1), URP 17.4 con Renderer2D y
luces 2D (`Light2D`). Dos escenas: `Assets/Scenes/Level1.unity` y `Level2.unity`.
No existe prefab de Player (vive en cada escena). No hay AnimatorControllers ni clips
en el proyecto: los `SetTrigger` de `PlayerCombat`/`PlayerController` son no-ops hoy.
La convención de arte es: PNGs pixelart de baja resolución en `Assets/Sprites/`,
importados y cableados por scripts de editor idempotentes bajo el menú "Candle Fury"
(`SpriteAssigner.cs`, `AestheticApplier.cs`, `LevelDresser.cs`), con Point filter,
sin compresión, y PPU derivado de la altura deseada en unidades de mundo.

Alcance acordado (4 features):

1. Imagen pixelart de bola de fuego para el proyectil del jugador.
2. Iluminación del proyectil del jugador.
3. Movimiento de brazos del jugador al atacar (overlay procedural, sin Animator).
4. Rediseño del final boss del Level 1 (Royal Pyromancer): visual + animación
   procedural. Su gameplay NO cambia.

Decisiones del usuario:

- Todo el arte nuevo lo genera el asistente por script (pixelart consistente con el
  estilo actual; reemplazable después sin tocar código).
- Identidad de color del proyectil: **cian-azul con núcleo blanco** (coherente con la
  llama del jugador #4AD4FF; los proyectiles enemigos son rojo y magenta).
- Brazos: overlay procedural por código, no animación por frames.
- Boss: solo visual + animación; sin fases ni ataques nuevos.

## 1. Bola de fuego pixelart del proyectil

**Arte:** 4 PNGs de 16×16 en `Assets/Sprites/Effects/`: `fireball_0.png` …
`fireball_3.png`. Bola de fuego cian-azul con núcleo blanco y lenguas de llama hacia
atrás (apuntando a la izquierda; el volteo lo da el flip existente). Los 4 frames son
un ciclo de parpadeo/ondulación de la llama.

**Componente nuevo:** `Assets/Scripts/SpriteFlipbook.cs` — MonoBehaviour mínimo que
cicla un array de sprites en el `SpriteRenderer` cada `frameTime` segundos
(default 0.08s). Sin Animator; consistente con el estilo code-driven del proyecto.

**Prefab:** `Assets/Prefabs/Proyectil.prefab`:

- `SpriteRenderer.sprite` = `fireball_0`; tinte blanco `(1,1,1,1)` — el color va en
  el sprite, se elimina el tinte cian actual.
- Material **Sprite-Unlit-Default**: el fuego es auto-emisivo y debe verse brillante
  incluso en la oscuridad del Level 2. (`AestheticApplier` solo fuerza
  Sprite-Lit-Default sobre objetos de escena; el prefab instanciado en runtime no se
  ve afectado.)
- Se añade `SpriteFlipbook` con los 4 frames.
- El `CircleCollider2D` (radio 0.5, trigger) y `Rigidbody2D` no cambian.

**Importación:** los PNGs de `Effects/` se importan vía `PixelSpriteImport.Import`
(Point, uncompressed, Single), invocado desde el menú `ProjectileDresser` ("Vestir
Proyectil"), con PPU calculado para una altura de mundo de ~0.9u (coherente con el
diámetro de colisión de 1u).

## 2. Iluminación del proyectil

Hoy `ProjectileGlow.Attach` añade en runtime una `Light2D` cian estática
(intensidad 1.2, radio 1.0) al proyectil del jugador. Cambios:

- **Parámetros del glow del jugador** (`FlameProjectile.cs:20`): intensidad ~2.0,
  radio exterior ~1.6. Color cian actual se mantiene.
- **Componente nuevo:** `Assets/Scripts/LightFlicker2D.cs` — parpadeo genérico de
  `Light2D` (intensidad y radio modulados por dos ondas sinusoidales), extraído del
  patrón de `FlameLightFlicker` pero **sin dependencia de `WaxSystem`**. Se añade al
  GameObject "Glow" que crea `ProjectileGlow`. `ProjectileGlow.Attach` gana
  parámetros opcionales `intensity` (default 1.2, el valor hardcodeado hoy) y
  `flicker` (default false); solo el call-site del proyectil del jugador pasa valores
  nuevos, así los proyectiles enemigos quedan idénticos sin tocar sus llamadas.
- **Estela:** `TrailRenderer` en `Proyectil.prefab` — tiempo 0.15s, ancho pequeño
  decreciente, degradado cian→transparente, material Sprite-Unlit-Default (el mismo
  material emisivo del proyectil; decidido en implementación). En Level 1
  (luz ambiental brillante) la estela es la señal visible; la Light2D luce en Level 2
  y zonas oscuras.

`EnemyFlameProjectile` y `MagicOrb` no se tocan (siguen usando `Attach` sin flicker).

## 3. Movimiento de brazos del jugador al atacar

**Arte:** 2 PNGs en `Assets/Sprites/Effects/` con la paleta muestreada de
`principe_vela.png` (43×44): `arm_sword.png` (brazo empuñando espada) y
`arm_cast.png` (brazo con palma abierta). Tamaño ~20×10 px, pivote en el hombro.

**Jerarquía:** hijo nuevo "Arm" del Player en ambas escenas: `SpriteRenderer`
(sorting order por encima del cuerpo), posición local en el hombro (~(0.05, 0.15)),
desactivado en reposo. Como el flip del jugador niega `localScale.x`
(`PlayerController.cs:163-169`), el brazo se voltea automáticamente.

**Componente nuevo:** `Assets/Scripts/PlayerAttackArm.cs` con tres métodos públicos,
cada uno una corrutina corta que muestra el brazo, lo anima y lo oculta:

- `PlaySword()` — sprite `arm_sword`, rotación de 70° a −50° (tajo descendente; ajustado en implementación por legibilidad del golpe) en ~0.18s (swing).
- `PlayShoot()` — sprite `arm_cast`, empuje hacia adelante (offset local x
  0.05→0.25→0.1) en ~0.15s, sincronizado con la instanciación del proyectil.
- `PlayPulse()` — sprite `arm_cast` alzado (~90°) + scale-punch sutil del cuerpo
  (~1.0→1.08→1.0) en ~0.25s.

Si una animación se relanza antes de terminar, la corrutina anterior se cancela
(no se acumulan brazos ni estados).

**Integración:** `PlayerCombat.cs` obtiene la referencia en `Awake` y llama a
`arm?.PlaySword()` / `PlayPulse()` / `PlayShoot()` junto a los `SetTrigger`
existentes (líneas 80, 102, 135), que se conservan.

**Editor:** menú nuevo idempotente "Candle Fury/Configurar Brazo Jugador" (en
`Assets/Editor/`) que crea/configura el hijo "Arm" y el componente en la escena
abierta (correr en Level1 y Level2). Correrlo dos veces no duplica nada.

## 4. Rediseño del boss (Royal Pyromancer) — visual + animación

**Arte:** 2 PNGs en `Assets/Sprites/Characters/`:

- `royal_pyromancer_idle.png` (~84×158): piromante encapuchado más imponente, con
  corona, bastón y llamas magenta/rosa (#FF4080, su color de luz actual en
  `AestheticApplier`). Reemplaza a `royal_pyromancer.png`.
- `royal_pyromancer_cast.png` (mismas dimensiones): pose de casteo, bastón en alto y
  brazo extendido.

La altura de mundo se mantiene en 1.56u (misma que hoy en `SpriteAssigner`) para no
romper el `CapsuleCollider2D` ni el posicionamiento en escena.

**Componente nuevo:** `Assets/Scripts/RoyalPyromancerVisuals.cs` (SRP: lo visual
separado de la lógica de combate):

- *Idle*: levitación — bob sinusoidal de la posición (world-space; equivalente porque
  el jefe es estático y sin padre móvil) (±0.08u, periodo ~2.5s).
  El bob mueve solo el transform visual; al ser Rigidbody2D kinemático y estático
  (moveSpeed 0), no interfiere con física.
- *Cast* (`OnCastStart()`): swap al sprite de casteo durante ~0.5s + pulso de una
  `Light2D` breve en el `CastPoint` + scale-punch pequeño; vuelve al idle.
- *Hit*: se conserva el `DamageFlash` rojo existente de `EnemyBase` sin cambios.

**Integración:** `RoyalPyromancer.cs` solo gana la referencia y una llamada
`visuals?.OnCastStart()` en `CastVolley` (junto al `SetTrigger("Cast")` existente,
`RoyalPyromancer.cs:73`). HP, daño, orbes, drops y detección no cambian.

**Editor:** `SpriteAssigner.cs` se actualiza para importar/asignar los dos PNGs
nuevos y añadir `RoyalPyromancerVisuals` al boss (idempotente).

## Componentes nuevos — resumen

| Archivo | Tipo | Propósito |
|---|---|---|
| `Assets/Scripts/SpriteFlipbook.cs` | MonoBehaviour | Ciclo de frames del fireball |
| `Assets/Scripts/LightFlicker2D.cs` | MonoBehaviour | Parpadeo genérico de Light2D |
| `Assets/Scripts/PlayerAttackArm.cs` | MonoBehaviour | Animación procedural del brazo |
| `Assets/Scripts/RoyalPyromancerVisuals.cs` | MonoBehaviour | Levitación + pose de casteo del boss |
| `Assets/Editor/PlayerArmSetup.cs` | Editor | Menú "Configurar Brazo Jugador" |
| `Assets/Editor/PixelSpriteImport.cs` | Editor | Importador pixelart compartido: PPU, Point, pivote custom |
| `Assets/Editor/ProjectileDresser.cs` | Editor | Menú "Vestir Proyectil": prefab fireball + estela |

Archivos modificados: `Proyectil.prefab`, `FlameProjectile.cs`, `ProjectileGlow.cs`,
`PlayerCombat.cs`, `RoyalPyromancer.cs`, `SpriteAssigner.cs` (solo cubre los sprites de
`Characters/`; los PNGs de `Effects/` se importan vía `PixelSpriteImport`, invocado
desde `ProjectileDresser.cs` y `PlayerArmSetup.cs`).
Arte nuevo: 4 fireball + 2 brazos + 2 boss = 8 PNGs (generados por script Python/PIL
en scratchpad; solo los PNGs finales entran al repo).

## Verificación (manual, en editor)

El proyecto no tiene infraestructura de tests de Unity; la verificación es una
checklist en el editor sobre ambos niveles:

1. Correr "Candle Fury/Asignar Sprites" y "Configurar Brazo Jugador" en Level1 y
   Level2; correrlos dos veces y confirmar que no duplican objetos.
2. En Play: los 3 ataques (Z espada, X pulso, C proyectil) muestran el brazo con su
   animación, mirando a ambos lados.
3. El proyectil muestra la bola de fuego animada con estela; en Level 2 a oscuras se
   ve su luz parpadeante.
4. El boss levita en idle y cambia a pose de casteo al lanzar orbes; su flash de daño
   sigue funcionando; sus stats y comportamiento no cambian.
5. Flip de dirección, colliders y pickups intactos.

---

# Addendum (2026-07-21, tras checklist en Play): espada, UI y onda de fuego

Feedback del usuario en la verificación: (1) la espada dibujada dentro del sprite del
jugador no se mueve y el overlay parece "otra cosa"; (2) quiere una barra de vida más
estética y el botón "volver a jugar" mejor ubicado (hoy el botón está siempre visible
en pantalla porque el panel de game over nunca se activa: las referencias del
UIManager están sin asignar en escena); (3) quiere un visual de "onda de fuego" para
el ataque X (hoy el pulso no tiene ningún efecto visible).

Decisión del usuario: la espada del overlay queda SIEMPRE visible en pose de guardia.

## 5. Fix de la espada integrada

- Se BORRA la espada dibujada de `principe_vela.png` (edición de píxeles por región,
  mismo archivo y guid — nada que recablear). La espada del overlay pasa a ser la
  única espada.
- `PlayerAttackArm` cambia a "guardia siempre visible": el SpriteRenderer del brazo
  queda habilitado permanentemente con `arm_sword` en pose de guardia (rotación
  −30°), el swing parte de la guardia, y durante Shoot/Pulse el sprite cambia a
  `arm_cast` y vuelve a la guardia al terminar.

## 6. Barra de cera estética

- Sprites pixelart nuevos en `Assets/Sprites/UI/`: `ui_bar_frame.png` (80×10, borde
  3px para 9-slice), `ui_bar_fill.png` (4×4 blanco, se tiñe por código), e
  `ui_icon_flame.png` (12×12, llama cian).
- El Slider existente se re-viste: hijo `Background` nuevo (frame 9-slice oscuro),
  Fill con `ui_bar_fill` (el lerp cian→rojo de `UIManager.UpdateWaxBar` por fin
  funciona al asignar `waxFill`), Handle deshabilitado (barra sin perilla), icono de
  llama a la izquierda.
- Reposición: ancla/pivote top-left (0,1), posición (20,−16), tamaño (200,16).
- `CanvasScaler` pasa a Scale With Screen Size (ref 800×600, match 0.5) para que la
  UI escale con la resolución.

## 7. Panel de game over y botón

- `PanelGameOver` gana `RectTransform` full-stretch + `Image` negra semitransparente
  (alpha 0.78), queda INACTIVO por defecto y solo lo activa `ShowGameOver()`.
- Botón centrado (220×44, ancla 0.5/0.5, pos (0,−30)) con sprite pixelart
  `ui_button.png` (32×16, borde 4px 9-slice) y su texto TMP; conserva su onClick
  persistente a `GameManager.RestartGame`.
- Texto nuevo `GameOverKills` (TMP) sobre el botón; se asigna a
  `UIManager.gameOverKillText` junto con `gameOverPanel` y `waxFill` (referencias hoy
  en null).
- Todo lo hace un editor script idempotente nuevo: menú "Candle Fury/Vestir UI"
  (`UiDresser.cs`), en ambas escenas.

## 8. Onda de fuego del pulso (X)

- Arte: `pulse_wave_0..2.png` (3 frames de anillo 48×48, paleta cian con núcleo
  claro).
- Prefab nuevo `Assets/Prefabs/PulseWave.prefab`: SpriteRenderer (unlit, frame 0) +
  `SpriteFlipbook` + componente nuevo `PulseWaveEffect` que escala el anillo desde
  ~0.5u hasta el diámetro real del daño (6u = radio 3 del pulso) en ~0.35s con fade
  de alpha 1→0 y se autodestruye.
- `PlayerCombat` gana campo serializado `pulseWavePrefab` y lo instancia en
  `FlameCheck()`; el prefab lo construye y cablea (en ambas escenas) un editor script
  nuevo `PulseWaveBuilder.cs` (menú "Candle Fury/Construir Onda de Pulso").

## Verificación del addendum (manual, en Play)

1. En reposo el príncipe empuña UNA espada (la del overlay, en guardia); con Z hace
   el swing; no queda ninguna espada estática pegada al cuerpo.
2. La barra de cera se ve arriba a la izquierda con marco pixelart, se vacía y cambia
   de color al recibir daño; sin perilla flotante.
3. El botón "Volver a Jugar" NO se ve durante el juego; al morir aparece el panel
   oscuro centrado con el conteo de kills y el botón funcional.
4. Con X se emite el anillo de fuego que se expande hasta el borde del área de daño.
