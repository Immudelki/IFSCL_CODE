using System;
using System.Collections.Generic;
using System.Linq;
using IFSCL.Programs;
using IFSCL.RealWorld;
using IFSCL.Save;
using IFSCL.Skid;
using IFSCL.UI;
using IFSCL.VirtualWorld;
using Sirenix.OdinInspector;
using UnityEngine;
using Random = UnityEngine.Random;
namespace IFSCL {
    using static ProgramsF;

    public enum LyokoOrder {
        nothing,
        attackKey,
        attackMaze,
        attackDomeVoid,
        attackDomeBridge,
        attackCore,
        attackSkidInGarage,
        attackSkidOnReplika,
        attackSkidInSea, //TODO 51X
        drainSkidInGarage,
        drainCore,
        defendTower,
        attackTower,
        destroySector,
        persecuteLW,
        throwLW,
        throwItself,
        landNested,
        diveInSea,
        waitForHopperToAppear,
        attackCocoon,
        destroyTowerByOverload,
        goFrontierThroughTower,
        attackSkidOnSurface //TODO 51X
    }

    public enum SpecialOrder {
        NONE,
        POLYMORPH_DEFEND_TOWER,
        SCYPHOZOA_ON_SKID_IN_GARAGE,
        SOUTIEN_BOSS
    }

    public class Xana : Camps {
        public Xana(Camp _camp) {
            camp = _camp;
        }
        #region PARAMS
        //Dès lors qu'aucune attaque n'est plus en activité (voir conditions), on reset le timer de Xana
        public XanaUtilsData utils = new();
        public readonly List<int> listeToursActivables = new();
        [FoldoutGroup("Timers")] public readonly MainTimer TSkidReplikaAttack = new("TSkidReplikaAttack");
        [FoldoutGroup("Timers")] public readonly MainTimer TMonsterSkidAttack = new("TMonsterSkidAttack");
        [FoldoutGroup("Timers")] public readonly MainTimer TDrainSkidAttack = new("TDrainSkidAttack");
        [FoldoutGroup("Timers")] public readonly MainTimer TMonsterCoreAttack = new("TMonsterCoreAttack");
        [FoldoutGroup("Timers")] public readonly MainTimer TDrainCoreAttack = new("TDrainCoreAttack");
        [FoldoutGroup("Timers")] public readonly MainTimer TReactivity = new("TReactivity");
        [FoldoutGroup("Timers")] public readonly MainTimer TGestionXanatifies = new("TGestionXanatifies");
        [FoldoutGroup("Timers")] public readonly MainTimer TSendEnergy = new("TSendEnergy");
        public Coroutine CCUnEnergyzeXW;
        public readonly TimeUnitDateManager time_tillAttack = new(RttpResponse.none);
        public CountdownUnit CU_tillKnockedOutEnds;
        public static readonly int xanaTotalSleepFJ_1 = 6;
        public static readonly int xanaTotalSleepFJ_2 = 12; //là on dit 24h (12FJ)
        public static readonly int xanaTotalSleepFJ_3 = 24;

        //le minimum de temps avant l'attaque reste le même (6,12,24) mais on ajoute un délai random de maximum 5 FJ
        public static readonly int xanaTotalSleepFJ_AddRandom = 5;
        public int lastRandomTaken = 5;

        //LyokoGuide.Create interdit, on doit passer par createMonstreSuite
        public XanaHiveMind hiveMind;
        #endregion PARAMS
        public int GetAddRandomTotalSleep() {
            //previous left, on défini à partir de la fin de la plage théorique initiale,
            //et non du moment aléatoire où à eu lieu l'attaque (si entre 0 et 5, on est tombé sur 2 et on finit l'attaque, on rajoute alors 3 min au prochain choix
            int previousDfLeft = xanaTotalSleepFJ_AddRandom - lastRandomTaken;
            lastRandomTaken = Random.Range(0, xanaTotalSleepFJ_AddRandom + 1);
            Debug.Log("GetAddRandomTotalSleep lastRandomTaken " + lastRandomTaken);
            Debug.Log("GetAddRandomTotalSleep previousDfLeft " + previousDfLeft);
            Debug.Log("GetAddRandomTotalSleep = " + (lastRandomTaken + previousDfLeft));
            return lastRandomTaken + previousDfLeft;
        }
        public void ResetAttackTime(bool withPushNotif, bool resetTimeTillAttack = true) {
            DebugXana("ResetAttackTime | resetTimeTillAttack = "+resetTimeTillAttack);
            //nextAttackFaster = false;
            if (resetTimeTillAttack) {
                if (withPushNotif) {
                    time_tillAttack.Set(xanaTotalSleepFJ_2 + GetAddRandomTotalSleep(), AgendaTimeMarkerType.xanaCanReattack);
                } else {
                    time_tillAttack.Set(xanaTotalSleepFJ_2 + GetAddRandomTotalSleep());
                }
            }
            TMonsterCoreAttack.Stop();
            // TEarthAttacks.Stop();
            TMonsterSkidAttack.Stop();
            TSkidReplikaAttack.Stop();
            TDrainSkidAttack.Stop();
            TDrainCoreAttack.Stop();
            TGestionXanatifies.Launch();
        }
        public void RemoveHasDoneSmth() {
            utils.hasAnyTower_AtkOnce = false;
            utils.hasTowerReplikaAtkOnce = false;
            utils.hasMonsterCoreAtkOnce = false;
            utils.hasMonsterSkidAtkOnce = false;
            utils.hasSkidReplikaAtkOnce = false;
            utils.hasDrainSkidAtkOnce = false;
            utils.hasDrainCoreAtkOnce = false;
        }
        //ne jamais forcer le temps de l'attaque à se réduire manuellement 'ForceAttackTime_ToNow'
        //car sinon ça rend forçément des situations impossibles avec les LG qui n'ont pas eu leur
        //temps réglementaire pour être revirtualisables

