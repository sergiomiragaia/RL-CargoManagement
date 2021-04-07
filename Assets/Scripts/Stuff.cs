using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Axel
{
	Front,
	Rear
}

[System.Serializable]
public struct Wheel
{
	public GameObject model;
	public WheelCollider collider;
	public Axel axel;
	public bool motor; 
	public bool brake;
}

[System.Serializable]
public class AgentSpawnArea
{
	public GameObject Area;
	public List<Transform> places;
	public float Probability;
	public float startProbability;
	public bool isEasy;
}

public class CargoItem
{
	public int id;
	public Transform place;
	public bool isPlaced;
	public BoxCollider connector_collider;
	public MeshCollider body_collider;
	public GameObject Cargo;
}

public class GoalItem
{
	public int id;
	public Transform place;
	public bool isPlaced;
	public List<MeshRenderer> renders;
}