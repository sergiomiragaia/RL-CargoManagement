using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Unity.MLAgents;

public class SceneController : MonoBehaviour
{

	public int stepSaturation = 1000000;
	[SerializeField] public List<GameObject> GoalAreas;
	[SerializeField] public List<GameObject> CargoAreas;
	public List<GameObject> spawnedCargoes;
	[SerializeField] public List<AgentSpawnArea> AgentSpawnAreas;
	[SerializeField] private TagMaster agent;
	[SerializeField] private List<GameObject> walls;

	[SerializeField] public int simultaneousCargo = 2;
	[SerializeField] public int targetCargo = 2;
	private int nextId = 0;
	public int step;
	public int agentPeriod = 0;
	public bool freeToLoad = true;
	public float currentRatio = 0;
	EnvironmentParameters m_envParam;


	public void Awake()
	{
		m_envParam = Academy.Instance.EnvironmentParameters;
		step = (int)m_envParam.GetWithDefault("Initial_Step", 0f);
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
		step++;
		if(freeToLoad)
		{
			foreach (GameObject CargoArea in CargoAreas)
			{
				CargoSpawnController cargoSpawnController = CargoArea.GetComponent<CargoSpawnController>();
				if (spawnedCargoes.Count < simultaneousCargo)
				{
					cargoSpawnController.spawnCargo(nextId);
					nextId++;
				}
			}
		}
		if (AllGoalAreasFilled())
		{
			agent.finishCount++;
			// Debug.Log("EndEpisode - AllCargoAreasFilled");
			agent.EndingEp(0.5f);
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

		float unclampedRatio = (float)step/(float)stepSaturation;
		currentRatio = Mathf.Clamp(unclampedRatio, 0f, 1f);

		// Update decision requester
		agentPeriod = agent.updateDecionRequest(unclampedRatio);

		foreach(AgentSpawnArea agentSpawnArea in AgentSpawnAreas)
		{
			if(agentSpawnArea.isEasy)
			{
				agentSpawnArea.Probability = 
					agentSpawnArea.startProbability * (1 - currentRatio);
			}
			else
			{
				agentSpawnArea.Probability = agentSpawnArea.startProbability +
					(1 - agentSpawnArea.startProbability) * currentRatio;
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
			wall.SetActive( Random.value < currentRatio );
		}

		int randomSpawnPoint = Random.Range(0, chosenAgentSpawnArea.places.Count);
		agent.transform.position = chosenAgentSpawnArea.places[randomSpawnPoint].position;
		agent.transform.rotation = chosenAgentSpawnArea.places[randomSpawnPoint].rotation;
		if(!chosenAgentSpawnArea.isEasy) agent.transform.Rotate(0f, Random.Range(-15 * currentRatio, 15 * currentRatio), 0f);

		freeToLoad = true;
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
