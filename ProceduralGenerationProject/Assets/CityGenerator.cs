using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using csDelaunay;

// splits the terrain into a set of Voronoi polygons, creates a path through the graph, and raises islands that are assigned biomes
// parts of this code has been adapted from http://forum.unity3d.com/threads/delaunay-voronoi-diagram-library-for-unity.248962/

public class CityGenerator : MonoBehaviour {
	public int minPolygons; // minimum number of polygons
	public int maxPolygons; // maximum number of polygons
	public int seed = 0; // random number generator
	public int connectionIts = 1; // number of iterations of connections
	public int riverSize = 5; // how big will the river gap be?
	public int bridgeSize = 5; // how big will the bridges be?
	public int noiseSize = 3; // modifier for Perlin noise
	public int noisePeriod = 80; // modifier for how often a noise cycle repeats
	Terrain terrain; // the actual terrain
	float[,] pointArray; // array representing point values
	float[,] heightmap; // array representing heightmap

	// The number of polygons/sites we want
	private int polygonNumber = 0;
	
	// This is where we will store the resulting data
	private Dictionary<Vector2f, Site> sites;
	private List<Edge> edges;
	private Vector2f startSite, endSite;
	private int tw, th;
	
	// Use this for initialization
	void Start () {
		// set seed
		Random.seed = seed;

		// pick number of polygons
		polygonNumber = Random.Range (minPolygons, maxPolygons);
		
		// get terrain
		terrain = (Terrain)gameObject.GetComponent ("Terrain");
		
		// get heightmap
		heightmap = new float[terrain.terrainData.heightmapWidth, terrain.terrainData.heightmapHeight];

		tw = terrain.terrainData.heightmapWidth;
		th = terrain.terrainData.heightmapHeight;
		
		// initialize heightmap array
		for(int i=0;i<heightmap.GetLength(0);i++){
			for(int j=0;j<heightmap.GetLength(1);j++){
				//heightmap[i,j] = 0f;
				heightmap[i,j] = 20f/terrain.terrainData.size.y;
			}
		}

		// Create your sites (lets call that the center of your polygons)
		List<Vector2f> points = CreateRandomPoint();
		
		// Create the bounds of the voronoi diagram
		// Use Rectf instead of Rect; it's a struct just like Rect and does pretty much the same,
		// but like that it allows you to run the delaunay library outside of unity (which mean also in another tread)
		Rectf bounds = new Rectf(0,0,512,512);
		
		// There is a two ways you can create the voronoi diagram: with or without the lloyd relaxation
		// Here I used it with 2 iterations of the lloyd relaxation
		Voronoi voronoi = new Voronoi(points,bounds,5);
		
		// Now retreive the edges from it, and the new sites position if you used lloyd relaxtion
		sites = voronoi.SitesIndexedByLocation;
		edges = voronoi.Edges;

		// apply polygons
		DisplayVoronoiDiagram();

		// generate maze
		for (int i=0; i<connectionIts; i++) {
			RandomConnections ();
		}

		// add noise
		heightmap = PerlinNoise (heightmap);

		// reattatch array to terrain
		terrain.terrainData.SetHeights(0,0,heightmap);

		// move player
		PlacePlayer ();
		
	}
	
	// choose seeds for the polygons
	// adaption from http://forum.unity3d.com/threads/delaunay-voronoi-diagram-library-for-unity.248962/
	private List<Vector2f> CreateRandomPoint() {
		// Use Vector2f, instead of Vector2
		// Vector2f is pretty much the same than Vector2, but like you could run Voronoi in another thread
		List<Vector2f> points = new List<Vector2f>();
		for (int i = 0; i < polygonNumber; i++) {
			points.Add(new Vector2f(Random.Range(0,terrain.terrainData.heightmapWidth), Random.Range(0,terrain.terrainData.heightmapHeight)));
		}
		
		return points;
	}

	// Here is a very simple way to display the result using a simple bresenham line algorithm
	// Just attach this script to a quad
	// adaption from http://forum.unity3d.com/threads/delaunay-voronoi-diagram-library-for-unity.248962/
	private void DisplayVoronoiDiagram() {
		foreach (Edge edge in edges) {
			// if the edge doesn't have clippedEnds, if was not within the bounds, dont draw it
			if (edge.ClippedEnds == null) continue;
			
			DrawLine(edge.ClippedEnds[LR.LEFT], edge.ClippedEnds[LR.RIGHT],0f,riverSize);
		}
	}
	
