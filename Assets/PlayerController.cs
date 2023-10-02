using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Factori {
    public Vector3Int position;
    public string type;
    public Vector3Int dir;
    public bool active = true;
}

[Serializable]
public struct FacType{
    public string key;
    public Tile val;
}
public class PlayerController : MonoBehaviour
{
    public Tilemap tilemap;
    public class DiamondFactory : Factori
    {
        private IEnumerator<Vector3Int> DiamondEnum;
        private Vector3Int StartPos;
        private PlayerController Pc;
        public DiamondFactory(Vector3Int startPos, Vector3Int direction, PlayerController pc)
        {
            DiamondEnum = GenDiamond().GetEnumerator();
            StartPos = startPos;
            position = startPos;
            type = "diamond";
            dir = direction;
            Pc = pc;
        }

        public void Move() {
            if(false){
                DiamondEnum.MoveNext();
                position = DiamondEnum.Current + StartPos;
            }else{
                while(Pc.tilemap.GetTile(position)==Pc.black){
                    DiamondEnum.MoveNext();
                    position = DiamondEnum.Current + StartPos;
                }
            }
        }
    }

    public TMP_Text totalScoreDisplay;
    public Tilemap factorymap;
    public Tilemap factoryui;
    public Tile black;
    public Tile white;
    public GameObject camera;
    public GameObject square;
    private int spacePress;
    private IEnumerator<Vector3Int> dit;
    private int maxx, maxy, minx, miny;
    
    List<Factori> factories;
    public List<FacType> facTypes;
    Dictionary<string,Tile> factoryTypes;
    Vector3Int? previousTile;
    float timer;
    public float interval = 5;
    int currDir = 0;
    string currMach = "";
    UInt64 totalSpace = 1;
    int goalSpace = 100;
    int localSpace = 1;
    public int phase = 0;
    int cutscene = 0;
    int iterations = 0;
    readonly Vector3Int[] dires = new Vector3Int[]{new (0,1,0), new (1,0,0), new(0,-1,0), new (-1,0,0)};
    // Start is called before the first frame update
    void Start()
    {
        factoryTypes = new Dictionary<string,Tile>{};
        factories = new List<Factori>();
        foreach (FacType item in facTypes)
            factoryTypes.Add(item.key,item.val);
        //Debug.Log(factoryTypes.Keys);
        Debug.Log("gamestart");
        dit = GenDiamond().GetEnumerator();
        maxx=0; maxy=0; minx=0; miny=0;
        spacePress = 0;
        previousTile = null;
        Whiteout();
        //cutscene = 1;
        //StartCoroutine(ZoomTransition());
    }

    // Update is called once per frame
    void Update()
    {
        if(cutscene>0){
            switch(cutscene){
                case 1:
                    
                    break;
            }
        }else{
            Vector3 mousePos = Input.mousePosition;
            mousePos.z = Camera.main.nearClipPlane;
            Vector3 worldPosition = Camera.main.ScreenToWorldPoint(mousePos);
            Vector3Int cellPosition = factoryui.WorldToCell(worldPosition);
            //Debug.Log(factoryTypes.Keys);
            if(previousTile.HasValue){
                factoryui.SetTile(previousTile.Value,null);
            }
            if(currMach!=""){
                factoryui.SetTile(cellPosition,factoryTypes[currMach]);
                factoryui.SetTransformMatrix(cellPosition,Matrix4x4.Rotate(Quaternion.FromToRotation(dires[0],dires[currDir])));
            }
            //Debug.Log(dires[currDir]);
            //float angle = Mathf.Atan2(dires[currDir].x, dires[currDir].y) * Mathf.Rad2Deg - 90f;
            previousTile = cellPosition;

            if(Input.GetMouseButtonDown(0)){
                if(tilemap.GetTile(cellPosition)==black){
                    switch(currMach){
                        case "liner":
                            factories.Add(new Factori{position=cellPosition,type="liner",dir=dires[currDir]});
                            factorymap.SetTile(cellPosition,factoryTypes["liner"]);
                            factorymap.SetTransformMatrix(cellPosition,Matrix4x4.Rotate(Quaternion.FromToRotation(dires[0],dires[currDir])));
                            break;
                        case "diamond":
                            factories.Add(new DiamondFactory(cellPosition, new Vector3Int(0, 1, 0),this));
                            factorymap.SetTile(cellPosition, factoryTypes["diamond"]);
                            break;
                    }        
                }
            }

            if (Input.GetKeyDown(KeyCode.Alpha1)) {
                currMach = "diamond";
            }
            if (Input.GetKeyDown(KeyCode.Alpha2)) {
                currMach = "liner";
            }
            
            
            if(Input.GetKeyDown(KeyCode.Space)){
                //Debug.Log("spacebar: " + spacePress);
                
                dit.MoveNext();
                //Debug.Log(dit.Current);
                while(tilemap.GetTile(dit.Current)==black){
                    dit.MoveNext();    
                }
                MakeSpace(dit.Current);
                spacePress+=1;            
            }

            if(Input.GetKeyDown(KeyCode.R)){
                currDir = (currDir+1)%4;
            }
        }
    }

    void FixedUpdate(){
        timer += Time.fixedDeltaTime;
        while(timer >= interval)
        {
            DoFactories();
            //Debug.Log("done factori");
            timer -= interval;
        }
    }
    
