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
	private string _activeMusic;

	public static bool IsMusicPlaying => Instance?._musicPlayer?.Playing ?? false;

	public override void _Ready(){
		// Continue running when paused
		ProcessMode = ProcessModeEnum.Always;

		PlayMusic("res://assets/audio/menu.mp3");
	}

	public override void _Process(double delta) {
		if(!IsMusicPlaying){ PlayMusic("res://assets/audio/menu.mp3"); }
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

		filePath = $"res://{filePath.Replace('\\', '/').StripFront("res://")}";

		if (!ResourceLoader.Exists(filePath) || !Instance.IsValid() || (IsMusicPlaying && Instance._activeMusic == filePath)) return;

		Instance._activeMusic = filePath;
		Instance._musicPlayer.Stream = ResourceLoader.Load<AudioStream>(filePath);
		Instance._musicPlayer.Play();

	}

	public static void StopMusic() {
		Instance._musicPlayer.Stop();
		Instance._activeMusic = "";
	}
}
