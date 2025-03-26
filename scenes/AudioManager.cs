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
	private string _queuedMusic;

	public static bool IsMusicPlaying => Instance?._musicPlayer?.Playing ?? false;

	//starts the background music, this can be changed if more background music is added
	public override void _Ready(){

		PlayMusic("res://assets/audio/menu.mp3");

	}

	// if the music ends play the queued music, sets the queued music.
	public override void _Process(double delta) {
	
		_queuedMusic = "res://assets/audio/menu.mp3"
		if(!IsMusicPlaying){
				PlayMusic(_queuedMusic);
		}
	}

	
	//when the tree is loaded setup the audio players and link busses
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

	//close audio players when no longer needed
	public override void _ExitTree() {
		_musicPlayer.QueueFree();
		foreach (var sfxPlayer in _sfxPlayers) sfxPlayer.QueueFree();
	}

	//takes in a filepath, finds the file and finds an empty sfx player and plays it through there
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

	//takes in filepath, plays file through the music player bus and sets current music playing
	public static void PlayMusic(string filePath) {

		filePath = $"res://{filePath.Replace('\\', '/').StripFront("res://")}";

		if (!ResourceLoader.Exists(filePath) || !Instance.IsValid() || (IsMusicPlaying && Instance._activeMusic == filePath)) return;

		Instance._activeMusic = filePath;
		Instance._musicPlayer.Stream = ResourceLoader.Load<AudioStream>(filePath);
		Instance._musicPlayer.Play();

	}

	//stops music from the audio bus
	public static void StopMusic() {
		Instance._musicPlayer.Stop();
		Instance._activeMusic = "";
	}
}
