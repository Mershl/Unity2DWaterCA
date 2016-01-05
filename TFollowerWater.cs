using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
 * This script should be used on an empty game object.
 * 
 * The map will be represented by tiles having a type based on (COLLISION, WATER, EMPTY). This
 * approach was used to add different types later on. The resolution of the tiles can be set by
 * setting the WaterTilesPerTile parameter (it will factor the current amount of tiles and place
 * the tiles accordingly).
 * 
 * The water moves on a circular automata basis. It will iterate through all cells, choose which
 * ones are able to move and perform a movement on the fitting water tiles. Each WaterTile has a
 * mass that will be used to display the depth of the water.
 * 
 * The approach of using GameObjects instead of a f.e. TextureMap was used to use the Unity native
 * collision system to determine which characters are in collision with water.
 * 
 * Optimization Note: The WaterTileObjects will be instantiated at runtime. When instantiating
 * an object without a parent (not grouping it in the TFollowerWater) the instation time was
 * lowered by about 4-5 times.
 * Optimization Note: Instantiating without a collider or a deactivated and activating it 
 * afterwards saves about 40% instantion time.
 * 
 * \author Marcel Weires
 * \version 1.0
 */

public class TFollowerWater : MonoBehaviour {

	public bool debug = false;

	// Block types
	// GROUND = collision
	public static int COLLISION = 0;
	public static int WATER = 1;
	public static int EMPTY = 2;

	public bool movementActivated;

	public GameObject levelObject;
	public GameObject waterTileObject;
	const float levelTileSize = 1.00f;
	public int waterTilesPerTile = 5;
	
	int mapWidth;
	int mapHeight;
	int arrayWidth;
	int arrayHeight;

	GameObject[,] waterGameObjects;
	int[,] waterField;

	//! mass on a scale from 0 to MaxMass
	float[,] mass;
	bool[,] massChanged;

	// optimization: use new mass to make the order of the iteration through the water mass irrelevant
	float[,] newMass;
	
	// water properties
	const float MaxMass = 1.0f; 		// The normal, un-pressurized mass of a full water cell
	const float MaxCompress = 0.02f; 	// How much excess water a cell can store, compared to the cell above it
	const float MinMass = 0.0001f;  	// Ignore cells that are almost dry
	
	const float MinimumJoeGoldMass = 0.2f;

	void Start() {
		CreateTiles();
		UpdateCollisionMap();

		movementActivated = true;
	}
	
	// Update is called once per frame
	void FixedUpdate () {
		if (movementActivated) {
			SimulateOneWaterStep();
		}

		DrawWaterMapOnTiles();
	}

	void LateUpdate() {
		if (debug) {
			TestMouseControl();
		}
	}

	public void SetMovementActivated(bool activated) {
		movementActivated = activated;
	}
	
	void TestMouseControl() {
		if (Input.GetMouseButton(0) || Input.GetMouseButton(1) || Input.GetMouseButton(2)) {
			Vector2 mousePos = Input.mousePosition;
			Ray ray = Camera.main.ScreenPointToRay(mousePos);

			RaycastHit2D[] hits = Physics2D.GetRayIntersectionAll(ray, 20.0f);

			foreach (RaycastHit2D hit in hits) {
				if (hit.collider) {
					if (hit.collider.tag == "Water") {
						TWaterTile waterComponent = hit.collider.gameObject.GetComponent<TWaterTile>();

						if (waterComponent) {
							// Left mouse button
							if (Input.GetMouseButton(0)) {
								AddWaterToWaterTile(waterComponent.posX, waterComponent.posY);
							}

							// Right mouse button
							if (Input.GetMouseButton(1)) {
								SetCollisionAtTile(waterComponent.posX, waterComponent.posY, true);
							}

							// Middle mouse button
							if (Input.GetMouseButton(2)) {
								SetCollisionAtTile(waterComponent.posX, waterComponent.posY, false);
							}
						} else {
							Debug.LogError ("Error - WaterTile has no attached TWaterTile component!");
						}
					}
				}
			}
		}

		if (Input.GetKeyDown(KeyCode.M)) {
			UpdateCollisionMap();
		}
	}
	
