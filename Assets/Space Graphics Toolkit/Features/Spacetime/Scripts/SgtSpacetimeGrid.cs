using System.Collections.Generic;
using UnityEngine;

namespace SpaceGraphicsToolkit
{
	/// <summary>This component allows you to generate the <b>SgtSpacetime</b> component's <b>Mesh</b> setting.</summary>
	[ExecuteInEditMode]
	[RequireComponent(typeof(SgtSpacetime))]
	[HelpURL(SgtHelper.HelpUrlPrefix + "SgtSpacetimeGrid")]
	[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Spacetime Grid")]
	public class SgtSpacetimeGrid : SgtSpacetimeModifier
	{
		/// <summary>The amount of cells along the X axis.</summary>
		public int CellsX { set { cellsX = value; } get { return cellsX; } } [SerializeField] private int cellsX = 5;

		/// <summary>The amount of cells along the Z axis.</summary>
		public int CellsZ { set { cellsZ = value; } get { return cellsZ; } } [SerializeField] private int cellsZ = 5;

		private static List<Matrix4x4> tempMatrices = new List<Matrix4x4>();

		[System.NonSerialized]
		private SgtSpacetime cachedSpacetime;

		[System.NonSerialized]
		private bool cachedSpacetimeSet;

		public override List<Matrix4x4> GetMatrices()
		{
			tempMatrices.Clear();

			if (cellsX > 0 && cellsZ > 0)
			{
				if (cachedSpacetimeSet == false)
				{
					cachedSpacetime    = GetComponent<SgtSpacetime>();
					cachedSpacetimeSet = true;
				}

				var matrix  = transform.localToWorldMatrix;
				var stepX   = 1.0f / cachedSpacetime.Size.x;
				var stepZ   = 1.0f / cachedSpacetime.Size.y;
				var sizeX   = cachedSpacetime.Size.x / cellsX;
				var sizeZ   = cachedSpacetime.Size.y / cellsZ;
				var offsetX = sizeX * (cellsX - 1) * -0.5f;
				var offsetZ = sizeZ * (cellsZ - 1) * -0.5f;
				var size    = new Vector2(sizeX, sizeZ);

				for (var z = 0; z < cellsZ; z++)
				{
					for (var x = 0; x < cellsX; x++)
					{
						var center = new Vector2(offsetX + sizeX * x, offsetZ + sizeZ * z);

						AddMatrix(matrix, center, size);
					}
				}
			}

			return tempMatrices;
		}

		private void AddMatrix(Matrix4x4 matrix, Vector2 center, Vector2 size)
		{
			matrix *= Matrix4x4.Translate(new Vector3(center.x, 0.0f, center.y));
			matrix *= Matrix4x4.Scale(new Vector3(size.x, 1.0f, size.y));

			tempMatrices.Add(matrix);
		}
	}
}

#if UNITY_EDITOR
namespace SpaceGraphicsToolkit
{
	using UnityEditor;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(SgtSpacetimeGrid))]
	public class SgtSpacetimeGrid_Editor : SgtEditor<SgtSpacetimeGrid>
	{
		protected override void OnInspector()
		{
			BeginError(Any(t => t.CellsX < 1));
				Draw("cellsX", "The amount of cells along the X axis.");
			EndError();
			BeginError(Any(t => t.CellsZ < 1));
				Draw("cellsZ", "The amount of cells along the Z axis.");
			EndError();
		}
	}
}
#endif