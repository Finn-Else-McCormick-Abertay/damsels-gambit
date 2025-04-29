using DamselsGambit.Util;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

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

	public override void _Process(double delta) {

		if (GameManager.CardGameController.IsValid()){
		if (lastValue != (GameManager.CardGameController.Score)){
			if(lastValue > GameManager.CardGameController.Score){
				PlaySFX("res://assets/audio/AffPlus.mp3");
			}else{PlaySFX("res://assets/audio/AffMinus.mp3");}
			lastValue = GameManager.CardGameController.Score;
		}}
	}

	public static class BusName {
		public static readonly StringName Master = "Master";
		public static readonly StringName Music = "Music";
		public static readonly StringName SFX = "SFX";
	}

	public override void _EnterTree() {
		if (Instance is not null) throw AutoloadException.For(this); Instance = this;
		ProcessMode = ProcessModeEnum.Always;

		_musicPlayer = new AudioStreamPlayer() { Bus = BusName.Music };
		AddChild(_musicPlayer);

		foreach (var _ in RangeOf<int>.UpTo(8)) {
			var sfxPlayer = new AudioStreamPlayer() { Bus = BusName.SFX };
			AddChild(sfxPlayer); _sfxPlayers.Add(sfxPlayer);
		}
	}

	public override void _ExitTree() {
		_musicPlayer.QueueFree();
		foreach (var sfxPlayer in _sfxPlayers) sfxPlayer.QueueFree();
	}

	public static void PlaySFX(string filePath) {
		if (!Instance.IsValid() || filePath.IsNullOrEmpty()) return;

		filePath = $"res://{filePath.Replace('\\', '/').StripFront("res://")}";
		if (!ResourceLoader.Exists(filePath)) return;

		if (Instance._sfxPlayers.FirstOrDefault(player => player.IsValid() && !player.Playing) is AudioStreamPlayer freePlayer) {
			freePlayer.Stream = ResourceLoader.Load<AudioStream>(filePath); freePlayer.Play();
		}
		else Console.Warning($"Failed to play SFX '{filePath}'");
	}

	public static void PlayMusic(string filePath) {
		if (!Instance.IsValid() || filePath.IsNullOrEmpty()) return;

		filePath = $"res://{filePath.Replace('\\', '/').StripFront("res://")}";
		if (IsMusicPlaying && Instance._activeMusic == filePath || !ResourceLoader.Exists(filePath)) return;

		Instance._activeMusic = filePath;
		Instance._musicPlayer.Stream = ResourceLoader.Load<AudioStream>(filePath);
		Instance._musicPlayer.Play();
	}

	public static void StopMusic() {
		Instance._musicPlayer.Stop();
		Instance._activeMusic = null;
	}
}
