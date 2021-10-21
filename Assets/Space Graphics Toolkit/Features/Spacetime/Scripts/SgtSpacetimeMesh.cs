using UnityEngine;

namespace SpaceGraphicsToolkit
{
	/// <summary>This component allows you to generate the <b>SgtSpacetime</b> component's <b>Mesh</b> setting.</summary>
	[ExecuteInEditMode]
	[RequireComponent(typeof(SgtSpacetime))]
	[HelpURL(SgtHelper.HelpUrlPrefix + "SgtSpacetimeMesh")]
	[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Spacetime Mesh")]
	public class SgtSpacetimeMesh : MonoBehaviour
	{
		/// <summary>The amount of quads along the X axis.</summary>
		public int QuadsX { set { if (quadsX != value) { quadsX = value; DirtyMesh(); } } get { return quadsX; } } [SerializeField] private int quadsX = 16;

		/// <summary>The amount of quads along the Z axis.</summary>
		public int QuadsZ { set { if (quadsZ != value) { quadsZ = value; DirtyMesh(); } } get { return quadsZ; } } [SerializeField] private int quadsZ = 16;

		[System.NonSerialized]
		private Mesh generatedMesh;

		[System.NonSerialized]
		private SgtSpacetime cachedSpacetime;

		[System.NonSerialized]
		private bool cachedSpacetimeSet;

		public SgtSpacetime CachedSpacetime
		{
			get
			{
				if (cachedSpacetimeSet == false)
				{
					cachedSpacetime    = GetComponent<SgtSpacetime>();
					cachedSpacetimeSet = true;
				}

				return cachedSpacetime;
			}
		}

		public void DirtyMesh()
		{
			UpdateMesh();
		}

#if UNITY_EDITOR
		/// <summary>This method allows you to export the generated mesh as an asset.
		/// Once done, you can remove this component, and set the <b>SgtSpacetime</b> component's <b>Mesh</b> setting using the exported asset.</summary>
		[ContextMenu("Export Mesh")]
		public void ExportMesh()
		{
			UpdateMesh();

			if (generatedMesh != null)
			{
				SgtHelper.ExportAssetDialog(generatedMesh, "Spacetime Mesh");
			}
		}
#endif

		[ContextMenu("Apply Mesh")]
		public void ApplyMesh()
		{
			CachedSpacetime.Mesh = generatedMesh;
		}

		[ContextMenu("Remove Mesh")]
		public void RemoveMesh()
		{
			if (CachedSpacetime.Mesh == generatedMesh)
			{
				CachedSpacetime.Mesh = null;
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

		private void UpdateMesh()
		{
			if (quadsX > 0 && quadsZ > 0)
			{
				if (generatedMesh == null)
				{
					generatedMesh = SgtHelper.CreateTempMesh("Spacetime Mesh (Generated)");
				}

				var vertsX    = quadsX + 1;
				var vertsZ    = quadsZ + 1;
				var total     = vertsX * vertsZ;
				var positions = new Vector3[total];
				var coords    = new Vector2[total];
				var indices   = new int[quadsX * quadsZ * 6];
				var stepX     = 1.0f / quadsX;
				var stepZ     = 1.0f / quadsZ;

				for (var z = 0; z < vertsZ; z++)
				{
					for (var x = 0; x < vertsX; x++)
					{
						var u = x * stepX;
						var v = z * stepZ;
						var i = x + z * vertsX;

						positions[i] = new Vector3(u - 0.5f, 0.0f, v - 0.5f);
						coords[i] = new Vector2(u, v);
					}
				}

				for (var z = 0; z < quadsZ; z++)
				{
					for (var x = 0; x < quadsX; x++)
					{
						var i = (x + z * quadsX) * 6;
						var a = x + z * vertsX;
						var b = a + 1;
						var c = a + vertsX;
						var d = b + vertsX;

						indices[i + 0] = a;
						indices[i + 1] = b;
						indices[i + 2] = c;
						indices[i + 3] = d;
						indices[i + 4] = c;
						indices[i + 5] = b;
					}
				}

				generatedMesh.Clear(false);
				generatedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
				generatedMesh.vertices    = positions;
				generatedMesh.uv          = coords;
				generatedMesh.triangles   = indices;
				generatedMesh.RecalculateNormals();
				//generatedMesh.RecalculateBounds();

				generatedMesh.bounds = new Bounds(Vector3.zero, Vector3.one);
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
	[CustomEditor(typeof(SgtSpacetimeMesh))]
	public class SgtSpacetimeMesh_Editor : SgtEditor<SgtSpacetimeMesh>
	{
		protected override void OnInspector()
		{
			var dirtyMesh = false;

			BeginError(Any(t => t.QuadsX < 1));
				Draw("quadsX", ref dirtyMesh, "The amount of quads along the X axis.");
			EndError();
			BeginError(Any(t => t.QuadsZ < 1));
				Draw("quadsZ", ref dirtyMesh, "The amount of quads along the Z axis.");
			EndError();

			if (dirtyMesh == true)
			{
				DirtyEach(t => t.DirtyMesh());
			}
		}
	}
}
#endif