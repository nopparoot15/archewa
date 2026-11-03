using System;
using System.Collections.Generic;
using UnityEngine;

namespace MyArcade
{
	[Serializable]
	public sealed class PixelAnimationSet
	{
		[Serializable]
		public sealed class Clip
		{
			public string name;
			public string direction;
			public int count;
			public float fps;
			public bool loop;
		}

		public Clip[] clips;
		private readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
		private static readonly Dictionary<string, PixelAnimationSet> Loaded = new Dictionary<string, PixelAnimationSet>();
		private string folder;

		public static string Id(Character owner) => owner is Player ? "player" : ((Enemy)owner).Kind.ToString().ToLowerInvariant();
		public static string Folder(string id) => "Sprites/pixel_cast_v5/" + id + "/";

		public static PixelAnimationSet Load(string id)
		{
			return LoadFolder(id, Folder(id));
		}

		public static PixelAnimationSet LoadGun()
		{
			return LoadFolder("player_gun_v6", "Sprites/player_gun_v6/");
		}

		private static PixelAnimationSet LoadFolder(string id, string root)
		{
			if (Loaded.TryGetValue(id, out var existing)) return existing;
			if (Resources.Load<TextAsset>(root + "ready") == null) return null;
			var data = Resources.Load<TextAsset>(root + "manifest");
			if (data == null) return null;
			var set = new PixelAnimationSet();
			JsonUtility.FromJsonOverwrite(data.text, set);
			if (set == null || set.clips == null || set.clips.Length == 0) return null;
			set.folder = root;
			Loaded[id] = set;
			return set;
		}

		public Clip Find(string name, string direction)
		{
			foreach (var clip in clips) if (clip.name == name && clip.direction == direction) return clip;
			return null;
		}

		public Clip Resolve(string name, string direction) => Find(name, direction) ?? Find(name, "right") ?? Find(name, "front");

		public Sprite Frame(Clip clip, int index)
		{
			if (clip == null || clip.count <= 0) return null;
			string key = clip.name + "/" + clip.direction + "_" + Mathf.Clamp(index, 0, clip.count - 1).ToString("00");
			if (!sprites.TryGetValue(key, out var sprite))
			{
				sprite = Resources.Load<Sprite>(folder + key);
				sprites[key] = sprite;
			}
			return sprite;
		}
	}
}
