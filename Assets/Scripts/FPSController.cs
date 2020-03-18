using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// TODO: Fix slope vector pushing collider

public class FPSController : MonoBehaviour
{
	[Header("Physics Settings")]
	[SerializeField] float moveSpeed = 5f;
	[SerializeField] float groundAccel = 0.1f;
	[SerializeField] AnimationCurve groundAccelCurve;
	[SerializeField] AnimationCurve velocityDecelCurve;
	float groundAccelStartTime = 0f;
	float velocityDecelStartTime = 0f;
	[SerializeField] float dashSpeed = 10f;
	[SerializeField] float jumpHeight = 6f;
	[SerializeField] float airControl = 0.01f;
	[SerializeField] float gravityStrength = 9.81f;
	[SerializeField] float gravityFallingMultiplier = 2f;
	[SerializeField] float coyoteTime = 0.2f;
	[SerializeField] float jumpBuffer = 0.5f;

	[Header("Input Settings")]
	[SerializeField] InputObject input;
	[SerializeField] float mouseSensitivity = 10f;

	[Header("Collision Settings")]
	[SerializeField] float groundCheckOffset = 0f;
	[SerializeField] float groundCheckRadius = 0.5f;
	[SerializeField] float groundCheckDistance = 0.15f;
	[SerializeField] float rayDistance = 1f;
	[SerializeField] CapsuleCollider collisionVolume;
	[SerializeField] SphereCollider groundCheckVolume;
	[Tooltip("The Radius the controller can collide with other colliders")]
	[SerializeField] float collisionRadius = 2f;
	[Tooltip("Don't collide with these layers. One must be the Controller's own layer")]
	[SerializeField] LayerMask excludedLayers;
	[SerializeField] float penetrationIgnoredistance = 0.015f;

	float xRotation = 0f;
	Transform mainCam;
	Camera cameraSettings;
	float FOV = 0f;

	Vector2 moveInput;

	public Vector3 momentum;
	float cacheGroundSpeed = 0f;
	
	Vector3 forward;
	Vector3 sideways;
	public Vector3 velocity;
	float velocityMagnitudeCache = 0f;

	float coyoteDelta = 0f;
	float jumpBufferDelta = 1000f;

	readonly Collider[] overlappingColliders = new Collider[10];
	readonly Collider[] groundColliders = new Collider[10];

	[SerializeField] bool isGrounded = false;
	[SerializeField] bool isGroundSettled = false;
	bool isGroundedInLastFrame = false;
	[SerializeField] bool isJumping = false;
	[SerializeField] bool isDashing = false;

	RaycastHit hit;

	int num = 0;

	[Header("Debug")]
	[SerializeField] bool debug = true;

	void Awake()
	{

		mainCam = Camera.main.transform;
		cameraSettings = mainCam.GetComponent<Camera>();
		FOV = cameraSettings.fieldOfView;
		Cursor.lockState = CursorLockMode.Locked;
		input.Listen(InputType.Move, MoveUpdate);
	}

	void OnDestroy() => input.Ignore(InputType.Move, MoveUpdate);

	private void MoveUpdate(InputAction.CallbackContext context)
	{
		moveInput = context.ReadValue<Vector2>();
	} 

