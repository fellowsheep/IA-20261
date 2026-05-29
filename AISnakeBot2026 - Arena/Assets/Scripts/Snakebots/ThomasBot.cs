using UnityEngine;

[CreateAssetMenu(menuName = "AIBehaviours/Thomasbot")]
public class SmartBot : AIBehaviour
{
    private SnakeMovement snake;

    // Parâmetros ajustáveis no Inspector
    public float dangerDistance = 3f;
    public float wanderRadius = 10f;

      public override void Init(GameObject owner, SnakeMovement ownMove)
    {
        snake = owner.GetComponent<SnakeMovement>();
    }

    public override void Execute()
    {
        if (snake == null) return;

        // PERCEPÇÃO
        GameObject food = FindClosest("Food");
        GameObject enemy = FindClosest("Snake");

        Vector3 target;

        // DECISÃO (arquitetura reativa)
        if (enemy != null && IsDanger(enemy))
        {
            target = Flee(enemy);
        }
        else if (food != null)
        {
            target = Seek(food);
        }
        else
        {
            target = Wander();
        }

        // AÇÃO
        MoveTo(target);
    }

    // =========================
    // COMPORTAMENTOS
    // =========================

    Vector3 Seek(GameObject targetObj)
    {
        return targetObj.transform.position;
    }

    Vector3 Flee(GameObject enemy)
    {
        Vector3 direction = (snake.transform.position - enemy.transform.position).normalized;
        return snake.transform.position + direction * 5f;
    }

    Vector3 Wander()
    {
        Vector2 random = Random.insideUnitCircle * wanderRadius;
        return new Vector3(random.x, random.y, 0);
    }

    bool IsDanger(GameObject enemy)
    {
        float dist = Vector3.Distance(snake.transform.position, enemy.transform.position);
        return dist < dangerDistance;
    }

    GameObject FindClosest(string tag)
    {
        GameObject[] objs = GameObject.FindGameObjectsWithTag(tag);

        GameObject closest = null;
        float minDist = Mathf.Infinity;

        foreach (GameObject obj in objs)
        {
            if (obj == snake.gameObject) continue;

            float dist = Vector3.Distance(snake.transform.position, obj.transform.position);

            if (dist < minDist)
            {
                minDist = dist;
                closest = obj;
            }
        }

        return closest;
    }

    void MoveTo(Vector3 target)
    {
        Vector3 dir = (target - snake.transform.position).normalized;
        direction.x = dir.x;
        direction.z = dir.z;
        //snake.SetDirection(direction);
    }
}