using System;
using UnityEngine;

public class PlayerMove : MonoBehaviour
{
	[SerializeField] CharacterController controller;
	[SerializeField] float moveSpeed = 5f;
	[SerializeField] float jumpHeight = 2f;    
	[SerializeField] float gravity = -9.81f;

	[SerializeField] float height = 0.5f;
	[SerializeField] float heightPadding = 0.05f;

	[SerializeField] float startDistanceFromBottom = 0.2f;
	[SerializeField] float sphereCastRadius = 0.25f;
	[SerializeField] float sphereCastDistance = 0.75f;
	// [SerializeField] float raycastLength = 0.75f;
	[SerializeField] LayerMask ground;
	[SerializeField] float maxGroundAngle = 120f;
	[SerializeField] bool debug;


	
	Vector2 input;
	Vector3 moveVector;
	Vector3 velocity;
	[SerializeField] Vector3 slopeVelocity;
	[SerializeField] float slopeSpeed = 1f;
	
	[SerializeField] float groundAngle;

	Vector3 forward;
	Vector3 sideways;
	Vector3 sphereCastOrigin;
	RaycastHit hitInfo;
	[SerializeField] bool isGrounded;
	[SerializeField] bool isSloping;


	void Update()
	{
		GetInput();

		CalculateDirectionVectors();
		CheckGround();
		CalculateGroundAngle();

		if(groundAngle >= maxGroundAngle && isGrounded)
		{
			isSloping = true;
		}
		else
		{
			isSloping = false;
		}

		if(Input.GetButtonDown("Jump") && isGrounded && !isSloping)
		{
			
			velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
		}

		ApplyGravity();



		DrawDebug();

		if(Mathf.Abs(input.x) < 1 && Mathf.Abs(input.y) < 1) return;

		Move();
	}

	/// <summary>
	/// Input based on Horizontal and Vertical axes
	/// </summary>
	void GetInput()
	{
		input.x = Input.GetAxisRaw("Horizontal");
		input.y = Input.GetAxisRaw("Vertical");
	}


	void Move()
	{

		moveVector = sideways * input.x + forward * input.y;
		moveVector.Normalize();
		controller.Move(moveVector * moveSpeed * Time.deltaTime); 
	}

	/// <summary>
	/// If player is not grounded, forward will be equal to transform forward
	/// Use a cross product to determine the new forward vector
	/// </summary>
	void CalculateDirectionVectors()
	{
		if(!isGrounded)
		{
			forward = transform.forward;
			sideways = transform.right;
			return;
		}

		forward = Vector3.Cross(transform.right, hitInfo.normal);
		sideways = Vector3.Cross(hitInfo.normal, transform.forward);
	}

	/// <summary>
	/// Use a Vector3 angle between the ground normal and the transform forward
	/// to determine the slope of the ground
	/// </summary>
	void CalculateGroundAngle()
	{
		if(!isGrounded)
		{
			groundAngle = 0;
			return;
		}

		groundAngle = Vector3.Angle(hitInfo.normal, Vector3.up);
	}

	void CheckGround()
	{
		// if(Physics.Raycast(transform.position, -Vector3.up, out hitInfo, height + heightPadding, ground))
		// {
		//     if(Vector3.Distance(transform.position, hitInfo.point) < height)
		//     {
		//         transform.position = Vector3.Lerp(transform.position, transform.position + Vector3.up * height, 5 * Time.deltaTime);
		//     }
		//     isGrounded = true;
		// }
		// else
		// {
		//     isGrounded = false;
		// }
		sphereCastOrigin = new Vector3(transform.position.x, transform.position.y - (controller.height / 2) + startDistanceFromBottom, transform.position.z);
		if(Physics.SphereCast(sphereCastOrigin, sphereCastRadius, Vector3.down, out hitInfo, sphereCastDistance, ground))
		{
			// if(Vector3.Distance(transform.position, hitInfo.point) < height)
			// {
			//     transform.position = Vector3.Lerp(transform.position, transform.position + Vector3.up * height, 5f * Time.deltaTime);
			//     // controller.Move(Vector3.up * Time.deltaTime * 5f);
			// }
			//groundSlopeAngle = Vector3.Angle(hitInfo.normal, Vector3.up);
			// if(Vector3.Angle(hitInfo.normal, Vector3.up) > maxGroundAngle)
			// {
			//     isSloping = true;
			// }
			isGrounded = true;
		}
		else
		{
			isGrounded = false;
		}

	}

	void ApplyGravity()
	{


		if(!isGrounded)
		{
			velocity.y += gravity * Time.deltaTime;
		}
		// else if(isGrounded && velocity.y <= 0)
		// {
		//     velocity.y -= gravity * Time.deltaTime;
		// }

		if(isSloping)
		{
			Vector3 temp = Vector3.Cross(Vector3.up, hitInfo.normal);
			Vector3 groundSlopeDir = Vector3.Cross(temp, hitInfo.normal);
			slopeVelocity += groundSlopeDir.normalized * Time.deltaTime;
		}
		else
		{
			slopeVelocity = Vector3.zero;
		}



		controller.Move(velocity * Time.deltaTime);
		controller.Move(slopeVelocity * slopeSpeed * Time.deltaTime);
	}

	void DrawDebug()
	{
		if(!debug) return;

		Debug.DrawLine(transform.position, transform.position + forward * height * 2, Color.blue);
		Debug.DrawLine(transform.position, transform.position + Vector3.Cross(transform.forward, hitInfo.normal) * height * 2, Color.red);
		Debug.DrawLine(transform.position, transform.position - Vector3.up * height, Color.green);
		
	}
}
