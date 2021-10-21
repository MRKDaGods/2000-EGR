using UnityEngine;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;

namespace SpaceGraphicsToolkit
{
	/// <summary>This component allows you to render the <b>SgtTerrain</b> component using the <b>SGT Planet</b> shader.</summary>
	[ExecuteInEditMode]
	[DefaultExecutionOrder(200)]
	[RequireComponent(typeof(SgtTerrain))]
	public class SgtTerrainPlanetMaterial : MonoBehaviour
	{
		/// <summary>The planet material that will be rendered.</summary>
		public Material Material { set { material = value; } get { return material; } } [SerializeField] private Material material;

		/// <summary>Normals bend incorrectly on high detail planets, so it's a good idea to fade them out. This allows you to set the camera distance at which the normals begin to fade out in local space.</summary>
		public double NormalFadeRange { set { normalFadeRange = value; } get { return normalFadeRange; } } [SerializeField] private double normalFadeRange;

		/// <summary>The current water level.
		/// 0 = Radius.
		/// 1 = Radius + Displacement.</summary>
		public float WaterLevel { set { if (waterLevel != value) { waterLevel = value; MarkAsDirty(); } } get { return waterLevel; } } [Range(-2.0f, 2.0f)] [SerializeField] private float waterLevel;

		/// <summary>Should the terrain heights be clamped if they go below the water level?</summary>
		public bool ClampHeights { set { clampHeights = value; } get { return clampHeights; } } [SerializeField] private bool clampHeights;

		/// <summary>This allows you to eliminate visual errors where water intersects with land when using a heightmap.</summary>
		public double HeightmapBias { set { if (heightmapBias != value) { heightmapBias = value; MarkAsDirty(); } } get { return heightmapBias; } } [SerializeField] [Range(0.0f, 1.0f)] private double heightmapBias = 0.125;

		//public float CameraOffset { set { cameraOffset = value; } get { return cameraOffset; } } [SerializeField] private float cameraOffset;

		private SgtTerrain cachedTerrain;

		private float bumpScale;

		public void MarkAsDirty()
		{
			if (cachedTerrain != null)
			{
				cachedTerrain.MarkAsDirty();
			}
		}

		protected virtual void OnEnable()
		{
			cachedTerrain = GetComponent<SgtTerrain>();

			cachedTerrain.OnDrawQuad += HandleDrawQuad;

			cachedTerrain.OnScheduleHeights         += HandleScheduleHeights;
			cachedTerrain.OnScheduleCombinedHeights += HandleScheduleHeights;
		}

		protected virtual void OnDisable()
		{
			cachedTerrain.OnDrawQuad -= HandleDrawQuad;

			cachedTerrain.OnScheduleHeights         -= HandleScheduleHeights;
			cachedTerrain.OnScheduleCombinedHeights -= HandleScheduleHeights;

			MarkAsDirty();
		}

		protected virtual void Update()
		{
			if (normalFadeRange > 0.0)
			{
				var localPosition = cachedTerrain.GetObserverLocalPosition();
				var localAltitude = math.length(localPosition);
				var localHeight   = cachedTerrain.GetLocalHeight(localPosition);

				bumpScale = (float)math.saturate((localAltitude - localHeight) / normalFadeRange);
			}
			else
			{
				bumpScale = 1.0f;
			}
		}

		private void HandleDrawQuad(Camera camera, SgtTerrainQuad quad, Matrix4x4 matrix, int layer)
		{
			if (material != null)
			{
				var properties = quad.Properties;

				properties.SetFloat(SgtShader._BumpScale, bumpScale);

				properties.SetFloat(SgtShader._WaterLevel, waterLevel);

				//if (cameraOffset != 0.0f)
				//{
				//	var direction = Vector3.Normalize(camera.transform.position - transform.position);
				//
				//	matrix = Matrix4x4.Translate(direction * cameraOffset) * matrix;
				//}

				foreach (var mesh in quad.CurrentMeshes)
				{
					Graphics.DrawMesh(mesh, matrix, material, gameObject.layer, camera, 0, properties);
				}
			}
		}

		private void HandleScheduleHeights(NativeArray<double3> points, NativeArray<double> heights, ref JobHandle handle)
		{
			if (clampHeights == true)
			{
				var terrainHeightmap = GetComponent<SgtTerrainHeightmap>();

				if (terrainHeightmap != null)
				{
					var job          = new HeightsJob();
					var displacement = terrainHeightmap.Displacement;

					job.WaterLevel = cachedTerrain.Radius + displacement * waterLevel + displacement * heightmapBias;
					job.Heights    = heights;

					handle = job.Schedule(heights.Length, 32, handle);
				}
			}
		}

		[BurstCompile]
		public struct HeightsJob : IJobParallelFor
		{
			public double WaterLevel;

			public NativeArray<double> Heights;

			public void Execute(int i)
			{
				if (double.IsNegativeInfinity(Heights[i]) == false)
				{
					Heights[i] = math.max(Heights[i], WaterLevel);
				}
			}
		}
	}
}

#if UNITY_EDITOR
namespace SpaceGraphicsToolkit
{
	using UnityEditor;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(SgtTerrainPlanetMaterial))]
	public class SgtTerrainPlanetMaterial_Editor : SgtEditor<SgtTerrainPlanetMaterial>
	{
		protected override void OnInspector()
		{
			var markAsDirty = false;

			BeginError(Any(t => t.Material == null));
				Draw("material", "The planet material that will be rendered.");
			EndError();
			//Draw("cameraOffset");
			Draw("normalFadeRange", "Normals bend incorrectly on high detail planets, so it's a good idea to fade them out. This allows you to set the camera distance at which the normals begin to fade out in local space.");
			Draw("waterLevel", ref markAsDirty, "The current water level.\n\n0 = Radius.\n\n1 = Radius + Displacement.");
			Draw("clampHeights", ref markAsDirty, "Should the terrain heights be clamped if they go below the water level?");
			Draw("heightmapBias", ref markAsDirty, "This allows you to eliminate visual errors where water intersects with land when using a heightmap.");

			if (markAsDirty == true)
			{
				Each(t => t.MarkAsDirty());
			}
		}
	}
}
#endif