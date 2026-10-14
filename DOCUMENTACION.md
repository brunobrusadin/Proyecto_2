# DOCUMENTACIÓN — Navegación por NavMesh

**Proyecto 2 · Navegación de enemigos con NavMesh**
**Materia:** Modelos y Métodos Inteligentes
**Modalidad:** Entrega individual (1–2 alumnos)

---

## 1. Resumen del proyecto

Juego en Unity 3D donde oleadas de enemigos (`Common_Enemy`, `Fire_Enemy`, `Cold_Enemy`,
`Lightning_Enemy`, `Poison_Enemy`) nacen en el punto `Spawn_Point` y deben recorrer el mapa hasta
llegar a `End_Tile`. El jugador puede colocar obstáculos para desviar el camino de los enemigos
(`ObstacleSpawner`). Los enemigos se desplazan mediante **NavMesh Agent** sobre un **NavMesh Surface**
horneado desde el terreno. Cada casilla especial (`Fire`, `Cold`, `Lightning`, `Poison`) tiene su propia
**área de navegación** mediante **NavMesh Modifiers**, y cada tipo de enemigo restringe con su
`NavMeshAgent` qué áreas puede pisar (y a qué coste): el enemigo común **no** pisa ningún terreno de color
y cada enemigo de color prohíbe una de las áreas ajenas (ver §5.4).

El movimiento de cada enemigo lo resuelve el script `Common_Enemy.cs`, que usa su `NavMeshAgent`
para navegar hasta `End_Tile` a través de `agent.SetDestination()`.

---

## 2. Arquitectura de la navegación

La navegación se compone de tres piezas del paquete **AI Navigation** (`com.unity.ai.navigation` 2.0.14):

| Pieza | Dónde | Función |
|---|---|---|
| `NavMeshSurface` | Objeto raíz `Terrain` | Genera la malla de navegación a partir de las geometrías del mapa |
| `NavMeshAgent` | Prefabs de enemigos (los 5 tipos) + script `EnemyAreaRules` | Mueve cada enemigo por la malla evitando obstáculos, con su máscara de áreas y costes |
| `NavMeshModifier` | Terrenos especiales `Fire`/`Cold`/`Lightning`/`Poison` (áreas 3–6) y muros `Wall_Tile` | Marca los muros como **Not Walkable** (huecos) y cada terreno especial con **su propia área de color** (transitable, costo propio) |
| `NavMeshObstacle` | Prefab `Obstacle` (colocado con clic) | **Recorta** la malla dinámicamente (`Carve = On`) al colocar el obstáculo |

Los **NavMesh Modifiers** no bloquean con física: solo "recortan" la malla de navegación, de modo que
los agentes la evitan sin saltos ni colisiones.

---

## 3. Configuración de la NavMesh Surface

`NavMeshSurface` se añade al GameObject raíz `Terrain` (padre de `Ground`, `Walls` y `Special`).

### 3.1 Parámetros principales

- **Agent Type:** `Humanoid`
- **Collect Objects:** `All` (captura todos los colliders/meshes con marcado estático de navegación)
- **Include Layers:** `Everything` (la malla incluye suelo y paredes)
- **Use Geometry:** `Physics Colliders`
- **Default Area:** `Walkable` (área 0) — todo lo que no tenga override queda transitable

### 3.2 Ajustes del agente usados al hornear

| Ajuste | Valor | Descripción |
|---|---|---|
| Radius | `0.25` | Radio de la cápsula del agente. **Debe ser menor que la mitad del ancho de pasillo (1 tile)**: con `0.5` la erosión dejaba la malla vacía |
| Height | `2` | Alto de la cápsula |
| Max Slope | `45` | Pendiente máxima transitable |
| Step Height | `0.75` | Altura de escalones que el agente puede subir |
| Max Climb / Drop | `0.75` | Saltos y descensos permitidos |

> **Importante — radio del agente vs. ancho de pasillo:** los pasillos del laberinto miden **1 tile**.
> El horneado *erosiona* la superficie caminable `Radius` unidades por cada lado, así que un pasillo de
> ancho `1` solo sobrevive si `Radius < 0.5`. Por eso el agente Humanoid usa `Radius = 0.25`
> (deja ~0.5 de ancho navegable). Con el radio original `0.5` la malla resultante quedaba **vacía**.

### 3.3 Horneado

