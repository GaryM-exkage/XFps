using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;

// TODO: Fix slope vector pushing collider

public class FPSController : MonoBehaviour
{
	[Header("Physics Settings")]
	[SerializeField] float moveSpeed = 5f;
	[SerializeField] float groundAccel = 0.1f;
	[SerializeField] float groundDecelSpeed = 0.9f;
	[SerializeField] AnimationCurve groundAccelCurve;
	[SerializeField] AnimationCurve velocityDecelCurve;
	float groundAccelStartTime = 0f;
	float velocityDecelStartTime = 0f;
	[SerializeField] float dashSpeed = 10f;
	[SerializeField] float dashTweenSpeed = 1f;
	[SerializeField] int dashFOVOffset = 5;
	[SerializeField] float jumpHeight = 6f;
	[SerializeField] float airControl = 0.01f;
	[SerializeField] float gravityStrength = 9.81f;
	[SerializeField] float gravityFallingMultiplier = 2f;
	[SerializeField] float jumpGravityMultiplier = 0.5f;

	[SerializeField] float coyoteTime = 0.2f;
	[SerializeField] float jumpBuffer = 0.5f;

	[Header("Temporary Audio Things")]
	[SerializeField] AudioClip jumpSound;
	[SerializeField] AudioClip landSound;
	[SerializeField] AudioSource moveAudioSource;

	[Header("Input Settings")]
	[SerializeField] InputObject input;
	[SerializeField] float mouseSensitivity = 10f;

	[Header("Collision Settings")]
	[SerializeField] float slopeWallRaycastOffset = 0.15f;
	[SerializeField] float groundCheckOffset = 0f;
	[SerializeField] float groundCheckRadius = 0.5f;
	[SerializeField] float groundCheckDistance = 0.15f;
	[SerializeField] float rayDistance = 1f;
	[SerializeField] CapsuleCollider collisionVolume;
	[SerializeField] SphereCollider groundCheckVolume;
	[Tooltip("The Radius the controller can collide with other colliders")]
	[SerializeField] float collisionRadius = 2f;
	[SerializeField] float collisionRadiusYOffset = 0f;
	[Tooltip("Don't collide with these layers. One must be the Controller's own layer")]
	[SerializeField] LayerMask excludedLayers;
	[SerializeField] float penetrationIgnoredistance = 0.015f;

	

	float xRotation = 0f;
	Transform mainCam;
	Camera cameraSettings;
	float FOV = 0f;

	Vector2 moveInput;

	public Vector3 momentum;
	public Vector3 combinedVelocity;

	float cacheGroundSpeed = 0f;
	[SerializeField] float jumpGravityTween = 1f;
	Vector3 forward;
	Vector3 sideways;
	public Vector3 velocity;
	public Vector3 velocityInLastFrame;
	float velocityMagnitudeCache = 0f;

	float coyoteDelta = 0f;
	float jumpBufferDelta = 1000f;

	readonly Collider[] overlappingColliders = new Collider[10];
	readonly Collider[] groundColliders = new Collider[10];

	[SerializeField] bool isGrounded = false;
	[SerializeField] bool isGroundSettled = false;
	bool isVelocityDecelTweening = false;
	bool isGroundedInLastFrame = false;
	[SerializeField] bool isJumping = false;
	[SerializeField] bool isHoldingJump = false;
	[SerializeField] bool isDashing = false;

	RaycastHit hit;

	Quaternion angle;

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

		if(isHoldingJump)
		{


			if(Input.GetButton("Jump"))
			{
				if(!DOTween.IsTweening(555))
				DOTween.To(()=>jumpGravityTween, x => jumpGravityTween = x, jumpGravityMultiplier, 0.9f).SetId(555).SetEase(Ease.OutCubic);
			}
			else
			{
				DOTween.Kill(555);
				jumpGravityTween = 1f;
				isHoldingJump = false;
			}
		}


		var tempMoveInput = moveInput;


		var dt = Time.deltaTime;

		float tempSpeed = moveSpeed;

		
		var groundCheckOffset = DoComplicatedCollisionGroundCheck();

		transform.position += groundCheckOffset;

		CalculateDirectionVectors();


		var moveDir = (sideways * tempMoveInput.x + forward * tempMoveInput.y);



