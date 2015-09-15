using UnityEngine;
using System.Collections;

// this script generates a coastline by raising the land slightly
// inspired by the algorithm suggested by in Controlled Procedural
// Terrain Generation Using Software Agents

public class CoastlineGenerator : MonoBehaviour {
	public float startTokens; // how big is the land mass?
	public float limit; // how many tokens are allowed to each agent
	public float agentRange = 10f; // how far can the attractor and repulsor be? Must be greater than 1.
	public float smoothReturnChance = 50f; // how regularly do smoothing agents return to the start?
	public float smoothAgents = 100f; // how many smoothing agents are there?
	public int seed = 0;
	public GameObject tree01;
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
		// set seed
		Random.seed = seed;

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

		// run coastline generation
		CoastlineGenerate (firstAgent);

		// smooth
		//heightmap = SmoothTerrain (heightmap);

		// add noise
		heightmap = PerlinNoise (heightmap);

		// smooth
		heightmap = SmoothTerrain (heightmap);
		
		// reattatch array to terrain
		terrain.terrainData.SetHeights(0,0,heightmap);

		// generate enviornment
		GenerateNature (heightmap);
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
				Vector3 newPoint = RandomAdjacentPoint (agent.point);//+Random.Range(0,2)*agent.direction;

				// direction
				Vector3 newDir = RandomDirection ();
				//Debug.Log(newDir);

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
				Vector3 repulDirection = RandomDirection();
				Vector3 repul = new Vector3(agent.point.x,agent.point.y,agent.point.z); // clone
				repul = repul+repulDirection*Random.Range(1f,agentRange);// translate
				
				// create attractor
				Vector3 attraDirection = repulDirection*-1;//RandomDirection();
				//while(attraDirection == repulDirection){
				//	attraDirection = RandomDirection();
				//}

				Vector3 attra = new Vector3(agent.point.x,agent.point.y,agent.point.z); // clone
				attra = attra+attraDirection*Random.Range(1f,agentRange);// translate

				// for all points surrounding the random adjacent point, score the point
				for(float j=0f;j<3f;j++){
					for(float k=0f;k<3f;k++){
						//  score if not the actual point
						if(j!=1f&&k!=1f){
							Vector3 thisPoint = new Vector3(agent.point.x+j-1,0,agent.point.z+k-1);
							pointArray[(int)j,(int)k] = ScorePoint(thisPoint,repul,attra);
							//pointArray[(int)j,(int)k] = ScorePoint(agent.point,repul,attra);
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
				
				// raise the heightmap of the heightest scoring point
				heightmap[sX+hX,sZ+hZ] = 2f/terrain.terrainData.size.y;

				// move to this point
				agent.point = new Vector3(sX+hX,0,sZ+hZ);
			}
		}
	}

	// returns a random point surrounding the initial point
	// if the point is out of bounds it returns the center of the map
	Vector3 RandomAdjacentPoint(Vector3 StartPoint){
		// get random value
		int rand = (int)Mathf.Round (Random.Range (0f, 7f));
		Vector3 newVector = new Vector3(1,0,1);
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
			newVector = new Vector3(StartPoint.x+1,0,StartPoint.z+1);
			break;

		}

		if (newVector.x < 0 || newVector.x > terrain.terrainData.heightmapWidth || newVector.z < 0 || newVector.z > terrain.terrainData.heightmapHeight) {
			newVector.x = terrain.terrainData.heightmapWidth/2;
			newVector.z = terrain.terrainData.heightmapHeight/2;
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
			xDir = Random.Range (-1f, 1f);
			zDir = Random.Range (-1f, 1f);
		} while(xDir == 0f && zDir == 0f);

