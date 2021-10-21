using UnityEngine;
using System.Collections.Generic;
using FSA = UnityEngine.Serialization.FormerlySerializedAsAttribute;

namespace SpaceGraphicsToolkit
{
	/// <summary>This component allows you to create a list of SgtFloatingObject prefabs that are associated with a specific Category name.
	/// This allows you to easily manage what objects get spawned from each type of spawner.</summary>
	[ExecuteInEditMode]
	[HelpURL(SgtHelper.HelpUrlPrefix + "SgtSpawnList")]
	[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Spawn List")]
	public class SgtSpawnList : SgtLinkedBehaviour<SgtSpawnList>
	{
		/// <summary>The type of prefabs these are (e.g. Planet).</summary>
		public string Category { set { category = value; } get { return category; } } [FSA("Category")] [SerializeField] private string category;

		/// <summary>The prefabs beloning to this spawn list.</summary>
		public List<SgtFloatingObject> Prefabs { get { if (prefabs == null) prefabs = new List<SgtFloatingObject>(); return prefabs; } } [FSA("Prefabs")] [SerializeField] private List<SgtFloatingObject> prefabs;

		public static SgtSpawnList Create(int layer = 0, Transform parent = null)
		{
			return Create(layer, parent, Vector3.zero, Quaternion.identity, Vector3.one);
		}

		public static SgtSpawnList Create(int layer, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
		{
			var gameObject = SgtHelper.CreateGameObject("Spawn List", layer, parent, localPosition, localRotation, localScale);
			var spawnList  = gameObject.AddComponent<SgtSpawnList>();

			return spawnList;
		}

#if UNITY_EDITOR
		[UnityEditor.MenuItem(SgtHelper.GameObjectMenuPrefix + "Spawn List", false, 10)]
		private static void CreateMenuItem()
		{
			var parent    = SgtHelper.GetSelectedParent();
			var spawnList = Create(parent != null ? parent.gameObject.layer : 0, parent);

			SgtHelper.SelectAndPing(spawnList);
		}
#endif
	}
}

#if UNITY_EDITOR
namespace SpaceGraphicsToolkit
{
	using UnityEditor;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(SgtSpawnList))]
	public class SgtSpawnList_Editor : SgtEditor<SgtSpawnList>
	{
		protected override void OnInspector()
		{
			Draw("category", "The type of prefabs these are (e.g. Planet).");
			Draw("prefabs", "The prefabs beloning to this spawn list.");
		}
	}
}
#endif