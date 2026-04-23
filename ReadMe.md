## A. Aggressive & Intention Controller & B. Core System framework sample codes from Unity project & C. Timeline Player
 A. AggressiveController is basic controller in enemy AI framework, contains aggressive token dealer and player intention estimator
- Enemy can enter aggressive state only after received aggressive token
- Boss AI framework is constructed based on player intention score, which quantifying player intention by player action and state

 B. ScriptableObjectSystem.cs & ScriptableObjectSystemToken.cs are our primary system reference method. 
- Using ScriptableObject reference instead of inject or static, syncing the system with unity object life cycle & reduce system coupling

 C. See as readme file in Timeline Player
