using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PlayerController : MonoBehaviour
{
    public Tilemap tilemap;
    public Tile black;
    public GameObject camera;
    private int spacePress;
    private IEnumerator<Vector3Int> dit;
    private int maxx, maxy, minx, miny;
    
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("gamestart");
        dit = GenDiamond().GetEnumerator();
        maxx=0; maxy=0; minx=0; miny=0;
        spacePress = 0;
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKey(KeyCode.Space)){
            Debug.Log("spacebar: " + spacePress);
            dit.MoveNext();
            Debug.Log(dit.Current);
            int currx=dit.Current.x,curry=dit.Current.y;
            maxx=Math.Max(maxx,currx);
            maxy=Math.Max(maxy,curry);
            minx=Math.Min(minx,currx);
            miny=Math.Min(miny,curry);
            AdjustCamera();
            tilemap.SetTile(dit.Current,black);
            spacePress+=1;
            
        }
    }

    void AdjustCamera(){
        camera.GetComponent<Camera>().orthographicSize=Math.Max(5,Math.Max( maxx-minx, maxy-miny ) *0.7f);
        camera.transform.position = new Vector3((maxx+minx)/2.0f + 0.5f,(maxy+miny)/2.0f + 0.5f,-10);
    }
    // Vector3Int getNextSpace(Vector3Int start){
    //     int n = 0;
    //     while(){
    //         int dist = n / 4;
    //         int side = n % 4;
    //         int pos = 2*n**2+2*n+1;
    //     }
    // }

    IEnumerable<Vector3Int> GenDiamond(){
        yield return new Vector3Int(0,0);
        int ring=0;
        int x=0,y=0;
        while(true){
            ring++;x=0;y=ring;
            for(int i=0;i<ring;i++){
                yield return new Vector3Int(x++,y--);
            }
            for(int i=0;i<ring;i++){
                yield return new Vector3Int(x--,y--);
            }
            for(int i=0;i<ring;i++){
                yield return new Vector3Int(x--,y++);
            }
            for(int i=0;i<ring;i++){
                yield return new Vector3Int(x++,y++);
            }
        }
    }
}
