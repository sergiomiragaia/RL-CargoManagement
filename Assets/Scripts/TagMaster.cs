using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Random = UnityEngine.Random;
using Unity.MLAgents.Sensors;




public class TagMaster : Agent
{
	[SerializeField] private float maxMotorTorque = 400f;
	[SerializeField] private float maxSteerAngle = 30f;
	[SerializeField] private float maxBrakeTorque = 200f;
	[SerializeField] private int framesUpdateTarget = 1000;
	[SerializeField] private int startDecisionPeriod = 50;
	[SerializeField] private int endDecisionPeriod = 5;
	[SerializeField] public List<Wheel> wheelInfos; 
	[SerializeField] public bool simplePickUp = true;
	[SerializeField] public Transform tailConnector;
	[SerializeField] private SceneController sceneController;

	public float CurrentAcceleration
	{
		get => m_currentAcceleration;
		set => m_currentAcceleration = Mathf.Clamp(value, -1f, 1f);
	}

	public float CurrentBrakeTorque
	{
		get => m_currentBrakeTorque;
		set
		{
			m_currentBrakeTorque = Mathf.Clamp(value, 0, 1f);
		} 
	}

	public float CurrentSteeringAngle
	{
		get => m_currentSteeringAngle;
		set => m_currentSteeringAngle = Mathf.Clamp(value,-1f,1f);
	}
	
	public float CurrentSpeed
	{
		get => m_currentSpeed;
		set => m_currentSpeed = Mathf.Round(100f * value)/100f;
	}

	private bool isCarryingCargo = false;
	private float m_currentSteeringAngle = 0f;
	private float m_currentAcceleration = 0f;
	private float m_currentBrakeTorque = 0f;
	private float m_currentSpeed = 0f;
	private Rigidbody rBody;
	private GameObject carriedCargo;
	private GameObject nearestCargo = null;
	private GameObject nearestUnloadArea = null;
	private int CarriedCargoID;

	private int frames = 0;
	public int cargoCount = 0;
	public int unloadCount = 0;
	public int finishCount = 0;

	public override void Initialize()
	{
		base.Initialize();
		rBody = GetComponent<Rigidbody>();
	}

	private void Start()
	{

	}

	private void Update()
	{

	}

	private void FixedUpdate()
	{

		if((nearestCargo == null || frames % framesUpdateTarget==0) && !isCarryingCargo)
		{
			nearestCargo = findNearestCargo();
		}
		if((nearestUnloadArea == null || frames % framesUpdateTarget==0) && isCarryingCargo)
		{
			nearestUnloadArea = findNearestUnloadArea();
		}
		
		frames++;

		AddReward(-1f / MaxStep);
		float motor = maxMotorTorque * m_currentAcceleration;
		float steering = maxSteerAngle * m_currentSteeringAngle;
		float brake = maxBrakeTorque * m_currentBrakeTorque;
		float speed = m_currentSpeed;
		// Debug.Log(motor + "," + steering + "," + brake + "," + speed);
		foreach (Wheel wheel in wheelInfos)
		{
			if (wheel.axel == Axel.Front) {
				wheel.collider.steerAngle = Mathf.Lerp(wheel.collider.steerAngle, steering, 0.5f);
			}
			if (wheel.motor) {
				wheel.collider.motorTorque = motor;
			}
			if (wheel.brake)
			{
				wheel.collider.brakeTorque = brake;
			}
		}
	}

	public void EndingEp(float reward)
	{
		AddReward(reward);
		// Debug.Log("End total_reward = " + total_reward);
		EndEpisode();
	}

	public int updateDecionRequest(float unclampedRatio)
	{
		float ratio = Mathf.Clamp(unclampedRatio / 2f, 0f, 1f);
		DecisionRequester decisionRequester = GetComponent<DecisionRequester>();
		int period = (int)(endDecisionPeriod * ratio + startDecisionPeriod * (1 - ratio));
		decisionRequester.DecisionPeriod = period;
		return period;
	}

