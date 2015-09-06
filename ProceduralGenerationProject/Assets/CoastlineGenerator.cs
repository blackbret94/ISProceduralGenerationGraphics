using UnityEngine;
using System.Collections;

// this script generates a coastline by raising the land slightly

public class CoastlineGenerator : MonoBehaviour {
	public float startTokens; // how big is the land mass?
	public float limit; // how many tokens are allowed to each agent
	Terrain terrain; // the actual terrain
	float[,] pointArray; // array representing point values
	float[,] heightmap; // array representing heightmap

	// agents do the work
	public struct Agent {
		public Vector3 point; // location of the agent
		public float tokens; // the number of verticies it is responsible for
		public Vector3 direction; // the direction it goes


		// constructor
		public Agent(Vector3 p,float t, Vector3 d){
			point = p;
			tokens = t;
			direction = d;
		}
	}

	// Use this for initialization
	void Start () {
		// get terrain and size of terrain
		terrain = (Terrain)gameObject.GetComponent ("Terrain");
		Vector3 tSize = terrain.terrainData.size;
		Debug.Log (tSize);
		
		// set perlin noise origin coordinates
		//float xOrg = Random.Range (0, .1f);
		//float yOrg = Random.Range (0, .1f);
		
		// get heightmap
		heightmap = new float[terrain.terrainData.heightmapWidth, terrain.terrainData.heightmapHeight];

		// create array to hold point values.  Points with the highest values will be raised
		pointArray = new float[terrain.terrainData.heightmapWidth, terrain.terrainData.heightmapHeight];

		// initialize arrays
		for(int i=0;i<heightmap.GetLength(0);i++){
			for(int j=0;j<heightmap.GetLength(1);j++){
				heightmap[i,j] = 0f;
				pointArray[i,j] = 0f;
			}
		}

		// create first agent
		Agent firstAgent = new Agent (new Vector3 (terrain.terrainData.heightmapWidth / 2, 0, terrain.terrainData.heightmapHeight / 2), startTokens, RandomDirection ());

		// run
		CoastlineGenerate (firstAgent);
		
		// reattatch array to terrain
		terrain.terrainData.SetHeights(0,0,heightmap);
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	// this is the method agents use to do their work
	void CoastlineGenerate(Agent agent){
		if (agent.tokens >= limit) {
			// create 2 child agents
			for (int i=0; i<2; i++) {
				// point
				Vector3 newPoint = RandomAdjacentPoint (agent.point);

				// direction
				Vector3 newDir = RandomDirection ();

				// create agent
				Agent newAgent = new Agent (newPoint, Mathf.Floor (agent.tokens / 2), newDir);

				// run recursively
				CoastlineGenerate (newAgent);
			}
		} else { 
			// for each token
			for(int i=0;i<agent.tokens;i++){
				// pick a random adjacent point
				Vector3 adjPoint = RandomAdjacentPoint(agent.point);

				// for all points surrounding the random adjacent point, raise the value of each point by 1
				for(float j=0f;j<3f;j++){
					for(float k=0f;k<3f;k++){
						// raise if this is not the actual point
						if(j!=1f&&k!=1f){
							// x
							int xx =  Mathf.FloorToInt(adjPoint.x+(j-1f));

							// z
							int zz =  Mathf.FloorToInt(adjPoint.z+(k-1f));

							pointArray[xx,zz]++;
						}
					}
				}

				// REPLACE THIS WITH A PRIORITY QUEUE LATER
				// save highest points
				int hX = 0;
				int hZ = 0;

				// find the highest point
				for(int j=0;j<heightmap.GetLength(0);j++){
					for(int k=0;k<heightmap.GetLength(1);k++){
						if(pointArray[j,k]>pointArray[hX,hZ]){
							hX = j;
							hZ = k;
						}

					}
				}

				// raise the heightmap of the heightest point, then set the point array to -inf so it is not raised again
				heightmap[hX,hZ] = .1f;
				pointArray[hX,hZ] = int.MinValue;
			}
		}
	}

	// returns a random point surrounding the initial point
	Vector3 RandomAdjacentPoint(Vector3 StartPoint){
		// get random value
		int rand = (int)Mathf.Floor (Random.Range (0, 7));
		Vector3 newVector = new Vector3(0,0,0);
		switch (rand) {
			//-1,-1
			case 0:
			newVector = new Vector3(StartPoint.x-1,0,StartPoint.z-1);
			break;

			//0,-1
		case 1:
			newVector = new Vector3(StartPoint.x,0,StartPoint.z-1);
			break;

			//1,-1
		case 2:
			newVector = new Vector3(StartPoint.x+1,0,StartPoint.z-1);
			break;

			//-1,0
		case 3:
			newVector = new Vector3(StartPoint.x-1,0,StartPoint.z);
			break;

			//1,0
		case 4:
			newVector = new Vector3(StartPoint.x+1,0,StartPoint.z);
			break;

			//-1,1
		case 5:
			newVector = new Vector3(StartPoint.x-1,0,StartPoint.z+1);
			break;

			//0,1
		case 6:
			newVector = new Vector3(StartPoint.x,0,StartPoint.z+1);
			break;

			//1,1
		case 7:
			newVector = new Vector3(StartPoint.x+1,0,StartPoint.z=1);
			break;

		}

		// return
		return newVector;
	}

	// returns a random direction on the XZ plane
	Vector3 RandomDirection(){
		float xDir;
		float zDir;

		// repeat, to make sure there IS a direction
		do{
			xDir = Random.Range (0, 2) - 1;
			zDir = Random.Range (0, 2) - 1;
		} while(xDir == 0 && zDir == 0);

		return new Vector3(xDir,0,zDir);
	}
}
