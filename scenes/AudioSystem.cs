using DamselsGambit.Util;
using Godot;
using System;

namespace DamselsGambit;

public partial class AudioSystem : Node
{
	public static AudioSystem Instance { get; private set; }

	public override void _EnterTree() {
		if (Instance is not null) throw AutoloadException.For(this);
		Instance = this;
	}

	[Export] private AudioStreamPlayer[] SFXPlayers;
	[Export] private AudioStreamPlayer MusicPlayer;
	private string _activeMusic;

	public static bool IsMusicPlaying => Instance?.MusicPlayer?.Playing ?? false;

	public static void PlaySFX(string filePath) {
		filePath = $"res://{filePath.Replace('\\', '/').StripFront("res://")}";
		
		if (!ResourceLoader.Exists(filePath) || !Instance.IsValid()) return;
		foreach (var player in Instance.SFXPlayers) {
			if (!player.Playing) {
				player.Stream = ResourceLoader.Load<AudioStream>(filePath);
				player.Play();
			}
		}
	}

	public static void PlayMusic(string filePath) {
		filePath = $"res://{filePath.Replace('\\', '/').StripFront("res://")}";
		if (!ResourceLoader.Exists(filePath) || !Instance.IsValid() || (IsMusicPlaying && Instance._activeMusic == filePath)) return;

		Instance._activeMusic = filePath;
		Instance.MusicPlayer.Stream = ResourceLoader.Load<AudioStream>(filePath);
		Instance.MusicPlayer.Play();
	}

	public static void StopMusic() {
		Instance.MusicPlayer.Stop();
		Instance._activeMusic = "";
	}
}
