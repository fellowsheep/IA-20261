using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "AIBehaviours/Meibot")]
public class Meibot : AIBehaviour {

    //steering params
    public float avoidRadius = 3f;
    public float avoidWeight = 2f;
    public float seekWeight  = 1f;

    //custom body colors
    public Color[] bodyColors = new Color[] {
        new Color(0xFF / 255f, 0x72 / 255f, 0xAD / 255f),
        new Color(0xFF / 255f, 0x96 / 255f, 0xBE / 255f),
        new Color(0xCC / 255f, 0x5B / 255f, 0x8C / 255f)
    };

    //playername
    public string playerName = "meibot";

    //init
    public override void Init(GameObject own, SnakeMovement ownMove) {
        base.Init(own, ownMove);
    }

    //tick
    public override void Execute() {
        //funnies
        applyBodyColors();
        updateNickname();

        //simple seek and avoid steering AI
        findSteeringDirection();

        //apply the movement
        move();
    }

    private void applyBodyColors() {
        //no colors
        if (bodyColors == null || bodyColors.Length == 0)
            return;

        int c = 0;

        //head
        SpriteRenderer headRenderer = owner.GetComponent<SpriteRenderer>();
        if (headRenderer != null)
            headRenderer.color = bodyColors[c];

        //body
        for (int i = 0; i < ownerMovement.bodyParts.Count; i++) {
            Transform bodyPart = ownerMovement.bodyParts[i];
            if (bodyPart == null)
            continue;

            SpriteRenderer renderer = bodyPart.GetComponent<SpriteRenderer>();
            if (renderer != null) {
                c = (c + 1) % bodyColors.Length;
                renderer.color = bodyColors[c];
            }
        }
    }

    private void updateNickname() {
        //no name
        if (playerName == null || playerName.Trim() == "")
            return;

        NameBanner nameBanner = owner.GetComponentInChildren<NameBanner>();
        if (nameBanner != null && nameBanner.snakeNameText != null) {
            //text
            nameBanner.snakeNameText.text = playerName;

            //color
            if (playerName.ToLower() == "meibot") {
                float hue = (Time.time * 0.3f) % 1f;
                nameBanner.snakeNameText.color = Color.HSVToRGB(hue, 0.5f, 1f);
            }
        }
    }

    private void findSteeringDirection() {
        //seek direction towards the nearest orb
        Vector3 seek = computeSeekDirection();

        //avoidance direction from nearby snakes
        Vector3 avoid = computeAvoidanceDirection();

        //combine steering with their weights
        Vector3 targetDir = seek * seekWeight + avoid * avoidWeight;

        //determine the final target direction
        //with a fallback to the previous direction
        if (targetDir.sqrMagnitude > Mathf.Epsilon)
            targetDir = targetDir.normalized;
        else
            targetDir = direction;

        //apply the new direction
        //ignore z-axis for 2D movement
        direction = targetDir;
        direction.z = 0f;
    }

    private void move() {
        //rotate smoothly towards the direction
        if (direction != Vector3.zero) {
            float angle = Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
            Quaternion rotation = Quaternion.AngleAxis(-angle, Vector3.forward);
            owner.transform.rotation = Quaternion.Slerp(owner.transform.rotation, rotation, ownerMovement.speed * Time.deltaTime);
        }

        //move towards the direction
        owner.transform.position = owner.transform.position + direction * ownerMovement.speed * Time.deltaTime;
    }

    private Vector3 computeSeekDirection() {
        //get all orbs in the scene
        GameObject[] orbs = GameObject.FindGameObjectsWithTag("Orb");
        Vector3 currentPos = owner.transform.position;

        //no orbs found
        if (orbs == null || orbs.Length == 0)
            return Vector3.zero;

        float bestDistSqr = float.MaxValue;
        Vector3 bestPos = currentPos;

        //find the closest orb
        foreach (GameObject orb in orbs) {
            if (orb == null)
                continue;

            //compare the distance
            Vector3 orbPos = orb.transform.position;
            float distSqr = (orbPos - currentPos).sqrMagnitude;
            if (distSqr < bestDistSqr) {
                bestDistSqr = distSqr;
                bestPos = orbPos;
            }
        }

        //seek direction towards orb
        Vector3 seek = bestPos - currentPos;
        return seek.sqrMagnitude > Mathf.Epsilon ? seek.normalized : Vector3.zero;
    }

    private Vector3 computeAvoidanceDirection() {
        Vector3 pos = owner.transform.position;
        Vector3 repulsion = Vector3.zero;

        //avoid snake heads
        GameObject[] snakes = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject snake in snakes) {
            //skip self
            if (snake == null || snake == owner)
                continue;

            //find distance
            Vector3 otherPos = snake.transform.position;
            Vector3 toOther = otherPos - pos;
            float dist = toOther.magnitude;

            //already colliding or invalid distance
            if (dist <= 0f)
                continue;

            //in range to avoid
            if (dist < avoidRadius) {
                //linear repulsion strength when closer
                float strength = (avoidRadius - dist) / avoidRadius;
                repulsion += -toOther.normalized * strength;
            }
        }

        //avoid body parts
        GameObject[] bodies = GameObject.FindGameObjectsWithTag("Body");
        foreach (GameObject body in bodies) {
            //skip self
            if (body == null || body.transform.parent == owner.transform.parent)
                continue;

            //find distance
            Vector3 bodyPos = body.transform.position;
            Vector3 toBody = bodyPos - pos;
            float dist = toBody.magnitude;

            //already colliding or invalid distance
            if (dist <= 0f)
                continue;

            //in range to avoid
            if (dist < avoidRadius) {
                //linear repulsion strength when closer
                float strength = (avoidRadius - dist) / avoidRadius;
                repulsion += -toBody.normalized * strength;
            }
        }

        //no repulsion needed
        if (repulsion.sqrMagnitude < Mathf.Epsilon)
            return Vector3.zero;

        //return the combined avoidance direction
        return repulsion.normalized;
    }
}