1. Seleccionar `Terrain` → `NavMeshSurface` → **Bake**.
2. El horneado recorre `Ground`, `Walls` y las casillas especiales.
3. Los muros (`Wall_Tile`) llevan `NavMeshModifier → Not Walkable`, de modo que **recortan la malla como huecos** (además de tener colliders físicos). Si solo se usaran los colliders, los muros del laberinto (que sobresalen apenas 0.5 sobre el suelo, por debajo del *Step Height*) se tratarían como escalones y la malla saldría como un **plano continuo**.
4. Las casillas especiales llevan `NavMeshModifier → Override Area` con **un área por color**: `Fire` (3), `Cold` (4), `Lightning` (5) y `Poison` (6). Son **transitables** pero caras, de modo que cada enemigo prefiere su propio color y evita las áreas ajenas (ver §5 y §5.4).
5. Los obstáculos colocados en *runtime* (`ObstacleSpawner`) usan un **`NavMeshObstacle` con `Carve = On`**
   (ver §5.3): recortan la malla dinámicamente al instanciarse, sin re-hornear, y los agentes recalculan su ruta.

> **Nota:** la malla debe hornearse en el editor (o al iniciar la escena) antes de ejecutar; sin
> `NavMesh`, los agentes no tienen por dónde desplazarse.

---

## 4. Configuración del NavMesh Agent en los enemigos

### 4.1 Componentes añadidos a los prefabs de enemigos

| Ajuste | Valor |
|---|---|
| Agent Type | `Humanoid` |
| Speed | `3.5` |
| Acceleration | `10` |
| Angular Speed | `120` |
| Stopping Distance | `0` |
| Auto Braking | `On` |
| Auto Repath | `On` |
| Obstacle Avoidance | `High Quality` |
| Radius | `0.25` |
| Height | `2` |
| Base Offset | `0` |
| Walkable Mask | `Walkable` únicamente (área 0) — el enemigo común **no** pisa ningún color |

El prefab `Common_Enemy` contiene: `Transform`, `MeshFilter`, `MeshRenderer`, `CapsuleCollider`,
**`NavMeshAgent`**, el script **`Common_Enemy`** y el script **`EnemyAreaRules`** (que fija la máscara de
áreas y los costos por agente en `Awake`). La referencia al agente se auto-asigna en `Awake`.

### 4.2 Lógica de navegación (`Common_Enemy.cs`)

El script implementa el recorrido hacia la meta:

```csharp
[SerializeField] NavMeshAgent agent;   // referencia al NavMeshAgent (auto-asignada en Awake)
[SerializeField] string endTileName = "End_Tile";

void Start() {
    endTile = GameObject.Find(endTileName).transform;  // localiza la meta
    SetDestinationToEnd();                             // arranca la navegación
}

void SetDestinationToEnd() {
    agent.SetDestination(endTile.position);            // rumbo hacia End_Tile
}
```

- En `Update` se re-emite `SetDestination()` si la ruta quedó **parcial** (p. ej. tras colocar un
  obstáculo con `ObstacleSpawner`), para que el enemigo recalcule el camino.
- Al llegar al final, el `EnemyDespawner` (trigger en la zona de `End_Tile`) destruye al enemigo
  (tag `Enemy`).

### 4.3 Flujo completo

```
EnemySpawner (InvokeRepeating + spawnPoint)
   └─► Instantiate(Common_Enemy, Spawn_Point)
          └─► Common_Enemy.Start() → GameObject.Find("End_Tile")
                 └─► agent.SetDestination(End_Tile.position)
                        └─► NavMeshAgent navega por la malla (evita Not Walkable)
                               └─► llega a End_Tile → EnemyDespawner.OnTriggerEnter → Destroy
```

---

## 5. NavMesh Modifiers: terrenos especiales por color y muros

Las casillas especiales del mapa son instancias de los prefabs:
`Fire_Tile`, `Cold_Tile`, `Lightning_Tile`, `Poison_Tile` (agrupadas bajo `Special`, capa 6).

A cada **terreno especial** se le asigna **su propia área de color** (transitable, costo propio). El
**mismo componente** se aplica al prefab `Wall_Tile` (bajo `Walls`, capa 7), pero con
`Area = Not Walkable`, para que cada muro recorte la malla como un hueco.

Cada casilla especial lleva un componente **`NavMeshModifier`** configurado así:

| Ajuste | Valor | Efecto |
|---|---|---|
| Override Area | `On` | La casilla usa un área propia en vez de la del Surface |
| Area | `Fire` / `Cold` / `Lightning` / `Poison` | Área de color propia de la casilla (índices 3–6) |
| Ignore From Build | `Off` | La casilla **sí participa** del horneado (aparece transitable en la malla) |
| Affects Agent Type | `Humanoid` | Solo afecta a los agentes Humanoid |

### 5.1 Resultado sobre la malla

1. El `NavMeshSurface` hornea el suelo (`Ground`) como área `Walkable`.
2. Cada casilla `Fire`/`Cold`/`Lightning`/`Poison` con `Override Area` **sí aparece** en la malla como
   transitable, pero etiquetada con **su** área de color (3/4/5/6).
