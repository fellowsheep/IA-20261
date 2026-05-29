using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "SnakeAI/JaquetBot")]
public class BotIA : AIBehaviour
{
    private SnakeMovement snake;

    public override void Init(GameObject own, SnakeMovement ownMove)
{
    base.Init(own, ownMove);
}

    public override void Execute()
    {
        if (snake == null) return;

        GameObject[] foods = GameObject.FindGameObjectsWithTag("Food");

        if (foods.Length == 0) return;

        GameObject closest = foods[0];
        float minDist = Vector2.Distance(snake.transform.position, closest.transform.position);

        foreach (var f in foods)
        {
            float d = Vector2.Distance(snake.transform.position, f.transform.position);
            if (d < minDist)
            {
                minDist = d;
                closest = f;
            }
        }

        Vector2 dir = (closest.transform.position - snake.transform.position).normalized;

        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
            dir = new Vector2(Mathf.Sign(dir.x), 0);
        else
            dir = new Vector2(0, Mathf.Sign(dir.y));

        direction.x = dir.x;
        direction.z = dir.y;

        //snake.SetDirection(dir);
    }
}