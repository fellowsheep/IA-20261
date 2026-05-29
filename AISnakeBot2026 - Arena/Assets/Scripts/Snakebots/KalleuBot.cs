using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "AIBehaviours/KalleuBot")]
public class KalleuBot : AIBehaviour
{
    // --- Parâmetros ajustáveis no Inspector ---
    public float raioDeteccaoObstaculo = 3.0f;
    public float distanciaUsarDash     = 6.0f;
    public float anguloConeFrontal     = 50f;

    // =========================================================================
    // INIT — chamado uma vez ao iniciar
    // =========================================================================
    public override void Init(GameObject own, SnakeMovement ownMove)
    {
        base.Init(own, ownMove);
        // Inicia a corrotina de mudança de direção (herdada do framework)
        ownerMovement.StartCoroutine(UpdateDirEveryXSeconds(timeChangeDir));
    }

    // =========================================================================
    // EXECUTE — loop principal: Perceber → Decidir → Agir
    // =========================================================================
    public override void Execute()
    {
        // 1. PERCEBER
        GameObject comidaMaisProxima  = EncontrarComidaMaisProxima();
        bool       obstaculoAFrente   = VerificarObstaculoAFrente();

        // 2. DECIDIR
        if (obstaculoAFrente)
        {
            // Perigo! Gira para desviar
            direction = CalcularDirecaoDeDesvio();
        }
        else if (comidaMaisProxima != null)
        {
            // Caminho livre: aponta para a comida mais próxima
            direction = comidaMaisProxima.transform.position - owner.transform.position;
            direction.z = 0f;
        }
        // Se não há comida nem obstáculo, mantém a direção atual (direction já definida)

        // 3. AGIR — aplica o movimento usando o padrão do framework
        MoverEmDirecao();

        // Dash se a comida estiver muito perto e o caminho estiver livre
        if (comidaMaisProxima != null && !obstaculoAFrente)
        {
            float distancia = Vector2.Distance(owner.transform.position,
                                               comidaMaisProxima.transform.position);
            //ownerMovement.dash = distancia < distanciaUsarDash;
        }
        else
        {
            //ownerMovement.dash = false;
        }
    }

    // =========================================================================
    // PERCEBER — Encontra o orb mais próximo
    // =========================================================================
    private GameObject EncontrarComidaMaisProxima()
    {
        GameObject[] orbs         = GameObject.FindGameObjectsWithTag("Orb");
        GameObject   maisProxima  = null;
        float        menorDist    = float.MaxValue;

        foreach (GameObject orb in orbs)
        {
            if (orb == null) continue;
            float dist = Vector2.Distance(owner.transform.position, orb.transform.position);
            if (dist < menorDist)
            {
                menorDist    = dist;
                maisProxima  = orb;
            }
        }

        return maisProxima;
    }

    // =========================================================================
    // PERCEBER — Detecta obstáculos (corpos de cobra ou paredes) à frente
    // =========================================================================
    private bool VerificarObstaculoAFrente()
    {
        Collider2D[] proximos = Physics2D.OverlapCircleAll(
            owner.transform.position,
            raioDeteccaoObstaculo
        );

        foreach (Collider2D col in proximos)
        {
            if (col.gameObject == owner) continue;

            bool ehObstaculo = col.CompareTag("SnakeBody") || col.CompareTag("Wall");
            if (!ehObstaculo) continue;

            Vector2 direcaoParaCol = (col.transform.position - owner.transform.position).normalized;
            float angulo = Vector2.Angle(direction.normalized, direcaoParaCol);

            if (angulo < anguloConeFrontal)
                return true;
        }

        return false;
    }

    // =========================================================================
    // DECIDIR — Calcula direção de desvio (esquerda ou direita)
    // =========================================================================
    private Vector3 CalcularDirecaoDeDesvio()
    {
        // Tenta desviar 90 graus para cada lado e escolhe o mais livre
        Vector3 desvioEsq = Quaternion.Euler(0, 0,  90f) * direction;
        Vector3 desvioDir = Quaternion.Euler(0, 0, -90f) * direction;

        bool esquerdaLivre = !Physics2D.Raycast(owner.transform.position,
                                                 desvioEsq.normalized,
                                                 raioDeteccaoObstaculo);
        bool direitaLivre  = !Physics2D.Raycast(owner.transform.position,
                                                 desvioDir.normalized,
                                                 raioDeteccaoObstaculo);

        if (esquerdaLivre) return desvioEsq;
        if (direitaLivre)  return desvioDir;

        // Último recurso: inverter
        return -direction;
    }

    // =========================================================================
    // AGIR — Move a cobra na direção calculada (padrão do framework)
    // =========================================================================
    private void MoverEmDirecao()
    {
        direction.z = 0f;

        float angle = Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
        Quaternion rotation = Quaternion.AngleAxis(-angle, Vector3.forward);
        owner.transform.rotation = Quaternion.Slerp(
            owner.transform.rotation,
            rotation,
            ownerMovement.speed * Time.deltaTime
        );

        owner.transform.position = Vector2.MoveTowards(
            owner.transform.position,
            owner.transform.position + direction,
            ownerMovement.speed * Time.deltaTime
        );
    }

    // =========================================================================
    // Corrotina herdada do padrão Dummy/Playerbot — atualiza randomPoint
    // =========================================================================
    IEnumerator UpdateDirEveryXSeconds(float x)
    {
        yield return new WaitForSeconds(x);
        ownerMovement.StopCoroutine(UpdateDirEveryXSeconds(x));
        randomPoint = new Vector3(
            Random.Range(owner.transform.position.x - 10, owner.transform.position.x - 5),
            Random.Range(owner.transform.position.y - 10, owner.transform.position.y - 5),
            0
        );
        direction   = randomPoint - owner.transform.position;
        direction.z = 0.0f;
        ownerMovement.StartCoroutine(UpdateDirEveryXSeconds(x));
    }
}
