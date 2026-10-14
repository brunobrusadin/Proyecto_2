using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    // Enum con los tipos de enemigos que se pueden generar
    public enum SpawnType : byte { Common, Fire, Cold, Lightning, Poison, Random }

    [Header("Referencias")]
    public GameObject spawnPoint;      // Punto de generación (inicio del mapa)
    public GameObject[] enemyPrefabs;  // Array de prefabs de enemigos que se pueden generar

    [Header("Configuración del spawn")]
    public float spawnInterval = 3f;   // Intervalo de tiempo entre cada spawn
    public float spawnStart = 2f;      // Tiempo de espera antes del primer spawn
    public int maxEnemies = 5;         // Máximo estricto de cápsulas en escena

    // Lista con las referencias de los enemigos generados (conteo interno, sin usar Tag)
    private readonly List<GameObject> spawnedEnemies = new List<GameObject>();

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
        // 1) Límite estricto: contamos las referencias vivas de forma interna
        if (CountAliveEnemies() >= maxEnemies)
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

        // 3) Instanciamos el enemigo y guardamos su referencia en la lista
        GameObject enemyPrefab = enemyPrefabs[spawnIndex];
        GameObject enemy = Instantiate(enemyPrefab, spawnPoint.transform.position, Quaternion.identity);
        spawnedEnemies.Add(enemy);
    }

    // Cuenta los enemigos vivos a partir de la lista interna y descarta las referencias ya destruidas
    private int CountAliveEnemies()
    {
        int aliveCount = 0;
        for (int i = spawnedEnemies.Count - 1; i >= 0; i--)
        {
            // 'enemy == null' es true cuando el GameObject ya fue destruido
            if (spawnedEnemies[i] == null)
            {
                spawnedEnemies.RemoveAt(i);
            }
            else
            {
                aliveCount++;
            }
        }
        return aliveCount;
    }

    // La UI puede fijar un tipo concreto; al usarse, se desactiva la rotación automática
    public void SetSpawnType(SpawnType newSpawnType)
    {
        manual = true;
        spawn = newSpawnType;
    }
}
