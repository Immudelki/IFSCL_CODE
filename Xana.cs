using System;
using System.Collections.Generic;
using UnityEngine;
namespace IFSCL {
    using static ProgramsF;
    using Programs;
    using RealWorld;
    using VirtualWorld;
    using Save;
    public enum LyokoOrder {
        nothing,
        attackKey,
        attackMaze,
        attackDomeVoid,
        attackDomeBridge,
        attackCore,
        attackSkidInGarage,
        attackSkidOnSurface,//TODO
        attackSkidInSea,//TODO
        drainSkidInGarage,
        drainCore,
        defendTower,
        attackTower,
        destroySector,
        harass,
        throwLG,
        throwItself,
        landNested,
        diveInSea
    };
    public enum SpecialOrder {
        NONE,
        POLYMORPH_DEFEND_TOWER,
        SCYPHOZOA_ON_SKID_IN_GARAGE,
    }
    public class Xana : Camps {
        public Xana(Camp _camp) {
            camp = _camp;
        }
        //Dès lors qu'aucune attaque n'est plus en activité (voir conditions), on reset le timer de Xana

        //ATTACKS IN CURRENT TIMEFRAME
        public bool hasTowerAtkOnce = false; //initiative
        public bool hasCoreAtkOnce = false;
        public bool hasGarageAtkOnce = false;
        public bool hasDrainSkidAtkOnce = false;
        public bool hasDrainCoreAtkOnce = false;
        public static bool canSpawnMantasOnSurface = true;
        public static bool canSpawnTarentulas = true;

        public List<int> listeToursActivables = new List<int>();
        public MainTimer TEarthAttacks = new MainTimer();
        public MainTimer TSkidGarageAttack = new MainTimer();
        public MainTimer TDrainSkidGarageAttack = new MainTimer();
        public MainTimer TDrainCoreAttack = new MainTimer();
        public MainTimer TCoreAttack = new MainTimer();
        public MainTimer TReactivity = new MainTimer();
        public MainTimer TGestionXanatifies = new MainTimer();
        public MainTimer TSendEnergy = new MainTimer();

        public bool manipVirtActives = false;
        public bool manipVehicleVirtActives = false;
        // sur carthage, elle tente de Xanatifier n'importe quel lyokoguerrier pour l'envoyer attaquer le coeur, sur les autres territoires, elle tente de Xanatifier Aelita
        public int turnsBefore_Redefending = 0;
        public int turnsBefore_RedefendingCarthage = 0;
        public bool used_clonePoly_ParDecryptage = false;
        public bool used_clonePoly_ForRandomAttack = false;
        public Tour towerTarget = null;
        public TowerAttackType towerAttackTypeToSet = TowerAttackType.NONE;
        public int waitturns_before_newMonsters = 3;
        //la toute première materialisation de monstres doit être légèrement retardée pour pas harasser le joueur trop vite
        public int waitturns_before_newMonsters_attaqueTour = 2;
        public Tour sendEnergyRegularlyTo = null;
        public TimeUnitDateManager time_tillAttack = new TimeUnitDateManager();
        public static int xanaTotalSleepFJ = 12; //là on dit 24h (12FJ), c'est 6H (3FJ) en 41X

        //LyokoGuide.Create interdit, on doit passer par createMonstreSuite
        public bool nextAttackFaster = false;
        public static bool dbgForceGardien = false;
        private readonly List<int> chancesVariety = new List<int>();
        private List<int> bossChancesVariety = new List<int>();

        public void ResetAttackTime(bool withPushNotif) {
            DebugXana("ResetAttackTime");
            //TODO 42X revoir les valeur par rapport au mockup agenda
            /*
            if (nextAttackFaster) { //si la dernière desactivation est due à une tour détruite (par destruction de territoire), alors la prochaine attaque est plus rapide
                if (withPushNotif) {
                    time_tillAttack.Set(3, AgendaTimeMarkerType.xanaCanReattack);
                } else {
                    time_tillAttack.Set(3); 
                }
                nextAttackFaster = false;
            } else {
                if (withPushNotif) {
                    time_tillAttack.Set(6, AgendaTimeMarkerType.xanaCanReattack);
                } else {
                    time_tillAttack.Set(6);
                }
            }*/
            nextAttackFaster = false;
            if (withPushNotif) {
                time_tillAttack.Set(xanaTotalSleepFJ, AgendaTimeMarkerType.xanaCanReattack);
            } else {
                time_tillAttack.Set(xanaTotalSleepFJ);
            }

            TCoreAttack.Stop();
            TEarthAttacks.Stop();
            TSkidGarageAttack.Stop();
            TDrainSkidGarageAttack.Stop();
            TDrainCoreAttack.Stop();
            TGestionXanatifies.Start();
            hasTowerAtkOnce = false;
            hasCoreAtkOnce = false;
            hasGarageAtkOnce = false;
            hasDrainSkidAtkOnce = false;
            hasDrainCoreAtkOnce = false;
        }
        //ne jamais forcer le temps de l'attaque à se réduire manuellement 'ForceAttackTime_ToNow'
        //car sinon ça rend forçément des situations impossibles avec les LG qui n'ont pas eu leur
        //temps réglementaire pour être revirtualisables
        public void SetupBeforeOption() {
            canSpawnMantasOnSurface = true;
            canSpawnTarentulas = true;
        }
        //NOTE LA FONCTION PRISE DE CONTROLE DE TOUR OU DE MONSTRES/LYOKOGUERRIER DOIT ETRE SIMILAIRE ENTRE XANA ET HOPPER;
        public void Initialisation() {
            DebugXana("XanaInitialisation");
            //confirmInitiativeLevel
            TCoreAttack.Stop();
            TEarthAttacks.Stop();
            TSkidGarageAttack.Stop();
            TDrainSkidGarageAttack.Stop();
            TDrainCoreAttack.Stop();
            switch (LevelableOption.GetOp(GameOptionName.xanaEarthAttacks).currentValue) {
                case 0:
                    TEarthAttacks.SetTickingTime(999);
                    break;
                case 1:
                    TEarthAttacks.SetTickingTime(25);
                    break;
                case 2:
                    TEarthAttacks.SetTickingTime(15);
                    break;
                case 3:
                    TEarthAttacks.SetTickingTime(7); //toutes les 3 minutes
                    break;
            }
            switch (LevelableOption.GetOp(GameOptionName.xanaGarageAttacks).currentValue) {
                //sachant que Xana à une chance sur 2 de faire l'attaque uniquement si des LG sont déjà dans le cinquième territoire
                //et une chance sur 10 si aucun n'est dispo pour être virtualisé à l'usine et qu'ils ne sont en dehors de Lyoko
                case 0:
                    TSkidGarageAttack.SetTickingTime(999);
                    break;
                case 1:
                    TSkidGarageAttack.SetTickingTime(25);
                    break;
                case 2:
                    TSkidGarageAttack.SetTickingTime(15);
                    break;
                case 3:
                    TSkidGarageAttack.SetTickingTime(7); //toutes les 3 minutes
                    break;
            }
            switch (LevelableOption.GetOp(GameOptionName.xanaScyphozoaDrainSkid).currentValue) {
                case 0:
                    TDrainSkidGarageAttack.SetTickingTime(999);
                    break;
                case 1:
                    TDrainSkidGarageAttack.SetTickingTime(25);
                    break;
                case 2:
                    TDrainSkidGarageAttack.SetTickingTime(15);
                    break;
                case 3:
                    TDrainSkidGarageAttack.SetTickingTime(7); //toutes les 3 minutes
                    break;
            }
            //TODO 44X
            /*
            switch (LevelableOption.GetOp(GameOptionName.xanaScyphozoaDrainCore).currentValue) {
                case 0:
                    TDrainCoreAttack.SetTickingTime(999);
                    break;
                case 1:
                    TDrainCoreAttack.SetTickingTime(25);
                    break;
                case 2:
                    TDrainCoreAttack.SetTickingTime(15);
                    break;
                case 3:
                    TDrainCoreAttack.SetTickingTime(7); //toutes les 3 minutes
                    break;
            }*/
            switch (LevelableOption.GetOp(GameOptionName.xanaCoreAttacks).currentValue) {
                //sachant que Xana à une chance sur 2 de faire l'attaque uniquement si des LG sont déjà dans le cinquième territoire
                //et une chance sur 10 si aucun n'est dispo pour être virtualisé à l'usine et qu'ils ne sont en dehors de Lyoko
                case 0:
                    TCoreAttack.SetTickingTime(999);
                    break;
                case 1:
                    TCoreAttack.SetTickingTime(25);
                    break;
                case 2:
                    TCoreAttack.SetTickingTime(15);
                    break;
                case 3:
                    TCoreAttack.SetTickingTime(7); //toutes les 3 minutes
                    break;
            }
            TReactivity.SetTickingTime(1); // en secondes, toutes les 10-12 secondes (*2 par waitturns_before_newMonsters au besoin)
            TReactivity.Start();
            TSendEnergy.SetTickingTime(0.1f);
            TSendEnergy.Start();
            DebugXana("TReactivity " + TReactivity.IsRunning());
            TGestionXanatifies.SetTickingTime(0.2f);
            TGestionXanatifies.Start();
            nextAttackFaster = false;
            ResetAttackTime(false);
            if (VarG.gameMode != GameMode.Story) {
                switch (LevelableOption.GetOp(GameOptionName.xanaFirstAwakening).currentValue) {
                    case 1:
                        //faire un try tower attack direct empêche le temps d'avoir un boot et donc la connexion à lyoko de faite
                        //on garde donc un petit délai
                        if (LevelableOption.GetOp(GameOptionName.xanaEarthAttacks).currentValue > 0) {
                            time_tillAttack.DESACTIVER();
                            GameScene.Instance.StartCoroutine(GameScene.Instance.DelayedAction(3, () => xana.TryTowerAttack(true)));
                        }
                        break;
                    case 2:
                        time_tillAttack.AddTimeUnits(xanaTotalSleepFJ);
                        break;
                }
                //miniature = direct, 30m, 6h, 13h
            }
        }
        public void DisableXana(bool withDbgMessage = false) {
            Elevator3D.XanaHack(false);
            TReactivity.Stop();
            TEarthAttacks.Stop();
            TCoreAttack.Stop();
            TSkidGarageAttack.Stop();
            TDrainSkidGarageAttack.Stop();
            TDrainCoreAttack.Stop();
            TSendEnergy.Stop();
            TGestionXanatifies.Stop();
            for (int a = 0; a < GetActivatedTowers().Length; a++) {
                Tour t = GetActivatedTowers()[a];
                t.Desactiver();
            }
            if (withDbgMessage)
                MSG.AffDebugInfo("xana fully disabled");
        }

        public void EnableXana() {
            TReactivity.Start();
            TSendEnergy.Start();
            TGestionXanatifies.Start();
            //les autres timers sont gérés par les canAttackAtAnyTime
        }

        public bool CanAttackAtAnyTime() {
            return !time_tillAttack.IsRUNNING();
        }

        public bool DoOrAim_Attack(bool core, bool sector, bool skidAttack, bool skidDrain) {
            //si monstre ou LG xanatifié
            foreach (LyokoGuide lg in LyokoGuide.liste) {
                if (lg.camp == Camp.XANA) {

                    if (core && lg.orderProfile.savedOrder == LyokoOrder.attackCore && (lg.carthageProfile.pos == CarthagePos.domeVoid || lg.carthageProfile.pos == CarthagePos.coreRoom)
                        && VarG.scLyoko.GetStatus() == VirtualBuildStatus.created)
                        return true;
                    if (sector && lg.lgType == LgTypes.LyokoGuerrier && lg.orderProfile.savedOrder == LyokoOrder.destroySector
                        && Territoire.IsATerritoire(lg.orderProfile.savedOrderStringValue) && Territoire.GetByName(lg.orderProfile.savedOrderStringValue).GetStatus() == VirtualBuildStatus.created)//xanatifié only
                        return true;
                    if (skidAttack && lg.orderProfile.savedOrder == LyokoOrder.attackSkidInGarage && (lg.carthageProfile.pos == CarthagePos.garageElevatorRoom || lg.carthageProfile.pos == CarthagePos.garageSkid)
                        && VarG.garageSkid.GetStatus() == VirtualBuildStatus.created && (VarG.skidbladnir.IsMater() && VarG.skidbladnir.IsKindaDockedToGarage())) //xanatifié only
                        return true;
                    if (skidDrain && lg.orderProfile.savedOrder == LyokoOrder.drainSkidInGarage && (lg.carthageProfile.pos == CarthagePos.garageElevatorRoom || lg.carthageProfile.pos == CarthagePos.garageSkid)
                        && VarG.garageSkid.GetStatus() == VirtualBuildStatus.created && (VarG.skidbladnir.IsMater() && VarG.skidbladnir.IsKindaDockedToGarage()))
                        return true;
                }
            }
            return false;
        }