	void Update()
	{	
		var tempMoveInput = moveInput;
		// moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;

		var dt = Time.deltaTime;

		float tempSpeed = moveSpeed;

		// DoGroundCheck();
		var groundCheckOffset = DoComplicatedCollisionGroundCheck();
		CalculateDirectionVectors();
		// var groundNormal = !isGrounded ? hit.normal : -Gravity.Down;
		// if(Input.GetButtonDown("Fire3"))
		// {
		// 	isDashing = true;
		// 	if(moveInput.magnitude > 0)
		// 	{
		// 		// tempSpeed += dashSpeed;
		// 	}
		// 	else
		// 	{
		// 		// tempSpeed += dashSpeed;

		// 	}
		// }
		// if(isDashing)
		// {
		// 	// tempSpeed += dashSpeed;
		// }

		var moveDir = (sideways * tempMoveInput.x + forward * tempMoveInput.y);



		if(!isGroundSettled)
		{
			velocityDecelStartTime = 0f;
			if(isJumping && velocity.y <= 0)
			{
				isJumping = false;
			}
			coyoteDelta += dt;
			velocity += isJumping ? Gravity.Down * (gravityStrength * dt) : Gravity.Down * (gravityStrength * gravityFallingMultiplier * dt);
			// momentum *= 0.99f;e
			float tempAirControl = airControl;
			// if(momentum.magnitude < 0.2f)
			// {
			// 	tempAirControl *= 0.05f;
			// }
			// else
			// if(momentum.magnitude < 1f)
			// {
			// 	tempAirControl *= 3f;
			// }

			momentum += moveDir * tempAirControl;
			var tempMaxAirSpeed = cacheGroundSpeed > moveSpeed ? cacheGroundSpeed : moveSpeed;
			momentum = Vector3.ClampMagnitude(momentum, tempMaxAirSpeed);
		}
		else 
		{

			coyoteDelta = 0f;
			// velocity = Gravity.Down * (gravityStrength * gravityFallingMultiplier * dt);
			if(velocityDecelStartTime == 0f)
			{
				velocityMagnitudeCache = Mathf.Clamp(velocity.sqrMagnitude, 0f, 200f);
				
			}
			velocity.y = 0f;

			if(velocity != Vector3.zero)
			{
				velocityDecelStartTime += dt;
				velocity = Vector3.Lerp(velocity, Vector3.zero, velocityDecelCurve.Evaluate(velocityDecelStartTime / ((velocityMagnitudeCache * 0.002f) + 0.00001f)));
			}

			if(moveInput != Vector2.zero)
			{
				groundAccelStartTime = 0f;
				momentum += isDashing ? moveDir * (groundAccel * 1f) : moveDir * groundAccel;
			}
			else
			{

				groundAccelStartTime += dt;
				momentum = Vector3.Lerp(momentum, Vector3.zero, groundAccelCurve.Evaluate(groundAccelStartTime * 0.9f));
				// momentum = Vector3.zero;
			}

			if(Input.GetButton("Fire3"))
			{
				isDashing = true;
				cacheGroundSpeed = dashSpeed;
				cameraSettings.fieldOfView = FOV + 5f;

			}
			else
			{
				isDashing = false;
			}
			if(!isDashing)
			{
				cameraSettings.fieldOfView = FOV;
				cacheGroundSpeed = moveSpeed;
			}
			// cacheGroundSpeed = isDashing ? dashSpeed : moveSpeed;
			// if(Input.GetButtonUp("Fire3"))
			// {
			// 	cacheGroundSpeed = Mathf.Lerp(dashSpeed, moveSpeed, Time.deltaTime * 0.1f);
			// }
			momentum = Vector3.ClampMagnitude(momentum, cacheGroundSpeed);
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
				
				isJumping = true;
			}
			// momentum = velocity + (sideways * moveInput.x + forward * moveInput.z);
		}



		// {
		//     velocity = Vector3.ProjectOnPlane(velocity, hit.normal);
		// }
		// momentum = Vector3.RotateTowards(momentum, transform.eulerAngles, 360, 0);

		// momentum.z = transform.forward.z;

		// momentum = Quaternion.AngleAxis(transform.eulerAngles.y, Vector3.up) * momentum;
		// momentum = Quaternion.Euler(momentum.x, momentum.y, momentum.z).eulerAngles;


		transform.position += (transform.rotation * momentum) * dt;     

		transform.position += velocity * dt;

		var collisionDisplacement = ResolveCollisions(ref velocity);

		transform.position += collisionDisplacement;

