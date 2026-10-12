using UnityEngine;

public class EnemyDespawner : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // Si el objeto que colisiona con el despawner es un enemigo, lo destruimos después de 0.5 segundos
        if (other.CompareTag("Enemy"))
        {
            Destroy(other.gameObject, 0.5f);
        }
    }
}