	public void AddWaterToWaterTile(int x, int y, float addedMass = 1.0f) {
		// Debug.Log ("AddWaterTo: " + x + "/" + y + " addedMass: " + addedMass);

		if (waterField[x+1,y+1] != COLLISION) {
			float addMass = mass[x+1,y+1] + addedMass;
			mass[x+1,y+1] += Mathf.Clamp(addMass, 0.0f, MaxMass);
		}
	}

	public void RemoveWaterFromWaterTile(int x, int y, float removeMass) {
		if (waterField[x+1,y+1] != COLLISION) {
			float removedMass = mass[x+1,y+1] - removeMass;
			mass[x+1,y+1] += Mathf.Clamp(removedMass, 0.0f, MaxMass);
		}
	}

	public float RemoveWaterFromWaterTile(int x, int y) {
		if (waterField[x+1,y+1] != COLLISION) {
			float removedMass = mass[x+1,y+1];
			mass[x+1,y+1] = 0.0f;
			return removedMass;
		}

		return 0.0f;
	}

	public void SetCollisionAtTile(int x, int y, bool collision) {
		if (collision) {
			waterField[x+1,y+1] = COLLISION;
			mass[x+1,y+1] = 0.0f;
		} else {
			waterField[x+1,y+1] = EMPTY;
			mass[x+1,y+1] = 0.0f;
		}
	}

	void UpdateCollisionMap() {
		Debug.Log ("TFollowerWater - UpdateCollisionMap()");

		foreach (GameObject waterTileObject in waterGameObjects) {
			Vector3 centerOfTileObject = waterTileObject.collider2D.renderer.bounds.center;
			centerOfTileObject.z -= 10.0f;

			TWaterTile waterComponent = waterTileObject.GetComponent<TWaterTile>();

			Ray ray = new Ray(centerOfTileObject, Vector3.forward);

			RaycastHit2D[] hits = Physics2D.GetRayIntersectionAll(ray, 20.0f);
			
			// Debug.Log ("Center: " + centerOfTileObject + " hit: " + (hits.Length > 0 ? "YES" : "NO"));

			bool hitCollision = false;
			foreach (RaycastHit2D hit in hits) {
				if (hit.collider) {
					// all Tags defined as collisions for water
					if (hit.collider.tag == "Wall") {
						hitCollision = true;
						break;
					}
				}
			}

			int posArrayX = waterComponent.posX + 1;
			int posArrayY = waterComponent.posY + 1;

			if (hitCollision) {
				waterField[posArrayX, posArrayY] = COLLISION;
				mass[posArrayX, posArrayY] = 0.0f;
			} else {
				if (waterField[posArrayX, posArrayY] == COLLISION) {
					waterField[posArrayX, posArrayY] = EMPTY;
				}
			}
		}
	}

	void ResetMassChangedArray() {
		for (int x = 0; x < arrayWidth; x++) {
			for (int y = 0; y < arrayHeight; y++) {
				massChanged[x, y] = false;
			}
		}
	}
	
	void SimulateOneWaterStep() {
		ResetMassChangedArray();

		// calculate and apply flow for each block
		for (int x = 1; x <= mapWidth; x++) {
			for (int y = 1; y <= mapHeight; y++) {
				// skip ground (collision blocks)
				if (waterField[x,y] == COLLISION) {
					continue;
				}

				if (mass[x,y] > 0.0f) {
					// "JoeGoldWater"
					HandleJoeGoldWaterAt(x, y);

					// newMass[x,y] = mass[x,y];
				}
			}
		}
		
		// CopyNewMassToMass();
		
		// flag waterField based on mass
		UpdateWaterFieldBasedOnMass();
		
		// remove water that has left the map (water that is on invisible border tile)
		for (int x = 0; x < arrayWidth; x++){
			mass[x,0] = 0;
			mass[x,arrayHeight - 1] = 0;
		}
		for (int y = 1; y < arrayHeight - 1; y++){
			mass[0,y] = 0;
			mass[arrayWidth - 1,y] = 0;
		}
	}
	
