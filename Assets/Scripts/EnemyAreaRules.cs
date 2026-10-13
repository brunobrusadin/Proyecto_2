using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAreaRules : MonoBehaviour
{
    public enum Element { Common = 0, Fire = 1, Cold = 2, Lightning = 3, Poison = 4 }

    [SerializeField] private Element element = Element.Common;

    private const int AREA_WALKABLE = 0;
    private const int AREA_FIRE = 3;
    private const int AREA_COLD = 4;
    private const int AREA_LIGHTNING = 5;
    private const int AREA_POISON = 6;

    private NavMeshAgent agent;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        ApplyAreaMask();
        ApplyAreaCosts();
    }

    private void OnEnable()
    {
        ApplyAreaMask();
        ApplyAreaCosts();
    }

    private void Start()
    {
        ApplyAreaMask();
        ApplyAreaCosts();
    }

    private static int Bit(int area)
    {
        return 1 << area;
    }

    private void ApplyAreaMask()
    {
        switch (element)
        {
            case Element.Common:
                agent.areaMask = Bit(AREA_WALKABLE);
                break;
            case Element.Fire:
                agent.areaMask = Bit(AREA_WALKABLE) | Bit(AREA_FIRE) | Bit(AREA_LIGHTNING) | Bit(AREA_POISON);
                break;
            case Element.Cold:
                agent.areaMask = Bit(AREA_WALKABLE) | Bit(AREA_COLD) | Bit(AREA_FIRE) | Bit(AREA_POISON);
                break;
            case Element.Lightning:
                agent.areaMask = Bit(AREA_WALKABLE) | Bit(AREA_LIGHTNING) | Bit(AREA_FIRE) | Bit(AREA_COLD);
                break;
            case Element.Poison:
                agent.areaMask = Bit(AREA_WALKABLE) | Bit(AREA_POISON) | Bit(AREA_COLD) | Bit(AREA_LIGHTNING);
                break;
        }
    }

    private void ApplyAreaCosts()
    {
        SetCost(AREA_FIRE, 10f);
        SetCost(AREA_COLD, 10f);
        SetCost(AREA_LIGHTNING, 10f);
        SetCost(AREA_POISON, 10f);

        switch (element)
        {
            case Element.Common:
                break;
            case Element.Fire:
                SetCost(AREA_FIRE, 1f);
                SetCost(AREA_LIGHTNING, 5f);
                SetCost(AREA_POISON, 10f);
                break;
            case Element.Cold:
                SetCost(AREA_COLD, 1f);
                SetCost(AREA_POISON, 5f);
                SetCost(AREA_FIRE, 10f);
                break;
            case Element.Lightning:
                SetCost(AREA_LIGHTNING, 1f);
                SetCost(AREA_FIRE, 5f);
                SetCost(AREA_COLD, 10f);
                break;
            case Element.Poison:
                SetCost(AREA_POISON, 1f);
                SetCost(AREA_COLD, 5f);
                SetCost(AREA_LIGHTNING, 10f);
                break;
        }
    }

    private void SetCost(int area, float cost)
    {
        agent.SetAreaCost(area, cost);
    }
}