		return new Vector3(xDir,0,zDir);
	}

	// checks to make sure the surrounding landmass is not raised
	bool IsSurroundedByLand(Vector3 pt){
		// save 0,0
		int sX = (int)pt.x-1;
		int sZ = (int)pt.z-1;

		// check for out of bounds
		if (sX < 0 || sZ < 0 || sX + 2 > terrain.terrainData.heightmapWidth || sZ + 2 > terrain.terrainData.heightmapHeight) {
			return false;
		}

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
		float deH = Mathf.Min(Vector3.Distance(pt,new Vector3(0,0,pt.z)),Vector3.Distance(pt,new Vector3(terrain.terrainData.heightmapWidth,0,pt.z)));

		// choose the best of the vert and horiz distance and set it to the distance to the edge of the map
		float de = Mathf.Min(deV,deH);

		// corners of the map
		/*for(int i=0;i<=1;i++){
			for(int j=0;j<=1;j++){
				de = Mathf.Min(de,Vector3.Distance(pt,new Vector3(i*terrain.terrainData.heightmapWidth,0,j*terrain.terrainData.heightmapHeight)));
			}
		}*/


		de = de * de;

		// return the calculation
		return dr - da;//+3*de;
	}

	// uses perlin noise to raise the terrain
	float[,] PerlinNoise(float[,] hm){
		// fractal origins
		float xOrg = Random.Range (0, .1f);
		float yOrg = Random.Range (0, .1f);

		// iterate through maps
		for(int i=0;i<hm.GetLength(0);i++){
			for(int j=0;j<hm.GetLength(1);j++){
				// make sure this is part of the land mass
				if(hm[i,j] > 0){
					// get perlin noise coordinates
					float xCoord = xOrg + (float)i / ((float)hm.GetLength(0)/25);
					float yCoord = yOrg + (float)j / ((float)hm.GetLength(1)/25);

					// multiply the current height by a multiple of the perlin noise
					hm[i,j] = hm[i,j]+3*Mathf.PerlinNoise(xCoord, yCoord)/80;
				}
			}
		}

		// return
		return heightmap;
	}

	// smooths the terrain by randomly walking around the landmass
	float[,] SmoothTerrain(float[,] hm){
		// for each agent...
		for (int j=0; j<smoothAgents; j++) {
			// create starting point and make sure starting point is on the landmass.
			Vector3 startingPoint = new Vector3 (terrain.terrainData.heightmapWidth / 2, 0, terrain.terrainData.heightmapHeight / 2);
			/*do {
				startingPoint = new Vector3 (Random.Range(1,terrain.terrainData.heightmapWidth-1), 0, Random.Range(1,terrain.terrainData.heightmapHeight-1));
			} while(hm[(int)startingPoint.x,(int)startingPoint.z] == 0);*/

			// clone starting point
			Vector3 activePoint = new Vector3 (startingPoint.x, 0, startingPoint.z);

			for (int i=0; i<startTokens; i++) {
				// weight average
				float wAvg = GetVonNeumannAverage (hm, activePoint);

				// set height
				hm [(int)activePoint.x, (int)activePoint.z] = wAvg;

				// chance to return to start
				if (Mathf.Round (Random.Range (0, smoothReturnChance)) == 0) {
					activePoint.x = startingPoint.x;
					activePoint.z = startingPoint.z;
				} else {
					// move to neighboring point
					activePoint = RandomAdjacentPoint (activePoint);
				}
			}
		}

		// return
		return hm;
	}

	// returns a weighted average of the extended von Neumann neighborhood
	float GetVonNeumannAverage(float[,] hm, Vector3 point){
		// declare average
		float avg = 0;

		// get horizontal sum
		for(int i=-2;i<=2;i++){
			if(point.x+i>0 && point.x+i<terrain.terrainData.heightmapWidth){
				avg += hm[(int)point.x+i,(int)point.z];
			}
		}

		// get verticle sum
		for (int i=-2; i<=2; i++) {
			if(point.z+i>0 && point.z+i<terrain.terrainData.heightmapHeight){
				avg += hm[(int)point.x,(int)point.z+i];
			}
		}

		// add center value (making the pointed weighted 3x the value of the other points)
		avg += hm[(int)point.x,(int)point.z];

		// divide by 11
		if (avg > 0) {
			avg = avg / 11;
		}

		// return
		return avg;
	}

	// generates nautre
	void GenerateNature(float[,] hm){
		// wipe old trees
		terrain.terrainData.treeInstances = new TreeInstance[0];

		// iterate through vertecies
		for(int i=0;i<terrain.terrainData.heightmapWidth;i++){
			for(int j=0;j<terrain.terrainData.heightmapHeight;j++){
				// TREES
				// is it above sea level?
				if(terrain.terrainData.GetHeight(i,j) > 1){
					// is it relatively flat?
					Vector3 thisPoint = new Vector3(i,0,j);
					float vna = GetVonNeumannAverage(hm,thisPoint);

					if(vna/hm[i,j]< 1.5 && hm[i,j]/vna < 1.5){
						// chance to generate tree
						if(Mathf.Round(Random.Range(0,15))==0){
							// generate trees
							TreeInstance treeInst = new TreeInstance();
							treeInst.prototypeIndex = Random.Range(0,2);
							Vector3 position = new Vector3(((float)i)/terrain.terrainData.heightmapWidth,0,((float)j)/terrain.terrainData.heightmapHeight);
							//Vector3 position = new Vector3(i,0,j);
							position.y = terrain.terrainData.GetInterpolatedHeight((float)i/terrain.terrainData.heightmapWidth, (float)j/terrain.terrainData.heightmapHeight) / terrain.terrainData.size.y;
							treeInst.position = position;
							float sizeMod = Random.Range(.8f,1.2f);
							treeInst.heightScale = sizeMod;
							treeInst.widthScale = sizeMod;

							float colorMod = Random.Range(.8f,1f);
							treeInst.color = new Color (colorMod, colorMod, colorMod);
							treeInst.lightmapColor = new Color (1, 1, 1);
							terrain.AddTreeInstance(treeInst);

							//print(position);

						}
					}

				}
			}
		}

		// flush and print
		terrain.Flush();
		print(terrain.terrainData.treeInstances.Length); //does show trees are being added to the treeInstances array
	}
}
