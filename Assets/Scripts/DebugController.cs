using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;

public class DebugController : MonoBehaviour
{
	private List<TagMaster> Agents = new List<TagMaster>();
	private List<SceneController> sceneControllers = new List<SceneController>();

	private int frames;
	public int updateRate = 1000;

	private StatsRecorder m_Recorder;

	public void Awake()
	{
		m_Recorder = Academy.Instance.StatsRecorder;
	}
	void Start() 
	{
		// Application.targetFrameRate = 300;

		Transform[] Objects  = GetComponentsInChildren<Transform>();
		foreach(Transform obj in Objects)
		{
			if(obj.CompareTag("tag_master"))
			{
				TagMaster agent = obj.GetComponent<TagMaster>();
				Agents.Add(agent);
			}
			if(obj.CompareTag("scene"))
			{
				SceneController scene = obj.GetComponent<SceneController>();
				sceneControllers.Add(scene);
			}
		}
	}

	void FixedUpdate()
	{
		frames++;
		if (frames % (Mathf.Round(updateRate)) == 0)
		{
			//Debug.Log(frames);
			for (int i = 0; i < Agents.Count; i++)
			{
				m_Recorder.Add("Load Count", Agents[i].cargoCount, StatAggregationMethod.Average);
				Agents[i].cargoCount = 0;
				m_Recorder.Add("Unload Count", Agents[i].unloadCount, StatAggregationMethod.Average);
				Agents[i].unloadCount = 0;
				m_Recorder.Add("Finish Count", Agents[i].finishCount, StatAggregationMethod.Average);
				Agents[i].finishCount = 0;
			}
			for (int i = 0; i < sceneControllers.Count; i++)
			{
				m_Recorder.Add("Current Ratio", sceneControllers[i].currentRatio, StatAggregationMethod.Average);
				m_Recorder.Add("Agent Period", sceneControllers[i].agentPeriod, StatAggregationMethod.Average);
			}
		}
	}
}
