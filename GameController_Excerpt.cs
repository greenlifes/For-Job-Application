    void Start() {
        timeLastWave = Time.time; 
        TimeController.Instance.OneSecondTimer += this.WaveControl(); //Register for check in every second
    }
    void OnDestroy() {
        TimeController.Instance.OneSecondTimer -= this.WaveControl();
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
