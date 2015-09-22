using UnityEngine;
using System.Collections;

// This simple script fills a terrain with perlin noise.  This is the most simple form of procedural generation
// I will be coding and was completed primarily to become familiar with Unity's terrain API

public class PerlinGenerator : MonoBehaviour {
	Terrain terrain;

	// Use this for initialization
	void Start () {
		// get terrain and size of terrain
		terrain = (Terrain)gameObject.GetComponent ("Terrain");
		//Vector3 tSize = terrain.terrainData.size;
		//Debug.Log (tSize);

		// set perlin noise origin coordinates
		float xOrg = Random.Range (0, .1f);
		float yOrg = Random.Range (0, .1f);

		// get heightmap
		float[,] heightmap = new float[terrain.terrainData.heightmapWidth, terrain.terrainData.heightmapHeight];

		// fill array with perlin noise values
		for(int i=0;i<heightmap.GetLength(0);i++){
			for(int j=0;j<heightmap.GetLength(1);j++){
				float xCoord = xOrg + (float)i / ((float)heightmap.GetLength(0)/5);
				float yCoord = yOrg + (float)j / ((float)heightmap.GetLength(1)/5);
				heightmap[i,j] = Mathf.PerlinNoise(xCoord, yCoord);
			}
		}

		// reattatch array to terrain
		terrain.terrainData.SetHeights(0,0,heightmap);
	}
	
	// Update is called once per frame
	void Update () {

	}
}