	// Bresenham line algorithm
	// adaption from http://forum.unity3d.com/threads/delaunay-voronoi-diagram-library-for-unity.248962/
	private void DrawLine(Vector2f p0, Vector2f p1, float height, int width,int offset = 0) {
		int x0 = (int)p0.x;
		int y0 = (int)p0.y;
		int x1 = (int)p1.x;
		int y1 = (int)p1.y;
		
		int dx = Mathf.Abs(x1-x0);
		int dy = Mathf.Abs(y1-y0);
		int sx = x0 < x1 ? 1 : -1;
		int sy = y0 < y1 ? 1 : -1;
		int err = dx-dy;
		
		while (true) {
			//tx.SetPixel(x0+offset,y0+offset,c);
			//heightmap[x0+offset,y0+offset] = 0f;
			ChangeHeightReg(x0+offset,y0+offset,width,height);

			if (x0 == x1 && y0 == y1) break;
			int e2 = 2*err;
			if (e2 > -dy) {
				err -= dy;
				x0 += sx;
			}
			if (e2 < dx) {
				err += dx;
				y0 += sy;
			}
		}
	}

	// modifies a size x size grid
	private void ChangeHeightReg(int x,int y, int size, float height){
		for (int i = x-(size-1)/2; i<x+(size-1)/2; i++) {
			for (int j = y-(size-1)/2; j<y+(size-1)/2; j++) {
				if(i>=0 && i< tw && j>=0 && j<th){
					heightmap[i,j] = height;
				}
			}
		}
	}

	// pick two random points, the start and end site
	public void ChooseRandomPoints(){
		// create list
		int s = sites.Count;
		Vector2f[] siteList = new Vector2f[s];
		//ArrayList siteList = new ArrayList();

		int i = 0;
		foreach (KeyValuePair<Vector2f,Site> kv in sites) {
			siteList[i] = kv.Key;
			i++;
		}

		// pick start
		int sIndex = Random.Range (0, s);
		int eIndex = Random.Range (0, s);

		// pick end
		while (sIndex == eIndex) {
			eIndex = Random.Range (0, s);
		} 

		// assign
		startSite = siteList[sIndex];
		endSite = siteList[eIndex];
	}

	// temporary solution to making an interesting maze
	// for each node, a line is drawn in a random direction until another node has been found
	public void RandomConnections(){
		// raise bridges
		foreach (KeyValuePair<Vector2f,Site> kv in sites) {
			// pick a direction
			Vector3 dir = RandomDirection();

			// create agent
			Vector3 agent = new Vector3(kv.Key.x,0,kv.Key.y);
			Vector3 end = new Vector3(0,0,0);

			// find end point
			bool riverFound = false;
			bool landFound = false;

			while(!landFound){
				// start over if near edge
				while(agent.x < 2 || agent.x > tw-2 || agent.z < 2 || agent.z > th-2){
					agent = new Vector3(kv.Key.x,0,kv.Key.y);
					dir = RandomDirection();
				}

				// add
				agent += dir;

				// check for land
				if(terrain.terrainData.GetHeight((int)agent.x,(int)agent.z) != 0f){
					// mark land as found
					if (riverFound) landFound = true;

					// mark point
					end = new Vector3(agent.x,0,agent.z);
				} else if (!riverFound){
					// check for water
					riverFound = true;
				}

			}

			// draw
			DrawLine (new Vector2f(kv.Key.x,kv.Key.y), new Vector2f(agent.x,agent.z),20f/terrain.terrainData.size.y,bridgeSize);
		}
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

	// places the player at a random node
	void PlacePlayer(){
		// save size
		int s = sites.Count;

		// iterate
		foreach (KeyValuePair<Vector2f,Site> kv in sites) {
			while(true){
				if(Random.Range(0,s) == 0){
					// move player
					Transform player = GameObject.Find("FPSController").GetComponent<Transform>();
					player.position = new Vector3 (kv.Key.x, 30f, kv.Key.y);
					return;
				}
			}
		}
	}

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
					hm[i,j] = hm[i,j]+noiseSize*Mathf.PerlinNoise(xCoord, yCoord)/noisePeriod;
				}
			}
		}
		
		// return
		return heightmap;
	}

}
