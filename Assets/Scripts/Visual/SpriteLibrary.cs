using System.Collections.Generic;
using UnityEngine;

namespace MyArcade
{
	/// <summary>
	/// Finds the 2D animation frames (Resources/Sprites/&lt;set&gt;/&lt;clip&gt;/&lt;direction&gt;_&lt;nn&gt;.png, made by
	/// tools/slice_sprites.py and slice_brute.py) and says how fast each clip plays.
	/// Every character with its own drawings is an "art set" with its own clip table; a new set only needs a folder
	/// and a table here. Characters without a set borrow the "player" set (tinted).
	/// </summary>
	public static class SpriteLibrary
	{
		public readonly struct ClipInfo
		{
			public readonly int Frames;
			public readonly float Fps;
			public readonly bool Loop;

			public ClipInfo(int frames, float fps, bool loop)
			{
				Frames = frames;
				Fps = fps;
				Loop = loop;
			}
		}

		/// <summary>The three drawn views. "Right" is mirrored for left.</summary>
		public enum View { Front = 0, Back = 1, Right = 2 }

		/// <summary>World height (in metres) of the standing character on the canvas: 92 px at 50 pixels per unit.</summary>
		public const float StandingHeight = 1.84f;

		private static readonly string[] ViewNames = { "front", "back", "right" };

		private static readonly Dictionary<string, Dictionary<string, ClipInfo>> Sets = new Dictionary<string, Dictionary<string, ClipInfo>>
		{
			{ "player", new Dictionary<string, ClipInfo>
				{
					{ "idle", new ClipInfo(4, 5f, true) },
					{ "walk", new ClipInfo(9, 12f, true) },
					{ "run", new ClipInfo(11, 16f, true) },
					{ "walk_side", new ClipInfo(8, 12f, true) },   // right view only, a proper 8-pose cycle
					{ "walk_front", new ClipInfo(8, 12f, true) },  // front view only
					{ "walk_back", new ClipInfo(8, 12f, true) },
					{ "idle_side", new ClipInfo(8, 6f, true) },    // drawn idle (right view; also stands in for the back view)
					{ "idle_front", new ClipInfo(8, 6f, true) },
					{ "run_side", new ClipInfo(8, 16f, true) },
					{ "run_back", new ClipInfo(8, 16f, true) },
					{ "run_front", new ClipInfo(8, 16f, true) },
					{ "dash_side", new ClipInfo(8, 20f, true) },   // right view only: the dash always shows its side view
					{ "hurt_side", new ClipInfo(8, 16f, false) },
					{ "die_side", new ClipInfo(7, 9f, false) },    // holds its last frame (lying down)
					{ "parry_side", new ClipInfo(8, 20f, false) },
					{ "slash_ready_side", new ClipInfo(3, 12f, false) }, // the crouched stance held while the slash line is chosen (right view, mirrored)
					{ "shoot_side", new ClipInfo(8, 24f, false) }, // frame 2 = aiming pose, frames 3.. = the shot with muzzle flash
					{ "pipe_swing", new ClipInfo(5, 32f, false) }, // the hit lands the moment the swing starts, so the strike frame must come fast
				}
			},
			{ "brute", new Dictionary<string, ClipInfo>
				{
					{ "idle_front", new ClipInfo(5, 6f, true) },
					{ "idle_side", new ClipInfo(5, 6f, true) },
					{ "run_front", new ClipInfo(10, 14f, true) },
					{ "run_side", new ClipInfo(10, 14f, true) },
					{ "run_back", new ClipInfo(9, 14f, true) },
					{ "pipe_swing", new ClipInfo(3, 14f, false) }, // his punch
					{ "hurt_side", new ClipInfo(4, 14f, false) },
					{ "die_side", new ClipInfo(7, 9f, false) },
				}
			},
		};

		private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

		public static bool Has(string set, string clip)
		{
			return Sets.TryGetValue(set, out var table) && table.ContainsKey(clip);
		}

		public static ClipInfo Info(string set, string clip)
		{
			return Sets[set][clip];
		}

		public static Sprite Get(string set, string clip, View view, int frame)
		{
			string path = "Sprites/" + set + "/" + clip + "/" + ViewNames[(int)view] + "_" + frame.ToString("00");
			if (!Cache.TryGetValue(path, out Sprite sprite))
			{
				sprite = Resources.Load<Sprite>(path);
				Cache[path] = sprite;
			}
			return sprite;
		}
	}
}