        //NOTE LA FONCTION PRISE DE CONTROLE DE TOUR OU DE MONSTRES/LYOKOGUERRIER DOIT ETRE SIMILAIRE ENTRE XANA ET HOPPER;
        private bool init;
        private bool initWithTowerDisable;
        public void Initialisation() {
            DebugXana("XanaInitialisation");
            hiveMind = new XanaHiveMind();
            TSkidReplikaAttack.Stop();
            TMonsterCoreAttack.Stop();
            TMonsterSkidAttack.Stop();
            TDrainSkidAttack.Stop();
            TDrainCoreAttack.Stop();
            SetupTickingTimes();
            TReactivity.Launch();
            TSendEnergy.Launch();
            DebugXana("TReactivity " + TReactivity.IsRunning());
            TGestionXanatifies.Launch();
            ResetAttackTime(false, VarG.currentGameLoadedFromAnySave == -1);
            RemoveHasDoneSmth();
            if (VarG.gameMode != GameMode.Story && VarG.currentGameLoadedFromAnySave == -1)
                SetupFirstAttack();
            CU_tillKnockedOutEnds = CountdownUnit.CreateNew("xanaEndKnockedOut", () => hiveMind.OnKnockedOutEnds());
            CU_tillKnockedOutEnds.AddDynamicDisplay(RealWorldCanvas.instance.quickInfoOverlay.CU_DisplayTillKnockedOutEnd);
            RealWorldCanvas.instance.quickInfoOverlay.CU_DisplayTillKnockedOutEnd.Setup();
            CU_tillKnockedOutEnds.SetTimer(GameBalanceList.GetREF(GameBalanceType.xanaKnockedOutDuration).GetCurrentValue());
            init = true;
            if (StoryConstantREF.GetByEnum(StoryConstantType.xanaIADisabled).IsOn()) {
                DisableXana(initWithTowerDisable);
            }
            utils.ShuffleMaxSimultaneousAttack();
        }
        public void RemoveTowerEnergyFromReplika() {
            if (VarG.replika != null && VarG.replika.GetSuperc() != null) {
                if (utils.GetTowerTarget(VarG.replika.GetSuperc()) != null) {
                    Cancel_TryTakeControlTower(VarG.replika.GetSuperc());
                }
                foreach (var t in Tour.GetListeToursActives().Where(t => t != null && t.IsActivatedBy(Camp.XANA))) {
                    t.Desactiver();
                }
            }
            VarG.replikaParam.GetSuperc().hasDestroyedTowerGenerator = true;
        }
        private void SetupFirstAttack() {
            DebugLogList.LogXana("xanaFirstAwakening.currentValue: " +
                                 LevelableOption.GetOp(GameOptionName.xanaFirstAwakening).currentValue);
            DebugLogList.LogXana("SetupFirstAttack - TimeManager.ABSTRACT_TIME_day: " +
                                 TimeManager.ABSTRACT_TIME.dateTime.Day);
            DebugLogList.LogXana("SetupFirstAttack - TimeManager.ABSTRACT_TIME_GetCurrentUnit: " +
                                 TimeManager.ABSTRACT_TIME.GetCurrentUnit());
            switch (LevelableOption.GetOp(GameOptionName.xanaFirstAwakening).currentValue) {
                case 1:
                    //on garde donc un petit délai (boot SC au démarrage de la partie)
                    time_tillAttack.DESACTIVER();
                    GameSceneTime.instance.StartDelayedAction(5, TryAttackOnStartup);
                    break;
                case 2:
                    DebugLogList.LogXana("SetupFirstAttack DayFragments " + xanaTotalSleepFJ_1);
                    //force the generation of last randomTaken 
                    GetAddRandomTotalSleep();
                    time_tillAttack.Set(xanaTotalSleepFJ_1 + lastRandomTaken);
                    break;
                case 3:
                    DebugLogList.LogXana("SetupFirstAttack DayFragments " + xanaTotalSleepFJ_2);
                    //force the generation of last randomTaken 
                    GetAddRandomTotalSleep();
                    time_tillAttack.Set(xanaTotalSleepFJ_2 + lastRandomTaken);
                    break;
                case 4:
                    DebugLogList.LogXana("SetupFirstAttack DayFragments " + xanaTotalSleepFJ_3);
                    //force the generation of last randomTaken 
                    GetAddRandomTotalSleep();
                    time_tillAttack.Set(xanaTotalSleepFJ_3 + lastRandomTaken);
                    break;
            }
            DebugLogList.LogXana("SetupFirstAttack - time_tillAttack_day: " +
                                 time_tillAttack.timeUnitDate.dateTime.Day);
            DebugLogList.LogXana("SetupFirstAttack - time_tillAttack_GetCurrentUnit: " +
                                 time_tillAttack.timeUnitDate.GetCurrentUnit());
            //miniature = direct, 6, 12, 24
        }
        public void SetupTickingTimes() { //ça reste utile pour les simultaneous attacks probablement.
            TSkidReplikaAttack.SetTickingTime(2);
            TReactivity.SetTickingTime(1); // en secondes, toutes les 10-12 secondes (*2 par waitturns_before_newMonsters au besoin)
            TSendEnergy.SetTickingTime(0.1f);
            TGestionXanatifies.SetTickingTime(0.2f);

            //sachant que Xana à une chance sur 2 de faire l'attaque uniquement si des LG sont déjà dans le cinquième territoire
            //et une chance sur 10 si aucun n'est dispo pour être virtualisé à l'usine et qu'ils ne sont en dehors de Lyoko
            SetupTick(TMonsterSkidAttack, GameOptionName.xanaMonsterSkidAttacks);
            SetupTick(TDrainSkidAttack, GameOptionName.xanaDrainSkidAttacks);
            SetupTick(TMonsterCoreAttack, GameOptionName.xanaMonsterCoreAttacks);
            //SetupTick(TDrainCoreAttack, GameOptionName.xanaDrainCoreAttacks);
            void SetupTick(MainTimer timer, GameOptionName option) {
                int[] values = { 999, 25, 15, 7 }; //999 = bloqué (valeur "0" dans l'option), 7 = toutes les 3 minutes
                int level = Mathf.Clamp(LevelableOption.GetOp(option).currentValue, 0, 3);
                timer.SetTickingTime(values[level]);
            }
        }
        public void DisableXana(bool withDbgMessage = false, bool andTowers = true) {
            if (!StoryConstantREF.GetByEnum(StoryConstantType.xanaIADisabled).IsOn()) {
                StoryConstantREF.GetByEnum(StoryConstantType.xanaIADisabled).Set(true);
                //ça va faire la boucle donc on peux mettre un return
                return;
            }
            if (!init) {
                initWithTowerDisable = andTowers;
                return;
            }
            Elevator3D.SetXanaHack(false);
            CU_tillKnockedOutEnds.Reset();
            TReactivity.Stop();
            //TEarthAttacks.Stop();
            TMonsterCoreAttack.Stop();
            TSkidReplikaAttack.Stop();
            TMonsterSkidAttack.Stop();
            TDrainSkidAttack.Stop();
            TDrainCoreAttack.Stop();
            TSendEnergy.Stop();
            TGestionXanatifies.Stop();
            if (andTowers)
                DisableAllTowers();
            if (withDbgMessage)
                MSG.AffDebugInfo("xana fully disabled");
        }
        public void EnableXana() {
            if (StoryConstantREF.GetByEnum(StoryConstantType.xanaIADisabled).IsOn()) {
                StoryConstantREF.GetByEnum(StoryConstantType.xanaIADisabled).Set(false);
                //ça va faire la boucle donc on peux mettre un return
                return;
            }
            if (!init)
                return;
            TReactivity.Launch();
            TSendEnergy.Launch();
            TGestionXanatifies.Launch();
            //les autres timers sont gérés par les canAttackAtAnyTime
        }
        public bool DoOrAim_Attack(bool core, bool sector, bool skidAttack, bool skidDrain) {
            //si monstre ou LG xanatifié
            foreach (var lg in LyokoGuide.liste.Where(lg => lg.camp == Camp.XANA)) {
                if (core && lg.orderProfile.savedOrder == LyokoOrder.attackCore &&
                    (lg.carthageProfile.cPos == CarthagePos.domeVoid ||
                     lg.carthageProfile.cPos == CarthagePos.coreRoom)
                    && VarG.scLyoko.GetStatus() == VirtualBuildStatus.created)
                    return true;
                if (sector && lg.lgType == LgTypes.LyokoGuerrier &&
                    lg.orderProfile.savedOrder == LyokoOrder.destroySector
                    && Territoire.IsATerritoire(lg.orderProfile.savedOrderStringValue) &&
                    Territoire.GetByName(lg.orderProfile.savedOrderStringValue).GetStatus() ==
                    VirtualBuildStatus.created) //xanatifié only
                    return true;
                if (skidAttack && lg.orderProfile.savedOrder == LyokoOrder.attackSkidInGarage &&
                    lg.carthageProfile.cPos is CarthagePos.garageElevatorRoom or CarthagePos.garageSkid
                    && VarG.garageSkid.GetStatus() == VirtualBuildStatus.created &&
                    (VarG.skidbladnir.IsMater() && VarG.skidbladnir.IsKindaDockedToGarage())) //xanatifié only
                    return true;
                if (skidAttack && lg.orderProfile.savedOrder == LyokoOrder.attackSkidOnReplika &&
                    VarG.garageSkid.GetStatus() == VirtualBuildStatus.created &&
                    (VarG.skidbladnir.IsMater() && VarG.skidbladnir.isAtTowerLevel)) //xanatifié only
                    return true;
                if (skidDrain && lg.orderProfile.savedOrder == LyokoOrder.drainSkidInGarage &&
                    (lg.carthageProfile.cPos == CarthagePos.garageElevatorRoom ||
                     lg.carthageProfile.cPos == CarthagePos.garageSkid)
                    && VarG.garageSkid.GetStatus() == VirtualBuildStatus.created &&
                    (VarG.skidbladnir.IsMater() && VarG.skidbladnir.IsKindaDockedToGarage()))
                    return true;
            }
            return false;
        }
        public void OnDecryptageCancelled(Tour _t) {
            if (utils.GetTowerTarget(_t.GetSuperc()) == _t)
                Cancel_TryTakeControlTower(_t.GetSuperc());
        }
        public void SendEnergy() {
            Tour tTarget = null;
            //MSG.AffDebugInfo("XANA SendEnergy");
            //DEMANDER ASKENERGIE ICI ?
            if (VarG.skidbladnir.IsDockedToTower() && utils.towerReplikaTarget != null &&
                utils.towerReplikaTarget.inUniverseID == VarG.skidbladnir.GetDockedTower()) {
                tTarget = utils.towerReplikaTarget;
            }
            if (GetA_SC<PrgJournalDecrypt>().tourDecryptage != null)
                tTarget = GetA_SC<PrgJournalDecrypt>().tourDecryptage;
            if (tTarget == null)
                return;
            if (utils.GetSendEnergyRegularlyTo(tTarget.GetSuperc()) != null && !tTarget.IsActivatedBy(Camp.XANA) && !tTarget.IsKo()) {
                if (tTarget.IsActivatedBy(Camp.HOPPER)) {
                    Cancel_TryTakeControlTower(tTarget.GetSuperc());
                    return;
                }
                bool verif = LyokoGuide.liste.Any(LGMonstre => LGMonstre.camp == Camp.XANA && LGMonstre.towerProfile.HasProximity() &&
                                                               LGMonstre.UnivIDAccessibleTower == utils.GetSendEnergyRegularlyTo(tTarget.GetSuperc()).inUniverseID);
                int bonus = 0;
                if (tTarget.GetSuperc().IsReplika())
                    bonus = 10;
                if (verif) {
                    //si Xana à des monstres près de la tour, la prise de controle est accélérée
                    energyProvider.SendTo(tTarget.energyProfile, 30 + bonus);
                } else {
                    //10 to 15 in 38X, car sans ses monstres, xana n'arrivait pas à rattraper
                    energyProvider.SendTo(tTarget.energyProfile, 15 + bonus);
                }
                if (tTarget.IsActivatedBy(Camp.XANA)) {
                    try {
                        if (tTarget.GetSuperc().IsReplika()) {
                            //code dans Skidbladnir.OnTowerActivated_ByXana
                            //GameSceneTime.Instance.StartCoroutine(GameSceneTime.Instance.DelayedAction(4, () => Results.CallGameEnded("gameOver_skidbreach", true, ResultType.Fail)));
                        } else {
                            GameSceneTime.instance.StartDelayedAction(4, () => Results.CallGameEnded("gameOver_securitybreach", true, ResultType.Fail));
                        }
                    } catch (Exception e) {
                        MSG.AffCriticalInfo("XanaSendEnergy issue onStartCoroutine " + e);
                    }
                }
            }
        }
        [Button]
        public void ConditionalDoUnEnergyzeXW() {
            if (LyokoGuerrierUtils.GetLwTranslated(XanaControlStatus.None).Length > 0)
                return;
            foreach (LyokoGuerrier xw in LyokoGuerrierUtils.GetLwTranslated(XanaControlStatus.Definitive)) {
                xw.Detranslater(DetranslationType.COUNTDOWN_LIMIT);
            }
            CCUnEnergyzeXW = null;
        }
        public void DoAttack(Supercalculateur _sc) {
            TryTowerAttack(_sc);
            TryTowerTakeControl(_sc);
            //karuzofix : https://docs.google.com/document/d/1Jawbdxnlb5od8yOqfJz1qmjdkYcJ8QxqnDMBhkQlg2Y/
            if (HasActivatedTower_In(_sc)) {
                utils.SetHasTowerAtkOnce(_sc, true);
                Debug.Log("waitingForAttack To False");
            } else {
                Debug.Log("hasn't activatedTower (didn't had time to tryTowerAttack because timeSpeeded up ? OR is sending energy to one)");
            }
        }
        public bool TrySkidDrainAttack(bool forced = false, bool useChances = true) {
            if (!VarG.skidbladnir.CanBeReachedInGarage(true))
                return false;
            if (!forced) {
                if (LevelableOption.GetOp(GameOptionName.xanaDrainSkidAttacks).currentValue <= 0)
                    return false;
                if (!utils.CanAttackMore || utils.hasAnySkidGarage_AtkOnce)
                    return false;
                if (!TDrainSkidAttack.IsRunning())
                    TDrainSkidAttack.Launch();
                if (HasActivatedTower_But(VarG.scLyoko)) {
                    DebugXana("drain skid empêché: Xana attaque déjà avec une tour EXTERIEURE à Lyoko");
                    return false;
                }
                if (!VarG.meduseFonction.CanSpawnToDrain()) {
                    DebugXana("drain skid empêché: Meduse deja en utilisation ou doit encore attendre avant d'être réutilisable ou multitransmission en cooldown");
                    return false;
                }
                DebugXana("TrySkidDrain");

                //on teste déjà la chance avant, qui appelle la fonction TrySkidDrain elle même
                if (useChances) {
                    if (!XanaUtilsData.HasRandomChance(7, "drain skid empêchée:"))
                        return false;
                }
            }
            XanaMonsterCreator.CreateStartum(null, LyokoOrder.drainSkidInGarage, SpecialOrder.SCYPHOZOA_ON_SKID_IN_GARAGE, Lex.none, VarG.carthageParam.skidbladnirAnchor);
            MissionManager.Call(AtkAlertType.alertSkidDrained);
            utils.hasDrainSkidAtkOnce = true;
            return true;
        }
        public bool TryDrainCoreAttack(bool forced = false, bool useChances = true) {
            if (VarG.scLyoko.GetStatus() != VirtualBuildStatus.created)
                return false;
            if (!forced) {
                if (!utils.CanAttackMore && LevelableOption.GetOp(GameOptionName.xanaDrainCoreAttacks).currentValue <= 0 || utils.hasAnyCore_AtkOnce)
                    return false;
                if (!TDrainCoreAttack.IsRunning())
                    TDrainCoreAttack.Launch();
                if (HasActivatedTower_But(VarG.scLyoko)) {
                    DebugXana("drain core empêché: Xana attaque déjà avec une tour EXTERIEURE à Lyoko");
                    return false;
                }
                if (!VarG.meduseFonction.CanSpawnToDrain()) {
                    DebugXana("drain skid empêché: Meduse deja en utilisation ou doit encore attendre avant d'être réutilisable ou multitransmission en cooldown");
                    return false;
                }
                DebugXana("TryCoreDrain");
                //on teste déjà la chance avant, qui appelle la fonction TryCoreDrain elle même
                if (useChances) {
                    int chances = 7; //sur 10
                    int chanceTest = Random.Range(0, 11); //entre 0 et 10
                    if (chanceTest > chances) {
                        DebugXana("drain core empêchée: pas assez de chances: " + chanceTest + "/" + chances);
                        return false;
                    }
                }
            }
            XanaMonsterCreator.CreateStartum(null, LyokoOrder.drainCore, SpecialOrder.SCYPHOZOA_ON_SKID_IN_GARAGE, Lex.none,
                VarG.carthageParam.coreElement);
            MissionManager.Call(AtkAlertType.alertCoreDrained);
            utils.hasDrainCoreAtkOnce = true;
            return true;
        }
        public void TryMonsterSkidReact_IfSkidBackInGarageWithMonsters() {
            // regardless of the blocker "has skid been attacked once / multiple attacks activated"
            if (LevelableOption.GetOp(GameOptionName.xanaMonsterSkidAttacks).currentValue <= 0 || VarG.skidbladnir.GetStatus() != VirtualBuildStatus.created ||
                !VarG.skidbladnir.CanBeReachedInGarage(false))
                return;
            bool doSequel = false;
            foreach (var _m in hiveMind.GetMonsterAvailableForReorder(VarG.carthage, LyokoOrder.attackSkidInGarage).Where(_m => _m.carthageProfile.cPos == CarthagePos.garageSkid)) {
                //specifically those in garageSkid, not those in elevatorRoom
                doSequel = true;
                _m.orderProfile.SetOrderGeneric(LyokoOrder.attackSkidInGarage); //faire des téléports ?
            }
            if (doSequel) {
                MissionManager.Call(AtkAlertType.alertSkidAttacked);
            }
        }
        public bool TryMonsterSkidAttack(bool forced = false, bool useChances = true) {
            //version garageSkid
            //si l'attaque est FORCED, on s'oblige  à forcer xanaMonsterSkidAttacks à changer d'option 1 pour que le SEQUEL soit bien appelé
            //en effet, on ne peux pas utiliser hasGarageAtkOnce = true dans le SEQUEL car cette valeur est reset par ResetAttackTimer
            if (forced && LevelableOption.GetOp(GameOptionName.xanaMonsterSkidAttacks).currentValue <= 0) {
                LevelableOption.GetOp(GameOptionName.xanaMonsterSkidAttacks).SetValue(1);
            }
            if (VarG.skidbladnir.GetStatus() != VirtualBuildStatus.created || !VarG.skidbladnir.CanBeReachedInGarage(false))
                return false;
            if (!forced) {
                if (LevelableOption.GetOp(GameOptionName.xanaMonsterSkidAttacks).currentValue <= 0)
                    return false;
                if (!utils.CanAttackMore || utils.hasAnySkidGarage_AtkOnce) {
                    DebugXana("skidAtk blocked: Xana has already atkOnce (or can't attack more because another attack already happened)");
                    return false;
                }
                if (HasActivatedTower_But(VarG.scLyoko)) {
                    DebugXana("skidAtk blocked: Xana already is attacking with a tower external to Lyoko");
                    return false;
                }
                //on teste déjà la chance avant, qui appelle la fonction TrySkidGarageAttack elle même
                if (useChances) {
                    int chances = 7; //sur 10
                    int chanceTest = Random.Range(0, 11); //entre 0 et 10
                    if (chanceTest > chances) {
                        DebugXana("skidAtk blocked: pas assez de chances: " + chanceTest + "/" + chances);
                        return false;
                    }
                }
                DebugXana("Start SkidGarageAttack");
                if (!TMonsterSkidAttack.IsRunning())
                    TMonsterSkidAttack.Launch();
            }
            //note: si il y a au moins un monstre, le reste sera créé en voulant persécuter les lyokoguerriers présents dans le garage skid
            foreach (LyokoGuide _m in hiveMind.GetMonsterAvailableForReorder(VarG.carthage, LyokoOrder.attackSkidInGarage)) {
                DebugXana(_m.nom, ", dispo pour aller attaquer le skid dans le garage");
                _m.orderProfile.SetOrderGeneric(LyokoOrder.attackSkidInGarage); //faire des téléports ?
            }
            for (int a = 0; a < 8; a++) { //max de 8 monstres pour attaquer le skid dans le garage
                if (LyokoGuideUtilities.GetTotalLG_AttackingSkid_InGarage() < 8) {
                    DebugXana("confirmation, il y'a moins de 6 monstres (actuellement " +
                              LyokoGuideUtilities.GetTotalLG_AttackingSkid_InGarage() +
                              ") allant attaquer ou attaquant le skid dans le garage donc on en créer");
                    XanaMonsterCreator.CreateStartum(null, LyokoOrder.attackSkidInGarage, 0, Lex.none, CarthageAppearPointElement.Get_MonsterGarageAppearPoint());
                }
            }
            MissionManager.Call(AtkAlertType.alertSkidAttacked);
            utils.hasMonsterSkidAtkOnce = true;
            return true;
        }
        public bool TryReplikaSkidReact(bool forced = false) {
            if (VarG.skidbladnir.GetStatus() != VirtualBuildStatus.created || !VarG.skidbladnir.isAtTowerLevel ||
                !VarG.skidbladnir.IsInReplika())
                return false;
            if (!forced) {
                if (utils.hasSkidReplikaAtkOnce || !utils.CanAttackMore_Replika) {
                    DebugXana("skidReplikaAtk blocked: Xana has already atkOnce (or can't attack more because another attack already happened) - BUT we send monsters that are left alone");
                    foreach (LyokoGuide _m in hiveMind.GetMonsterAvailableForReorder(VarG.skidbladnir.GetGuide().territoire, LyokoOrder.attackSkidOnReplika)) {
                        DebugXana(_m.nom, ", dispo pour aller attaquer le skid sur le Replika");
                        _m.orderProfile.SetOrderGeneric(LyokoOrder.attackSkidOnReplika); //faire des téléports ?
                    }
                    return false;
                }
                //on teste déjà la chance avant, qui appelle la fonction TrySkidSurfaceAttack elle même
                int chances = 7; //sur 10
                int chanceTest = Random.Range(0, 11); //entre 0 et 10
                if (chanceTest > chances) {
                    DebugXana("skidReplikaAtk blocked: pas assez de chances: " + chanceTest + "/" + chances);
                    return false;
                }
                DebugXana("Start SkidReplikaAttack");
                if (!TSkidReplikaAttack.IsRunning())
                    TSkidReplikaAttack.Launch();
            }
            //note: si il y a au moins un monstre, le reste sera créé en voulant persécuter les lyokoguerriers présents à la surface
            foreach (LyokoGuide _m in hiveMind.GetMonsterAvailableForReorder(VarG.skidbladnir.GetGuide().territoire,
                         LyokoOrder.attackSkidOnReplika)) {
                DebugXana(_m.nom, ", dispo pour aller attaquer le skid sur le Replika");
                _m.orderProfile.SetOrderGeneric(LyokoOrder.attackSkidOnReplika); //faire des téléports ?
            }
            if (LyokoGuideUtilities.GetTotalLG_AttackingSkid_OnReplikaGround_PowerLevel(Camp.XANA) < 3) {
                DebugXana("confirmation, GetTotalLG_AttackingSkid_OnReplikaGround_PowerLevel est plus bas que (actuellement " + LyokoGuideUtilities.GetTotalLG_AttackingSkid_OnReplikaGround_PowerLevel(Camp.XANA) + ") donc on créer du monstre");
                XanaMonsterCreator.CreateStartum(VarG.skidbladnir.GetGuide().GetSecondClosestTower(false, 1000), LyokoOrder.attackSkidOnReplika);
            }
            MissionManager.Call(AtkAlertType.alertSkidAttacked);
            utils.hasSkidReplikaAtkOnce = true;
            return true;
        }
        public bool TryCorruptedCoreAttack(bool asNewAttack = false, bool useChances = true) {
            //use Anthozoa
            return false;
        }
        public bool TryMonsterCoreAttack(bool asNewAttack = false, bool useChances = true) {
            XanaTickDisplay.UpdateTickPanel(TickPanelType.coreAtk);
            if ((!asNewAttack && LevelableOption.GetOp(GameOptionName.xanaMonsterCoreAttacks).currentValue == 0) ||
                VarG.scLyoko.GetStatus() != VirtualBuildStatus.created ||
                VarG.carthage.GetStatus() != VirtualBuildStatus.created)
                return false;
            if (!utils.CanAttackMore || utils.hasMonsterCoreAtkOnce || utils.hasDrainCoreAtkOnce)
                return false;
            if (!TMonsterCoreAttack.IsRunning())
                TMonsterCoreAttack.Launch();
            if (HasActivatedTower_But(VarG.scLyoko)) {
                DebugXana("attaque coeur empêchée: Xana attaque déjà avec une tour EXTERIEURE à Lyoko");
                return false;
            }
            DebugXana("TryCoreAttack");
            if (!asNewAttack && useChances && !utils.HasEnoughChances_ForCoreAtk())
                return false;
            //note: si il y a au moins un monstre, le reste sera créé en voulant persécuter les lyokoguerriers présents dans le coeur ?
            foreach (LyokoGuide _m in hiveMind.GetMonsterAvailableForReorder(VarG.carthage, LyokoOrder.attackCore)) {
                DebugXana(_m.nom, ", dispo pour aller attaquer le coeur de Lyoko");
                _m.orderProfile.SetOrderGeneric(LyokoOrder.attackCore);
            }
            for (int a = 0; a < 8; a++) { //max de 8 monstres pour attaquer le coeur
                if (LyokoGuideUtilities.GetTotalLG_AttackingCore() < 8) {
                    DebugXana("confirmation, y'a moins de " + a +
                              " monstres allant attaquer ou attaquant le coeur de lyoko donc on en créer");
                    XanaMonsterCreator.CreateStartum(null, LyokoOrder.attackCore, 0, Lex.none, VarG.carthageParam.coreElement);
                }
            }
            MissionManager.Call(AtkAlertType.alertCoreAttacked);
            utils.hasMonsterCoreAtkOnce = true;
            return true;
            //normalement en comptant les monstres créés naturellement pour persécuter, ça devrait en faire assez
        }
        public bool TryTowerAttack(Supercalculateur _sc, TowerAttackType forcedTowerAttackType = TowerAttackType.NONE) {
            DebugLogList.LogXanaATK("TryTowerAttack");
            if (_sc == VarG.scLyoko) {
                if (!utils.CanAttackMore || utils.hasAnyTower_AtkOnce || !XanaUtilsData.CanAttackAtAnyTime || !LevelableOption.GetOp(GameOptionName.xanaEarthAttacks).ToBool())
                    return false;
            } else {
                if (_sc != null && _sc.IsReplika()) {
                    //si sur Replika, on considère que c'est toujours en tant que défense, et on ne vérifie pas que 'canAttackAtAnyTime'
                    //on utilise pas hasTowerReplikaAtkOnce, on veut qu'il puisse reattaquer si le besoin d'en fait sentir
                    //if (utils.hasTowerReplikaAtkOnce)
                    //return true;
                    if (_sc.hasDestroyedTowerGenerator)
                        return false;
                }
            }
            if (utils.GetTowerTarget(_sc) != null) {
                //towertarget est delete après tentativePriseDeControle donc on save
                Tour towerTargetSave = utils.GetTowerTarget(_sc);
                TryTowerTakeControl(_sc);
                DebugLogList.LogXanaATK("towerTargetSave " + towerTargetSave);
                if (towerTargetSave.IsActivatedBy(Camp.XANA))
                    utils.SetActivatedTower(true, _sc.IsReplika());
                return towerTargetSave.IsActivatedBy(Camp.XANA);
            }
            DebugLogList.LogXanaATK("Xana initiative, tenterais bien d'activer une tour");
            if (_sc.IsReplika()) {
                ActiverTourRandom(false, false, _sc,
                    forcedTowerAttackType); //marche mieux pour les tentative de prise de controle de la tour avec le skid
            } else {
                ActiverTourRandom(false, true, _sc, forcedTowerAttackType);
            }
            bool a = HasActivatedTower_In(_sc) || GetAByOS<PrgCorruptedCode>(OSTarget.SC).IsAnticipatingAttack();
            if (_sc.IsReplika()) {
                utils.hasTowerReplikaAtkOnce = a;
            } else {
                utils.hasAnyTower_AtkOnce = a;
            }
            return a;
        }
        public void TryAttackOnStartup() {
            DebugLogList.LogXana("TryAttackOnStartup");
            if (utils.HasAnyLyokoAtkOnce)
                return;
            DebugLogList.LogXana("TryAttackOnStartupFollow");
            List<GameOptionName> randomStack = new List<GameOptionName>() {
                GameOptionName.xanaEarthAttacks,
                GameOptionName.xanaMonsterCoreAttacks,
                GameOptionName.xanaMonsterSkidAttacks,
                GameOptionName.xanaDrainSkidAttacks,
                GameOptionName.xanaDrainCoreAttacks,
                GameOptionName.xanaCorruptedCoreAttacks
            };
            for (var index = randomStack.Count - 1; index >= 0; index--) {
                GameOptionName a = randomStack.RemoveRandom();
                if (a != GameOptionName.none && LevelableOption.GetOp(a).currentValue > 0) {
                    DebugLogList.LogXana("TryAttackOnStartupFollow - "+a);
                    switch (a) {
                        case GameOptionName.xanaEarthAttacks:
                            if (TryTowerAttack(VarG.scLyoko))
                                return;
                            break;
                        case GameOptionName.xanaMonsterCoreAttacks:
                            if (TryMonsterCoreAttack(true))
                                return;
                            break;
                        case GameOptionName.xanaMonsterSkidAttacks:
                            XanaTickDisplay.UpdateTickPanel(TickPanelType.skidGarageAtk);
                            if (TryMonsterSkidAttack(true))
                                return;
                            break;
                        case GameOptionName.xanaDrainSkidAttacks:
                            if (TrySkidDrainAttack(true))
                                return;
                            break;
#if UNITY_EDITOR
                        case GameOptionName.xanaDrainCoreAttacks:
                            if (TryDrainCoreAttack(true))
                                return;
                            break;
                        case GameOptionName.xanaCorruptedCoreAttacks:
                            if (TryCorruptedCoreAttack(true))
                                return;
                            break;
#endif
                    }
                }
            }
            DebugLogList.LogXana("TryAttackOnStartup Ended");
        }
        private void DoEarthListRandomChances(ref List<GameOptionName> _list) {
            if (LevelableOption.GetOp(GameOptionName.xanaEarthAttacks).ToBool()) {
                foreach (GameOptionName earthAtk in SavedGameOptions.attackPrefList) {
                    if (LevelableOption.GetOp(earthAtk).currentValue > 0)
                        AddChancesIn(ref _list, earthAtk);
                }
            }
        }
        private void AddChancesIn(ref List<GameOptionName> list, GameOptionName gameOptionName) {
            for (int a = 0; a < LevelableOption.GetOp(gameOptionName).currentValue; a++)
                list.Add(gameOptionName);
        }
        public bool TryAttackWhileWaiting() {
            if (utils.hasAnyTower_AtkOnce)
                MSG.AffCriticalInfo("hasTowerAtkOnce should be false");
            if (utils.HasAnyLyokoAtkOnce)
                MSG.AffCriticalInfo("HasLyokoAtkOnce should be false");
            if (utils.hasMonsterCoreAtkOnce)
                MSG.AffCriticalInfo("HasCoreAtkOnce should be false");
            if (utils.hasMonsterSkidAtkOnce)
                MSG.AffCriticalInfo("hasGarageAtkOnce should be false");
            if (utils.hasSkidReplikaAtkOnce)
                MSG.AffCriticalInfo("hasSkidSurfaceAtkOnce should be false");
            if (utils.hasDrainSkidAtkOnce)
                MSG.AffCriticalInfo("hasDrainSkidAtkOnce should be false");
            List<GameOptionName> randomChancesPerOption = new List<GameOptionName>();
            DoEarthListRandomChances(ref randomChancesPerOption);
            AddChancesIn(ref randomChancesPerOption, GameOptionName.xanaMonsterCoreAttacks);
            AddChancesIn(ref randomChancesPerOption, GameOptionName.xanaMonsterSkidAttacks);
            AddChancesIn(ref randomChancesPerOption, GameOptionName.xanaDrainSkidAttacks);
            string logList = randomChancesPerOption.Aggregate("", (current, option) => current + (option + " | "));
            while (randomChancesPerOption.Count > 0) {
                int chance = Random.Range(0, randomChancesPerOption.Count);
                DebugLogList.LogXanaATK("TryAttack chanceHit: " + chance);
                switch (randomChancesPerOption[chance]) {
                    case GameOptionName.xanaMonsterCoreAttacks:
                        if (TryMonsterCoreAttack(true, false)) {
                            HighlightLogEnd(randomChancesPerOption[chance]);
                            return true;
                        }
                        HighlightLogRed(randomChancesPerOption[chance]);
                        randomChancesPerOption.RemoveAll(it => it == randomChancesPerOption[chance]);
                        break;
                    case GameOptionName.xanaMonsterSkidAttacks:
                        if (TryMonsterSkidAttack(true, false)) {
                            HighlightLogEnd(randomChancesPerOption[chance]);
                            return true;
                        }
                        HighlightLogRed(randomChancesPerOption[chance]);
                        randomChancesPerOption.RemoveAll(it => it == randomChancesPerOption[chance]);
                        break;
                    case GameOptionName.xanaDrainSkidAttacks:
                        if (TrySkidDrainAttack(true, false)) {
                            HighlightLogEnd(randomChancesPerOption[chance]);
                            return true;
                        }
                        HighlightLogRed(randomChancesPerOption[chance]);
                        randomChancesPerOption.RemoveAll(it => it == randomChancesPerOption[chance]);
                        break;
                    default: //earth attacks
                        TowerAttackType towerAttackTypeToSet = TowerAttackType.NONE;
                        foreach (TowerAttackType towerAtk in Enum.GetValues(typeof(TowerAttackType))) {
                            if (randomChancesPerOption[chance].ToString().ToLower().Contains(towerAtk.ToString().ToLower()))
                                towerAttackTypeToSet = towerAtk;
                        }
                        if (TryTowerAttack(VarG.scLyoko, towerAttackTypeToSet)) {
                            HighlightLogEnd(randomChancesPerOption[chance]);
                            return true;
                        }
                        HighlightLogRed(randomChancesPerOption[chance]);
                        randomChancesPerOption.RemoveAll(it => it == randomChancesPerOption[chance]);
                        break;
                }
            }
            void HighlightLogEnd(GameOptionName option) {
                logList = logList.Replace(option.ToString(), "<color=#28FF00FF>" + option + "</color>");
                DebugLogList.LogXanaATK("TryAttack for waiting LISTED: " + logList);
            }
            void HighlightLogRed(GameOptionName option) {
                logList = logList.Replace(option.ToString(), "<color=#B93E41FF>" + option + " </color>");
            }
            DebugLogList.LogXanaATK("TryAttack for waiting - None Found: " + logList);

            //if none has been triggered, we default to try tower attack again
            return TryTowerAttack(VarG.scLyoko);

            //en théorie, xanaSectorDestruction est inutilisable par Xana car on ne peut pas 
            //attendre avec une Aelita xanatifiée indéfinitivement chez Xana, seul moyen pour
            //lui de détruire un territoire
        }
        public bool TryTowerAtkAsSimultaneous() {
            List<GameOptionName> randomChancesPerOption = new List<GameOptionName>();
            DoEarthListRandomChances(ref randomChancesPerOption);
            if (randomChancesPerOption.Count > 0) {
                int chance = Random.Range(0, randomChancesPerOption.Count);
                TowerAttackType towerAttackTypeToSet = TowerAttackType.NONE;
                foreach (TowerAttackType towerAtk in Enum.GetValues(typeof(TowerAttackType))) {
                    if (randomChancesPerOption[chance].ToString().ToLower().Contains(towerAtk.ToString().ToLower()))
                        towerAttackTypeToSet = towerAtk;
                }
                return TryTowerAttack(VarG.scLyoko, towerAttackTypeToSet);
            }
            return false;
        }
        public override void UpdateTimer() {
            if (XanaUtilsData.IsKnockedOut)
                return;

            //fix issues related to tower attack not declaring from XanaCode if game speeded up
            if (PrgCorruptedCode.Instance.IsAnticipatingAttack())
                return;
            if (TGestionXanatifies.TryDecrement())
                hiveMind.Gerer_Xanatifies();
            if (TSkidReplikaAttack.TryDecrement()) {
                XanaTickDisplay.UpdateTickPanel(TickPanelType.skidSurfaceAtk);
                TryReplikaSkidReact();
            }
            if (TMonsterSkidAttack.TryDecrement()) {
                XanaTickDisplay.UpdateTickPanel(TickPanelType.skidGarageAtk);
                TryMonsterSkidAttack();
            } else {
                TryMonsterSkidReact_IfSkidBackInGarageWithMonsters();
            }
            if (TMonsterCoreAttack.TryDecrement()) {
                TryMonsterCoreAttack();
            }
            if (TReactivity.TryDecrement()) {
                XanaTickDisplay.UpdateTickPanel(TickPanelType.reactivity);
                DebugXana("T-Reactivity (persecution/tower defense) | XanaUtilsData.CanAttackAtAnyTime = " + XanaUtilsData.CanAttackAtAnyTime);
                //persecuter en toute fin permet d'utiliser les monstres restants.
                //note que même si xanaRandomMonster est désactivé, on garde ça pour les boss, dont les apparitions restent possibles
                hiveMind.REACT_LwProximity();
                hiveMind.REACT_LwActivityInTower();
                hiveMind.REACT_OnFranzTowerStillActivated();
                hiveMind.REACT_DefendActivatedTower();
                hiveMind.REACT_OnSkidEnergyzing();
                //ce que l'on peut aussi voir c'est la vitesse de "rechargment" de Xana, notamment en sandbox.
                //'activation tour' vaut aussi pour une activation de Xana lui même, et donc la défense de la tour
                //pour l'instant les monstres ne peuvent toujours pas se détacher d'une tour qu'ils défendaient
                if (XanaUtilsData.CanAttackAtAnyTime) {
                    //initiative désactivée par l'activation d'une tour, on le reactive si besoin
                    bool coreAttackCanStart = false;
                    bool skidAttackCanStart = false;
                    if (!TMonsterCoreAttack.IsRunning() && LyokoGuideUtilities.GetTotalLG_AttackingCore() <= 0 &&
                        LevelableOption.GetOp(GameOptionName.xanaMonsterCoreAttacks).currentValue > 0) {
                        coreAttackCanStart = true;
                    }
                    if (!TMonsterSkidAttack.IsRunning() &&
                        LyokoGuideUtilities.GetTotalLG_AttackingSkid_InGarage() <= 0 &&
                        LevelableOption.GetOp(GameOptionName.xanaMonsterSkidAttacks).currentValue > 0) {
                        skidAttackCanStart = true;
                    }
                    if (coreAttackCanStart && skidAttackCanStart) {
                        if (Random.Range(0, 2) == 0) {
                            TMonsterSkidAttack.Launch();
                        } else {
                            TMonsterCoreAttack.Launch();
                        }
                    } else {
                        if (skidAttackCanStart)
                            TMonsterSkidAttack.Launch();
                        if (coreAttackCanStart)
                            TMonsterCoreAttack.Launch();
                    }
                }
            }
            if (TReactivity.IsRunning() && XanaUtilsData.CanAttackAtAnyTime && utils.HasAnyLyokoAtkOnce) {
                //fully complete skidDrain & coreDrain attacks:
                /*if (LyokoGuideUtilities.GetTotalLG_AttackingCore() == 0 &&
                    LyokoGuideUtilities.GetTotalLG_AttackingSkid_InGarage() == 0 &&
                    !HasActivatedTower_In(VarG.scLyoko)) {
                    if (!Skidbladnir.instance.IsSkidbladnirDrained() && !VarG.scLyoko.core.IsDrained()) {
                        bool continuerAtkComplete = true;
                        if (utils.CanAttackMore && !utils.hasAnyTower_AtkOnce) {
                            //add tower attack if still possible
                            if (TryTowerAtkAsSimultaneous())
                                continuerAtkComplete = false;
                        }
                        if (continuerAtkComplete)
                            OnAtkComplete();
                    }
                }*/
                //true simultaneous
                if (!HasActivatedTower_In(VarG.scLyoko)) {
                    bool continuerAtkComplete = true;
                    //add tower attack if still possible
                    if (utils.CanAttackMore && !utils.hasAnyTower_AtkOnce) {
                        if (TryTowerAtkAsSimultaneous())
                            continuerAtkComplete = false;
                    }
                    if (continuerAtkComplete &&
                        LyokoGuideUtilities.GetTotalLG_AttackingCore() == 0 &&
                        LyokoGuideUtilities.GetTotalLG_AttackingSkid_InGarage() == 0 &&
                        !Skidbladnir.instance.IsSkidbladnirDrained() && !VarG.scLyoko.core.IsDrained()) {
                        OnAllAtkCompleted();
                    }
                }
            }
            if (TSendEnergy.TryDecrement()) {
                XanaTickDisplay.UpdateTickPanel(TickPanelType.sendEnergy);
                SendEnergy();
            }
            if (LyokoGuerrierUtils.GetLwTranslated(XanaControlStatus.Definitive).Length > 0 && LyokoGuerrierUtils.GetLwTranslated(XanaControlStatus.None).Length == 0 && CCUnEnergyzeXW == null) {
                MSG.AffDebugInfo("no more lw translated, xana launch timer before unenergyzing xanaWarrior");
                CCUnEnergyzeXW = GameSceneTime.instance.StartDelayedAction(30, () => ConditionalDoUnEnergyzeXW());
            }
        }
        public void OnAllAtkCompleted() {
            DebugXana("NO MORE ATTACK, TIME BEFORE NEXT ATTACK RESETTED, REACTIVITY KEPT");
            RazFrom_Restart_Rvlp_TimePassed();
            ResetAttackTime(true);
            xana.TReactivity.Launch();
        }
        public void OnPassingXanaAtkTime_WithoutAtkHappening() {
            MSG.AffDebugInfo("NO ATTACK has been triggered during the time it was supposed to (no lyoko ?), TIME BEFORE NEXT ATTACK RESETTED, REACTIVITY KEPT");
            DebugXana("NO ATTACK has been triggered during the time it was supposed to (no lyoko ?), TIME BEFORE NEXT ATTACK RESETTED, REACTIVITY KEPT");
            RazFrom_Restart_Rvlp_TimePassed();
            ResetAttackTime(true);
            xana.TReactivity.Launch();
        }
        public void OnTimeSkipped_OrMissionEnded_Normal() {
            //serves to fix, because if we start with a non-tower atk (core etc...),
            //it won't register an 'attack complete' when done 'cause others can be done,
            //so we reset there
            RazFrom_Restart_Rvlp_TimePassed(false, true, false);
            if (VarG.gardienFonction.created && !VarG.gardienFonction.IsUsingItsPower()) {
                VarG.gardienFonction.RazClassique();
            }
            if (VarG.meduseFonction.created && !VarG.meduseFonction.IsUsingItsPower()) {
                VarG.meduseFonction.RazClassique();
            }
        }
        public void RazFrom_Restart_Rvlp_TimePassed(bool resetTimers = false, bool stopTimers = true,
            bool resetRandomTaken = true) {
            CU_tillKnockedOutEnds?.Reset();
            if (VarG.scLyoko != null)
                utils.SetTowerTarget(VarG.scLyoko, null);
            if (VarG.replika != null)
                utils.SetTowerTarget(VarG.replika.GetSuperc(), null); //par défaut
            if (VarG.scLyoko != null)
                utils.SetSendEnergyRegularlyTo(VarG.scLyoko, null);
            if (VarG.replika != null)
                utils.SetSendEnergyRegularlyTo(VarG.replika.GetSuperc(), null);
            if (resetRandomTaken)
                lastRandomTaken = 0;
            List<MainTimer> allTimers = new List<MainTimer>() {
                TMonsterCoreAttack,
                TMonsterSkidAttack,
                TSkidReplikaAttack,
                TDrainSkidAttack,
                TDrainCoreAttack,
                TReactivity
            }; //TEarthAttacks not included
            if (resetTimers)
                ResetAllTimers();
            if (stopTimers)
                StopAllTimers();
            RemoveHasDoneSmth();
            utils.used_clonePoly_ParDecryptage = false;
            utils.ShuffleMaxSimultaneousAttack();
            void ResetAllTimers() {
                foreach (var timer in allTimers)
                    timer.Reset();
            }
            void StopAllTimers() {
                foreach (var timer in allTimers)
                    timer.Stop();
            }
        }
        public void Cancel_TryTakeControlTower(Supercalculateur _sc) {
            //au cas où xana ai commencer à envoyer de l'énergie, on l'enlève.
            if (utils.GetTowerTarget(_sc) == null)
                return;
            Debug.Log("Cancel_TentativeDePriseDeControle");
            utils.SetSendEnergyRegularlyTo(_sc, null);
            if (utils.GetTowerTarget(_sc).energyProfile.GetCampMineur() == Camp.XANA)
                utils.GetTowerTarget(_sc).energyProfile.GiveBackEnergy(true, false);
            utils.SetTowerTarget(_sc, null);
        }
        [Button]
        public void TryTowerTakeControl(Supercalculateur _sc) {
            DebugXana("TryTowerTakeControl " + _sc.nom);
            //Xana peut avoir la volonté d'activer une tour/ou d'en prendre controle, mais ne pas avoir assez d'énergie le pousse à ré-essayer à chaque fois
            if (utils.GetTowerTarget(_sc) == null)
                return;
            switch (utils.GetTowerTarget(_sc).activation) {
                case Camp.XANA:
                    Cancel_TryTakeControlTower(_sc);
                    return;
                case Camp.HOPPER:
                    //on fait abandonner Xana, il tentera de tout façon de détruire la tour
                    Cancel_TryTakeControlTower(_sc);
                    return;
                case Camp.JEREMIE:
                    if (GetAByOS<PrgJournalDecrypt>(OSTarget.SC).T_Decryptage.IsRunning() ||
                        (VarG.skidbladnir.IsDockedToTower() && VarG.skidbladnir.GetDockedTower() ==
                            utils.GetTowerTarget(_sc).inUniverseID)) {
                        utils.SetSendEnergyRegularlyTo(_sc, utils.GetTowerTarget(_sc));
                    } else {
                        Cancel_TryTakeControlTower(_sc);
                    }
                    return;
                default:
                    //si Xana à déjà une tour, il se concentre sur celle existante
                    if (_sc.IsReplika()) {
                        if (HasActivatedTower_In(_sc)) {
                            Cancel_TryTakeControlTower(_sc);
                        } else {
                            //TODO missing askEnergy
                            energyProvider.SendTo(utils.GetTowerTarget(_sc).energyProfile,
                                DepensesEnergetiques.tour.maintien);
                        }
                    } else {
                        //si Xana à déjà une tour ou que Jeremie à activer une tour, il se concentre sur celle existante
                        if ((!StoryConstantList.GetREF(StoryConstantType.xanaCanActivateMultipleTowers_InSameWorld)
                                .IsOn() && HasActivatedTower_In(_sc)) ||
                            (jeremie.HasActivatedTower_In(_sc) &&
                             Tour.ReturnActiveTowers(Camp.JEREMIE)[0].GetSuperc() == _sc &&
                             !Tour.ReturnActiveTowers(Camp.JEREMIE).Contains(utils.GetTowerTarget(_sc)))) {
                            Cancel_TryTakeControlTower(_sc);
                        } else {
                            //TODO missing askEnergy
                            energyProvider.SendTo(utils.GetTowerTarget(_sc).energyProfile,
                                DepensesEnergetiques.tour.maintien);
                        }
                    }
                    return;
            }
        }
        public bool ActiverTourRandom(bool excludeCarthage, bool delayXanaCode, Supercalculateur _sc, TowerAttackType forcedTowerAtkType = TowerAttackType.NONE) {
            if (HasActivatedTower_In(_sc))
                return false;
            listeToursActivables.Clear();
            foreach (Tour tourTest in Tour.listeDispo) {
                if (tourTest.GetSuperc() != _sc)
                    continue;
                if (excludeCarthage && tourTest.GetTerritoire() == VarG.carthage)
                    continue;
                //we forbid xana to activate only for lyoko or wayTower of Lyoko
                if (_sc != null) {
                    if (jeremie.HasActivatedTower_In(_sc) && _sc.IsLyoko())
                        continue;
                    if (_sc.IsReplika() && tourTest.IsActivatedBy(Camp.JEREMIE))
                        continue;
                    if (tourTest.passage && _sc.IsLyoko())
                        continue;
                }
                //on enlève celles dans lesquelles se trouve Aelita
                if (VarG.aelita.IsVirt() && VarG.aelita.GetGuide().inTour && VarG.aelita.GetGuide().UnivIDAccessibleTower == tourTest.inUniverseID)
                    continue;
                //puis on enlève aussi les tours proches des LG (uniquement non xana)
                if (LyokoGuideUtilities.IsThereCloseFromTower(tourTest, true))
                    continue;
                if (tourTest.IsKo())
                    continue;
                AddTourActivableToList(tourTest);
            }
            if (_sc != null && _sc.IsReplika() && listeToursActivables.Count == 0) {
                //on fallback sur la tour activée par jérémie, et on essaye exceptionnellement d'en prendre le contrôle
                foreach (var tourTest in Tour.listeDispo.Where(tourTest => tourTest.IsActivatedBy(Camp.JEREMIE))) {
                    AddTourActivableToList(tourTest);
                    break;
                }
            }
            //DebugXana("listeToursActivables : "+listeToursActivables);
            if (listeToursActivables.Count > 0) {
                ActiverTour(listeToursActivables[Mathf.FloorToInt(Random.Range(0, listeToursActivables.Count))],
                    delayXanaCode, forcedTowerAtkType);
                return true;
            }
            DebugLogList.LogXanaATK("xana couldn't find a tower to activate");
            return false;
        }
        public override void ActiverTourFinal(Tour _t, bool fromXanaCode = false, TowerAttackType _forcedTowerAttackType = TowerAttackType.NONE, ProgramsF programOrigin = null) {
            utils.towerAttackTypeToSet = _forcedTowerAttackType;
            if (!GetA_SC<PrgRvlp>().CanAttack()) {
                Debug.LogWarning("attaque stoppée, le rvlp est trop proche de la fin ou la fenêtre de xana terrestre est toujours ouverte");
                return;
            }
            //on revérifie ça une seconde fois au cas où on ai appelé direct cette fonction
            if (!_t.CanBeCalledForActivation())
                return;
            utils.SetTowerTarget(_t.GetSuperc(), _t);
            DebugXana("xana - veut prendre Controle de la Tour (ID universel):  " + (utils.GetTowerTarget(_t.GetSuperc()).inUniverseID) + " / fromXanaCode:" + fromXanaCode);
            //with delay for DoAttack
            //si y'a pas la connexion à Lyoko, on la lance direct vu que la fenêtre peut pas s'ouvrir
            if (fromXanaCode && GetAByOS<PrgLyokoConnect>(OSTarget.SC).GetStatus() == ConnexionStatus.CONNECTED) {
                PrgCorruptedCode.Instance.Execution(false);
                return;
            }
            DoAttack(_t.GetSuperc());
        }
        public void AddTourActivableToList(Tour _tower) {
            listeToursActivables.AddMany(_tower.inUniverseID);
        }
        public void DoDefendTower(Tour tour, bool createMonstersFallback = true, SpecialOrder _specialOrder = SpecialOrder.NONE) {
            DebugXana("doDefendTower - start"); //1= par rapport à un decryptage
            int monstres_defendant_DEJA_tour =
                LyokoGuideUtilities.GetTotalLG_DefendingTower(tour.inUniverseID, LgTypes.Monstre, Camp.XANA);
            if (monstres_defendant_DEJA_tour > 1) {
                DebugXana("doDefendTower - inutile, xana à déjà assez de défenseurs de la tour");
                return;
            }
            if (hiveMind.GetMonsterAvailableForReorder(tour.GetTerritoire(), LyokoOrder.defendTower).Count > 0) {
                DebugXana("doDefendTower - il y a des monstres dispo pour défendre de la tour, on les utilise si il y a 1 seul défenseur de la tour ou moins");
                foreach (var _m in hiveMind.GetMonsterAvailableForReorder(tour.GetTerritoire(),
                             LyokoOrder.defendTower).TakeWhile(_m => monstres_defendant_DEJA_tour < 2)) {
                    _m.orderProfile.SetOrderTowerDefense(tour.inUniverseID);
                    monstres_defendant_DEJA_tour++;
                }
            } else {
                if (createMonstersFallback) {
                    //on ralenti la création des monstres pour défendre la tour.
                    DebugXana("doDefendTower - aucun monstre dispo pour défendre de la tour, on en créer de nouveaux si on le peut");
                    if (tour.turnsBefore_Redefending <= 0) {
                        XanaMonsterCreator.CreateStartum(tour, LyokoOrder.defendTower, _specialOrder);
                        if (_specialOrder != SpecialOrder.POLYMORPH_DEFEND_TOWER) { //les tours sont reservées 
                            LyokoGuerrier xanaWarrior = LyokoGuerrierUtils.GetRandomXanafiedLW_ToVirt();
                            if (xanaWarrior != null) {
                                DebugXana("doDefendTower - un xanaguerrier est dispo pour défendre la tour, xana l'envoie immédiatement après avoir créé sa vague de monstres");
                                //autrement l'envoi se faisait trop tard, après avoir battu la 1ere vague de monstres
                                XanaMonsterCreator.CreateSuite(CRDManager.C(tour, false), xanaWarrior.character.ToString(), 0, LyokoOrder.defendTower, "", tour);
                            }
                        }
                        tour.turnsBefore_Redefending = 2;
                    } else {
                        tour.turnsBefore_Redefending--;
                    }
                } else {
                    DebugXana("doDefendTower - aucun monstre dispo pour défendre de la tour, mais on évite d'en créer de nouveaux");
                }
            }
        }
        public static void DebugXana(params string[] rest) {
            string messageFinal = "";
            foreach (string a in rest)
                messageFinal += a + " ";
            DebugLogList.LogXana(messageFinal);
        }
        public void LoadFromGameSave(XanaData data) {
            time_tillAttack.timeUnitDate.LoadFrom_Savegame(data.time_tillAttack);
            lastRandomTaken = data.lastRandomTaken;
            utils = data.utils;
            DebugLogList.LogXana("LoadFromGameSave: time_tillAttack.timeUnitDate "+data.time_tillAttack.timeUnitDate);
        }
    }
}
