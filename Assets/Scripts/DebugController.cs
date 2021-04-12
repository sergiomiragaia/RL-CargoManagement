using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;

public class DebugController : MonoBehaviour
{
	private List<TagMaster> m_Agents = new List<TagMaster>();
	private List<SceneController> m_sceneControllers = new List<SceneController>();
	private int m_frames;
	public int updateRate = 1000;

	private StatsRecorder m_Recorder;

	private int m_totalCargoCount = 0;
	private int m_totalUnloadCount = 0;
	private int m_totalFinishCount = 0;

	public void Awake()
	{
		m_Recorder = Academy.Instance.StatsRecorder;
	}
	void Start() 
	{
		// Application.targetFrameRate = 60;

		Transform[] Objects  = GetComponentsInChildren<Transform>();
		foreach(Transform obj in Objects)
		{
			if(obj.CompareTag("tag_master"))
			{
				TagMaster agent = obj.GetComponent<TagMaster>();
				m_Agents.Add(agent);
			}
			if(obj.CompareTag("scene"))
			{
				SceneController scene = obj.GetComponent<SceneController>();
				m_sceneControllers.Add(scene);
			}
		}
	}

	void FixedUpdate()
	{
		m_frames++;
		if (m_frames % (Mathf.Round(updateRate)) == 0)
		{
			for (int i = 0; i < m_Agents.Count; i++)
			{
				m_Recorder.Add("Achivements/Load Count", m_Agents[i].cargoCount, StatAggregationMethod.Average);
				m_totalCargoCount += m_Agents[i].cargoCount;
				m_Agents[i].cargoCount = 0;
				m_Recorder.Add("Achivements/Unload Count", m_Agents[i].unloadCount, StatAggregationMethod.Average);
				m_totalUnloadCount += m_Agents[i].unloadCount;
				m_Agents[i].unloadCount = 0;
				m_Recorder.Add("Achivements/Finish Count", m_Agents[i].finishCount, StatAggregationMethod.Average);
				m_totalFinishCount += m_Agents[i].finishCount;
				m_Agents[i].finishCount = 0;
			}
		}
	}
}
