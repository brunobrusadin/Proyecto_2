using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    // Enum para representar los diferentes tipos de enemigos que se pueden generar
    public enum SpawnType : byte { Common, Fire, Cold, Lightning, Poison, Random }
    public GameObject spawnPoint; // Refencia al punto de generación de enemigos, al principio del mapa
    public GameObject[] enemyPrefabs; // Array de prefabs de enemigos que se pueden generar
    private float spawnRate = 5f; // Intervalo de tiempo entre cada spawn
    private float spawnStart = 2f; // Tiempo de espera antes de que comience el primer spawn
    private SpawnType spawn; // Tipo de enemigo que se va a generar

    void Start()
    {
        // Seteamos el spawn inicial a enemigos comunes
        spawn = SpawnType.Common;
        // Iniciamos el spawn de enemigos
        InvokeRepeating("SpawnRandomEnemy", spawnStart, spawnRate);
    }

    private void SpawnRandomEnemy()
    {
        int spawnIndex = 0;
        // Si el tipo de spawn es Random, seleccionamos un índice aleatorio del array de prefabs de enemigos
        if (spawn == SpawnType.Random)
        {
            spawnIndex = Random.Range(0, enemyPrefabs.Length);
        }
        // Si no, usamos el índice correspondiente al tipo de spawn seleccionado
        else
        {
            spawnIndex = (int)spawn;
        }
        // Instanciamos el prefab del enemigo seleccionado en la posición del spawn point
        GameObject enemyPrefab = enemyPrefabs[spawnIndex];
        Instantiate(enemyPrefab, spawnPoint.transform.position, Quaternion.identity);
    }

    public void SetSpawnType(SpawnType newSpawnType)
    {
        // Cambiamos el tipo de spawn a uno nuevo
        spawn = newSpawnType;
    }
}
