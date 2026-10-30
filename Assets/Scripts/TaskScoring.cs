using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace Temnorid.Master
{
	/// <summary>The tracing score: resample both paths by length, compare point by point. Everything is in panel units, so it does not depend on the screen.</summary>
	public static class TraceMath
	{
		public static float Length(IList<Vector2> points)
		{
			var length = 0f;
			for (var i = 1; i < points.Count; i++) length += Vector2.Distance(points[i - 1], points[i]);
			return length;
		}

		/// <summary>The path as <paramref name="count"/> points spaced evenly along its length.</summary>
		public static Vector2[] Resample(IList<Vector2> points, int count)
		{
			var result = new Vector2[count];
			if (points.Count == 0) return result;
			var total = Length(points);
			if (total <= 1e-6f || points.Count == 1)
			{
				for (var i = 0; i < count; i++) result[i] = points[0];
				return result;
			}
			var step = total / (count - 1);
			result[0] = points[0];
			var next = 1;
			var travelled = 0f;
			var target = step;
			for (var i = 1; i < points.Count && next < count; i++)
			{
				var a = points[i - 1];
				var b = points[i];
				var segment = Vector2.Distance(a, b);
				while (next < count && travelled + segment >= target - 1e-6f)
				{
					var t = segment <= 1e-9f ? 0f : (target - travelled) / segment;
					result[next++] = Vector2.Lerp(a, b, Mathf.Clamp01(t));
					target = step * next;
				}
				travelled += segment;
			}
			while (next < count) result[next++] = points[points.Count - 1];
			return result;
		}

		static float MeanError(IList<Vector2> a, IList<Vector2> b)
		{
			var sum = 0f;
			for (var i = 0; i < a.Count; i++) sum += Vector2.Distance(a[i], b[i]);
			return sum / a.Count;
		}

		/// <summary>0 to 100: <c>round(100 × (1 − error / 0.15))</c> over 64 resampled points. Drawing the path backwards is allowed.</summary>
		public static int Score(IList<Vector2> drawn, IList<Vector2> reference)
		{
			var player = Resample(drawn, MasterConfig.ResamplePoints);
			var guide = Resample(reference, MasterConfig.ResamplePoints);
			var error = Mathf.Min(MeanError(player, guide), MeanError(player.Reverse().ToArray(), guide));
			return Mathf.Clamp(Mathf.RoundToInt(100f * (1f - error / MasterConfig.TraceZeroError)), 0, 100);
		}

		/// <summary>A screen point as a point in the panel, 0 to 1 on both axes (y up), clamped to the panel.</summary>
		public static Vector2 Normalize(Vector2 screenPoint, Rect panel)
		{
			return new Vector2(Mathf.Clamp01((screenPoint.x - panel.xMin) / Mathf.Max(1e-6f, panel.width)), Mathf.Clamp01((screenPoint.y - panel.yMin) / Mathf.Max(1e-6f, panel.height)));
		}
	}

	/// <summary>The sliding markers: one place on a bar at any moment, locked by a press.</summary>
	public static class TimingMath
	{
		/// <summary>A marker that sweeps 0 to 1 and back, <paramref name="speed"/> bars per second.</summary>
		public static float Marker(float time, float speed)
		{
			var cycle = Mathf.Repeat(time * speed, 2f);
			return cycle <= 1f ? cycle : 2f - cycle;
		}

		public static bool Hit(float marker, float centre, float halfWidth) => Mathf.Abs(marker - centre) <= halfWidth;

		/// <summary>The three stirring beats of a pot: where the zone is and how fast the marker moves each time.</summary>
		public static readonly float[] StirCentres = { 0.35f, 0.65f, 0.5f };
		public static readonly float[] StirSpeeds = { 0.6f, 0.8f, 1f };
		/// <summary>The three stitches: the same idea with other places.</summary>
		public static readonly float[] StitchCentres = { 0.3f, 0.7f, 0.5f };
		public static readonly float[] StitchSpeeds = { 0.55f, 0.75f, 0.95f };
		public const float ZoneHalfWidth = 0.1f;
		/// <summary>The sweep of the single bars (heat, test, grind).</summary>
		public const float BarSpeed = 0.7f;
	}

	/// <summary>What a task came to: whether it is ready, whether it passes, its quality and the pieces it is made of.</summary>
	public sealed class TaskOutcome
	{
		public bool Complete;
		public bool Success;
		public int Quality;
		public readonly List<string> Reasons = new List<string>();
		/// <summary>What was wrong, one item per reason where it helps ("WATER" for a wrong ingredient).</summary>
		public readonly List<string> Details = new List<string>();
		public readonly List<KeyValuePair<string, int>> Parts = new List<KeyValuePair<string, int>>();
	}

	/// <summary>What the player has done so far on an attempt, as plain key and value text, so it can be saved and so the rules can read it back.</summary>
	public sealed class AttemptData
	{
		readonly Dictionary<string, string> _values = new Dictionary<string, string>();

		public IEnumerable<KeyValuePair<string, string>> All => _values;
		public int Count => _values.Count;
		public bool Has(string key) => _values.ContainsKey(key);
		public void Set(string key, string value) => _values[key] = value;
		public void Remove(string key) => _values.Remove(key);
		public void Clear() => _values.Clear();
		public string Get(string key) => _values.TryGetValue(key, out var v) ? v : null;
		public void Set(string key, float value) => _values[key] = value.ToString("0.####", CultureInfo.InvariantCulture);
		public void Set(string key, int value) => _values[key] = value.ToString(CultureInfo.InvariantCulture);
		public void Set(string key, Vector2 value) => _values[key] = value.x.ToString("0.####", CultureInfo.InvariantCulture) + "," + value.y.ToString("0.####", CultureInfo.InvariantCulture);

		public float Float(string key, float fallback = 0f) => _values.TryGetValue(key, out var v) && float.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out var f) ? f : fallback;
		public int Int(string key, int fallback = 0) => _values.TryGetValue(key, out var v) && int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i) ? i : fallback;

		public Vector2 Vector(string key)
		{
			if (!_values.TryGetValue(key, out var v)) return Vector2.zero;
			var parts = v.Split(',');
			return parts.Length == 2 && float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var x) && float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var y) ? new Vector2(x, y) : Vector2.zero;
		}

		public string[] List(string key) => _values.TryGetValue(key, out var v) && v.Length > 0 ? v.Split(',') : new string[0];
		public void SetList(string key, IEnumerable<string> items) => _values[key] = string.Join(",", items);
	}

	/// <summary>
	/// The pure scoring of every profession: from what the player did and the authored commission to a pass or a rejection and a quality.
	/// The checks come first; the upgrade bonuses are added only to a task that has passed them.
	/// </summary>
	public static class TaskScoring
	{
		static int Clamp100(float value) => Mathf.Clamp(Mathf.RoundToInt(value), 0, 100);
		static int Capped(int value) => Mathf.Clamp(value, 0, 100);

		public static int HeatScore(float locked, float target) => Clamp100(100f * (1f - Mathf.Abs(locked - target) / 0.40f));
		public static int StrikeScore(Vector2 pointer, Vector2 centre) => Clamp100(100f * (1f - Mathf.Clamp(Vector2.Distance(pointer, centre), 0f, 1f) / 0.20f));
		public static int TestScore(float locked, float target) => Clamp100(100f * (1f - Mathf.Abs(locked - target) / 0.45f));
		public static int CookHeatScore(float locked, float target) => Clamp100(100f * (1f - Mathf.Abs(locked - target) / 0.45f));
		public static int GrindScore(float locked, float target) => Clamp100(100f * (1f - Mathf.Abs(locked - target) / 0.45f));
		public static int StirScore(int hits) => Clamp100(100f * hits / 3f);

		/// <summary>The tracing score with the guide upgrade, capped at 100.</summary>
		public static int WithGuide(int baseScore, bool guide) => Capped(baseScore + (guide ? MasterConfig.GuideBonus : 0));

		public static TaskOutcome Evaluate(JobDef job, AttemptData data, ICollection<string> upgrades)
		{
			var guide = upgrades.Contains(Ids.Guide);
			var bench = upgrades.Contains(Ids.Bench);
			switch (job.Profession)
			{
				case Ids.Scribe: return Scribe(job, data, guide);
				case Ids.Forge: return Forge(job, data, bench);
				case Ids.Workshop: return Workshop(job, data, bench);
				case Ids.Cooking: return Cooking(job, data, bench);
				case Ids.Tailor: return Tailor(job, data, guide);
				case Ids.Apothecary: return Apothecary(job, data, bench);
				default: return new TaskOutcome();
			}
		}

		static TaskOutcome Reject(TaskOutcome o, string reason, string detail = null)
		{
			o.Success = false;
			o.Quality = 0;
			o.Reasons.Add(reason);
			if (detail != null) o.Details.Add(detail);
			return o;
		}

		// ---- scribe ----

		static TaskOutcome Scribe(JobDef job, AttemptData d, bool guide)
		{
			var o = new TaskOutcome { Complete = d.Has("choice") && d.Has("trace") && d.Has("seal") && d.Has("sealpos") };
			if (!o.Complete) return o;
			var trace = WithGuide(d.Int("trace"), guide);
			o.Parts.Add(new KeyValuePair<string, int>("trace", trace));
			var inspectOk = d.Get("choice") == job.Answer;
			var sealOk = d.Get("seal") == job.SealId && Vector2.Distance(d.Vector("sealpos"), job.SealTarget) <= MasterConfig.SealTolerance;
			if (trace < MasterConfig.ScribeMinTrace) return Reject(o, "why.trace");
			if (!inspectOk) Reject(o, "why.inspect");
			if (!sealOk) Reject(o, "why.seal");
			if (!inspectOk || !sealOk) return o;
			o.Success = true;
			o.Quality = Capped(40 + Mathf.RoundToInt(30f * trace / 100f) + 30);
			return o;
		}

		// ---- forge ----

		static TaskOutcome Forge(JobDef job, AttemptData d, bool bench)
		{
			var o = new TaskOutcome { Complete = d.Has("heat") && d.Has("h0") && d.Has("h1") && d.Has("h2") && d.Has("finish") };
			if (!o.Complete) return o;
			var heat = HeatScore(d.Float("heat"), job.Target);
			var strikes = new int[3];
			for (var i = 0; i < 3; i++) strikes[i] = StrikeScore(d.Vector("h" + i), job.Hammer[i]);
			var hammer = Mathf.RoundToInt((float)strikes.Average());
			o.Parts.Add(new KeyValuePair<string, int>("heat", heat));
			o.Parts.Add(new KeyValuePair<string, int>("hammer", hammer));
			if (d.Get("finish") != job.Choice) return Reject(o, "why.finish");
			var quality = Mathf.RoundToInt(0.40f * heat + 0.60f * hammer);
			if (quality < MasterConfig.SuccessQuality) return Reject(o, "why.low");
			o.Success = true;
			o.Quality = Capped(quality + (bench ? MasterConfig.BenchBonus : 0));
			return o;
		}

		// ---- workshop ----

		static TaskOutcome Workshop(JobDef job, AttemptData d, bool bench)
		{
			var o = new TaskOutcome { Complete = d.Has("s0") && d.Has("s1") && d.Has("s2") && d.Has("test") };
			if (!o.Complete) return o;
			for (var i = 0; i < 3; i++)
				if (d.Get("s" + i) != job.Correct[i]) return Reject(o, "why.parts", d.Get("s" + i));
			var test = TestScore(d.Float("test"), job.Target);
			o.Parts.Add(new KeyValuePair<string, int>("test", test));
			o.Success = true;
			o.Quality = Capped(Mathf.RoundToInt(70f + 0.30f * test) + (bench ? MasterConfig.BenchBonus : 0));
			return o;
		}

		// ---- cooking ----

		/// <summary>True when the chosen items are exactly the right set, whatever the order they were picked in.</summary>
		public static bool SameSet(IEnumerable<string> chosen, IEnumerable<string> correct) => new HashSet<string>(chosen).SetEquals(correct) && chosen.Count() == correct.Count();

		static TaskOutcome Cooking(JobDef job, AttemptData d, bool bench)
		{
			var chosen = d.List("ing");
			var o = new TaskOutcome { Complete = chosen.Length == 3 && d.Has("heat") && d.Has("stir") };
			if (!o.Complete) return o;
			if (!SameSet(chosen, job.Correct)) return Reject(o, "why.set", chosen.FirstOrDefault(c => !job.Correct.Contains(c)));
			var heat = CookHeatScore(d.Float("heat"), job.Target);
			var stir = StirScore(d.Int("stir"));
			o.Parts.Add(new KeyValuePair<string, int>("heat", heat));
			o.Parts.Add(new KeyValuePair<string, int>("stir", stir));
			o.Success = true;
			o.Quality = Capped(Mathf.RoundToInt(60f + 0.20f * heat + 0.20f * stir) + (bench ? MasterConfig.BenchBonus : 0));
			return o;
		}

		// ---- tailor ----

		static TaskOutcome Tailor(JobDef job, AttemptData d, bool guide)
		{
			var o = new TaskOutcome { Complete = d.Has("fabric") && d.Has("cut") && d.Has("stitch") };
			if (!o.Complete) return o;
			var cut = WithGuide(d.Int("cut"), guide);
			var stitches = StirScore(d.Int("stitch"));
			o.Parts.Add(new KeyValuePair<string, int>("cut", cut));
			o.Parts.Add(new KeyValuePair<string, int>("stitch", stitches));
			if (d.Get("fabric") != job.Choice) return Reject(o, "why.fabric");
			if (cut < MasterConfig.TailorMinCut) return Reject(o, "why.cut");
			var quality = Mathf.RoundToInt(0.60f * cut + 0.40f * stitches);
			if (quality < MasterConfig.SuccessQuality) return Reject(o, "why.low");
			o.Success = true;
			o.Quality = Capped(quality);
			return o;
		}

		// ---- apothecary ----

		static TaskOutcome Apothecary(JobDef job, AttemptData d, bool bench)
		{
			var chosen = d.List("herbs");
			var o = new TaskOutcome { Complete = chosen.Length == 2 && d.Has("grind") && d.Has("label") };
			if (!o.Complete) return o;
			var herbsOk = SameSet(chosen, job.Correct);
			var labelOk = d.Get("label") == job.Choice;
			var grind = GrindScore(d.Float("grind"), job.Target);
			o.Parts.Add(new KeyValuePair<string, int>("grind", grind));
			if (!herbsOk) Reject(o, "why.herbs", chosen.FirstOrDefault(c => !job.Correct.Contains(c)));
			if (!labelOk) Reject(o, "why.label");
			if (!herbsOk || !labelOk) return o;
			o.Success = true;
			o.Quality = Capped(Mathf.RoundToInt(60f + 0.40f * grind) + (bench ? MasterConfig.BenchBonus : 0));
			return o;
		}
	}
}
