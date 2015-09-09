using UnityEngine;
using System.Collections;

// this script generates a coastline by raising the land slightly
// inspired by the algorithm suggested by in Controlled Procedural
// Terrain Generation Using Software Agents

public class CoastlineGenerator : MonoBehaviour {
	public float startTokens; // how big is the land mass?
	public float limit; // how many tokens are allowed to each agent
	public float agentRange = 10; // how far can the attractor and repulsor be? Must be greater than 1.
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
		// get terrain
		terrain = (Terrain)gameObject.GetComponent ("Terrain");
		
		// get heightmap
		heightmap = new float[terrain.terrainData.heightmapWidth, terrain.terrainData.heightmapHeight];

		// create array to hold point values.  Points with the highest values will be raised
		pointArray = new float[3,3];

		// initialize heightmap array
		for(int i=0;i<heightmap.GetLength(0);i++){
			for(int j=0;j<heightmap.GetLength(1);j++){
				heightmap[i,j] = 0f;
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
				Vector3 newPoint = RandomAdjacentPoint (agent.point)+Random.Range(0,5)*agent.direction;

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
				// check if the point is surrounded by land
				while(IsSurroundedByLand(agent.point)){
					agent.point = agent.point+agent.direction;
				}

				// create repulsor
				Vector3 repul = new Vector3(agent.point.x,agent.point.y,agent.point.z); // clone
				repul = repul+agent.direction*Random.Range(1,agentRange);// translate
				
				// create attractor
				Vector3 attra = new Vector3(agent.point.x,agent.point.y,agent.point.z); // clone
				attra = attra-agent.direction*Random.Range(1,agentRange);// translate

				// for all points surrounding the random adjacent point, score the point
				for(float j=0f;j<3f;j++){
					for(float k=0f;k<3f;k++){
						//  score if not the actual point
						if(j!=1f&&k!=1f){
							pointArray[(int)j,(int)k] = ScorePoint(agent.point,repul,attra);
						}
					}
				}

				// raise the highest scoring surrounding point
				// save pointArray coordinates of highest scoring point
				int hX = 0;
				int hZ = 0;

				// save 0,0 point
				int sX = (int)agent.point.x-1;
				int sZ = (int)agent.point.z-1;
				
				// find the highest point
				for(int j=0;j<3;j++){
					for(int k=0;k<3;k++){
						// check that it is higher than the previously saved point
						if(pointArray[j,k]>pointArray[hX,hZ]){
							hX = j;
							hZ = k;
						}
					}
				}
				
				// raise the heightmap of the heightest point, then set the point array to -inf so it is not raised again
				heightmap[sX+hX,sZ+hZ] = .01f;

				// move to this point
				agent.point = new Vector3(sX+hX,0,sZ+hZ);
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

	// checks to make sure the surrounding landmass is not raised
	bool IsSurroundedByLand(Vector3 pt){
		// save 0,0
		int sX = (int)pt.x-1;
		int sZ = (int)pt.z-1;

		// iterate through adjacent points
		for(int j=0;j<3;j++){
			for(int k=0;k<3;k++){
				if(heightmap[sX+j,sZ+k]==0){
					// this point is sea level
					return false;
				}
			}
		}

		// no points were at sea level
		return true;
	}
	
	// scores a point
	float ScorePoint(Vector3 pt, Vector3 repulsor, Vector3 attractor){
		// square of distance between pt and attractor
		float da = Vector3.Distance (pt, attractor);
		da = da * da;

		// square of distance between pt and repulsor
		float dr = Vector3.Distance (pt, repulsor);
		dr = dr * dr;

		// vertical distance to the edge of the map
		float deV = Mathf.Min(Vector3.Distance(pt,new Vector3(pt.x,0,0)),Vector3.Distance(pt,new Vector3(pt.x,0,terrain.terrainData.heightmapHeight)));

		// horizontal distance to the edge of the map;
		float deH = Mathf.Min(Vector3.Distance(pt,new Vector3(0,0,pt.z)),Vector3.Distance(pt,new Vector3(terrain.terrainData.heightmapWidth,0,pt.z)));;

		// choose the best of the vert and horiz distance and set it to the distance to the edge of the map
		float de = Mathf.Min(deV,deH);

		// return the calculation
		return dr-da+3*de;
	}
}
