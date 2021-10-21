using UnityEngine;
using FSA = UnityEngine.Serialization.FormerlySerializedAsAttribute;

namespace SpaceGraphicsToolkit
{
	/// <summary>This component allows you to generate the SgtRing.Mesh field.</summary>
	[ExecuteInEditMode]
	[RequireComponent(typeof(SgtRing))]
	[HelpURL(SgtHelper.HelpUrlPrefix + "SgtRingMesh")]
	[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Ring Mesh")]
	public class SgtRingMesh : MonoBehaviour
	{
		/// <summary>The amount of segments the final ring will be comprised of.</summary>
		public int Segments { set { if (segments != value) { segments = value; UpdateMesh(); } } get { return segments; } } [FSA("Segments")] [SerializeField] private int segments = 8;

		/// <summary>The amount of triangle edges along the inner and outer edges of each segment.</summary>
		public int SegmentDetail { set { if (segmentDetail != value) { segmentDetail = value; UpdateMesh(); } } get { return segmentDetail; } } [FSA("SegmentDetail")] [SerializeField] private int segmentDetail = 50;

		/// <summary>The radius of the inner edge in local space.</summary>
		public float RadiusMin { set { if (radiusMin != value) { radiusMin = value; UpdateMesh(); } } get { return radiusMin; } } [FSA("RadiusMin")] [SerializeField] private float radiusMin = 1.0f;

		/// <summary>The radius of the outer edge in local space.</summary>
		public float RadiusMax { set { if (radiusMax != value) { radiusMax = value; UpdateMesh(); } } get { return radiusMax; } } [FSA("RadiusMax")] [SerializeField] private float radiusMax = 2.0f;

		/// <summary>The amount of edge loops around the generated ring. If you have a very large ring then you can end up with very skinny triangles, so increasing this can give them a better shape.</summary>
		public int RadiusDetail { set { if (radiusDetail != value) { radiusDetail = value; UpdateMesh(); } } get { return radiusDetail; } } [FSA("RadiusDetail")] [SerializeField] private int radiusDetail = 1;

		/// <summary>The amount the mesh bounds should get pushed out by in local space. This should be used with 8+ Segments.</summary>
		public float BoundsShift { set { if (boundsShift != value) { boundsShift = value; UpdateMesh(); } } get { return boundsShift; } } [FSA("BoundsShift")] [SerializeField] private float boundsShift;

		/// <summary>If you want these values to control the shadow RadiusMin/Max, then set this here.</summary>
		public SgtShadowRing Shadow { set { if (shadow != value) { shadow = value; UpdateMesh(); } } get { return shadow; } } [FSA("Shadow")] [SerializeField] private SgtShadowRing shadow;

		[System.NonSerialized]
		private Mesh generatedMesh;

		[System.NonSerialized]
		private SgtRing cachedRing;

		[System.NonSerialized]
		private bool cachedRingSet;

		public SgtRing CachedRing
		{
			get
			{
				if (cachedRingSet == false)
				{
					cachedRing    = GetComponent<SgtRing>();
					cachedRingSet = true;
				}

				return cachedRing;
			}
		}

		public Mesh GeneratedMesh
		{
			get
			{
				return generatedMesh;
			}
		}

#if UNITY_EDITOR
		/// <summary>This method allows you to export the generated mesh as an asset.
		/// Once done, you can remove this component, and set the <b>SgtRing</b> component's <b>Mesh</b> setting using the exported asset.</summary>
		[ContextMenu("Export Mesh")]
		public void ExportOuterTexture()
		{
			if (generatedMesh != null)
			{
				SgtHelper.ExportAssetDialog(generatedMesh, "Ring Mesh");
			}
		}
#endif

		[ContextMenu("Apply Mesh")]
		public void ApplyMesh()
		{
			CachedRing.Mesh = generatedMesh;
		}

		[ContextMenu("Remove Mesh")]
		public void RemoveMesh()
		{
			if (CachedRing.Mesh == generatedMesh)
			{
				cachedRing.Mesh = null;
			}
		}

		protected virtual void OnEnable()
		{
			UpdateMesh();
		}

		protected virtual void OnDisable()
		{
			RemoveMesh();
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
			UpdateMesh();
		}

#if UNITY_EDITOR
		protected virtual void OnValidate()
		{
			UpdateMesh();
		}
#endif

#if UNITY_EDITOR
		protected virtual void OnDrawGizmosSelected()
		{
			Gizmos.matrix = transform.localToWorldMatrix;

			SgtHelper.DrawCircle(Vector3.zero, Vector3.up, RadiusMin);
			SgtHelper.DrawCircle(Vector3.zero, Vector3.up, RadiusMax);
		}
#endif

		private void UpdateMesh()
		{
			if (Segments > 0 && SegmentDetail > 0 && RadiusDetail > 0)
			{
				if (generatedMesh == null)
				{
					generatedMesh = SgtHelper.CreateTempMesh("Ring Mesh (Generated)");

					ApplyMesh();
				}

				var slices     = SegmentDetail + 1;
				var rings      = RadiusDetail + 1;
				var total      = slices * rings * 2;
				var positions  = new Vector3[total];
				var coords1    = new Vector2[total];
				var coords2    = new Vector2[total];
				var colors     = new Color[total];
				var indices    = new int[SegmentDetail * RadiusDetail * 6];
				var yawStep    = (Mathf.PI * 2.0f) / Segments / SegmentDetail;
				var sliceStep  = 1.0f / SegmentDetail;
				var ringStep   = 1.0f / RadiusDetail;

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
						var radius  = Mathf.Lerp(RadiusMin, RadiusMax, ring01);

						positions[v] = new Vector3(x * radius, 0.0f, z * radius);
						colors[v] = new Color(1.0f, 1.0f, 1.0f, 0.0f);
						coords1[v] = new Vector2(ring01, slice01);
						coords2[v] = new Vector2(radius, slice01 * radius);
					}
				}

				for (var slice = 0; slice < SegmentDetail; slice++)
				{
					for (var ring = 0; ring < RadiusDetail; ring++)
					{
						var i  = (slice * RadiusDetail + ring) * 6;
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

				generatedMesh.bounds = SgtHelper.NewBoundsCenter(bounds, bounds.center + bounds.center.normalized * BoundsShift);
			}

			if (Shadow != null)
			{
				Shadow.RadiusMin = RadiusMin;
				Shadow.RadiusMax = RadiusMax;
			}

			ApplyMesh();
		}
	}
}

#if UNITY_EDITOR
namespace SpaceGraphicsToolkit
{
	using UnityEditor;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(SgtRingMesh))]
	public class SgtRingMesh_Editor : SgtEditor<SgtRingMesh>
	{
		protected override void OnInspector()
		{
			BeginError(Any(t => t.Segments < 1));
				Draw("segments", "The amount of segments the final ring will be comprised of.");
			EndError();
			BeginError(Any(t => t.SegmentDetail < 1));
				Draw("segmentDetail", "The amount of triangle edges along the inner and outer edges of each segment.");
			EndError();
			BeginError(Any(t => t.RadiusMin == t.RadiusMax));
				Draw("radiusMin", "The radius of the inner edge in local space.");
				Draw("radiusMax", "The radius of the outer edge in local space.");
			EndError();
			BeginError(Any(t => t.RadiusDetail < 1));
				Draw("radiusDetail", "The amount of edge loops around the generated ring. If you have a very large ring then you can end up with very skinny triangles, so increasing this can give them a better shape.");
			EndError();
			Draw("boundsShift", "The amount the mesh bounds should get pushed out by in local space. This should be used with 8+ Segments.");
			Draw("shadow", "If you want these values to control the shadow RadiusMin/Max, then set this here.");
		}
	}
}
#endif