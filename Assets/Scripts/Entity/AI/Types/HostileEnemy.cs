using UnityEngine;

[RequireComponent(typeof(Fighter))]
public class HostileEnemy : AI
{
    [SerializeField] private Fighter fighter;
    [SerializeField] private bool isFighting;
    private SpriteRenderer spriteRenderer;

    private void OnValidate()
    {
        fighter = GetComponent<Fighter>();
        AStar = GetComponent<AStar>();
    }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public override void RunAI()
    {
        if (!fighter.Target)
        {
            fighter.Target = GameManager.instance.Actors[0];
        }
        else if (fighter.Target && !fighter.Target.IsAlive)
        {
            fighter.Target = null;
        }

        if (fighter.Target)
        {
            Vector3Int targetPosition = MapManager.instance.FloorMap.WorldToCell(fighter.Target.transform.position);
            if (isFighting || GetComponent<Actor>().FieldOfView.Contains(targetPosition))
            {
                if (!isFighting)
                {
                    isFighting = true;
                }

                float targetDistance = Vector3.Distance(transform.position, fighter.Target.transform.position);

                if (targetDistance <= 1.5f)
                {
                    Action.MeleeAction(GetComponent<Actor>(), fighter.Target);
                    FlipSprite(fighter.Target.transform.position);
                    return;
                }
                else //If not in range, move towards target
                {
                    MoveAlongPath(targetPosition);
                    FlipSprite(fighter.Target.transform.position);
                    return;
                }
            }
        }

        Action.WaitAction();
    }

    private void FlipSprite(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - transform.position;
        if (direction.x < 0)
        {
            spriteRenderer.flipX = true; // Flip sprite horizontally if moving left
        }
        else if (direction.x > 0)
        {
            spriteRenderer.flipX = false; // Do not flip sprite if moving right
        }
    }

    public override AIState SaveState() => new AIState(
        type: "HostileEnemy"
    );
}