3. Cada `NavMeshAgent` filtra las áreas con su `Walkable Mask` y les da un **costo propio** (ver §5.4): el
   enemigo común usa solo `Walkable`; cada enemigo de color prefiere su propia área (coste 1) y solo pisa
   las áreas ajenas permitidas a **alto costo**, mientras que su área prohibida queda **fuera de su máscara**.
4. `Walls` llevan el mismo `NavMeshModifier → Not Walkable`: cada `Wall_Tile` abre un **hueco** en la malla, de modo que los agentes solo transitan por los pasillos.

### 5.2 Áreas de navegación usadas

| Área | Índice | Coste base | Uso |
|---|---|---|---|
| `Walkable` | 0 | 1 | Suelo normal; ruta preferida por todos los enemigos |
| `Not Walkable` | 1 | 1 | Muros (`Wall_Tile`): no transitables (huecos en la malla) |
| `Jump` | 2 | 2 | Reservada (sin uso actual) |
| `Fire` | 3 | 2 | Casillas `Fire_Tile` (naranja) |
| `Cold` | 4 | 3 | Casillas `Cold_Tile` (azul) |
| `Lightning` | 5 | 4 | Casillas `Lightning_Tile` (amarillo) |
| `Poison` | 6 | 5 | Casillas `Poison_Tile` (verde) |

> Los costes **base** son los del proyecto (*Project Settings → Navigation → Areas*). Además, cada
> enemigo de color **sobreescribe los costos en tiempo de ejecución** con `NavMeshAgent.SetAreaCost()`
> para que sus áreas ajenas sean mucho más caras (§5.4).

### 5.3 NavMesh Obstacle en los obstáculos (Carve)

El prefab `Obstacle.prefab` (instanciado por `ObstacleSpawner` al hacer clic) lleva un componente
**`NavMeshObstacle`** configurado para recortar el NavMesh en tiempo real:

| Ajuste | Valor | Efecto |
|---|---|---|
| Shape | `Box` | Coincide con la forma del cubo |
| Size | `(1, 1, 1)` | Igual que su `BoxCollider` |
| Carve | `On` | **Recorta** la malla al colocarse, creando un hueco bloqueado |
| Carve Only Stationary | `On` | El recorte se aplica cuando el obstáculo está quieto (evita recalcular en cada frame) |

Con `Carve = On` el enemigo **no atraviesa** el obstáculo: su `NavMeshAgent` detecta el hueco y, gracias a
`Auto Repath`, recalcula el camino para rodearlo en tiempo real (sin re-hornear la malla).

### 5.4 Matriz de áreas, colores y costos por tipo de agente

Cada prefab de enemigo lleva el script `EnemyAreaRules`, que en `Awake` fija su **máscara de áreas**
(`NavMeshAgent.areaMask`) y un **costo por área** propio (`NavMeshAgent.SetAreaCost`). El enemigo
**común** solo puede pisar `Walkable`; cada enemigo de color puede pisar `Walkable`, **su** área (barata)
y **dos** áreas ajenas (una a coste medio y otra a coste alto), y tiene **una** área **prohibida** (queda
fuera de su máscara).

| Enemigo | Color (RGB) | Material | Área propia (coste) | Área prohibida | Otras permitidas (coste) | `areaMask` |
|---|---|---|---|---|---|---|
| `Common_Enemy` | blanco (1, 1, 1) | `Common.mat` | — | Fire, Cold, Lightning, Poison | — | `1` (solo Walkable) |
| `Fire_Enemy` | naranja (1, 0.334, 0) | `Fire.mat` | Fire (1) | Cold | Lightning (5), Poison (10) | `105` |
| `Cold_Enemy` | azul (0, 0.24, 0.849) | `Cold.mat` | Cold (1) | Lightning | Poison (5), Fire (10) | `89` |
| `Lightning_Enemy` | amarillo (1, 0.953, 0) | `Lightning.mat` | Lightning (1) | Poison | Fire (5), Cold (10) | `57` |
| `Poison_Enemy` | verde (0, 0.509, 0.116) | `Poison.mat` | Poison (1) | Fire | Cold (5), Lightning (10) | `113` |

Notas de la matriz:

- **Enemigo común:** su máscara (`1` = bit del área 0) excluye las áreas 3–6, por lo que **no puede pisar
  ningún terreno de color**; si el camino normal se bloquea, se detiene antes que cruzar un color.
- **Enemigos de color:** cada uno tiene **una** área prohibida (bit a 0 en la máscara) y **dos** áreas
  ajenas permitidas con costos altos y **diferenciados** (5 y 10), de modo que siempre prefiere `Walkable`
  y su propio color.
- La máscara (`areaMask`) controla **qué** áreas se pueden pisar; los costos (`SetAreaCost`) controlan
  **cuánto** cuesta cada una. Ambos se configuran por agente.
- Bits de la máscara: `1<<0`=Walkable, `1<<3`=Fire, `1<<4`=Cold, `1<<5`=Lightning, `1<<6`=Poison.

