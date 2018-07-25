using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
public enum ProgressPhase
{
    StartUp = 0,            // 0 = Already startUp | Generate Next Level
    PlateGenerated = 1,     // 1 = Already generate plate | Generate Next Wave
    MinionGenerated = 2,    // 2 = Already generate minions -> back to 1 or goto 0/3(depend on waveCount & PlateIsfull)
    Destroy = 3,            // 3 = Already Destroy | Generate Next Stage
}
public class GameController : MonoBehaviour {
    public float startUpInterval;
	public float waveInterval;
	public float waveMaxInterval;
	public float reStartUpInterval;

	public GameObject plateBoss;
	public GameObject plateFocus;
	public GameObject plateDistribute;
	public List<Transform> plateParents;
	public List<GameObject> plateBlocks;

    public Action<ProgressPhase> OnChangeProgressPhase;
    public Action<bool> OnSpawnMinions;

    private static GameController instance;
	private LevelController levelController;
	private CameraController cameraControl;

    private ProgressPhase currentPhase = 0;
    private int waveRemain = 0;
	private int plateCount = 0; //plateProgress, not total plate
	private float timeLastWave;
	private PlateController[] plateHolder;
	private List<Plate> nextLevel;
	private List<MinionType> bagMinions = new List<MinionType>();
	private MinionHolder minionHolder;

    public const int WAVE_PER_LEVEL = 2;
    public const int PLATE_PER_STAGE = 6;
    public const int MINION_TYPE_COUNT = 6;
    public const int ENEMY_TYPE_COUNT = 9;