        public void SendEnergy() {
            //TODO DEMANDER ASKENERGIE ICI !!
            if (sendEnergyRegularlyTo != null && GetAByOS<PrgJournalDecrypt>(OSTarget.SC).tourDecryptage != null && !GetAByOS<PrgJournalDecrypt>(OSTarget.SC).tourDecryptage.IsActivatedBy(Camp.XANA) && !GetAByOS<PrgJournalDecrypt>(OSTarget.SC).tourDecryptage.IsKo()) {
                if (GetAByOS<PrgJournalDecrypt>(OSTarget.SC).tourDecryptage.IsActivatedBy(Camp.HOPPER)) {
                    Cancel_TentativeDePriseDeControle();
                    return;
                }
                bool verif = false;
                foreach (LyokoGuide LGMonstre in LyokoGuide.liste) {
                    if (LGMonstre.camp == Camp.XANA && LGMonstre.HasTowerProximity() && LGMonstre.id_UNIV_tourEntrable == sendEnergyRegularlyTo.ID_universel) {
                        verif = true;
                        break;
                    }
                }
                if (verif) {
                    //si Xana à des monstres près de la tour, la prise de controle est accélérée
                    energyManager.SendTo(GetAByOS<PrgJournalDecrypt>(OSTarget.SC).tourDecryptage.energyProfile, 30);
                } else {
                    //10 to 15 in 38X, car sans ses monstres, xana n'arrivait pas à rattraper
                    energyManager.SendTo(GetAByOS<PrgJournalDecrypt>(OSTarget.SC).tourDecryptage.energyProfile, 15);
                }
            }
        }
        public override void ActiverTour(int _IDuniv = 0, bool fromXanaCode = false, TowerAttackType _forcedTowerAttackType = TowerAttackType.NONE, ProgramsF programOrigin = null) {
            towerAttackTypeToSet = _forcedTowerAttackType;
            if (!GetA_SC<PrgRvlp>().CanAttack(false)) {
                Debug.LogWarning("attaque stoppée, le rvlp est trop proche de la fin ou la fenêtre de xana terrestre est toujours ouverte");
                return;
            }
            if (Tour.GetByUnivID(_IDuniv).IsKo()) //on revérifie ça une seconde fois au cas où on ai appelé direct cette fonction
                return;
            towerTarget = Tour.GetByUnivID(_IDuniv);
            DebugXana("xana - veut prendre Controle de la Tour (ID universel):  " + (towerTarget.ID_universel - 1) + " / fromXanaCode:" + fromXanaCode);
            //with delay for DoAttack
            //si y'a pas la connexion à Lyoko, on la lance direct vu que la fenêtre peut pas s'ouvrir
            if (fromXanaCode && GetAByOS<PrgConnexionLyoko>(OSTarget.SC).GetStatus() == ConnexionStatus.CONNECTED) {
                PrgXanaCode.Instance.Execution(false);
                return;
            }
            DoAttack();
        }
        public bool TrySkidDrain(bool forced = false) {
            if (!VarG.skidbladnir.CanBeReachedInGarage(true))
                return false;
            if (!forced) {
                if (LevelableOption.GetOp(GameOptionName.xanaScyphozoaDrainSkid).currentValue <= 0 || hasDrainSkidAtkOnce || !CanAttackMore())
                    return false;
                if (!TDrainSkidGarageAttack.IsRunning())
                    TDrainSkidGarageAttack.Start();
                if (HasActivatedTower_OutsideLyoko()) {
                    DebugXana("drain skid empêché: Xana attaque déjà avec une tour EXTERIEURE à Lyoko");
                    return false;
                }
                if (!VarG.meduseFonction.CouldSpawnToDrain()) {
                    DebugXana("drain skid empêché: Meduse deja en utilisation ou doit encore attendre avant d'être réutilisable ou multitransmission en cooldown");
                    return false;
                }
                DebugXana("TrySkidDrain");

                //on teste déjà la chance avant, qui appelle la fonction TrySkidDrain elle même
                int chances = 5; //sur 10
                int chanceTest = UnityEngine.Random.Range(0, 11); //entre 0 et 10
                if (chanceTest > chances) {
                    DebugXana("drain skid empêchée: pas assez de chances: " + chanceTest + "/" + chances);
                    return false;
                }
            }
            CreateMonsters(null, LyokoOrder.drainSkidInGarage, SpecialOrder.SCYPHOZOA_ON_SKID_IN_GARAGE, "", VarG.carthageParam.skidbladnirAnchor);
            CrisisManager.Call(AtkAlertType.alertSkidDrained);
            hasDrainSkidAtkOnce = true;
            return true;
        }
        public bool TryCoreDrain(bool forced = false) {
            if (VarG.scLyoko.GetStatus() != VirtualBuildStatus.created)
                return false;
            if (!forced) {
                if (LevelableOption.GetOp(GameOptionName.xanaScyphozoaDrainCore).currentValue <= 0 || hasDrainCoreAtkOnce || !CanAttackMore())
                    return false;
                if (!TDrainCoreAttack.IsRunning())
                    TDrainCoreAttack.Start();
                if (HasActivatedTower_OutsideLyoko()) {
                    DebugXana("drain core empêché: Xana attaque déjà avec une tour EXTERIEURE à Lyoko");
                    return false;
                }
                if (!VarG.meduseFonction.CouldSpawnToDrain()) {
                    DebugXana("drain skid empêché: Meduse deja en utilisation ou doit encore attendre avant d'être réutilisable ou multitransmission en cooldown");
                    return false;
                }
                DebugXana("TryCoreDrain");

                //on teste déjà la chance avant, qui appelle la fonction TryCoreDrain elle même
                int chances = 5; //sur 10
                int chanceTest = UnityEngine.Random.Range(0, 11); //entre 0 et 10
                if (chanceTest > chances) {
                    DebugXana("drain core empêchée: pas assez de chances: " + chanceTest + "/" + chances);
                    return false;
                }
            }
            CreateMonsters(null, LyokoOrder.drainCore, SpecialOrder.SCYPHOZOA_ON_SKID_IN_GARAGE, "", VarG.carthageParam.core);
            CrisisManager.Call(AtkAlertType.alertCoreDrained);
            hasDrainCoreAtkOnce = true;
            return true;
        }
        public bool TrySEQUEL_SkidGarageAttack() {
            if (LevelableOption.GetOp(GameOptionName.xanaGarageAttacks).currentValue <= 0 || VarG.skidbladnir.GetStatus() != VirtualBuildStatus.created || !VarG.skidbladnir.CanBeReachedInGarage(false))
                return false;
            // If there's monsters in garage (likely because an attack already happened and 'stopped because you lift off the skid out of garage skid')
            // when bringing skid back to garage, it now check if there's monsters and will make them attack again, 
            // regardless of the blocker "has skid been attacked once / multiple attacks activated"
            bool doSequel = false;
            foreach (LyokoGuide _m in GetMonsterAvailableFor(VarG.carthage, LyokoOrder.attackSkidInGarage)) {
                if (_m.carthageProfile.pos == CarthagePos.garageSkid) { //specifically those in garageSkid, not those in elevatorRoom
                    doSequel = true;
                    _m.orderProfile.GiveCarthageOrder(_m, LyokoOrder.attackSkidInGarage); //faire des téléports ?
                }
            }
            if (doSequel) {
                CrisisManager.Call(AtkAlertType.alertSkidAttacked);
                return true;
            } else {
                return false;
            }
        }
        public bool TrySkidGarageAttack(bool forced = false) {
            //si l'attaque est FORCED, on s'oblige  à forcer xanaGarageAttacks à changer d'option 1 pour que le SEQUEL soit bien appelé
            //en effet, on ne peux pas utiliser hasGarageAtkOnce = true dans le SEQUEL car cette valeur est reset par ResetAttackTimer
            if (forced && LevelableOption.GetOp(GameOptionName.xanaGarageAttacks).currentValue <= 0) {
                LevelableOption.GetOp(GameOptionName.xanaGarageAttacks).SetValue(1);
            }
            if (VarG.skidbladnir.GetStatus() != VirtualBuildStatus.created || !VarG.skidbladnir.CanBeReachedInGarage(false))
                return false;
            if (!forced) {
                if (LevelableOption.GetOp(GameOptionName.xanaGarageAttacks).currentValue <= 0)
                    return false;

                if (hasGarageAtkOnce || !CanAttackMore()) {
                    DebugXana("skidAtk blocked: Xana has already atkOnce (or can't attack more because another attack already happened)");
                    return false;
                }

                if (HasActivatedTower_OutsideLyoko()) {
                    DebugXana("skidAtk blocked: Xana already is attacking with a tower external to Lyoko");
                    return false;
                }
                //on teste déjà la chance avant, qui appelle la fonction TrySkidGarageAttack elle même
                int chances = 5; //sur 10
                int chanceTest = UnityEngine.Random.Range(0, 11); //entre 0 et 10
                if (chanceTest > chances) {
                    DebugXana("skidAtk blocked: pas assez de chances: " + chanceTest + "/" + chances);
                    return false;
                }
                DebugXana("StartSkidGarageAttack");
                if (!TSkidGarageAttack.IsRunning())
                    TSkidGarageAttack.Start();
            }
            //note: si il y a au moins un monstre, le reste sera créé en voulant harceler les lyokoguerriers présents dans le garage skid
            foreach (LyokoGuide _m in GetMonsterAvailableFor(VarG.carthage, LyokoOrder.attackSkidInGarage)) {
                DebugXana(_m.nom, ", dispo pour aller attaquer le skid dans le garage");
                _m.orderProfile.GiveCarthageOrder(_m, LyokoOrder.attackSkidInGarage); //faire des téléports ?
            }
            for (int a = 0; a < 5; a++) {//max de 5 monstres pour attaquer le skid dans le garage
                if (LyokoGuideUtilities.GetTotalLG_AttackingSkid_InGarage() < 6) {
                    DebugXana("confirmation, il y'a moins de 6 monstres (actuellement " + LyokoGuideUtilities.GetTotalLG_AttackingSkid_InGarage() + ") allant attaquer ou attaquant le skid dans le garage donc on en créer");
                    CreateMonsters(null, LyokoOrder.attackSkidInGarage, 0, "", CarthageAppearPointElement.Get_MonsterGarageAppearPoint());
                }
            }
            CrisisManager.Call(AtkAlertType.alertSkidAttacked);
            hasGarageAtkOnce = true;
            return true;
        }
        private bool CanAttackMore() {
            return (LevelableOption.GetOp(GameOptionName.xanaMultipleAttacks).ToBool() || !HasAtkOnce());
        }
        private bool HasAtkOnce() {
            return hasTowerAtkOnce || hasCoreAtkOnce || hasGarageAtkOnce || hasDrainSkidAtkOnce || hasDrainCoreAtkOnce;
        }
        public bool TryCoreAttack(bool forced = false) {
            XanaTickDisplay.UpdateTickPanel(TickPanelType.coreAtk);
            if ((!forced && LevelableOption.GetOp(GameOptionName.xanaCoreAttacks).currentValue == 0) || VarG.scLyoko.GetStatus() != VirtualBuildStatus.created || VarG.carthage.GetStatus() != VirtualBuildStatus.created)
                return false;
            if (hasCoreAtkOnce || !CanAttackMore())
                return false;
            if (!TCoreAttack.IsRunning())
                TCoreAttack.Start();
            if (HasActivatedTower_OutsideLyoko()) {
                DebugXana("attaque coeur empêchée: Xana attaque déjà avec une tour EXTERIEURE à Lyoko");
                return false;
            }
            DebugXana("TryCoreAttack");
            int chances = 10; //sur 10
            //si il y a des LyokoGuerriers virtualisés, mais PAS dans Lyoko, on réduit les chances par 6 des monstres qui attaquent le coeur, mais on augmente si il y a déjà des LG
            if (LyokoGuideUtilities.GetAllLgOutside(VarG.scLyoko, LgTypes.LyokoGuerrier, null, false, Camp.JEREMIE).Count > 0) {
                chances -= 6;
            } else {
                if (LyokoGuideUtilities.GetAllLgIn(VarG.scLyoko, LgTypes.LyokoGuerrier, null, false, Camp.JEREMIE).Count > 0) {
                    chances += 3;
                }
            }
            //si il y a déjà des LyokoGuerriers dans carthage, on réduit les chances par le nb de LG d'avoir de nouveaux monstres qui attaquent le coeur
            chances -= LyokoGuideUtilities.GetAllLgIn(VarG.carthage, LgTypes.LyokoGuerrier, null, false, Camp.JEREMIE).Count;

            int chanceTest = UnityEngine.Random.Range(0, 11); //entre 0 et 10
            if (!forced && chanceTest > chances) {
                DebugXana("attaque coeur empêchée: pas assez de chances: " + chanceTest + "/" + chances);
                return false;
            }
            //note: si il y a au moins un monstre, le reste sera créé en voulant harceler les lyokoguerriers présents dans le coeur ?
            if (IsMonsterAvailableFor(VarG.carthage, LyokoOrder.attackCore)) {
                foreach (LyokoGuide LGMonstre in LyokoGuide.liste) {
                    if (LGMonstre != null && LGMonstre.camp == Camp.XANA && LGMonstre.nestedIN == null && !LGMonstre.T_chute.IsRunning() && LGMonstre.GetTerritoire() == VarG.carthage &&
                        LGMonstre.orderProfile.savedOrder != LyokoOrder.defendTower && LGMonstre.orderProfile.savedOrder != LyokoOrder.attackTower && LGMonstre.orderProfile.savedOrder != LyokoOrder.attackSkidInGarage && LGMonstre.orderProfile.harcelerWhichLG == "" &&
                        (LGMonstre.carthageProfile.pos == CarthagePos.coreRoom || LGMonstre.carthageProfile.pos == CarthagePos.domeVoid || (LGMonstre.carthageProfile.pos == CarthagePos.domeBridge && LGMonstre.HasVol()))) {
                        DebugXana(LGMonstre.nom, ", dispo pour aller attaquer le coeur de Lyoko");
                        LGMonstre.orderProfile.GiveCarthageOrder(LGMonstre, LyokoOrder.attackCore);
                    }
                }
            }
            for (int a = 0; a < 4; a++) {//max de 4 monstres pour attaquer le coeur
                if (LyokoGuideUtilities.GetTotalLG_AttaquantCoeur() < a) {
                    DebugXana("confirmation, y'a moins de " + a + " monstres allant attaquer ou attaquant le coeur de lyoko donc on en créer");
                    CreateMonsters(null, LyokoOrder.attackCore, 0, "", VarG.carthageParam.core);
                }
            }
            CrisisManager.Call(AtkAlertType.alertCoreAttacked);
            hasCoreAtkOnce = true;
            return true;
            //normalement en comptant les monstres créés naturellement pour harceler, ça devrait en faire assez
        }

