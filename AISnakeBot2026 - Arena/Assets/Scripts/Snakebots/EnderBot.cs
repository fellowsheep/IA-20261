using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(menuName = "AIBehaviours/EnderBot")]
public class EnderBot : AIBehaviour
{
    public Color colorA = new Color(0f, 0.7f, 0.9f);        // Azul esverdeado
    public Color colorB = new Color(0.1f, 0.1f, 0.1f);    // Cinza escuro

    public float rotationSpeed = 5f;
    
    public float wanderInterval = 1.5f;
    public float wanderRadius = 6f;
    
    public float orbDetectionRadius = 12f;
    
    public float enemyDetectionRadius = 5f;
    public float fleeWeight = 1.8f;

    private GameLogic gameLogic;
    private enum BotState { Seeking, Wandering, Fleeing }
    private BotState currentState;
    private Vector3 desiredDirection;
    private float lastWanderTime;

    private int lastBodyCount = 0;

    public override void Init(GameObject own, SnakeMovement ownMove)
    {
        base.Init(own, ownMove);
        direction = Random.insideUnitCircle.normalized;
        desiredDirection = direction;
        currentState = BotState.Wandering;
        lastWanderTime = Time.time;

        gameLogic = Object.FindFirstObjectByType<GameLogic>();
    }

    public override void Execute()
    {
        if (gameLogic == null || ownerMovement == null) return;

        List<GameObject> activeOrbs = GetActiveOrbs();
        DecideState(activeOrbs);
        CalculateDesiredDirection(activeOrbs);
        ApplyMovement();
        
        if (ownerMovement.bodyParts.Count != lastBodyCount)
        {
            SetSnakeColor();
            lastBodyCount = ownerMovement.bodyParts.Count;
        }
    }

    private List<GameObject> GetActiveOrbs()
    {
        return gameLogic.orbPool.Where(o => o.activeInHierarchy).ToList();
    }

    private void DecideState(List<GameObject> activeOrbs)
    {
        bool enemyNear = IsSnakeNear();
        bool orbNear = activeOrbs.Any(orb =>
            Vector3.Distance(owner.transform.position, orb.transform.position) <= orbDetectionRadius);

        if (enemyNear)
            currentState = BotState.Fleeing;
        else if (orbNear && activeOrbs.Count > 0)
            currentState = BotState.Seeking;
        else
            currentState = BotState.Wandering;
    }

    private bool IsSnakeNear()
    {
        GameObject[] allBodies = GameObject.FindGameObjectsWithTag("Body");
        foreach (GameObject body in allBodies)
        {
            if (body.transform.parent == owner.transform.parent)
                continue;

            float dist = Vector3.Distance(owner.transform.position, body.transform.position);
            if (dist < enemyDetectionRadius)
                return true;
        }
        return false;
    }

    private Vector3 CalculateFleeDirection()
    {
        Vector3 flee = Vector3.zero;
        int count = 0;

        GameObject[] allBodies = GameObject.FindGameObjectsWithTag("Body");
        foreach (GameObject body in allBodies)
        {
            if (body.transform.parent == owner.transform.parent)
                continue;

            float dist = Vector3.Distance(owner.transform.position, body.transform.position);
            if (dist < enemyDetectionRadius)
            {
                Vector3 away = owner.transform.position - body.transform.position;
                away.Normalize();

                float weight = (enemyDetectionRadius - dist) / enemyDetectionRadius;
                flee += away * weight;
                count++;
            }
        }

        if (count > 0)
        {
            flee /= count;
            return flee.normalized;
        }
        return Vector3.zero;
    }

    private void CalculateDesiredDirection(List<GameObject> activeOrbs)
    {
        Vector3 baseDirection;
        if (currentState == BotState.Seeking)
        {
            Transform closest = activeOrbs.OrderBy(o => Vector3.Distance(owner.transform.position, o.transform.position)).FirstOrDefault()?.transform;
            baseDirection = closest ? (closest.position - owner.transform.position).normalized : CalculateWanderDirection();
        }
        else if (currentState == BotState.Fleeing)
        {
            baseDirection = CalculateFleeDirection();
            if (baseDirection == Vector3.zero) baseDirection = CalculateWanderDirection();
        }
        else 
        {
            baseDirection = CalculateWanderDirection();
        }

        Vector3 desired;
        if (currentState == BotState.Fleeing)
        {
            desired = baseDirection;
        }
        else
        {
            Vector3 fleeDir = CalculateFleeDirection();
            if (fleeDir != Vector3.zero)
            {
                desired = (baseDirection + fleeDir * fleeWeight).normalized;
            }
            else
            {
                desired = baseDirection;
            }
        }

        direction = Vector3.RotateTowards(direction, desired, rotationSpeed * Time.deltaTime, 0f);
        if (direction != Vector3.zero) direction.Normalize();
        else direction = Vector3.right;
    }

    private Vector3 CalculateWanderDirection()
    {
        if (Time.time - lastWanderTime >= wanderInterval)
        {
            Vector3 offset = Random.insideUnitCircle * wanderRadius;
            randomPoint = owner.transform.position + new Vector3(offset.x, offset.y, 0);
            lastWanderTime = Time.time;
        }
        return randomPoint != Vector3.zero ? (randomPoint - owner.transform.position).normalized : direction;
    }

    private void ApplyMovement()
    {
        float angle = Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
        Quaternion targetRotation = Quaternion.AngleAxis(-angle, Vector3.forward);
        owner.transform.rotation = Quaternion.Slerp(owner.transform.rotation, targetRotation, ownerMovement.speed * Time.deltaTime);

        Vector3 targetPos = owner.transform.position + direction * ownerMovement.speed * Time.deltaTime;
        owner.transform.position = Vector2.MoveTowards(owner.transform.position, targetPos, ownerMovement.speed * Time.deltaTime);
    }

    private void SetSnakeColor()
    {
        if (ownerMovement == null) return;

        SpriteRenderer headRenderer = owner.GetComponent<SpriteRenderer>();
        if (headRenderer != null)
            headRenderer.color = colorB;

        Transform eyes = owner.transform.Find("Eyes");
        if (eyes != null)
        {
            SpriteRenderer eyesRenderer = eyes.GetComponent<SpriteRenderer>();
            if (eyesRenderer != null) eyesRenderer.color = colorA;
        }

        int i = 0;

        foreach (Transform part in ownerMovement.bodyParts)
        {
            i++;
            SpriteRenderer partRenderer = part.GetComponent<SpriteRenderer>();
            if (partRenderer != null)
                if (i%2 == 0)
                {
                    partRenderer.color = colorB;
                }
                else
                {
                    partRenderer.color = colorA;
                }
        }
    }

}