		if(!isGroundSettled)
		{

			if(isJumping && velocity.y <= 0)
			{
				isJumping = false;

			}
			coyoteDelta += dt;
			velocity += isJumping ? Gravity.Down * (gravityStrength * jumpGravityTween * dt) : Gravity.Down * (gravityStrength * gravityFallingMultiplier * dt);

			float tempAirControl = airControl;


			momentum += moveDir * tempAirControl;
			var tempMaxAirSpeed = cacheGroundSpeed > moveSpeed && momentum.magnitude > moveSpeed ? cacheGroundSpeed : moveSpeed;
			if(tempMaxAirSpeed == moveSpeed && isDashing)
			{
				DOTween.Kill(75);
				if(!DOTween.IsTweening(1001))
				cameraSettings.DOFieldOfView(FOV, dashTweenSpeed).SetId(1001);
			}
			momentum = Vector3.ClampMagnitude(momentum, tempMaxAirSpeed);
		}
		else 
		{

			coyoteDelta = 0f;

			if(!isVelocityDecelTweening)
			{
				velocityMagnitudeCache = Mathf.Clamp(velocity.magnitude * 0.012f, 0f, 1f);
			}
				
			velocity.y = 0f;

			if(velocity != Vector3.zero)
			{
				isVelocityDecelTweening = true;

				if(!DOTween.IsTweening(900))
				DOTween.To(()=> velocity.x, x => velocity.x = x, 0f, velocityMagnitudeCache).SetId(900).SetEase(Ease.OutFlash);
				if(!DOTween.IsTweening(910))
				DOTween.To(()=> velocity.z, x => velocity.z = x, 0f, velocityMagnitudeCache).SetId(910).SetEase(Ease.OutFlash);

			}
			else
			{
				isVelocityDecelTweening = false;
			}

			if(moveInput != Vector2.zero)
			{
				DOTween.Pause(1000);

				momentum += isDashing ? moveDir * (groundAccel * 1f) : moveDir * groundAccel;
				

				if(isDashing)
				{
					if(!DOTween.IsTweening(75))
					cameraSettings.DOFieldOfView(FOV + dashFOVOffset, dashTweenSpeed).SetId(75);
				}
			}
			else
			{
				if(isDashing)
				{
					DOTween.Kill(75);
					if(!DOTween.IsTweening(1001))
					cameraSettings.DOFieldOfView(FOV, dashTweenSpeed).SetId(1001);
				}

				if(momentum != Vector3.zero)
				{
					if(!DOTween.IsTweening(1000))
					DOTween.Rewind(1000);
					DOTween.Rewind(1001);
					DOTween.To(()=> momentum, x => momentum = x, Vector3.zero, groundDecelSpeed).SetId(1000).SetEase(Ease.OutCubic);
				}

			}

			if(Input.GetButton("Fire3"))
			{
				isDashing = true;
				cacheGroundSpeed = dashSpeed;

			}
			else
			{

				DOTween.Kill(75);
				if(!DOTween.IsTweening(1001))
				cameraSettings.DOFieldOfView(FOV, dashTweenSpeed * 0.5f).SetId(1001);
				cacheGroundSpeed = moveSpeed;
				isDashing = false;
			}

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
				if(!moveAudioSource.isPlaying)
				moveAudioSource.PlayOneShot(jumpSound, 0.2f);
				velocity.y = Mathf.Sqrt(jumpHeight * -2f * -gravityStrength);
				
				isJumping = true;
				isHoldingJump = true;
			}

		}

		combinedVelocity = ((transform.rotation * momentum) + velocity);
		var displacement = combinedVelocity * dt;     
		if (displacement.magnitude > collisionVolume.radius)
        {
            displacement = ClampDisplacement(velocity + momentum, displacement, transform.position);
        }


		transform.position += displacement;


		velocityInLastFrame = velocity;

		var collisionDisplacement = ResolveCollisions();

		transform.position += collisionDisplacement;

		DoMouselook();



	}

	void CalculateDirectionVectors()
	{
		if(!isGrounded)
		{
			forward = Vector3.forward;
			sideways = Vector3.right;
			return;
		}
		
		var inverseRot = Quaternion.Inverse(transform.rotation);

		forward = (Vector3.Cross(transform.right, hit.normal)).normalized;
		sideways = (Vector3.Cross(hit.normal, transform.forward)).normalized;

		var tMoveInput = transform.rotation * (Vector3.right * moveInput.x + Vector3.forward * moveInput.y);

		// Debug.DrawLine(transform.position + (Vector3.up * 2.5f), transform.position + (Vector3.up * 2.5f) + tMoveInput, Color.red);
		// Debug.DrawLine(transform.position + (Vector3.up * 2.5f), transform.position + (Vector3.up * 2.5f) + transform.rotation*forward, Color.green);
		// Debug.DrawLine(transform.position + (Vector3.up * 2.5f), transform.position + (Vector3.up * 2.5f) + transform.rotation*sideways, Color.green);

		if(Physics.CapsuleCast(transform.position, transform.position + (Vector3.up * 2.5f), 0.6f, tMoveInput, collisionVolume.radius + slopeWallRaycastOffset, ~excludedLayers))
		{
			// Debug.Log(hit.normal);
			// Debug.Log(Vector3.SignedAngle(Vector3.Cross(transform.right, hit.normal).normalized, Vector3.up, transform.forward));

			// Debug.DrawLine(hit.point, hit.point + hit.normal, Color.red);
			// Debug.DrawLine(hit.point, hit.point + (Quaternion.Euler(0, 90f, 0) * hit.normal), Color.green);

			forward.y = moveInput.y > 0 ? Mathf.Clamp(forward.y, -1000f, 0f) : Mathf.Clamp(forward.y, 0f, 1000f);
			sideways.y = moveInput.x > 0 ? Mathf.Clamp(sideways.y, -1000f, 0f) : Mathf.Clamp(sideways.y, 0f, 1000f);



			// Debug.DrawLine(transform.position + (Vector3.up * 2.5f), transform.position + (Vector3.up * 2.5f) + forward, Color.blue);
			// Debug.DrawLine(transform.position + (Vector3.up * 2.5f), transform.position + (Vector3.up * 2.5f) + sideways, Color.blue);		

			// if(Vector3.SignedAngle(Vector3.Cross(transform.right, hit.normal), Vector3.up, transform.forward) > 0)
			// {
			// 	forward = Vector3.forward;
			// }

			// if(Vector3.SignedAngle(hit.normal, Vector3.up, -transform.right) > 0)
			// {
			// 	sideways = Vector3.right;
			// }
		}
		forward = inverseRot * forward;
		sideways = inverseRot * sideways;
		
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
				// totalDisplacement += new Vector3(0f, collisionNormal.y * (collisionDistance + 0.0001f), 0f);
				// totalDisplacement += new Vector3(0f, (collisionNormal * collisionDistance).y, 0f);
				totalDisplacement += collisionNormal * collisionDistance;

				// Crop down the velocity component which is in the direction of penetration
				// Debug.Log(Vector3.Angle(hit.normal, Vector3.up));

				// if(Vector3.Angle(hit.normal, Vector3.up) > 46f)
				// {
				// 	velocity -= Vector3.Project(velocity, collisionNormal);
				// }
			}
		}

		if(dirtyGroundFlag)
		{
			// transform.position += totalDisplacement;
			isGroundSettled = true;
			// isGrounded = true;
		}

		if(Physics.SphereCast(groundCheckVolume.transform.position, groundCheckVolume.radius * 0.15f, Vector3.down, out hit, groundCheckVolume.radius + rayDistance, ~excludedLayers, QueryTriggerInteraction.UseGlobal))
		{
			if(isGroundSettled)
			{
				isGrounded = true;
			}
			// if(!isJumping && transform.position.y > hit.point.y + 0.27f)
			// {
			// 	transform.DOMoveY(hit.point.y + 0.27f, 0.01f, false );
			// }
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

			//Collision radius
			Gizmos.DrawWireSphere(transform.position + new Vector3(0f, collisionVolume.height/2 + collisionRadiusYOffset, 0f), collisionRadius + 0.1f);


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

	private Vector3 ResolveCollisions()
	{
		// Get nearby colliders
		num = Physics.OverlapSphereNonAlloc(transform.position + new Vector3(0f, collisionVolume.height/2 + collisionRadiusYOffset, 0f), collisionRadius + 0.1f,
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
				DOTween.Kill(900);
				DOTween.Kill(910);

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
				// if(Vector3.SignedAngle(collisionNormal, Vector3.up, transform.forward) > 46f && !isGrounded && !isJumping && Vector3.Dot(collisionNormal, Vector3.up) > 0.1f)
				// Debug.Log(Vector3.Dot(collisionNormal, -Vector3.up));
				// Debug.Log(Vector3.Dot(collisionNormal, -Vector3.up));

				if(!isJumping || !isGrounded)
				{
					if(Vector3.Dot(collisionNormal, -Vector3.up) < -0.1f && Vector3.Dot(collisionNormal, -Vector3.up) > -0.6f)
					{
						// Debug.Log(Vector3.SignedAngle(collisionNormal, Vector3.up, transform.forward));
						velocity -= Vector3.Project(velocity, collisionNormal);
					}
				}
				// momentum -= Vector3.Project(momentum, collisionNormal);

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

	private Vector3 ClampDisplacement(Vector3 playerVelocity, Vector3 displacement, Vector3 playerPosition)
    {
        RaycastHit hit;
        if (Physics.Raycast(playerPosition, playerVelocity.normalized, out hit, displacement.magnitude, ~excludedLayers))
        {
            return hit.point - playerPosition;
        }
		else
		return displacement;
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