        public void DoAttack() {
            if (!TEarthAttacks.IsRunning() && LevelableOption.GetOp(GameOptionName.xanaEarthAttacks).currentValue > 0)
                TEarthAttacks.Start();
            if (LevelableOption.GetOp(GameOptionName.xanaEarthAttacks).currentValue == 0)
                TryTowerAttack(true);
            TentativePriseDeControleTour();
            //karuzofix : https://docs.google.com/document/d/1Jawbdxnlb5od8yOqfJz1qmjdkYcJ8QxqnDMBhkQlg2Y/
            if (this.HasTourActive()) {
                this.hasTowerAtkOnce = true;
            }
        }

        public void Gerer_Xanatifies() {
            XanaTickDisplay.UpdateTickPanel(TickPanelType.manageXanafied);
            foreach (LyokoGuerrier LGuerrier in LyokoGuerrier.listeLG) {
                if (LGuerrier.IsVirt() && LGuerrier.controle != XanaControlStatus.None &&
                    !LGuerrier.GetGuide().battleProfile.inBattle && !LGuerrier.GetGuide().IsUnconscious() && !LGuerrier.GetGuide().T_chute.IsRunning()) {
                    DebugXana("gestion du xanatifié", LGuerrier.nom);
                    if (LGuerrier.IsAloneOnSector(true) && LGuerrier.GetGuide().orderProfile.savedOrder == LyokoOrder.harass && LGuerrier.GetGuide().orderProfile.savedOrderStringValue == "") {
                        LGuerrier.GetGuide().orderProfile.DeleteAllOrders();
                        // DebugXana("00");
                    }
                    if (LGuerrier.nom == "william" && VarG.aelita.IsVirt() && VarG.aelita.GetGuide() == LGuerrier.GetGuide().territoire && LevelableOption.GetOp(GameOptionName.franzHopperAlive).ToBool() && VarG.franzFonction.currentGuide != null) {
                        LGuerrier.GetGuide().GiveHarassOrder("aelita"); //vise aelita en PRIO pour aller la supersmoker si Franz est vivant
                                                                        // DebugXana("01");
                        return;
                    }
                    if (LGuerrier.GetGuide().orderProfile.savedOrder == LyokoOrder.nothing ||
                        LGuerrier.GetGuide().orderProfile.savedOrder == LyokoOrder.destroySector || //on continue de vérifier si on veux continuer l'ordre
                        LGuerrier.GetGuide().orderProfile.savedOrder == LyokoOrder.harass) { //si y'a un ordre simple d'harass, il est overiddé
                        if (LGuerrier.GetGuide().GetTerritoire().GetMondeV() == VarG.scLyoko) {
                            if (LGuerrier.GetGuide().GetTerritoire().GetMondeV() == VarG.carthage) {
                                if (LevelableOption.GetOp(GameOptionName.xanaCoreAttacks).currentValue > 0) {
                                    if (LGuerrier.GetGuide().orderProfile.GiveCoreDestructionOrder(LGuerrier.GetGuide().GetTerritoire(), LGuerrier.GetGuide())) //autoAdapt to carthage et aelita
                                        return;
                                }
                            } else {
                                if (LevelableOption.GetOp(GameOptionName.xanaSectorDestruction).currentValue > 0) {
                                    if (LGuerrier.GetGuide().orderProfile.GiveSectorDestructionOrder(LGuerrier.GetGuide().GetTerritoire(), LGuerrier.GetGuide().T_chute.IsRunning(), LGuerrier.GetGuide())) //autoAdapt to carthage et aelita
                                        return;
                                }
                            }
                        }
                    }
                    //si aucun de ses ordres n'est intéressant pour le LG on lui laisse faire un test de harass normal
                    foreach (LyokoGuerrier _lg in LyokoGuerrier.listeLG) {
                        if (!_lg.IsVirt())
                            continue;
                        if (_lg.CanBeHarrassedOnLyokoOrReplika() && _lg.GetGuide().GetTerritoire() == LGuerrier.GetGuide().GetTerritoire()) {
                            //si il est dans la salle du coeur, c'est que les attaques de coeur sont autorisées, il y restera donc jusqu'à avoir fini sa tâche
                            //si on lui donne l'ordre de suivre un Lguide et que ce dernier sort de la salle, on est pas avançé
                            //donc il ne doit harceler personne une fois dans la salle du coeur
                            if (LGuerrier.GetGuide().carthageProfile.pos != CarthagePos.coreRoom) {
                                DebugXana("harass task given for XANAWARRIOR " + LGuerrier.nom);
                                LGuerrier.GetGuide().GiveHarassOrder(_lg.nom);
                                return;
                            }
                        }
                    }
                    //do not do LGuerrier.IsAloneOnSector(true)
                    if (LGuerrier.controle != XanaControlStatus.Definitive &&
                        !LGuerrier.GetGuide().battleProfile.inBattle &&
                        LGuerrier.GetGuide().orderProfile.savedOrder == LyokoOrder.nothing && !LGuerrier.GetGuide().isMoving) {
                        LGuerrier.GetGuide().orderProfile.GiveThrowItselfInSeaOrder(LGuerrier.GetGuide());
                        DebugXana(LGuerrier.nom + " got throw himself into sea order");
                        return;
                    }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    DebugXana("NO NEW ACTIVITY FOUND FOR XANAWARRIOR " + LGuerrier.nom);
#endif
                }
            }
        }

        public void DebugXana(params string[] rest) {
            string messageFinal = "";
            foreach (string a in rest)
                messageFinal += a + " ";
            DebugLogList.LogXana(messageFinal);
        }

        public override void UpdateTimer() {
            if (TGestionXanatifies.IncrementReach())
                Gerer_Xanatifies();
            if (TEarthAttacks.IncrementReach()) {
                XanaTickDisplay.UpdateTickPanel(TickPanelType.earthAtk);
                if (PrgXanaCode.Instance.etat == OpenCloseStatus.Closed) {
                    DebugXana("TEarthAttacks prise de controle de Tour");
                    TryTowerAttack(false);
                }
            }
            if (TSkidGarageAttack.IncrementReach()) {
                XanaTickDisplay.UpdateTickPanel(TickPanelType.skidGarageAtk);
                TrySkidGarageAttack(false);
            } else {
                TrySEQUEL_SkidGarageAttack();
            }

            if (TCoreAttack.IncrementReach()) {
                TryCoreAttack(false);
            }

            if (TReactivity.IncrementReach()) {
                XanaTickDisplay.UpdateTickPanel(TickPanelType.reactivity);
                DebugXana("T-réactivité (harcèlement/défense de tour)");
                //harceler en toute fin permet d'utiliser les monstres restants.
                //note que même si xanaRandomMonster est désactivé, on garde ça pour les boss, dont les apparitions restent possibles
                REACT_LwProximity();
                REACT_LwActivityInTower();
                REACT_OnTowerStillActivated();
                //ce que l'on peut aussi voir c'est la vitesse de "rechargment" de Xana, notamment en sandbox.
                //'activation tour' vaut aussi pour une activation de Xana lui même, et donc la défense de la tour
                //pour l'instant les monstres ne peuvent toujours pas se détacher d'une tour qu'ils défendaient

                if (CanAttackAtAnyTime()) {
                    //initiative désactivée par l'activation d'une tour, on le reactive si besoin
                    if (!TEarthAttacks.IsRunning() && !HasTourActive() && LevelableOption.GetOp(GameOptionName.xanaEarthAttacks).currentValue > 0)
                        TEarthAttacks.Start();
                    if (!TCoreAttack.IsRunning() && LyokoGuideUtilities.GetTotalLG_AttaquantCoeur() <= 0 && LevelableOption.GetOp(GameOptionName.xanaCoreAttacks).currentValue > 0)
                        TCoreAttack.Start();
                    if (!TSkidGarageAttack.IsRunning() && LyokoGuideUtilities.GetTotalLG_AttackingSkid_InGarage() <= 0 && LevelableOption.GetOp(GameOptionName.xanaGarageAttacks).currentValue > 0)
                        TSkidGarageAttack.Start();

                    //on s'assure auparavant qu'il y a déjà eu au moins une attaque d'un type
                    if (HasAtkOnce()) {
                        if (LyokoGuideUtilities.GetTotalLG_AttaquantCoeur() <= 0 && LyokoGuideUtilities.GetTotalLG_AttackingSkid_InGarage() <= 0 && !HasTourActive()) {
                            ResetAttackTime(true);
                            DebugXana("NO MORE ATTACK, TIME BEFORE NEXT ATTACK RESETTED");
                        }
                    }
                }
            }
            if (TSendEnergy.IncrementReach()) {
                XanaTickDisplay.UpdateTickPanel(TickPanelType.sendEnergy);
                SendEnergy();
            }
        }

        public bool TryTowerAttack(bool forced = false) {
            if (hasTowerAtkOnce || !CanAttackMore())
                return false;
            if (forced || LevelableOption.GetOp(GameOptionName.xanaEarthAttacks).currentValue >= 1) {
                if (towerTarget != null) {
                    //towertarget est delete après tentativePriseDeControle donc on save
                    Tour towerTargetSave = towerTarget;
                    TentativePriseDeControleTour();
                    DebugXana("towerTargetSave " + towerTargetSave);
                    if (towerTargetSave.IsActivatedBy(Camp.XANA))
                        hasTowerAtkOnce = true;
                    return towerTargetSave.IsActivatedBy(Camp.XANA);
                } else {
                    if (UnityEngine.Random.value > .3f || forced) {
                        DebugXana("Xana initiative, tenterais bien d'activer une tour");
                        ActiverTourRandom(false, true, false, true);
                        if (this.HasTourActive())
                            hasTowerAtkOnce = true;
                        return this.HasTourActive();
                    }
                }
            }
            return false;
        }

        public void RazFromRestartRvlp(bool fromRVLP = false) {
            towerTarget = null;
            sendEnergyRegularlyTo = null;
            if (fromRVLP) {
                TCoreAttack.Reset();
                TSkidGarageAttack.Reset();
                TDrainSkidGarageAttack.Reset();
                TDrainCoreAttack.Reset();
                TReactivity.Reset();

                if (LevelableOption.GetOp(GameOptionName.xanaEarthAttacks).currentValue > 0)
                    TEarthAttacks.Reset();
            } else {
                TCoreAttack.Stop();
                TSkidGarageAttack.Stop();
                TDrainSkidGarageAttack.Stop();
                TDrainCoreAttack.Stop();
                TReactivity.Stop();

                TEarthAttacks.Stop();
            }
            hasTowerAtkOnce = false;
            hasCoreAtkOnce = false;
            hasGarageAtkOnce = false;
            hasDrainSkidAtkOnce = false;
            hasDrainCoreAtkOnce = false;
            used_clonePoly_ParDecryptage = false;
            used_clonePoly_ForRandomAttack = false;
            //veutFairePeterCentrale = false;
        }

