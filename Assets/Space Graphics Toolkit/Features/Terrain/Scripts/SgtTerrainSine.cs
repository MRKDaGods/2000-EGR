using UnityEngine;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;

namespace SpaceGraphicsToolkit
{
	/// <summary></summary>
	[ExecuteInEditMode]
	[RequireComponent(typeof(SgtTerrain))]
	public class SgtTerrainSine : MonoBehaviour
	{
		/// <summary>This allows you to control where this biome appears based on the <b>SgtTerrainTerra</b> component's <b>Biomes</b> splatmap.</summary>
		public int Area { set { area = value; MarkAsDirty(); } get { return area; } } [SerializeField] private int area;

		/// <summary>The amount of peaks and valleys across the mesh.</summary>
		public double Frequency { set { frequency = value; MarkAsDirty(); } get { return frequency; } } [SerializeField] private double frequency = 1000.0;

		/// <summary>The maximum +- displacement of the first octave.
		/// NOTE: The final displacement may be greater than this range when using multiple octaves.</summary>
		public double Amplitude { set { amplitude = value; MarkAsDirty(); } get { return amplitude; } } [SerializeField] private double amplitude = 100.0;

		/// <summary>The amount of noise layers.</summary>
		public int Octaves { set { octaves = value; MarkAsDirty(); } get { return octaves; } } [SerializeField] [Range(1, 20)] private int octaves = 8;

		private SgtTerrain cachedTerrain;

		private NativeArray<float> tempWeights;

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

			cachedTerrain.OnScheduleHeights         += HandleScheduleHeights;
			cachedTerrain.OnScheduleCombinedHeights += HandleScheduleHeights;

			cachedTerrain.MarkAsDirty();

			tempWeights = new NativeArray<float>(0, Allocator.Persistent);
		}

		protected virtual void OnDisable()
		{
			cachedTerrain.OnScheduleHeights         -= HandleScheduleHeights;
			cachedTerrain.OnScheduleCombinedHeights -= HandleScheduleHeights;

			cachedTerrain.MarkAsDirty();

			// TODO: Make sure this executes after the job finishes
			tempWeights.Dispose();
		}

		private void HandleScheduleHeights(NativeArray<double3> points, NativeArray<double> heights, ref JobHandle handle)
		{
			var job = new HeightsJob();

			job.Frequency = frequency;
			job.Amplitude = amplitude;
			job.Octaves   = octaves;
			job.Points    = points;
			job.Heights   = heights;

			if (cachedTerrain.Areas != null && cachedTerrain.Areas.SplatCount > 0)
			{
				job.Area           = math.clamp(area, 0, cachedTerrain.Areas.SplatCount - 1);
				job.AreaSize       = cachedTerrain.Areas.Size;
				job.AreaSplatCount = cachedTerrain.Areas.SplatCount;
				job.AreaWeights    = cachedTerrain.Areas.Weights;
			}
			else
			{
				job.Area           = 0;
				job.AreaSize       = int2.zero;
				job.AreaSplatCount = 0;
				job.AreaWeights    = tempWeights;
			}

			handle = job.Schedule(heights.Length, 32, handle);
		}

		[BurstCompile]
		public struct HeightsJob : IJobParallelFor
		{
			public double Frequency;
			public double Amplitude;
			public int    Octaves;
			public int2   Size;

			public NativeArray<double3> Points;
			public NativeArray<double>  Heights;

			[ReadOnly] public int                Area;
			[ReadOnly] public int2               AreaSize;
			[ReadOnly] public int                AreaSplatCount;
			[ReadOnly] public NativeArray<float> AreaWeights;

			public void Execute(int i)
			{
				if (double.IsNegativeInfinity(Heights[i]) == false)
				{
					var weight = 1.0f;

					if (AreaWeights.Length > 0)
					{
						weight = SgtTerrainTopology.Sample_Cubic_Equirectangular(AreaWeights, AreaSplatCount, Area, AreaSize, Points[i]);
						weight = math.clamp(20000.0f - weight, 0.0f, 20000.0f) / 20000.0f;
					}

					if (weight > 0.0f)
					{
						var contribution = FBM(Points[i] * Frequency, Amplitude);

						Heights[i] += contribution * weight;
					}
				}
			}

			private double FBM(double3 p, double str)
			{
				var height = 0.0;
		
				for (var i = 0; i < Octaves; i++)
				{
					height += math.sin((float)p.x) * math.sin((float)p.y) * math.sin((float)p.z) * str;

					str *= 0.5f;
					p   *= 2.0f;
				}

				return height;
			}
		}
	}
}

#if UNITY_EDITOR
namespace SpaceGraphicsToolkit
{
	using UnityEditor;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(SgtTerrainSine))]
	public class SgtWorldSine_Editor : SgtEditor<SgtTerrainSine>
	{
		protected override void OnInspector()
		{
			var markAsDirty = false;

			markAsDirty |= SgtTerrain_Editor<SgtTerrain>.DrawArea(serializedObject.FindProperty("area"), Target.GetComponent<SgtTerrain>());

			EditorGUILayout.Separator();

			BeginError(Any(t => t.Frequency == 0.0));
				Draw("frequency", ref markAsDirty, "The amount of peaks and valleys across the mesh.");
			EndError();
			BeginError(Any(t => t.Amplitude == 0.0));
				Draw("amplitude", ref markAsDirty, "The maximum +- displacement of the first octave.\n\nNOTE: The final displacement may be greater than this range when using multiple octaves.");
			EndError();
			Draw("octaves", ref markAsDirty, "The amount of noise layers.");

			if (markAsDirty == true)
			{
				Each(t => t.MarkAsDirty());
			}
		}
	}
}
#endif