		DoMouselook();


	}

	void CalculateDirectionVectors()
	{
		// if(!isGrounded)
		{
			forward = Vector3.forward;
			sideways = Vector3.right;
			return;
		}
		forward = Vector3.Cross(transform.right, hit.normal).normalized;
		sideways = Vector3.Cross(hit.normal, transform.forward).normalized;
	}

	Vector3 DoComplicatedCollisionGroundCheck()
	{
		// Get nearby colliders
		num = Physics.OverlapSphereNonAlloc(groundCheckVolume.transform.position, groundCheckRadius,
			groundColliders, ~excludedLayers);

		var totalDisplacement = Vector3.zero;
		var checkedColliderIndices = new HashSet<int>();
		bool dirtyGroundFlag = false;
		
		// If the player is intersecting with that environment collider, separate them
		for (var i = 0; i < num; i++)
		{
			
			// Two player colliders shouldn't resolve collision with the same environment collider
			if (checkedColliderIndices.Contains(i))
			{
				continue;
			}

			var envColl = groundColliders[i];

			// Skip empty slots
			if (envColl is null)
			{
				continue;
			}

			// Vector3 collisionNormal;
			// float collisionDistance;
			if (Physics.ComputePenetration(
				groundCheckVolume, groundCheckVolume.transform.position, groundCheckVolume.transform.rotation,
				envColl, envColl.transform.position, envColl.transform.rotation,
				out Vector3 collisionNormal, out float collisionDistance))
			{

				dirtyGroundFlag = true;
				// Ignore very small penetrations
				// Required for standing still on slopes
				// ... still far from perfect though
				// if (collisionDistance < penetrationIgnoredistance)
				// {
				// 	continue;
				// }

				// if(Vector3.Dot(hit.normal, collisionNormal) > 0.95)
				// {
				// 	Debug.Log(Vector3.Dot(hit.normal, collisionNormal));
				// 	// if (collisionDistance < penetrationIgnoredistance)
				// 	// {
				// 	// 	continue;
				// 	// }
				// 	continue;
				// }

				checkedColliderIndices.Add(i);

				// Get outta that collider!
				totalDisplacement += collisionNormal * (collisionDistance + 0.008f);

				// Crop down the velocity component which is in the direction of penetration
				// velocity -= Vector3.Project(velocity, collisionNormal);
			}
		}
		if(dirtyGroundFlag)
		{
			transform.position += totalDisplacement;
			isGroundSettled = true;
			// isGrounded = true;
		}
		else
		if(isGroundSettled)
		{
			if(Physics.Raycast(groundCheckVolume.transform.position, Vector3.down, out hit, groundCheckVolume.radius + rayDistance, ~excludedLayers, QueryTriggerInteraction.UseGlobal))
			{
				isGrounded = true;
			}
			else
			{
				{
					isGrounded = false;
					isGroundSettled = false;
				}
			}
		}
		else
		{
			isGrounded = false;
			isGroundSettled = false;
		}
					// It's better to be in a clean state in the next resolve call
		for (var i = 0; i < num; i++)
		{
			groundColliders[i] = null;
		}

		return totalDisplacement;
	}

	void DoGroundCheck()
	{
		var sphereCastOrigin = transform.position - new Vector3(0, -groundCheckOffset, 0);

		if(Physics.SphereCast(sphereCastOrigin, groundCheckRadius, Vector3.down, out hit, groundCheckDistance, ~excludedLayers))
		{
			// while(hit.point.y - transform.position.y > 0.15f)
			// {
			// 	// transform.position = Vector3.Lerp(transform.position, transform.position + Vector3.up * (groundCheckDistance), Time.deltaTime * 20f);
			// 	transform.position += Vector3.up * 0.001f;
			// }
			// if(hit.distance > 0.15f)
			// {
				// transform.position = new Vector3(transform.position.x, hit.point.y + 0.08f, transform.position.z);
			// }
			isGrounded = true;
			isGroundedInLastFrame = true;
		}
		// else
		// if(!isJumping)
		// {
		// 	if(Physics.Raycast(sphereCastOrigin, Vector3.down, rayDistance, ~excludedLayers, QueryTriggerInteraction.UseGlobal ))
		// 	{
		// 		isGrounded = true;
		// 	}
		// }
		else
		{
			isGrounded = false;
		}

	}

	void OnDrawGizmos()
	{
		if(debug)
		{
			var center = transform.position - new Vector3(0, -groundCheckOffset, 0);
			Gizmos.color = Color.green;
			// Gizmos.DrawSphere(center, groundCheckRadius);
			Gizmos.color = Color.blue;

			// Momentum Vector
			Gizmos.DrawSphere(transform.position + ((transform.rotation * momentum) * 0.3f) + (Vector3.up * 3f), 0.1f);

			// Gizmos.DrawSphere(center - new Vector3(0, groundCheckDistance, 0), groundCheckRadius);
			Gizmos.DrawWireSphere(transform.position + new Vector3(0f, collisionVolume.height/2, 0f), collisionRadius + 0.1f);
			Gizmos.DrawLine(center, center + (Vector3.down * rayDistance));
			Gizmos.color = Color.red;
			Gizmos.DrawSphere(transform.position + (transform.forward * 2f) + (Vector3.up * 3f), 0.1f);
			Gizmos.DrawSphere(groundCheckVolume.transform.position, groundCheckVolume.radius);
			Gizmos.DrawWireSphere(groundCheckVolume.transform.position, groundCheckRadius);
			Gizmos.color = Color.cyan;
			Gizmos.DrawLine(collisionVolume.transform.position + new Vector3(0f, collisionVolume.height/2, 0f), collisionVolume.transform.position - new Vector3(0f, collisionVolume.height/2, 0f));
			if(hit.collider is object)
			{
				Gizmos.DrawSphere(new Vector3(transform.position.x, hit.collider.bounds.max.y, transform.position.z), 0.1f);
			}
		}
	}

	private Vector3 ResolveCollisions(ref Vector3 playerVelocity)
	{
		// Get nearby colliders
		num = Physics.OverlapSphereNonAlloc(transform.position + new Vector3(0f, collisionVolume.height/2, 0f), collisionRadius + 0.1f,
			overlappingColliders, ~excludedLayers);

		var totalDisplacement = Vector3.zero;
		var checkedColliderIndices = new HashSet<int>();
		
		// If the player is intersecting with that environment collider, separate them
		for (var i = 0; i < num; i++)
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
				// if (collisionDistance < penetrationIgnoredistance)
				// {
				// 	continue;
				// }

				// if(Vector3.Dot(hit.normal, collisionNormal) > 0.95)
				// {
				// 	Debug.Log(Vector3.Dot(hit.normal, collisionNormal));
				// 	// if (collisionDistance < penetrationIgnoredistance)
				// 	// {
				// 	// 	continue;
				// 	// }
				// 	continue;
				// }

				checkedColliderIndices.Add(i);

				// Get outta that collider!
				totalDisplacement += collisionNormal * collisionDistance;

				// Crop down the velocity component which is in the direction of penetration
				velocity -= Vector3.Project(velocity, collisionNormal);

				if(!isGrounded)
				{
					momentum *= 0.9f;
				}
			}
		}

		// It's better to be in a clean state in the next resolve call
		for (var i = 0; i < num; i++)
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
		// var athing = Quaternion.Euler(xRotation, 0f, 0f).eulerAngles;
		// momentum = Vector3.RotateTowards(momentum, Quaternion.Euler(0f, 0f, 0f).eulerAngles, 360, 0);
		
	}
}