        public bool TryAttackWhileWaiting() {
            //le temps d'attaque est géré différemment selon que l'on soit en attente ou non
            if (UnityEngine.Random.Range(1, 300) < LevelableOption.GetOp(GameOptionName.xanaEarthAttacks).currentValue * 10 && !hasCoreAtkOnce) {
                return TryTowerAttack(false);
            }
            if (UnityEngine.Random.Range(1, 300) < LevelableOption.GetOp(GameOptionName.xanaCoreAttacks).currentValue * 10 && !hasCoreAtkOnce) {
                return TryCoreAttack(false);
            }
            if (UnityEngine.Random.Range(1, 300) < LevelableOption.GetOp(GameOptionName.xanaGarageAttacks).currentValue * 10 && !hasGarageAtkOnce) {
                return TrySkidGarageAttack(false);
            }
            if (UnityEngine.Random.Range(1, 300) < LevelableOption.GetOp(GameOptionName.xanaScyphozoaDrainSkid).currentValue * 10 && !hasDrainSkidAtkOnce) {
                return TrySkidDrain(false);
            }
            //TODO 42X
            /*if (UnityEngine.Random.Range(1, 300) < LevelableOption.GetOp(GameOptionName.xanaScyphozoaDrainCore).currentValue * 10 && !hasDrainCoreAtkOnce) {
                return TryCoreDrain(true);
            }*/
            return false;
            //en théorie, xanaSectorDestruction est inutilisable par Xana car on ne peut pas 
            //attendre avec une Aelita xanatifiée indéfinitivement chez Xana, seul moyen pour
            //lui de détruire un territoire
        }

        public void Cancel_TentativeDePriseDeControle() {
            //au cas où xana ai commencer à envoyer de l'énergie, on l'enlève.
            if (towerTarget == null)
                return;
            sendEnergyRegularlyTo = null;
            TEarthAttacks.Stop();
            if (towerTarget.energyProfile.GetCampMineur() == 0)
                towerTarget.energyProfile.RendreEnergie(true, false);
            towerTarget = null;
        }

        public void TentativePriseDeControleTour() {
            DebugXana("tentativePRISEcontroleTOUR");
            //Xana peut avoir la volonté d'activer une tour/ou d'en prendre controle, mais ne pas avoir assez d'énergie le pousse à ré-essayer à chaque fois
            if (towerTarget == null)
                return;
            switch (towerTarget.activation) {
                case Camp.XANA:
                    Cancel_TentativeDePriseDeControle();
                    return;
                case Camp.HOPPER:
                    //on fait abandonner Xana, il tentera de tout façon de détruire la tour
                    Cancel_TentativeDePriseDeControle();
                    return;
                case Camp.JEREMIE:
                    if (GetAByOS<PrgJournalDecrypt>(OSTarget.SC).TDecryptage.IsRunning()) {
                        sendEnergyRegularlyTo = towerTarget;
                    } else {
                        Cancel_TentativeDePriseDeControle();
                    }
                    return;
                default:
                    //si Xana à déjà une tour ou que Jeremie à activer une tour, alors Xana n'activera pas de tour
                    //(si c'est sur le même monde), il préfèrera attaquer la tour existante (ou la convertir)
                    if (HasTourActive() || (Camps.jeremie.HasTourActive() && Tour.ReturnActiveTowers(Camp.JEREMIE)[0].GetTerritoire().GetMondeV() == towerTarget.GetTerritoire().GetMondeV() && !Tour.ReturnActiveTowers(Camp.JEREMIE).Contains(towerTarget))) {
                        Cancel_TentativeDePriseDeControle();
                    } else {
                        //DEMANDER ASKENERGIE ICI !!;
                        energyManager.SendTo(towerTarget.energyProfile, DepensesEnergetiques.tour.maintien);
                    }
                    return;
            }
        }

        public void UsualBossToHarassCheck() {
            foreach (LyokoGuerrier _lg in LyokoGuerrier.listeLG) {
                if (!_lg.IsVirt())
                    continue;
                if (!TReactivity.IsRunning() || !_lg.CanBeHarrassedOnLyokoOrReplika()) {
                    BossChancesPanelLw.UpdateAll(_lg.nom,_lg.GetGuide().GetTerritoire().GetTranslatedName(), 0, 0, 0, 0);
                } else {
                    CheckBossesChances(_lg, _lg.GetGuide()); //faire une moyenne du test pour tout les LG ?
                }
            }
        }

        private void REACT_LwProximity() {
            if (waitturns_before_newMonsters > 0)
                waitturns_before_newMonsters--;
            DebugXana("REACT_LwProximity");
            bool STOPLOOP = false;
            foreach (LyokoGuerrier _LGuerrier in LyokoGuerrier.listeLG) {
                if (!_LGuerrier.IsVirt())
                    continue;
                if (STOPLOOP) { // TODO 43X: improve so that only other LW from the same sector will be outed of loop if the first one on that sector is getting new monsters
                    DebugXana("STOPLOOP");
                    return;
                }
                LyokoGuide _LGuide = _LGuerrier.GetGuide();
                if (!_LGuerrier.CanBeHarrassedOnLyokoOrReplika())
                    continue;
                if (LyokoGuideUtilities.GetTotalMonsterCountIn(_LGuide.GetTerritoire(), Camp.XANA) >= 6)
                    continue;

                //on utilise le getDistance pour Hopper
                if (IsMonsterAvailableFor(_LGuide.GetTerritoire(), LyokoOrder.harass) && LyokoGuideUtilities.GetTotalLG_AttaquantDefendantTours(_LGuide.GetTerritoire(), Camp.XANA) <= 0) {
                    //DebugXana("on dit aux monstres dispos d'aller harceler (sachant qu'il n'y a pas d'attaque ou de défense de tour)");
                    DebugXana("monstres asked to harass LW");
                    foreach (LyokoGuide LGMonstre in LyokoGuide.liste) {
                        if (LGMonstre != null && LGMonstre.nestedIN == null && !LGMonstre.T_chute.IsRunning() && LGMonstre.camp == Camp.XANA && LGMonstre.GetTerritoire() == _LGuide.GetTerritoire()
                             && LGMonstre.orderProfile.savedOrder != LyokoOrder.destroySector && LGMonstre.orderProfile.savedOrder != LyokoOrder.defendTower && LGMonstre.orderProfile.savedOrder != LyokoOrder.attackTower && LGMonstre.orderProfile.harcelerWhichLG == "" && !LGMonstre.IsMeduseGardienOccuper()) {
                            DebugXana(LGMonstre.nom, "dispo pour aller harceler, sauf si exception carthage");
                            if (LGMonstre.GetTerritoire() == VarG.carthage) {
                                if (LGMonstre.IsLG_accessibleInCarthage(_LGuide)) {
                                    //la fonction ci dessus prend en compte le fait que l'on puisse ne pas être sur carthage
                                    LGMonstre.GiveHarassOrder(_LGuide.nom);
                                    continue;
                                }
                                if (_LGuide.carthageProfile.pos == CarthagePos.maze) {
                                    STOPLOOP = true; // NEW MONSTERS JUST CREATED SO WE DON'T TRY TO CREATE NEW FOR OTHER LW YET IN THIS LOOP
                                                     //on fait ça seulement pour quand les héros sont repérés dans les couloirs de carthage, 
                                                     //pour le reste, c'est les responsePresenceVoute etc...qui s'occupent de faire apparaitre des mponstres via defendreClef/defendreVoute...
                                    CreateMonsterBossToHarass(_LGuerrier, _LGuide, true);
                                    if (LevelableOption.GetOp(GameOptionName.xanaRandomMonster).ToBool())
                                        CreateMonsterBossToHarass(_LGuerrier, _LGuide, false);
                                    break;
                                }
                            } else {
                                LGMonstre.GiveHarassOrder(_LGuide.nom);
                            }
                        }
                    }
                } else {
                    //mais on offre la possibilité que un groupe en atk/defense se splitte
                    if (LyokoGuideUtilities.GetTotalLG_AttaquantDefendantTours(_LGuide.GetTerritoire(), Camp.XANA) > 0) {
                        DebugXana("il y a déjà des monstres attaquants défendant des tours -> donc on ne va créer des monstres pour harceler en prime. if LG close to tower, monster will attack or splitUp");
                        //voir fonction associée dans LyokoGuide
                        continue;
                    }
                    if (CrisisManager.started) {
                        DebugXana("confirmation, y'a pas de monstres dispo pour harceler", _LGuide.nom);
                        //note : si il y a des monstres en attaque ou en défense, on ne rajoute pas de Xana monstres pour harceler,

                        // si on a une expedition en cours, on autorise la création de nouveaux ennemis, sinon, on se calme direct,
                        // pas besoin d'aller déloger Aelita qui n'a rien demander >>
                        if (waitturns_before_newMonsters == 0) {
                            STOPLOOP = true; // NEW MONSTERS JUST CREATED SO WE DON'T TRY TO CREATE NEW FOR OTHER LW YET IN THIS LOOP
                                             //nobreakHere
                            CreateMonsterBossToHarass(_LGuerrier, _LGuide, true);
                            if (LevelableOption.GetOp(GameOptionName.xanaRandomMonster).ToBool())
                                CreateMonsterBossToHarass(_LGuerrier, _LGuide, false);
                            waitturns_before_newMonsters = 7; //4 trop peu apparemment - passé de 6 à 7 dans la 350
                        }
                    }
                }
            }
        }

        public List<int> CheckBossesChances(LyokoGuerrier _LGuerrier, LyokoGuide _LGuide) {
            chancesVariety.Clear();
            int chancesMeduse = 0;
            int chancesXanafiedLw = 0;
            int chancesGardien = 0;
            int chancesClonePolymorphe = 0;

            if (VarG.meduseFonction.CouldSpawn()) {
                chancesMeduse += Mathf.Clamp(LevelableOption.GetOp(GameOptionName.xanaScyphozoaXanafie).currentValue + LevelableOption.GetOp(GameOptionName.xanaScyphozoaSteal).currentValue, 0, 3);
                if (LevelableOption.GetOp(GameOptionName.xanaSectorDestruction).currentValue > 0)
                    chancesMeduse++;
                if (_LGuerrier.IsAloneOnSector(false))
                    chancesMeduse--;
                if (_LGuerrier.IsAloneOnSector(false))
                    chancesMeduse++;
                if (_LGuide.nom == "aelita")
                    chancesMeduse++;
                if (_LGuide.camp == Camp.HOPPER)
                    chancesXanafiedLw -= 3;
                if (_LGuide.GetTerritoire() == VarG.carthage)
                    chancesMeduse++;
                if (!VarG.meduseFonction.CanBeUsed())
                    chancesMeduse = 0;
                if (_LGuide.nom == "franz")
                    chancesMeduse -= 6;
            }
            LyokoGuerrier xanafiedLWToUse = LyokoGuerrierUtilities.GetRandomXanafiedLW_ToVirt();
            if (xanafiedLWToUse != null) {
                chancesXanafiedLw = VarG.xanaPocketliste.Count + 2;
                if (_LGuide.nom == "aelita")
                    chancesXanafiedLw++;
                if (_LGuide.inTour) // as XANAWARRIOR can kick LW out of tower, chances to spawn them is increased
                    chancesXanafiedLw++;
                if (_LGuide.camp == Camp.HOPPER)
                    chancesXanafiedLw += 2;
                //  chancesXanafiedLw += 999; //DEBUG
            }
            if (dbgForceGardien || (VarG.gardienFonction.CouldSpawn() && _LGuide.GetTerritoire() != VarG.carthage)) {
                chancesGardien += LevelableOption.GetOp(GameOptionName.xanaGuardianUse).currentValue;
                if (_LGuerrier.IsAloneOnSector(false))
                    chancesGardien--;
                if (_LGuerrier.IsAloneOnSector(false))
                    chancesGardien++;
                if (dbgForceGardien) {
                    chancesGardien += 100;
                }
            }
            if (Xana.CouldSpawnPolyClone()) {
                chancesClonePolymorphe += LevelableOption.GetOp(GameOptionName.xanaPolymorphUse).currentValue + 1;
                if (_LGuerrier.nom != "aelita")
                    chancesClonePolymorphe++;
            }
            chancesVariety.AddMany(chancesXanafiedLw, chancesMeduse, chancesGardien, chancesClonePolymorphe);

            //DebugXana("UPDATE Panels");
            if (_LGuerrier != null) {
                string secName = "";
                if (_LGuerrier.IsVirt())
                    secName = _LGuerrier.GetGuide().GetTerritoire().GetTranslatedName();
                BossChancesPanelLw.UpdateAll(_LGuerrier.nom, _LGuerrier.GetGuide().GetTerritoire().GetTranslatedName(), chancesXanafiedLw, chancesMeduse, chancesGardien, chancesClonePolymorphe);
            }
            return chancesVariety;
        }

        public static bool CouldSpawnPolyClone() {
            return !Camps.xana.used_clonePoly_ForRandomAttack && LevelableOption.GetOp(GameOptionName.xanaPolymorphUse).currentValue > 0;
        }

