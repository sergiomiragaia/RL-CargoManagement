using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class GoalAreaController : MonoBehaviour
{
	public string CargoTag;
	private GameObject placementAreas;
	[SerializeField] private SceneController SceneController;

	public class CargoItem
	{
		public int id;
		public Transform place;
		public bool isPlaced;
		public List<MeshRenderer> renders;
	}

	private List<CargoItem> GoalArea = new List<CargoItem>();
	public bool isFull;

	void Start()
	{
		placementAreas = transform.gameObject;
		List<Transform> placementAreasList = new List<Transform>(placementAreas.GetComponentsInChildren<Transform>());
		foreach (Transform placementArea in placementAreasList)
		{
			if(placementArea.CompareTag("unload_placement"))
			{
				GoalArea.Add(new CargoItem { 
					id = -1, 
					place = placementArea, 
					isPlaced = false,
					renders = new List<MeshRenderer>(placementArea.GetComponentsInChildren<MeshRenderer>())});
			}
		}
	}

	public void resetArea()
	{
		foreach (CargoItem item in GoalArea)
		{
			item.id = -1;
			item.isPlaced = false;
			foreach(MeshRenderer render in item.renders)
			{
				render.enabled = false;
			}
		}
		isFull = false;
	}

	public void addCargo(GameObject cargo)
	{
		CargoController cargoController = cargo.GetComponentInChildren<CargoController>();
		foreach (CargoItem item in GoalArea)
		{
			if (item.isPlaced == false)
			{
				item.id = cargoController.id;
				item.isPlaced = true;
				foreach(MeshRenderer render in item.renders)
				{
					render.enabled = true;
				}
				break;
			}
		}
		if (GoalArea.All(CargoItem => CargoItem.isPlaced))
		{
			isFull = true;
		}
	}

}
