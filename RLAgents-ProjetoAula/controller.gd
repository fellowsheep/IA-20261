extends AIController3D

# Armazena a ação amostrada pela política do agente, rodando no Python
var move_action : float = 0.0

func get_obs() -> Dictionary:
	# Obtém a posição e a velocidade da bola no referencial local do jogador (paddle)
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
	move_action = clamp(action["move_action"][0], -1.0, 1.0)
