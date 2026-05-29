using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class ArenaVisuals : MonoBehaviour
{
    [Header("Configurações da Grid")]
    public float gridSize = 2.0f;
    public float lineWidth = 0.05f;
    [ColorUsage(true, true)]
    public Color gridColor = new Color(0.0f, 0.8f, 1.0f, 1.0f); 
    public Material lineMaterial; 

    [Header("Fundo da Arena")]
    public Color backgroundColor = new Color(0.0f, 0.05f, 0.15f, 1.0f); // Azul bem escuro

    [Header("Decoração Central")]
    public GameObject trophyPrefab; 
    [ColorUsage(true, true)] // Isso habilita o seletor HDR no Inspector!
    public Color trophyGlowColor = new Color(0.0f, 0.8f, 1.0f, 1.0f); // Azul neon padrão

    void Start()
    {
        GenerateArenaVisuals();
    }

    void GenerateArenaVisuals()
    {
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        float minX = col.bounds.min.x;
        float minY = col.bounds.min.y;
        float maxX = col.bounds.max.x;
        float maxY = col.bounds.max.y;

        // 1. Criar o Fundo Sólido Procedural primeiro (para ficar por baixo)
        CreateBackground(minX, maxX, minY, maxY);

        // 2. Criar a Grid
        GameObject gridParent = new GameObject("Procedural_Grid");
        gridParent.transform.parent = transform;

        for (float x = minX; x <= maxX; x += gridSize)
        {
            DrawLine(new Vector3(x, minY, 0), new Vector3(x, maxY, 0), gridParent.transform);
        }

        for (float y = minY; y <= maxY; y += gridSize)
        {
            DrawLine(new Vector3(minX, y, 0), new Vector3(maxX, y, 0), gridParent.transform);
        }

        // 3. Posicionar o Troféu
        if (trophyPrefab != null)
        {
            Vector3 center = new Vector3((minX + maxX) / 2f, (minY + maxY) / 2f, 0);
            GameObject trophy = Instantiate(trophyPrefab, center, Quaternion.identity);
            trophy.name = "Trophy_Center";
            trophy.transform.parent = transform;
            
            SpriteRenderer sr = trophy.GetComponent<SpriteRenderer>();
            if(sr != null) 
            {
                sr.sortingOrder = -10; 
                sr.color = trophyGlowColor; // Aplica a cor HDR para acender o Bloom
            }
        }
    }

    void CreateBackground(float minX, float maxX, float minY, float maxY)
    {
        GameObject bgObj = new GameObject("Arena_Background");
        bgObj.transform.parent = transform;

        SpriteRenderer sr = bgObj.AddComponent<SpriteRenderer>();

        // Gera uma textura 1x1 branca diretamente pela memória
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();

        // Transforma a textura em Sprite
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        sr.color = backgroundColor;

        // Calcula a largura e altura baseada no colisor
        float width = maxX - minX;
        float height = maxY - minY;
        
        // Posiciona no centro e estica para o tamanho exato do colisor
        bgObj.transform.position = new Vector3((minX + maxX) / 2f, (minY + maxY) / 2f, 0);
        bgObj.transform.localScale = new Vector3(width, height, 1);

        // Garante que fique atrás das linhas da grid (que estão em -20)
        sr.sortingOrder = -30;
    }

    void DrawLine(Vector3 startPos, Vector3 endPos, Transform parent)
    {
        GameObject lineObj = new GameObject("Grid_Line");
        lineObj.transform.parent = parent;
        
        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.material = lineMaterial;
        
        lr.startColor = gridColor;
        lr.endColor = gridColor;
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        
        lr.numCapVertices = 2; 
        lr.sortingOrder = -20; 
        
        lr.SetPosition(0, startPos);
        lr.SetPosition(1, endPos);
    }
}