using UnityEngine;

namespace SpaceGraphicsToolkit
{
	/// <summary>This component should modify <b>SgtFloatingObject</b> to work with Rigidbodies that have interpolation to eliminate stuttering from origin shifts. But for some reason it doesn't do anything?</summary>
	[ExecuteInEditMode]
	[DisallowMultipleComponent]
	[RequireComponent(typeof(Rigidbody))]
	[HelpURL(SgtHelper.HelpUrlPrefix + "SgtFloatingRigidbody")]
	[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Floating Rigidbody")]
	public class SgtFloatingRigidbody : SgtFloatingObject
	{
		private Rigidbody cachedRigidbody;

		protected override void UpdatePositionNow(SgtFloatingCamera floatingCamera)
		{
			if (cachedRigidbody == null)
			{
				cachedRigidbody = GetComponent<Rigidbody>();
			}

			expectedPosition = floatingCamera.CalculatePosition(position);

			cachedRigidbody.position = expectedPosition;

			transform.position = expectedPosition;

			expectedPositionSet = true;
		}
	}
}