﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FirstPersonController : MonoBehaviour {

	[Header("References")]
	private GameObject[] planets;
	private GravityBody gravityScript;
	private Transform cameraTransform;
	private Transform flagHolder;
	private Rigidbody rb;

	[Header("Variables")]
	[SerializeField] private float mouseSensitivityX = 3.5f;
	[SerializeField] private float mouseSensitivityY = 3.5f;
	[SerializeField] private float walkSpeed = 4f;
	[SerializeField] private float jumpForce = 220f;
	[SerializeField] private LayerMask groundedMask;

	private float verticalLookRotation;
	private Vector3 moveAmount;
	private Vector3 smoothMoveVelocity;
	private bool grounded;
	private bool hasFlag;

	void Awake ()
	{
		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;

		if (planets == null)
		{
			planets = GameObject.FindGameObjectsWithTag("Planet");
		}

		gravityScript = GetComponent<GravityBody>();
		cameraTransform = Camera.main.transform;
		flagHolder = transform.Find("FlagHolder");
		rb = GetComponent<Rigidbody>();
	}

	void Update ()
	{
		// Caméra
		// Horizontal (on bouge le corps)
		transform.Rotate(Vector3.up * Input.GetAxis("Mouse X") * mouseSensitivityX);
		// Vertical (on bouge la caméra)
		// On clamp la rotation avant de changer le transform
		verticalLookRotation += Input.GetAxis("Mouse Y") * mouseSensitivityY;
		verticalLookRotation = Mathf.Clamp(verticalLookRotation, -60f, 60f);
		cameraTransform.localEulerAngles = Vector3.left * verticalLookRotation;

		// Déplacement

		float inputX = Input.GetAxisRaw("Horizontal");
		float inputY = Input.GetAxisRaw("Vertical");
		Vector3 moveDirection = new Vector3(inputX, 0f, inputY).normalized;
		Vector3 targetMoveAmount = moveDirection * walkSpeed;
		moveAmount = Vector3.SmoothDamp(moveAmount, targetMoveAmount, ref smoothMoveVelocity, 0.15f);


		// Lâcher de drapeau
		if (hasFlag)
		{
			if (Input.GetMouseButtonDown(0))
			{
				LeaveFlag();
			}
		}

		// Si le joueur est en l'air, on cherche quelle planète est la plus proche
		if (!grounded)
		{
			gravityScript.ChangePlanetAttractedTo();
		}
	}

	void FixedUpdate ()
	{
		// Déplacement
		// MovePosition = world space
		// Il nous faut du local space pour que le personnage se déplace par rapport à son axe propre
		// TransformDirection permet de faire cette transition
		Vector3 localMove = transform.TransformDirection(moveAmount) * Time.fixedDeltaTime;
		rb.MovePosition(rb.position + localMove);

		// Saut
		if (Input.GetButtonDown("Jump"))
		{
			if (grounded)
			{
				rb.AddForce(transform.up * jumpForce);
			}
		}

		Ray ray = new Ray(transform.position, -transform.up);
		RaycastHit hit;

		if (Physics.Raycast(ray, out hit, 1.1f, groundedMask))
		{
			grounded = true;
		}
		else
		{
			grounded = false;
		}
	}

	void OnCollisionEnter (Collision collision)
	{
		if (!hasFlag)
		{
			if (collision.gameObject.tag == "Flag")
			{
				GetFlag(collision.gameObject);
			}
		}
	}

	private void GetFlag (GameObject flag)
	{
		hasFlag = true;
		// On rend l'objet kinematic et on désactive sa boite de collision pour qu'il suive bien le joueur
		flag.GetComponent<Rigidbody>().isKinematic = true;
		flag.GetComponent<BoxCollider>().isTrigger = true;
		// On désactive le script de la gravité et on transforme le drapeau en enfant du joueur
		flag.GetComponent<GravityBody>().enabled = false;
		flag.transform.parent = flagHolder;
		// Puis on met le bon transform
		flag.transform.localPosition = Vector3.zero;
		flag.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);

	}

	private void LeaveFlag ()
	{
		GameObject flag = flagHolder.Find("Flag").gameObject;
		flag.GetComponent<GravityBody>().enabled = true;
		flag.GetComponent<Rigidbody>().isKinematic = false;
		flag.GetComponent<BoxCollider>().isTrigger = false;

		flag.GetComponent<GravityBody>().ChangePlanetAttractedTo();

		flag.transform.parent = null;
		flag.transform.position = transform.position + transform.forward*2;
		flag.transform.localScale = Vector3.one;

		hasFlag = false;
	}
}