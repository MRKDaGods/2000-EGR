using System.Collections.Generic;
using UnityEngine;

namespace SpaceGraphicsToolkit
{
	/// <summary>This component allows you to generate the <b>SgtSpacetime</b> component's <b>Mesh</b> setting.</summary>
	[ExecuteInEditMode]
	[RequireComponent(typeof(SgtSpacetime))]
	public abstract class SgtSpacetimeModifier : MonoBehaviour
	{
		public abstract List<Matrix4x4> GetMatrices();
	}
}