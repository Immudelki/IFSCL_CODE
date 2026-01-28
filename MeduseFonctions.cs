using IFSCL.Programs;
using IFSCL.Skid;
using Sirenix.OdinInspector;
using UnityEngine;
namespace IFSCL.VirtualWorld {
    public enum ScyphozoaSkill {
        None,
        VolAdn,
        Xanatifier,
        DrainCore,
        DrainSkid
    }

    //DNA theft should be considered as theft of Lyoko keys in the case of Aelita and a Xana imprisoned on Lyoko
    public class MeduseFonctions : CreatureUnique {
        [ReadOnly] public MainTimer T_SkillUse = new ("T_MeduseSkillUse",0.01f);
        public ScyphozoaSkill skillMode = ScyphozoaSkill.None;
        public bool usedOncePerMaze;
        public EnergieNeeder drain = new(DepensesEnergetiques.drain, 0, "-");
        public LyokoGuerrier capturedLw;
        public CloneEncounterProfile cloneEncounterProfile = new();
        //the scyphozoa can only be destroyed when it uses its power. On Skidbladnir or lyokoguerriers
        public MeduseFonctions() {
            nom = Lex.meduse.ToString();
            cloneEncounterProfile.linkedCreature = this;
            cloneEncounterProfile.monsterLex = Lex.meduse;
        }
        public static string GetPreciseDrainName() {
            return VarG.meduseFonction.skillMode switch {
                ScyphozoaSkill.DrainCore => MSG.GetOptionNames("xanaDrainCoreAttacks"),
                ScyphozoaSkill.DrainSkid => MSG.GetOptionNames("xanaDrainSkidAttacks"),
                _ => " - "
            };
        }
        public bool HasUnderControl(LyokoGuerrier _LG) {
            return capturedLw == _LG;
        }
        public override void OnVirt(params object[] arg) {
            if (created) {
                MSG.AffCriticalInfo("unique creature " + nom + " already created, a duplicate spawn will cause issues");
            }
            Territoire t = arg[0] as Territoire;
            if (t.superc.IsReplika()) {
                MSG.AffCriticalInfo("scyphozoa is forbidden to spawn on replika");
            }
            created = true;
        }
        public void OnSkidDemater() {
            if (created && skillMode == ScyphozoaSkill.DrainSkid && T_SkillUse.IsRunning()) {
                LyokoGuide.GetByName(Lex.meduse)?.Devirtualiser();
            }
        }
        public bool CanSpawnToDrain() {
            //for now, we limit the couldSpawnToDrain if the combinedEnergyTransmission is in cooldown.
            //in the long term, we can imagine that using combinedEnergy would cause a very significant energy depletion that would take a long time to return, but would not block us excessively
            return !created && !time_TillReuse.IsRUNNING() && !PrgMegascan.singleAction.time_TillReuse.IsRUNNING();
        }
        public override bool CanSpawn() {
            return !created && !time_TillReuse.IsRUNNING() &&
                   (LevelableOption.GetOp(GameOptionName.xanaScyphozoaXanafie).currentValue > 0 ||
                    LevelableOption.GetOp(GameOptionName.xanaScyphozoaSteal).currentValue > 0);
        }
        public bool IsDrainingSkid() {
            return T_SkillUse.IsRunning() && skillMode == ScyphozoaSkill.DrainSkid;
        }
        public bool IsDrainingCore() {
            return T_SkillUse.IsRunning() && skillMode == ScyphozoaSkill.DrainCore;
        }
        public bool IsUsingItsPower() {
            return T_SkillUse.IsRunning();
        }
        public void UpdateTimers() {
            if (T_SkillUse.TryDecrement())
                UsePower();
        }
        public void RAZ(bool needDevirt = false) { // Debug.Log("razMeduse");
            if (needDevirt)
                RazClassique();
            T_SkillUse.Stop();
            StopPower();
            skillMode = ScyphozoaSkill.None;
            RazMinimum();
        }
        public void SetToDrainingMode(ScyphozoaSkill drainMode) {
            if (skillMode == ScyphozoaSkill.None) {
                skillMode = drainMode;
                Camps.jeremie.energyProvider.SendTo(drain, DepensesEnergetiques.drain.maintien, false, true);
                //ProgramsF.energieStat.Execution(false);
                T_SkillUse.Launch();
                //ProgramsF.energieStat.graph.DisplayEnergyUse();
                Debug.Log("StartPower " + drainMode);
                switch (drainMode) {
                    case ScyphozoaSkill.DrainCore when VarG.scLyoko.IsShellConnected():
                        PrgAnomaly.ExecuteSentence("coreIsUnderAttack", false, OSTarget.SC);
                        break;
                    case ScyphozoaSkill.DrainCore:
                        PrgAnomaly.ExecuteSentence("sector5anomaly", false, OSTarget.SC);
                        break;
                    case ScyphozoaSkill.DrainSkid: {
                        if (VarG.skidbladnir.GetGuide().GetTerritoire() == VarG.carthage && !VarG.scLyoko.IsShellConnected()) {
                            PrgAnomaly.ExecuteSentence("sector5anomaly", false, OSTarget.SC);
                        } else {
                            PrgAnomaly.ExecuteSentence("skidIsUnderAttack", false, OSTarget.SC);
                        }
                        Skidbladnir.instance.OnDrainStarted();
                        break;
                    }
                }
            } else {
                Debug.LogWarning("There's already a scyphozoa mode enabled " + skillMode);
            }
        }
        public ScyphozoaSkill TryChooseActionOn(LyokoGuide LG) {
            Debug.Log("TrySetLGcontrol " + LG.nom);
            if (LG.nom == CharacterBase.franz.ToString() || !LyokoGuerrier.IsLyokoGuerrier(LG.nom) || LyokoGuerrier.GetByName(LG.nom).controle != XanaControlStatus.None || LyokoGuide.GetByName(Lex.meduse) == null)
                return ScyphozoaSkill.None;
            LyokoGuerrier _target = LyokoGuerrier.GetByName(LG.nom);
            float stealValue = LevelableOption.GetOp(GameOptionName.xanaScyphozoaSteal).currentValue;
            float xanafieValue = LevelableOption.GetOp(GameOptionName.xanaScyphozoaXanafie).currentValue;
            
            // TODO 52X stealKeys in the case of Aelita with the Keys and Xana locked up on Lyoko
            // le pitch: aelita doit être bloquée sur Lyoko (sans sa mémoire) avec ou sans code terre,
            // c'est donc une option précise XanaBoundToLyoko, qui doit être activée conjointement avec la meduse 'voleuse de mémoire' et aelita 'sans mémoire'
            if (xanafieValue > 0 && stealValue > 0) {
                //if the LW is already in deadly devirt, since xanatification is possible, we automatically choose this one
                if (_target.HasDevirtMortelle()) {
                    return ScyphozoaSkill.Xanatifier;
                }

                //which option stands the best chance? we test it
                if (xanafieValue == stealValue) {
                    //if values are equal
                    return Random.value > 0.5f ? ScyphozoaSkill.VolAdn : ScyphozoaSkill.Xanatifier;
                }
                //if values are not equal
                return GetRandomVal(stealValue, xanafieValue);
            }
            if (stealValue > 0 && !_target.HasDevirtMortelle()) {
                return ScyphozoaSkill.VolAdn;
            }
            if (xanafieValue > 0) {
                return ScyphozoaSkill.Xanatifier;
            }
            Debug.Log("Saved prefs do not contains xanaScyphozoaSteal (+noMortalDevirt) or Xanafie values, so they should fight");
            //fight!
            return ScyphozoaSkill.None;
        }
        [Button]
        public static ScyphozoaSkill GetRandomVal(float stealValue, float xanafieValue) {
            //pour aller plus loin: https://www.youtube.com/watch?v=84rs2Q0z9ak
            float r = Random.Range(0, stealValue + xanafieValue);
            //Debug.Log("r " + r + "/" + Mathf.Ceil(stealValue + xanafieValue));
            if (stealValue > xanafieValue) {
                if (r < xanafieValue) {
                    return ScyphozoaSkill.Xanatifier;
                }
                return ScyphozoaSkill.VolAdn;
            }
            if (r < stealValue) {
                return ScyphozoaSkill.VolAdn;
            }
            return ScyphozoaSkill.Xanatifier;
        }
        public bool TryActionOn(LyokoGuide LG) {
            if (LG.waitForUnReboot) //we wait because the creature can try this immediately after the endReboot, whereas the LG waits for the endRebootfreeze which messes things up.
                return false;
            ScyphozoaSkill _mp = TryChooseActionOn(LG);
            if (_mp == ScyphozoaSkill.None)
                return false;
            DoActionOn(LG, _mp);
            return true;
        }
        public void DoActionOn(LyokoGuide _LG, ScyphozoaSkill _mp) {
            LyokoGuide _monsterGuide = LyokoGuide.GetByName(Lex.meduse);
            //if the scyphozoa had another purpose and ended up starting its power by colision, then it has to stop moving.
            _monsterGuide.orderProfile.SetOrder(LyokoOrder.nothing); //note: this will stop any power already in the pipeline
            _monsterGuide.DeleteObjectifAlternatif();
            _monsterGuide.DeleteObjectif();
            skillMode = _mp;
            capturedLw = LyokoGuerrier.GetByName(_LG.nom);
            _LG.OnCapturedByScyphozoa(_monsterGuide);
            if (!_monsterGuide.HasWalkablePathUnderneath(false) && _LG.carthageProfile.cPos != CarthagePos.maze) {
                _monsterGuide.Remonter_surface(true, true);
                if (ProgramsF.GetA_SC<PrgVirtualMap>().IsOpen() &&
                    ProgramsF.GetA_SC<PrgVirtualMap>().displayedTerritoire == _monsterGuide.territoire) {
                    PrgAnomaly.ExecuteDetailled(OSTarget.SC,
                        MSG.GetAnomaly("LGteleportedBy").Replace("[LG_NAME]", capturedLw.nomTraduit),
                        ProgramsF.GetA_SC<PrgVirtualMap>(), true);
                }
            }
            if (capturedLw.memoryQuantity is 9999 or 0)
                capturedLw.ResetMemory();
            ProgramsF.GetAByOS<PrgVBrainAnalyzer>(OSTarget.SC).ExecuteFor(capturedLw);
            //Debug.Log("aucun lyokoguerrier n'est controlable pour détruire le territoire");
            //Debug.Log(">>la méduse xanatifie donc dans le seul but d'envoyer les lyokoguerriers attaquer le territoire");
            ProgramsF.GetAByOS<PrgVBrainAnalyzer>(OSTarget.SC).SetModeM(skillMode);
            Debug.Log("setLGuidecontrol_meduseFonctions_" + _LG.nom + " " + capturedLw.memoryQuantity);
            //BanqueSonore.channelMemoireLyokoG=BanqueSonore.memoireCalcul.play(0,1000);
            T_SkillUse.Launch();
            Debug.Log("StartPower " + skillMode);
            if (_monsterGuide.noVirtZone && ProgramsF.GetA_SC<PrgVirtualMap>().GetCam3D().rtsCam._followTarget == _monsterGuide.transform) {
                _monsterGuide.noVirtZone.gameObject.SetActive(true);
                _monsterGuide.noVirtZone.AnmDisplay();
            }
        }
        public LyokoGuide _cachedMonsterGuideUsePower;
        public void UsePower() {
            if (_cachedMonsterGuideUsePower==null)
                _cachedMonsterGuideUsePower = LyokoGuide.GetByName(Lex.meduse);
            switch (skillMode) {
                case ScyphozoaSkill.DrainCore:
                    if (VarG.scLyoko.core.GetTotal_Hp() <= 0) {
                        EndPower(true);
                    }
                    //value reduction is managed directly in the lyokoGuide ATK
                    break;
                case ScyphozoaSkill.DrainSkid:
                    if (VarG.skidbladnir.GetTotal_Hp(false) <= 0 || !VarG.skidbladnir.IsMater()) {
                        EndPower(true);
                    }
                    //value reduction is managed directly in the lyokoGuide ATK
                    break;
                case ScyphozoaSkill.VolAdn:
                    if (capturedLw.memoryQuantity <= 5) {
                        capturedLw.memoryQuantity = 0;
                        capturedLw.PerdreCodeADN();
                        EndPower(true);
                    } else {
                        if (!_cachedMonsterGuideUsePower.battleProfile.IsFreezedOrParalyzed()){
                            capturedLw.memoryQuantity = Mathf.Clamp(capturedLw.memoryQuantity -= Mathf.FloorToInt(GameBalanceList.GetREF(GameBalanceType.scyphozoaMemoryChangeSpeed).GetCurrentValue() * (Time.deltaTime)), 0, 9999);
                        }
                    }
                    ProgramsF.GetAByOS<PrgVBrainAnalyzer>(OSTarget.SC).Maj(false);
                    break;
                case ScyphozoaSkill.Xanatifier:
                    if (capturedLw.memoryQuantity >= 9995) {
                        capturedLw.memoryQuantity = 9999;
                        capturedLw.Xanatifier();
                        EndPower(true);
                    } else {
                        if (!_cachedMonsterGuideUsePower.battleProfile.IsFreezedOrParalyzed()){
                            capturedLw.memoryQuantity = Mathf.Clamp(capturedLw.memoryQuantity += Mathf.FloorToInt(GameBalanceList.GetREF(GameBalanceType.scyphozoaMemoryChangeSpeed).GetCurrentValue() * (Time.deltaTime)), 0, 9999);
                        }
                        
                    }
                    ProgramsF.GetAByOS<PrgVBrainAnalyzer>(OSTarget.SC).Maj(false);
                    break;
            }
        }
        public void OnLwRvlpWhileXanafying() {
            if (capturedLw == null)
                return;
            if (Application.isEditor)
                Debug.Log(capturedLw.character + " we put it into definitive xanatification AND devirtualize it");
            capturedLw.Xanatifier(true);
            if (capturedLw.GetGuide())
                capturedLw.GetGuide().Devirtualiser(PreLw_DevirtType.CHUTE); //équivalent d'une devirt par chute
            StopPower();
            LyokoGuide.GetByName(Lex.meduse)?.Devirtualiser();
        }
        public void StopPower() {
            //the power is stopped, the character released and unconscious; this normally happens when a jellyfish is attacked while using its power.
            //or when affected by RVLP
            if (capturedLw != null) {
                Debug.Log("StopPower");
                T_SkillUse.Stop();
                if (capturedLw.IsVirt() && !capturedLw.GetGuide().PreLW_Devirt_Started) {
                    capturedLw.GetGuide().DoParalyzeMidAir(false);
                    capturedLw.GetGuide().DoControledFx(false);
                    capturedLw.GetGuide().SetFlyingMode(false); //if the LW was above the digital sea and had been made to 'fly', we make it fall again
                    if (!capturedLw.IsUnconscious()) {
                        //if the LW has not already been rendered unconscious by having just been xanatified
                        capturedLw.SetUnconscious(0, 15); //50 seconds is too long, we can avoid the character
                    }
                    if (capturedLw.GetGuide().nestedIN) {
                        //since in the case of a reboot, the LW is released but the creature is not destroyed, the unbundling must be forced.
                        Grouping.Degrouper(capturedLw.GetGuide());
                    }
                    capturedLw.GetGuide().TryEnableAI();
                }
                capturedLw = null;
                if (VarG.isQuittingApplication)
                    return;
                ProgramsF.GetAByOS<PrgVBrainAnalyzer>(OSTarget.SC).Maj(false);
                LyokoGuide.GetByName(Lex.meduse)?.GetComponentInChildren<NoVirtZone>(true)?.AnmDisplay(false);
            }
            if (skillMode is ScyphozoaSkill.DrainSkid or ScyphozoaSkill.DrainCore) {
                switch (skillMode) {
                    case ScyphozoaSkill.DrainCore when LyokoGuide.GetByName(Lex.meduse)?.orderProfile.savedOrder == LyokoOrder.drainCore:
                        LyokoGuide.GetByName(Lex.meduse)?.orderProfile.SetOrder(LyokoOrder.nothing);
                        break;
                    case ScyphozoaSkill.DrainSkid when LyokoGuide.GetByName(Lex.meduse)?.orderProfile.savedOrder == LyokoOrder.drainSkidInGarage:
                        LyokoGuide.GetByName(Lex.meduse)?.orderProfile.SetOrder(LyokoOrder.drainSkidInGarage);
                        break;
                }
                Debug.Log("StopPower");
                T_SkillUse.Stop();
                drain.GiveBackEnergy();
            }
        }
        public void EndPower(bool needDevirt) {
            Debug.Log("EndPower");
            ProgramsF.GetAByOS<PrgVBrainAnalyzer>(OSTarget.SC).Maj(false);
            RAZ(needDevirt);
        }
    }
}
