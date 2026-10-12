using UnityEngine;

public class ObstacleControls : MonoBehaviour
{
    public ObstacleSpawner os; // Referencia al objeto ObstacleSpawner

    public void ActivateSpawning()
    {
        // Activa el spawn de obstáculos
        os.ActivateSpawning();
    }

    public void RemoveObstacles()
    {
        // Elimina todos los obstáculos del mapa
        os.RemoveObstacles();
    }
}
