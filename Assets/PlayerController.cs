using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Tilemaps;
using Random = UnityEngine.Random;
using UnityEngine.UI;

public abstract class Factori
{
    public Vector3Int position;
    public string type;
    public Vector3Int dir;
    public bool active = true;

    public abstract void Tick(float delta);
    public float delay = 0;
}

[Serializable]
public struct FacType
{
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

        override public void Tick(float delta)
        {
            delay += delta;
            if (delay < Pc.interval) {
                return;
            }
            delay -= Pc.interval;
            Vector3Int lastPos = position;
            if (false)
            {
                DiamondEnum.MoveNext();
                position = DiamondEnum.Current + StartPos;
            }
            else
            {
                while (Pc.tilemap.GetTile(position) == Pc.black)
                {
                    DiamondEnum.MoveNext();
                    position = DiamondEnum.Current + StartPos;
                }
            }

            Pc.MoveFactory(this, lastPos);
            Pc.MakeSpace(position);
        }
    }

    public class LinerFactory : Factori
    {
        private PlayerController Pc;
        public LinerFactory(Vector3Int startPos, Vector3Int direction, PlayerController pc)
        {
            position = startPos;
            type = "liner";
            dir = direction;
            Pc = pc;
        }

        override public void Tick(float delta)
        {
            delay += delta;
            if (delay < Pc.interval) {
                return;
            }
            delay -= Pc.interval;

            Vector3Int lastPos = position;
            while (Pc.tilemap.GetTile(position) == Pc.black)
            {
                position += dir;
            }
            
            Pc.MoveFactory(this, lastPos);
            Pc.MakeSpace(position);
        }
    }
    public class SpeedFactory : Factori
    {
        private PlayerController Pc;
        public SpeedFactory(Vector3Int startPos, Vector3Int direction, PlayerController pc)
        {
            position = startPos;
            type = "speed";
            dir = direction;
            Pc = pc;
        }

        override public void Tick(float delta)
        {
            delay += delta;
            if (delay < Pc.interval) {
                return;
            }
            delay -= Pc.interval;

            Pc.MoveFactory(this, position);
        }
    }

    private class Movement {
        public Vector3 position;
        public Vector3 velocity;
        public Vector3 acceleration;

        public void Tick(float delta) {
            velocity += acceleration * delta;
            position += velocity * delta;
        }
    }

    private class Bomblet : Factori {
        protected PlayerController Pc;
        private int spacesUntilExplode = 10;
        private Movement movement;
        public Bomblet(Movement movement, PlayerController pc) {
            position = Vector3Int.RoundToInt(movement.position);
            type = "bomblet";
            dir = Vector3Int.RoundToInt(movement.velocity);
            Pc = pc;
            this.movement = movement;
        }

        override public void Tick(float delta) {
            if (spacesUntilExplode <= 0) {
                Explode();
                return;
            }

            Vector3Int lastPos = position;
            movement.Tick(delta);
            position = Vector3Int.RoundToInt(movement.position);

            Pc.MoveFactory(this, lastPos);

            if (Pc.tilemap.GetTile(position) == Pc.white) {
                spacesUntilExplode--;
                Pc.MakeSpace(position);
            }
        }

        protected virtual void Explode() {
            Pc.RemoveFactory(this);
        }
    }

    private class Bomb : Bomblet {
        public Bomb(Vector3Int startPos, Vector3 velocity, PlayerController pc) : base(new Movement{
                    position = startPos,
                    velocity = velocity * 4,
                    acceleration = new Vector3(0, -0.8f, 0) * 4,
            }, pc) {
        }

        override protected void Explode() {
            float scale = 4;
            for (int i = 0; i < 20; i++) {
                Movement subMovement = new Movement{
                    position = position,
                    velocity = new Vector3(Random.Range(-6f, 6f), Random.Range(1f, 9f), 0) * 4,
                    //acceleration = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized * 4,
                    acceleration = new Vector3(0, -4, 0) * 4,
                };

                Pc.AddFactory(new Bomblet(subMovement, Pc));
            }

            base.Explode();
        }
    }
    public GameObject wintext;
    public AudioClip mainTheme;
    public AudioClip endTheme;
    public GameObject explosion;
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
    List<Factori> factoriesToRemove;
    List<Factori> factoriesToAdd;
    public List<FacType> facTypes;
    public List<FacType> secretFacTypes;
    Dictionary<string, Tile> factoryTypes;
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
    List<Vector3Int> blackList = new();
    List<bool> isAuto = new();
    List<float> cooldown = new();
    int speedMod = 0;
    readonly Vector3Int[] dires = new Vector3Int[]{new (0,1,0), new (1,0,0), new(0,-1,0), new (-1,0,0)};
    // Start is called before the first frame update
    void Start()
    {
        factoryTypes = new Dictionary<string, Tile> { };
        factories = new List<Factori>();
        factoriesToRemove = new List<Factori>();
        factoriesToAdd = new List<Factori>();
        foreach (FacType item in facTypes)
            factoryTypes.Add(item.key,item.val);
        foreach (FacType item in secretFacTypes)
            factoryTypes.Add(item.key,item.val);
        for(int i=0;i<factoryTypes.Count;i++){
            isAuto.Add(false);
            cooldown.Add(0);
        }
        //Debug.Log(factoryTypes.Keys);
        Debug.Log("gamestart");
        dit = GenDiamond().GetEnumerator();
        maxx = 0; maxy = 0; minx = 0; miny = 0;
        spacePress = 0;
        previousTile = null;
        Whiteout();
        // play final seq
        //  iterations=5;
        //  cutscene = 2;
        //  StartCoroutine(EndScene());
    }

    // Update is called once per frame
    void Update()
    {
        if (cutscene > 0)
        {
            switch (cutscene)
            {
                case 1:
                    break;
            }
        }
        else
        {
            Vector3 mousePos = Input.mousePosition;
            mousePos.z = Camera.main.nearClipPlane;
            Vector3 worldPosition = Camera.main.ScreenToWorldPoint(mousePos);
            Vector3Int cellPosition = factoryui.WorldToCell(worldPosition);
            //Debug.Log(factoryTypes.Keys);
            if (previousTile.HasValue)
            {
                factoryui.SetTile(previousTile.Value, null);
            }
            if (currMach != "")
            {
                factoryui.SetTile(cellPosition, factoryTypes[currMach]);
                if(currMach=="liner" || currMach=="bomb")
                    factoryui.SetTransformMatrix(cellPosition, Matrix4x4.Rotate(Quaternion.FromToRotation(dires[0], dires[currDir])));
            }
            //Debug.Log(dires[currDir]);
            //float angle = Mathf.Atan2(dires[currDir].x, dires[currDir].y) * Mathf.Rad2Deg - 90f;
            previousTile = cellPosition;

            for(int i=0;i<facTypes.Count;i++){
                if(cooldown[i]>0)
                    cooldown[i]-=Time.deltaTime*Mathf.Pow(1.05f,speedMod);
                else
                    cooldown[i]=0;
                if(isAuto[i] && cooldown[i]==0)
                {
                    Vector3Int pos = blackList[Random.Range(0,blackList.Count)];
                    PlaceFactory(i,pos,Random.Range(0,4));
                }
                upgrades.transform.GetChild(i+1).GetChild(2).GetComponent<RectTransform>().sizeDelta = new Vector2(65,100*cooldown[i]/facTypes[i].cooldown);
            }
            if(Input.GetMouseButtonDown(0)){
                if(tilemap.GetTile(cellPosition)==black && factorymap.GetTile(cellPosition)==null){
                    switch(currMach){
                        case "liner":
                            if(cooldown[0]==0){
                                PlaceFactory(0,cellPosition,currDir);
                            }
                            break;
                        case "diamond":
                            if(cooldown[1]==0){
                                PlaceFactory(1,cellPosition,currDir);
                            }
                            break;
                        case "bomb":
                            if(cooldown[2]==0){
                                PlaceFactory(2,cellPosition,currDir);
                            }
                            break;
                        case "speed":
                            if(cooldown[3]==0){
                                PlaceFactory(3,cellPosition,currDir);
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
                while (tilemap.GetTile(dit.Current) == black)
                {
                    dit.MoveNext();
                }
                MakeSpace(dit.Current);
                spacePress += 1;
            }

            if(Input.GetKeyDown(KeyCode.R)){
                upgrades.transform.GetChild(0).GetComponent<Button>().onClick.Invoke();
            }
            }
        }
        
    void FixedUpdate(){
        float delta = Time.fixedDeltaTime;
        foreach (Factori factory in factories) {
            factory.Tick(delta);

            if (Math.Abs(factory.position.x) > 50 || Math.Abs(factory.position.y) > 50)
            {
                RemoveFactory(factory);
            }
        }

        foreach (Factori factory in factoriesToRemove) {
            factories.Remove(factory);
            factorymap.SetTile(factory.position, null);
        }
        factoriesToRemove.Clear();
        foreach (Factori factory in factoriesToAdd) {
            factories.Add(factory);
            factorymap.SetTile(factory.position, factoryTypes[factory.type]);
            factorymap.SetTransformMatrix(factory.position,
                                          Matrix4x4.Rotate(Quaternion.FromToRotation(dires[0], dires[currDir])));
        }
        factoriesToAdd.Clear();
        //Debug.Log(factoriesToRemove);
    }

    void PlaceFactory(int i, Vector3Int cellPosition, int dirp) {
        switch(i){
            case 0:
                factories.Add(new LinerFactory(cellPosition, dires[dirp], this));
                factorymap.SetTile(cellPosition,factoryTypes["liner"]);
                factorymap.SetTransformMatrix(cellPosition,Matrix4x4.Rotate(Quaternion.FromToRotation(dires[0],dires[dirp])));
                cooldown[0]=facTypes[0].cooldown;
                break;
            case 1:
                factories.Add(new DiamondFactory(cellPosition, new Vector3Int(0, 1, 0),this));
                factorymap.SetTile(cellPosition, factoryTypes["diamond"]);
                cooldown[1]=facTypes[1].cooldown;
                break;
            case 2:
                Vector3 direction = dires[dirp] * 3 + new Vector3(0, Random.Range(0.3f, 3f), 0);
                factories.Add(new Bomb(cellPosition, direction, this));
                factorymap.SetTile(cellPosition, factoryTypes["bomb"]);
                cooldown[2]=facTypes[2].cooldown;
                break;
            case 3:
                factories.Add(new SpeedFactory(cellPosition, new Vector3Int(0, 1, 0),this));
                factorymap.SetTile(cellPosition, factoryTypes["speed"]);
                cooldown[3]=facTypes[3].cooldown;
                speedMod+=1;
                interval = 2 * Mathf.Pow(0.95f,speedMod);
                break;
        }
    }
    void AddFactory(Factori factory) {
        factoriesToAdd.Add(factory);
    }

    void RemoveFactory(Factori factory) {
        factoriesToRemove.Add(factory);
    }

    void MoveFactory(Factori factory, Vector3Int fromPos)
    {
        Tile tile = factoryTypes[factory.type];
        // Delete the last position
        if (factorymap.GetTile(fromPos) == tile)
        {
            factorymap.SetTile(fromPos, null);
        }
        // Set the next position
        factorymap.SetTile(factory.position, tile);
        factorymap.SetTransformMatrix(
            factory.position, Matrix4x4.Rotate(
                Quaternion.FromToRotation(dires[0], factory.dir)));
    }

    void MakeSpace(Vector3Int place){
        if(tilemap.GetTile(place)!=black){
        int currx=place.x,curry=place.y;
        

        maxx=Math.Max(maxx,currx);
        maxy=Math.Max(maxy,curry);
        minx=Math.Min(minx,currx);
        miny=Math.Min(miny,curry);
        tilemap.SetTile(place,black);
        blackList.Add(place);
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
                            iterations += 1;
                            goalSpace = 10;
                            localSpace = 1;
                            phase = 0;
                            dit = GenDiamond().GetEnumerator();
                            if(iterations==5){
                                cutscene = 2;
                                StartCoroutine(EndScene());
                            }else{
                                cutscene = 1;
                                StartCoroutine(ZoomTransition());
                            }
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
            case 3:
                upgrades.transform.GetChild(3).gameObject.SetActive(true);
                break;
            case 4:
                upgrades.transform.GetChild(4).gameObject.SetActive(true);
                break;
            case 5:
                upgrades.transform.GetChild(1).GetChild(0).GetChild(1).gameObject.SetActive(true);
                break;
            case 6:
                upgrades.transform.GetChild(2).GetChild(0).GetChild(1).gameObject.SetActive(true);
                break;
            case 7:
                upgrades.transform.GetChild(3).GetChild(0).GetChild(1).gameObject.SetActive(true);
                break;
            case 8:
                upgrades.transform.GetChild(4).GetChild(0).GetChild(1).gameObject.SetActive(true);
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
        while (currFade < fadeTime)
        {
            if(iterations==5){
                //Debug.Log("wha");
                square.GetComponent<SpriteRenderer>().color = new Color(0, 0, 0, 1);
                wintext.GetComponent<Image>().color = new Color(1, 1, 1, Mathf.SmoothStep(1, 0, currFade / fadeTime));
                wintext.transform.GetChild(0).GetComponent<TMP_Text>().color = new Color(1,1,1, Mathf.SmoothStep(1, 0, currFade / fadeTime));
                wintext.transform.GetChild(1).GetComponent<TMP_Text>().color = new Color(1,1,1, Mathf.SmoothStep(1, 0, currFade / fadeTime));
                wintext.transform.GetChild(2).GetComponent<TMP_Text>().color = new Color(1,1,1, Mathf.SmoothStep(1, 0, currFade / fadeTime));
                camera.GetComponent<AudioSource>().volume = Mathf.SmoothStep(0,1,currFade / fadeTime);
            }else{
                square.GetComponent<SpriteRenderer>().color = new Color(0, 0, 0, Mathf.SmoothStep(0, 1, currFade / fadeTime));
            }
            currFade += Time.deltaTime;
            //Debug.Log(currFade);
            yield return null;
        }
        factories.Clear();
        factorymap.ClearAllTiles();
        Whiteout();
        square.GetComponent<SpriteRenderer>().color = new Color(0,0,0,0);
        float zoomTime;
        if(iterations<3){
            zoomTime = 7;
        }else if(iterations==3){
            zoomTime = 6;
        }else {
            zoomTime = 4;
        }
        float currZoom = 0;
        while (currZoom < zoomTime)
        {
            float adjust = Mathf.Pow(currZoom / zoomTime, 2);
            camera.GetComponent<Camera>().orthographicSize = Mathf.Lerp(0.1f, 5, adjust);
            currZoom += Time.deltaTime;
            yield return null;
        }
        camera.GetComponent<Camera>().orthographicSize = 5;
        UpdateScore();
        cutscene = 0;
        //Debug.Log(iterations);
        yield break;
    }

    IEnumerator EndScene(){
        float fadeTime = 6;
        float currFade = 0;
        while (currFade < fadeTime)
        {
            square.GetComponent<SpriteRenderer>().color = new Color(0, 0, 0, Mathf.SmoothStep(0, 1, currFade / fadeTime));
            camera.GetComponent<AudioSource>().volume = Mathf.SmoothStep(1,0,currFade / fadeTime);
            currFade += Time.deltaTime;
            //Debug.Log(currFade);
            yield return null;
        }
        factories.Clear();
        factorymap.ClearAllTiles();
        Whiteout();
        square.GetComponent<SpriteRenderer>().color = new Color(0,0,0,0);
        float zoomTime = 9.137f;
        float currZoom = 0;
        camera.GetComponent<AudioSource>().Stop();
        camera.GetComponent<AudioSource>().volume = 1;
        camera.GetComponent<AudioSource>().PlayOneShot(endTheme);
        while (currZoom < zoomTime)
        {
            float adjust = Mathf.Pow(currZoom / zoomTime, 2);
            camera.GetComponent<Camera>().orthographicSize = Mathf.Lerp(0.1f, 50, adjust);
            currZoom += Time.deltaTime;
            yield return null;
        }
        camera.GetComponent<Camera>().orthographicSize = 50;
        GameObject newBoom = Instantiate(explosion,new Vector3(0.5f,0.5f,0),Quaternion.identity);
        float waitTime=8;
        float currTime=0;
        while (currTime < waitTime){
            currTime+=Time.deltaTime;
            yield return null;
        }
        fadeTime = 7;
        currFade = 0;
        while (currFade < fadeTime)
        {
            wintext.GetComponent<Image>().color = new Color(1, 1, 1, Mathf.SmoothStep(0, 1, currFade / fadeTime));
            currFade += Time.deltaTime;
            yield return null;
        }
        Destroy(newBoom);
        fadeTime = 4;
        currFade = 0;
        while (currFade < fadeTime)
        {
            wintext.transform.GetChild(0).GetComponent<TMP_Text>().color = new Color(1,1,1, Mathf.SmoothStep(0, 1, currFade / fadeTime));
            currFade += Time.deltaTime;
            yield return null;
        }
        fadeTime = 4;
        currFade = 0;
        while (currFade < fadeTime)
        {
            wintext.transform.GetChild(1).GetComponent<TMP_Text>().color = new Color(1,1,1, Mathf.SmoothStep(0, 1, currFade / fadeTime));
            currFade += Time.deltaTime;
            yield return null;
        }
        fadeTime = 4;
        currFade = 0;
        while (currFade < fadeTime)
        {
            wintext.transform.GetChild(2).GetComponent<TMP_Text>().color = new Color(1,1,1, Mathf.SmoothStep(0, 1, currFade / fadeTime));
            currFade += Time.deltaTime;
            yield return null;
        }
        waitTime=7;
        currTime=0;
        while (currTime < waitTime){
            currTime+=Time.deltaTime;
            yield return null;
        }
        
        while(!Input.GetKeyDown(KeyCode.Space)){
            yield return null;
        }
        
        camera.GetComponent<AudioSource>().Play();
        cutscene = 1;
        StartCoroutine(ZoomTransition());
        yield break;
    }
    void Whiteout()
    {
        blackList.Clear();
        for (int x = -50; x <= 50; x++)
        {
            for (int y = -50; y <= 50; y++)
            {
                tilemap.SetTile(new Vector3Int(x, y), white);
            }
        }
        tilemap.SetTile(new Vector3Int(0, 0), black);
        blackList.Add(new Vector3Int(0, 0));
    }
    void AdjustCamera()
    {
        //Debug.Log(phase);
        float adjust = Mathf.Log10(((float)localSpace+850)/950);
        switch(phase){
            case 2:
                //adjust= ((float)totalSpace/10000);
                //Debug.Log(adjust); 
                camera.GetComponent<Camera>().orthographicSize = Mathf.Lerp(5, 50, adjust);
                break;
            case 3:
                //adjust= ((float)totalSpace/10000);
                //Debug.Log(adjust); 
                camera.GetComponent<Camera>().orthographicSize = Mathf.Lerp(5, 50, adjust);
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

    static IEnumerable<Vector3Int> GenDiamond()
    {
        yield return new Vector3Int(0, 0);
        int ring = 0;
        int x = 0, y = 0;
        while (true)
        {
            ring += 2; x = -ring / 2; y = ring / 2;
            for (int i = 0; i < ring; i++) yield return new Vector3Int(++x, y);
            for (int i = 0; i < ring; i++) yield return new Vector3Int(x, --y);
            for (int i = 0; i < ring; i++) yield return new Vector3Int(--x, y);
            for (int i = 0; i < ring; i++) yield return new Vector3Int(x, ++y);

        }
    }

    public void Rotate(){
        currDir = (currDir+1)%4;
        upgrades.transform.GetChild(1).GetChild(1).rotation = Quaternion.FromToRotation(dires[0],dires[currDir]);
        upgrades.transform.GetChild(3).GetChild(1).rotation = Quaternion.FromToRotation(dires[0],dires[currDir]);
    }
    public void ButtonPress(int num){
        switch(num){
            case 1:
                currMach="liner";
                break;
            case 2:
                currMach="diamond";
                break;
            case 3:
                currMach="bomb";
                break;
            case 4:
                currMach="speed";
                break;
        }
    }
    public void AutoPress(int num){
        SetPressed(num,!isAuto[num-1]);
    }

    void SetPressed(int num, bool on){
        isAuto[num-1]=on;
        //Debug.Log(on);
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
