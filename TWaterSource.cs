using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
 * This script can be used on an empty game object. It will work only if a TFollowerWater (or anything 
 * providing WaterTiles) is present in the scene.
 * 
 * It will spring water on a set interval. Using the parameters different kinds of water sources can be
 * simulated. Using the WaterTarget Point and Radius a collider area will be produced, on this a set
 * amount of random tiles will be choosen and their mass attribute will be filled by a set amount.
 * 
 * \author Marcel Weires
 * \version 1.0
 */

public class TWaterSource : MonoBehaviour {

	public bool activated;
	public GameObject mainWaterObject;

	public Vector2 waterTargetPoint;
	public float   waterTargetRadius;

	float secSinceLastWaterTick;
	public float secBetweenWaterTicks = 1.0f;
	public float waterMassPerTick = 0.1f;
	public int waterMassTimesPerTick = 1;
	public float startWaterAfterSec = 3.0f;

	public bool playsSound;

	private GameObject player;
	private Death deathComp;

	void Start() {
		/*if (startWaterAfterSec >= 0.0f) {
			Invoke("ActivateSource", startWaterAfterSec);
		}*/
		player = GameObject.FindGameObjectWithTag ("Player");
		if (player) {
			deathComp = player.GetComponent<Death>();
		}
	}

	// Update is called once per frame
	void Update () {
		if (deathComp && deathComp.IsDead()) {
			if (audio && audio.isPlaying) {
				audio.Stop();
			}
		}

		if (activated) {
			if (startWaterAfterSec > 0.0f) {
				startWaterAfterSec -= Time.deltaTime;
			} else {
				if (playsSound && !audio.isPlaying)
				{
					audio.Play();
				}
				if (mainWaterObject) {
					secSinceLastWaterTick += Time.deltaTime;
					
					if (secSinceLastWaterTick >= secBetweenWaterTicks) {
						for (int times = 0; times < waterMassTimesPerTick; times++) {
							Vector2 target = gameObject.transform.position;
							target.x += waterTargetPoint.x;
							target.y += waterTargetPoint.y;
							
							Collider2D[] hits = Physics2D.OverlapCircleAll (target, waterTargetRadius);
							List<Collider2D> waterHits = new List<Collider2D>();
							
							foreach (Collider2D hit in hits) {
								if (hit.tag.Equals("Water")) {
									waterHits.Add(hit);
								}
							}
							
							for (int i = 0; i < waterHits.Count; i++) {
								int randomWaterTile = Random.Range(0, waterHits.Count);
								Collider2D choosenTile = waterHits[randomWaterTile];
								
								if (choosenTile) {
									TWaterTile waterComponent = choosenTile.gameObject.GetComponent<TWaterTile>();
									TFollowerWater tileWaterComponent = mainWaterObject.GetComponent<TFollowerWater>();
									
									if (tileWaterComponent && waterComponent) {
										if (waterComponent.waterMass <= 1.0f) {
											tileWaterComponent.AddWaterToWaterTile(waterComponent.posX, waterComponent.posY, waterMassPerTick);
										}
									}
								}
							}
						}
						
						secSinceLastWaterTick = 0;
					}
				} else {
					print ("TileWaterObject not set!");
				}
			}
		}
	}

	void OnDrawGizmosSelected() {
		Gizmos.color = Color.red;
		Vector3 target = gameObject.transform.position;
		target.x += waterTargetPoint.x;
		target.y += waterTargetPoint.y;
		Gizmos.DrawSphere (target, waterTargetRadius);
	}

	public void ActivateSource() {
		if (playsSound) {
			audio.Play();
		}
		this.activated = true;
	}

	public void DeactivateSource() {
		if (audio && audio.isPlaying) {
			audio.Stop();
		}

		this.activated = false;
	}
}
