using UnityEngine;

public class SpawnType : MonoBehaviour
{
    public EnemySpawner es; // Referencia al objeto EnemySpawner

    public void SetCommonSpawn()
    {
        // Establece el tipo de spawn a enemigos comunes
        es.SetSpawnType(EnemySpawner.SpawnType.Common);
    }

    public void SetFireSpawn()
    {
        // Establece el tipo de spawn a enemigos de fuego
        es.SetSpawnType(EnemySpawner.SpawnType.Fire);
    }
    public void SetColdSpawn()
    {
        // Establece el tipo de spawn a enemigos de frio
        es.SetSpawnType(EnemySpawner.SpawnType.Cold);
    }

    public void SetLightningSpawn()
    {
        // Establece el tipo de spawn a enemigos de rayo
        es.SetSpawnType(EnemySpawner.SpawnType.Lightning);
    }

    public void SetPoisonSpawn()
    {
        // Establece el tipo de spawn a enemigos de veneno
        es.SetSpawnType(EnemySpawner.SpawnType.Poison);
    }

    public void SetRandomSpawn()
    {
        // Establece el tipo de spawn a enemigos random
        es.SetSpawnType(EnemySpawner.SpawnType.Random);
    }
}
