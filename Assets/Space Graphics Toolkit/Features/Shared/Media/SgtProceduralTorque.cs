using UnityEngine;
using FSA = UnityEngine.Serialization.FormerlySerializedAsAttribute;

namespace SpaceGraphicsToolkit
{
	/// <summary>This component rotates the current GameObject along a random axis, with a random speed.</summary>
	[RequireComponent(typeof(Rigidbody))]
	[HelpURL(SgtHelper.HelpUrlPrefix + "SgtProceduralTorque")]
	[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Procedural Torque")]
	public class SgtProceduralTorque : SgtProcedural
	{
		/// <summary>Minimum degrees per second.</summary>
		public float SpeedMin { set { speedMin = value; } get { return speedMin; } } [FSA("SpeedMin")] [SerializeField] private float speedMin;

		/// <summary>Maximum degrees per second.</summary>
		public float SpeedMax { set { speedMax = value; } get { return speedMax; } } [FSA("SpeedMax")] [SerializeField] private float speedMax = 10.0f;

		protected override void DoGenerate()
		{
			var axis  = Random.onUnitSphere;
			var speed = Random.Range(speedMin, speedMax);

			transform.localRotation = Random.rotation;

			GetComponent<Rigidbody>().angularVelocity = axis * speed * Mathf.Deg2Rad;
		}
	}
}

#if UNITY_EDITOR
namespace SpaceGraphicsToolkit
{
	using UnityEditor;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(SgtProceduralTorque))]
	public class SgtProceduralTorque_Editor : SgtProcedural_Editor<SgtProceduralTorque>
	{
		protected override void OnInspector()
		{
			base.OnInspector();

			Draw("speedMin", "Minimum degrees per second.");
			Draw("speedMax", "Maximum degrees per second.");
		}
	}
}
#endif