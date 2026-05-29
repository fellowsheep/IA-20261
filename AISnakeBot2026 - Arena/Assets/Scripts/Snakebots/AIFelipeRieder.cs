using System.Collections;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
using UnityEditor;
using UnityEngine;
using static UnityEngine.UI.Image;


[CreateAssetMenu(menuName = "AIBehaviours/AIFelipeRieder")]
public class AIFelipeRieder : AIBehaviour
{
    private float fleeTime = 0.0f;
    private const float fleeDuration = 2.0f;
    private GameObject currentThreat;

    //variaveis do detector de orbes
    private float detectionRange = 15.0f;

    public override void Init(GameObject own, SnakeMovement ownMove)
    {
        base.Init(own, ownMove);
        ownerMovement.StartCoroutine(UpdateDirEveryXSeconds(timeChangeDir));

    }

    //seria interessante ter um controlador com o colisor que define o mundo pra poder gerar pontos dentro desse colisor

    public override void Execute()
    {
        DecideAction();
    }


    IEnumerator UpdateDirEveryXSeconds(float x)
    {
        yield return new WaitForSeconds(x);
        ownerMovement.StopCoroutine(UpdateDirEveryXSeconds(x));
        randomPoint = new Vector3(
                Random.Range(
                    Random.Range(owner.transform.position.x - 10, owner.transform.position.x - 5),
                    Random.Range(owner.transform.position.x + 5, owner.transform.position.x + 10)
                ),
                Random.Range(
                    Random.Range(owner.transform.position.y - 10, owner.transform.position.y - 5),
                    Random.Range(owner.transform.position.y + 5, owner.transform.position.y + 10)
                ),
                0
            );
        direction = randomPoint - owner.transform.position;
        direction.z = 0.0f;

        ownerMovement.StartCoroutine(UpdateDirEveryXSeconds(x));
    }

    void DecideAction()
    {
        //"Scanner" de detec��o
        Collider2D[] hits = Physics2D.OverlapCircleAll(owner.transform.position, detectionRange);
        if ( DetectDanger()){
            return;
        }

        if (fleeTime >0f)
        {
            fleeTime -= Time.deltaTime;
            if (currentThreat != null)
                Flee(currentThreat);
            return;
        }

        if (FindNearestOrbInRange(hits))
        {
            return;
        }
        Wander();
    }

    bool FindNearestOrbInRange(Collider2D[] hits)
    {
        float nearestOrbDistance = Mathf.Infinity;
        GameObject nearestOrb = null;

        foreach (Collider2D hit in hits)
        {
            if (hit.gameObject.CompareTag("Orb"))
            {
                float distanceToOrb = Vector3.Distance(owner.transform.position, hit.gameObject.transform.position);
                if (distanceToOrb < nearestOrbDistance)
                {
                    nearestOrbDistance = distanceToOrb;
                    nearestOrb = hit.gameObject;
                }
            }
        }
        if (nearestOrb != null)
        {
            ChaseOrb(nearestOrb);
            return true;
        }
        else
        {
            return false;
        }
    }

    bool DetectDanger()
    {
        //Criar um raycast pra detectar se tem alguma cobra inimiga muito pr�xima � frente, se tiver, fugir na dire��o oposta
        Vector2 snakeEye = owner.transform.position;
        Vector2 snakeForward = owner.transform.up;

        float rayLength = 5.0f;

        RaycastHit2D[] dangerSense = Physics2D.CircleCastAll(snakeEye,0.5f, snakeForward, rayLength);
        foreach (RaycastHit2D hit in dangerSense)
        { 
            GameObject hitObject = hit.collider.gameObject;
            if (hit.collider.transform.root == owner.transform.root) continue; // Ignora a pr�pria cobra

            bool isEnemySnake = hitObject.TryGetComponent<SnakeMovement>(out _) || hitObject.TryGetComponent<SnakeBody>(out _);
            if (isEnemySnake)
            {
                currentThreat = hitObject;
                fleeTime = fleeDuration;
                Flee(hitObject);
                Debug.DrawRay(snakeEye + Vector2.Perpendicular(snakeForward) * 0.5f, snakeForward * rayLength, Color.red);
                Debug.DrawRay(snakeEye - Vector2.Perpendicular(snakeForward) * 0.5f, snakeForward * rayLength, Color.red);
                return true; // Foge do primeiro inimigo detectado
            }
        }
        // Debug visual 
        Debug.DrawRay(snakeEye + Vector2.Perpendicular(snakeForward) * 0.5f, snakeForward * rayLength, Color.green);
        Debug.DrawRay(snakeEye - Vector2.Perpendicular(snakeForward) * 0.5f, snakeForward * rayLength, Color.green);
        return false;
    }



    void Wander()
    {
        float angle = Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
        Quaternion rotation = Quaternion.AngleAxis(-angle, Vector3.forward);
        owner.transform.rotation = Quaternion.Slerp(owner.transform.rotation, rotation, ownerMovement.speed * Time.deltaTime);

        owner.transform.position = Vector2.MoveTowards(owner.transform.position, randomPoint, ownerMovement.speed * Time.deltaTime);
    }

    void ChaseOrb(GameObject nearestOrb)
    {
        Vector2 dir = (nearestOrb.transform.position - owner.transform.position).normalized;

        float angle = Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg;
        Quaternion rotation = Quaternion.AngleAxis(-angle, Vector3.forward);
        owner.transform.rotation = Quaternion.Slerp(owner.transform.rotation, rotation, ownerMovement.speed * Time.deltaTime);

        owner.transform.position = Vector2.MoveTowards(owner.transform.position, nearestOrb.transform.position, ownerMovement.speed * Time.deltaTime);
    }
     void Flee(GameObject enemySnake)
    {
        Vector2 dir = (owner.transform.position - enemySnake.transform.position).normalized;
        float angle = Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg;
        Quaternion rotation = Quaternion.AngleAxis(-angle, Vector3.forward);

        owner.transform.rotation = Quaternion.Slerp(owner.transform.rotation, rotation, ownerMovement.speed * Time.deltaTime);
        owner.transform.position += (Vector3)(dir * ownerMovement.speed * Time.deltaTime);
    }

}