    public GameController Instance {
        get {
            if (instance == null)
                instance = this;
            return instance;
        }
    }
    //Time Interval Table
    public float GetPhaseInterval(ProgressPhase phase) {
        switch (phase)
        {
            case ProgressPhase.StartUp:         return startUpInterval;
            case ProgressPhase.PlateGenerated:  return waveInterval;
            case ProgressPhase.MinionGenerated: return waveMaxInterval;
            case ProgressPhase.Destroy:         return reStartUpInterval;
            default:
                Debug.LogWarning("GetPhaseInterval : Unknown Phase");
                return 0;
        }
    }
	void Awake () {
        instance = this;
        this.levelController = GameObject.FindGameObjectWithTag("LevelController").GetComponent<LevelController>();
        this.minionHolder = GameObject.FindGameObjectWithTag("MinionHolder").GetComponent<MinionHolder>();
        this.cameraControl = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<CameraController>();
		plateHolder = new PlateController[6];

        if (levelController == null || minionHolder == null || cameraControl == null)
            Debug.LogError("Key Controller Missing, please check");
	}
    void Start() {
        timeLastWave = Time.time; 
        TimeController.Instance.OneSecondTimer += this.WaveControl(); //Register for check in every second
    }
    void OnDestroy() {
        TimeController.Instance.OneSecondTimer -= this.WaveControl();
    }
	void Update () {
	}
    void WaveControl() //Control GameProgress from StartUp -> BuildPlate -> MinionGenerated -> StartUp/Destroy
    {
        float timeInterval = this.GetPhaseInterval(currentPhase);
        bool releaseInAdvance = (currentPhase == ProgressPhase.MinionGenerated && minionHolder.minionsCount == 0); //Wipeout all minions

        if (Time.time - timeLastWave > timeInterval || releaseInAdvance)
        {
            timeLastWave = Time.time;
            switch (currentPhase)
            {
                case ProgressPhase.StartUp: //Establish first/next plate(level)
                case ProgressPhase.Destroy:
                    nextLevel = levelController.GetNextSection(); //Get next level information from levelController
                    cameraControl.SetCamera(nextLevel);
                    GenerateUnEstablish(); 
                    plateCount++;
                    waveRemain = WAVE_PER_LEVEL;
                    currentPhase = ProgressPhase.PlateGenerated;
                    break;
                case ProgressPhase.PlateGenerated: //Generate Next Wave
                    if (nextLevel[0].type == Plate.PlateType.Boss){
                        SpawnMinions(true); // Start Spawn Boss or Minions
                        levelController.NextWave(WAVE_PER_LEVEL);
                        waveRemain = 0;
                    }
                    else{
                        SpawnMinions(); // Start SpawnMinions
                    }
                    currentPhase = ProgressPhase.MinionGenerated;
                    break;
                case ProgressPhase.MinionGenerated: //Wave end/time out
                    if (waveRemain != 0) //next wave
                    {
                        waveRemain--;
                        currentPhase = ProgressPhase.PlateGenerated;
                    }
                    if (plateCount >= PLATE_PER_STAGE) //next stage
                    {
                        ClearPlates();
                        currentPhase = ProgressPhase.Destroy;
                    }
                    else{ //next level
                        currentPhase = ProgressPhase.StartUp;
                    }

                    levelController.NextWave();
                    break;
            }
            if (OnChangeProgressPhase != null)
                this.OnChangeProgressPhase(currentPhase);
        }
        return;
    }
	void SpawnMinions(bool isBossLevel = false){
		int numArcher = 0;
		int numMagician = 0;
		int numArcherCharge = 0;
        int numMagicianSlow = 0;
        int limitGenerateNum = 0;

        if(isBossLevel) //Basic Boss generate not include in RNG
            plateHolder[nextLevel[0].pos].SpawnBoss(BossRNG(levelController.SectionNumber));

		ResetMinionsRNG (); //reset rng bag

		foreach (PlateController plateCtrl in plateHolder) {
            if (plateCtrl != null){
                if (isBossLevel && plateCtrl.Info.type == Plate.PlateType.Boss && plateCtrl != plateHolder[nextLevel[0].pos])
                { //Another Boss Plate(past wave) may generate a boss or minions in bossLevel
                    MinionType bossType = this.MinionsRNG(true); //Pick type
                    if ((int)bossType >= MINION_TYPE_COUNT)
                    {
                        plateCtrl.SpawnBoss(bossType);
                    }
                    else
                    {
                        plateCtrl.SetSpawn(bossType);
                        plateCtrl.Spawn();
                    }
                    continue;
                }
                for (int i = 0; i < plateCtrl.SpawnPointCount; i++)
                {
                    MinionType minionType = this.MinionsRNG(); //Pick type
                    plateCtrl.SetSpawn(minionType); //set spawn info
                    switch (minionType) //Counting type num to remove from pickUp bag
                    {
                        case MinionType.Archer:
                            limitGenerateNum = numArcher++;
                            break;
                        case MinionType.Magician:
                            limitGenerateNum = numMagician++;
                            break;
                        case MinionType.ArcherCharge:
                            limitGenerateNum = numArcherCharge++;
                            break;
                        case MinionType.MagicianSlow:
                            limitGenerateNum = numMagicianSlow++;
                            break;
                    }
                    if (limitGenerateNum >= levelController.SpawnLimit[minionType] && bagMinions.Contains(minionType))
                        bagMinions.Remove(minionType);
				}
                plateCtrl.Spawn(); //spawn
			}
		}
        if (OnSpawnMinions != null)
            this.OnSpawnMinions(isBossLevel);
	}
    MinionType MinionsRNG(bool includeBoss = false) {
        if (includeBoss)
        {
            int pick = UnityEngine.Random.Range(0, ENEMY_TYPE_COUNT);
            return (MinionType)pick;
        }
        else
        {
            int pick = UnityEngine.Random.Range(0, bagMinions.Count);
            return bagMinions[pick];
        }
    }
	MinionType BossRNG(int section) {
		return (MinionType)(6 + (section % levelController.bossLevelInterval));
	}
	MinionType BossRNG() {
        return (MinionType)UnityEngine.Random.Range(MINION_TYPE_COUNT, ENEMY_TYPE_COUNT);
	}
	void ResetMinionsRNG() {
		bagMinions.Clear ();
		bagMinions.Add(MinionType.Warrior);
		bagMinions.Add(MinionType.Archer);
		bagMinions.Add(MinionType.Magician);
		bagMinions.Add(MinionType.WarriorShield);
		bagMinions.Add(MinionType.ArcherCharge);
		bagMinions.Add(MinionType.MagicianSlow);
	}
	void GenerateUnEstablish() {
		foreach (Plate plate in nextLevel) {
			if (plateHolder [plate.pos] == null) {
				EstablishPlate (plate);
			}
		}
	}
    //Plate Control
	void EstablishPlate(Plate plateInfo){ //Instantiate and register 
        GameObject plateCon;
        switch (plateInfo.type) { 
            case Plate.PlateType.Boss:
			    plateCon = Instantiate (plateBoss, plateParents [plateInfo.pos].transform, false);
                break;
            case Plate.PlateType.Focus:
			    plateCon = Instantiate (plateBoss, plateParents [plateInfo.pos].transform, false);
                break;
            case Plate.PlateType.Distribute:
			    plateCon = Instantiate (plateBoss, plateParents [plateInfo.pos].transform, false);
                break;
            default:
                Debug.LogWarning("EstablishPlate : Unknown type");
                break;
		}
        plateHolder[plateInfo.pos] = plateCon.GetComponent<PlateController>();
        plateHolder[plateInfo.pos].Info = plateInfo;
	}
    void ClearPlates()
    {
        for (int i = 0; i < 6; i++)
        {
            PlateDisable(i);
        }
        plateCount = 0;
        cameraControl.CameraReset();
    }
    void PlateDisable(int pos)
    {
        if (plateHolder[pos] != null)
        {
            plateBlocks[pos].SetActive(true);
            plateHolder[pos].Fall();
            plateHolder[pos] = null;
        }
    }
    //Public Notify
	public void PlateArrived(int pos){ //remove invisible wall
		plateBlocks [pos].SetActive (false);
	}
}
