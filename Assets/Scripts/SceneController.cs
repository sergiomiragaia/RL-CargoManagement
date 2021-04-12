using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Unity.MLAgents;

public class SceneController : MonoBehaviour
{
	public List<GameObject> GoalAreas;
	public List<GameObject> CargoAreas;
	public List<GameObject> spawnedCargoes;
	public List<AgentSpawnArea> AgentSpawnAreas;
	[SerializeField] private TagMaster agent;
	[SerializeField] private List<GameObject> walls;

	public int simultaneousCargo = 2;
	public int targetCargo = 2;
	private int m_nextId = 0;
	private int m_step = 0;
	public bool m_freeToLoad = true;

	EnvironmentParameters m_envParam;
	private int m_config = -1;
	private float m_wallProb;
	private float m_easySpawnProb;
	private float m_spawnAngle;
	private float m_maxApprochSpeed;
	private float m_maxApprochAngle;
	private float m_maxApprochDistance;
	private float m_maxBackwardSpeed;

	private int m_agentPeriod;


	public void Awake()
	{
		m_envParam = Academy.Instance.EnvironmentParameters;
	}

	void Start()
	{
		foreach (AgentSpawnArea agentSpawnArea in AgentSpawnAreas)
		{
			List<Transform> areas = new List<Transform>(agentSpawnArea.Area.GetComponentsInChildren<Transform>());
			foreach (Transform area in areas)
			{
				if(area.CompareTag("agent_spawn"))
				{
					agentSpawnArea.places.Add(area);
				}
			}
		}
	}

	void FixedUpdate()
	{
		m_step++;
		if(m_freeToLoad)
		{
			foreach (GameObject CargoArea in CargoAreas)
			{
				CargoSpawnController cargoSpawnController = CargoArea.GetComponent<CargoSpawnController>();
				if (spawnedCargoes.Count < simultaneousCargo)
				{
					cargoSpawnController.spawnCargo(m_nextId);
					m_nextId++;
				}
			}
		}
		if (AllGoalAreasFilled())
		{
			agent.finishCount++;
			// Debug.Log("EndEpisode - AllCargoAreasFilled");
			agent.EndingEp(agent.SuccessReward);
		}
	}

	void Update()
	{

	}

	public GameObject loadCargo(Collider cargoCollider)
	{
		int area = 0; //ToDo
		CargoSpawnController cargoSpawnController = CargoAreas[area].GetComponent<CargoSpawnController>();
		GameObject Cargo = cargoSpawnController.loadCargo(cargoCollider);
		return Cargo;
	}

	public void ResetArea()
	{
		m_config = (int)m_envParam.GetWithDefault("cargo_variables", 1);
		// Wall Prob, Easy Spawn Prob, Spawn Angle Range, Max Approuch Speed, Max Approuch Angle, Max Approuch Dist, Decision Period
		switch(m_config)
		{
			case 0:
				m_wallProb = 0f;
				m_easySpawnProb = 0.90f;
				m_spawnAngle = 0f;
				m_maxApprochSpeed = 10f;
				m_maxApprochAngle = 30f;
				m_maxApprochDistance = 2f;
				m_maxBackwardSpeed = 4f;
				break;
			case 1:
				m_wallProb = 0.4f;
				m_easySpawnProb = 0.75f;
				m_spawnAngle = 15f;
				m_maxApprochSpeed = 6f;
				m_maxApprochAngle = 15f;
				m_maxApprochDistance = 1.5f;
				m_maxBackwardSpeed = 3.9f;
				break;
			case 2:
				m_wallProb = 0.8f;
				m_easySpawnProb = 0.5f;
				m_spawnAngle = 30f;
				m_maxApprochSpeed = 5f;
				m_maxApprochAngle = 10f;
				m_maxApprochDistance = 1f;
				m_maxBackwardSpeed = 3.8f;
				break;
			case 3:
				m_wallProb = 1f;
				m_easySpawnProb = 0f;
				m_spawnAngle = 40f;
				m_maxApprochSpeed = 5f;
				m_maxApprochAngle = 5f;
				m_maxApprochDistance = 1f;
				m_maxBackwardSpeed = 3.7f;
				break;
			case 4:
				m_wallProb = 1f;
				m_easySpawnProb = 0f;
				m_spawnAngle = 50f;
				m_maxApprochSpeed = 5f;
				m_maxApprochAngle = 5f;
				m_maxApprochDistance = 1f;
				m_maxBackwardSpeed = 3.6f;
				break;
			case 5:
				m_wallProb = 1f;
				m_easySpawnProb = 0f;
				m_spawnAngle = 60f;
				m_maxApprochSpeed = 5f;
				m_maxApprochAngle = 5f;
				m_maxApprochDistance = 1f;
				m_maxBackwardSpeed = 3.5f;
				break;
			default:
				Debug.LogError("Unknown config");
				break;
		}

		// Update agent
		m_agentPeriod = (int)m_envParam.GetWithDefault("agent_period",40);
		agent.updateDecionRequest(m_agentPeriod);
		agent.MaxApprochSpeed = m_maxApprochSpeed;
		agent.MaxApprochAngle = m_maxApprochAngle;
		agent.MaxApprochDistance = m_maxApprochDistance;
		agent.maxBackwardSpeed = m_maxBackwardSpeed;

		// Easy Spawn Probability
		foreach(AgentSpawnArea agentSpawnArea in AgentSpawnAreas)
		{
			if(agentSpawnArea.isEasy)
			{
				agentSpawnArea.Probability = m_easySpawnProb;
			}
			else
			{
				agentSpawnArea.Probability = (1 - m_easySpawnProb);
			}
		}

		float v = Random.value;
		float total_v=0f;
		AgentSpawnArea chosenAgentSpawnArea = null;
		
		foreach(AgentSpawnArea agentSpawnArea in AgentSpawnAreas)
		{
			total_v += agentSpawnArea.Probability;
			if(v <= total_v)
			{
				chosenAgentSpawnArea = agentSpawnArea;
				break;
			}
		}

		foreach(GameObject wall in walls)
		{
			wall.SetActive( Random.value < m_wallProb );
		}

		int randomSpawnPoint = Random.Range(0, chosenAgentSpawnArea.places.Count);
		agent.transform.position = chosenAgentSpawnArea.places[randomSpawnPoint].position;
		agent.transform.rotation = chosenAgentSpawnArea.places[randomSpawnPoint].rotation;
		if(!chosenAgentSpawnArea.isEasy) agent.transform.Rotate(0f, Random.Range(-m_spawnAngle, m_spawnAngle), 0f);

		m_freeToLoad = true;
		foreach (GameObject GoalArea in GoalAreas)
		{
			GoalAreaController goalAreaController = GoalArea.GetComponent<GoalAreaController>();
			goalAreaController.resetArea();
		}
		foreach (GameObject CargoArea in CargoAreas)
		{
			CargoSpawnController cargoSpawnController = CargoArea.GetComponent<CargoSpawnController>();
			cargoSpawnController.resetArea();
		}
		spawnedCargoes.Clear();
	}

	public bool AllGoalAreasFilled()
	{
		return GoalAreas.All(GoalArea => GoalArea.gameObject.GetComponent<GoalAreaController>().isFull);
	}
}
