using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using csDelaunay;

// splits the terrain into a set of Voronoi polygons, creates a path through the graph, and raises islands that are assigned biomes
// parts of this code has been adapted from http://forum.unity3d.com/threads/delaunay-voronoi-diagram-library-for-unity.248962/

public class CityGenerator : MonoBehaviour {
	public int minPolygons; // minimum number of polygons
	public int maxPolygons; // maximum number of polygons
	public int seed = 0;
	Terrain terrain; // the actual terrain
	float[,] pointArray; // array representing point values
	float[,] heightmap; // array representing heightmap

	// The number of polygons/sites we want
	public int polygonNumber = 0;
	
	// This is where we will store the resulting data
	private Dictionary<Vector2f, Site> sites;
	private List<Edge> edges;
	
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
		
		// But you could also create it without lloyd relaxtion and call that function later if you want
		//Voronoi voronoi = new Voronoi(points,bounds);
		//voronoi.LloydRelaxation(5);
		
		// Now retreive the edges from it, and the new sites position if you used lloyd relaxtion
		sites = voronoi.SitesIndexedByLocation;
		edges = voronoi.Edges;

		// apply polygons
		DisplayVoronoiDiagram();
		
		// reattatch array to terrain
		terrain.terrainData.SetHeights(0,0,heightmap);
		
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
		//Texture2D tx = new Texture2D(512,512);
		//foreach (KeyValuePair<Vector2f,Site> kv in sites) {
		//	tx.SetPixel((int)kv.Key.x, (int)kv.Key.y, Color.red);
		//}
		foreach (Edge edge in edges) {
			// if the edge doesn't have clippedEnds, if was not within the bounds, dont draw it
			if (edge.ClippedEnds == null) continue;
			
			DrawLine(edge.ClippedEnds[LR.LEFT], edge.ClippedEnds[LR.RIGHT]);
		}
		//tx.Apply();
		
		//this.renderer.material.mainTexture = tx;
	}
	
	// Bresenham line algorithm
	// adaption from http://forum.unity3d.com/threads/delaunay-voronoi-diagram-library-for-unity.248962/
	private void DrawLine(Vector2f p0, Vector2f p1, int offset = 0) {
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
			ChangeHeightReg(x0+offset,y0+offset,5,0f);

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
		int tw = terrain.terrainData.heightmapWidth;
		int th = terrain.terrainData.heightmapHeight;

		for (int i = x-(size-1)/2; i<x+(size-1)/2; i++) {
			for (int j = y-(size-1)/2; j<y+(size-1)/2; j++) {
				if(i>=0 && i< tw && j>=0 && j<th){
					heightmap[i,j] = height;
				}
			}
		}
	}

}