	void HandleJoeGoldWaterAt(int x, int y) {
		if (mass[x,y] > MinimumJoeGoldMass) {
			float sumMass = mass[x,y];
			int countSum = 1;
			bool goUp = false;
			bool goRight = false;
			bool goDown = false;
			bool goLeft = false;

			// Up
			if (waterField[x,y+1] != COLLISION && mass[x,y+1] < mass[x,y]) {
				sumMass += mass[x,y+1];
				++countSum;
				goUp = true;
			}

			// Right
			if (waterField[x+1,y] != COLLISION && mass[x+1,y] < mass[x,y]) {
				sumMass += mass[x+1,y];
				++countSum;
				goRight = true;
			}

			// Down
			if (waterField[x,y-1] != COLLISION && mass[x,y-1] < mass[x,y]) {
				sumMass += mass[x,y-1];
				++countSum;
				goDown = true;
			}

			// Left
			if (waterField[x-1,y] != COLLISION && mass[x-1,y] < mass[x,y]) {
				sumMass += mass[x-1,y];
				++countSum;
				goLeft = true;
			}

			float evenWaterLevel = sumMass / countSum;

			// at least one neighbour to balance
			if (countSum > 1) {
				mass[x,y] = evenWaterLevel;
				massChanged[x,y] = true;
			}

			if (goUp) {
				mass[x,y+1] = evenWaterLevel;
				massChanged[x,y+1] = true;
			}

			if (goRight) {
				mass[x+1,y] = evenWaterLevel;
				massChanged[x+1,y] = true;
			}

			if (goDown) {
				mass[x,y-1] = evenWaterLevel;
				massChanged[x,y-1] = true;
			}

			if (goLeft) {
				mass[x-1,y] = evenWaterLevel;
				massChanged[x-1,y] = true;
			}
		}
	}
	
	void InitArrays() {
		for (int x = 0; x < arrayWidth; x++) {
			for (int y = 0; y < arrayHeight; y++) {
				waterField[x, y] = WATER;
				mass[x, y] = 0.0f;
				massChanged[x, y] = false;
				newMass[x, y] = 0.0f;
				
				/*if (y == 0) {
					waterField[x,y] = COLLISION;
				}*/
			}
		}
	}

	void InitGameObjects() {
		System.Diagnostics.Stopwatch methodWatch = new System.Diagnostics.Stopwatch();
		/*System.Diagnostics.Stopwatch instantiateWatch = new System.Diagnostics.Stopwatch();
		System.Diagnostics.Stopwatch transformWatch = new System.Diagnostics.Stopwatch();
		System.Diagnostics.Stopwatch parentingWatch = new System.Diagnostics.Stopwatch();*/
		methodWatch.Start();

		UnityTools.Instance.CleanChildren(gameObject);
		float gameObjectScale = GetWaterTileObjectScale();

		for (int x = 0; x < mapWidth; x++) {
			for (int y = 0; y < mapHeight; y++) {
				float posX = x * gameObjectScale - (levelTileSize / 2.0f) + (gameObjectScale / 2.0f) + gameObject.transform.position.x;
				float posY = y * gameObjectScale - (levelTileSize / 2.0f) + (gameObjectScale / 2.0f) + gameObject.transform.position.y;
				Vector3 pos = new Vector3(posX, posY, 1);

				//instantiateWatch.Start();
				GameObject nextWaterObject = Instantiate(waterTileObject) as GameObject;
				//instantiateWatch.Stop();

				
				//transformWatch.Start();
				nextWaterObject.transform.localScale = new Vector3(gameObjectScale, gameObjectScale);
				nextWaterObject.transform.position = pos;
				nextWaterObject.collider2D.enabled = true;
				//transformWatch.Stop();

				//parentingWatch.Start();
				// nextWaterObject.transform.parent = gameObject.transform;
				//parentingWatch.Stop();

				TWaterTile waterComponent = nextWaterObject.GetComponent<TWaterTile>();
				if (waterComponent) {
					waterComponent.posX = x;
					waterComponent.posY = y;
					waterComponent.SetParent(this);
				} else {
					Debug.LogError ("Error - WaterTile has no attached TWaterTile script!");
				}

				waterGameObjects[x,y] = nextWaterObject;
			}
		}

		methodWatch.Stop();
		/*Debug.Log  ("Instantiate() took " + instantiateWatch.ElapsedMilliseconds + "ms for " + (mapHeight * mapWidth) + " objects.");
		Debug.Log  ("Transform took " + transformWatch.ElapsedMilliseconds + "ms for " + (mapHeight * mapWidth) + " objects.");
		Debug.Log  ("Parenting took " + parentingWatch.ElapsedMilliseconds + "ms for " + (mapHeight * mapWidth) + " objects.");*/
		Debug.Log  ("TFollowerWater - InitGameObjects() with resolution:" + waterTilesPerTile + " took " + methodWatch.ElapsedMilliseconds + "ms.");
	}