        public void CreateMonsterBossToHarass(LyokoGuerrier _LGuerrier, LyokoGuide _LGuide, bool areBosses = false) {
            if (_LGuerrier.controle != XanaControlStatus.None || _LGuide == null || _LGuide.midAirParalysis)
                return;
            DebugXana("noMonster/Boss available -> create boss to harass " + _LGuide.nom);
            string nmTocreate = "";
            if (areBosses) {
                bossChancesVariety = CheckBossesChances(_LGuerrier, _LGuide);
                int chancesXanafiedLw = bossChancesVariety[0];
                int chancesMeduse = bossChancesVariety[1];
                int chancesGardien = bossChancesVariety[2];
                int chancesClonePolymorphe = bossChancesVariety[3];

                int chosenChanceValue = 0;
                for (int a = 0; a < bossChancesVariety.Count; a++) {
                    int alea = Mathf.FloorToInt(UnityEngine.Random.Range(0, bossChancesVariety.Count));
                    //DebugXana("aleaBoss",alea);
                    if (alea < bossChancesVariety[a]) {
                        if (a == 0 && chancesXanafiedLw > 0) {
                            nmTocreate = LyokoGuerrierUtilities.GetRandomXanafiedLW_ToVirt().nom;
                            chosenChanceValue = chancesXanafiedLw;
                            break;
                        } else if (a == 1 && chancesMeduse > 0) {
                            nmTocreate = "meduse";
                            chosenChanceValue = chancesMeduse;
                            break;
                        } else if (a == 2 && chancesGardien > 0) {
                            nmTocreate = "gardien";
                            chosenChanceValue = chancesGardien;
                            break;
                        } else if (a == 3 && chancesClonePolymorphe > 0) {
                            nmTocreate = "clone_polymorphe";
                            chosenChanceValue = chancesClonePolymorphe;
                            break;
                        }
                    }
                }
                if (nmTocreate == "")
                    return;
                // on à choisi un boss, maintenant on regarde si ça vaut le coup ou si on fait juste un monstre
                int ran = UnityEngine.Random.Range(0, 3); //entre 0 et 2
                if (ran == 0 || chosenChanceValue > 3) { //1 chance out of 3 to try to create a boss. If chosenChance has more than 3, boss is mandatory
                    DebugXana("boss chosen: " + nmTocreate + " and confirmed");
                } else {
                    nmTocreate = "";
                    DebugXana("boss chosen: " + nmTocreate + " CANCELLED because chances are too low");
                    return;
                }
            }

            Territoire territoireCible = _LGuide.GetTerritoire();
            if (territoireCible.GetMondeV() == VarG.reseau) {
                DebugXana("xana voudrait créer des monstres mais c'est indispo dans le réseau pour l'instant");
                return;
            }
            // VIRT DESTINATION
            Tour tour = null;
            List<Tour> listeToursP = new List<Tour>();
            if (territoireCible != VarG.carthage) {
                foreach (Tour tourTest in territoireCible.listeTours) {
                    if (tourTest.IsActivatedBy(Camp.XANA) || tourTest.IsActivatedBy(Camp.NEUTRE))
                        listeToursP.Add(tourTest);
                }
                if (listeToursP.Count <= 0) {
                    Debug.LogError("Aucune tour disponible dans le territoire - erreur!");
                } else {
                    int a = UnityEngine.Random.Range(0, listeToursP.Count);// - 1 inutile pour le count
                    tour = listeToursP[a];
                }
            } else {
                //dans carthage, on ne selectionne pas de tour pour harceler
                //pour l'attaque de la tour, c'est le response to tower activated by jeremie qui s'en occupe
                //pour l'instant, ce type de harcelement dans carthage n'est pas en mesure de générer un BOSS
                switch (_LGuide.carthageProfile.pos) {
                    /* case CarthagePos.inTowerRoom:
                            tour = VarG.carthage.listeTours[0];
                            break;*/
                    case CarthagePos.domeVoid:
                        CreateMonsters(null, LyokoOrder.attackDomeVoid, 0, _LGuerrier.nom, CarthageAppearPointElement.Get_MonsterAppearPoint(_LGuide.carthageProfile.HasElementProximity(VarG.carthageParam.southPole)));
                        return;
                    //ne rien faire pour garageSkid, ce ne serait pas utile
                    case CarthagePos.domeBridge:
                        CreateMonsters(null, LyokoOrder.attackDomeBridge, 0, _LGuerrier.nom, VarG.carthageParam.domeBridge);
                        return;
                        //case CarthagePos.inCoreRoom:
                        //    CreateMonsters(null, LyokoOrder.attackCore, 0, LG.nom, VarG.carthageParam.core);
                        //    return;
                }
            }
            if (tour == null) {
                Debug.LogWarning("Aucune tour trouvée pour y créer des monstres!");
                return;
            }
            if (areBosses) {
                DebugXana("creating boss " + nmTocreate + " to harass");
                LyokoGuide LG1 = CreateMonstersSuite(CRDManager.C(territoireCible, tour.GetDisplayableID_perSector(), false), nmTocreate, 0, LyokoOrder.harass, _LGuerrier.nom, null);
                if (LG1 != null && nmTocreate == "clone_polymorphe")
                    used_clonePoly_ForRandomAttack = true;
            } else {
                //MONSTERS
                //dans le cas de carthage, les monstres sont créés avec les autres checks, sauf pour les persos dans les couloirs
                if (territoireCible != VarG.carthage) {
                    DebugXana("creation monstre pour harceler, après ça normalement, y'a pas d'autres monstres qui devraient être créés pour harceler, sauf si territoires différents");
                    CreateMonsters(tour, LyokoOrder.harass, 0, _LGuerrier.nom);
                } else {
                    //on ne créer pas les monstres du MAZE ici mais par une colision avec une specialeTile
                    /*if (_LGuide.carthageProfile.pos == CarthagePos.inMaze) {
                        //s'assurer ici qu'il n'y a pas déjà trop de monstres dans le couloir
                        DebugXana("creation monstre pour héros dans couloir de carthage");
                        CreateMonsters(tour, LyokoOrder.harass, 0, LG.nom);
                    }*/
                }
            }
        }

        public bool ActiverTourRandom(bool excludeCarthage = true, bool delayXanaCode = false, bool withErrorMessage = false, bool excludeReplikas = true) {
            if (!HasTourActive()) {
                listeToursActivables.Clear();
                foreach (Tour tourTest in Tour.listeDispo) {
                    if (excludeCarthage && tourTest.GetTerritoire() == VarG.carthage)
                        continue;
                    if (excludeReplikas && (tourTest.GetTerritoire().tType == TerritoireType.replika || tourTest.GetTerritoire().tType == TerritoireType.replika_carthage))
                        continue;
                    ///si Jérémie à une tour activée sur le supercalculateur en question, Xana ne va pas tenter d'y activer tour
                    if (Camps.jeremie.HasTourActive() && tourTest.GetTerritoire().GetMondeV() == Tour.ReturnActiveTowers(Camp.JEREMIE)[0].GetTerritoire().GetMondeV())
                        continue;
                    if (tourTest.passage)//on enlève celles de passage
                        continue;
                    //on enlève celles dans lesquelles se trouve Aelita
                    if (VarG.aelita.IsVirt() && VarG.aelita.GetGuide().inTour && VarG.aelita.GetGuide().id_UNIV_tourEntrable == tourTest.ID_universel)
                        continue;
                    if (tourTest.IsKo())
                        continue;
                    //puis on enlève aussi les tours proches des LG (uniquement non xana (nouveau 40X))
                    if (LyokoGuideUtilities.IsThereCloseFromTower(tourTest, true))
                        continue;
                    AddTourActivableToList(tourTest);
                }
                //DebugXana("listeToursActivables : "+listeToursActivables);
                if (listeToursActivables.Count > 0) {
                    ActiverTour(listeToursActivables[Mathf.FloorToInt(UnityEngine.Random.Range(0, listeToursActivables.Count))], delayXanaCode);
                    TEarthAttacks.Stop(); //on désactive jusqu'à la prochaine réinit de Xana ou la prochaine desactivation de tour
                    return true;
                }
            }
            /* } else {
                 if (withErrorMessage)
                     Aff("tourutilisee", MSGcolor.grey);
                 //DebugXana("xana ne peut activer une tour car elle en a déjà une d'active");
             }*/
            return false;
        }
        public void AddTourActivableToList(Tour _tower) {
            listeToursActivables.AddMany(_tower.ID_universel);
        }

        public void CreateMonsters_ForMaze(bool toDefendKey, Vector3 tileLocalPos) {
            int totalLG_inMaze = 0;
            foreach (LyokoGuerrier _lg in LyokoGuerrier.listeLG) {
                if (_lg.IsVirt() && _lg.controle == XanaControlStatus.None && _lg.GetGuide().GetTerritoire() == VarG.carthage &&
                    !_lg.GetGuide().VerifDevirt() && !_lg.GetGuide().midAirParalysis && !_lg.GetGuide().T_chute.IsRunning() && !_lg.IsUnconscious() && _lg.GetGuide().carthageProfile.pos == CarthagePos.maze) {
                    totalLG_inMaze++;
                }
            }
            LyokoOrder _order = LyokoOrder.attackMaze;
            if (toDefendKey)
                _order = LyokoOrder.attackKey;

            // TODO ajouter la possibilité de faire apparaitre un xanatifié ici?

            DebugXana("tileLocalPos " + tileLocalPos);
            if ((LevelableOption.GetOp(GameOptionName.xanaScyphozoaSteal).currentValue <= 0 && LevelableOption.GetOp(GameOptionName.xanaScyphozoaXanafie).currentValue <= 0) ||
                !VarG.meduseFonction.CanBeUsed() ||
                VarG.meduseFonction.usedOncePerMaze ||
                totalLG_inMaze > 1) {
                if (toDefendKey) {
                    CreateMonstersSuite(CRDManager.C(VarG.carthage, tileLocalPos, false), "rampant", DifficultyManager.GetLevel() + Mathf.Clamp(totalLG_inMaze, 0, 2), _order);
                } else {
                    CreateMonstersSuite(CRDManager.C(VarG.carthage, tileLocalPos, false), "rampant", DifficultyManager.GetLevel(), _order);
                }
            } else {
                CreateMonstersSuite(CRDManager.C(VarG.carthage, tileLocalPos, false), "meduse", 0, _order);
                VarG.meduseFonction.usedOncePerMaze = true;
            }
        }

        public void REACT_LwActivityInTower() {
            // DebugXana("ResponseLwActivityInTower");
            foreach (LyokoGuerrier _lg in LyokoGuerrier.listeLG) {
                //si : il y a un échange
                //ou si il y a une réparation
                //ou si il y a un simplement un LG dedans et un decryptage en cours
                //tout cela exclu l'usage d'une tour pour un clone sur terre par exemple, que Xana ne va donc pas attaquer - note! il ne faut pas que le LG soit entrain de changer de territoire
                if (_lg.IsGameAvailable() && _lg.IsVirt() && _lg.GetGuide().status == VirtualBuildStatus.created && !_lg.GetGuide().IsInTransition() &&
                    _lg.GetGuide().inTour && (_lg.dnaProtocols.isRepairing || _lg.dnaProtocols.GetState() == DnaModes.exchange || Tour.GetByUnivID(_lg.GetGuide().id_UNIV_tourEntrable).IsActivatedBy(Camp.JEREMIE))) {
                    ///en cas de decryptage de journal
                    if (GetAByOS<PrgJournalDecrypt>(OSTarget.SC).TDecryptage.IsRunning() && GetAByOS<PrgJournalDecrypt>(OSTarget.SC).graph.decryptageTaux > 6) {
                        if (waitturns_before_newMonsters_attaqueTour == 0) {
                            ActiverTour(GetAByOS<PrgJournalDecrypt>(OSTarget.SC).tourDecryptage.ID_universel);
                            DoDefendTower(GetAByOS<PrgJournalDecrypt>(OSTarget.SC).tourDecryptage, SpecialOrder.POLYMORPH_DEFEND_TOWER);
                            waitturns_before_newMonsters_attaqueTour = 3;
                        } else {
                            DebugXana(">>>>>>>>>>>>>>attendre encore un peu avant d'envoyer des monstres sur la tour");
                            waitturns_before_newMonsters_attaqueTour--;
                        }
                    } else {
                        ///dans tout les autres cas (LG présents dans tour activée, en reparation ADN, etc...
                        if (waitturns_before_newMonsters_attaqueTour == 0) {
                            DebugXana(">>>>>>>>>>>>>>DoTowerAttack");
                            CreateTowerRandomAttack(_lg.GetGuide().id_UNIV_tourEntrable, 0);
                            waitturns_before_newMonsters_attaqueTour = 3;
                        } else {
                            DebugXana(">>>>>>>>>>>>>>attendre encore un peu avant d'envoyer des monstres sur la tour");
                            waitturns_before_newMonsters_attaqueTour--;
                        }
                    }
                    break;
                }
            }
        }

        public void REACT_OnTowerStillActivated() {
            if (Tour.IsThereActive(Camp.HOPPER)) {
                CreateTowerRandomAttack(Tour.ReturnActiveTowersInt(Camp.HOPPER)[0]);
            }
        }

