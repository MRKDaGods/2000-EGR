using UnityEngine;
using FSA = UnityEngine.Serialization.FormerlySerializedAsAttribute;

namespace SpaceGraphicsToolkit
{
	/// <summary>This component rotates the current GameObject along a random axis, with a random speed.</summary>
	[HelpURL(SgtHelper.HelpUrlPrefix + "SgtProceduralScale")]
	[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Procedural Scale")]
	public class SgtProceduralScale : SgtProcedural
	{
		/// <summary>The default scale of your object.</summary>
		public Vector3 BaseScale { set { baseScale = value; } get { return baseScale; } } [FSA("BaseScale")] [SerializeField] private Vector3 baseScale = Vector3.one;

		/// <summary>The minimum multiplication of the BaseScale.</summary>
		public float ScaleMultiplierMin { set { scaleMultiplierMin = value; } get { return scaleMultiplierMin; } } [FSA("ScaleMultiplierMin")] [SerializeField] private float scaleMultiplierMin = 1.0f;

		/// <summary>The maximum multiplication of the BaseScale.</summary>
		public float ScaleMultiplierMax { set { scaleMultiplierMax = value; } get { return scaleMultiplierMax; } } [FSA("ScaleMultiplierMax")] [SerializeField] private float scaleMultiplierMax = 2.0f;

		protected override void DoGenerate()
		{
			transform.localScale = baseScale * Mathf.Lerp(scaleMultiplierMin, scaleMultiplierMax, Random.value);
		}
	}
}

#if UNITY_EDITOR
namespace SpaceGraphicsToolkit
{
	using UnityEditor;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(SgtProceduralScale))]
	public class SgtProceduralScale_Editor : SgtProcedural_Editor<SgtProceduralScale>
	{
		protected override void OnInspector()
		{
			base.OnInspector();

			BeginError(Any(t => t.BaseScale == Vector3.zero));
				Draw("baseScale", "The default scale of your object.");
			EndError();
			Draw("scaleMultiplierMin", "The minimum multiplication of the BaseScale.");
			Draw("scaleMultiplierMax", "The maximum multiplication of the BaseScale.");
		}
	}
}
#endif