using UnityEngine;
using FSA = UnityEngine.Serialization.FormerlySerializedAsAttribute;

namespace SpaceGraphicsToolkit
{
	/// <summary>This component allows you to deform SgtSpacetime grids.</summary>
	[ExecuteInEditMode]
	[HelpURL(SgtHelper.HelpUrlPrefix + "SgtSpacetimeWell")]
	[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Spacetime Well")]
	public class SgtSpacetimeWell : SgtLinkedBehaviour<SgtSpacetimeWell>
	{
		public enum DistributionType
		{
			Gaussian,
			Ripple,
			Twist
		}

		/// <summary>The method used to deform the spacetime.</summary>
		public DistributionType Distribution { set { distribution = value; } get { return distribution; } } [FSA("Distribution")] [SerializeField] private DistributionType distribution = DistributionType.Gaussian;

		/// <summary>The radius of this spacetime well.</summary>
		public float Radius { set { radius = value; } get { return radius; } } [FSA("Radius")] [SerializeField] private float radius = 1.0f;

		/// <summary>The frequency of the ripple.</summary>
		public float Frequency { set { frequency = value; } get { return frequency; } } [FSA("Frequency")] [SerializeField] private float frequency = 1.0f;

		/// <summary>The minimum strength of the well.</summary>
		public float Strength { set { strength = value; } get { return strength; } } [FSA("Strength")] [SerializeField] private float strength = 1.0f;

		/// <summary>The frequency offset.</summary>
		public float Offset { set { offset = value; } get { return offset; } } [FSA("Offset")] [SerializeField] private float offset;

		/// <summary>The frequency offset speed per second.</summary>
		public float OffsetSpeed { set { offsetSpeed = value; } get { return offsetSpeed; } } [FSA("OffsetSpeed")] [SerializeField] private float offsetSpeed;

		/// <summary>The size of the twist hole.</summary>
		public float HoleSize { set { holeSize = value; } get { return holeSize; } } [FSA("HoleSize")] [SerializeField] [Range(0.0f, 0.9f)] private float holeSize;

		/// <summary>The power of the twist hole.</summary>
		public float HolePower { set { holePower = value; } get { return holePower; } } [FSA("HolePower")] [SerializeField] private float holePower = 10.0f;

		public static SgtSpacetimeWell Create(int layer = 0, Transform parent = null)
		{
			return Create(layer, parent, Vector3.zero, Quaternion.identity, Vector3.one);
		}

		public static SgtSpacetimeWell Create(int layer, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
		{
			var gameObject    = SgtHelper.CreateGameObject("Spacetime Well", layer, parent, localPosition, localRotation, localScale);
			var spacetimeWell = gameObject.AddComponent<SgtSpacetimeWell>();

			return spacetimeWell;
		}

#if UNITY_EDITOR
		[UnityEditor.MenuItem(SgtHelper.GameObjectMenuPrefix + "Spacetime Well", false, 10)]
		public static void CreateItem()
		{
			var parent        = SgtHelper.GetSelectedParent();
			var spacetimeWell = Create(parent != null ? parent.gameObject.layer : 0, parent);

			SgtHelper.SelectAndPing(spacetimeWell);
		}
#endif

		protected virtual void Update()
		{
#if UNITY_EDITOR
		if (Application.isPlaying == false)
		{
			return;
		}
#endif
			offset += offsetSpeed * Time.deltaTime;
		}

#if UNITY_EDITOR
		protected virtual void OnDrawGizmosSelected()
		{
			Gizmos.DrawWireSphere(transform.position, radius);
		}
#endif
	}
}

#if UNITY_EDITOR
namespace SpaceGraphicsToolkit
{
	using UnityEditor;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(SgtSpacetimeWell))]
	public class SgtSpacetimeWell_Editor : SgtEditor<SgtSpacetimeWell>
	{
		protected override void OnInspector()
		{
			BeginError(Any(t => t.Radius < 0.0f));
				Draw("radius", "The radius of this spacetime well.");
			EndError();
			Draw("strength", "The minimum strength of the well.");

			Separator();

			Draw("distribution", "The method used to deform the spacetime.");
			BeginIndent();
				if (Any(t => t.Distribution == SgtSpacetimeWell.DistributionType.Ripple || t.Distribution == SgtSpacetimeWell.DistributionType.Twist))
				{
					Draw("frequency", "The frequency of the ripple.");
				}

				if (Any(t => t.Distribution == SgtSpacetimeWell.DistributionType.Ripple))
				{
					Draw("offset", "The frequency offset.");
					Draw("offsetSpeed", "The frequency offset speed per second.");
				}

				if (Any(t => t.Distribution == SgtSpacetimeWell.DistributionType.Twist))
				{
					BeginError(Any(t => t.HoleSize < 0.0f));
						Draw("holeSize", "The size of the twist hole.");
					EndError();
					Draw("holePower", "The power of the twist hole.");
				}
			EndIndent();
		}
	}
}
#endif