        public void REACT_OnTowerActived(Tour t) {
            //si une tour est activée par Hopper, il va tenter de la détruire
            if (Tour.IsThereActive(Camp.JEREMIE)) {
            }
            if (Tour.IsThereActive(Camp.HOPPER)) {
                CreateTowerRandomAttack(Tour.ReturnActiveTowersInt(Camp.HOPPER)[0], 0);
            }
            if (Tour.IsThereActive(Camp.XANA)) {
                DoDefendTower(Tour.ReturnActiveTowers(Camp.XANA)[0]);
                if (VarG.scLyoko.seuilDegats < 100) {
                    if (towerAttackTypeToSet != TowerAttackType.NONE) { //sauf si déjà setter lors de l'appel de l'activation de la tour
                        DebugXana("Reinforce Atk: " + towerAttackTypeToSet);
                    } else {
                        DebugXana("Create Atk: " + towerAttackTypeToSet);
                        /////////////////////////////////////////////////
                        int alea;
                        List<LevelableOption> listSP = new List<LevelableOption>();
                        foreach (GameOptionName lp in SavedGameOptions.attackPrefList) {
                            alea = UnityEngine.Random.Range(0, 11);//entre 0 & 10
                            if (alea < LevelableOption.GetOp(lp).currentValue * 2) {
                                listSP.Add(LevelableOption.GetOp(lp));
                                //DebugXana("add: " + lp.nom);
                            }
                        }
                        //Log("SavedGameOptions.attackPrefList: " + SavedGameOptions.attackPrefList.Count);
                        if (listSP.Count <= 0) {
                            //aucune attaque n'a été prise, on les prend donc toutes direct sans faire de distinction
                            foreach (GameOptionName lp in SavedGameOptions.attackPrefList) {
                                if (LevelableOption.GetOp(lp).currentValue > 0) {
                                    listSP.Add(LevelableOption.GetOp(lp));
                                    // DebugXana("add_basic: " + lp.ToString());
                                }
                            }
                        }
                        if (listSP.Count == 0) {
#if UNITY_EDITOR || DEBUG_MODE || EARLY_MODE
                            MSG.AffDebugInfo("no earth attack is enabled in option, so we manually activate xanaHumanXanatif, this should trigger only because of debug command");
                            LevelableOption.GetOp("xanaHumanXanatif").currentValue = 2;
                            listSP.Add(LevelableOption.GetOp("xanaHumanXanatif"));
#else
                            //note: c'est du au fait qu'on commence en mode éditeur avec les saved game options editeur,
                            //et que au restart ça charge les saved game options de mydocuments, hors,
                            //si ces dernières n'ont pas d'attaque-terre, ça bugguera
                            MSG.AffCriticalInfo("Xana attacked launched whereas no attack is available in the saved options");
                            return;
#endif
                        }
                        //DebugXana(">>>>>>>listSP.Count " + listSP.Count);
                        //DebugXana(">>>>>>> " + listSP[UnityEngine.Random.Range(0, listSP.Count - 1)].nom);
                        switch (listSP[UnityEngine.Random.Range(0, listSP.Count)].nom) {// - 1 inutile count
                            case "xanaHumanXanatif":
                                towerAttackTypeToSet = TowerAttackType.HumanXanatif;
                                break;
                            case "xanaNonHumanXanatif":
                                towerAttackTypeToSet = TowerAttackType.NonHumanXanatif;
                                break;
                            case "xanaObjectXanatif":
                                towerAttackTypeToSet = TowerAttackType.ObjectXanatif;
                                break;
                            case "xanaRailwayAccident":
                                towerAttackTypeToSet = TowerAttackType.RailwayAccident;
                                break;
                            case "xanaMeteor":
                                towerAttackTypeToSet = TowerAttackType.Meteor;
                                break;
                            case "xanaNuclearOverload":
                                towerAttackTypeToSet = TowerAttackType.NuclearOverload;
                                break;
                            case "xanaWeatherDisaster":
                                towerAttackTypeToSet = TowerAttackType.WeatherDisaster;
                                break;
                            case "xanaElectricSphere":
                                towerAttackTypeToSet = TowerAttackType.ElectricSphere;
                                break;
                            case "xanaSuicideBus":
                                towerAttackTypeToSet = TowerAttackType.SuicideBus;
                                break;
                        }
                        if (towerAttackTypeToSet == TowerAttackType.SuicideBus || towerAttackTypeToSet == TowerAttackType.ElectricSphere) {
                            DebugXana("fallBack sur humanXanatif");
                            //TODO 43X remove pour freeroam
                            towerAttackTypeToSet = TowerAttackType.HumanXanatif;
                        }
                        if (towerAttackTypeToSet == TowerAttackType.NONE) {
                            MSG.AffDebugInfo("Editor Mode >> no any attack selected >> fallback on xanaHumanXanatif");
                            //là en généralement, si il n'y a rien (malgré les protections du menu qui t'empêchent d'avoir 0 type d'attaque de choisies, c'est parce que on est en mode éditeur
                            //donc on choisi arbitrairement la xanaHuman
                            towerAttackTypeToSet = TowerAttackType.HumanXanatif;
                        }
                    }
                    Tour.ReturnActiveTowers(Camp.XANA)[0].CreateReinforceAtk(Camps.xana, towerAttackTypeToSet);
                }
            }
        }

        public bool HasAttackOfType(TowerAttackType t) {
            return Tour.ReturnActiveTowers(Camp.XANA).Count > 0 && Tour.ReturnActiveTowers(Camp.XANA)[0].linkedAtk != null && Tour.ReturnActiveTowers(Camp.XANA)[0].linkedAtk.typeAttaque == t;
        }

        private void CreateTowerRandomAttack(int numTour, SpecialOrder _specialOrder = SpecialOrder.NONE) {
            if (numTour == -1) {
                Debug.LogError("trying tower attack on tower of ID -1, targetted LG doesn't have the right univID proximity");
                return;
            }
            //si il y a déjà des monstres sur le territoire ET qu'ils ne sont pas occupés, on les envoie attaquer la tour.
            //sinon, on créer de nouveaux monstres
            //Dès lors qu'il y a moins de 1 monstre, on en renvoie

            Territoire secteurR = Tour.GetByUnivID(numTour).GetTerritoire();
            if (!IsMonsterAvailableFor(secteurR, LyokoOrder.attackTower)) {
                DebugXana("pas de monstres dispo pour attaquer tour " + numTour + " sur le territoire " + secteurR.GetTranslatedName());

                /*int randomTour = (Mathf.FloorToInt(UnityEngine.Random.Range(0,10)));/:entre 0 & 9
                int randomTour2 = (Mathf.FloorToInt(UnityEngine.Random.Range(0,10)));
                Tour tourChoice = secteurR.listeTours[randomTour];
                Tour tourChoice2 = secteurR.listeTours[randomTour2];*/
                CreateMonsters(Tour.GetByUnivID(numTour), LyokoOrder.attackTower, _specialOrder, "");
            }
            foreach (LyokoGuide LGMonstre in LyokoGuide.liste) {
                if (LGMonstre.camp == Camp.XANA && LGMonstre.GetTerritoire() == secteurR) {
                    //DebugXana(LGMonstre.TourActiveProche()," ",LGMonstre.timerDoTowerAttack.running," ",LGMonstre.DoDefendTower," ",LGMonstre.harcelerLG);
                    if (LGMonstre.TourActiveProche() || LGMonstre.orderProfile.savedOrder == LyokoOrder.defendTower || LGMonstre.orderProfile.savedOrder == LyokoOrder.attackCore || LGMonstre.orderProfile.harcelerWhichLG != "") {
                        DebugXana(LGMonstre.nom + " ne va pas attaquer pas car occupé");
                    } else {
                        Suite0(numTour, LGMonstre);
                    }
                }
            }
        }

        private void Suite0(int numTour, LyokoGuide LGMonstre) {
            LGMonstre.orderProfile.GiveTowerAttackOrder(LGMonstre, numTour);
        }

        public void DoDefendTower(Tour tour = null, SpecialOrder _specialOrder = SpecialOrder.NONE) {
            DebugXana("lance la défense de la tour"); //1= par rapport à un decryptage
            int monstres_defendant_DEJA_tour = LyokoGuideUtilities.GetTotalLG_DefendantTour(tour.ID_universel, LgTypes.Monstre, Camp.XANA);
            if (monstres_defendant_DEJA_tour > 1) {
                DebugXana("inutile, xana à déjà assez de défenseurs de la tour");
                return;
            }
            if (!IsMonsterAvailableFor(tour.GetTerritoire(), LyokoOrder.defendTower)) {
                //on ralenti la création des monstres pour défendre la tour.
                DebugXana("!!!!!!!!!!!!!!défendreTour - aucun monstre dispo pour défendre de la tour, on en créer de nouveaux si on le peut");
                if (tour.turnsBefore_Redefending <= 0) {
                    CreateMonsters(tour, LyokoOrder.defendTower, _specialOrder, "");
                    if (_specialOrder != SpecialOrder.POLYMORPH_DEFEND_TOWER) { //les tours sont reservées 
                        LyokoGuerrier xanaWarrior = LyokoGuerrierUtilities.GetRandomXanafiedLW_ToVirt();
                        if (xanaWarrior != null) {
                            DebugXana("Ajout End 368. Un xanaguerrier est dispo pour défendre la tour, xana l'envoie immédiatement après avoir créé sa vague de monstres");
                            //autrement l'envoi se faisait trop tard, après avoir battu la 1ere vague de monstres
                            CreateMonstersSuite(CRDManager.C(tour.GetTerritoire(), tour.GetDisplayableID_perSector(), false), xanaWarrior.nom, 0, LyokoOrder.defendTower, "", tour);
                        }
                    }
                    tour.turnsBefore_Redefending = 2;
                } else {
                    tour.turnsBefore_Redefending--;
                }
            } else {
                DebugXana("!!!!!!!!!!!!!!défendreTour - il y a des monstres dispo pour défendre de la tour, on les utilise si il y a 1 seul défenseur de la tour ou moins");
                foreach (LyokoGuide LGMonstre in LyokoGuide.liste) {
                    if (monstres_defendant_DEJA_tour <= 1) {
                        if (LGMonstre.orderProfile.savedOrder != LyokoOrder.defendTower && LGMonstre.orderProfile.savedOrder != LyokoOrder.attackTower &&
                            LGMonstre.orderProfile.savedOrder != LyokoOrder.attackSkidInGarage && LGMonstre.orderProfile.savedOrder != LyokoOrder.attackCore && LGMonstre.camp == Camp.XANA && LGMonstre.GetTerritoire() == tour.GetTerritoire()) {
                            LGMonstre.orderProfile.GiveTowerDefenseOrder(LGMonstre, tour.ID_universel);
                            monstres_defendant_DEJA_tour++;
                        }
                    } else {
                        break;
                    }
                }
            }
        }

        //nouvelle version de IsMonsterAvailableFor
        public List<LyokoGuide> GetMonsterAvailableFor(Territoire territoire, LyokoOrder _order = LyokoOrder.nothing) {
            List<LyokoGuide> availableMonsters = new List<LyokoGuide>();
            foreach (LyokoGuide _m in LyokoGuide.liste) {
                //priorité aux attaque et défense, à égalité, puis vient le harcèlement
                if (_m != null && _m.nestedIN == null && !_m.T_chute.IsRunning() && _m.camp == Camp.XANA && _m.GetTerritoire() == territoire) {
                    switch (_order) {
                        case LyokoOrder.attackSkidInGarage:
                            if (_m.orderProfile.savedOrder == LyokoOrder.nothing && (_m.carthageProfile.pos == CarthagePos.garageSkid || _m.carthageProfile.pos == CarthagePos.garageElevatorRoom)) {
                                availableMonsters.Add(_m);
                            }
                            break;
                    }
                }
            }
            return availableMonsters;
        }
        public bool IsMonsterAvailableFor(Territoire territoire, LyokoOrder _order = LyokoOrder.nothing) {
            bool pasBesoin_NouveauMonstre = false;
            foreach (LyokoGuide LGMonstre in LyokoGuide.liste) {
                //priorité aux attaque et défense, à égalité, puis vient le harcèlement
                if (LGMonstre != null && LGMonstre.nestedIN == null && !LGMonstre.T_chute.IsRunning() && LGMonstre.camp == Camp.XANA && LGMonstre.GetTerritoire() == territoire) {
                    switch (_order) {
                        case LyokoOrder.harass:
                            if (LGMonstre.nom != "meduse" && LGMonstre.nom != "gardien") {
                                //note : si il y a des monstres en attaque ou en défense, on ne rajoute pas de Xana monstres pour harceler
                                //qu'ils n'aient rien à faire, qu'ils attaquent/défendent une tour ou un emplacement, rien n'empêche les monstres d'aller harceler
                                /*if (!LGMonstre.attaquerCoeur) {
                                        pasBesoin_NouveauMonstre = true;
                                        break;
                                    }*/
                                if (LGMonstre.orderProfile.harcelerWhichLG != "") {
                                    pasBesoin_NouveauMonstre = true;
                                    break;
                                }
                            }
                            break;
                        case LyokoOrder.attackTower:
                            if (LGMonstre.orderProfile.savedOrder == LyokoOrder.attackTower) {
                                pasBesoin_NouveauMonstre = true;
                                break;
                            }
                            break;
                        case LyokoOrder.defendTower:
                            if (LGMonstre.orderProfile.savedOrder == LyokoOrder.defendTower) {
                                DebugXana("MonstrePour défendre la tour existe, il s'agit d'un", LGMonstre.nom);
                                pasBesoin_NouveauMonstre = true;
                                break;
                            }
                            break;
                        case LyokoOrder.attackCore:
                            if (LGMonstre.orderProfile.savedOrder == LyokoOrder.attackCore) {
                                pasBesoin_NouveauMonstre = true;
                                break;
                            }
                            break;
                        //------------------cas particuliers qui suivents :
                        //ils sont appellés pour défendre, mais iront directement harceler,
                        //avec les modifs, peut être ceux du dessus également
                        //le but est de savoir si ils sont toujours dans les environs pour défendre

                        case LyokoOrder.attackDomeBridge:
                            if (LGMonstre.carthageProfile.pos == CarthagePos.domeBridge) {
                                pasBesoin_NouveauMonstre = true;
                                break;
                            }
                            break;
                        case LyokoOrder.attackDomeVoid:
                            if (LGMonstre.carthageProfile.pos == CarthagePos.domeVoid && LGMonstre.nom == "manta") {
                                pasBesoin_NouveauMonstre = true;
                                break;
                            }
                            break;
                    }
                }
            }
            return pasBesoin_NouveauMonstre;
        }