	float GetWaterTileObjectScale() {
		return (levelTileSize / waterTilesPerTile);
	}
	
	void UpdateWaterFieldBasedOnMass() {
		for (int x = 1; x <= mapWidth; x++) {
			for (int y = 1; y <= mapHeight; y++) {
				if (waterField[x,y] == COLLISION) {
					continue;
				}
				
				if (mass[x,y] > MinMass) {
					waterField[x,y] = WATER;
				} else {
					waterField[x,y] = EMPTY;
				}
			}
		}
	}
	
	void CopyNewMassToMass() {
		for (int i = 0; i < arrayWidth; i++) {
			for (int j = 0; j < arrayHeight; j++) {
				mass[i, j] = newMass[i, j];
			}
		}
	}
	
	void DrawWaterMapOnTiles() {
		for (int x = 1; x <= mapWidth; x++) {
			for (int y = 1; y <= mapHeight; y++) {
				if (massChanged[x,y] == true) {
					TWaterTile waterTileObject = waterGameObjects[x-1,y-1].GetComponent<TWaterTile>();

					if (waterTileObject) {
						if (waterField[x,y] == COLLISION) {
							if (debug) {
								waterTileObject.ShowCollision();
							} else {
								waterTileObject.ClearDisplay();
							}
						} else {
							if (mass[x,y] > 0.0f) {
								waterTileObject.DisplayMass(mass[x,y]);
							} else {
								//waterTileObject.ShowEmpty();
								waterTileObject.ClearDisplay();
							}
						}
					} else {
						Debug.LogError ("TWaterTile script is not attached to child objects!");
						break;
					}
				}
			}
		}
	}
	
	/*void SetPixelOfArray(ref Color[] pixels, int textureWidth, int textureHeight, int x, int y, Color toColor) {
		int arrayLocation = y * textureWidth + x;
		
		pixels [arrayLocation] = toColor;
		
		Debug.Log (x + "/" + y + " would be " + arrayLocation);
	}*/

	public void CreateTiles() {
		bool error = false;

		if (levelObject) {
			FillWithTile tileFiller = levelObject.GetComponent<FillWithTile>();

			if (tileFiller && !error) {
				mapWidth = tileFiller.numTilesX * waterTilesPerTile;
				mapHeight = tileFiller.numTilesY * waterTilesPerTile;
				arrayWidth = mapWidth + 2;
				arrayHeight = mapHeight + 2;

				waterGameObjects = new GameObject[mapWidth, mapHeight];
				waterField = new int[arrayWidth, arrayHeight];
				mass = new float[arrayWidth, arrayHeight];
				massChanged = new bool[arrayWidth, arrayHeight];
				newMass = new float[arrayWidth, arrayHeight];
				
				Debug.Log ("Array Size: " + arrayWidth + "/" + arrayHeight);
				
				InitArrays ();
				InitGameObjects ();
				// AddTestWater ();
			} else {
				error = true;
				Debug.LogError ("FillWithTile script is not attached to LevelObject!");
			}
		} else {
			Debug.LogError ("Level object not set!");
		}
	}

	public int[,] GetWaterField() {
		return waterField;
	}

	void LevelChanged() {
		UpdateCollisionMap();
	}
	
	Bounds GetBoundsOfLevel(GameObject level) {
		Bounds bounds = new Bounds();
		foreach (Transform groundTile in level.transform) {
			bounds.Encapsulate(groundTile.gameObject.renderer.bounds);
		}
		
		return bounds;
	}
}
