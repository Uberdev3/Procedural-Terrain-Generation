using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
[ExecuteInEditMode]
public class ProceduralTerrainGenerator : MonoBehaviour
{
    [SerializeField] private float seaLevel;
    [SerializeField] private GameObject[] islandSpawnableObjects;
    [SerializeField] private float[] islandSpawnObjectsTerrainSlopeIntensities;
    [SerializeField] private float[] islandSpawnObjectsSpawnPercentages;
    //[SerializeField] private game
    [SerializeField] private GameObject plane;
    [SerializeField] private Material material;
    Mesh mesh;
    const int MAX_VERT_COUNT = 65536;
    [SerializeField] private int resolution;
    Vector3[] vertices;
    Vector2[] uv;
    int[] triangles;
    [SerializeField] private int xStartSize;
    [SerializeField] private int zStartSize;
    [SerializeField] private Vector3 planeSize;
    [SerializeField] private int seed;
    [SerializeField] private float amplitude;
    [SerializeField] private float scale;
    [SerializeField] private int perlinLayers;
    [SerializeField] private float LayerScaleMultiplication;
    [SerializeField] private float LayerAmplitudeDivision;
    [SerializeField] private AnimationCurve fallOffCurve;
    [SerializeField] private float fallOffIntensity;
    GameObject spawnableContainer;
    float previousAmplitude;
    Dictionary<int, float[]> PerlinLayerDictionary = new Dictionary<int, float[]>();
    public void CreatePlane()
    {
        //destroy spawned objects like trees, rocks, etc
        if (spawnableContainer != null) DestroyImmediate(spawnableContainer);
        mesh = new Mesh();
        //generate vertice positions
        vertices = new Vector3[(xStartSize * resolution + 1 ) * (zStartSize * resolution + 1)];
        int i = 0;
        for (float z = 0; z <= zStartSize * resolution; z++)
        {
            for (float x = 0; x <= xStartSize * resolution; x++)
            {
                vertices[i] = new Vector3(x / resolution, 0, z / resolution);
                i++;
            }
        }
        int vert = 0;
        int tris = 0;         
        triangles = new int[xStartSize * zStartSize * 6 * resolution * resolution];
        for (int z = 0; z < zStartSize * resolution; z++)
        {
            for (int x = 0; x < xStartSize * resolution; x++)
            {
                // Generate vertices and triangles...
                triangles[tris + 0] = vert;
                triangles[tris + 1] = vert + xStartSize * resolution + 1;
                triangles[tris + 2] = vert + 1;
                triangles[tris + 3] = vert + 1;
                triangles[tris + 4] = vert + xStartSize * resolution + 1;
                triangles[tris + 5] = vert + xStartSize * resolution + 2;   
                tris += 6;
                vert++;
            }
            vert++;
        }
        //create uv
        uv = new Vector2[vertices.Length];
        for (int u = 0; u < vertices.Length; u++)
        {
            uv[u] = new Vector2(vertices[u].x / xStartSize, vertices[u].z / zStartSize);
        }
        //update mesh
        UpdateMesh(plane);
        plane.transform.localScale = planeSize;
    }
    public void CreateTerrain()
    {
        PerlinLayerDictionary.Clear();
        //destroy all spawned objects like trees, rocks, etc
        if (spawnableContainer != null) DestroyImmediate(spawnableContainer);
        //reset scale to one to hinder complications
        plane.transform.localScale = Vector3.one;
        for (int l = 1; l <= perlinLayers; l++)
        {
            //generate perlin noises
            float[] newPerlinNoise;
            if (l == 1)
            {
                newPerlinNoise = Noise.PerlinNoise(seed, xStartSize, zStartSize, amplitude, scale, resolution);
            }
            else
            {
                newPerlinNoise = Noise.PerlinNoise(seed, xStartSize, zStartSize, amplitude / (LayerAmplitudeDivision * l), scale * (LayerScaleMultiplication * l), resolution);
            }
            //Debug.Log("newPerlinNoise Length: " + newPerlinNoise.Length); 
            PerlinLayerDictionary.Add(l, newPerlinNoise);
        }
        //loop through each vertice and get the y offset generated by all the perlin layers
        int i = 0;
        for (int z = 0; z <= zStartSize * resolution; z++)
        {
            for (int x = 0; x <= xStartSize * resolution; x++)
            {
                float yOffset = 0;
                for (int l = 1; l <= perlinLayers; l++)
                {
                    
                        yOffset += PerlinLayerDictionary[l][i];
                    
                    
                    
                }
                vertices[i] = new Vector3(x, yOffset * resolution, z) / resolution;
                //generate fall off
                Vector2 currentPosition = new Vector2(vertices[i].x + plane.transform.position.x, vertices[i].z + plane.transform.position.z);
                float divisionAmount = ( ((xStartSize + zStartSize)) / resolution); 
                float distanceToCentre = Vector2.Distance(currentPosition, new Vector2(plane.transform.position.x, plane.transform.position.z)
                    + (new Vector2(xStartSize, zStartSize) / 2));
                float result = fallOffCurve.Evaluate(distanceToCentre / divisionAmount);
                
                vertices[i].y = yOffset - (amplitude * 0.5f);
                //add fall off
                vertices[i].y -= result * fallOffIntensity;

                i++;

            }
        }
        plane.transform.localScale = planeSize;
       
        UpdateMesh(plane);
        if(islandSpawnableObjects.Length > 0)
        GenerateIslandSpawnables();
    }
    void GenerateIslandSpawnables()
    {
        //generate island spawnables
        Mesh mesh = plane.GetComponent<MeshFilter>().sharedMesh;
        Vector3[] normals = mesh.normals;
        //destroy existing spawnables
        if (spawnableContainer != null) DestroyImmediate(spawnableContainer);
        GameObject parent = new GameObject("SpawnableContainer");
        System.Random prng = new System.Random(seed);
        for (int i = 0; i < vertices.Length; i++)
        {
            if (Mathf.Abs(vertices[i].y * planeSize.y + plane.transform.position.y) > Mathf.Abs(seaLevel)) continue;
                Vector3 normal = normals[i];
            GameObject gameObjectToSpawn = null;
            int maxSpawnChance = 100;
            int selectedIndex = 0;
            
            for(int s = 0; s < islandSpawnObjectsSpawnPercentages.Length; s++)
            {
                int random = prng.Next(0, maxSpawnChance);
                if (islandSpawnObjectsSpawnPercentages[s] >= random)
                {
                    gameObjectToSpawn = islandSpawnableObjects[s];
                    selectedIndex = s;
                }
            }
            // Check if normal is pointing upwards
            if (Vector3.Dot(normal, Vector3.up) * islandSpawnObjectsTerrainSlopeIntensities[selectedIndex] > 0.7f && gameObjectToSpawn != null)
            {
                Vector3 spawnPos = new Vector3( vertices[i].x * planeSize.x, vertices[i].y * planeSize.y, vertices[i].z * planeSize.z) + plane.transform.position;

                // Instantiate object at the vertex position, oriented to the normal
                GameObject spawnedGameObject = Instantiate(gameObjectToSpawn, spawnPos, Quaternion.FromToRotation(Vector3.up, normal));
                spawnedGameObject.transform.parent = parent.transform;
            }
        }
        parent.transform.parent = plane.transform;
        spawnableContainer = parent;
    }
    private void Update()
    {
        if (amplitude != previousAmplitude) CreateTerrain();
        previousAmplitude = amplitude;
    }
    void UpdateMesh(GameObject gameObject)
    {
        //update mesh triangles, vertices, uv and recalculate values
        gameObject.GetComponent<MeshFilter>().mesh = mesh;
        
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();
        gameObject.GetComponent<MeshRenderer>().material = material;
        
    }
}