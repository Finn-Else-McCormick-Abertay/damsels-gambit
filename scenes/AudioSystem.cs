using Godot;
using System;

public partial class AudioSystem : Node
{
	[Export] private AudioStreamPlayer[] SoundPlayers;
	[Export] private AudioStreamPlayer musicPlayer;

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

	public void Play(string Path) {
		
		foreach (var player in SoundPlayers) {
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
		musicPlayer.Stream = GD.Load<AudioStream>(Path);
		musicPlayer.Play();
		IsMusicPlaying = true;
	}
	public void StopMusic() {
		musicPlayer.Stop();
		IsMusicPlaying = false;
	}
}
