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
	[SerializeField] public float maxBackwardSpeed = 10f;
	[SerializeField] private int framesUpdateTarget = 1000;
	[SerializeField] private List<Wheel> wheelInfos; 
	[SerializeField] private bool simplePickUp = true;
	[SerializeField] private Transform tailConnector;
	[SerializeField] private SceneController sceneController;

	private int m_frames = 0;
	[HideInInspector] public int m_actions = 0;
	private bool isCarryingCargo = false;
	private float m_currentSteeringAngle = 0f;
	private float m_currentAcceleration = 0f;
	private float m_currentBrakeTorque = 0f;
	private float m_currentSpeed = 0f;
	private Rigidbody m_rBody;
	private GameObject m_carriedCargo;
	private GameObject m_nearestCargo = null;
	private GameObject m_nearestUnloadArea = null;
	private int m_CarriedCargoID;

	public int cargoCount = 0;
	public int unloadCount = 0;
	public int finishCount = 0;
	public int episodes = 0;

	private int m_period;
	private float m_stepReward;
	private float m_laneReward;
	private float m_backwardsReward;
	private float m_failReward;
	private float m_successReward;
	private float m_loadReward;
	private float m_unloadReward;
	//public bool onLane = false;

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
	
	public float MaxApprochSpeed
	{
		get; set;
	}
	
	public float MaxApprochAngle
	{
		get; set;
	}
	
	public float MaxApprochDistance
	{
		get; set;
	}

	public float FailReward
	{
		get => m_failReward;//(m_failReward * (1 - (float)m_actions/(float)MaxStep));
	}
	public float SuccessReward
	{
		get => m_successReward;
	}

	public override void Initialize()
	{
		base.Initialize();
		m_rBody = GetComponent<Rigidbody>();

		m_failReward = -0.5f;
		m_successReward = 0.1f;
		m_loadReward = 0.2f * (1f - m_successReward)/sceneController.targetCargo;
		m_unloadReward = 0.8f * (1f - m_successReward)/sceneController.targetCargo;
		m_laneReward = 0.3f / MaxStep;
	}

	private void Start()
	{

	}

	private void Update()
	{

	}

	private void FixedUpdate()
	{

		if((m_nearestCargo == null || m_frames % framesUpdateTarget==0) && !isCarryingCargo)
		{
			m_nearestCargo = findNearestCargo();
		}
		if((m_nearestUnloadArea == null || m_frames % framesUpdateTarget==0) && isCarryingCargo)
		{
			m_nearestUnloadArea = findNearestUnloadArea();
		}
		
		m_frames++;

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
		Debug.Log("Final Reward = " + GetCumulativeReward() + " | Actions = " + m_actions);
		EndEpisode();
	}
	public void EndingEp(bool isFail)
	{
		float cumulativeReward = GetCumulativeReward();
		if (isFail) AddReward(m_failReward - cumulativeReward);
		else AddReward(m_successReward - cumulativeReward);
		Debug.Log("Final Reward = " + GetCumulativeReward() + " | Actions = " + m_actions);
		EndEpisode();
	}

	public void updateDecionRequest(int period)
	{
		m_period = period;
		m_stepReward = 0.9f * (-1f - m_failReward) / (MaxStep/m_period);
		m_backwardsReward = 0.1f * (-1f - m_failReward) / (MaxStep/m_period);
		DecisionRequester decisionRequester = GetComponent<DecisionRequester>();
		decisionRequester.DecisionPeriod = period;
	}

	public override void CollectObservations(VectorSensor sensor)
	{
		// 1 observation
		sensor.AddObservation(isCarryingCargo);

		//Nearest Cargo
		if (m_nearestCargo == null)
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
			CargoController nearestCargoController = m_nearestCargo.GetComponentInChildren<CargoController>();
			Vector3 toCargo = nearestCargoController.hingeTransform.position - tailConnector.position;
			// 3 observation
			sensor.AddObservation(toCargo.normalized);
			// 1 observation
			sensor.AddObservation(Vector3.Dot(toCargo.normalized, -nearestCargoController.hingeTransform.forward.normalized));
			// 1 observation
			sensor.AddObservation(Vector3.Dot(tailConnector.forward.normalized, -nearestCargoController.hingeTransform.forward.normalized));
		}

		//Nearest Unload Area
		if (m_nearestUnloadArea == null)
		{
			float[] sensors = new float[3];
			sensors[0] = 0f;
			sensors[1] = 0f;
			sensors[2] = 0f;
			sensor.AddObservation(sensors);
		}
		else
		{
			Vector3 toUnloadArea = m_nearestUnloadArea.transform.position - transform.position;
			// 3 observation
			sensor.AddObservation(toUnloadArea.normalized);
		}
	}

	public override void OnActionReceived(ActionBuffers actionBuffers)
	{
		m_actions++;
		AddReward(m_stepReward);
		Debug.Log("Cumulative Reward = " + GetCumulativeReward() + " | Actions = " + m_actions);
		
		CurrentSpeed = Vector3.Dot(m_rBody.velocity, transform.forward);

		if((actionBuffers.ContinuousActions[0] >= 0 && CurrentSpeed >= 0))
		{
			// Debug.Log("Accelerate forward");
			CurrentAcceleration = actionBuffers.ContinuousActions[0];
			CurrentBrakeTorque = 0f;
		}
		else if(actionBuffers.ContinuousActions[0] <= 0 && CurrentSpeed <= 0)
		{
			// Debug.Log("Accelerate backward");
			CurrentAcceleration = (1 + Mathf.Clamp(CurrentSpeed/maxBackwardSpeed,-1f,0f)) * actionBuffers.ContinuousActions[0];
			CurrentBrakeTorque = 0f;
			AddReward(m_backwardsReward);
		}
		else
		{
			// Debug.Log("Break");
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

		episodes++;
		sceneController.ResetArea();

		m_frames = 0;
		m_actions = 0;

		m_rBody.constraints = RigidbodyConstraints.FreezeAll;
		foreach(Wheel wheel in wheelInfos)
		{
			wheel.collider.brakeTorque = Mathf.Infinity;
			wheel.collider.steerAngle = 0f;
		}
		StartCoroutine("freeze");
		m_rBody.constraints = RigidbodyConstraints.None;

		m_currentSteeringAngle = 0f;
		m_currentAcceleration = 0f;
		m_currentBrakeTorque = 0f;
		m_currentSpeed = 0f;
		if (isCarryingCargo)
		{			
			CargoController cargoController = m_carriedCargo.GetComponentInChildren<CargoController>();
			cargoController.resetCargo();
			sceneController.spawnedCargoes.Remove(m_carriedCargo);
			Destroy(m_carriedCargo);
			isCarryingCargo = false;
		}
		// Debug.Log("Episode begin");
	}

	private void OnTriggerStay(Collider other)
	{
		if (isCarryingCargo && other.gameObject.CompareTag("lane"))
		{
			AddReward(m_laneReward);
		}
	}
	private void OnTriggerEnter(Collider other)
	{

		//if (other.CompareTag("human"))
		//{
		//	Debug.Log("Hit human");
		//	EndingEp(FailReward);
		//}

		if (other.CompareTag("load_area"))
		{
			sceneController.m_freeToLoad = false;
		}

		if (!isCarryingCargo && other.CompareTag("cargo_connector"))
		{
			if (simplePickUp)
			{
				loadCargo(other);
			}
			else 
			{
				if( checkAllignment(other) )
				{
					loadCargo(other);
				}
				else
				{
					// Debug.Log("Failed pick up");
					EndingEp(FailReward);
				}
			}
		}

		if (isCarryingCargo && other.gameObject.CompareTag("lane"))
		{
			AddReward(m_laneReward);
		}

		if (isCarryingCargo && other.CompareTag("unload_area"))
		{
			unloadCargo(other);
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (other.CompareTag("load_area"))
		{
			sceneController.m_freeToLoad = true;
		}

		if (isCarryingCargo && other.gameObject.CompareTag("lane"))
		{
			//onLane = false;
		}
	}
	private void OnCollisionEnter(Collision other)
	{
		if (other.gameObject.CompareTag("wall"))
		{
			// Debug.Log("Hit wall");
			EndingEp(FailReward);
		}
		if (other.gameObject.CompareTag("cargo0"))
		{
			// Debug.Log("Hit cargo");
			EndingEp(FailReward);
		}
	}
	private void loadCargo(Collider other)
	{
		cargoCount++;
		m_carriedCargo = sceneController.loadCargo(other);
		isCarryingCargo = true;
		CargoController cargoController = m_carriedCargo.GetComponentInChildren<CargoController>();
		m_rBody.constraints = RigidbodyConstraints.FreezeAll;
		StartCoroutine("freeze");
		cargoController.attachToMaster(transform.gameObject);
		m_rBody.constraints = RigidbodyConstraints.None;
		AddReward(m_loadReward);

		m_nearestUnloadArea = findNearestUnloadArea();
	}

	IEnumerator freeze() 
	{
		yield return new WaitForSeconds(Time.deltaTime);
	}

	private void unloadCargo(Collider other)
	{
		//Debug.Log("unload cargo");
		unloadCount++;
		CargoController cargoController = m_carriedCargo.GetComponentInChildren<CargoController>();
		cargoController.resetCargo();
		GoalAreaController GoalArea = other.GetComponent<GoalAreaController>();
		GoalArea.addCargo(m_carriedCargo);
		sceneController.spawnedCargoes.Remove(m_carriedCargo);
		Destroy(m_carriedCargo);
		isCarryingCargo = false;
		AddReward(m_unloadReward);

		m_nearestCargo = findNearestCargo();
	}

	private bool checkAllignment(Collider other)
	{
		// Max speed
		//if (m_rBody.velocity.magnitude > MaxApprochSpeed) return false;

		// Max angle
		Vector3 pickUpFace = other.transform.forward.normalized;
		float angle = Vector3.Angle(pickUpFace, transform.forward);
		if (Mathf.Abs(angle) > MaxApprochAngle) return false;

		// Max axis distance
		Vector3 d = transform.position - other.transform.position;
		Vector3 projX = Vector3.Project(d, transform.right.normalized);
		if (Mathf.Abs(projX.magnitude) > MaxApprochDistance) return false;

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