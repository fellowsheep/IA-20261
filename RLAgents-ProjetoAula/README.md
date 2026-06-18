# Passo-a-paasso: Treino de IA no Godot 4 com Aprendizado por Reforço (Godot RL Agents)

Este tutorial detalha o processo de criação de um ambiente personalizado no motor Godot 4 e a implementação de um controlador de Inteligência Artificial que aprende a jogar de forma autônoma usando **Deep Reinforcement Learning** (Algoritmo PPO via Stable Baselines 3). A base deste documento é o tutorial que se encontra [neste link](https://huggingface.co/learn/deep-rl-course/unitbonus3/godotrl) e o projeto pode ser baixado [diretamente deles](https://drive.google.com/file/d/1C7xd3TibJHlxFEJPBgBLpksgxrFZ3D8e/view?usp=share_link) ou de [nosso repositório no GitHub](https://github.com/fellowsheep/IA-20261/tree/main/RLAgents-ProjetoAula).

O fluxo de trabalho divide-se em duas partes:

* O **Python** realiza os cálculos matemáticos complexos da rede neural.

* A **Godot** atua como o simulador onde a IA interage, observa e ganha recompensas.

---

## Fase 1: Preparação do Ambiente

### 1. Requisitos do Motor

É obrigatório utilizar a versão [**Godot Engine .NET (Mono)**](https://downloads.godotengine.org/?version=4.7&flavor=stable&slug=mono_win64.zip&platform=windows.64). A versão "Standard" não funcionará na etapa final, pois a inferência (leitura do cérebro treinado da IA) exige compilação em C#.

> A professora utilizou a Godot 4.6. Mas deve funcionar com a versão atual.

### 2. Instalação das Bibliotecas Python

Para garantir que o treino e a exportação do modelo ocorram sem falhas de dependência, abra o terminal e instale as bibliotecas base:

```bash
pip install godot-rl stable-baselines3 onnx onnxscript tensorboard
```
Para isso, você precisa ter instalado o [interpretador do Python](https://www.python.org/downloads/) em sua máquina. 

> A professora utilizou o Python 3.12. Mas deve funcionar com a versão atual.

## Fase 2: Construção do "Cérebro" da IA

### 1. Instalação e configuração do RL Agents

* Abrir o projeto pelo arquivo `project.godot`, ir em **Biblioteca de Recursos**, buscar por *RL Agents* e baixar o recurso **Godot RL Agents**.
* Ir em **Projeto > Configuração do Projeto > Plugins** e ativar o plugin do RL Agents.
   
 ### 2. Adição do script Controlador do agente:
* Abra a cena do jogador (`player.tscn`).
* Clique com o botão direito no nó raiz (`Player`), selecione **Add Child Node** e insira um `AIController3D`.
* Programar os Sentidos e Ações (`controller.gd`):
   * Adicione um script ao nó `AIController3D`. Este script define as observações (o que a IA vê), as ações (o que ela pode fazer) e as recompensas.
   * Sobrescreva os métodos conforme o código abaixo:

```python
extends AIController3D

# Armazena a ação amostrada pela política do agente
var move_action : float = 0.0

func get_obs() -> Dictionary:
    # Obtém a posição e a velocidade da bola no referencial local do jogador
    var ball_pos = to_local(_player.ball.global_position)
    var ball_vel = to_local(_player.ball.linear_velocity)
    var obs = [ball_pos.x, ball_pos.z, ball_vel.x/10.0, ball_vel.z/10.0]
    return {"obs": obs}

func get_reward() -> float:
    return reward

func get_action_space() -> Dictionary:
    return {
        "move_action" : { "size": 1, "action_type": "continuous" }
    }

func set_action(action) -> void:
    # CORREÇÃO DE TIPO (Godot 4):
    # O Python envia a ação empacotada num Array. A função clamp() do Godot
    # exige argumentos do mesmo tipo (float). Usamos [0] para extrair o valor.
    move_action = clamp(action["move_action"][0], -1.0, 1.0)
```
## Fase 3: Integração com o Jogador

### 1. Atualização do Script de Movimento (`Player.gd`)
O personagem precisa de uma lógica híbrida para saber quando obedecer ao teclado e quando obedecer à Rede Neural.

```python
extends Node3D

@export var rotation_speed = 3.0
@onready var ball = get_node("../Ball")
@onready var ai_controller = $AIController3D

var movement : float
var needs_reset = false

func _ready() -> void:
    ai_controller.init(self) # Conecta o corpo ao cérebro (AIController)

func _physics_process(delta):
    if needs_reset:
        ball.reset()
        needs_reset = false
        return
        
    # Lógica Híbrida de Controle
    if ai_controller.heuristic == "human":
        movement = Input.get_axis("rotate_anticlockwise", "rotate_clockwise")
    else:
        movement = ai_controller.move_action
        
    rotate_y(movement * delta * rotation_speed)
```

### 2. Adição do nó de Sincronização 
   
* Abra a cena principal de treinamento (`train.tscn`).
* Adicione o nó `Sync` (Godot RL Agents Sync). Ele fará a ponte TCP entre a engine e o terminal Python.
* **Dica de Performance:** No Inspetor deste nó, aumente a propriedade `Speed Up` (ex: `8`) para acelerar a simulação visual e otimizar o tempo de treino.

---

## Fase 4: O Treinamento

### 1. Inicialização do treinamento em Python
No terminal do seu sistema operacional (dentro da pasta do projeto), execute o script de treinamento:

```bash
python stable_baselines3_example.py --timesteps=100000 --onnx_export_path=model.onnx
```
O terminal entrará em espera com a mensagem: `waiting for remote GODOT connection`.

### 2. Inicialização do treinamento na Godot
Volte ao editor, certifique-se de que a cena `train.tscn` está aberta e pressione **PLAY** (F5).
O jogo abrirá rodando em alta velocidade e o terminal começará a imprimir as métricas de evolução da IA.

### 3. Visualização (Opcional)
Para acompanhar os gráficos interativos do aprendizado em tempo real, abra um **segundo terminal** no mesmo diretório do projeto e rode o comando abaixo:

```bash
tensorboard --logdir logs
```

(Acesse o link gerado pelo terminal no seu navegador de internet).

---

## Fase 5: Inferência

Quando o treino termina, o ficheiro `model.onnx` é gerado. Para fazer a IA jogar sozinha:
1. Selecione o nó `Sync`.
2. Mude o `Control Mode` para `Onnx Inference`.
3. Carregue o `model.onnx` no campo `Onnx Model Path`.
4. Dê **PLAY** e assista seu agente inteligente (treinado) jogando 😊

---

## Resolução de Possíveis Problemas (Troubleshooting)

Baseado nos problemas que enfrentei ao rodar este tutorial, seguem algumas dicas:

### Problema 1: Erro de `Safe Save` ou Bloqueio de Arquivo
Ao carregar o `.onnx`, o Godot acusa erro de permissão ou falha de gravação segura.

**Causa:** Antivírus (ex: Kaspersky) ou nuvens (OneDrive) estão a bloquear a leitura/escrita simultânea do ficheiro.

**Solução Rápida:**
  1. Vá até à pasta do projeto usando o Explorador de Arquivos.
  2. Copie e cole o `model.onnx` (gerando um `model - Copia.onnx`).
  3. Volte ao Godot e carregue esta cópia limpa. Nenhum outro programa estará a bloquear este novo ficheiro.
* **Outra solução:** Escreva o caminho completo no campo `Onnx Model Path` do Inspetor (ex: `C:\Users\<seu_usuario>\Documents\ProjetosGodot\RingPong_starter_aula\model.onnx`).

**Solução permanente:** Desativar a sincronização da nuvem ou colocar a pasta do Godot nas exclusões do Antivírus.

### Problema 2: Erro de compilação C# (`Nonexistent function 'new' in base 'CSharpScript'`)
O jogo bloqueia ao dar Play na inferência pois o Godot não consegue executar o wrapper em C#.

**Causa:** Ao abrir o projeto, o Godot entendeu que seria um projeto utilizando apenas scripts `.gd` e não criou uma *solution* (projeto) para compilar os scripts `.net` do plugin RL Agents.

**Solução:**
  1. Vá ao menu superior: **Project > Tools > C# > Create C# solution**.
  2. O botão de **Build** (ícone de martelo) aparecerá no canto superior direito.
  3. Agora, ao apertar F5 a Godot deve antes compilar o script em C# necessário.
  4. Se isso não acontecer, clique no botão de Build (ícone de martelo).

### Problema 3: Em falta o namespace `Microsoft.ML`
Ao clicar no botão de Build (martelo), a compilação falha dizendo que `Microsoft.ML` não existe.

**Causa:** O projeto não possui a biblioteca ONNX Runtime instalada.

**Solução:**
  1. Abra o terminal na pasta do projeto.
  2. Execute o comando para injetar o pacote diretamente no ficheiro do projeto (`.csproj`):

```bash
dotnet add RingPong.csproj package Microsoft.ML.OnnxRuntime
```

1. Volte ao Godot e clique no botão de **Build** (martelo) novamente.
2. Aguarde o aviso de Build succeeded, dê **PLAY** e assista seu agente inteligente (treinado) jogando 😊

---

## Links úteis

* [Github do RL Agents](https://github.com/edbeeching/godot_rl_agents) - além do código, possui link para outros tutoriais 
