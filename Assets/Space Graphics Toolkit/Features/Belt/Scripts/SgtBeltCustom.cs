using UnityEngine;
using System.Collections.Generic;
using FSA = UnityEngine.Serialization.FormerlySerializedAsAttribute;

namespace SpaceGraphicsToolkit
{
	/// <summary>This component allows you to specify the exact position/size/etc of each asteroid in this asteroid belt.</summary>
	[ExecuteInEditMode]
	[HelpURL(SgtHelper.HelpUrlPrefix + "SgtBeltCustom")]
	[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Belt Custom")]
	public class SgtBeltCustom : SgtBelt
	{
		/// <summary>The custom asteroids in this belt.</summary>
		public List<SgtBeltAsteroid> Asteroids { get { if (asteroids == null) asteroids = new List<SgtBeltAsteroid>(); return asteroids; } } [FSA("Asteroids")] [SerializeField] private List<SgtBeltAsteroid> asteroids;

		public static SgtBeltCustom Create(int layer = 0, Transform parent = null)
		{
			return Create(layer, parent, Vector3.zero, Quaternion.identity, Vector3.one);
		}

		public static SgtBeltCustom Create(int layer, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
		{
			var gameObject = SgtHelper.CreateGameObject("Belt Custom", layer, parent, localPosition, localRotation, localScale);
			var beltCustom = gameObject.AddComponent<SgtBeltCustom>();

			return beltCustom;
		}

#if UNITY_EDITOR
		[UnityEditor.MenuItem(SgtHelper.GameObjectMenuPrefix + "Belt Custom", false, 10)]
		public static void CreateMenuItem()
		{
			var parent     = SgtHelper.GetSelectedParent();
			var beltCustom = Create(parent != null ? parent.gameObject.layer : 0, parent);

			SgtHelper.SelectAndPing(beltCustom);
		}
#endif

		protected override void OnDestroy()
		{
			base.OnDestroy();

			if (asteroids != null)
			{
				for (var i = asteroids.Count - 1; i >= 0; i--)
				{
					SgtPoolClass<SgtBeltAsteroid>.Add(asteroids[i]);
				}
			}
		}

		protected override int BeginQuads()
		{
			if (asteroids != null)
			{
				return asteroids.Count;
			}

			return 0;
		}

		protected override void NextQuad(ref SgtBeltAsteroid asteroid, int asteroidIndex)
		{
			asteroid.CopyFrom(asteroids[asteroidIndex]);
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
	[CustomEditor(typeof(SgtBeltCustom))]
	public class SgtBeltCustom_Editor : SgtBelt_Editor<SgtBeltCustom>
	{
		protected override void OnInspector()
		{
			var dirtyMaterial = false;
			var dirtyMesh     = false;

			DrawMaterial(ref dirtyMaterial);

			Separator();

			DrawMainTex(ref dirtyMaterial, ref dirtyMesh);

			Separator();

			DrawLighting(ref dirtyMaterial);

			Separator();

			Draw("Asteroids", ref dirtyMesh, "The custom asteroids in this belt.");

			RequireCamera();

			serializedObject.ApplyModifiedProperties();

			if (dirtyMaterial == true) DirtyEach(t => t.DirtyMaterial());
			if (dirtyMesh     == true) DirtyEach(t => t.DirtyMesh    ());
		}
	}
}
#endif