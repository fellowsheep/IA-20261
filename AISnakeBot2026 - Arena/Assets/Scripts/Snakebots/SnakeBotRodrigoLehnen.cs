using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "AIBehaviours/Rodribot")]
public class SnakeBotRodrigoLehnen : AIBehaviour
{
    public float dangerDistance = 3.0f;
    public float foodDetectionRadius = 10.0f;

    private enum State
    {
        SEEK_FOOD,
        AVOID,
        WANDER
    }

    private State currentState;

    private Transform closestFood;
    private Transform closestEnemy;

    private Vector3 wanderDirection;
    private float changeDirTimer = 0f;

    // 1. Trocamos o Start() pelo Init() sobrescrito da classe base
    public override void Init(GameObject own, SnakeMovement ownMove)
    {
        base.Init(own, ownMove); // Importante para gravar o owner e ownerMovement
        
        wanderDirection = Random.insideUnitSphere;
        wanderDirection.y = 0;
    }

    // 2. Trocamos o Update() pelo Execute()
    public override void Execute()
    {
        if (owner == null) return; // Proteção extra

        Perceive();
        Decide();
        Act();
    }

    void Perceive()
    {
        closestFood = GetClosestFood();
        closestEnemy = GetClosestEnemy();
    }

    Transform GetClosestFood()
    {
        GameObject[] foods = GameObject.FindGameObjectsWithTag("Food");

        float minDist = Mathf.Infinity;
        Transform closest = null;

        foreach (GameObject food in foods)
        {
            // 3. Trocamos transform.position por owner.transform.position
            float dist = Vector3.Distance(owner.transform.position, food.transform.position);

            if (dist < minDist && dist <= foodDetectionRadius)
            {
                minDist = dist;
                closest = food.transform;
            }
        }

        return closest;
    }

    Transform GetClosestEnemy()
    {
        GameObject[] snakes = GameObject.FindGameObjectsWithTag("Snake");

        float minDist = Mathf.Infinity;
        Transform closest = null;

        foreach (GameObject snake in snakes)
        {
            // Evita que a cobra calcule a distância para si mesma
            if (snake.transform == owner.transform)
                continue;

            float dist = Vector3.Distance(owner.transform.position, snake.transform.position);

            if (dist < minDist)
            {
                minDist = dist;
                closest = snake.transform;
            }
        }

        return closest;
    }

    void Decide()
    {
        if (closestEnemy != null)
        {
            float distEnemy = Vector3.Distance(owner.transform.position, closestEnemy.position);

            if (distEnemy < dangerDistance)
            {
                currentState = State.AVOID;
                return;
            }
        }

        if (closestFood != null)
        {
            currentState = State.SEEK_FOOD;
        }
        else
        {
            currentState = State.WANDER;
        }
    }

    void Act()
    {
        switch (currentState)
        {
            case State.SEEK_FOOD:
                SeekFood();
                break;

            case State.AVOID:
                AvoidEnemy();
                break;

            case State.WANDER:
                Wander();
                break;
        }
    }

    void SeekFood()
    {
        if (closestFood == null) return;

        // Calcula a direção em relação à posição do owner
        Vector3 dir = (closestFood.position - owner.transform.position).normalized;

        // 4. Salva na variável 'direction' da classe base AIBehaviour
        direction = dir; 

        // float dist = Vector3.Distance(owner.transform.position, closestFood.position);
        // Se você tiver um método de Boost no SnakeMovement, você chama assim:
        // ownerMovement.SetBoost(dist < 2.0f);
    }

    void AvoidEnemy()
    {
        if (closestEnemy == null) return;

        Vector3 dir = (owner.transform.position - closestEnemy.position).normalized;
        direction = dir;
        
        // ownerMovement.SetBoost(true);
    }

    void Wander()
    {
        // Time.deltaTime funciona perfeitamente em ScriptableObjects
        changeDirTimer -= Time.deltaTime;

        if (changeDirTimer <= 0f)
        {
            wanderDirection = new Vector3(
                Random.Range(-1f, 1f),
                0,
                Random.Range(-1f, 1f)
            ).normalized;

            changeDirTimer = Random.Range(1f, 3f);
        }

        direction = wanderDirection;
        // ownerMovement.SetBoost(false);
    }
}