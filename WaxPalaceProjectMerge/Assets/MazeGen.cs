﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class MazeGen : MonoBehaviour
{
    public float floorY;
    public float minX;
    public float maxX;
    public float minZ;
    public float maxZ;
    public float height;

    public float holeX_world;
    public float holeZ_world;

    private int numX = 100;
    private int numZ = 100;
    private float widthX;
    private float widthZ;

    private int numEmpty = 0;

    public NavMeshData navMesh;
    GameObject wallTilePrefab;
    public GameObject DeafAI;
    public GameObject BlindAI;
    List<int[]> added_points = new List<int[]>();
    List<int[]> all_empty_points = new List<int[]>();

    // GameObject newWall;

    public GameObject player = null;


    int[,] grid; // 0 means wall, > 0 means tunnel

    int holeX;
    int holeZ;

    Color[] wall_colors;

    // public bool start_set;

    public float startX_world;
    public float startZ_world;

    int startX;
    int startZ;


    int log2(int x) {
        int power = 1;
        int log = 0;
        while(power < x) {
            power *= 2;
            log++;
        }
        return log;
    }

    // Start is called before the first frame update
    void Start()
    {
        wallTilePrefab = Resources.Load<GameObject>("WallTile");
        widthX = (maxX - minX) / numX;
        widthZ = (maxZ - minZ) / numZ;
        int[] tmp = toGrid(holeX_world, holeZ_world);
        holeX = tmp[0];
        holeZ = tmp[1];
        tmp = toGrid(startX_world, startZ_world);
        startX = tmp[0];
        startZ = tmp[1];

        grid = new int[numX, numZ];
        for(int x = 0; x < numX; x++) {
            for(int z = 0; z < numZ; z++) {
                grid[x, z] = 0;
            }
        }

        for(int x = Mathf.Max(0, holeX - 5); x < Mathf.Min(numX, holeX + 5); x++) {
            for(int z = Mathf.Max(0, holeZ - 5); z < Mathf.Min(numZ, holeZ + 5); z++) {
                markEmpty(x, z, 1);
            }
        }

        

        wall_colors = new Color[5];
        wall_colors[0] = Color.blue;
        wall_colors[1] = Color.gray;
        wall_colors[2] = Color.green;
        wall_colors[3] = Color.black;
        wall_colors[4] = Color.red;

        StartRandomWalk(holeX, holeZ, 0);

        // ActuallyPlaceWall(-239, -86);

        if(player == null) {
            for(int x = Mathf.Max(0, startX - 5); x < Mathf.Min(numX, startX + 5); x++) {
                for(int z = Mathf.Max(0, startZ - 5); z < Mathf.Min(numZ, startZ + 5); z++) {
                    markEmpty(x, z, 1);
                }
            }
        }

        for(int x = 0; x < numX; x++) {
            for(int z = 0; z < numZ; z++) {
                if(grid[x, z] == 0) {
                    ActuallyPlaceWall(x, z, log2(grid[x, z]));
                }
                else
                {
                    if (chance(.005f))
                    {
                        Vector3 p = toWorld(x, z);
                        p = new Vector3(p.x, p.y + 3, p.z);
                        NavMesh.SamplePosition(p, out NavMeshHit hit, 100, -1);
                        if (chance(.5f))
                        {
                            GameObject generatedAI = Instantiate(DeafAI, hit.position, Quaternion.identity);
                        }
                        else
                        {
                            GameObject generatedAI = Instantiate(BlindAI, hit.position, Quaternion.identity);
                        }

                    }
                }
            }
        }

        if(player != null) {
            added_points.Clear();
            for(int x = 0; x < numX; x++) {
                for(int z = 0; z < numZ; z++) {
                    if(grid[x, z] != 0) {
                        int[] p2 = new int[2];
                        p2[0] = x;
                        p2[1] = z;
                        added_points.Add(p2);
                    }
                }
            }

            int[] p = choose_random(added_points);
            Vector3 player_pos = toWorld(p[0], p[1]) + Vector3.up * 3.0f;
            player.transform.position = player_pos;
            // ActuallyPlaceWall(x, z, log2(grid[x, z]));
        }   
    }


    bool chance(float p) {
        return Random.value < p;
    }

    int randInt(float[] p) {
        float s = 0;
        for(int i = 0; i < p.Length; i++) {
            s += p[i];
        }
        // float[] pn = new float[p.Length];
        for(int i = 0; i < p.Length; i++) {
            p[i] = p[i] / s;
        }

        float r = Random.value;
        s = 0;
        for(int i = 0; i < p.Length; i++) {
            float newS = s + p[i];
            if(r >= s && r <= newS) {
                return i;
            }
            s = newS;
        }

        Debug.Log("BAD!!!");
        return 0;
    }


    void markEmpty(int x, int z, int colorId) {
        // Debug.Log("Marking empty: " + x + "," + z);
        if(grid[x, z] == 0) {
            numEmpty++;
            int[] p = new int[2];
            p[0] = x;
            p[1] = z;
            added_points.Add(p);
            all_empty_points.Add(p);
        }
        grid[x,z] |= colorId;
    }

    int colorId(int c) {
        return 1 << c;
    }

    float distanceToNearestAnyEmpty(int qx, int qz) {
        int minDist2 = int.MaxValue;
        foreach(int[] p in all_empty_points) {
            int x = p[0];
            int z = p[1];
            int dist = (qx-x)*(qx-x) + (qz-z)*(qz-z);
            if(dist < minDist2) {
                minDist2 = dist;
            }
        }
        return Mathf.Sqrt((float)minDist2);
    }

    void stepPos(int oldx, int oldz, int d, ref int x, ref int z) {
        x = oldx;
        z = oldz;

        switch(d) {
            case 0:
                x--;
                break;
            case 1:
                x--;
                z++;
                break;
            case 2:
                z++;
                break;
            case 3:
                x++;
                z++;
                break;
            case 4:
                x++;
                break;
            case 5:
                x++;
                z--;
                break;
            case 6:
                z--;
                break;
            case 7:
                z--;
                x--;
                break;
        }
    }


    float[] distr = new float[8];

    void randomStep(ref int x, ref int z, int colorId) {
        // float[] distr = new float[8];
        for(int _d = 0; _d < 8; _d++) {
            int nx = 0;
            int nz = 0;
            stepPos(x, z, _d, ref nx, ref nz);
            distr[_d] = distanceToNearestAnyEmpty(nx, nz) + 0.0001f;

            if(!(nx >= 0 && nx < numX && nz >= 0 && nz < numZ)) {
                distr[_d] = 0;
            }
        }

        // int d = (randInt(8) * 2) % 8;
        int d = randInt(distr);

        int oldX = x;
        int oldZ = z;

        stepPos(oldX, oldZ, d, ref x, ref z);

        // if(!(x >= 0 && x < numX && z >= 0 && z < numZ)) {
        //     Debug.Log("VERY BAD!");
        // }

        markEmpty(x,z,colorId);

        switch(d) {
            case 1:
                markEmpty(x,z-1,colorId);
                markEmpty(x+1,z,colorId);
                break;
            case 3:
                markEmpty(x-1,z,colorId);
                markEmpty(x,z-1,colorId);
                break;
            case 5:
                markEmpty(x-1,z,colorId);
                markEmpty(x,z+1,colorId);
                break;
            case 7:
                markEmpty(x+1,z,colorId);
                markEmpty(x,z+1,colorId);
                break;
        }
    }

    float percentEmpty() {
        return (float)numEmpty / (float)(numX*numZ);
    }

    T choose_random<T>(List<T> xs) {
        return xs[Random.Range(0, xs.Count)];
    }

    int max_colors = 30;

    bool isStartEmpty() {
        // returns true if there is no start, or if the start is tunnel
        return player != null || grid[startX, startZ] > 0;
    }
    void StartRandomWalk(int x, int z, int color) {
        int colorId = this.colorId(color);

        Debug.Log(colorId);

        added_points.Clear();

        markEmpty(x, z, colorId);
        // float expectedSteps = 1000.0f;

        int steps = 0;
        while(steps < 1000) {
        // for(int i = 0; i < steps; i++) {
            randomStep(ref x, ref z, colorId);
            // grid[x, z] = 0;
            steps++;
        }

        Debug.Log("Generated random walk with " + steps + " steps");

        if(color + 1 < max_colors && (percentEmpty() < 0.4 || !isStartEmpty())) {
            int[] bp = choose_random(added_points);
            StartRandomWalk(bp[0], bp[1], color + 1);
        }

    }

    void ActuallyPlaceWall(int x, int z, int color) {
        Vector3 p = toWorld(x, z);
        GameObject newWall = Instantiate(wallTilePrefab, p, Quaternion.identity);
        newWall.transform.localScale = new Vector3(widthX, height, widthZ);
        // newWall.GetComponent<Renderer>().material.color = wall_colors[color];
    }

    Vector3 toWorld(int x, int z) {
        return new Vector3(minX + x*((maxX-minX)/numX), floorY + height/2.0f, minZ + z*((maxZ-minZ)/numZ));
    }

    int[] toGrid(float wx, float wz) {
        int[] g = new int[2];
        g[0] = (int)((wx - minX) / (maxX - minX) * numX);
        g[1] = (int)((wz - minZ) / (maxZ - minZ) * numZ);
        return g;
    }

    public static Vector3 RandomNavSphere(Vector3 origin, float distance, int layermask)
    {
        Vector3 randomDirection = UnityEngine.Random.insideUnitSphere * distance;

        randomDirection += origin;

        Debug.Log(layermask);

        NavMesh.SamplePosition(randomDirection, out NavMeshHit navHit, distance, layermask);


        return navHit.position;
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
