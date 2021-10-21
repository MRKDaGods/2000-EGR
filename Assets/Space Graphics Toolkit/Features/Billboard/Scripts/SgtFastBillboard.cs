using UnityEngine;
using FSA = UnityEngine.Serialization.FormerlySerializedAsAttribute;

namespace SpaceGraphicsToolkit
{
	/// <summary>This component rotates the current Gameobject to the rendering camera.</summary>
	[ExecuteInEditMode]
	[HelpURL(SgtHelper.HelpUrlPrefix + "SgtFastBillboard")]
	[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Fast Billboard")]
	public class SgtFastBillboard : SgtLinkedBehaviour<SgtFastBillboard>
	{
		/// <summary>If the camera rolls, should this billboard roll with it?</summary>
		public bool RollWithCamera { set { rollWithCamera = value; } get { return rollWithCamera; } } [FSA("RollWithCamera")] [SerializeField] private bool rollWithCamera = true;

		/// <summary>If your billboard is clipping out of view at extreme angles, then enable this.</summary>
		public bool AvoidClipping { set { avoidClipping = value; } get { return avoidClipping; } } [FSA("AvoidClipping")] [SerializeField] private bool avoidClipping;

		[HideInInspector]
		public Quaternion Rotation = Quaternion.identity;

		[System.NonSerialized]
		public int Mask;

		[System.NonSerialized]
		public Transform cachedTransform;

		public void RandomlyRotate(int seed)
		{
			Rotation = Quaternion.Euler(0.0f, 0.0f, Random.value * 360.0f);
		}

		protected override void OnEnable()
		{
			base.OnEnable();

			Mask = 1 << gameObject.layer;

			cachedTransform = GetComponent<Transform>();
		}
	}
}

#if UNITY_EDITOR
namespace SpaceGraphicsToolkit
{
	using UnityEditor;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(SgtFastBillboard))]
	public class SgtFastBillboard_Editor : SgtEditor<SgtFastBillboard>
	{
		protected override void OnInspector()
		{
			Draw("rollWithCamera", "If the camera rolls, should this billboard roll with it?");
			Draw("avoidClipping", "If your billboard is clipping out of view at extreme angles, then enable this.");
		}
	}
}
#endif