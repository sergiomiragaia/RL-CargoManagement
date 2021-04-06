using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CargoController : MonoBehaviour
{
	public int id;
	public GameObject Connector_model;
	private GameObject connector = null;
	private HingeJoint hinge1;
	private HingeJoint hinge2;
	public Transform hingeTransform;
	private TagMaster Agent = null;

	private void Start()
	{

	}

	public void attachToMaster(GameObject agent)
	{
		Agent = agent.GetComponent<TagMaster>();
		// Debug.Log("Attach");

		Rigidbody rbody = transform.gameObject.GetComponent<Rigidbody>();
		rbody.constraints = RigidbodyConstraints.None;

		hinge1 = transform.gameObject.AddComponent<HingeJoint>();
		hinge1.anchor = hingeTransform.localPosition;
		hinge1.axis = new Vector3(1f, 0f, 0f);
		hinge1.autoConfigureConnectedAnchor = false;
		hinge1.enableCollision = true;
		hinge1.connectedAnchor = new Vector3(0f, 0.64f, 0f);

		connector = Instantiate(Connector_model, hingeTransform.position, hingeTransform.rotation);
		hinge1.connectedBody = connector.GetComponent<Rigidbody>();
		hinge2 = connector.GetComponent<HingeJoint>();
		hinge2.connectedBody = agent.GetComponent<Rigidbody>();
		hinge2.autoConfigureConnectedAnchor = false;
		hinge2.connectedAnchor = new Vector3(0f, 0.36f, -3.4f);
	}

	public void resetCargo()
	{
		if(connector != null) Destroy(connector);
	}

	private void OnTriggerStay(Collider other)
	{
		TriggerEnterOrStay(other);
	}
	private void OnTriggerEnter(Collider other)
	{
		TriggerEnterOrStay(other);
	}
	private void OnCollisionEnter(Collision other)
	{
		CollisionEnterOrStay(other);
	}
	private void OnCollisionStay(Collision other)
	{
		CollisionEnterOrStay(other);
	}
	private void CollisionEnterOrStay(Collision other)
	{
		if (other.gameObject.CompareTag("wall"))
		{
			if (Agent != null)
			{
				// Debug.Log("Cargo hit wall");
				Agent.EndingEp(-1f);
			}
		}
		if (other.gameObject.CompareTag("cargo0"))
		{
			if (Agent != null) // && other.gameObject != transform.gameObject
			{
				// Debug.Log("Cargo hit cargo");
				Agent.EndingEp(-1f);
			}
		}
	}

	private void TriggerEnterOrStay(Collider other)
	{

	}
	
}
