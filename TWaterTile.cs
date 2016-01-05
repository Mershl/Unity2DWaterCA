using UnityEngine;
using System.Collections;

/**
 * This script can be used any game object with a attached sprite renderer and a collider.
 * 
 * Don't instantiate this manually. It will be instantited by the parent MainWater object.
 * 
 * This script represents a single WaterTile inside the MainWater object. Based on the
 * settings of DarkWater and LightWater the display of the water will be scaled between a
 * mass of 0.0 to 1.0.
 * 
 * The caps of SlowingWaterMass and DrowningWaterMass will call Moving and Breath components
 * of any character that will enter the collider of a TWaterTile.
 * 
 * \author Marcel Weires
 * \version 1.0
 */

public class TWaterTile : MonoBehaviour {

	TFollowerWater parentWaterField;
	public float waterMass = 0.0f;
	public int posX = 0;
	public int posY = 0;

	public Color darkWater = new Color(0.0f, 0.055f, 0.50f, 0.75f);
	public Color lightWater = new Color(0, 1.00f, 0.96f, 0.5f);

	// at what water mass does a collider slow down
	public float slowingWaterMassCap = 0.2f;
	// at what water mass does a collider drown
	public float drowningWaterMassCap = 0.8f;

	SpriteRenderer spriteRenderer;

	void Start() {
		spriteRenderer = gameObject.renderer as SpriteRenderer;

		if (spriteRenderer) {
			spriteRenderer.enabled = false;
		}
	}

	void OnTriggerStay2D(Collider2D other) {
		if (waterMass > 0.0f) {
			switch (other.tag) {
			case "Player":
				Moving movingComponent = other.gameObject.GetComponent<Moving> ();
				Breath breathComponent = other.gameObject.GetComponent<Breath> ();
				
				if (waterMass > slowingWaterMassCap) {
					if (movingComponent) {
						
					}
					
					if (waterMass > drowningWaterMassCap) {
						if (breathComponent) {
							breathComponent.SetUnderWater();
						}
					}
				}

				break;

			case "Sandbag":
				FillWithWater sandbagComponent = other.GetComponent<FillWithWater>();

				if (sandbagComponent) {
					if (parentWaterField) {
						sandbagComponent.Fill(parentWaterField.RemoveWaterFromWaterTile(posX, posY));
					} else {
						Debug.LogError("No parentWaterField assigned to TWaterTile.");
					}
				} else {
					Debug.LogError("Sandbag has no FillWithWater script attached.");
				}

				break;
			}
		}
	}

	public void SetParent(TFollowerWater parentObject) {
		this.parentWaterField = parentObject;
	}

	public void DisplayMass(float waterMass) {
		if (spriteRenderer) {
			if (waterMass > 0.0f) {
				if (!spriteRenderer.enabled) {
					spriteRenderer.enabled = true;
				}

				waterMass = Mathf.Clamp(waterMass, 0.0f, 1.0f);
				this.waterMass = waterMass;

				spriteRenderer.color = Color.Lerp(lightWater, darkWater, waterMass);
			} else {
				spriteRenderer.enabled = false;
			}
		}
	}

	public void ShowCollision() {
		if (spriteRenderer) {
			if (!spriteRenderer.enabled) {
				spriteRenderer.enabled = true;
			}

			Color lightMagenta = Color.magenta;
			lightMagenta.a = 0.25f;

			this.waterMass = 0.0f;

			spriteRenderer.color = lightMagenta;
		}
	}

	public void ShowEmpty() {
		if (spriteRenderer) {
			if (!spriteRenderer.enabled) {
				spriteRenderer.enabled = true;
			}

			Color lightGreen = Color.green;
			lightGreen.a = 0.25f;

			this.waterMass = 0.0f;
			
			spriteRenderer.color = lightGreen;
		}
	}

	public void ClearDisplay() {
		if (spriteRenderer && spriteRenderer.enabled) {
			this.waterMass = 0.0f;

			spriteRenderer.color = Color.clear;
			spriteRenderer.enabled = false;
		}
	}

}
