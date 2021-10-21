using UnityEngine;
using FSA = UnityEngine.Serialization.FormerlySerializedAsAttribute;

namespace SpaceGraphicsToolkit
{
	/// <summary>This component will automatically spawn prefabs in a ring around the attached SgtFloatingPoint.</summary>
	[HelpURL(SgtHelper.HelpUrlPrefix + "SgtFloatingSpawnerRing")]
	[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Floating Spawner Ring")]
	public class SgtFloatingSpawnerRing : SgtFloatingSpawner
	{
		/// <summary>The amount of prefabs that will be spawned.</summary>
		public int Count { set { count = value; } get { return count; } } [FSA("Count")] [SerializeField] private int count = 10;

		/// <summary>The minimum distance away the prefabs can spawn.</summary>
		public SgtLength RadiusMin { set { radiusMin = value; } get { return radiusMin; } } [FSA("RadiusMin")] [SerializeField] private SgtLength radiusMin = 200000.0;

		/// <summary>The maximum distance away the prefabs can spawn in meters.</summary>
		public SgtLength RadiusMax { set { radiusMax = value; } get { return radiusMax; } } [FSA("RadiusMax")] [SerializeField] private SgtLength radiusMax = 2000000.0;

		protected override void SpawnAll()
		{
			var parentPoint = GetComponentInParent<SgtFloatingPoint>();

			BuildSpawnList();

			SgtHelper.BeginRandomSeed(CachedObject.Seed);
			{
				var radMin = (double)radiusMin;
				var radMax = (double)radiusMax;
				var radRng = radMax - radMin;

				for (var i = 0; i < count; i++)
				{
					var position = parentPoint.Position;
					var angle    = Random.Range(-Mathf.PI, Mathf.PI);
					var offset   = transform.rotation * new Vector3(Mathf.Sin(angle), 0.0f, Mathf.Cos(angle));
					var radius   = radMin + radRng * Random.value;

					position.LocalX += offset.x * radius;
					position.LocalY += offset.y * radius;
					position.LocalZ += offset.z * radius;
					position.SnapLocal();

					SpawnAt(position, i);
				}
			}
			SgtHelper.EndRandomSeed();
		}
	}
}

#if UNITY_EDITOR
namespace SpaceGraphicsToolkit
{
	using UnityEditor;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(SgtFloatingSpawnerRing))]
	public class SgtFloatingSpawnerRing_Editor : SgtFloatingSpawner_Editor<SgtFloatingSpawnerRing>
	{
		protected override void OnInspector()
		{
			base.OnInspector();

			Separator();

			Draw("count", "The amount of prefabs that will be spawned.");
			BeginError(Any(t => t.RadiusMin <= 0.0 || t.RadiusMin > t.RadiusMax));
				Draw("radiusMin", "The minimum distance away the prefabs can spawn in meters.");
				Draw("radiusMax");
			EndError();

			if (Any(t => t.RadiusMin > t.Range || t.RadiusMax > t.Range))
			{
				EditorGUILayout.HelpBox("The spawn range should be greater than the spawn radius.", MessageType.Warning);
			}
		}
	}
}
#endif