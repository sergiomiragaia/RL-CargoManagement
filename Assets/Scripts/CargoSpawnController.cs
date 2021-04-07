using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class CargoSpawnController : MonoBehaviour
{
	public string CargoTag;
	private GameObject placementAreas;
	[SerializeField] private SceneController sceneController;
	[SerializeField] private GameObject CargoModel;


	private List<CargoItem> CargoArea = new List<CargoItem>();

	void Start()
	{
		placementAreas = transform.gameObject;
		List<Transform> placementAreasList = new List<Transform>(placementAreas.GetComponentsInChildren<Transform>());
		foreach (Transform placementArea in placementAreasList)
		{
			if(placementArea != placementAreas.transform)
			{	
				CargoArea.Add(new CargoItem { 
					id = -1, 
					place = placementArea, 
					isPlaced = false,
					Cargo = null,
					connector_collider = null,
					body_collider = null,
				});
			}
		}
	}

	public void resetCargoArea()
	{
		foreach (CargoItem item in CargoArea)
		{
			item.id = -1;
			item.isPlaced = false;
			item.Cargo = null;
			item.connector_collider = null;
			item.body_collider = null;
		}
	}

	public void spawnCargo (int id)
	{
		if (CargoArea.All(CargoItem => CargoItem.isPlaced))
		{
			return;
		}
		else
		{
			int randomSpawnPoint = Random.Range(0, CargoArea.Count);
			if (CargoArea[randomSpawnPoint].isPlaced == false)
			{
				CargoArea[randomSpawnPoint].id = id;
				CargoArea[randomSpawnPoint].isPlaced = true;
				CargoArea[randomSpawnPoint].Cargo = Instantiate(
					CargoModel, 
					CargoArea[randomSpawnPoint].place.position, 
					CargoArea[randomSpawnPoint].place.rotation
					);
				CargoArea[randomSpawnPoint].connector_collider = CargoArea[randomSpawnPoint].Cargo.GetComponentInChildren<BoxCollider>();
				CargoArea[randomSpawnPoint].connector_collider.enabled = true;

				CargoArea[randomSpawnPoint].body_collider = CargoArea[randomSpawnPoint].Cargo.GetComponentInChildren<MeshCollider>();
				
				Rigidbody rbody = CargoArea[randomSpawnPoint].Cargo.GetComponentInChildren<Rigidbody>();
				rbody.constraints = RigidbodyConstraints.FreezeAll;

				CargoController cargoController = CargoArea[randomSpawnPoint].Cargo.GetComponentInChildren<CargoController>();
				cargoController.id = id;
				sceneController.spawnedCargoes.Add(CargoArea[randomSpawnPoint].Cargo);
			}
		}
	}

	public GameObject loadCargo(Collider cargoCollider)
	{
		int id = -1;
		GameObject Cargo = null;
		foreach (CargoItem item in CargoArea)
		{
			if (item.Cargo != null)
			{
				if(item.connector_collider == cargoCollider)
				{
					id = item.id;
					item.id = -1;
					item.isPlaced = false;
					Cargo = item.Cargo;
					item.connector_collider.enabled = false;
					item.Cargo = null;
					return Cargo;
				}
			}
		}
		Debug.LogError("Cargo not found...");
		return null;
	}

	public void resetArea()
	{
		foreach (CargoItem item in CargoArea)
		{
			item.id = -1;
			item.isPlaced = false;
			sceneController.spawnedCargoes.Remove(item.Cargo);
			Destroy(item.Cargo);
			item.Cargo = null;
		}
	}
}