        public void CreateMonsters(Tour tour, LyokoOrder ordre = LyokoOrder.nothing, SpecialOrder ordreSpecial = SpecialOrder.NONE, string LGharcelement = "", LyokoElement specialCarthageElement = null) {

            ///dans le cas du harcelement, les coordonnées de la tour ont déjà été modifiées et sont déjà considérées comme des coordonnées (mais elles devraient plutôt être traitées ici )

            //pour l'instant il manque peut être de déplacer la création de boss ci dessus DANS le système "CreateMonsters"

            //le concept, c'est que la difficulté ne doit pas influer sur le nombre max de monstres mais sur la difficulté pour les BATTRE (leur résistance)
            //tout les types de monstres doivent être accessibles quelque soit la difficulté, et ils doivent être en grand nombre pour le plaisir des casual et des hardcores gamers
            Territoire sector;
            CRDManager crd;
            if (specialCarthageElement != null) {
                sector = VarG.carthage; //variation pour replika carthage à rajouter
                crd = CRDManager.C(sector, specialCarthageElement.transform.localPosition, false); //place we look for, a mettre en transform dans les paramètres
                                                                                                   //on rechange ça encore une fois après coup si c'est une manta (qui nait sur les bords)
                                                                                                   //idem pour les attaques dans le garage skid
                                                                                                   //idem pour la meduse qui apparait pour Drainer
                                                                                                   ///////////////// A REMPLACER /////////////////////////
            } else {
                sector = tour.GetTerritoire();
                crd = CRDManager.C(sector, tour.GetDisplayableID_perSector_INT(), false);
            }
            if (ordre == LyokoOrder.attackTower) {
                //on change les coordonnées pour une autre tour au pif sur le territoire:
                List<Tour> listeToursP = new List<Tour>();
                if (sector != VarG.carthage && sector != VarG.replika_carthage) {
                    foreach (Tour tourTest in sector.listeTours) {
                        if (tourTest.IsActivatedBy(Camp.XANA) || tourTest.IsActivatedBy(Camp.NEUTRE)) {
                            listeToursP.Add(tourTest);
                        }
                    }
                    if (listeToursP.Count <= 0) {
                        Debug.LogError("Aucune tour disponible dans le territoire - erreur!");
                    } else {
                        int a = UnityEngine.Random.Range(0, listeToursP.Count);// - 1 inutile pour le count
                        crd = CRDManager.C(sector, listeToursP[a].GetDisplayableID_perSector(), false);
                        DebugXana("tour random définie pour la virtualisation du monstre : " + listeToursP[a].ID_universel);
                    }
                } else {
                    crd = CRDManager.C(sector, 1, false);
                }
            }
            LyokoGuide LG;
            int nbCopies = 0;
            string refMonstre = "kankrelat";

            List<string> randomMonsterTypes = new List<string>();
            switch (ordreSpecial) {
                case SpecialOrder.NONE:
                    // KOLOSSE : son appel devra être identique à la Méduse ou au Gardien, des conditions très spécifiques non traitées dans cette partie du code donc
                    // ces nombres doivent être les mêmes que pour la division possible en groupe (voir le code bien au dessus avec grouping)
                    if (ordre == LyokoOrder.attackTower) {
                        if (crd.GetTerritoire().tType == TerritoireType.carthage || crd.GetTerritoire().tType == TerritoireType.replika_carthage) {
                            if (LyokoGuideUtilities.GetTotalMonsterType_OnSector(crd.GetTerritoire(), "rampant") < 3) {
                                refMonstre = "rampant";
                                if (LyokoGuideUtilities.GetTotalMonsterType_OnSector(crd.GetTerritoire(), "rampant") == 0) {
                                    nbCopies = 2;
                                }
                            } else {
                                return; //cancel, already enough megatanks
                            }
                        } else {
                            if (LyokoGuideUtilities.GetTotalMonsterType_OnSector(crd.GetTerritoire(), "megatank") < 2) {
                                refMonstre = "megatank";
                                if (LyokoGuideUtilities.GetTotalMonsterType_OnSector(crd.GetTerritoire(), "megatank") == 0) {
                                    nbCopies = 1;
                                }
                            } else {
                                return; //cancel, already enough megatanks
                            }
                        }
                    } else {
                        if (sector.tType == TerritoireType.surface) {
                            randomMonsterTypes.AddMany("kankrelat", "block", "krabe", "frolion");
                            if (LyokoGuideUtilities.GetTotalMonsterType_OnSector(crd.GetTerritoire(), "megatank") < 2) {
                                randomMonsterTypes.AddMany("megatank");
                            }
                            if (canSpawnMantasOnSurface) {
                                if (LyokoGuideUtilities.GetTotalMonsterType_OnSector(crd.GetTerritoire(), "manta") < 3) {
                                    randomMonsterTypes.AddMany("manta");
                                }
                            }
                            if (canSpawnTarentulas) {
                                if (LyokoGuideUtilities.GetTotalMonsterType_OnSector(crd.GetTerritoire(), "tarentule") < 3) {
                                    randomMonsterTypes.AddMany("tarentule");
                                }
                            }
                            refMonstre = RandomMonster(randomMonsterTypes);
                        }
                        if (sector.tType == TerritoireType.carthage) {
                            randomMonsterTypes.AddMany("rampant", "manta");
                            if (ordre == LyokoOrder.attackDomeVoid) {
                                //si c'est au dessus du void, on force la manta et on change la position après coup
                                refMonstre = "manta";
                            } else {
                                refMonstre = RandomMonster(randomMonsterTypes); //rampant ou manta. si c'est la manta, sa position sera changée automatiquement après coup
                            }
                        }
                        if (refMonstre == "megatank" || refMonstre == "tarentule") {
                            nbCopies = 0;
                        }
                        if (refMonstre == "krabe" || refMonstre == "block" || refMonstre == "manta") {
                            nbCopies = 1;
                        }
                        if (refMonstre == "kankrelat" || refMonstre == "frolion" || refMonstre == "rampant") {
                            nbCopies = 2;
                        }
                        if (ordre == LyokoOrder.defendTower) {
                            nbCopies += 1;
                        }
                        if (ordre == LyokoOrder.attackSkidInGarage && nbCopies == 2) {
                            nbCopies = UnityEngine.Random.Range(0, 2); //pas de groupes de plus de 2, donc 0 ou 1 copies avec ce random
                        }
                    }
                    DebugXana("creerMonstre " + refMonstre + " at " + sector.GetTranslatedName() + ", nbCopies " + nbCopies);

                    //si c'est dans le maze, on est directement passé par CreateMonstersSuite avant ça
                    //si c'est une manta, faire apparaitre via FX
                    if (specialCarthageElement != null) {
                        CreateMonstersSuite(crd, refMonstre, nbCopies, ordre, LGharcelement);
                    } else {
                        CreateMonstersSuite(crd, refMonstre, nbCopies, ordre, LGharcelement, tour);
                    }
                    break;
                case SpecialOrder.POLYMORPH_DEFEND_TOWER:
                    if (ordre != LyokoOrder.defendTower) {
                        Debug.LogWarning("erreur : envoyer des monstres pour attaquer la tour, et qui ne corresponde pas à des megatanks de toute façon...");
                    }
                    // "défense" d'une tour avec des clones polymorphes et tarentules, 
                    // en effet, xana essaye de prendre le contrôle de la tour, pas de la détruire
                    if (!Camps.xana.used_clonePoly_ParDecryptage && LevelableOption.GetOp(GameOptionName.xanaPolymorphUse).currentValue > 0) {
                        LG = CreateMonstersSuite(crd, "clone_polymorphe", 0, ordre, LGharcelement, tour);
                        if (LG != null) {
                            Camps.xana.used_clonePoly_ParDecryptage = true;
                            if (DifficultyManager.GetLevel() == 0) {
                                LG.DoPolymorph("yumi");
                            } else if (DifficultyManager.GetLevel() == 1) {
                                LG.DoPolymorph("odd");
                            } else if (DifficultyManager.GetLevel() == 2) {
                                LG.DoPolymorph("ulrich");
                            }
                        }
                    } else {
                        if (sector.tType == TerritoireType.carthage) {
                            randomMonsterTypes.AddMany("rampant", "manta");
                            refMonstre = RandomMonster(randomMonsterTypes);
                            if (refMonstre == "rampant") {
                                CreateMonstersSuite(crd, refMonstre, 2, ordre, LGharcelement, tour);
                            } else {
                                CreateMonstersSuite(crd, refMonstre, 1, ordre, LGharcelement, tour);
                            }
                        } else {
                            randomMonsterTypes.AddMany("kankrelat", "block");
                            if (canSpawnTarentulas) {
                                randomMonsterTypes.AddMany("tarentule");
                            }
                            refMonstre = RandomMonster(randomMonsterTypes);
                            if (refMonstre == "kankrelat") {
                                CreateMonstersSuite(crd, refMonstre, 2, ordre, LGharcelement, tour);
                            } else {
                                CreateMonstersSuite(crd, refMonstre, 1, ordre, LGharcelement, tour);
                            }
                        }
                    }
                    break;
                case SpecialOrder.SCYPHOZOA_ON_SKID_IN_GARAGE: //scyphozoa on skidbladnir in garage Skid
                    CreateMonstersSuite(crd, "meduse", 0, ordre, LGharcelement, tour);
                    break;
            }
        }