	public override void CollectObservations(VectorSensor sensor)
	{
		// 1 observation
		sensor.AddObservation(isCarryingCargo);

		//Nearest Cargo
		if (nearestCargo == null)
		{
			float[] sensors = new float[5];
			sensors[0] = 0f;
			sensors[1] = 0f;
			sensors[2] = 0f;
			sensors[3] = 0f;
			sensors[4] = 0f;
			sensor.AddObservation(sensors);
		}
		else
		{
			CargoController nearestCargoController = nearestCargo.GetComponentInChildren<CargoController>();
			Vector3 toCargo = nearestCargoController.hingeTransform.position - tailConnector.position;
			// 3 observation
			sensor.AddObservation(toCargo.normalized);
			// 1 observation
			sensor.AddObservation(Vector3.Dot(toCargo.normalized, -nearestCargoController.hingeTransform.forward.normalized));
			// 1 observation
			sensor.AddObservation(Vector3.Dot(tailConnector.forward.normalized, -nearestCargoController.hingeTransform.forward.normalized));
		}

		//Nearest Unload Area
		if (nearestUnloadArea == null)
		{
			float[] sensors = new float[3];
			sensors[0] = 0f;
			sensors[1] = 0f;
			sensors[2] = 0f;
			sensor.AddObservation(sensors);
		}
		else
		{
			Vector3 toUnloadArea = nearestUnloadArea.transform.position - transform.position;
			// 3 observation
			sensor.AddObservation(toUnloadArea.normalized);
		}
	}

	public override void OnActionReceived(ActionBuffers actionBuffers)
	{
		
		CurrentSpeed = Vector3.Dot(rBody.velocity, transform.forward);

		if((actionBuffers.ContinuousActions[0] >= 0 && CurrentSpeed >= 0) ||
		(actionBuffers.ContinuousActions[0] <= 0 && CurrentSpeed <= 0))
		{
			// Debug.Log("Acelera");
			CurrentAcceleration = actionBuffers.ContinuousActions[0];
			CurrentBrakeTorque = 0f;
		}
		else
		{
			// Debug.Log("Freia");
			CurrentAcceleration = 0f;
			CurrentBrakeTorque = Mathf.Abs(actionBuffers.ContinuousActions[0]);
		}
		CurrentSteeringAngle = actionBuffers.ContinuousActions[1];
	}


	public override void Heuristic(in ActionBuffers actionBuffers)
	{
		var continuousActionsOut = actionBuffers.ContinuousActions;

		if (Input.GetKey(KeyCode.W)) continuousActionsOut[0] = 1f;
		else if (Input.GetKey(KeyCode.S)) continuousActionsOut[0] = -1f;
		else continuousActionsOut[0] = 0f;

		if (Input.GetKey(KeyCode.D)) continuousActionsOut[1] = 1f;
		else if (Input.GetKey(KeyCode.A)) continuousActionsOut[1] = -1f;
		else continuousActionsOut[1] = 0f;

	}
	public override void OnEpisodeBegin()
	{
		sceneController.ResetArea();

		frames = 0;
		rBody.velocity = Vector3.zero;
		rBody.angularVelocity = Vector3.zero;
		m_currentSteeringAngle = 0f;
		m_currentAcceleration = 0f;
		m_currentBrakeTorque = 0f;
		m_currentSpeed = 0f;
		if (isCarryingCargo)
		{			
			CargoController cargoController = carriedCargo.GetComponentInChildren<CargoController>();
			cargoController.resetCargo();
			sceneController.spawnedCargoes.Remove(carriedCargo);
			Destroy(carriedCargo);
			isCarryingCargo = false;
		}

		// Debug.Log("Episode begin");
	}

