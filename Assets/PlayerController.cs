using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;


public class Factori {
    public Vector3Int position;
    public string type;
    public Vector3Int dir;
    public bool active = true;
    public float delay = 0;
}

[Serializable]
public struct FacType{
    public string key;
    public Tile val;
    public float cooldown;
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
    public GameObject upgrades;
    int upgradeLevel = 0;
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
    int goalSpace = 10;
    int localSpace = 1;
    public int phase = 0;
    int cutscene = 0;
    int iterations = 0;
    List<bool> isAuto = new List<bool>();
    List<float> cooldown = new List<float>();
    readonly Vector3Int[] dires = new Vector3Int[]{new (0,1,0), new (1,0,0), new(0,-1,0), new (-1,0,0)};
    // Start is called before the first frame update
    void Start()
    {
        factoryTypes = new Dictionary<string,Tile>{};
        factories = new List<Factori>();
        foreach (FacType item in facTypes)
            factoryTypes.Add(item.key,item.val);
        for(int i=0;i<factoryTypes.Count;i++){
            isAuto.Add(false);
            cooldown.Add(0);
        }
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

            for(int i=0;i<factoryTypes.Count;i++){
                if(cooldown[i]>0)
                    cooldown[i]-=Time.deltaTime;
                else
                    cooldown[i]=0;
                upgrades.transform.GetChild(i+1).GetChild(2).GetComponent<RectTransform>().sizeDelta = new Vector2(65,100*cooldown[i]/facTypes[i].cooldown);
            }
            if(Input.GetMouseButtonDown(0)){
                if(tilemap.GetTile(cellPosition)==black && factorymap.GetTile(cellPosition)==null){
                    switch(currMach){
                        case "liner":
                            if(cooldown[0]==0){
                            factories.Add(new Factori{position=cellPosition,type="liner",dir=dires[currDir],delay= UnityEngine.Random.Range(0.0f,1.0f)*interval});
                            factorymap.SetTile(cellPosition,factoryTypes["liner"]);
                            factorymap.SetTransformMatrix(cellPosition,Matrix4x4.Rotate(Quaternion.FromToRotation(dires[0],dires[currDir])));
                            cooldown[0]=facTypes[0].cooldown;
                            }
                            break;
                        case "diamond":
                            if(cooldown[1]==0){
                            factories.Add(new DiamondFactory(cellPosition, new Vector3Int(0, 1, 0),this));
                            factorymap.SetTile(cellPosition, factoryTypes["diamond"]);
                            cooldown[1]=facTypes[1].cooldown;
                            }
                            break;
                    }        
                }
            }

            if (Input.GetKeyDown(KeyCode.Alpha1)) {
                currMach = "liner";
            }
            if (Input.GetKeyDown(KeyCode.Alpha2)) {
                currMach = "diamond";
            }
            if (Input.GetKeyDown(KeyCode.Alpha3)) {
                currMach = "bomb";
            }
            if (Input.GetKeyDown(KeyCode.Alpha4)) {
                currMach = "speed";
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
                upgrades.transform.GetChild(0).GetComponent<Button>().onClick.Invoke();
            }
        }
    }

    void FixedUpdate(){
        float delta = Time.fixedDeltaTime;
        for(int i=0;i<factories.Count;i++){
            Factori machine = factories[i];
            machine.delay += delta;
            if(machine.delay>=interval){
                machine.delay-=interval;
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
                    Upgrade();
                    switch(phase){
                        case 1:
                            goalSpace = 100;
                            break;
                        case 2:
                            goalSpace = 1000;
                            //camera.GetComponent<Camera>().orthographicSize=50;
                            break;
                        case 3:
                            goalSpace = 10000;
                            break;
                        case 4:
                            goalSpace = 10;
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
    void Upgrade(){
        upgradeLevel+=1;
        switch(upgradeLevel){
            case 1:
                upgrades.SetActive(true);
                break;
            case 2:
                upgrades.transform.GetChild(2).gameObject.SetActive(true);
                break;
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
        float fadeTime = 6;
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
        float zoomTime = 7;
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
            case 2:
                //adjust= ((float)totalSpace/10000);
                //Debug.Log(adjust); 
                camera.GetComponent<Camera>().orthographicSize = Mathf.Lerp(5,50,adjust);
                break;
            case 3:
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

    public void Rotate(){
        currDir = (currDir+1)%4;
    }
    public void ButtonPress(int num){
        switch(num){
            case 1:
                currMach="liner";
                break;
            case 2:
                currMach="diamond";
                break;
        }
    }
    public void AutoPress(int num){
        SetPressed(num,!isAuto[num-1]);
    }

    void SetPressed(int num, bool on){
        isAuto[num-1]=on;
        Debug.Log(on);
        Transform button = upgrades.transform.GetChild(num).GetChild(0).GetChild(1);
        var color = button.GetComponent<Image>().color;
        if(on){
            color = Color.red;
        }else{
            color = Color.white;
        }
        button.GetComponent<Image>().color = color;
    }
}
