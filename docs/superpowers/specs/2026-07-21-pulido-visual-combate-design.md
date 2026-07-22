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
