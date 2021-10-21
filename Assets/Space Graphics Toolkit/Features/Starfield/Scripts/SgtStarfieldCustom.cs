using UnityEngine;
using System.Collections.Generic;
using FSA = UnityEngine.Serialization.FormerlySerializedAsAttribute;

namespace SpaceGraphicsToolkit
{
	/// <summary>This component allows you to specify the exact position/size/etc of each star in this starfield.</summary>
	[ExecuteInEditMode]
	[HelpURL(SgtHelper.HelpUrlPrefix + "SgtStarfieldCustom")]
	[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Starfield Custom")]
	public class SgtStarfieldCustom : SgtStarfield
	{
		/// <summary>The stars that will be rendered by this starfield.</summary>
		public List<SgtStarfieldStar> Stars { get { if (stars == null) stars = new List<SgtStarfieldStar>(); return stars; } } [FSA("Stars")] [SerializeField] private List<SgtStarfieldStar> stars;

		public static SgtStarfieldCustom Create(int layer = 0, Transform parent = null)
		{
			return Create(layer, parent, Vector3.zero, Quaternion.identity, Vector3.one);
		}

		public static SgtStarfieldCustom Create(int layer, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
		{
			var gameObject      = SgtHelper.CreateGameObject("Starfield Custom", layer, parent, localPosition, localRotation, localScale);
			var starfieldCustom = gameObject.AddComponent<SgtStarfieldCustom>();

			return starfieldCustom;
		}

#if UNITY_EDITOR
		[UnityEditor.MenuItem(SgtHelper.GameObjectMenuPrefix + "Starfield/Custom", false, 10)]
		private static void CreateMenuItem()
		{
			var parent          = SgtHelper.GetSelectedParent();
			var starfieldCustom = Create(parent != null ? parent.gameObject.layer : 0, parent);

			SgtHelper.SelectAndPing(starfieldCustom);
		}
#endif

		protected override void OnDestroy()
		{
			base.OnDestroy();

			if (stars != null)
			{
				for (var i = stars.Count - 1; i >= 0; i--)
				{
					SgtPoolClass<SgtStarfieldStar>.Add(stars[i]);
				}
			}
		}

		protected override int BeginQuads()
		{
			if (stars != null)
			{
				return stars.Count;
			}

			return 0;
		}

		protected override void NextQuad(ref SgtStarfieldStar quad, int starIndex)
		{
			quad.CopyFrom(stars[starIndex]);
		}

		protected override void EndQuads()
		{
		}
	}
}

#if UNITY_EDITOR
namespace SpaceGraphicsToolkit
{
	using UnityEditor;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(SgtStarfieldCustom))]
	public class SgtStarfieldCustom_Editor : SgtStarfield_Editor<SgtStarfieldCustom>
	{
		protected override void OnInspector()
		{
			var dirtyMaterial = false;
			var dirtyMesh     = false;

			DrawMaterial(ref dirtyMaterial);

			Separator();

			DrawMainTex(ref dirtyMaterial, ref dirtyMesh);

			Separator();

			DrawPointMaterial(ref dirtyMaterial);

			Separator();

			Draw("stars", ref dirtyMesh, "The stars that will be rendered by this starfield.");

			RequireCamera();

			serializedObject.ApplyModifiedProperties();

			if (dirtyMaterial == true) DirtyEach(t => t.DirtyMaterial());
			if (dirtyMesh     == true) DirtyEach(t => t.DirtyMesh    ());
		}
	}
}
#endif