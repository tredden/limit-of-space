using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Tilemaps;
using Random = UnityEngine.Random;

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
            position += dir;
            Pc.MoveFactory(this, lastPos);
            Pc.MakeSpace(position);
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
    readonly Vector3Int[] dires = new Vector3Int[] { new(0, 1, 0), new(1, 0, 0), new(0, -1, 0), new(-1, 0, 0) };
    // Start is called before the first frame update
    void Start()
    {
        factoryTypes = new Dictionary<string, Tile> { };
        factories = new List<Factori>();
        factoriesToRemove = new List<Factori>();
        factoriesToAdd = new List<Factori>();
        foreach (FacType item in facTypes)
            factoryTypes.Add(item.key, item.val);
        //Debug.Log(factoryTypes.Keys);
        Debug.Log("gamestart");
        dit = GenDiamond().GetEnumerator();
        maxx = 0; maxy = 0; minx = 0; miny = 0;
        spacePress = 0;
        previousTile = null;
        Whiteout();
        //cutscene = 1;
        //StartCoroutine(ZoomTransition());
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
                factoryui.SetTransformMatrix(cellPosition, Matrix4x4.Rotate(Quaternion.FromToRotation(dires[0], dires[currDir])));
            }
            //Debug.Log(dires[currDir]);
            //float angle = Mathf.Atan2(dires[currDir].x, dires[currDir].y) * Mathf.Rad2Deg - 90f;
            previousTile = cellPosition;

            if (Input.GetMouseButtonDown(0))
            {
                if (tilemap.GetTile(cellPosition) == black)
                {
                    switch (currMach)
                    {
                        case "liner":
                            factories.Add(new LinerFactory(cellPosition, dires[currDir], this));
                            factorymap.SetTile(cellPosition, factoryTypes["liner"]);
                            factorymap.SetTransformMatrix(cellPosition, Matrix4x4.Rotate(Quaternion.FromToRotation(dires[0], dires[currDir])));
                            break;
                        case "diamond":
                            factories.Add(new DiamondFactory(cellPosition, new Vector3Int(0, 1, 0), this));
                            factorymap.SetTile(cellPosition, factoryTypes["diamond"]);
                            break;
                        case "bomb":
                            Vector3 direction = dires[currDir] * 3 + new Vector3(0, Random.Range(0.3f, 3f), 0);
                            factories.Add(new Bomb(cellPosition, direction, this));
                            factorymap.SetTile(cellPosition, factoryTypes["bomb"]);
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


            if (Input.GetKeyDown(KeyCode.Space))
            {
                Debug.Log("spacebar: " + spacePress);

                dit.MoveNext();
                //Debug.Log(dit.Current);
                while (tilemap.GetTile(dit.Current) == black)
                {
                    dit.MoveNext();
                }
                MakeSpace(dit.Current);
                spacePress += 1;
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                currDir = (currDir + 1) % 4;
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
        Debug.Log(factoriesToRemove);
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
        while (currFade < fadeTime)
        {
            square.GetComponent<SpriteRenderer>().color = new Color(0, 0, 0, Mathf.SmoothStep(0, 1, currFade / fadeTime));
            currFade += Time.deltaTime;
            //Debug.Log(currFade);
            yield return null;
        }
        factories.Clear();
        Whiteout();
        square.GetComponent<SpriteRenderer>().color = new Color(0,0,0,0);
        float zoomTime = 7;
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
    void Whiteout()
    {
        for (int x = -50; x <= 50; x++)
        {
            for (int y = -50; y <= 50; y++)
            {
                tilemap.SetTile(new Vector3Int(x, y), white);
            }
        }
        tilemap.SetTile(new Vector3Int(0, 0), black);
    }
    void AdjustCamera()
    {
        //Debug.Log(phase);
        float adjust = Mathf.Log10(((float)localSpace+1000)/1000);
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
}
