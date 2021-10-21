using UnityEngine;
using FSA = UnityEngine.Serialization.FormerlySerializedAsAttribute;

namespace SpaceGraphicsToolkit
{
	/// <summary>This component applies force to the attached Rigidbody based on nearby SgtGravitySource components.</summary>
	[ExecuteInEditMode]
	[RequireComponent(typeof(Rigidbody))]
	[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Gravity Receiver")]
	public class SgtGravityReceiver : MonoBehaviour
	{
		public LineRenderer Visual { set { visual = value; } get { return visual; } } [FSA("Visual")] [SerializeField] private LineRenderer visual;

		public int VisualCount { set { visualCount = value; } get { return visualCount; } } [FSA("VisualCount")] [SerializeField] [Range(1, 1000)] private int visualCount = 10;

		[System.NonSerialized]
		private Rigidbody cachedRigidbody;

		[System.NonSerialized]
		private bool cachedRigidbodySet;

		public void RebuildVisual()
		{
			if (visual != null)
			{
				var position = transform.position;
				var velocity = cachedRigidbody.velocity;
				var mass     = cachedRigidbody.mass;

				visual.useWorldSpace = true;
				visual.positionCount = visualCount + 1;

				visual.SetPosition(0, transform.position);

				for (var i = 1; i <= visualCount; i++)
				{
					position += velocity * Time.fixedDeltaTime;

					visual.SetPosition(i, position);

					velocity += CalculateAcceleration(position, mass) * Time.fixedDeltaTime;
				}
			}
		}

		public static Vector3 CalculateAcceleration(Vector3 position, float mass)
		{
			var acceleration = Vector3.zero;

			foreach (var gravitySource in SgtGravitySource.Instances)
			{
				var totalMass  = mass * gravitySource.Mass;
				var vector     = gravitySource.transform.position - position;
				var distanceSq = vector.sqrMagnitude;

				if (distanceSq > 0.0f)
				{
					acceleration += vector.normalized * (totalMass / distanceSq);
				}
			}

			return acceleration;
		}

		protected virtual void Update()
		{
			// Always snap the first position so it looks smooth
			if (visual != null && visual.positionCount > 0)
			{
				visual.SetPosition(0, transform.position);
			}
		}

		protected virtual void FixedUpdate()
		{
			if (cachedRigidbodySet == false)
			{
				cachedRigidbody    = GetComponent<Rigidbody>();
				cachedRigidbodySet = true;
			}

			cachedRigidbody.velocity += CalculateAcceleration(transform.position, cachedRigidbody.mass) * Time.fixedDeltaTime;

			RebuildVisual();
		}
	}
}

#if UNITY_EDITOR
namespace SpaceGraphicsToolkit
{
	using UnityEditor;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(SgtGravityReceiver))]
	public class SgtGravityReceiver_Editor : SgtEditor<SgtGravityReceiver>
	{
		protected override void OnInspector()
		{
			EditorGUILayout.HelpBox("This component applies force to the attached Rigidbody based on nearby SgtGravitySource components.", MessageType.Info);

			Separator();

			Draw("visual");
			Draw("visualCount");
		}
	}
}
#endif