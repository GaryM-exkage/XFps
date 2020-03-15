using System;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
	[Header("Results")]
	[SerializeField] private float groundSlopeAngle = 0f;
	[SerializeField] private Vector3 groundSlopeDir = Vector3.zero;

	[Header("Settings")]
	[SerializeField] private CharacterController controller;
	[SerializeField] private Transform groundCheck;
	[SerializeField] private float speed = 12f;
	[SerializeField] private float gravity = -9.81f;
	[SerializeField] private float groundDistance = 0.4f;
	[SerializeField] private float jumpHeight = 3f;
	[SerializeField] private LayerMask groundMask;
	[SerializeField] private float startDistanceFromBottom = 0.2f;
	[SerializeField] private float sphereCastRadius = 0.25f;
	[SerializeField] private float sphereCastDistance = 0.75f;
	[SerializeField] private float raycastLength = 0.75f;

	[SerializeField] private Vector3 rayOriginOffset1 = new Vector3(-0.2f, 0f, 0.16f);
	[SerializeField] private Vector3 rayOriginOffset2 = new Vector3(0.2f, 0f, -0.16f);

	[SerializeField] private float slopeFriction = 0.5f;
	[SerializeField] private float slopeAngleThreshold = 50f;
	[SerializeField] private bool isGrounded;
	private Vector3 velocity;

	public bool isSloping = false;

	void Update()
	{
		// isGrounded = Physics.CheckSphere(this.transform.position + Vector3.up * (controller.radius - 1f + Physics.defaultContactOffset), 
		//                                     controller.radius - Physics.defaultContactOffset, 
		//                                     groundMask);
		
		CheckGround(new Vector3(transform.position.x, transform.position.y - (controller.height / 2) + startDistanceFromBottom, transform.position.z));

		if(isGrounded && velocity.y <= 0 )
		{
			velocity.y = gravity * Time.deltaTime;
		}

		float x = Input.GetAxis("Horizontal");
		float z = Input.GetAxis("Vertical");

		Vector3 move = transform.right * x + transform.forward * z;

		controller.Move(move * speed * Time.deltaTime);

		if(Input.GetButtonDown("Jump") && isGrounded)
		{
			velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
		}

		velocity.y += gravity * Time.deltaTime;

		controller.Move(velocity * Time.deltaTime);
	}

	private void CheckGround(Vector3 origin)
	{
		RaycastHit hit;

		if(Physics.SphereCast(origin, sphereCastRadius, Vector3.down, out hit, sphereCastDistance, groundMask))
		{
			groundSlopeAngle = Vector3.Angle(hit.normal, Vector3.up);

			Vector3 temp = Vector3.Cross(Vector3.up, hit.normal);

			groundSlopeDir = Vector3.Cross(temp, hit.normal);
			if(groundSlopeAngle <= slopeAngleThreshold)
			{
				isGrounded = true;
			}
			else
			{
				isGrounded = false;
				isSloping = true;
			}
			// isGrounded = (groundSlopeAngle <= slopeAngleThreshold);
		}
		else
		{
			isGrounded = false;
			isSloping = false;
		}

		RaycastHit slopeHit1;
		RaycastHit slopeHit2;

		if(Physics.Raycast(origin + rayOriginOffset1, Vector3.down, out slopeHit1, raycastLength))
		{
			float angleOne = Vector3.Angle(slopeHit1.normal, Vector3.up);

			if(Physics.Raycast(origin + rayOriginOffset2, Vector3.down, out slopeHit2, raycastLength))
			{
				float angleTwo = Vector3.Angle(slopeHit2.normal, Vector3.up);

				float[] tempArray = new float[]{ groundSlopeAngle, angleOne, angleTwo };
				Array.Sort(tempArray);
				groundSlopeAngle = tempArray[1];
			}
			else
			{
				float average = (groundSlopeAngle + angleOne) / 2;
				groundSlopeAngle = average;
			}
		}
		
		if(!isGrounded)
		{
			//velocity = Vector3.RotateTowards(velocity, new Vector3((1f - hit.normal.y) * hit.normal.x * slopeFriction, velocity.y, (1f - hit.normal.y) * hit.normal.z * slopeFriction), 1000, 1000);
			var tempmag = velocity.magnitude;
			//var moveDir = Vector3.Cross(hit.normal, Vector3.up);
			groundSlopeDir.y = 0;
			velocity += groundSlopeDir * Time.deltaTime * slopeFriction;
			// velocity.x += (1f - hit.normal.y) * hit.normal.x * slopeFriction * Time.deltaTime;
			// velocity.z += (1f - hit.normal.y) * hit.normal.z * slopeFriction * Time.deltaTime;
			//velocity = Vector3.ClampMagnitude(velocity, tempmag);
		}
		else
		{
			velocity.x = 0f;
			velocity.z = 0f;
			groundSlopeDir = Vector3.zero;
		}
		// if(!isGrounded && isSloping)
		// {
		//     velocity.x += (1f - hit.normal.y) * hit.normal.x * slopeFriction;
		//     velocity.z += (1f - hit.normal.y) * hit.normal.z * slopeFriction;
		// }
		// else
		// {
		//     isSloping = false;
		//     velocity.x = 0f;
		//     velocity.z = 0f;
		// }

	}
}
