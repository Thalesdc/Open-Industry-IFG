extends PanelContainer

@onready var play_button: Button = $HBoxContainer/Play
@onready var pause_button: Button = $HBoxContainer/Pause
@onready var stop_button: Button = $HBoxContainer/Stop

var play = false
var pause = true
var stop = true

func _disable_buttons() -> void:
	play_button.disabled = true
	pause_button.disabled = true
	stop_button.disabled = true
	
func _enable_buttons() -> void:
	play_button.disabled = play
	pause_button.disabled = pause
	stop_button.disabled = stop


func _ready() -> void:
	get_tree().paused = false
	var Main = get_tree().root.get_node("Cena_1")

	
	GlobalVariables.simulation_started.connect(func () -> void:
		play_button.disabled = true
		pause_button.disabled = false
		stop_button.disabled = false
		play = true
		pause = false
		stop = false
	)
	GlobalVariables.simulation_ended.connect(func () -> void:
		play_button.disabled = false
		pause_button.disabled = true
		stop_button.disabled = true
		play = false
		pause = true
		stop = true	
	)
	
	play_button.pressed.connect(func () -> void:
		print("\n play_button.pressed SimulationControl")
		
		Main.SavePositions();
		
		pause_button.button_pressed = false
		play = false
		GlobalVariables.simulation_set_paused.emit(false)
		GlobalVariables.simulation_started.emit()
		if(EditorInterface.has_method("set_simulation_started")):
			EditorInterface.call("set_simulation_started",true)
	)
	pause_button.toggled.connect(func (pressed: bool) -> void:
		print("\n pause_button.pressed SimulationControl")
		GlobalVariables.simulation_set_paused.emit(pressed)
	)
	stop_button.pressed.connect(func () -> void:
		print("\n stop_button.pressed SimulationControl")
		
		Main.ResetPositions();
		
		pause_button.button_pressed = false
		pause = false
		GlobalVariables.simulation_set_paused.emit(false)
		GlobalVariables.simulation_ended.emit()
		if(EditorInterface.has_method("set_simulation_started")):
			EditorInterface.call("set_simulation_started",false)
	)
