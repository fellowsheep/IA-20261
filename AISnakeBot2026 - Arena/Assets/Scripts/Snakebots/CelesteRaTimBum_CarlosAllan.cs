using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "AIBehaviours/CelesteRaTimBum")]
public class CelesteRaTimBum : AIBehaviour
{
    enum State { Wander, SeekOrb, Flee, Encircle }

    State _state = State.Wander;

    public float detectionRadius = 6f;
    public float fleeRadius = 3f;
    public float orbRadius = 5f;
    public float circleSpeed = 90f;

    Vector3 _circleCenter;
    float _circleRadius;
    float _currentAngle;
    float _accumulatedAngle;
    bool _targetLeftCircle;
    GameObject _encircleTarget;

    public override void Init(GameObject own, SnakeMovement ownMove)
    {
        base.Init(own, ownMove);
        ownerMovement.StartCoroutine(WanderRoutine());
    }

    public override void Execute()
    {
        if (ownerMovement.isDead) return;

        if (CheckFlee()) return;
        if (_state == State.Encircle) { DoEncircle(); return; }
        if (CheckEncircle()) return;
        if (CheckOrb()) return;
        DoWander();
    }

    bool CheckFlee()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(owner.transform.position, fleeRadius);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Body") && hit.transform.parent != null && hit.transform.parent.name != owner.transform.parent.name)
            {
                _state = State.Flee;
                Vector3 away = owner.transform.position - hit.transform.position;
                away.z = 0f;
                RotateToward(away.normalized);
                MoveForward();
                return true;
            }
        }

        if (_state == State.Flee) _state = State.Wander;
        return false;
    }

    bool CheckEncircle()
    {
        int mySize = ownerMovement.bodyParts.Count;
        Collider2D[] hits = Physics2D.OverlapCircleAll(owner.transform.position, detectionRadius);
        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Head")) continue;
            if (hit.gameObject == owner) continue;

            SnakeMovement other = hit.GetComponentInParent<SnakeMovement>();
            if (other == null) continue;
            if (other.isDead) continue;
            if (other.bodyParts.Count >= mySize) continue;

            _encircleTarget = hit.gameObject;
            _circleCenter = hit.transform.position;
            _circleRadius = Mathf.Max(2.5f, other.bodyParts.Count * 0.3f + 2f);
            _currentAngle = Mathf.Atan2(
                owner.transform.position.y - _circleCenter.y,
                owner.transform.position.x - _circleCenter.x
            ) * Mathf.Rad2Deg;
            _accumulatedAngle = 0f;
            _targetLeftCircle = false;
            _state = State.Encircle;
            return true;
        }
        return false;
    }

    void DoEncircle()
    {
        if (_encircleTarget == null || (_encircleTarget.GetComponentInParent<SnakeMovement>()?.isDead ?? true))
        {
            _state = State.Wander;
            return;
        }

        float step = circleSpeed * Time.deltaTime;
        _currentAngle += step;
        _accumulatedAngle += step;

        float rad = _currentAngle * Mathf.Deg2Rad;
        Vector3 targetPoint = _circleCenter + new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * _circleRadius;

        Vector3 dir = targetPoint - owner.transform.position;
        dir.z = 0f;
        RotateToward(dir.normalized);
        MoveForward();

        if (_accumulatedAngle < 360f) return;

        float distToCenter = Vector3.Distance(_encircleTarget.transform.position, _circleCenter);
        if (distToCenter > _circleRadius)
            _state = State.Wander;
        else
        {
            _circleRadius = Mathf.Max(1.2f, _circleRadius - 0.4f);
            _accumulatedAngle = 0f;
        }
    }

    bool CheckOrb()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(owner.transform.position, orbRadius);
        GameObject closest = null;
        float bestDist = float.MaxValue;

        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Orb")) continue;
            float d = Vector3.Distance(owner.transform.position, hit.transform.position);
            if (d < bestDist) { bestDist = d; closest = hit.gameObject; }
        }

        if (closest != null)
        {
            _state = State.SeekOrb;
            Vector3 dir = closest.transform.position - owner.transform.position;
            dir.z = 0f;
            RotateToward(dir.normalized);
            MoveForward();
            return true;
        }

        if (_state == State.SeekOrb) _state = State.Wander;
        return false;
    }

    void DoWander()
    {
        _state = State.Wander;
        RotateToward(direction.normalized);
        MoveForward();
    }

    void RotateToward(Vector3 dir)
    {
        if (dir == Vector3.zero) return;
        float angle = Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg;
        Quaternion rotation = Quaternion.AngleAxis(-angle, Vector3.forward);
        owner.transform.rotation = Quaternion.Slerp(owner.transform.rotation, rotation, ownerMovement.speed * Time.deltaTime);
    }

    void MoveForward()
    {
        owner.transform.position = Vector2.MoveTowards(
            owner.transform.position,
            owner.transform.position + owner.transform.up,
            ownerMovement.speed * Time.deltaTime
        );
    }

    IEnumerator WanderRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(timeChangeDir > 0 ? timeChangeDir : 2f);
            if (_state == State.Wander)
            {
                randomPoint = owner.transform.position + new Vector3(
                    Random.Range(-8f, 8f),
                    Random.Range(-8f, 8f),
                    0f
                );
                direction = randomPoint - owner.transform.position;
                direction.z = 0f;
            }
        }
    }
}