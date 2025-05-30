using System;
using System.Collections.Generic;
using UnityEngine;
using Green.Status;

namespace Green.Buffs
{
    [CreateAssetMenu(fileName = "BindBuff", menuName = "Buffs/Bind")]
    public class BindSetting : BuffSettingCore<Bind> { }
    [Serializable]
    public class Bind : BuffCore
    {
        public override void OnStart(CharacterStatus characterStatus, out bool ClaimRestartCalculate)
        {
            SetBind(characterStatus);
            ClaimRestartCalculate = false;
        }
        public override void OnClear(CharacterStatus characterStatus, out bool ClaimRestartCalculate)
        {
            ClearBind(characterStatus);
            ClaimRestartCalculate = false;
        }
        public override void OnEnd(CharacterStatus characterStatus, out bool ClaimRestartCalculate)
        {
            ClearBind(characterStatus);
            ClaimRestartCalculate = false;
        }
        public Action OnBindEnd;
        private void SetBind(CharacterStatus characterStatus)
        {
            switch (characterStatus)
            {
                case PlayerStatus player:
                    player.PlayerCharacter.SetBind();
                    break;
                case NPCStatus npc:
                    //NPC Bind Flow
                    break;
            }
            if (characterStatus is PlayerStatus) { (characterStatus as PlayerStatus).PlayerCharacter.SetBind(); }
            else if (characterStatus is PlayerStatus) { } //NPC Bind Flow
        }
        private void ClearBind(CharacterStatus characterStatus)
        {
            OnBindEnd?.Invoke();
            switch (characterStatus)
            {
                case PlayerStatus player:
                    player.PlayerCharacter.ClearBind();
                    break;
                case NPCStatus npc:
                    //NPC ClearBind Flow
                    break;
            }
        }
    }
}
