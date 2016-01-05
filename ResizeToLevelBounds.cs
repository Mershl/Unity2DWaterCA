using UnityEngine;
using System.Collections;

/**
 * This script can be used on any game object with a sprite renderer attached.
 * 
 * It will resize the renderer of the attached game object to the size of a passed GameObject.
 * This can be used to scale an overlay to the size of the level by passing the level bounds.
 * 
 * Note: LevelBoundsObject has to be assigned as a reference to an existing game object.
 * Note: The difference between this and the ResizeRendererToLevelBounds script is that this
 *       script also resizes the assigned texture of the attached object.
 * 
 * \author Marcel Weires
 * \version 1.0
 */

public class ResizeToLevelBounds : MonoBehaviour {
	
	public GameObject levelBoundsObject;
	
	const int tileSize = 100;
	public int textureTilesPerTile = 100;
	
	SpriteRenderer spriteRenderer;
	
	// Use this for initialization
	void Start () {
		spriteRenderer = gameObject.renderer as SpriteRenderer;
		
		if (levelBoundsObject && spriteRenderer) {
			Bounds boundsOfLevel = UnityTools.Instance.GetBoundsOfGameObjectWithChildren(levelBoundsObject);
			
			float sizeOfTextureX = spriteRenderer.bounds.size.x;
			float sizeOfTextureY = spriteRenderer.bounds.size.y;
			
			float scaleTextureX = boundsOfLevel.size.x / sizeOfTextureX;
			float scaleTextureY = boundsOfLevel.size.y / sizeOfTextureY;
			
			Vector3 textureScale = gameObject.transform.localScale;
			textureScale.x = scaleTextureX;
			textureScale.y = scaleTextureY;
			gameObject.transform.localScale = textureScale;
			
			// center position	
			Vector3 texturePosition = gameObject.transform.position;
			texturePosition.x = levelBoundsObject.transform.position.x 
				+ (boundsOfLevel.size.x / 2.0f) - (sizeOfTextureX / 2.0f);
			texturePosition.y = levelBoundsObject.transform.position.y
				+ (boundsOfLevel.size.y / 2.0f) - (sizeOfTextureY / 2.0f);
			gameObject.transform.position = texturePosition;
			
			int targetTextureSizeX = (int)(boundsOfLevel.size.x * textureTilesPerTile);
			int targetTextureSizeY = (int)(boundsOfLevel.size.y * textureTilesPerTile);
			spriteRenderer.sprite.texture.Resize(
				targetTextureSizeX, targetTextureSizeY, TextureFormat.RGBA32, false);
			spriteRenderer.sprite.texture.Apply();
		}
	}
	
	public Rect GetSingleTextureTileSize() {
		return new Rect (0, 0, tileSize / textureTilesPerTile, tileSize / textureTilesPerTile);
	}
}
