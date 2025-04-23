using DamselsGambit.Util;
using Godot;
using System;
using System.Collections.Generic;

namespace DamselsGambit;

// This is an autoload singleton. Because of how Godot works, you can technically instantiate it yourself. Don't.
public partial class AudioManager : Node
{
	public static AudioManager Instance { get; private set; }

	private readonly List<AudioStreamPlayer> _sfxPlayers = [];
	private AudioStreamPlayer _musicPlayer;
	public string _activeMusic;
	public int lastValue;

	public static bool IsMusicPlaying => Instance?._musicPlayer?.Playing ?? false;

	public override void _Ready(){

		PlayMusic("res://assets/audio/Menu.mp3");

	}

	public override void _Process(double delta) {
		if(!IsMusicPlaying){
				PlayMusic(Instance._activeMusic);
		}

		if (GameManager.CardGameController.IsValid()){
		if (lastValue != (GameManager.CardGameController.Score)){
			if(lastValue > GameManager.CardGameController.Score){
				PlaySFX("res://assets/audio/AffMinus.mp3");
			}else{PlaySFX("res://assets/audio/AffPlus.mp3");}
			lastValue = GameManager.CardGameController.Score;
		}}
	}

	

	public override void _EnterTree() {
		if (Instance is not null) throw AutoloadException.For(this);
		Instance = this;

		_musicPlayer = new AudioStreamPlayer() { Bus = "Music" };
		AddChild(_musicPlayer); _musicPlayer.Owner = this;
		
		
		for (int i = 0; i < 8; ++i) {
			var sfxPlayer = new AudioStreamPlayer() { Bus = "SFX" };
			AddChild(sfxPlayer); sfxPlayer.Owner = this;
			_sfxPlayers.Add(sfxPlayer);
		}
	}

	public override void _ExitTree() {
		_musicPlayer.QueueFree();
		foreach (var sfxPlayer in _sfxPlayers) sfxPlayer.QueueFree();
	}

	public static void PlaySFX(string filePath) {
		filePath = $"res://{filePath.Replace('\\', '/').StripFront("res://")}";
		if (!ResourceLoader.Exists(filePath) || !Instance.IsValid()) return;

		foreach (var player in Instance._sfxPlayers) {
			if (!player.IsValid() || player.Playing) continue;

			player.Stream = ResourceLoader.Load<AudioStream>(filePath);
			player.Play();
			return;
		}
	}

	public static void PlayMusic(string filePath) {
		Instance._activeMusic = filePath;
		filePath = $"res://{filePath.Replace('\\', '/').StripFront("res://")}";

		if (!ResourceLoader.Exists(filePath) || !Instance.IsValid() || (IsMusicPlaying && Instance._activeMusic == filePath)) return;

	
		Instance._musicPlayer.Stream = ResourceLoader.Load<AudioStream>(filePath);
		Instance._musicPlayer.Play();

	}

	public static void StopMusic() {
		Instance._musicPlayer.Stop();
		Instance._activeMusic = "";
	}
}
