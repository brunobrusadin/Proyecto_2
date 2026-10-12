using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class Common_Enemy : MonoBehaviour
{
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private string endTileName = "End_Tile";

    private Transform endTile;

    private void Awake()
    {
        if (agent == null)
        {
            agent = GetComponent<NavMeshAgent>();
        }
    }

    private void Start()
    {
        GameObject endTileObject = GameObject.Find(endTileName);
        if (endTileObject != null)
        {
            endTile = endTileObject.transform;
        }
        SetDestinationToEnd();
    }

    private void Update()
    {
        if (agent != null && agent.isOnNavMesh && agent.hasPath && agent.pathStatus == NavMeshPathStatus.PathPartial)
        {
            SetDestinationToEnd();
        }
    }

    private void SetDestinationToEnd()
    {
        if (agent == null || endTile == null)
        {
            return;
        }
        agent.SetDestination(endTile.position);
    }
}