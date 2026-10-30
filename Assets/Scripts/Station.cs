using System.Collections.Generic;
using UnityEngine;

namespace Temnorid.Master
{
	/// <summary>A workstation in the hub: it has the profession's id and glows when the pointer is over it. What it opens is decided by <see cref="MasterGame"/>.</summary>
	public sealed class Station : MonoBehaviour
	{
		public static readonly List<Station> All = new List<Station>();

		public string Id;
		public Renderer[] Renderers;

		bool _hover;
		Vector3 _scale;
		MaterialPropertyBlock _block;

		void Awake()
		{
			_scale = transform.localScale;
			_block = new MaterialPropertyBlock();
		}

		void OnEnable() => All.Add(this);
		void OnDisable() => All.Remove(this);

		public bool Hover
		{
			get => _hover;
			set
			{
				if (_hover == value) return;
				_hover = value;
				transform.localScale = value ? _scale * 1.03f : _scale;
				if (Renderers == null) return;
				foreach (var r in Renderers)
				{
					if (r == null) continue;
					r.GetPropertyBlock(_block);
					var baseColour = r.sharedMaterial != null && r.sharedMaterial.HasProperty("_BaseColor") ? r.sharedMaterial.GetColor("_BaseColor") : Color.white;
					_block.SetColor("_BaseColor", value ? Color.Lerp(baseColour, new Color(1f, 0.85f, 0.4f), 0.4f) : baseColour);
					r.SetPropertyBlock(_block);
				}
			}
		}
	}
}
