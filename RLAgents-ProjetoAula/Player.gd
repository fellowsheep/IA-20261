extends Node3D

@export var rotation_speed = 3.0
@onready var ball = get_node("../Ball")

@onready var ai_controller = $AIController3D


var movement : float
	
var needs_reset = false

func game_over():
	needs_reset = true

func _physics_process(delta):
	if needs_reset:
		ball.reset()
		needs_reset = false
		return
		
	# Verifica se o controle está configurado para um Humano no editor
	if ai_controller.heuristic == "human":
		# Lê as teclas configuradas no Input Map da Godot
		movement = Input.get_axis("rotate_anticlockwise", "rotate_clockwise")
	else:
		# Lê a ação (o valor entre -1.0 e 1.0) vinda da Rede Neural no Python
		movement = ai_controller.move_action 
		
	# Aplica o movimento escolhido rotacionando o personagem no eixo Y
	rotate_y(movement * delta * rotation_speed)	


func _on_area_3d_body_entered(body):
	print("ball hit paddle")
	
func _ready() -> void:
	ai_controller.init(self)
