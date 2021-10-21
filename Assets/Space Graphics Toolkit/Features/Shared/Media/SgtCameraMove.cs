using UnityEngine;
using FSA = UnityEngine.Serialization.FormerlySerializedAsAttribute;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SpaceGraphicsToolkit
{
	/// <summary>This component allows you to move the current GameObject based on WASD/mouse/finger drags. NOTE: This requires the SgtInputManager in your scene to function.</summary>
	[HelpURL(SgtHelper.HelpUrlPrefix + "SgtCameraMove")]
	[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Camera Move")]
	public class SgtCameraMove : MonoBehaviour
	{
		public enum RotationType
		{
			None,
			Acceleration,
			MainCamera
		}

		/// <summary>The distance the camera moves per second with keyboard inputs.</summary>
		public float KeySensitivity { set { keySensitivity = value; } get { return keySensitivity; } } [FSA("KeySensitivity")] [SerializeField] private float keySensitivity = 100.0f;

		/// <summary>The distance the camera moves relative to the finger drag.</summary>
		public float PanSensitivity { set { panSensitivity = value; } get { return panSensitivity; } } [FSA("PanSensitivity")] [SerializeField] private float panSensitivity = 1.0f;

		/// <summary>The distance the camera moves relative to the finger pinch scale.</summary>
		public float PinchSensitivity { set { pinchSensitivity = value; } get { return pinchSensitivity; } } [FSA("PinchSensitivity")] [SerializeField] private float pinchSensitivity = 200.0f;

		/// <summary>If you want the mouse wheel to simulate pinching then set the strength of it here.</summary>
		public float WheelSensitivity { set { wheelSensitivity = value; } get { return wheelSensitivity; } } [FSA("WheelSensitivity")] [SerializeField] [Range(-1.0f, 1.0f)] private float wheelSensitivity = -0.2f;

		/// <summary>How quickly the position goes to the target value (-1 = instant).</summary>
		public float Damping { set { damping = value; } get { return damping; } } [FSA("Dampening")] [SerializeField] private float damping = 10.0f;

		/// <summary>If you want movements to apply to Rigidbody.velocity, set it here.</summary>
		public Rigidbody Target { set { target = value; } get { return target; } } [FSA("Target")] [SerializeField] private Rigidbody target;

		/// <summary>If the target is something like a spaceship, rotate it based on movement?</summary>
		public RotationType TargetRotation { set { targetRotation = value; } get { return targetRotation; } } [FSA("TargetRotation")] [SerializeField] private RotationType targetRotation;

		/// <summary>The speed of the velocity rotation.</summary>
		public float TargetDampening { set { targetDampening = value; } get { return targetDampening; } } [FSA("TargetDampening")] [SerializeField] private float targetDampening = 1.0f;

		/// <summary>Slow down movement when approaching planets and other objects?</summary>
		public bool SlowOnProximity { set { slowOnProximity = value; } get { return slowOnProximity; } } [FSA("SlowOnProximity")] [SerializeField] private bool slowOnProximity;

		public float SlowDistanceMin { set { slowDistanceMin = value; } get { return slowDistanceMin; } } [FSA("SlowDistanceMin")] [SerializeField] private float slowDistanceMin = 10.0f;

		public float SlowDistanceMax { set { slowDistanceMax = value; } get { return slowDistanceMax; } } [FSA("SlowDistanceMax")] [SerializeField] private float slowDistanceMax = 100.0f;

		public float SlowMultiplier { set { slowMultiplier = value; } get { return slowMultiplier; } } [FSA("SlowMultiplier")] [SerializeField] [Range(0.0f, 1.0f)] private float slowMultiplier = 0.1f;

		[System.NonSerialized]
		private Vector3 remainingDelta;

		[System.NonSerialized]
		private SgtInputManager inputManager = new SgtInputManager();

		protected virtual void Update()
		{
			inputManager.Update();

			if (target == null)
			{
				AddToDelta();
				DampenDelta();
			}
		}

		protected virtual void FixedUpdate()
		{
			if (target != null)
			{
				AddToDelta();
				DampenDelta();
			}
		}

		private void AddToDelta()
		{
			// Get delta from fingers
			var deltaXY = inputManager.GetAverageDeltaScaled() * panSensitivity;
			var deltaZ  = (inputManager.GetPinchScale() - 1.0f) * pinchSensitivity;

			if (inputManager.Fingers.Count < 2)
			{
				deltaXY = Vector2.zero;

				keySensitivity *= inputManager.GetPinchScale(wheelSensitivity);
			}

			// Add delta from keyboard
			deltaXY.x += Input.GetAxisRaw("Horizontal") * keySensitivity * Time.deltaTime;
			deltaZ    += Input.GetAxisRaw("Vertical") * keySensitivity * Time.deltaTime;

			if (slowOnProximity == true)
			{
				var distance = float.PositiveInfinity;

				SgtHelper.InvokeCalculateDistance(transform.position, ref distance);

				if (distance < slowDistanceMax)
				{
					var distance01 = Mathf.InverseLerp(slowDistanceMin, slowDistanceMax, distance);
					var multiplier = Mathf.Lerp(slowMultiplier, 1.0f, distance01);

					deltaXY *= multiplier;
					deltaZ  *= multiplier;
				}
			}

			// Store old position
			var oldPosition = transform.position;

			// Translate
			var delta = new Vector3(deltaXY.x, deltaXY.y, deltaZ);

			transform.Translate(delta, Space.Self);

			// Add to remaining
			var acceleration = transform.position - oldPosition;

			remainingDelta += acceleration;

			// Revert position
			transform.position = oldPosition;

			// Rotate to acceleration?
			if (target != null && targetRotation != RotationType.None && delta != Vector3.zero)
			{
				var factor   = SgtHelper.DampenFactor(targetDampening, Time.deltaTime);
				var rotation = target.transform.rotation;

				switch (targetRotation)
				{
					case RotationType.Acceleration:
					{
						rotation = Quaternion.LookRotation(acceleration, target.transform.up);
					}
					break;

					case RotationType.MainCamera:
					{
						var camera = Camera.main;

						if (camera != null)
						{
							rotation = camera.transform.rotation;
						}
					}
					break;
				}

				target.transform.rotation = Quaternion.Slerp(target.transform.rotation, rotation, factor);
				target.angularVelocity    = Vector3.Lerp(target.angularVelocity, Vector3.zero, factor);
			}
		}

		private void DampenDelta()
		{
			// Dampen remaining delta
			var factor   = SgtHelper.DampenFactor(damping, Time.deltaTime);
			var newDelta = Vector3.Lerp(remainingDelta, Vector3.zero, factor);

			// Translate by difference
			if (target != null)
			{
				target.velocity += remainingDelta - newDelta;
			}
			else
			{
				transform.position += remainingDelta - newDelta;
			}

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
	[CustomEditor(typeof(SgtCameraMove))]
	public class SgtCameraMove_Editor : SgtEditor<SgtCameraMove>
	{
		protected override void OnInspector()
		{
			BeginError(Any(t => t.KeySensitivity == 0.0f));
				Draw("keySensitivity", "The distance the camera moves per second with keyboard inputs.");
			EndError();
			BeginError(Any(t => t.PanSensitivity == 0.0f));
				Draw("panSensitivity", "The distance the camera moves relative to the finger drag.");
			EndError();
			BeginError(Any(t => t.PinchSensitivity == 0.0f));
				Draw("pinchSensitivity", "The distance the camera moves relative to the finger pinch scale.");
			EndError();
			Draw("wheelSensitivity", "If you want the mouse wheel to simulate pinching then set the strength of it here.");
			Draw("damping", "How quickly the position goes to the target value (-1 = instant).");

			Separator();

			Draw("target", "If you want movements to apply to Rigidbody.velocity, set it here.");
			Draw("targetRotation", "If the target is something like a spaceship, rotate it based on movement?");
			Draw("targetDampening", "The speed of the velocity rotation.");

			Separator();

			Draw("slowOnProximity", "Slow down movement when approaching planets and other objects?");
			Draw("slowDistanceMin", "");
			Draw("slowDistanceMax", "");
			Draw("slowMultiplier", "");
		}
	}
}
#endif