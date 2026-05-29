using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class GameLogic : MonoBehaviour
{
    [Header("Settings")]
    public int poolSize = 500;
    public float orbLifetime = 30f;
    public float orbSpawnInterval = 2f;
    public int orbsPerSpawn = 5;
    public int nSnakes = 50;

    [Header("Camera Zoom Settings")]
    public float zoomSpeed = 5f;
    public float minZoom = 2f;
    public float maxZoom = 20f;

    [Header("Prefabs & References")]
    public GameObject orbPreFab;
    public GameObject snakePrefab;
    public List<AIBehaviour> behaviors = new List<AIBehaviour>();
    public List<string> snakeNames = new List<string>();

    [Header("Runtime Data")]
    public List<GameObject> snakes = new List<GameObject>();
    // Esta é a lista que os alunos usarão para a Tomada de Decisão
    public List<GameObject> orbPool = new List<GameObject>();

    [Header("Match Settings")]
    public float matchDuration = 120f; // 2 minutos
    private float timer;
    private bool matchEnded = false;

    // NOVO: Dicionário para rastrear e cancelar os temporizadores antigos
    //private Dictionary<GameObject, Coroutine> orbTimers = new Dictionary<GameObject, Coroutine>();

    private float minX, minY, maxX, maxY;
    private int selectedId;

    void Start()
{
    Time.timeScale = 1f; // Força o tempo a correr
    timer = matchDuration; 
    SetupBounds();
    InitializeOrbPool();
    SpawnInitialSnakes();

    Debug.Log("Tentando ativar os primeiros 10 orbes na marra...");
    for(int i = 0; i < 10; i++) 
    {
        ActivateOrbFromPool(); // Chama direto, sem esperar a coroutine
    }

    StartCoroutine(PoolSpawnRoutine()); // Tenta iniciar a rotina normal
}

    void SetupBounds()
    {
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        minX = col.bounds.min.x;
        minY = col.bounds.min.y;
        maxX = col.bounds.max.x;
        maxY = col.bounds.max.y;
    }

    // Criamos todos os orbes no início, desativados
    void InitializeOrbPool()
    {
         Debug.Log("<color=magenta>ORBS INICIANDO!</color>");
        GameObject orbParent = new GameObject("OrbPool");
        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj = Instantiate(orbPreFab);
            obj.transform.parent = orbParent.transform;
            obj.SetActive(false);
            orbPool.Add(obj);
        }
    }

    IEnumerator PoolSpawnRoutine()
    {
        Debug.Log("<color=magenta>A COROUTINE DOS ORBES COMEÇOU!</color>");
        while (true)
        {
            for (int i = 0; i < orbsPerSpawn; i++)
            {
                ActivateOrbFromPool();
            }
            yield return new WaitForSeconds(orbSpawnInterval);
        }
    }

    void ActivateOrbFromPool()
{
    // 1. Pega o primeiro orbe inativo da lista
    GameObject orb = orbPool.FirstOrDefault(o => o != null && !o.activeInHierarchy);

    if (orb != null)
    {
        // 2. Define a posição antes de ativar
        orb.transform.position = new Vector3(Random.Range(minX, maxX), Random.Range(minY, maxY), 0);
        
        // 3. ATIVAÇÃO (Se chegar aqui, ele TEM que aparecer)
        orb.SetActive(true);

        // 4. Em vez de dicionário, paramos todas as coroutines deste script 
        // que possam estar afetando este orbe específico e iniciamos uma nova.
        StartCoroutine(DeactivateOrbAfterTime(orb, orbLifetime));
    }
}   

    IEnumerator DeactivateOrbAfterTime(GameObject orb, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (orb != null && orb.activeInHierarchy)
        {
            orb.SetActive(false);
        }
    }

    // Método para ser chamado pelo SnakeMovement.cs em vez de Destroy()
    public void CollectOrb(GameObject orb)
    {
        orb.SetActive(false);
    }

    void SpawnInitialSnakes()
    {
        // Lógica original de spawn de cobras mantida e adaptada
        for (int i = 0; i < nSnakes; i++)
        {
            Vector3 pos = new Vector3(Random.Range(minX, maxX), Random.Range(minY, maxY), 0);
            GameObject newSnake = Instantiate(snakePrefab, pos, Quaternion.identity);
            newSnake.name = snakeNames[i];

            newSnake.GetComponentInChildren<SnakeMovement>().SetBehaviour(behaviors[i]);
            snakes.Add(newSnake);
        }

        if (snakes.Count > 0)
        {
            snakes[0].GetComponentInChildren<SnakeMovement>().selected = true;
        }
    }

    void Update()
    {
        if (matchEnded) return; // Para tudo se a partida acabou
        timer -= Time.deltaTime;
        if (timer <= 0)
        {
            EndMatch();
        }

        //for (int i = 0; i < snakes.Count; i++)
        //{
        //if (snakes[i] != null && snakes[i].GetComponentInChildren<SnakeMovement>().isDead)
        //{
                //Destroy(snakes[i]);
                //snakes.RemoveAt(i);
        //        Debug.Log("Morreu a Snake " + snakeNames[i]);
        //}
        //}
        
        CheckInput();
        MonitorSelectedSnake();
    }

    

    void CheckInput()
    {
    if (Input.GetKeyDown(KeyCode.E)) SelectSnake(1);
    if (Input.GetKeyDown(KeyCode.Q)) SelectSnake(-1);

    // Controle de Zoom Contínuo
    if (Input.GetKey(KeyCode.Equals) || Input.GetKey(KeyCode.KeypadPlus)) // KeyCode.Equals é o '+' no teclado padrão
    {
        Camera.main.orthographicSize = Mathf.Max(Camera.main.orthographicSize - zoomSpeed * Time.deltaTime, minZoom);
    }
    
    if (Input.GetKey(KeyCode.Minus) || Input.GetKey(KeyCode.KeypadMinus))
    {
        Camera.main.orthographicSize = Mathf.Min(Camera.main.orthographicSize + zoomSpeed * Time.deltaTime, maxZoom);
    }
    }

    void MonitorSelectedSnake()
    {
        if (snakes.Count == 0) return;

        GameObject current = snakes[selectedId];
        
        // Se a cobra atual for destruída (null) ou desativada da arena...
        if (current == null || !current.activeInHierarchy)
        {
            // ...força a câmera a pular para a próxima cobra automaticamente!
            SelectSnake(1);
        }
    }

    // MÉTODO ATUALIZADO: Seleciona apenas cobras vivas
    void SelectSnake(int step)
    {
        if (snakes.Count == 0) return;

        // 1. Tira a seleção da cobra atual (com segurança, checando se ela ainda existe)
        GameObject current = snakes[selectedId];
        if (current != null)
        {
            SnakeMovement currentMov = current.GetComponentInChildren<SnakeMovement>();
            if (currentMov != null) currentMov.selected = false;
        }

        // 2. Procura a próxima cobra que esteja VIVA
        int attempts = 0;
        int nextId = selectedId;

        // O loop impede que o jogo trave caso TODAS as cobras estejam mortas
        while (attempts < snakes.Count)
        {
            nextId = (nextId + step + snakes.Count) % snakes.Count;
            GameObject candidate = snakes[nextId];
            
            // Verifica se a próxima candidata existe e está ativa na arena
            if (candidate != null && candidate.activeInHierarchy)
            {
                selectedId = nextId;
                SnakeMovement nextMov = candidate.GetComponentInChildren<SnakeMovement>();
                if (nextMov != null) nextMov.selected = true;
                return; // Encontrou uma viva, passa o foco para ela e encerra a busca!
            }
            attempts++;
        }
    }

    void EndMatch()
{
    matchEnded = true;
    Time.timeScale = 0; // Pausa o jogo fisicamente
    
    Debug.Log("<color=cyan><b>--- RELATÓRIO FINAL DA PARTIDA ---</b></color>");

    foreach (GameObject snake in snakes)
    {
        if (snake == null) continue;

        // 1. Identificar se está viva (se o objeto principal está ativo)
        string status = snake.activeInHierarchy ? "<color=green>VIVO</color>" : "<color=red>MORTO</color>";

        // 2. Contar segmentos
        // Assumindo que os segmentos são filhos do objeto ou estão no script SnakeMovement
        var movement = snake.GetComponentInChildren<SnakeMovement>();
        int segmentCount = 0;
        
        if (movement != null)
        {
            // Se você tiver uma lista de segmentos no SnakeMovement, use movement.bodyParts.Count
            // Se os segmentos forem apenas objetos filhos, usamos:
            segmentCount = snake.GetComponentsInChildren<SnakeBody>().Length;
        }

        Debug.Log($"Bot: {snake.name} | Status: {status} | Segmentos: {segmentCount}");
    }
    
    Debug.Log("<color=cyan><b>----------------------------------</b></color>");
}
}