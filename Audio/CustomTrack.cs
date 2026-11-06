using System;
using UnityEngine;

namespace Jukebox
{
    public class CustomTrack
    {
        // File name with extension. This is what the settings store, so "song.mp3" and "song.ogg"
        // can be switched on and off separately.
        public string Key { get; }

        // File name without extension, shown in the menu.
        public string Name { get; }

        public string Path { get; }
        public DateTime LastWriteUtc { get; }
        public long Size { get; }
        public AudioClip Clip { get; }

        public CustomTrack(string key, string name, string path, DateTime lastWriteUtc, long size, AudioClip clip)
        {
            Key = key;
            Name = name;
            Path = path;
            LastWriteUtc = lastWriteUtc;
            Size = size;
            Clip = clip;
        }
    }
}
