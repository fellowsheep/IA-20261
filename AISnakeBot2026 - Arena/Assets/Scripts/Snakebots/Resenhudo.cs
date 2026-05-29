using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "AIBehaviours/Resenhudo")]
public class Resenhudo : AIBehaviour
{
    private GameLogic logic;

    [Header("Pesos de decisão")]
    public float seekFoodWeight = 6.0f;    
    public float avoidEnemyWeight = 8.0f;  
    public float aggressionWeight = 9.0f;  
    public float dodgeWeight = 12.0f;      

    [Header("Sensores")]
    public float visionDistance = 7.0f;

    private Vector3 smoothedDirection;

    public override void Init(GameObject own, SnakeMovement ownMove)
    {
        base.Init(own, ownMove);
        logic = Object.FindFirstObjectByType<GameLogic>();
        smoothedDirection = own.transform.up; 
        
        
        if (ownerMovement != null) ownerMovement.speed = 3.5f;
    }

    public override void Execute()
    {
        if (logic == null || ownerMovement.isDead) return;

        ownerMovement.speed = 3.5f;

        Vector3 currentPos = owner.transform.position;
        Vector3 forward = owner.transform.up;

        
        GameObject targetOrb;
        Vector3 seekForce = SeekOrbs(currentPos, out targetOrb) * seekFoodWeight;
        
        Vector3 avoidEnemyForce;
        Vector3 aggressionForce;
        Vector3 dodgeForce;
        
        
        EvaluateEnemies(currentPos, forward, targetOrb, out avoidEnemyForce, out aggressionForce, out dodgeForce);

        avoidEnemyForce *= avoidEnemyWeight;
        aggressionForce *= aggressionWeight;
        dodgeForce *= dodgeWeight;

        
        Vector3 totalForce = seekForce + avoidEnemyForce + aggressionForce + dodgeForce;

        Vector3 targetDirection = forward;
        if (totalForce.sqrMagnitude > 0.01f)
        {
            targetDirection = totalForce.normalized;
        }

        
        smoothedDirection = Vector3.Lerp(smoothedDirection, targetDirection, Time.deltaTime * 6f).normalized;
        direction = smoothedDirection;

        
        MoveForward();
    }

    private Vector3 SeekOrbs(Vector3 currentPos, out GameObject bestOrb)
    {
        bestOrb = null;
        float minDistance = Mathf.Infinity;

        foreach (GameObject orb in logic.orbPool)
        {
            if (orb.activeInHierarchy)
            {
                float dist = Vector3.Distance(currentPos, orb.transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    bestOrb = orb;
                }
            }
        }

        if (bestOrb != null)
        {
            return (bestOrb.transform.position - currentPos).normalized;
        }
        return Vector3.zero;
    }

    private void EvaluateEnemies(Vector3 currentPos, Vector3 forward, GameObject targetOrb, 
                                 out Vector3 avoidance, out Vector3 aggression, out Vector3 dodge)
    {
        avoidance = Vector3.zero;
        aggression = Vector3.zero;
        dodge = Vector3.zero;

        
        float angle = 45f;
        Vector3 rightFeeler = Quaternion.Euler(0, 0, -angle) * forward;
        Vector3 leftFeeler = Quaternion.Euler(0, 0, angle) * forward;

        avoidance += CastAvoidanceSensor(currentPos, forward, visionDistance, 3.0f);
        avoidance += CastAvoidanceSensor(currentPos, rightFeeler, visionDistance * 0.7f, 1.5f);
        avoidance += CastAvoidanceSensor(currentPos, leftFeeler, visionDistance * 0.7f, 1.5f);

        
        foreach (GameObject snake in logic.snakes)
        {
            if (snake == null || !snake.activeInHierarchy) continue;
            SnakeMovement otherSnake = snake.GetComponentInChildren<SnakeMovement>();

            if (otherSnake == null || otherSnake == ownerMovement || otherSnake.isDead) continue;

            float distToHead = Vector3.Distance(currentPos, otherSnake.transform.position);

            
            if (distToHead < visionDistance * 1.5f)
            {
                Vector3 enemyForward = otherSnake.transform.up;
                Vector3 dirToEnemy = (otherSnake.transform.position - currentPos).normalized;
                
                
                float dotForward = Vector3.Dot(forward, enemyForward); 
                float dotFacingUs = Vector3.Dot(enemyForward, -dirToEnemy); 

                
                if (dotForward < -0.2f && dotFacingUs > 0.5f && distToHead < visionDistance)
                {
                    
                    Vector3 rightVector = new Vector3(forward.y, -forward.x, 0);
                    float sideDot = Vector3.Dot(dirToEnemy, rightVector);
                    
                    
                    if (sideDot > 0) dodge -= rightVector;
                    else dodge += rightVector;
                    
                    
                    dodge += (currentPos - otherSnake.transform.position).normalized * 2.0f;
                }
                
                
                else if (targetOrb != null)
                {
                    float enemyDistToOrb = Vector3.Distance(otherSnake.transform.position, targetOrb.transform.position);
                    float myDistToOrb = Vector3.Distance(currentPos, targetOrb.transform.position);

                    
                    if (enemyDistToOrb < 5.0f && myDistToOrb < 7.0f)
                    {
                        
                        if (ownerMovement.bodyParts.Count >= otherSnake.bodyParts.Count || dotForward > 0.3f)
                        {
                            
                            Vector3 interceptPoint = otherSnake.transform.position + (enemyForward * 3.5f);
                            aggression += (interceptPoint - currentPos).normalized;
                        }
                        else
                        {
                            
                            avoidance += (currentPos - targetOrb.transform.position).normalized;
                        }
                    }
                }
            }
        }

        if (avoidance != Vector3.zero) avoidance = avoidance.normalized;
        if (aggression != Vector3.zero) aggression = aggression.normalized;
        if (dodge != Vector3.zero) dodge = dodge.normalized;
    }

    private Vector3 CastAvoidanceSensor(Vector3 origin, Vector3 dir, float dist, float weightMult)
    {
        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, dir, dist);
        foreach (var hit in hits)
        {
            if (hit.collider != null && hit.collider.transform.parent != owner.transform.parent)
            {
                if (hit.collider.CompareTag("Body") || hit.collider.CompareTag("Player"))
                {
                    Vector3 hitNormal = hit.normal != Vector2.zero ? (Vector3)hit.normal : -dir;
                    return hitNormal * weightMult * (1.0f / Mathf.Max(hit.distance, 0.5f));
                }
            }
        }
        return Vector3.zero;
    }

    private void MoveForward()
    {
        float angle = Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
        Quaternion targetRotation = Quaternion.AngleAxis(-angle, Vector3.forward);

        
        float turnSpeed = 5.0f;

        owner.transform.rotation = Quaternion.Slerp(owner.transform.rotation, targetRotation, ownerMovement.speed * Time.deltaTime * turnSpeed);
        owner.transform.position += owner.transform.up * ownerMovement.speed * Time.deltaTime;
    }
}