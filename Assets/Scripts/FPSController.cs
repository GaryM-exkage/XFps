using System.Collections.Generic;
using UnityEngine;

public class FPSController : MonoBehaviour
{
	[Header("Physics Settings")]
	[SerializeField] float moveSpeed = 5f;
	[SerializeField] float jumpHeight = 6f;
	[SerializeField] float airControl = 0.01f;
	[SerializeField] float gravityStrength = 9.81f;
	[SerializeField] float coyoteTime = 0.2f;
	[SerializeField] float jumpBuffer = 0.5f;

	[Header("Mouse Settings")]
	[SerializeField] float mouseSensitivity = 10f;

	[Header("Collision Settings")]
	[SerializeField] float groundCheckOffset = 0f;
	[SerializeField] float groundCheckRadius = 0.5f;
	[SerializeField] float groundCheckDistance = 0.15f;
	[SerializeField] CapsuleCollider collisionVolume;
	[Tooltip("The Radius the controller can collide with other colliders")]
	[SerializeField] float collisionRadius = 2f;
	[Tooltip("Don't collide with these layers. One must be the Controller's own layer")]
	[SerializeField] LayerMask excludedLayers;

	float xRotation = 0f;
	Transform mainCam;

	Vector3 moveInput;

	Vector3 momentum;
	
	Vector3 forward;
	Vector3 sideways;
	Vector3 velocity;

	float coyoteDelta = 0f;
	float jumpBufferDelta = 1000f;

	readonly Collider[] overlappingColliders = new Collider[10];

	[SerializeField] bool isGrounded = false;

	RaycastHit hit;

	[Header("Debug")]
	[SerializeField] bool debug = true;

	void Start()
	{
		mainCam = Camera.main.transform;
		Cursor.lockState = CursorLockMode.Locked;
	}

	void Update()
	{
		moveInput = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).normalized;
		moveInput *= moveSpeed;

		var dt = Time.deltaTime;

		DoGroundCheck();
		CalculateDirectionVectors();
		// var groundNormal = !isGrounded ? hit.normal : -Gravity.Down;

		if(!isGrounded)
		{
			coyoteDelta += dt;
			velocity += Gravity.Down * (gravityStrength * dt);
			// momentum *= 0.99f;e
			float tempAirControl = airControl;
			if(momentum.magnitude < 1f)
			{
				tempAirControl *= 3f;
			}
			momentum += (sideways * moveInput.x + forward * moveInput.z) * tempAirControl;
			momentum = Vector3.ClampMagnitude(momentum, moveSpeed);
			moveInput *= 0f;
		}
		else 
		{
			coyoteDelta = 0f;
			velocity = Vector3.zero;
			momentum = Vector3.zero;
		}

		if(Input.GetButtonDown("Jump"))
		{
			jumpBufferDelta = 0f;
		}

		if(jumpBufferDelta < jumpBuffer)
		{
			jumpBufferDelta += dt;
			if(isGrounded || coyoteDelta < coyoteTime && velocity.y <= 0)
			{
				velocity.y = Mathf.Sqrt(jumpHeight * -2f * -gravityStrength);
				momentum = (sideways * moveInput.x + forward * moveInput.z) * 0.5f;
			}
			// momentum = velocity + (sideways * moveInput.x + forward * moveInput.z);
		}



		// {
		//     velocity = Vector3.ProjectOnPlane(velocity, hit.normal);
		// }

		transform.position += (sideways * moveInput.x + forward * moveInput.z) * dt;        

		transform.position += (velocity + momentum) * dt;

		var collisionDisplacement = ResolveCollisions(ref velocity);

		transform.position += collisionDisplacement;

		DoMouselook();


	}

	void CalculateDirectionVectors()
	{
		if(!isGrounded)
		{
			forward = transform.forward;
			sideways = transform.right;
			return;
		}

		forward = Vector3.Cross(transform.right, hit.normal);
		sideways = Vector3.Cross(hit.normal, transform.forward);
	}

	void DoGroundCheck()
	{
		var sphereCastOrigin = collisionVolume.transform.TransformPoint(collisionVolume.center) - new Vector3(0, collisionVolume.height/2 + groundCheckOffset, 0);

		if(Physics.SphereCast(sphereCastOrigin, groundCheckRadius, Vector3.down, out hit, groundCheckDistance, ~excludedLayers))
		{
			while(hit.point.y - transform.position.y > 0)
			{
				// transform.position = Vector3.Lerp(transform.position, transform.position + Vector3.up * (groundCheckDistance), Time.deltaTime * 20f);
				transform.position += Vector3.up * 0.001f;
			}
			isGrounded = true;
		}
		else
		{
			isGrounded = false;
		}
	}

	void OnDrawGizmos()
	{
		if(debug)
		{
			Gizmos.color = Color.blue;
			Gizmos.DrawSphere(collisionVolume.transform.TransformPoint(collisionVolume.center) - new Vector3(0, collisionVolume.height/2 + groundCheckOffset, 0), groundCheckRadius);
			Gizmos.DrawWireSphere(transform.position + new Vector3(0f, collisionVolume.height/2, 0f), collisionRadius + 0.1f);
		}
	}

	private Vector3 ResolveCollisions(ref Vector3 playerVelocity)
	{
		// Get nearby colliders
		Physics.OverlapSphereNonAlloc(transform.position + new Vector3(0f, collisionVolume.height/2, 0f), collisionRadius + 0.1f,
			overlappingColliders, ~excludedLayers);

		var totalDisplacement = Vector3.zero;
		var checkedColliderIndices = new HashSet<int>();
		
		// If the player is intersecting with that environment collider, separate them
		for (var i = 0; i < overlappingColliders.Length; i++)
		{
			// Two player colliders shouldn't resolve collision with the same environment collider
			if (checkedColliderIndices.Contains(i))
			{
				continue;
			}

			var envColl = overlappingColliders[i];

			// Skip empty slots
			if (envColl is null)
			{
				continue;
			}

			// Vector3 collisionNormal;
			// float collisionDistance;
			if (Physics.ComputePenetration(
				collisionVolume, collisionVolume.transform.position, collisionVolume.transform.rotation,
				envColl, envColl.transform.position, envColl.transform.rotation,
				out Vector3 collisionNormal, out float collisionDistance))
			{
				// Ignore very small penetrations
				// Required for standing still on slopes
				// ... still far from perfect though
				if (collisionDistance < 0.015)
				{
					continue;
				}

				checkedColliderIndices.Add(i);

				// Get outta that collider!
				totalDisplacement += collisionNormal * collisionDistance;

				// Crop down the velocity component which is in the direction of penetration
				playerVelocity -= Vector3.Project(playerVelocity, collisionNormal);
			}
		}

		// It's better to be in a clean state in the next resolve call
		for (var i = 0; i < overlappingColliders.Length; i++)
		{
			overlappingColliders[i] = null;
		}

		return totalDisplacement;
	}

	private void DoMouselook()
	{
		float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
		float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
		xRotation -= mouseY;
		xRotation = Mathf.Clamp(xRotation, -90f, 90f);

		mainCam.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
		transform.Rotate(Vector3.up * mouseX);
	}
}
