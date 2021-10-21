using UnityEngine;
using FSA = UnityEngine.Serialization.FormerlySerializedAsAttribute;

namespace SpaceGraphicsToolkit
{
	/// <summary>This component will automatically spawn prefabs in orbit around the attached SgtFloatingPoint.</summary>
	[HelpURL(SgtHelper.HelpUrlPrefix + "SgtFloatingSpawnerOrbit")]
	[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Floating Spawner Orbit")]
	public class SgtFloatingSpawnerOrbit : SgtFloatingSpawner
	{
		/// <summary>The amount of prefabs that will be spawned.</summary>
		public int Count { set { count = value; } get { return count; } } [FSA("Count")] [SerializeField] private int count = 10;

		/// <summary>The maximum degrees an orbit can tilt.</summary>
		public float TiltMax { set { tiltMax = value; } get { return tiltMax; } } [FSA("TiltMax")] [SerializeField] [Range(0.0f, 180.0f)] private float tiltMax = 10.0f;

		/// <summary>The maximum amount an orbit can be squashed.</summary>
		public float OblatenessMax { set { oblatenessMax = value; } get { return oblatenessMax; } } [FSA("OblatenessMax")] [SerializeField] [Range(0.0f, 1.0f)] private float oblatenessMax;

		/// <summary>The minimum distance away the prefabs can spawn.</summary>
		public SgtLength RadiusMin { set { radiusMin = value; } get { return radiusMin; } } [FSA("RadiusMin")] [SerializeField] private SgtLength radiusMin = 200000.0;

		/// <summary>The maximum distance away the prefabs can spawn in meters.</summary>
		public SgtLength RadiusMax { set { radiusMax = value; } get { return radiusMax; } } [FSA("RadiusMax")] [SerializeField] private SgtLength radiusMax = 2000000.0;

		protected override void SpawnAll()
		{
			var parentPoint = CachedObject;

			BuildSpawnList();

			SgtHelper.BeginRandomSeed(CachedObject.Seed);
			{
				var radMin = (double)radiusMin;
				var radMax = (double)radiusMax;
				var radRng = radMax - radMin;

				for (var i = 0; i < count; i++)
				{
					var radius     = radMin + radRng * Random.value;
					var angle      = Random.Range(0.0f, 360.0f);
					var tilt       = new Vector3(Random.Range(-tiltMax, tiltMax), 0.0f, Random.Range(-tiltMax, tiltMax));
					var oblateness = Random.Range(0.0f, oblatenessMax);
					var position   = SgtFloatingOrbit.CalculatePosition(parentPoint, radius, angle, tilt, oblateness);
					var instance   = SpawnAt(position, i);
					var orbit      = instance.GetComponent<SgtFloatingOrbit>();

					if (orbit == null)
					{
						orbit = instance.gameObject.AddComponent<SgtFloatingOrbit>();
					}

					orbit.ParentPoint      = parentPoint; 
					orbit.Radius           = radius;
					orbit.Angle            = angle;
					orbit.Oblateness       = oblateness;
					orbit.DegreesPerSecond = 1000.0 / radius;
					orbit.Tilt             = tilt;
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
	[CustomEditor(typeof(SgtFloatingSpawnerOrbit))]
	public class SgtFloatingSpawnerOrbit_Editor : SgtFloatingSpawner_Editor<SgtFloatingSpawnerOrbit>
	{
		protected override void OnInspector()
		{
			base.OnInspector();

			Separator();

			Draw("count", "The amount of prefabs that will be spawned.");
			Draw("tiltMax", "The maximum degrees an orbit can tilt.");
			Draw("oblatenessMax", "The maximum amount an orbit can be squashed.");
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