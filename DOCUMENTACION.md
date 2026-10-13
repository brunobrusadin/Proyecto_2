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
| `NavMeshAgent` | Prefabs de enemigos (`Common_Enemy`) | Mueve cada enemigo por la malla evitando obstáculos |
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

### 4.1 Componente añadido al prefab `Common_Enemy`

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
| Walkable Mask | Todas las áreas caminables (excluye `Not Walkable`) |

El prefab `Common_Enemy` contiene: `Transform`, `MeshFilter`, `MeshRenderer`, `CapsuleCollider`,
**`NavMeshAgent`** y el script **`Common_Enemy`** (con la referencia al agente ya asignada).

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

## 5. NavMesh Modifiers: terrenos especiales (alto costo) y muros

Las casillas especiales del mapa son instancias de los prefabs:
`Fire_Tile`, `Cold_Tile`, `Lightning_Tile`, `Poison_Tile` (agrupadas bajo `Special`, capa 6).

A los **terrenos especiales** se les asigna un **área transitable de alto costo** (ver tabla). El **mismo
componente** se aplica al prefab `Wall_Tile` (bajo `Walls`, capa 7), pero con `Area = Not Walkable`, para que
cada muro recorte la malla como un hueco.

Cada una debe llevar un componente **`NavMeshModifier`** configurado así:

| Ajuste | Valor | Efecto |
|---|---|---|
| Override Area | `On` | La casilla usa un área propia en vez de la del Surface |
| Area | `Water` | La casilla es **transitable** pero con **alto costo** (área 3, coste 10) |
| Ignore From Build | `Off` | Aunque es no transitable, **sí participa** del horneado (recorta la malla) |
| Affects Agent Type | `Humanoid` | Solo afecta a los agentes Humanoid |

### 5.1 Resultado sobre la malla

1. El `NavMeshSurface` hornea el suelo (`Ground`) como área `Walkable`.
2. Cada casilla `Fire`/`Cold`/`Lightning`/`Poison` con `Override Area → Water` (coste 10) **sí aparece** en
   la malla como transitable.
3. Los `NavMeshAgent` prefieren el suelo `Walkable` (coste 1) y **solo pisan el área `Water`** cuando el
   pasillo normal está bloqueado (p. ej. por un obstáculo), evitando quedar varados.
4. `Walls` llevan el mismo `NavMeshModifier → Not Walkable`: cada `Wall_Tile` abre un **hueco** en la malla, de modo que los agentes solo transitan por los pasillos.

### 5.2 Áreas de navegación usadas

| Área | Índice | Coste | Uso |
|---|---|---|---|
| `Walkable` | 0 | 1 | Suelo normal; ruta preferida por los enemigos |
| `Not Walkable` | 1 | 1 | Muros (`Wall_Tile`): no transitables (huecos en la malla) |
| `Water` | 3 | 10 | Terrenos especiales (Fire, Cold, Lightning, Poison): transitables pero caros |

> Cualquier área adicional (`Jump`, `Swim`, costos específicos) puede añadirse desde
> *Window → AI → Navigation* con sus costos por agente en la pestaña *Areas*.

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

---

## 6. Scripts relacionados del mapa

| Script | Rol |
|---|---|
| `EnemySpawner.cs` | Instancia enemigos periódicamente desde `Spawn_Point` y cambia el tipo de oleada |
| `EnemyDespawner.cs` | Destruye enemigos que llegan a la meta (`OnTriggerEnter`, tag `Enemy`) |
| `ObstacleSpawner.cs` | Coloca/elimina obstáculos con el mouse para alterar el camino de los agentes |
| `ObstacleControls.cs` | Botones de UI que activan/quitan el spawn de obstáculos |
| `SpawnType.cs` | Botones de UI que eligen el tipo de enemigo a spawnear |
| `Common_Enemy.cs` | Navega hasta `End_Tile` con `agent.SetDestination()` |

---

## 7. Requisitos para ejecutar

- Paquete **AI Navigation** instalado (`com.unity.ai.navigation` 2.0.14) — ya presente en el proyecto.
- `NavMeshSurface` en `Terrain` con la malla **horneada** (agente Humanoid con `Radius = 0.25`).
- Prefab `Common_Enemy` con `NavMeshAgent` (`Radius = 0.25`) + script `Common_Enemy` — ya configurados.
- `NavMeshModifier` aplicado a `Wall_Tile` (`Not Walkable`) y a las casillas especiales Fire/Cold/Lightning/Poison (área `Water`, coste 10).
- `End_Tile` presente en la escena con nombre exacto `End_Tile` (el script lo busca por nombre).

---

## 8. Cambios finales y verificación

Ajustes aplicados para que el horneado genere un laberinto navegable en lugar de un plano continuo
(o de una malla vacía):

1. **`NavMeshModifier → Not Walkable` en `Wall_Tile`**: los muros dejan de ser "escalones" y pasan a
   recortar la malla como huecos.
2. **Radio del agente Humanoid `0.5 → 0.25`** (tanto en los ajustes de horneado como en el
   `NavMeshAgent` del prefab `Common_Enemy`), alineado con el ancho de pasillo de 1 tile.
3. **Casillas especiales (Fire/Cold/Lightning/Poison) → área `Water` (índice 3, coste 10)** en lugar de
   `Not Walkable`: ahora son **transitables pero caras**, de modo que la ruta por defecto sigue siendo el
   suelo `Walkable` (coste 1) pero, si el pasillo normal se bloquea, el agente prefiere cruzar el terreno
   especial en vez de quedar varado.
4. **Re-horneado** del `NavMeshSurface` con persistencia del asset
   `Assets/Scenes/LevelScene/NavMesh-Terrain.asset` y guardado de la escena `LevelScene`.

**Resultado verificado en ejecución:**

- Malla resultante: **607 vértices / 283 triángulos** (incluye ahora las casillas especiales como área `Water`).
- Ruta `Spawn_Point (-12, 0)` → `End_Tile (-12, 14)` por defecto: `NavMeshPath.status = PathComplete`, **27 corners**, sin pisar el área `Water`.
- Al **excluir el área 3** de la máscara, esa misma ruta pasa a `PathPartial`: confirma que las casillas
  especiales actúan como desvío transitable cuando hace falta.
- Coste en ejecución: `NavMesh.GetAreaCost(3) = 10`, `NavMesh.GetAreaFromName("Water") = 3`.