	private void OnTriggerStay(Collider other)
	{
		//if (other.gameObject) return;
		if (isCarryingCargo && other.gameObject.CompareTag("lane"))
		{
			AddReward(0.5f / MaxStep);
		}
	}
	private void OnTriggerEnter(Collider other)
	{

		//if (other.CompareTag("human"))
		//{
		//	Debug.Log("Hit human");
		//	EndingEp(-1f);
		//}

		if (other.CompareTag("load_area"))
		{
			sceneController.freeToLoad = false;
		}

		if (!isCarryingCargo && other.CompareTag("cargo_connector"))
		{
			if (simplePickUp)
			{
				pickUpCargo(other);
			}
			else 
			{
				if( checkAllignment(other) )
				{
					pickUpCargo(other);
				}
				else
				{
					// Debug.Log("Failed pick up");
					EndingEp(-1f);
				}
			}
		}

		if (isCarryingCargo && other.CompareTag("unload_area"))
		{
			//Debug.Log("unload cargo");
			unloadCount++;
			AddReward(3f);
			CargoController cargoController = carriedCargo.GetComponentInChildren<CargoController>();
			cargoController.resetCargo();
			GoalAreaController GoalArea = other.GetComponent<GoalAreaController>();
			GoalArea.addCargo(carriedCargo);
			sceneController.spawnedCargoes.Remove(carriedCargo);
			Destroy(carriedCargo);
			isCarryingCargo = false;
			nearestCargo = null;
		}

		if (isCarryingCargo && other.gameObject.CompareTag("lane"))
		{
			AddReward(0.5f / MaxStep);
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (other.CompareTag("load_area"))
		{
			sceneController.freeToLoad = true;
		}
	}
	private void OnCollisionEnter(Collision other)
	{
		if (other.gameObject.CompareTag("wall"))
		{
			// Debug.Log("Hit wall");
			EndingEp(-1f);
		}
		if (other.gameObject.CompareTag("cargo0"))
		{
			// Debug.Log("Hit cargo");
			EndingEp(-1f);
		}
	}
	private void pickUpCargo(Collider other)
	{
		cargoCount++;
		carriedCargo = sceneController.loadCargo(other);
		isCarryingCargo = true;
		CargoController cargoController = carriedCargo.GetComponentInChildren<CargoController>();
		rBody.constraints = RigidbodyConstraints.FreezeAll;
		StartCoroutine("freeze");
		cargoController.attachToMaster(transform.gameObject);
		rBody.constraints = RigidbodyConstraints.None;
		AddReward(2f);
	}

	IEnumerator freeze() 
	{
		yield return new WaitForSeconds(Time.deltaTime);
	}

	private bool checkAllignment(Collider other)
	{
		// Debug.Log("V = " + rBody.velocity.magnitude);
		if (rBody.velocity.magnitude > 10f) return false;
		Vector3 pickUpFace = other.transform.forward.normalized;
		float angle = Vector3.Angle(pickUpFace, transform.forward);
		// Debug.Log("Angle = " + angle);
		if (Mathf.Abs(angle) > 30f) return false;
		Vector3 d = transform.position - other.transform.position;
		Vector3 projX = Vector3.Project(d, transform.right.normalized);
		// Debug.Log("projX = " + projX);
		if (Mathf.Abs(projX.magnitude) > 2f) return false;
		// Vector3 projZ = Vector3.Project(d, transform.forward.normalized);
		// Debug.Log("projZ = " + projZ);
		return true;
	}

	private GameObject findNearestCargo()
	{
		float d = Mathf.Infinity;
		GameObject _nearestCargo = null;
		foreach(GameObject cargo in sceneController.spawnedCargoes)
		{
			float _d = (transform.position - cargo.transform.position).magnitude;
			if( _d < d )
			{
				_nearestCargo = cargo;
				d = _d;
			}
		}
		return _nearestCargo;
	}
	private GameObject findNearestUnloadArea()
	{
		float d = Mathf.Infinity;
		GameObject _nearestUnloadArea = null;
		foreach(GameObject unloadArea in sceneController.GoalAreas)
		{
			float _d = (transform.position - unloadArea.transform.position).magnitude;
			if( _d < d )
			{
				_nearestUnloadArea = unloadArea;
				d = _d;
			}
		}
		return _nearestUnloadArea;
	}

}