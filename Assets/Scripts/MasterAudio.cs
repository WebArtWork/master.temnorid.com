using ITKamianets.Engine.Synth;
using UnityEngine;

namespace Temnorid.Master
{
	public enum Sfx
	{
		Click,
		/// <summary>A pen scratching on paper.</summary>
		Quill,
		/// <summary>The seal press: a soft thud.</summary>
		Stamp,
		Page,
		Coin,
		Good,
		Bad,
		/// <summary>An iron hammer on an anvil.</summary>
		Hammer,
		/// <summary>The bubbling of a pot.</summary>
		Bubble,
		/// <summary>Scissors and cloth.</summary>
		Cloth,
		Munch,
		Brush,
		Hoof,
		Whoosh,
		Neigh,
		LevelUp,
		Tick,
	}

	/// <summary>The sounds, made from simple waves and noise when the game starts, so no audio files are needed. A muted game plays exactly the same; the timing never depends on a sound.</summary>
	public sealed class MasterAudio : MonoBehaviour
	{
		SoundBoard _boardCache;

		/// <summary>The game's Awake may set the volumes before this component has woken up, so the board is made on first use.</summary>
		SoundBoard Board => _boardCache != null ? _boardCache : (_boardCache = gameObject.AddComponent<SoundBoard>());

		public float MasterVolume => Board.MasterVolume;
		public float SfxVolume => Board.SfxVolume;

		void Awake()
		{
			var kinds = System.Enum.GetValues(typeof(Sfx));
			var clips = new AudioClip[kinds.Length];
			foreach (Sfx kind in kinds) clips[(int)kind] = Make(kind);
			Board.Init(Ambience(), clips, 0.25f);
		}

		public void SetVolumes(float master, float sfx) => Board.SetVolumes(master, sfx);

		public void Play(Sfx kind, float volume = 1f) => Board.Play((int)kind, volume);

		static AudioClip Make(Sfx kind)
		{
			var random = new System.Random((int)kind * 131 + 7);
			float Noise() => Tone.Noise(random);
			switch (kind)
			{
				case Sfx.Click:
					return Tone.Build("click", 0.05f, t => Mathf.Sin(t * 2f * Mathf.PI * 1000f) * Tone.Envelope(t, 0.05f) * 0.45f);
				case Sfx.Tick:
					return Tone.Build("tick", 0.04f, t => Mathf.Sin(t * 2f * Mathf.PI * 1600f) * Tone.Envelope(t, 0.04f) * 0.4f);
				case Sfx.Quill:
					return Tone.Build("quill", 0.18f, t => Noise() * Tone.Envelope(t, 0.18f, 0.01f) * 0.3f * (0.6f + 0.4f * Mathf.Sin(t * 90f)));
				case Sfx.Stamp:
					return Tone.Build("stamp", 0.25f, t => (Mathf.Sin(t * 2f * Mathf.PI * 70f) * 0.9f + Noise() * 0.2f) * Tone.Envelope(t, 0.25f, 0.003f) * 0.8f);
				case Sfx.Page:
					return Tone.Build("page", 0.22f, t => Noise() * Tone.Envelope(t, 0.22f, 0.02f) * (0.5f - 0.3f * t / 0.22f));
				case Sfx.Coin:
					return Tone.Build("coin", 0.3f, t => (Mathf.Sin(t * 2f * Mathf.PI * 1400f) + 0.5f * Mathf.Sin(t * 2f * Mathf.PI * 2100f)) * Mathf.Exp(-9f * t) * 0.35f);
				case Sfx.Good:
					return Tone.Build("good", 0.6f, t => Mathf.Sin(t * 2f * Mathf.PI * (t < 0.2f ? 523f : t < 0.4f ? 659f : 784f)) * Tone.Envelope(t, 0.6f, 0.01f) * 0.45f);
				case Sfx.Hammer:
					return Tone.Build("hammer", 0.3f, t => (Mathf.Sin(t * 2f * Mathf.PI * 520f) * 0.5f + Mathf.Sin(t * 2f * Mathf.PI * 1310f) * 0.3f + Noise() * 0.25f) * Mathf.Exp(-14f * t) * 0.7f);
				case Sfx.Bubble:
					return Tone.Build("bubble", 0.3f, t => Mathf.Sin(t * 2f * Mathf.PI * (300f + 900f * t)) * Mathf.Exp(-10f * t) * 0.4f);
				case Sfx.Cloth:
					return Tone.Build("cloth", 0.25f, t => Noise() * Tone.Envelope(t, 0.25f, 0.04f) * 0.25f * Mathf.Sin(Mathf.PI * t / 0.25f));
				case Sfx.Munch:
					return Tone.Build("munch", 0.3f, t => Noise() * (Mathf.Sin(t * 50f) > 0f ? 1f : 0.2f) * Tone.Envelope(t, 0.3f, 0.01f) * 0.35f);
				case Sfx.Brush:
					return Tone.Build("brush", 0.35f, t => Noise() * Tone.Envelope(t, 0.35f, 0.05f) * 0.22f * (0.7f + 0.3f * Mathf.Sin(t * 40f)));
				case Sfx.Hoof:
					return Tone.Build("hoof", 0.1f, t => (Mathf.Sin(t * 2f * Mathf.PI * 150f) + Noise() * 0.3f) * Mathf.Exp(-30f * t) * 0.6f);
				case Sfx.Whoosh:
					return Tone.Build("whoosh", 0.3f, t => Noise() * Mathf.Sin(Mathf.PI * t / 0.3f) * 0.22f);
				case Sfx.Neigh:
					return Tone.Build("neigh", 0.5f, t => Mathf.Sin(t * 2f * Mathf.PI * (500f + 300f * Mathf.Sin(t * 14f))) * Tone.Envelope(t, 0.5f, 0.05f) * 0.3f);
				case Sfx.LevelUp:
					return Tone.Build("levelup", 0.8f, t => Mathf.Sin(t * 2f * Mathf.PI * (t < 0.2f ? 392f : t < 0.4f ? 523f : t < 0.6f ? 659f : 784f)) * Tone.Envelope(t, 0.8f, 0.01f) * 0.4f);
				default:
					return Tone.Build("bad", 0.45f, t => Mathf.Sin(t * 2f * Mathf.PI * (t < 0.2f ? 220f : 165f)) * Tone.Envelope(t, 0.45f, 0.01f) * 0.5f);
			}
		}

		static AudioClip Ambience()
		{
			var random = new System.Random(5);
			var low = 0f;
			const float length = 6f;
			return Tone.Build("ambience", length, t =>
			{
				var white = (float)(random.NextDouble() * 2.0 - 1.0);
				low += (white - low) * 0.01f;
				var crackle = random.NextDouble() < 0.0006 ? white * 0.4f : 0f;
				var edge = Mathf.Clamp01(Mathf.Min(t, length - t) / 0.2f);
				return (low * 1.5f + crackle) * edge;
			});
		}
	}
}
