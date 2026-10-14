using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    // Enum con los tipos de enemigos que se pueden generar
    public enum SpawnType : byte { Common, Fire, Cold, Lightning, Poison, Random }

    [Header("Referencias")]
    public GameObject spawnPoint;      // Punto de generación (inicio del mapa)
    public GameObject[] enemyPrefabs;  // Prefabs en orden: Common, Fire, Cold, Lightning, Poison

    [Header("Configuracion de spawn")]
    public float spawnInterval = 3f;   // Intervalo de tiempo entre cada spawn (3 segundos)
    public float spawnStart = 2f;      // Tiempo de espera antes del primer spawn
    public int maxEnemies = 5;         // Máximo de enemigos vivos al mismo tiempo

    private int cycleIndex = 0;        // Posición actual en la rotación de tipos
    private bool manual = false;       // Si la UI fija un tipo, se desactiva la rotación
    private SpawnType spawn;           // Tipo elegido manualmente desde la UI

    void Start()
    {
        // Arrancamos el spawn repetido: primer spawn tras 'spawnStart', luego cada 'spawnInterval'
        InvokeRepeating("SpawnNextEnemy", spawnStart, spawnInterval);
    }

    private void SpawnNextEnemy()
    {
        // 1) Límite: si ya hay 'maxEnemies' enemigos vivos, no generamos ninguno más
        if (GameObject.FindGameObjectsWithTag("Enemy").Length >= maxEnemies)
        {
            return;
        }

        // 2) Elegimos el tipo de enemigo que toca
        int spawnIndex;
        if (manual)
        {
            // Modo manual: la UI eligió un tipo concreto
            spawnIndex = (spawn == SpawnType.Random) ? Random.Range(0, enemyPrefabs.Length) : (int)spawn;
        }
        else
        {
            // Rotación automática en orden: Common -> Fire -> Cold -> Lightning -> Poison -> ...
            spawnIndex = cycleIndex % enemyPrefabs.Length;
            cycleIndex++;
        }

        // 3) Instanciamos el enemigo en la posición del punto de spawn
        GameObject enemyPrefab = enemyPrefabs[spawnIndex];
        Instantiate(enemyPrefab, spawnPoint.transform.position, Quaternion.identity);
    }

    // La UI puede fijar un tipo concreto; al usarse, se desactiva la rotación automática
    public void SetSpawnType(SpawnType newSpawnType)
    {
        manual = true;
        spawn = newSpawnType;
    }
}
