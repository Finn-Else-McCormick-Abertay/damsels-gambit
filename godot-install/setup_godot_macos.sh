#!/bin/sh

curl -L https://github.com/godotengine/godot/releases/download/4.4-stable/Godot_v4.4-stable_mono_macos.universal.zip > godot_macos.zip && \
unzip godot_macos.zip && \
rm godot_macos.zip