    void DoFactories(){
        for(int i=0;i<factories.Count;i++){
            Factori machine = factories[i];
            //if (!machine.active)
            //    continue;
            switch(machine.type){
                case "liner":
                    factorymap.SetTile(machine.position,null);
                    machine.position = machine.position + machine.dir;
                    factorymap.SetTile(machine.position,factoryTypes["liner"]);
                    factorymap.SetTransformMatrix(machine.position,Matrix4x4.Rotate(Quaternion.FromToRotation(dires[0],machine.dir)));
        
                    MakeSpace(machine.position);
                    break;
                case "diamond":
                    factorymap.SetTile(machine.position,null);
                    ((DiamondFactory) machine).Move();
                    factorymap.SetTile(machine.position,factoryTypes["diamond"]);
                    MakeSpace(machine.position);
                    break;
                default:
                    break;
            }
            // if(Math.Abs(machine.position.x) > 50 || Math.Abs(machine.position.y)>50){
            //     machine.active=false;
            //     factorymap.SetTile(machine.position,null);
            // }
                    
        }
    }
    void MakeSpace(Vector3Int place){
        if(tilemap.GetTile(place)!=black){
        int currx=place.x,curry=place.y;
        

        maxx=Math.Max(maxx,currx);
        maxy=Math.Max(maxy,curry);
        minx=Math.Min(minx,currx);
        miny=Math.Min(miny,curry);
        tilemap.SetTile(place,black);
        if(-50<=currx && currx<=50 && -50<=curry && curry<=50 && cutscene==0){
            if(localSpace<10000){
                //totalSpace++;
                
                localSpace++;

                AdjustCamera();
                UpdateScore();
                if(localSpace>=goalSpace){
                    phase+=1;
                    switch(phase){
                        case 1:
                            goalSpace = 1000;
                            //camera.GetComponent<Camera>().orthographicSize=50;
                            break;
                        case 2:
                            goalSpace = 10000;
                            break;
                        case 3:
                            goalSpace = 100;
                            localSpace = 1;
                            phase = 0;
                            cutscene = 1;
                            iterations += 1;
                            dit = GenDiamond().GetEnumerator();
                            StartCoroutine(ZoomTransition());
                            break;
                    } 
                }
            }
        }
        }
    }

    void UpdateScore(){
        string bigZeros="";
        for(int i=0;i<iterations;i++){
            bigZeros +="0000";
        }
        totalScoreDisplay.text = localSpace.ToString() + bigZeros + " / " + goalSpace.ToString() + bigZeros;
    }

    IEnumerator ZoomTransition(){
        float fadeTime = 5;
        float currFade = 0;
        while(currFade < fadeTime){
            square.GetComponent<SpriteRenderer>().color = new Color(0,0,0,Mathf.SmoothStep(0,1,currFade/fadeTime));
            currFade+=Time.deltaTime;
            //Debug.Log(currFade);
            yield return null;
        }
        factories.Clear();
        Whiteout();
        square.GetComponent<SpriteRenderer>().color = new Color(0,0,0,0);
        float zoomTime = 5;
        float currZoom = 0;
        while(currZoom < zoomTime){
            float adjust = Mathf.Pow(currZoom/zoomTime,2);
            camera.GetComponent<Camera>().orthographicSize = Mathf.Lerp(0.1f,5,adjust);
            currZoom+=Time.deltaTime;
            yield return null;
        }
        camera.GetComponent<Camera>().orthographicSize = 5;
        UpdateScore();
        cutscene = 0;
        //Debug.Log(iterations);
        yield break;
    }
    void Whiteout(){
        for(int x=-50;x<=50;x++){
            for(int y=-50;y<=50;y++){
                tilemap.SetTile(new Vector3Int(x,y),white);
            }
        }
        tilemap.SetTile(new Vector3Int(0,0),black);
    }
    void AdjustCamera(){
        //Debug.Log(phase);
        float adjust = Mathf.Log10(((float)localSpace+1000)/1000);
        switch(phase){
            case 1:
                //adjust= ((float)totalSpace/10000);
                //Debug.Log(adjust); 
                camera.GetComponent<Camera>().orthographicSize = Mathf.Lerp(5,50,adjust);
                break;
            case 2:
                //adjust= ((float)totalSpace/10000);
                //Debug.Log(adjust); 
                camera.GetComponent<Camera>().orthographicSize = Mathf.Lerp(5,50,adjust);
                break;
            
        //camera.transform.position = new Vector3((maxx+minx)/2.0f + 0.5f,(maxy+miny)/2.0f + 0.5f,-10);
        }
    }
    // Vector3Int getNextSpace(Vector3Int start){
    //     int n = 0;
    //     while(){
    //         int dist = n / 4;
    //         int side = n % 4;
    //         int pos = 2*n**2+2*n+1;
    //     }
    // }

    static IEnumerable<Vector3Int> GenDiamond(){
        yield return new Vector3Int(0,0);
        int ring=0;
        int x=0,y=0;
        while(true){
            ring+=2;x=-ring/2;y=ring/2;
            for(int i=0;i<ring;i++) yield return new Vector3Int(++x,y);
            for(int i=0;i<ring;i++) yield return new Vector3Int(x,--y);
            for(int i=0;i<ring;i++) yield return new Vector3Int(--x,y);
            for(int i=0;i<ring;i++) yield return new Vector3Int(x,++y);
           
        }
    }

}