---

## 6. Scripts relacionados del mapa

| Script | Rol |
|---|---|
| `EnemySpawner.cs` | Genera enemigos cada **3 s** rotando el tipo en orden (`Common`→`Fire`→`Cold`→`Lightning`→`Poison`), con un **máximo de 5 enemigos vivos** |
| `EnemyDespawner.cs` | Destruye enemigos que llegan a la meta (`OnTriggerEnter`, tag `Enemy`) |
| `ObstacleSpawner.cs` | Coloca/elimina obstáculos con el mouse para alterar el camino de los agentes |
| `ObstacleControls.cs` | Botones de UI que activan/quitan el spawn de obstáculos |
| `SpawnType.cs` | Botones de UI que eligen el tipo de enemigo a spawnear |
| `Common_Enemy.cs` | Navega hasta `End_Tile` con `agent.SetDestination()` |
| `EnemyAreaRules.cs` | Fija la máscara de áreas y los costos por agente del `NavMeshAgent` (matriz §5.4) |

---

## 7. Requisitos para ejecutar

- Paquete **AI Navigation** instalado (`com.unity.ai.navigation` 2.0.14) — ya presente en el proyecto.
- `NavMeshSurface` en `Terrain` con la malla **horneada** (agente Humanoid con `Radius = 0.25`).
- Prefabs de enemigos con `NavMeshAgent` (`Radius = 0.25`) + scripts `Common_Enemy` y `EnemyAreaRules` — ya configurados.
- `NavMeshModifier` aplicado a `Wall_Tile` (`Not Walkable`) y a las casillas especiales Fire/Cold/Lightning/Poison con **su área de color** (`Fire`=3, `Cold`=4, `Lightning`=5, `Poison`=6).
- `End_Tile` presente en la escena con nombre exacto `End_Tile` (el script lo busca por nombre).

---

## 8. Cambios finales y verificación

Ajustes aplicados para que el horneado genere un laberinto navegable en lugar de un plano continuo
(o de una malla vacía):

1. **`NavMeshModifier → Not Walkable` en `Wall_Tile`**: los muros dejan de ser "escalones" y pasan a
   recortar la malla como huecos.
2. **Radio del agente Humanoid `0.5 → 0.25`** (tanto en los ajustes de horneado como en el
   `NavMeshAgent` de los prefabs de enemigos), alineado con el ancho de pasillo de 1 tile.
3. **Áreas de color Fire/Cold/Lightning/Poison (índices 3–6)** con costes base **diferenciados** (2/3/4/5)
   definidas en *Project Settings → Navigation → Areas* y asignadas a cada casilla con
   `NavMeshModifier → Override Area` (en lugar de `Not Walkable`): las casillas son **transitables**.
4. **Máscaras y costes por agente** (`EnemyAreaRules`): el enemigo común usa solo `Walkable`
   (`areaMask = 1`, no pisa ningún color); cada enemigo de color prohíbe una de las áreas ajenas y da
   costes propios a las demás (ver §5.4).
5. **Re-horneado** del `NavMeshSurface` con persistencia del asset
   `Assets/Scenes/LevelScene/NavMesh-Terrain.asset` y guardado de la escena `LevelScene`.

**Resultado verificado:**

- Áreas en ejecución: `NavMesh.GetAreaFromName("Fire"/"Cold"/"Lightning"/"Poison") = 3/4/5/6`, con
  costes base `2/3/4/5`.
- Malla resultante: **607 vértices / 283 triángulos**.
- Máscaras de área serializadas en los prefabs: `Common = 1`, `Fire = 105`, `Cold = 89`,
  `Lightning = 57`, `Poison = 113` (los bits codifican `Walkable` + las áreas de color permitidas).
- Rutas `Spawn_Point (-12, 0)` → `End_Tile (-12, 14)` calculadas con la máscara y los costes de cada tipo
  de enemigo (`NavMesh.CalculatePath` + `NavMeshQueryFilter`):

  | Enemigo | Máscara | Estado | Tramos |
  |---|---|---|---|
  | `Common` | `1` | `PathComplete` | **46** (rodea todos los colores) |
  | `Fire` | `105` | `PathComplete` | 16 |
  | `Cold` | `89` | `PathComplete` | 12 |
  | `Lightning` | `57` | `PathComplete` | 24 |
  | `Poison` | `113` | `PathComplete` | 24 |

- El enemigo **común** obtiene una ruta mucho más larga (46 tramos) porque **no puede pisar ningún
  color**, mientras que los enemigos de color **acortan** el camino usando sus áreas permitidas:
  confirma que la máscara y los costes se aplican por agente.
- Material/color de cada prefab verificado: `Common` blanco, `Fire` naranja, `Cold` azul,
  `Lightning` amarillo, `Poison` verde.