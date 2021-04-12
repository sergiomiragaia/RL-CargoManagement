using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CargoController : MonoBehaviour
{
	public int id;
	public GameObject Connector_model;
	private GameObject connector = null;
	private HingeJoint m_hinge1;
	private HingeJoint m_hinge2;
	public Transform hingeTransform;
	private TagMaster m_agent = null;

	private void Start()
	{

	}

	public void attachToMaster(GameObject agent)
	{
		m_agent = agent.GetComponent<TagMaster>();
		// Debug.Log("Attach");

		Rigidbody rbody = transform.gameObject.GetComponent<Rigidbody>();
		rbody.constraints = RigidbodyConstraints.None;

		m_hinge1 = transform.gameObject.AddComponent<HingeJoint>();
		m_hinge1.anchor = hingeTransform.localPosition;
		m_hinge1.axis = new Vector3(1f, 0f, 0f);
		m_hinge1.autoConfigureConnectedAnchor = false;
		m_hinge1.enableCollision = true;
		m_hinge1.connectedAnchor = new Vector3(0f, 0.64f, 0f);

		connector = Instantiate(Connector_model, hingeTransform.position, hingeTransform.rotation);
		m_hinge1.connectedBody = connector.GetComponent<Rigidbody>();
		m_hinge2 = connector.GetComponent<HingeJoint>();
		m_hinge2.connectedBody = agent.GetComponent<Rigidbody>();
		m_hinge2.autoConfigureConnectedAnchor = false;
		m_hinge2.connectedAnchor = new Vector3(0f, 0.36f, -3.4f);
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
			if (m_agent != null)
			{
				// Debug.Log("Cargo hit wall");
				m_agent.EndingEp(m_agent.FailReward);
			}
		}
		if (other.gameObject.CompareTag("cargo0"))
		{
			if (m_agent != null) // && other.gameObject != transform.gameObject
			{
				// Debug.Log("Cargo hit cargo");
				m_agent.EndingEp(m_agent.FailReward);
			}
		}
	}

	private void TriggerEnterOrStay(Collider other)
	{

	}
	
}
