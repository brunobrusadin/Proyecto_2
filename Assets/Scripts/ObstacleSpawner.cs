using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class ObstacleSpawner : MonoBehaviour
{
    public GameObject obstaclePrefab; // Referencia al prefab de los obstaculos
    public LayerMask groundLayer; // Capa del suelo para detectar donde poner los obstaculos 
    public LayerMask obstacleLayer; // Capa de obstaculos (colocados y para evitarlos)
    public LayerMask noObstacleLayer; // Capa de zonas donde no se pueden poner obstaculos
    public bool startActivated = true; // Si esta activo, el clic coloca obstaculos desde el arranque
    private bool spawnActivated = false; // Flag para activar el spawn de obstaculos
    private int obstacleCount = 0; // Cantidad de obstaculos colocados
    private int maxObstacles = 5; // Maxima cantidad de obstaculos que se pueden colocar
    private GameObject[] placedObstacles; // Arreglo de obstaculos colocados para poder eliminarlos

    void Start()
    {
        // Inicializamos el arreglo de obstaculos colocados con el tamaño maximo
        placedObstacles = new GameObject[maxObstacles];
        // El spawn arranca activado para que el clic izquierdo funcione de inmediato
        spawnActivated = startActivated;
    }

    void Update()
    {
        // Verificamos si el mouse esta sobre un boton para no interactuar con el mapa
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        // Leemos la posicion del mouse y que boton se presiono en este frame
        Vector2 mousePosition;
        bool leftPressed;
        bool rightPressed;
        if (!ReadPointer(out mousePosition, out leftPressed, out rightPressed))
        {
            return;
        }

        // Solo actuamos si hubo un clic
        if (!leftPressed && !rightPressed)
        {
            return;
        }

        if (Camera.main == null || obstaclePrefab == null)
        {
            return;
        }

        Ray r = Camera.main.ScreenPointToRay(mousePosition);

        // 1) QUITAR: clic izquierdo o derecho sobre un obstaculo ya colocado
        if (Physics.Raycast(r, out RaycastHit obstacleHit, Mathf.Infinity, obstacleLayer))
        {
            if (TryRemoveObstacle(obstacleHit.collider.gameObject))
            {
                return;
            }
        }

        // 2) COLOCAR: solo con clic izquierdo, sobre una casilla de suelo vacia
        if (!leftPressed)
        {
            return;
        }

        if (Physics.Raycast(r, out RaycastHit hit, Mathf.Infinity, groundLayer))
        {
            // Tomamos la posicion del centro del cubo de suelo y calculamos donde se pega el obstaculo
            Vector3 groundCenter = hit.transform.position;
            Vector3 snapPosition = new Vector3(Mathf.Round(groundCenter.x), groundCenter.y + 1f, Mathf.Round(groundCenter.z));

            // Si el spawn esta activado y no se ha llegado al maximo de obstaculos
            if (spawnActivated && obstacleCount < maxObstacles)
            {
                // Cubo invisible de prueba para verificar si hay espacio libre
                Vector3 testPosition = new Vector3(0.4f, 0.4f, 0.4f);
                bool noObstaclesZone = Physics.CheckBox(snapPosition, testPosition, Quaternion.identity, noObstacleLayer);
                bool occupiedSpace = Physics.CheckBox(snapPosition, testPosition, Quaternion.identity, obstacleLayer);
                // Si estas pruebas pasan, colocamos el obstaculo y aumentamos el contador
                if (!noObstaclesZone && !occupiedSpace)
                {
                    placedObstacles[obstacleCount] = Instantiate(obstaclePrefab, snapPosition, Quaternion.identity);
                    obstacleCount++;
                }
            }
        }
    }

    // Elimina un obstaculo colocado (si el objeto golpeado pertenece a uno de los registrados)
    private bool TryRemoveObstacle(GameObject candidate)
    {
        for (int i = 0; i < obstacleCount; i++)
        {
            GameObject placed = placedObstacles[i];
            if (placed == null)
            {
                continue;
            }
            if (placed == candidate || candidate.transform.IsChildOf(placed.transform))
            {
                // Destruimos el obstaculo: al destruirse, su NavMeshObstacle deja de hacer Carving
                Destroy(placed);
                // Compactamos el arreglo para liberar el cupo y permitir colocar otro
                for (int j = i; j < obstacleCount - 1; j++)
                {
                    placedObstacles[j] = placedObstacles[j + 1];
                }
                placedObstacles[obstacleCount - 1] = null;
                obstacleCount--;
                return true;
            }
        }
        return false;
    }

    // Lee la posicion del mouse y los clics izquierdo/derecho segun el backend de input activo
    private bool ReadPointer(out Vector2 mousePosition, out bool leftPressed, out bool rightPressed)
    {
        mousePosition = Vector2.zero;
        leftPressed = false;
        rightPressed = false;
#if ENABLE_INPUT_SYSTEM
        // Input System (New): lectura directa del dispositivo mouse
        if (Mouse.current != null)
        {
            mousePosition = Mouse.current.position.ReadValue();
            leftPressed = Mouse.current.leftButton.wasPressedThisFrame;
            rightPressed = Mouse.current.rightButton.wasPressedThisFrame;
            return true;
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        // Input Manager (Old): solo compila si el manejo legacy esta habilitado
        mousePosition = Input.mousePosition;
        leftPressed = Input.GetMouseButtonDown(0);
        rightPressed = Input.GetMouseButtonDown(1);
        return true;
#else
        return false;
#endif
    }

    public void RemoveObstacles()
    {
        // Eliminamos todos los obstaculos colocados y reiniciamos el contador
        for (int i = 0; i < obstacleCount; i++)
        {
            Destroy(placedObstacles[i]);
        }
        obstacleCount = 0;
    }

    public void ActivateSpawning()
    {
        // Activamos el spawn de obstaculos
        spawnActivated = true;
    }
}