        public LyokoGuide CreateMonstersSuite(CRDManager crd = null, string nomMonstre = "", int nbCopies = 0, LyokoOrder ordre = LyokoOrder.nothing, string LGharcelement = "", Tour tourAttackDefendTarget = null) {
            bool creationValidated = false;
            if (tourAttackDefendTarget == VarG.carthage.listeTours[0]) {
                DebugXana("CreateMonsters_TYPE0");
                //crd = CRDManager.C("carthage", VarG.carthageParam.towerCarthage.transform.localPosition+(Vector3.up*10)+(Vector3.back * 15)); //atkcarthagetower
                crd = CRDManager.C("carthage", VarG.carthageParam.towerCarthage.GetCrdToVirt(), false);
            }
            if (ordre == LyokoOrder.attackSkidInGarage) {
                DebugXana("CreateMonsters_GARAGE");
                crd = CRDManager.C("carthage", CarthageAppearPointElement.Get_MonsterGarageAppearPoint().transform.localPosition, false);
            }
            if ((ordre == LyokoOrder.attackDomeBridge || ordre == LyokoOrder.attackDomeVoid) && nomMonstre == "manta") {
                //REPOSITION BIRTH OVER VOID IF MANTA
                DebugXana("CreateMonsters_TYPE1");
                crd = CRDManager.C("carthage", CarthageAppearPointElement.Get_MonsterAppearPoint(false).transform.localPosition, false);
            }
            if (ordre == LyokoOrder.attackCore && nomMonstre == "manta") {
                DebugXana("CreateMonsters_TYPE2");
                crd = CRDManager.C("carthage", CarthageAppearPointElement.Get_MonsterAppearPoint(true).transform.localPosition, false);
            }
            if (ordre == LyokoOrder.drainSkidInGarage) {
                DebugXana("CreateMeduseDrainSkid");
                crd = CRDManager.C("carthage", VarG.carthageParam.skidbladnirAnchor.transform.localPosition + (Vector3.up * 10), false);
            }
            if (ordre == LyokoOrder.drainCore) {
                DebugXana("CreateMeduseDrainCore");
                crd = CRDManager.C("carthage", VarG.carthageParam.corePlane.transform.localPosition + (Vector3.up * 10), false);
            }
            if (ordre == LyokoOrder.attackCore && nomMonstre == "rampant") {
                DebugXana("CreateMonsters_TYPE3");
                crd = CRDManager.C("carthage", VarG.carthageParam.coreDown.transform.localPosition, false);
            }
            if (crd == null)
                Debug.LogError("NULL CRD on creating monsters");

            if (nbCopies == 0) {
                if (LyokoGuideUtilities.GetTotalLGtype_OnSector(crd.GetTerritoire(), LgTypes.Monstre) + 1 + nbCopies <= 6)
                    creationValidated = true;
            } else {
                for (int a = nbCopies; a > 0; a--) {
                    if (LyokoGuideUtilities.GetTotalLGtype_OnSector(crd.GetTerritoire(), LgTypes.Monstre) + 1 + a <= 6) {
                        creationValidated = true;
                        nbCopies = a;
                        break;
                    } else {
                        DebugXana("reduction de nombre de copies: " + nbCopies);
                    }
                }
            }
            //ANNULATION SI TROP DE MONSTRES
            if (ordre != LyokoOrder.attackKey && ordre != LyokoOrder.attackMaze && !creationValidated) {
                DebugXana("Nombre max de monstre par territoire surpassé >> annulation de la création du/des monstre, déjà " + (LyokoGuideUtilities.GetTotalLGtype_OnSector(crd.GetTerritoire(), LgTypes.Monstre) + 1) + " présents");
                return null;
            }

            if (crd.GetTerritoire().GetStatus() == VirtualBuildStatus.inDestruction) {
                DebugXana("creation de monstres par Xana annulée: territoire en destruction");
                return null;
            }
            if (crd.GetTerritoire() == VarG.replika && (VarG.skidbladnir.IsMater() && VarG.skidbladnir.GetGuide().GetTerritoire().GetMondeV() != crd.GetTerritoire().GetMondeV())) {
                DebugXana("creation de monstres par Xana annulée: replika n'ayant potentiellement plus de recast car le skid n'y est plus");
                return null;
            }
            LyokoGuide lg = LyokoGuide.Create(nomMonstre, crd, nbCopies);

            DebugXana("initMonstre" + lg.nom + " " + ordre + " " + crd);// + " " + crd.GetTour().ID_de_secteur + " " + LGharcelement);
            DebugXana("ordre de" + " " + lg.nom + " " + "envoyé :" + " " + ordre.ToString());
            if (ordre == LyokoOrder.attackTower) {
                lg.orderProfile.GiveTowerAttackOrder(lg, tourAttackDefendTarget.ID_universel);
            }
            if (ordre == LyokoOrder.defendTower) {
                lg.orderProfile.GiveTowerDefenseOrder(lg, tourAttackDefendTarget.ID_universel);
            }
            if (ordre == LyokoOrder.harass) {
                lg.GiveHarassOrder(LGharcelement);
            }
            if (ordre == LyokoOrder.attackMaze) {
                lg.orderProfile.GiveCarthageOrder(lg, LyokoOrder.attackMaze);
                lg.carthageProfile.SetPos(CarthagePos.maze); //permet d'éviter les bugs de comabt
                lg.SetupVol(false);//force le non vol pour la chute sur le terrain
            }
            if (tourAttackDefendTarget == VarG.carthage.listeTours[0]) {
                lg.SetupVol(true); //force le vol pour la chute efficace
            }
            if (ordre == LyokoOrder.drainSkidInGarage) {
                lg.SetupVol(true);
                lg.AI.AllowsMoving(false);
                lg.DisableAI();
                lg.AnnulerChute();
                lg.orderProfile.GiveCarthageOrder(lg, LyokoOrder.drainSkidInGarage);
                lg.carthageProfile.SetPos(CarthagePos.garageSkid);
            }
            if (ordre == LyokoOrder.attackKey) {
                lg.orderProfile.GiveCarthageOrder(lg, LyokoOrder.attackKey);
                lg.SetupVol(false);//force le non vol pour la chute sur le terrain
            }
            if (ordre == LyokoOrder.attackDomeBridge) {
                if (lg.nom == "manta") {
                    Fx_Vmap.Create(FxType.mantaBirth, lg.transform.position, VarG.carthage, lg.gameObject.layer);
                    lg.carthageProfile.SetPos(CarthagePos.domeVoid);
                } else {
                    lg.carthageProfile.SetPos(CarthagePos.domeBridge);
                }
                // On harcèle direct dans ce cas ci, si possible
                if (LGharcelement != "") {
                    lg.GiveHarassOrder(LGharcelement);
                } else {
                    lg.carthageProfile.UpdateCarthageObjective(VarG.carthageParam.domeBridge);
                    lg.orderProfile.GiveCarthageOrder(lg, LyokoOrder.attackDomeBridge);
                    ;
                }
            }
            if (ordre == LyokoOrder.attackDomeVoid) {
                if (lg.nom == "manta") {
                    Fx_Vmap.Create(FxType.mantaBirth, lg.transform.position, VarG.carthage, lg.gameObject.layer);
                    lg.carthageProfile.SetPos(CarthagePos.domeVoid);
                } else {
                    lg.carthageProfile.SetPos(CarthagePos.domeBridge);
                }
                // On harcèle direct dans ce cas ci, si possible
                if (LGharcelement != "") {
                    lg.GiveHarassOrder(LGharcelement);
                } else {
                    if (lg.nom == "manta")
                        lg.carthageProfile.UpdateCarthageObjective(VarG.carthageParam.domeVoid);
                    lg.orderProfile.GiveCarthageOrder(lg, LyokoOrder.attackDomeVoid);
                }
            }
            if (ordre == LyokoOrder.attackSkidInGarage) {
                lg.orderProfile.GiveCarthageOrder(lg, LyokoOrder.attackSkidInGarage);
                lg.carthageProfile.SetPos(CarthagePos.garageSkid);
            }
            if (ordre == LyokoOrder.attackCore) {
                if (lg.nom == "manta") {
                    Fx_Vmap.Create(FxType.mantaBirth, lg.transform.position, VarG.carthage, lg.gameObject.layer);
                    lg.carthageProfile.SetPos(CarthagePos.domeVoid);
                } else {
                    lg.carthageProfile.SetPos(CarthagePos.coreRoom);
                }
                lg.orderProfile.GiveCarthageOrder(lg, LyokoOrder.attackCore);
            }
            return lg;
        }

        public void ResetClonePolyUsage() {
            DebugXana("ResetClonePolyUsage resetted on new decyphering");
            used_clonePoly_ParDecryptage = false;
        }
        public void OnTowerFullyDeactivated(Tour _tower) { //new 40X
            if (_tower == towerTarget) {
                towerTarget = null;
                sendEnergyRegularlyTo = null;
            }
        }
        public string RandomMonster(List<string> listeMonstres) {
            if (listeMonstres.Count == 0) {
                Debug.LogError("noMonsterAvailableInRandomization!");
            }
            int hasard = UnityEngine.Random.Range(0, listeMonstres.Count);
            /*for (int a = 0; a < 100; a++) {
                DebugXana("testHasard : "+ listeMonstres[UnityEngine.Random.Range(0, listeMonstres.Count)]);
            }*/
            return listeMonstres[hasard];
        }
        /*
                private string RandomMonster_4(string a, string b, string c, string d) {
                    //on peut remplacer les frolions/kankrelats x3 par une tarentule ou un megatank, voir deux en mode difficile
                    int hasard = Mathf.FloorToInt(UnityEngine.Random.Range(0, 101));
                    if (hasard < 25) {
                        return a;
                    } else if (hasard >= 25 && hasard < 50) {
                        return b;
                    } else if (hasard >= 50 && hasard < 75) {
                        return c;
                    } else {
                        return d;
                    }
                }*/

        public void RameuterMonstres(LyokoGuide a) {
            DebugXana("le monstre appelle les autres monstres présents sur le territoire");
            foreach (LyokoGuide LGMonstre in LyokoGuide.liste) {
                //on verifie que le monstre testé soit de Xana, qu'il ne soit pas celui qui appelle mais qu'il soit sur le même territoire
                if (LGMonstre.camp == Camp.XANA && LGMonstre != a && LGMonstre.GetTerritoire() == a.GetTerritoire() && LGMonstre.nom != "gardien" && LGMonstre.nom != "meduse") {
                    if (LGMonstre.battleProfile.inBattle)
                        continue;
                    if (LGMonstre.TourActiveProche() || LGMonstre.orderProfile.savedOrder == LyokoOrder.attackTower ||
                        LGMonstre.orderProfile.savedOrder == LyokoOrder.defendTower) {
                        DebugXana(LGMonstre.nom + " ne bouge pas car près d'une tour activée ou à déjà pour ordre d'en attaquer une");
                    } else {
                        LGMonstre.SetObjectif(a);
                    }
                }
            }
        }

        public static void OnVirt_ManipDna(string nomScan, string lastScan) { //une seule manip ADN possible ?
            if (nomScan != "" && lastScan != "" && nomScan != lastScan && LyokoGuerrier.GetByName(nomScan).dnaProtocols.GetState() == DnaModes.nothing) {
                if (UnityEngine.Random.Range(0, 11) >= LevelableOption.GetOp(GameOptionName.dnaMixBugs).currentValue * 2.5f) {
                    if (LyokoGuerrierUtilities.earthCodeNeeded && UnityEngine.Random.Range(0, 11) < LevelableOption.GetOp(GameOptionName.dnaMixBugs).currentValue * 1.5f && nomScan == "aelita") {
                        VarG.aelita.dnaProtocols.SetState(DnaModes.bugEarthCode);
                        return;
                    }
                    if (LyokoGuerrier.GetByName(lastScan).dnaProtocols.GetState() != DnaModes.nothing)
                        return;
                    //for (int a = 0; a < 100; a++) {
                    //   DebugXana("UnityEngine.Random.Range(0,2)  " + UnityEngine.Random.Range(0, 2));
                    //}
                    if (UnityEngine.Random.Range(0, 2) == 0) {//2 est exclus
                        LyokoGuerrierUtilities.SetStateMultiple(DnaModes.confusion, LyokoGuerrier.GetByName(nomScan), LyokoGuerrier.GetByName(lastScan));
                    } else {
                        LyokoGuerrierUtilities.SetStateMultiple(DnaModes.melange, LyokoGuerrier.GetByName(nomScan), LyokoGuerrier.GetByName(lastScan));
                    }
                }
            }
        }

        #region old
        // public void OnTowerActivationReaction(Tour t) {
        //ACTIONS spécifiques à Xana juste après l'activation d'une tour

        //on verifie si les lyokoguerriers sont virtualisés et on provoque un effet aléatoire de modification ADN, selon les paramètres de la partie
        //annulé, la modif adn n'a lieu que lors de la virtualisation (et en plus celle d'ici est peut être bugguée sur les setmultiple
        /*if (LevelableOption.GetOp(GameOptionName.dnaMixBugs).currentValue > 0) {
            List<LyokoGuerrier> nbVirtLGuerrier = new List<LyokoGuerrier>();
            foreach (LyokoGuerrier LGn in LyokoGuerrier.listeLG) {
                if (LGn.IsVirt() && !LGn.IsFront()) nbVirtLGuerrier.Add(LGn);
            }
            if (nbVirtLGuerrier.Count >= 1) {
                //on utilise l'index et le nbVirtLGuerrier pour rajouter le statut mélange à un autre lyokoguerrier si il y en a un qui tombe dessus
                uint index = 0;
                foreach (LyokoGuerrier LG in nbVirtLGuerrier) {
                    if (LG.dnaProtocols.GetState() == DnaModes.nothing) {
                        int alea = Mathf.FloorToInt(UnityEngine.Random.Range(0, 11));
                        bool cancel = false;
                        // a n'activer que si on est en mode -code terre-
                        DebugXana("<color=yellow> aela ADN xanaManip : " + alea + " on " + LG.nom + "</color>");
                        if (alea < LevelableOption.GetOp(GameOptionName.dnaMixBugs).currentValue * 2f && LG.nom == "aelita" && LyokoGuerrierUtilities.earthCodeNeeded) {
                            LG.SetState(DnaModes.bugEarthCode);
                        } else {
                            if (nbVirtLGuerrier.Count >= 2) {
                                if (alea < LevelableOption.GetOp(GameOptionName.dnaMixBugs).currentValue * 2f) {
                                    //si il n'y a AUCUN lyokoguerrier avec adn mélangé, on peut faire un couple
                                    foreach (LyokoGuerrier Lgg in LyokoGuerrier.listeLG) {
                                        if (Lgg.dnaProtocols.GetState() == DnaModes.melange || Lgg.dnaProtocols.GetState() == DnaModes.confusion) {
                                            cancel = true;
                                            break;
                                        }
                                    }
                                    if (!cancel) {
                                        //on inflige melange ou confusion, mais seulement si on trouve un autre lyokoguerrier virtualisé et sans ADN changé
                                        foreach (LyokoGuerrier LGn2 in nbVirtLGuerrier) {
                                            if (LGn2 != LG && LGn2.dnaProtocols.GetState() == DnaModes.nothing) {
                                                if (UnityEngine.Random.value >.5f) {
                                                    LyokoGuerrierUtilities.SetStateMultiple(DnaModes.confusion, LG, LGn2);
                                                } else {
                                                    LyokoGuerrierUtilities.SetStateMultiple(DnaModes.melange, LG, LGn2);
                                                }
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    index++;
                }
            }
        }*/
        // ResponseTowerActivation(); //on force la défense directe de la tour qui vient d'être activée (sans attendre le timer d'initiative de xana)
        //}
        #endregion
    }
}
