using UnityEngine;
using FSA = UnityEngine.Serialization.FormerlySerializedAsAttribute;

namespace SpaceGraphicsToolkit
{
	/// <summary>This component allows you to rotate the current GameObject based on mouse/finger drags. NOTE: This requires the SgtInputManager in your scene to function.</summary>
	[HelpURL(SgtHelper.HelpUrlPrefix + "SgtCameraLook")]
	[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Camera Look")]
	public class SgtCameraLook : MonoBehaviour
	{
		/// <summary>The speed the camera rotates relative to the mouse/finger drag distance.</summary>
		public float Sensitivity { set { sensitivity = value; } get { return sensitivity; } } [FSA("Sensitivity")] [SerializeField] private float sensitivity = 0.1f;

		/// <summary>How quickly the rotation transitions from the current to the target value (-1 = instant).</summary>
		public float Damping { set { damping = value; } get { return damping; } } [FSA("Dampening")] [SerializeField] private float damping = 10.0f;

		/// <summary>The degrees per second of roll.</summary>
		public float RollSpeed { set { rollSpeed = value; } get { return rollSpeed; } } [FSA("RollSpeed")] [SerializeField] private float rollSpeed = 45.0f;

		/// <summary>The key required to roll left.</summary>
		public KeyCode RollLeftKey { set { rollLeftKey = value; } get { return rollLeftKey; } } [FSA("RollLeftKey")] [SerializeField] private KeyCode rollLeftKey = KeyCode.Q;

		/// <summary>The key required to roll right.</summary>
		public KeyCode RollRightKey { set { rollRightKey = value; } get { return rollRightKey; } } [FSA("RollRightKey")] [SerializeField] private KeyCode rollRightKey = KeyCode.E;

		[System.NonSerialized]
		private Quaternion remainingDelta = Quaternion.identity;

		[System.NonSerialized]
		private SgtInputManager inputManager = new SgtInputManager();

		protected virtual void Update()
		{
			inputManager.Update();

			AddToDelta();
			DampenDelta();
		}

		private void AddToDelta()
		{
			// Calculate delta
			var delta = inputManager.GetAverageDeltaScaled() * sensitivity;

			if (inputManager.Fingers.Count > 1)
			{
				delta = Vector2.zero;
			}

			// Store old rotation
			var oldRotation = transform.localRotation;

			// Rotate
			transform.Rotate(delta.y, -delta.x, 0.0f, Space.Self);

			var roll = 0.0f;

			if (Input.GetKey(rollLeftKey) == true)
			{
				roll += 1.0f;
			}

			if (Input.GetKey(rollRightKey) == true)
			{
				roll -= 1.0f;
			}

			transform.Rotate(0.0f, 0.0f, roll * rollSpeed * Time.deltaTime, Space.Self);

			// Add to remaining
			remainingDelta *= Quaternion.Inverse(oldRotation) * transform.localRotation;

			// Revert rotation
			transform.localRotation = oldRotation;
		}

		private void DampenDelta()
		{
			// Dampen remaining delta
			var factor   = SgtHelper.DampenFactor(damping, Time.deltaTime);
			var newDelta = Quaternion.Slerp(remainingDelta, Quaternion.identity, factor);

			// Rotate by difference
			transform.localRotation = transform.localRotation * Quaternion.Inverse(newDelta) * remainingDelta;

			// Update remaining
			remainingDelta = newDelta;
		}
	}
}

#if UNITY_EDITOR
namespace SpaceGraphicsToolkit
{
	using UnityEditor;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(SgtCameraLook))]
	public class SgtCameraLook_Editor : SgtEditor<SgtCameraLook>
	{
		protected override void OnInspector()
		{
			BeginError(Any(t => t.Sensitivity == 0.0f));
				Draw("sensitivity", "The speed the camera rotates relative to the mouse/finger drag distance.");
			EndError();
			Draw("damping", "How quickly the rotation transitions from the current to the target value (-1 = instant).");
			Draw("rollSpeed", "The degrees per second of roll.");
			Draw("rollLeftKey", "The key required to roll left.");
			Draw("rollRightKey", "The key required to roll right.");
		}
	}
}
#endif