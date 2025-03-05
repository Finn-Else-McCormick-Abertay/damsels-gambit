using Godot;
using System;

public partial class AudioSystem : Node
{
	[Export] private AudioStreamPlayer[] SFXPlayers;
	[Export] private AudioStreamPlayer MusicPlayer;

	public bool IsMusicPlaying { get; set; } = false;
	private string MusicPlaying = string.Empty;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void PlaySFX(string Path) {
		
		foreach (var player in SFXPlayers) {
			if (player.Playing == false) {
				player.Stream = GD.Load<AudioStream>(Path);
				player.Play();
			}
		}
	}

		public void PlayMusic(string Path) {
		if (Path == "") { return; }
		if (MusicPlaying == Path && IsMusicPlaying == true) { return; }
		MusicPlaying = Path;
		MusicPlayer.Stream = GD.Load<AudioStream>(Path);
		MusicPlayer.Play();
		IsMusicPlaying = true;
	}
	public void StopMusic() {
		MusicPlayer.Stop();
		IsMusicPlaying = false;
	}
}
