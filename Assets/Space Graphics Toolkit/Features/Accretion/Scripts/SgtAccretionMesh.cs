using UnityEngine;
using FSA = UnityEngine.Serialization.FormerlySerializedAsAttribute;

namespace SpaceGraphicsToolkit
{
	/// <summary>This component allows you to generate the SgtAccretion.Mesh field.</summary>
	[ExecuteInEditMode]
	[RequireComponent(typeof(SgtAccretion))]
	[HelpURL(SgtHelper.HelpUrlPrefix + "SgtAccretionMesh")]
	[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Accretion Mesh")]
	public class SgtAccretionMesh : MonoBehaviour
	{
		/// <summary>The amount of segments the final disc will be comprised of.</summary>
		public int Segments { set { if (segments != value) { segments = value; DirtyMesh(); } } get { return segments; } } [FSA("Segments")] [SerializeField] private int segments = 8;

		/// <summary>The amount of triangle edges along the inner and outer edges of each segment.</summary>
		public int SegmentDetail { set { if (segmentDetail != value) { segmentDetail = value; DirtyMesh(); } } get { return segmentDetail; } } [FSA("SegmentDetail")] [SerializeField] private int segmentDetail = 50;

		/// <summary>The amount of times the main texture is tiled around the ring segment.</summary>
		public int SegmentTiling { set { if (segmentTiling != value) { segmentTiling = value; DirtyMesh(); } } get { return segmentTiling; } } [FSA("SegmentTiling")] [SerializeField] private int segmentTiling = 1;

		/// <summary>The radius of the inner edge in local space.</summary>
		public float RadiusMin { set { if (radiusMin != value) { radiusMin = value; DirtyMesh(); } } get { return radiusMin; } } [FSA("RadiusMin")] [SerializeField] private float radiusMin = 1.0f;

		/// <summary>The radius of the outer edge in local space.</summary>
		public float RadiusMax { set { if (radiusMax != value) { radiusMax = value; DirtyMesh(); } } get { return radiusMax; } } [FSA("RadiusMax")] [SerializeField] private float radiusMax = 2.0f;

		/// <summary>The amount of edge loops around the generated disc. If you have a very large ring then you can end up with very skinny triangles, so increasing this can give them a better shape.</summary>
		public int RadiusDetail { set { if (radiusDetail != value) { radiusDetail = value; DirtyMesh(); } } get { return radiusDetail; } } [FSA("RadiusDetail")] [SerializeField] private int radiusDetail = 1;

		/// <summary>The amount the mesh bounds should get pushed out by in local space. This should be used with 8+ Segments.</summary>
		public float BoundsShift { set { if (boundsShift != value) { boundsShift = value; DirtyMesh(); } } get { return boundsShift; } } [FSA("BoundsShift")] [SerializeField] private float boundsShift;

		[System.NonSerialized]
		private Mesh generatedMesh;

		[System.NonSerialized]
		private SgtAccretion cachedAccretion;

		[System.NonSerialized]
		private bool cachedAccretionSet;

		public SgtAccretion CachedAccretion
		{
			get
			{
				if (cachedAccretionSet == false)
				{
					cachedAccretion    = GetComponent<SgtAccretion>();
					cachedAccretionSet = true;
				}

				return cachedAccretion;
			}
		}

		public void DirtyMesh()
		{
			UpdateMesh();
		}

#if UNITY_EDITOR
		/// <summary>This method allows you to export the generated mesh as an asset.
		/// Once done, you can remove this component, and set the <b>SgtAccretion</b> component's <b>Mesh</b> setting using the exported asset.</summary>
		[ContextMenu("Export Mesh")]
		public void ExportMesh()
		{
			UpdateMesh();

			if (generatedMesh != null)
			{
				SgtHelper.ExportAssetDialog(generatedMesh, "Accretion Mesh");
			}
		}
#endif

		[ContextMenu("Apply Mesh")]
		public void ApplyMesh()
		{
			CachedAccretion.Mesh = generatedMesh;
		}

		[ContextMenu("Remove Mesh")]
		public void RemoveMesh()
		{
			if (CachedAccretion.Mesh == generatedMesh)
			{
				CachedAccretion.Mesh = null;
			}
		}

		protected virtual void OnEnable()
		{
			UpdateMesh();
		}

		protected virtual void OnDestroy()
		{
			if (generatedMesh != null)
			{
				generatedMesh.Clear(false);

				SgtObjectPool<Mesh>.Add(generatedMesh);
			}
		}

		protected virtual void OnDidApplyAnimationProperties()
		{
			DirtyMesh();
		}

#if UNITY_EDITOR
		protected virtual void OnDrawGizmosSelected()
		{
			Gizmos.matrix = transform.localToWorldMatrix;

			SgtHelper.DrawCircle(Vector3.zero, Vector3.up, radiusMin);
			SgtHelper.DrawCircle(Vector3.zero, Vector3.up, radiusMax);
		}
#endif

		private void UpdateMesh()
		{
			if (segments > 0 && segmentDetail > 0 && radiusDetail > 0)
			{
				if (generatedMesh == null)
				{
					generatedMesh = SgtHelper.CreateTempMesh("Accretion Mesh (Generated)");
				}

				var slices     = segmentDetail + 1;
				var rings      = radiusDetail + 1;
				var total      = slices * rings * 2;
				var positions  = new Vector3[total];
				var coords1    = new Vector2[total];
				var coords2    = new Vector2[total];
				var colors     = new Color[total];
				var indices    = new int[segmentDetail * radiusDetail * 6];
				var yawStep    = (Mathf.PI * 2.0f) / segments / segmentDetail;
				var sliceStep  = 1.0f / segmentDetail;
				var ringStep   = 1.0f / radiusDetail;

				for (var slice = 0; slice < slices; slice++)
				{
					var a = yawStep * slice;
					var x = Mathf.Sin(a);
					var z = Mathf.Cos(a);

					for (var ring = 0; ring < rings; ring++)
					{
						var v       = rings * slice + ring;
						var slice01 = sliceStep * slice;
						var ring01  = ringStep * ring;
						var radius  = Mathf.Lerp(radiusMin, radiusMax, ring01);

						positions[v] = new Vector3(x * radius, 0.0f, z * radius);
						colors[v] = new Color(1.0f, 1.0f, 1.0f, 0.0f);
						coords1[v] = new Vector2(ring01, slice01);
						coords2[v] = new Vector2(radius, slice01 * radius * segmentTiling);
					}
				}

				for (var slice = 0; slice < segmentDetail; slice++)
				{
					for (var ring = 0; ring < radiusDetail; ring++)
					{
						var i  = (slice * radiusDetail + ring) * 6;
						var v0 = slice * rings + ring;
						var v1 = v0 + rings;

						indices[i + 0] = v0 + 0;
						indices[i + 1] = v0 + 1;
						indices[i + 2] = v1 + 0;
						indices[i + 3] = v1 + 1;
						indices[i + 4] = v1 + 0;
						indices[i + 5] = v0 + 1;
					}
				}

				generatedMesh.Clear(false);
				generatedMesh.vertices  = positions;
				generatedMesh.colors    = colors;
				generatedMesh.uv        = coords1;
				generatedMesh.uv2       = coords2;
				generatedMesh.triangles = indices;
				generatedMesh.RecalculateNormals();
				generatedMesh.RecalculateBounds();

				var bounds = generatedMesh.bounds;

				generatedMesh.bounds = SgtHelper.NewBoundsCenter(bounds, bounds.center + bounds.center.normalized * boundsShift);
			}

			ApplyMesh();
		}
	}
}

#if UNITY_EDITOR
namespace SpaceGraphicsToolkit
{
	[UnityEditor.CanEditMultipleObjects]
	[UnityEditor.CustomEditor(typeof(SgtAccretionMesh))]
	public class SgtAccretionMesh_Editor : SgtEditor<SgtAccretionMesh>
	{
		protected override void OnInspector()
		{
			var dirtyMesh = false;

			BeginError(Any(t => t.Segments < 1));
				Draw("segments", ref dirtyMesh, "The amount of segments the final disc will be comprised of.");
			EndError();
			BeginError(Any(t => t.SegmentDetail < 1));
				Draw("segmentDetail", ref dirtyMesh, "The amount of triangle edges along the inner and outer edges of each segment.");
			EndError();
			BeginError(Any(t => t.SegmentTiling < 1));
				Draw("segmentTiling", ref dirtyMesh, "The amount of times the main texture is tiled around the ring segment.");
			EndError();

			Separator();

			BeginError(Any(t => t.RadiusMin == t.RadiusMax));
				Draw("radiusMin", ref dirtyMesh, "The radius of the inner edge in local space.");
				Draw("radiusMax", ref dirtyMesh, "The radius of the outer edge in local space.");
			EndError();
			BeginError(Any(t => t.RadiusDetail < 1));
				Draw("radiusDetail", ref dirtyMesh, "The amount of edge loops around the generated disc. If you have a very large ring then you can end up with very skinny triangles, so increasing this can give them a better shape.");
			EndError();

			Separator();

			Draw("boundsShift", ref dirtyMesh, "The amount the mesh bounds should get pushed out by in local space. This should be used with 8+ Segments.");

			if (dirtyMesh == true)
			{
				DirtyEach(t => t.DirtyMesh());
			}
		}
	}
}
#endif