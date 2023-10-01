using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public struct Factori {
    public Vector3Int position;
    public string type;
}

[Serializable]
public struct FacType{
    public string key;
    public Tile val;
}
public class PlayerController : MonoBehaviour
{
    public Tilemap tilemap;
    public Tilemap factorymap;
    public Tilemap factoryui;
    public Tile black;
    public GameObject camera;
    private int spacePress;
    private IEnumerator<Vector3Int> dit;
    private int maxx, maxy, minx, miny;
    
    List<Factori> factories;
    public List<FacType> facTypes;
    Dictionary<string,Tile> factoryTypes;
    Vector3Int? previousTile;
    // Start is called before the first frame update
    void Start()
    {
        factoryTypes = new Dictionary<string,Tile>{};
        foreach (FacType item in facTypes)
            factoryTypes.Add(item.key,item.val);
        //Debug.Log(factoryTypes.Keys);
        Debug.Log("gamestart");
        dit = GenDiamond().GetEnumerator();
        maxx=0; maxy=0; minx=0; miny=0;
        spacePress = 0;
        previousTile = null;
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = Camera.main.nearClipPlane;
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(mousePos);
        Vector3Int cellPosition = factoryui.WorldToCell(worldPosition);
        Debug.Log(factoryTypes.Keys);
        if(previousTile.HasValue){
            factoryui.SetTile(previousTile.Value,null);
        }
        factoryui.SetTile(cellPosition,factoryTypes["liner"]);
        previousTile = cellPosition;

        if(Input.GetMouseButtonDown(0)){
            factories.Add(new Factori{position=cellPosition,type="liner"});
            factorymap.SetTile(cellPosition,factoryTypes["liner"]);
        }

        if(Input.GetKeyDown(KeyCode.Space)){
            Debug.Log("spacebar: " + spacePress);
            dit.MoveNext();
            Debug.Log(dit.Current);
            MakeSpace(dit.Current);
            spacePress+=1;            
        }

        
    }

    void MakeSpace(Vector3Int place){
        int currx=place.x,curry=place.y;
        maxx=Math.Max(maxx,currx);
        maxy=Math.Max(maxy,curry);
        minx=Math.Min(minx,currx);
        miny=Math.Min(miny,curry);
        AdjustCamera();
        tilemap.SetTile(place,black);
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
            for(int i=0;i<ring;i++) yield return new Vector3Int(x++,y--);
            for(int i=0;i<ring;i++) yield return new Vector3Int(x--,y--);
            for(int i=0;i<ring;i++) yield return new Vector3Int(x--,y++);
            for(int i=0;i<ring;i++) yield return new Vector3Int(x++,y++);
        }
    }
}
