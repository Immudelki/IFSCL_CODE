using System;
using System.Collections.Generic;
using UnityEngine;

public enum Order
{
    nothing,
    attackKey,
    attackMaze,
    attackDomeVoid,
    attackDomeBridge,
    attackCore,
    attackSkidInGarage,
    drainSkidInGarage,
    drainCore,
    defendTower,
    attackTower,
    destroySector,
    harass,
    throwAelita,
    throwItself,
    landAelita,
    diveInSea
};

public class Xana : Camps
{

    //Dès lors qu'aucune attaque n'est plus en activité (voir conditions), on reset le timer de Xana

    //ATTACKS IN CURRENT TIMEFRAME
    public bool hasTowerAtkOnce = false; //initiative
    public bool hasCoreAtkOnce = false;
    public bool hasGarageAtkOnce = false;
    public bool hasDrainSkidAtkOnce = false;
    public bool hasDrainCoreAtkOnce = false;
    
    public List<int> listeToursActivables;
    public MainTimer TInitiative = new MainTimer();
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
    private static bool DebugXanaV = true;
    public TowerTypeAttack chosenAttack = TowerTypeAttack.nothing;
    public int waitturns_before_newMonsters = 3;
    //la toute première materialisation de monstres doit être légèrement retardée pour pas harasser le joueur trop vite
    public int waitturns_before_newMonsters_attaqueTour = 2;
    public Tour sendEnergyRegularlyTo = null;
    public DateTime minimumDateTime_ToAttack;
    //LyokoGuide.Create interdit, on doit passer par createMonstreSuite
    public bool nextAttackFaster = false;
    public void ResetAttackTime() {
        DebugXana("ResetAttackTime");
        if (nextAttackFaster) { //si la dernière desactivation est due à une tour détruire (par destruction de territoire), alors la prochaine attaque est plus rapide
            minimumDateTime_ToAttack = VarG.fictionalTime.AddHours(6).AddMinutes(59);
            nextAttackFaster = false;
        } else {
            minimumDateTime_ToAttack = VarG.fictionalTime.AddHours(12).AddMinutes(59);
        }
        TCoreAttack.Stop();
        TInitiative.Stop();
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
    /*() {
        minimumDateTime_ToAttack = VarG.fictionalTime;
        if (SavedPreferences.xanaInitiativeLevel.currentValue > 0)
            TInitiative.Start();
        if (SavedPreferences.xanaCoreAttacks.currentValue > 0)
            TCoreAttack.Start();
        if (SavedPreferences.xanaGarageAttacks.currentValue > 0)
            TSkidGarageAttack.Start();

        TGestionXanatifies.Start();
    }*/
    //NOTE LA FONCTION PRISE DE CONTROLE DE TOUR OU DE MONSTRES/LYOKOGUERRIER DOIT ETRE SIMILAIRE ENTRE XANA ET HOPPER;
    public void Initialisation() {
        //confirmInitiativeLevel
        TCoreAttack.Stop();
        TInitiative.Stop();
        TSkidGarageAttack.Stop();
        TDrainSkidGarageAttack.Stop();
        TDrainCoreAttack.Stop();
        switch (SavedPreferences.xanaInitiativeLevel.currentValue) {
            case 0:
                TInitiative.setSpeed(999);
                break;
            case 1:
                TInitiative.setSpeed(11); //toutes les 5 minutes (sachant que pour l'activation des tours, xana attends une fois supplémentaire avant de re-défendre)
                break;
            case 2:
                TInitiative.setSpeed(7); //toutes les 3 minutes
                break;
            case 3:
                TInitiative.setSpeed(5); //toutes les QUASI 2 minutes
                break;
        }
        switch (SavedPreferences.xanaGarageAttacks.currentValue) {
            //sachant que Xana à une chance sur 2 de faire l'attaque uniquement si des LG sont déjà dans le cinquième territoire
            //et une chance sur 10 si aucun n'est dispo pour être virtualisé à l'usine et qu'ils ne sont en dehors de Lyoko
            case 0:
                TSkidGarageAttack.setSpeed(999);
                break;
            case 1:
                TSkidGarageAttack.setSpeed(25);
                break;
            case 2:
                TSkidGarageAttack.setSpeed(15);
                break;
            case 3:
                TSkidGarageAttack.setSpeed(7); //toutes les 3 minutes
                break;
        }
        switch (SavedPreferences.xanaScyphozoaDrainSkid.currentValue) {
            case 0:
                TDrainSkidGarageAttack.setSpeed(999);
                break;
            case 1:
                TDrainSkidGarageAttack.setSpeed(25);
                break;
            case 2:
                TDrainSkidGarageAttack.setSpeed(15);
                break;
            case 3:
                TDrainSkidGarageAttack.setSpeed(7); //toutes les 3 minutes
                break;
        }
        switch (SavedPreferences.xanaScyphozoaDrainCore.currentValue) {
            case 0:
                TDrainCoreAttack.setSpeed(999);
                break;
            case 1:
                TDrainCoreAttack.setSpeed(25);
                break;
            case 2:
                TDrainCoreAttack.setSpeed(15);
                break;
            case 3:
                TDrainCoreAttack.setSpeed(7); //toutes les 3 minutes
                break;
        }
        switch (SavedPreferences.xanaCoreAttacks.currentValue) {
            //sachant que Xana à une chance sur 2 de faire l'attaque uniquement si des LG sont déjà dans le cinquième territoire
            //et une chance sur 10 si aucun n'est dispo pour être virtualisé à l'usine et qu'ils ne sont en dehors de Lyoko
            case 0:
                TCoreAttack.setSpeed(999);
                break;
            case 1:
                TCoreAttack.setSpeed(25);
                break;
            case 2:
                TCoreAttack.setSpeed(15);
                break;
            case 3:
                TCoreAttack.setSpeed(7); //toutes les 3 minutes
                break;
        }
        TReactivity.setSpeed(1); // en secondes, toutes les 10-12 secondes (*2 par waitturns_before_newMonsters au besoin)
        TReactivity.Start();
        TSendEnergy.setSpeed(0.1f);
        TSendEnergy.Start();
        Debug.Log("TReactivity " + TReactivity.IsRunning());
        TGestionXanatifies.setSpeed(0.2f);
        TGestionXanatifies.Start();
        nextAttackFaster = false;
        ResetAttackTime();
        //puis on reforce finalement à juste 30 min de décalage en début de partie
        if (!SavedPreferences.xanaAttackOnStartup.toBool())
            minimumDateTime_ToAttack = VarG.fictionalTime.AddMinutes(30);
    }

    public void DisableXana() {
        TReactivity.Stop();
        TInitiative.Stop();
        TCoreAttack.Stop();
        TSkidGarageAttack.Stop();
        TDrainSkidGarageAttack.Stop();
        TDrainCoreAttack.Stop();
        TSendEnergy.Stop();
        TGestionXanatifies.Stop();
    }

    public void EnableXana() {
        TReactivity.Start();
        TSendEnergy.Start();
        TGestionXanatifies.Start();
        //les autres timers sont gérés par les canAttackAtAnyTime
    }

    public bool CanAttackAtAnyTime() {
        return GetSpanTime_TillNextAttack().CompareTo(TimeSpan.Zero) < 0;
    }

    public TimeSpan GetSpanTime_TillNextAttack() {
        TimeSpan tt = minimumDateTime_ToAttack - VarG.fictionalTime;
        return tt;
    }

    public bool DoOrAim_Attack(bool core, bool sector, bool skidAttack, bool skidDrain) {
        //si monstre ou LG xanatifié
        foreach (LyokoGuide lg in LyokoGuide.liste) {
            if (lg.camp == Camps.xana) {
                if (core && lg.savedOrderType == Order.attackCore && (lg.carthagePos == CarthagePos.inDomeVoid || lg.carthagePos == CarthagePos.inCoreRoom)
                    && VarG.MV_lyoko.GetStatus() == VirtualBuildStatus.created)
                    return true;
                if (sector && lg.lgType == LgTypes.LyokoGuerrier && lg.savedOrderType == Order.destroySector
                    && Territoire.IsATerritoire(lg.savedOrderStringValue) && Territoire.GetByName(lg.savedOrderStringValue).GetStatus() == VirtualBuildStatus.created)//xanatifié only
                    return true;
                if (skidAttack && lg.savedOrderType == Order.attackSkidInGarage && (lg.carthagePos == CarthagePos.inGarageElevatorRoom || lg.carthagePos == CarthagePos.inGarageSkid)
                    && VarG.garageSkid.GetStatus() == VirtualBuildStatus.created && (VarG.skidbladnir.IsMater() && VarG.skidbladnir.IsKindaDockedToGarage())) //xanatifié only
                    return true;
                if (skidDrain && lg.savedOrderType == Order.drainSkidInGarage && (lg.carthagePos == CarthagePos.inGarageElevatorRoom || lg.carthagePos == CarthagePos.inGarageSkid)
                    && VarG.garageSkid.GetStatus() == VirtualBuildStatus.created && (VarG.skidbladnir.IsMater() && VarG.skidbladnir.IsKindaDockedToGarage()))
                    return true;
            }
        }
        return false;
    }

    public void SendEnergy() {
        //DEMANDER ASKENERGIE ICI !!
        //si Xana à des monstres près de la tour, la prise de controle est accélérée
        if (sendEnergyRegularlyTo != null && ActionsF.journalDecrypt.tourDecryptage != null && !ActionsF.journalDecrypt.tourDecryptage.IsActivatedBy("xana")) {
            if (ActionsF.journalDecrypt.tourDecryptage.IsActivatedBy("franz")) {
                Cancel_TentativeDePriseDeControle();
                return;
            }
            bool verif = false;
            foreach (LyokoGuide LGMonstre in LyokoGuide.liste) {
                if (LGMonstre.camp == Camps.xana && LGMonstre.HasTowerProximity() && LGMonstre.id_UNIV_tourEntrable == sendEnergyRegularlyTo.ID_universel) {
                    verif = true;
                    break;
                }
            }
            if (verif) {
                energyManager.SendTo(ActionsF.journalDecrypt.tourDecryptage.energieProfile, 20);
            } else {
                energyManager.SendTo(ActionsF.journalDecrypt.tourDecryptage.energieProfile, 10);
            }
        }
    }

    public override void ActiverTour(int numTour = 0, bool fromXanaCode = false, TowerTypeAttack forcedAttaque = TowerTypeAttack.nothing) {
        if (ActionsF.rvlp.canAttack(false)) {
            if (!Tour.GetByUnivID(numTour).IsKo()) { //on revérifie ça une seconde fois au cas où on ai appelé direct cette fonction
                towerTarget = Tour.GetByUnivID(numTour);
                chosenAttack = forcedAttaque;
                Debug.Log("xana - veut prendre Controle de la Tour (ID universel):  " + (towerTarget.ID_universel - 1) + " / fromXanaCode:" + fromXanaCode);
                if (fromXanaCode) {
                    //with delay for DoAttack
                    //si y'a pas la connexion à Lyoko, on la lance direct vu que la fenêtre peut pas s'ouvrir
                    if (ActionsF.connexionLyoko.etatConnexion) {
                        ActionsF.xanaCode.Execution(false);
                    } else {
                        DoAttack();
                    }
                } else {
                    DoAttack();
                }
            }
        } else {
            Debug.LogWarning("attaque stoppée, le rvlp est trop proche de la fin ou la fenêtre de xana terrestre est toujours ouverte");
        }
    }
    public bool TrySkidDrain(bool forced = false) {
        if (!VarG.skidbladnir.CanBeReachedInGarage())
            return false;
        if (!forced) {
            if (SavedPreferences.xanaScyphozoaDrainSkid.currentValue <= 0 || hasDrainSkidAtkOnce || !CanAttackMore())
                return false;
            if (!TDrainSkidGarageAttack.IsRunning())
                TDrainSkidGarageAttack.Start();
            if (HasTourActive_OutsideLyoko()) {
                DebugXana("drain skid empêché: Xana attaque déjà avec une tour EXTERIEURE à Lyoko");
                return false;
            }
            if (!VarG.meduseFonction.CouldSpawnToDrain()) {
                DebugXana("drain skid empêché: Meduse deja en utilisation ou doit encore attendre avant d'être réutilisable");
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
        CreateMonsters(null, Order.drainSkidInGarage, 5, "", VarG.carthageParam.skidbladnirAnchor);
        hasDrainSkidAtkOnce = true;
        return true;
    }
    public bool TryCoreDrain(bool forced = false) {
        if (VarG.MV_lyoko.GetStatus()!=VirtualBuildStatus.created)
            return false;
        if (!forced) {
            if (SavedPreferences.xanaScyphozoaDrainCore.currentValue <= 0 || hasDrainCoreAtkOnce || !CanAttackMore())
                return false;
            if (!TDrainCoreAttack.IsRunning())
                TDrainCoreAttack.Start();
            if (HasTourActive_OutsideLyoko()) {
                DebugXana("drain core empêché: Xana attaque déjà avec une tour EXTERIEURE à Lyoko");
                return false;
            }
            if (!VarG.meduseFonction.CouldSpawnToDrain()) {
                DebugXana("drain core empêché: Meduse deja en utilisation ou doit encore attendre avant d'être réutilisable");
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
        CreateMonsters(null, Order.drainCore, 5, "", VarG.carthageParam.core);
        hasDrainCoreAtkOnce = true;
        return true;
    }


    public bool TrySkidGarageAttack(bool forced = false) {
        if (!VarG.skidbladnir.CanBeReachedInGarage())
            return false;
        if (!forced) {
            if (SavedPreferences.xanaGarageAttacks.currentValue <= 0 || hasGarageAtkOnce || !CanAttackMore())
                return false;
            if (!TSkidGarageAttack.IsRunning())
                TSkidGarageAttack.Start();
            if (HasTourActive_OutsideLyoko()) {
                DebugXana("attaque skid empêchée: Xana attaque déjà avec une tour EXTERIEURE à Lyoko");
                return false;
            }
            DebugXana("TrySkidGarageAttack");

            //on teste déjà la chance avant, qui appelle la fonction TrySkidGarageAttack elle même
            int chances = 5; //sur 10
            int chanceTest = UnityEngine.Random.Range(0, 11); //entre 0 et 10
            if (chanceTest > chances) {
                DebugXana("attaque skid empêchée: pas assez de chances: " + chanceTest + "/" + chances);
                return false;
            }
        }
        //note: si il y a au moins un monstre, le reste sera créé en voulant harceler les lyokoguerriers présents dans le garage skid
        if (IsMonsterAvailableFor(VarG.carthage, Order.attackSkidInGarage)) {
            foreach (LyokoGuide LGMonstre in LyokoGuide.liste) {
                if (LGMonstre.camp == Camps.xana && LGMonstre.nestedIN == null && !LGMonstre.T_chute.IsRunning() && !LGMonstre.recyclable && LGMonstre.GetTerritoire() == VarG.carthage &&
                    LGMonstre.savedOrderType != Order.defendTower && LGMonstre.savedOrderType != Order.attackSkidInGarage && LGMonstre.savedOrderType != Order.attackTower && LGMonstre.savedOrderType != Order.attackCore && LGMonstre.harcelerWhichLG == "" &&
                    (LGMonstre.carthagePos == CarthagePos.inGarageSkid || LGMonstre.carthagePos == CarthagePos.inGarageElevatorRoom)) {
                    DebugXana(LGMonstre.nom, ", dispo pour aller attaquer le skid dans le garage");
                    LGMonstre.GiveCarthageOrder(Order.attackSkidInGarage); //faire des téléports ?
                }
            }
        }
        for (int a = 0; a < 5; a++) {//max de 5 monstres pour attaquer le skid dans le garage
            if (LyokoGuideUtilities.GetTotalLG_AttackingSkidInGarage() < 6) {
                DebugXana("confirmation, y'a moins de " + a + " monstres allant attaquer ou attaquant le skid dans le garage donc on en créer");
                CreateMonsters(null, Order.attackSkidInGarage, 0, "", CarthageAppearPointElement.Get_MonsterGarageAppearPoint());
            }
        }
        hasGarageAtkOnce = true;
        return true;
    }



    private bool CanAttackMore() {
        return (SavedPreferences.xanaMultipleAttacks.toBool() || (!HasAtkOnce() && !SavedPreferences.xanaMultipleAttacks.toBool()));
    }
    private bool HasAtkOnce() {
        return hasTowerAtkOnce || hasCoreAtkOnce || hasGarageAtkOnce || hasDrainSkidAtkOnce || hasDrainCoreAtkOnce;
    }
    public bool TryCoreAttack(bool forced = false) {
        if (SavedPreferences.xanaCoreAttacks.currentValue <= 0 || VarG.MV_lyoko.GetStatus() != VirtualBuildStatus.created || VarG.carthage.GetStatus() != VirtualBuildStatus.created)
            return false;
        if (hasCoreAtkOnce || !CanAttackMore())
            return false;
        if (!TCoreAttack.IsRunning())
            TCoreAttack.Start();
        if (HasTourActive_OutsideLyoko()) {
            DebugXana("attaque coeur empêchée: Xana attaque déjà avec une tour EXTERIEURE à Lyoko");
            return false;
        }
        DebugXana("TryCoreAttack");
        int chances = 10; //sur 10
                          //si il y a des LyokoGuerriers virtualisés, mais PAS dans Lyoko, on réduit les chances par 6 des monstres qui attaquent le coeur, mais on augmente si il y a déjà des LG
        if (LyokoGuideUtilities.GetAllLgOutside(VarG.MV_lyoko, LgTypes.LyokoGuerrier, null, false, Camps.jeremie).Count > 0) {
            chances -= 6;
        } else {
            if (LyokoGuideUtilities.GetAllLgIn(VarG.MV_lyoko, LgTypes.LyokoGuerrier, null, false, Camps.jeremie).Count > 0) {
                chances += 3;
            }
        }
        //si il y a déjà des LyokoGuerriers dans carthage, on réduit les chances par le nb de LG d'avoir de nouveaux monstres qui attaquent le coeur
        chances -= LyokoGuideUtilities.GetAllLgIn(VarG.carthage, LgTypes.LyokoGuerrier, null, false, Camps.jeremie).Count;

        int chanceTest = UnityEngine.Random.Range(0, 11); //entre 0 et 10
        if (!forced && chanceTest > chances) {
            DebugXana("attaque coeur empêchée: pas assez de chances: " + chanceTest + "/" + chances);
            return false;
        }
        //note: si il y a au moins un monstre, le reste sera créé en voulant harceler les lyokoguerriers présents dans le coeur ?
        if (IsMonsterAvailableFor(VarG.carthage, Order.attackCore)) {
            foreach (LyokoGuide LGMonstre in LyokoGuide.liste) {
                if (LGMonstre.camp == Camps.xana && LGMonstre.nestedIN == null && !LGMonstre.T_chute.IsRunning() && !LGMonstre.recyclable && LGMonstre.GetTerritoire() == VarG.carthage &&
                    LGMonstre.savedOrderType != Order.defendTower && LGMonstre.savedOrderType != Order.attackTower && LGMonstre.savedOrderType != Order.attackSkidInGarage && LGMonstre.harcelerWhichLG == "" &&
                    (LGMonstre.carthagePos == CarthagePos.inCoreRoom || LGMonstre.carthagePos == CarthagePos.inDomeVoid || (LGMonstre.carthagePos == CarthagePos.onDomeBridge && LGMonstre.HasVol()))) {
                    DebugXana(LGMonstre.nom, ", dispo pour aller attaquer le coeur de Lyoko");
                    LGMonstre.GiveCarthageOrder(Order.attackCore);
                }
            }
        }
        for (int a = 0; a < 4; a++) {//max de 4 monstres pour attaquer le coeur
            if (LyokoGuideUtilities.GetTotalLG_AttaquantCoeur() < a) {
                DebugXana("confirmation, y'a moins de " + a + " monstres allant attaquer ou attaquant le coeur de lyoko donc on en créer");
                CreateMonsters(null, Order.attackCore, 0, "", VarG.carthageParam.core);
            }
        }
        hasCoreAtkOnce = true;
        return true;
        //normalement en comptant les monstres créés naturellement pour harceler, ça devrait en faire assez
    }

    public void DoAttack() {
        if (!TInitiative.IsRunning() && SavedPreferences.xanaInitiativeLevel.currentValue > 0)
            TInitiative.Start();
        if (SavedPreferences.xanaInitiativeLevel.currentValue == 0)
            TryTowerAttack(true);
        TentativePriseDeControleTour();
    }

    public void Gerer_Xanatifies() {
        foreach (LyokoGuerrier LGuerrier in LyokoGuerrier.liste) {
            if (LGuerrier.IsVirt() && LGuerrier.controleXana &&
                !LGuerrier.GetGuide().inBattle && !LGuerrier.GetGuide().IsUnconscious() && !LGuerrier.GetGuide().T_chute.IsRunning()) {
                DebugXana("gestion du xanatifié", LGuerrier.nom);
                if (LGuerrier.IsAloneOnSector(true) && LGuerrier.GetGuide().savedOrderType == Order.harass && LGuerrier.GetGuide().savedOrderStringValue == "") {
                    LGuerrier.GetGuide().DeleteAllOrders();
                    // Debug.Log("00");
                }
                if (LGuerrier.nom == "william" && VarG.aelita.IsVirt() && VarG.aelita.GetGuide() == LGuerrier.GetGuide().territoire && SavedPreferences.franzHopperAlive.toBool() && VarG.franzFonction.currentGuide != null) {
                    LGuerrier.GetGuide().GiveHarassOrder(VarG.aelita.GetGuide()); //vise aelita en PRIO pour aller la supersmoker si Franz est vivant
                                                                                  // Debug.Log("01");
                    return;
                }
                if (LGuerrier.GetGuide().savedOrderType == Order.nothing || LGuerrier.GetGuide().savedOrderType == Order.harass) { //si y'a un ordre simple d'harass, il est overiddé
                    if (LGuerrier.GetGuide().GetTerritoire().GetMondeV() == VarG.MV_lyoko) {
                        if (LGuerrier.GetGuide().GetTerritoire().GetMondeV() == VarG.carthage) {
                            if (SavedPreferences.xanaCoreAttacks.currentValue > 0) {
                                if (LGuerrier.GetGuide().GiveSectorDestructionOrder()) //autoAdapt to carthage et aelita
                                    return;
                            }
                        } else {
                            if (SavedPreferences.xanaSectorAttacks.currentValue > 0) {
                                if (LGuerrier.GetGuide().GiveSectorDestructionOrder()) //autoAdapt to carthage et aelita
                                    return;
                            }
                        }
                    }
                }
                //si aucun de ses ordres n'est intéressant pour le LG on lui laisse faire un test de harass normal
                foreach (LyokoGuerrier _lg in LyokoGuerrier.liste) {
                    if (_lg.CanBeHarrassedOnLyoko() && _lg.GetGuide().GetTerritoire() == LGuerrier.GetGuide().GetTerritoire()) {
                        //si il est dans la salle du coeur, c'est que les attaques de coeur sont autorisées, il y restera donc jusqu'à avoir fini sa tâche
                        //si on lui donne l'ordre de suivre un Lguide et que ce dernier sort de la salle, on est pas avançé
                        //donc il ne doit harceler personne une fois dans la salle du coeur
                        if (LGuerrier.GetGuide().carthagePos != CarthagePos.inCoreRoom) {
                            Debug.Log("harass task given for XANAWARRIOR " + LGuerrier.nom);
                            LGuerrier.GetGuide().GiveHarassOrder(_lg.GetGuide().nom);
                            return;
                        }
                    }
                }
                //do not do LGuerrier.IsAloneOnSector(true)
                if (!LGuerrier.isDefinitiveControl && !LGuerrier.GetGuide().inBattle && LGuerrier.GetGuide().savedOrderType == Order.nothing && !LGuerrier.GetGuide().enDeplacement) {
                    LGuerrier.GetGuide().GiveThrowItselfInSeaOrder();
                    Debug.Log(LGuerrier.nom + " got throw himself into sea order");
                    return;
                }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log("NO NEW ACTIVITY FOUND FOR XANAWARRIOR " + LGuerrier.nom);
#endif
            }
        }
    }

    public void DebugXana(params string[] rest) {
        if (DebugXanaV) {
            string messageFinal = "";
            foreach (string a in rest)
                messageFinal += a + " ";
            Debug.Log("//Xana//" + messageFinal);
        }
    }

    public override void UpdateTimer() {
        if (TGestionXanatifies.incrementReach())
            Gerer_Xanatifies();
        if (TInitiative.incrementReach()) {
            if (ActionsF.xanaCode.etat == 0) {
                if (DebugXanaV)
                    Debug.Log("TInitiative prise de controle de Tour par Xana!");
                TryTowerAttack(false);
            }
        }
        if (TSkidGarageAttack.incrementReach())
            TrySkidGarageAttack(false);
        if (TCoreAttack.incrementReach())
            TryCoreAttack(false);
        if (TReactivity.incrementReach()) {
            Debug.Log("Xana - T-réactivité (harcèlement/défense de tour) par Xana!");
            //harceler en toute fin permet d'utiliser les monstres restants.

            //note que même si xanaRandomMonster est désactivé, on garde ça pour les boss, dont les apparitions restent possibles
            ResponseLwProximity();

            ResponseLwActivityInTower();
            ResponseHopperTower();

            VerifContinuelle();
        }
        if (TSendEnergy.incrementReach())
            SendEnergy();
    }

    public bool TryTowerAttack(bool forced = false) {
        if (hasTowerAtkOnce || !CanAttackMore())
            return false;
        if (forced || SavedPreferences.xanaInitiativeLevel.currentValue >= 1) {
            if (towerTarget != null) {
                //towertarget est delete après tentativePriseDeControle donc on save
                Tour towerTargetSave = towerTarget;
                TentativePriseDeControleTour();
                Debug.Log("towerTargetSave " + towerTargetSave);
                if (towerTargetSave.IsActivatedBy("xana"))
                    hasTowerAtkOnce = true;
                return towerTargetSave.IsActivatedBy("xana");
            } else {
                if (UnityEngine.Random.value > .3f || forced) {
                    Debug.Log("Xana initiative, tenterais bien d'activer une tour");
                    ActiverTourRandom(false, true);
                    if (this.HasTourActive())
                        hasTowerAtkOnce = true;
                    return this.HasTourActive();
                }
            }
        }
        return false;
    }

    public void raz(bool fromRVLP = false) {
        towerTarget = null;
        sendEnergyRegularlyTo = null;
        if (fromRVLP) {
            TCoreAttack.Reset();
            TSkidGarageAttack.Reset();
            TDrainSkidGarageAttack.Reset();
            TDrainCoreAttack.Reset();
            TReactivity.Reset();

            if (SavedPreferences.xanaInitiativeLevel.currentValue > 0)
                TInitiative.Reset();
        } else {
            TCoreAttack.Stop();
            TSkidGarageAttack.Stop();
            TDrainSkidGarageAttack.Stop();
            TDrainCoreAttack.Stop();
            TReactivity.Stop();

            TInitiative.Stop();
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

    public void VerifContinuelle() {
        //ce que l'on peut aussi voir c'est la vitesse de "rechargment" de Xana, notamment en sandbox.
        //'activation tour' vaut aussi pour une activation de Xana lui même, et donc la défense de la tour
        //pour l'instant les monstres ne peuvent toujours pas se détacher d'une tour qu'ils défendaient

        if (CanAttackAtAnyTime()) {
            //initiative désactivée par l'activation d'une tour, on le reactive si besoin
            if (!TInitiative.IsRunning() && !HasTourActive() && SavedPreferences.xanaInitiativeLevel.currentValue > 0)
                TInitiative.Start();
            if (!TCoreAttack.IsRunning() && LyokoGuideUtilities.GetTotalLG_AttaquantCoeur() <= 0 && SavedPreferences.xanaCoreAttacks.currentValue > 0)
                TCoreAttack.Start();
            if (!TSkidGarageAttack.IsRunning() && LyokoGuideUtilities.GetTotalLG_AttackingSkidInGarage() <= 0 && SavedPreferences.xanaGarageAttacks.currentValue > 0)
                TSkidGarageAttack.Start();

            //on s'assure auparavant qu'il y a déjà eu au moins une attaque d'un type
            if (HasAtkOnce()) {
                if (LyokoGuideUtilities.GetTotalLG_AttaquantCoeur() <= 0 && LyokoGuideUtilities.GetTotalLG_AttackingSkidInGarage() <= 0 && !HasTourActive()) {
                    ResetAttackTime();
                    Debug.Log("NO MORE ATTACK, TIME BEFORE NEXT ATTACK RESETTED");
                }
            }
        }
    }

    public bool TryAttackWhileWaiting() {
        //le temps d'attaque est géré différemment selon que l'on soit en attente ou non
        if (UnityEngine.Random.Range(1, 300) < SavedPreferences.xanaInitiativeLevel.currentValue * 10 && !hasCoreAtkOnce) {
            return TryTowerAttack(true);
        }
        if (UnityEngine.Random.Range(1, 300) < SavedPreferences.xanaCoreAttacks.currentValue * 10 && !hasCoreAtkOnce) {
            return TryCoreAttack(true);
        }
        if (UnityEngine.Random.Range(1, 300) < SavedPreferences.xanaGarageAttacks.currentValue * 10 && !hasGarageAtkOnce) {
            return TrySkidGarageAttack(true);
        }
        if (UnityEngine.Random.Range(1, 300) < SavedPreferences.xanaScyphozoaDrainSkid.currentValue * 10 && !hasDrainSkidAtkOnce) {
            return TrySkidDrain(true);
        }
        if (UnityEngine.Random.Range(1, 300) < SavedPreferences.xanaScyphozoaDrainCore.currentValue * 10 && !hasDrainCoreAtkOnce) {
            return TryCoreDrain(true);
        }
        return false;
        //en théorie, xanaSectorAttacks est inutilisable par Xana car on ne peut pas 
        //attendre avec une Aelita xanatifiée indéfinitivement chez Xana, seul moyen pour
        //lui de détruire un territoire
    }

    public void Cancel_TentativeDePriseDeControle() {
        //au cas où xana ai commencer à envoyer de l'énergie, on l'enlève.
        if (towerTarget != null) {
            sendEnergyRegularlyTo = null;
            TInitiative.Stop();
            if (towerTarget.energieProfile.getCampMineur() == 0)
                towerTarget.energieProfile.RendreEnergie(true, false);

            towerTarget = null;
        }
    }

    public void TentativePriseDeControleTour() {
        Debug.Log("tentativePRISEcontroleTOUR");
        //Xana peut avoir la volonté d'activer une tour/ou d'en prendre controle, mais ne pas avoir assez d'énergie le pousse à ré-essayer à chaque fois
        if (towerTarget != null) {
            if (towerTarget.IsActivatedBy("xana")) {
                Cancel_TentativeDePriseDeControle();
            } else {
                if (towerTarget.IsActivatedBy("franz")) {
                    //on fait abandonner Xana, il tentera de tout façon de détruire la tour
                    Cancel_TentativeDePriseDeControle();
                } else {
                    if (towerTarget.IsActivatedBy("jeremie")) {
                        if (ActionsF.journalDecrypt.TDecryptage.IsRunning()) {
                            sendEnergyRegularlyTo = towerTarget;
                        } else {
                            Cancel_TentativeDePriseDeControle();
                        }
                    } else {
                        //si Xana à déjà une tour ou que Jeremie à activer une tour, alors Xana n'activera pas de tour (si c'est sur le même monde), il préfèrera attaquer la tour (ou la convertir)
                        if (HasTourActive() || (Camps.jeremie.HasTourActive() && Tour.ReturnActiveTowers("jeremie")[0].ID_Supercalculateur == towerTarget.ID_Supercalculateur && !Tour.ReturnActiveTowers("jeremie").Contains(towerTarget))) {
                            Cancel_TentativeDePriseDeControle();
                        } else {
                            //DEMANDER ASKENERGIE ICI !!;
                            energyManager.SendTo(towerTarget.energieProfile, DepensesEnergy.tour.maintien);
                        }
                    }
                }
            }
        }
    }

    public void UsualBossToHarassCheck() {
        foreach (LyokoGuerrier _LGuerrier in LyokoGuerrier.liste) {
            if (!TReactivity.IsRunning() || !_LGuerrier.CanBeHarrassedOnLyoko()) {
                BossChancesPanelLw.UpdateAll(_LGuerrier, 0, 0, 0, 0);
            } else {
                CheckBossesChances(_LGuerrier, _LGuerrier.GetGuide()); //faire une moyenne du test pour tout les LG ?
            }
        }
    }

    private void ResponseLwProximity() {
        if (waitturns_before_newMonsters > 0)
            waitturns_before_newMonsters--;
        DebugXana("ResponseLwProximity");
        bool STOPLOOP = false;
        foreach (LyokoGuerrier _LGuerrier in LyokoGuerrier.liste) {
            if (STOPLOOP) { // TODO: improve so that only other LW from the same sector will be outed of loop if the first one on that sector is getting new monsters
                Debug.Log("STOPLOOP");
                return;
            }
            LyokoGuide _LGuide = _LGuerrier.GetGuide();
            if (_LGuerrier.CanBeHarrassedOnLyoko()) {
                int maxMonsterCount = 0;
                foreach (LyokoGuide lg in LyokoGuide.liste) {
                    if (!lg.recyclable && lg.GetTerritoire() == _LGuide.GetTerritoire() && lg.camp == Camps.xana)
                        maxMonsterCount++;
                }
                if (maxMonsterCount < 6) {
                    //on utilise le getDistance pour Hopper
                    if (IsMonsterAvailableFor(_LGuide.GetTerritoire(), Order.harass) && LyokoGuideUtilities.GetTotalLG_AttaquantDefendantTours(_LGuide.GetTerritoire(), Camps.xana) <= 0) {
                        DebugXana("on dit aux monstres dispos d'aller harceler (sachant qu'il n'y a pas d'attaque ou de défense de tour)");
                        foreach (LyokoGuide LGMonstre in LyokoGuide.liste) {
                            if (LGMonstre.nestedIN == null && !LGMonstre.T_chute.IsRunning() && !LGMonstre.recyclable && LGMonstre.camp == Camps.xana && LGMonstre.GetTerritoire() == _LGuide.GetTerritoire()
                                 && LGMonstre.savedOrderType != Order.destroySector && LGMonstre.savedOrderType != Order.defendTower && LGMonstre.savedOrderType != Order.attackTower && LGMonstre.harcelerWhichLG == "" && !LGMonstre.IsMeduseGardienOccuper()) {
                                DebugXana(LGMonstre.nom, "dispo pour aller harceler, sauf si exception carthage");
                                if (LGMonstre.GetTerritoire() == VarG.carthage) {
                                    if (LGMonstre.IsLG_accessibleInCarthage(_LGuide)) {
                                        //la fonction ci dessus prend en compte le fait que l'on puisse ne pas être sur carthage
                                        LGMonstre.GiveHarassOrder(_LGuide.nom);
                                    } else {
                                        if (_LGuide.carthagePos == CarthagePos.inMaze) {
                                            STOPLOOP = true; // NEW MONSTERS JUST CREATED SO WE DON'T TRY TO CREATE NEW FOR OTHER LW YET IN THIS LOOP
                                                             //on fait ça seulement pour quand les héros sont repérés dans les couloirs de carthage, 
                                                             //pour le reste, c'est les responsePresenceVoute etc...qui s'occupent de faire apparaitre des mponstres via defendreClef/defendreVoute...
                                            CreateMonsterBossToHarass(_LGuerrier, _LGuide, true);
                                            if (SavedPreferences.xanaRandomMonster.toBool())
                                                CreateMonsterBossToHarass(_LGuerrier, _LGuide, false);
                                            break;
                                        }
                                    }
                                } else {
                                    LGMonstre.GiveHarassOrder(_LGuide.nom);
                                }
                            }
                        }
                    } else {
                        DebugXana("confirmation, y'a pas de monstres dispo pour harceler", _LGuide.nom);
                        //note : si il y a des monstres en attaque ou en défense, on ne rajoute pas de Xana monstres pour harceler,
                        //mais on offre la possibilité que un groupe en atk/defense se splitte
                        if (LyokoGuideUtilities.GetTotalLG_AttaquantDefendantTours(_LGuide.GetTerritoire(), Camps.xana) > 0) {
                            DebugXana("il y a déjà des monstres attaquants défendant des tours -> donc on ne va créer des monstres pour harceler en prime. if LG close to tower, monster will attack or splitUp");
                            //voir fonction associée dans LyokoGuide
                        } else {
                            if (waitturns_before_newMonsters == 0) {
                                STOPLOOP = true; // NEW MONSTERS JUST CREATED SO WE DON'T TRY TO CREATE NEW FOR OTHER LW YET IN THIS LOOP
                                                 //nobreakHere
                                CreateMonsterBossToHarass(_LGuerrier, _LGuide, true);
                                if (SavedPreferences.xanaRandomMonster.toBool())
                                    CreateMonsterBossToHarass(_LGuerrier, _LGuide, false);
                                waitturns_before_newMonsters = 7; //4 trop peu apparemment - passé de 6 à 7 dans la 350
                            }
                        }
                    }
                }
            }
        }
    }

    public List<int> CheckBossesChances(LyokoGuerrier _LGuerrier, LyokoGuide _LGuide) {
        List<int> chancesVariety = new List<int>();
        int chancesMeduse = 0;
        int chancesXanafiedLw = 0;
        int chancesGardien = 0;
        int chancesClonePolymorphe = 0;

        if (VarG.meduseFonction.CouldSpawn()) {
            chancesMeduse += Mathf.Clamp(SavedPreferences.xanaScyphozoaXanafie.currentValue + SavedPreferences.xanaScyphozoaSteal.currentValue, 0, 3);
            if (SavedPreferences.xanaSectorAttacks.currentValue > 0)
                chancesMeduse++;
            if (_LGuerrier.IsAloneOnSector(false))
                chancesMeduse--;
            if (_LGuerrier.IsAloneOnSector(false))
                chancesMeduse++;
            if (_LGuide.nom == "aelita")
                chancesMeduse++;
            if (_LGuide.camp == Camps.franz)
                chancesXanafiedLw -= 3;
            if (_LGuide.GetTerritoire() == VarG.carthage)
                chancesMeduse++;
            if (!VarG.meduseFonction.CanBeUsed())
                chancesMeduse -= 3;
            if (_LGuide.nom == "franz")
                chancesMeduse -= 6;
        }
        LyokoGuerrier xanafiedLWToUse = LyokoGuerrierUtilities.GetRandomXanafiedLW_ToVirt();
        if (xanafiedLWToUse != null) {
            chancesXanafiedLw = VarG.xanaPocketliste.Count + 2;
            if (_LGuide.nom == "aelita")
                chancesXanafiedLw++;
            if (_LGuide.inTour) // as Xanawarrior can kick LW out of tower, chances to spawn them is increased
                chancesXanafiedLw++;
            if (_LGuide.camp == Camps.franz)
                chancesXanafiedLw += 2;

            //  chancesXanafiedLw += 999; //DEBUG
        }
        if (VarG.gardienFonction.CouldSpawn() && _LGuide.GetTerritoire() != VarG.carthage) {
            chancesGardien += SavedPreferences.xanaGuardianUse.currentValue;
            if (_LGuerrier.IsAloneOnSector(false))
                chancesGardien--;
            if (_LGuerrier.IsAloneOnSector(false))
                chancesGardien++;
        }
        if (Xana.CouldSpawnPolyClone()) {
            chancesClonePolymorphe += SavedPreferences.xanaPolymorphUse.currentValue + 1;
            if (_LGuerrier.nom != "aelita")
                chancesClonePolymorphe++;
        }
        chancesVariety.AddMany(chancesXanafiedLw, chancesMeduse, chancesGardien, chancesClonePolymorphe);
        //Debug.Log("UPDATE Panels");
        BossChancesPanelLw.UpdateAll(_LGuerrier, chancesXanafiedLw, chancesMeduse, chancesGardien, chancesClonePolymorphe);
        return chancesVariety;
    }
    public static bool CouldSpawnPolyClone() {
        return !Camps.xana.used_clonePoly_ForRandomAttack && SavedPreferences.xanaPolymorphUse.currentValue > 0;
    }
    public void CreateMonsterBossToHarass(LyokoGuerrier _LGuerrier, LyokoGuide _LGuide, bool areBosses = false) {
        if (_LGuerrier.controleXana || _LGuide.recyclable || _LGuide.paralysie)
            return;
        DebugXana("noMonster/Boss available -> create boss to harass " + _LGuide.nom);
        string nmTocreate = "";
        if (areBosses) {
            List<int> chancesVariety = CheckBossesChances(_LGuerrier, _LGuide);
            int chancesXanafiedLw = chancesVariety[0];
            int chancesMeduse = chancesVariety[1];
            int chancesGardien = chancesVariety[2];
            int chancesClonePolymorphe = chancesVariety[3];

            int chosenChanceValue = 0;
            for (int a = 0; a < chancesVariety.Count; a++) {
                int alea = Mathf.FloorToInt(UnityEngine.Random.Range(0, chancesVariety.Count));
                //DebugXana("aleaBoss",alea);
                if (alea < chancesVariety[a]) {
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
                Debug.Log("boss chosen: " + nmTocreate + " and confirmed");
            } else {
                nmTocreate = "";
                Debug.Log("boss chosen: " + nmTocreate + " CANCELLED because chances are too low");
                return;
            }
        }

        Territoire territoireCible = _LGuide.GetTerritoire();
        // VIRT DESTINATION
        Tour tour = null;
        List<Tour> listeToursP = new List<Tour>();
        if (territoireCible != VarG.carthage) {
            foreach (Tour tourTest in territoireCible.listeTours) {
                if (tourTest.IsActivatedBy("xana") || tourTest.IsActivatedBy("lyoko"))
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
            switch (_LGuide.carthagePos) {
                /* case CarthagePos.inTowerRoom:
                        tour = VarG.carthage.listeTours[0];
                        break;*/
                case CarthagePos.inDomeVoid:
                    CreateMonsters(null, Order.attackDomeVoid, 0, _LGuerrier.nom, CarthageAppearPointElement.Get_MonsterAppearPoint(_LGuide.HasCarthageElementProximity(VarG.carthageParam.southPole)));
                    return;
                //ne rien faire pour garageSkid, ce ne serait pas utile
                case CarthagePos.onDomeBridge:
                    CreateMonsters(null, Order.attackDomeBridge, 0, _LGuerrier.nom, VarG.carthageParam.domeBridge);
                    return;
                    //case CarthagePos.inCoreRoom:
                    //    CreateMonsters(null, Order.attackCore, 0, LG.nom, VarG.carthageParam.core);
                    //    return;
            }
        }
        if (tour == null) {
            Debug.LogWarning("Aucune tour trouvée pour y créer des monstres!");
            return;
        }
        if (areBosses) {
            Debug.Log("creating boss " + nmTocreate + " to harass");
            LyokoGuide LG1 = CreateMonstersSuite(CRDManager.c(territoireCible, tour.GetDisplayableID_perSector()), nmTocreate, 0, Order.harass, _LGuerrier.nom, null);
            if (LG1 != null && nmTocreate == "clone_polymorphe")
                used_clonePoly_ForRandomAttack = true;
        } else {
            //MONSTERS
            //dans le cas de carthage, les monstres sont créés avec les autres checks, sauf pour les persos dans les couloirs
            if (territoireCible != VarG.carthage) {
                DebugXana("creation monstre pour harceler, après ça normalement, y'a pas d'autres monstres qui devraient être créés pour harceler, sauf si territoires différents");
                CreateMonsters(tour, Order.harass, 0, _LGuerrier.nom);
            } else {
                //on ne créer pas les monstres du MAZE ici mais par une colision avec une specialeTile
                /*if (_LGuide.carthagePos == CarthagePos.inMaze) {
                    //s'assurer ici qu'il n'y a pas déjà trop de monstres dans le couloir
                    DebugXana("creation monstre pour héros dans couloir de carthage");
                    CreateMonsters(tour, Order.harass, 0, LG.nom);
                }*/
            }
        }
    }

    public bool ActiverTourRandom(bool notCarthage = true, bool delayXanaCode = false, bool withErrorMessage = false) {
        if (!HasTourActive()) {
            /*if (Territoire.GetBySectorID(choixSecteur) != null && Territoire.getByID(choixSecteur).tType == TerritoireType.carthage) {
                if (VarG.carthage.listeTours[0].activation != "xana") {
                    ActiverTour(VarG.carthage.listeTours[0].ID_universel, delayXanaCode);
                    return true;
                }
            } else {*/
            listeToursActivables = new List<int>();
            bool tourActivable = true;
            foreach (Tour tourTest in Tour.listeDispo) {
                if (notCarthage && tourTest.GetTerritoire() == VarG.carthage) {
                    tourActivable = false;
                } else {
                    tourActivable = true;
                    ///si Jérémie à une tour activée sur le supercalculateur en question, Xana ne va pas tenter d'y activer tour
                    if (Camps.jeremie.HasTourActive() && tourTest.ID_Supercalculateur == Tour.ReturnActiveTowers("jeremie")[0].ID_Supercalculateur) {
                        tourActivable = false;
                    } else {
                        if (tourTest.passage) {//on enlève celles de passage
                            tourActivable = false;
                        } else {
                            //on enlève celles dans lesquelles se trouve Aelita
                            if (VarG.aelita.IsVirt() && VarG.aelita.GetGuide().inTour && VarG.aelita.GetGuide().id_UNIV_tourEntrable == tourTest.ID_universel) {
                                tourActivable = false;
                            } else {
                                if (tourTest.IsKo()) {
                                    tourActivable = false;
                                } else {
                                    tourActivable = true;
                                    //puis on enlève aussi les tours proches des LG
                                    foreach (LyokoGuide lg in LyokoGuide.liste) {
                                        if (lg.id_UNIV_tourEntrable == tourTest.ID_universel) {
                                            tourActivable = false;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                if (tourActivable)
                    listeToursActivables.AddMany(tourTest.ID_universel);
            }
            //DebugXana("listeToursActivables : "+listeToursActivables);
            if (listeToursActivables.Count > 0) {
                ActiverTour(listeToursActivables[Mathf.FloorToInt(UnityEngine.Random.Range(0, listeToursActivables.Count))], delayXanaCode);
                xana.TInitiative.Stop(); //on désactive jusqu'à la prochaine réinit de Xana ou la prochaine desactivation de tour
                return true;
            }
        }
        /* } else {
             if (withErrorMessage)
                 MSG.Aff("tourutilisee", MSGcolor.grey);
             //DebugXana("xana ne peut activer une tour car elle en a déjà une d'active");
         }*/
        return false;
    }

    public void CreateMonsters_ForMaze(bool toDefendKey, Vector3 tileLocalPos) {
        List<LyokoGuerrier> LG_inMaze = new List<LyokoGuerrier>();
        foreach (LyokoGuerrier LG in LyokoGuerrier.liste) {
            if (LG.IsVirt() && !LG.controleXana && LG.GetGuide().GetTerritoire() == VarG.carthage &&
                !LG.GetGuide().VerifDevirt() && !LG.GetGuide().paralysie && !LG.GetGuide().T_chute.IsRunning() && !LG.IsUnconscious() && LG.GetGuide().carthagePos == CarthagePos.inMaze) {
                LG_inMaze.AddMany(LG);
            }
        }
        Order _order = Order.attackMaze;
        if (toDefendKey)
            _order = Order.attackKey;

        // TODO ajouter la possibilité de faire apparaitre un xanatifié ici?

        Debug.Log("tileLocalPos " + tileLocalPos);
        if ((SavedPreferences.xanaScyphozoaSteal.currentValue <= 0 && SavedPreferences.xanaScyphozoaXanafie.currentValue <= 0) || !VarG.meduseFonction.CanBeUsed() || VarG.meduseFonction.usedOncePerMaze || LG_inMaze.Count > 1) {
            if (toDefendKey) {
                CreateMonstersSuite(CRDManager.c(VarG.carthage, tileLocalPos), "rampant", DifficultyManager.level + Mathf.Clamp(LG_inMaze.Count, 0, 2), _order);
            } else {
                CreateMonstersSuite(CRDManager.c(VarG.carthage, tileLocalPos), "rampant", DifficultyManager.level, _order);
            }
        } else {
            CreateMonstersSuite(CRDManager.c(VarG.carthage, tileLocalPos), "meduse", 0, _order);
            VarG.meduseFonction.usedOncePerMaze = true;
        }
    }

    public void ResponseLwActivityInTower() {
        // DebugXana("ResponseLwActivityInTower");
        foreach (LyokoGuerrier LG in LyokoGuerrier.liste) {
            //si : il y a un échange
            //ou si il y a une réparation
            //ou si il y a un simplement un LG dedans et un decryptage en cours
            //tout cela exclu l'usage d'une tour pour un clone sur terre par exemple, que Xana ne va donc pas attaquer
            if (LG.IsMissionAvailable() && LG.IsVirt() && !LG.GetGuide().enCreationDestruction && LG.GetGuide().inTour && (LG.dna_isRepairing || LG.Dna_getState() == DnaModes.exchange || Tour.GetByUnivID(LG.GetGuide().id_UNIV_tourEntrable).IsActivatedBy("jeremie"))) {
                ///en cas de decryptage de journal
                if (ActionsF.journalDecrypt.TDecryptage.IsRunning() && ActionsF.journalDecrypt.graph.decryptageTaux > 6) {
                    if (waitturns_before_newMonsters_attaqueTour == 0) {
                        ActiverTour(ActionsF.journalDecrypt.tourDecryptage.ID_universel);
                        DoDefendTower(ActionsF.journalDecrypt.tourDecryptage, 1);
                        waitturns_before_newMonsters_attaqueTour = 3;
                    } else {
                        DebugXana(">>>>>>>>>>>>>>attendre encore un peu avant d'envoyer des monstres sur la tour");
                        waitturns_before_newMonsters_attaqueTour--;
                    }
                } else {
                    ///dans tout les autres cas (LG présents dans tour activée, en reparation ADN, etc...
                    if (waitturns_before_newMonsters_attaqueTour == 0) {
                        DebugXana(">>>>>>>>>>>>>>DoTowerAttack");
                        DoTowerAttack(LG.GetGuide().id_UNIV_tourEntrable, 0);
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

    public void ResponseHopperTower() {
        if (Tour.isThereActive("franz")) {
            DoTowerAttack(Tour.ReturnActiveTowersInt("franz")[0]);
        }
    }

    public void ResponseTowerActivation() {
        //si une tour est activée par Hopper, il va tenter de la détruire
        if (Tour.isThereActive("franz")) {
            DoTowerAttack(Tour.ReturnActiveTowersInt("franz")[0], 0);
        }
        if (Tour.isThereActive("xana")) {
            DoDefendTower(Tour.ReturnActiveTowers("xana")[0]);
            if (!VarG.ModeMission || MissionGestion.ID == 3) {
                if (VarG.scFrance.seuilDegats < 100) {
                    DebugXana("Create/TryReinforce Atk: "+ chosenAttack);
                    if (chosenAttack != TowerTypeAttack.nothing) {
                        Tour.ReturnActiveTowers("xana")[0].CreateReinforceAtk(Camps.xana, chosenAttack);
                    } else {
                        /////////////////////////////////////////////////
                        TowerTypeAttack attaqueChoisie = TowerTypeAttack.nothing;

                        int alea;
                        List<LevelPreference> listSP = new List<LevelPreference>();
                        foreach (LevelPreference lp in SavedPreferences.attackPrefList) {
                            alea = UnityEngine.Random.Range(0, 11);//entre 0 & 10
                            if (alea < lp.currentValue * 2) {
                                listSP.Add(lp);
                                //Debug.Log("add: " + lp.nom);
                            }
                        }

                        //Log("SavedPreferences.attackPrefList: " + SavedPreferences.attackPrefList.Count);

                        if (listSP.Count <= 0) {
                            //aucune attaque n'a été prise, on les prend donc toutes direct sans faire de distinction
                            foreach (LevelPreference lp in SavedPreferences.attackPrefList) {
                                if (lp.currentValue > 0) {
                                    listSP.Add(lp);
                                    // Debug.Log("add_basic: " + lp.ToString());
                                }
                            }
                        }

                        //Debug.Log(">>>>>>>listSP.Count " + listSP.Count);
                        //Debug.Log(">>>>>>> " + listSP[UnityEngine.Random.Range(0, listSP.Count - 1)].nom);
                        switch (listSP[UnityEngine.Random.Range(0, listSP.Count)].nom) {// - 1 inutile count
                            case "xanaHumanXanatif":
                                attaqueChoisie = TowerTypeAttack.HumanXanatif;
                                break;
                            case "xanaNonHumanXanatif":
                                attaqueChoisie = TowerTypeAttack.NonHumanXanatif;
                                break;
                            case "xanaMeteore":
                                attaqueChoisie = TowerTypeAttack.Meteore;
                                break;
                            case "xanaPowerPlantOverload":
                                attaqueChoisie = TowerTypeAttack.PowerPlantOverload;
                                break;
                            case "xanaElectricSphere":
                                attaqueChoisie = TowerTypeAttack.ElectricSphere;
                                break;
                            case "xanaWeatherDisaster":
                                attaqueChoisie = TowerTypeAttack.WeatherDisaster;
                                break;
                                //TODO
                            case "xanaSuicideBus":
                                //attaqueChoisie = TowerTypeAttack.SuicideBus;
                                break;
                        }
                        if (attaqueChoisie == TowerTypeAttack.nothing) {
                            Debug.Log("Editor Mode >> no any attack selected >> fallback on xanaHumanXanatif");
                            //là en généralement, si il n'y a rien (malgré les protections du menu qui t'empêchent d'avoir 0 type d'attaque de choisies, c'est parce que on est en mode éditeur
                            //donc on choisi arbitrairement la xanaHuman
                            attaqueChoisie = TowerTypeAttack.HumanXanatif;
                        }
                        Tour.ReturnActiveTowers("xana")[0].CreateReinforceAtk(Camps.xana, attaqueChoisie);
                    }
                    chosenAttack = TowerTypeAttack.nothing;
                }
            }
        }
    }

    public bool HasAttackOfType(TowerTypeAttack t) {
        return Tour.ReturnActiveTowers("xana").Count > 0 && Tour.ReturnActiveTowers("xana")[0].attaqueTour!=null && Tour.ReturnActiveTowers("xana")[0].attaqueTour.typeAttaque == t;
    }

    public void DoTowerAttack(int numTour, int ID_attaque = 0) {
        if (numTour == -1) {
            Debug.LogError("trying tower attack on tower of ID -1, targetted LG doesn't have the right univID proximity");
            return;
        }
        //si il y a déjà des monstres sur le territoire ET qu'ils ne sont pas occupés, on les envoie attaquer la tour.
        //sinon, on créer de nouveaux monstres
        //Dès lors qu'il y a moins de 1 monstre, on en renvoie

        Territoire secteurR = Tour.GetByUnivID(numTour).GetTerritoire();
        if (!IsMonsterAvailableFor(secteurR, Order.attackTower)) {
            DebugXana("pas de monstres dispo pour attaquer tour " + numTour + " sur le territoire " + secteurR.GetTranslatedName());
            //Tour random dafuck??? >> TO DELETE

            /*int randomTour = (Mathf.FloorToInt(UnityEngine.Random.Range(0,10)));/:entre 0 & 9
            int randomTour2 = (Mathf.FloorToInt(UnityEngine.Random.Range(0,10)));
            Tour tourChoice = secteurR.listeTours[randomTour];
            Tour tourChoice2 = secteurR.listeTours[randomTour2];*/
            CreateMonsters(Tour.GetByUnivID(numTour), Order.attackTower, ID_attaque, "");
        }
        foreach (LyokoGuide LGMonstre in LyokoGuide.liste) {
            if (LGMonstre.camp == Camps.xana && LGMonstre.GetTerritoire() == secteurR) {
                //DebugXana(LGMonstre.TourActiveProche()," ",LGMonstre.timerDoTowerAttack.running," ",LGMonstre.DoDefendTower," ",LGMonstre.harcelerLG);
                if (LGMonstre.TourActiveProche() || LGMonstre.savedOrderType == Order.defendTower || LGMonstre.savedOrderType == Order.attackCore || LGMonstre.harcelerWhichLG != "") {
                    DebugXana(LGMonstre.nom + " ne va pas attaquer pas car occupé");
                } else {
                    Suite0(numTour, LGMonstre);
                }
            }
        }
    }

    private void Suite0(int numTour, LyokoGuide LGMonstre) {
        LGMonstre.GiveTowerAttackOrder(numTour);
    }

    public void DoDefendTower(Tour tour = null, int ordreSpecial = 0) {
        DebugXana("lance la défense de la tour");
        int monstres_defendant_DEJA_tour = LyokoGuideUtilities.GetTotalLG_DefendantTour(tour.ID_universel, LgTypes.Monstre, Camps.xana);
        if (monstres_defendant_DEJA_tour <= 1) {
            if (!IsMonsterAvailableFor(tour.GetTerritoire(), Order.defendTower)) {
                //on ralenti la création des monstres pour défendre la tour.
                DebugXana("!!!!!!!!!!!!!!défendreTour - aucun monstre dispo pour défendre de la tour, on en créer de nouveaux si on le peut");
                if (tour.turnsBefore_Redefending <= 0) {
                    CreateMonsters(tour, Order.defendTower, ordreSpecial, "");
                    tour.turnsBefore_Redefending = 2;
                } else {
                    tour.turnsBefore_Redefending--;
                }
            } else {
                DebugXana("!!!!!!!!!!!!!!défendreTour - il y a des monstres dispo pour défendre de la tour, on les utilise si il y a 1 seul défenseur de la tour ou moins");
                foreach (LyokoGuide LGMonstre in LyokoGuide.liste) {
                    if (monstres_defendant_DEJA_tour <= 1) {
                        if (LGMonstre.savedOrderType != Order.defendTower && LGMonstre.savedOrderType != Order.attackTower && LGMonstre.savedOrderType != Order.attackSkidInGarage && LGMonstre.savedOrderType != Order.attackCore && LGMonstre.camp == Camps.xana && LGMonstre.GetTerritoire() == tour.GetTerritoire()) {
                            LGMonstre.GiveTowerDefenseOrder(tour.ID_universel);
                            monstres_defendant_DEJA_tour++;
                        }
                    } else {
                        break;
                    }
                }
            }
        } else {
            DebugXana("inutile, xana à déjà assez de défenseurs de la tour");
        }
    }

    public bool IsMonsterAvailableFor(Territoire territoire, Order _order = Order.nothing) {
        bool pasBesoin_NouveauMonstre = false;
        //DebugXana("checkIsMonstresDispo",territoire.Nom,forWhat);
        /*foreach (LyokoGuide LGM in LyokoGuide.liste) {
            DebugXana("CHECK", LGM.nom, LGM.camp.ToString(), LGM.harcelerLG.ToString());
        }*/
        foreach (LyokoGuide LGMonstre in LyokoGuide.liste) {
            //priorité aux attaque et défense, à égalité, puis vient le harcèlement
            if (LGMonstre.nestedIN == null && !LGMonstre.T_chute.IsRunning() && !LGMonstre.recyclable && LGMonstre.camp == Camps.xana && LGMonstre.GetTerritoire() == territoire) {
                switch (_order) {
                    case Order.harass:
                        if (LGMonstre.nom != "meduse" && LGMonstre.nom != "gardien") {
                            //note : si il y a des monstres en attaque ou en défense, on ne rajoute pas de Xana monstres pour harceler
                            //qu'ils n'aient rien à faire, qu'ils attaquent/défendent une tour ou un emplacement, rien n'empêche les monstres d'aller harceler
                            /*if (!LGMonstre.attaquerCoeur) {
                                    pasBesoin_NouveauMonstre = true;
                                    break;
                                }*/
                            if (LGMonstre.harcelerWhichLG != "") {
                                pasBesoin_NouveauMonstre = true;
                                break;
                            }
                        }
                        break;
                    case Order.attackTower:
                        if (LGMonstre.savedOrderType == Order.attackTower) {
                            pasBesoin_NouveauMonstre = true;
                            break;
                        }
                        break;
                    case Order.defendTower:
                        if (LGMonstre.savedOrderType == Order.defendTower) {
                            DebugXana("MonstrePour défendre la tour existe, il s'agit d'un", LGMonstre.nom);
                            pasBesoin_NouveauMonstre = true;
                            break;
                        }
                        break;
                    case Order.attackCore:
                        if (LGMonstre.savedOrderType == Order.attackCore) {
                            pasBesoin_NouveauMonstre = true;
                            break;
                        }
                        break;
                    //------------------cas particuliers qui suivents :
                    //ils sont appellés pour défendre, mais iront directement harceler,
                    //avec les modifs, peut être ceux du dessus également
                    //le but est de savoir si ils sont toujours dans les environs pour défendre

                    case Order.attackDomeBridge:
                        if (LGMonstre.carthagePos == CarthagePos.onDomeBridge) {
                            pasBesoin_NouveauMonstre = true;
                            break;
                        }
                        break;
                    case Order.attackDomeVoid:
                        if (LGMonstre.carthagePos == CarthagePos.inDomeVoid && LGMonstre.nom == "manta") {
                            pasBesoin_NouveauMonstre = true;
                            break;
                        }
                        break;
                }
            }
        }
        return pasBesoin_NouveauMonstre;
    }


    public void CreateMonsters(Tour tour, Order ordre = Order.nothing, int ordreSpecial = 0, string LGharcelement = "", LyokoElement specialCarthageElement = null) {

        ///dans le cas du harcelement, les coordonnées de la tour ont déjà été modifiées et sont déjà considérées comme des coordonnées (mais elles devraient plutôt être traitées ici )

        //pour l'instant il manque peut être de déplacer la création de boss ci dessus DANS le système "CreateMonsters"

        //le concept, c'est que la difficulté ne doit pas influer sur le nombre max de monstres mais sur la difficulté pour les BATTRE (leur résistance)
        //tout les types de monstres doivent être accessibles quelque soit la difficulté, et ils doivent être en grand nombre pour le plaisir des casual et des hardcores gamers
        Territoire sector;
        CRDManager crd;
        if (specialCarthageElement != null) {
            sector = VarG.carthage; //variation pour replika carthage à rajouter
            crd = CRDManager.c(sector, specialCarthageElement.transform.localPosition); //place we look for, a mettre en transform dans les paramètres
                                                                                        //on rechange ça encore une fois après coup si c'est une manta (qui nait sur les bords)
                                                                                        //idem pour les attaques dans le garage skid
                                                                                        //idem pour la meduse qui apparait pour Drainer
                                                                                        ///////////////// A REMPLACER /////////////////////////
        } else {
            sector = tour.GetTerritoire();
            crd = CRDManager.c(sector, tour.GetDisplayableID_perSector_INT());
        }
        if (ordre == Order.attackTower) {
            //on change les coordonnées pour une autre tour au pif sur le territoire:
            List<Tour> listeToursP = new List<Tour>();
            if (sector != VarG.carthage && sector != VarG.replika_carthage) {
                foreach (Tour tourTest in sector.listeTours) {
                    if (tourTest.IsActivatedBy("xana") || tourTest.IsActivatedBy("lyoko")) {
                        listeToursP.Add(tourTest);
                    }
                }
                if (listeToursP.Count <= 0) {
                    Debug.LogError("Aucune tour disponible dans le territoire - erreur!");
                } else {
                    int a = UnityEngine.Random.Range(0, listeToursP.Count);// - 1 inutile pour le count
                    crd = CRDManager.c(sector, listeToursP[a].GetDisplayableID_perSector());
                    Debug.Log("tour random définie pour la virtualisation du monstre : " + listeToursP[a].ID_universel);
                }
            } else {
                crd = CRDManager.c(sector, 1);
            }
        }
        LyokoGuide LG;
        int nbCopies = 0;
        string refMonstre = "kankrelat";

        List<string> m = new List<string>();
        switch (ordreSpecial) {
            case 0:
                // KOLOSSE : son appel devra être identique à la Méduse ou au Gardien, des conditions très spécifiques non traitées dans cette partie du code donc
                // ces nombres doivent être les mêmes que pour la division possible en groupe (voir le code bien au dessus avec grouping)
                if (ordre == Order.attackTower) {
                    refMonstre = "megatank";
                    nbCopies = 1;
                } else {
                    if (sector.tType == TerritoireType.surface) {
                        m.AddMany("kankrelat", "block", "krabe", "frolion", "tarentule", "megatank", "manta");
                        refMonstre = RandomMonster(m);
                    }
                    if (sector.tType == TerritoireType.carthage) {
                        m.AddMany("rampant", "manta");
                        if (ordre == Order.attackDomeVoid) {
                            //si c'est au dessus du void, on force la manta et on change la position après coup
                            refMonstre = "manta";
                        } else {
                            refMonstre = RandomMonster(m); //rampant ou manta. si c'est la manta, sa position sera changée automatiquement après coup
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
                    if (ordre == Order.defendTower) {
                        nbCopies += 1;
                    }
                    if (ordre == Order.attackSkidInGarage && nbCopies == 2) {
                        nbCopies = UnityEngine.Random.Range(0, 2); //pas de groupes de plus de 2, donc 0 ou 1 copies avec ce random
                    }
                }
                DebugXana("creerMonstre " + refMonstre + " at " + sector.GetTranslatedName() + ", nbCopies " + nbCopies);

                //si c'est dans le maze, on est directement passé par CreateMonstersSuite avant ça
                //si c'est une manta, faire apparaitre via FX
                if (specialCarthageElement != null) {
                    LG = CreateMonstersSuite(crd, refMonstre, nbCopies, ordre, LGharcelement);
                } else {
                    LG = CreateMonstersSuite(crd, refMonstre, nbCopies, ordre, LGharcelement, tour);
                }
                break;
            case 1: //défense de tour spéciale PAR POLYMORPHE
                if (ordre != Order.defendTower) {
                    Debug.LogWarning("erreur : envoyer des monstres pour attaquer la tour, et qui ne corresponde pas à des megatanks de toute façon...");
                }
                // "défense" d'une tour avec des clones polymorphes et tarentules, 
                // en effet, xana essaye de prendre le contrôle de la tour, pas de la détruire
                if (!Camps.xana.used_clonePoly_ParDecryptage && SavedPreferences.xanaPolymorphUse.currentValue > 0) {
                    LG = CreateMonstersSuite(crd, "clone_polymorphe", 0, ordre, LGharcelement, tour);
                    if (LG != null) {
                        Camps.xana.used_clonePoly_ParDecryptage = true;
                        if (DifficultyManager.level == 0) {
                            LG.DoPolymorph("yumi");
                        } else if (DifficultyManager.level == 1) {
                            LG.DoPolymorph("odd");
                        } else if (DifficultyManager.level == 2) {
                            LG.DoPolymorph("ulrich");
                        }
                    }
                } else {
                    if (sector.tType == TerritoireType.carthage) {
                        m.AddMany("rampant", "manta");
                        refMonstre = RandomMonster(m);
                        if (refMonstre == "rampant") {
                            LG = CreateMonstersSuite(crd, refMonstre, 2, ordre, LGharcelement, tour);
                        } else {
                            LG = CreateMonstersSuite(crd, refMonstre, 1, ordre, LGharcelement, tour);
                        }
                    } else {
                        m.AddMany("kankrelat", "tarentule");
                        refMonstre = RandomMonster(m);
                        if (refMonstre == "kankrelat") {
                            LG = CreateMonstersSuite(crd, refMonstre, 2, ordre, LGharcelement, tour);
                        } else {
                            LG = CreateMonstersSuite(crd, refMonstre, 1, ordre, LGharcelement, tour);
                        }
                    }
                }
                break;
            case 5: //scyphozoa on skidbladnir in garage Skid
                CreateMonstersSuite(crd, "meduse", 0, ordre, LGharcelement, tour);
                break;
        }
    }

    private LyokoGuide CreateMonstersSuite(CRDManager crd = null, string nomMonstre = "", int nbCopies = 0, Order ordre = Order.nothing, string LGharcelement = "", Tour tourAttackDefendTarget = null) {
        bool continuer = false;
        if (tourAttackDefendTarget == VarG.carthage.listeTours[0]) {
            Debug.Log("CreateMonsters_TYPE0");
            //crd = CRDManager.c("carthage", VarG.carthageParam.towerCarthage.transform.localPosition+(Vector3.up*10)+(Vector3.back * 15)); //atkcarthagetower
            crd = CRDManager.c("carthage", VarG.carthageParam.towerCarthage.GetCrdToVirt());
        }
        if (ordre == Order.attackSkidInGarage) {
            Debug.Log("CreateMonsters_GARAGE");
            crd = CRDManager.c("carthage", CarthageAppearPointElement.Get_MonsterGarageAppearPoint().transform.localPosition);
        }
        if ((ordre == Order.attackDomeBridge || ordre == Order.attackDomeVoid) && nomMonstre == "manta") {
            //REPOSITION BIRTH OVER VOID IF MANTA
            Debug.Log("CreateMonsters_TYPE1");
            crd = CRDManager.c("carthage", CarthageAppearPointElement.Get_MonsterAppearPoint(false).transform.localPosition);
        }
        if (ordre == Order.attackCore && nomMonstre == "manta") {
            Debug.Log("CreateMonsters_TYPE2");
            crd = CRDManager.c("carthage", CarthageAppearPointElement.Get_MonsterAppearPoint(true).transform.localPosition);
        }
        if (ordre == Order.drainSkidInGarage) {
            Debug.Log("CreateMeduseDrainSkid");
            crd = CRDManager.c("carthage", VarG.carthageParam.skidbladnirAnchor.transform.localPosition + (Vector3.up * 10));
        }
        if (ordre == Order.drainCore) {
            Debug.Log("CreateMeduseDrainCore");
            crd = CRDManager.c("carthage", VarG.carthageParam.corePlane.transform.localPosition + (Vector3.up * 10));
        }
        if (ordre == Order.attackCore && nomMonstre == "rampant") {
            Debug.Log("CreateMonsters_TYPE3");
            crd = CRDManager.c("carthage", VarG.carthageParam.coreDown.transform.localPosition);
        }
        if (crd == null)
            Debug.LogError("NULL CRD on creating monsters");

        if (nbCopies == 0) {
            if (LyokoGuideUtilities.GetTotalLGtype_OnSector(crd.GetTerritoire(), LgTypes.Monstre) + 1 + nbCopies <= 6)
                continuer = true;
        } else {
            for (int a = nbCopies; a > 0; a--) {
                if (LyokoGuideUtilities.GetTotalLGtype_OnSector(crd.GetTerritoire(), LgTypes.Monstre) + 1 + a <= 6) {
                    continuer = true;
                    break;
                } else {
                    DebugXana("reduction de nombre de copies: " + nbCopies);
                }
            }
        }
        if (ordre != Order.attackKey && ordre != Order.attackMaze && !continuer) {
            Debug.Log("Nombre max de monstre par territoire surpassé >> annulation de la création du/des monstre, déjà " + (LyokoGuideUtilities.GetTotalLGtype_OnSector(crd.GetTerritoire(), LgTypes.Monstre) + 1) + " présents");
            return null;
        } else {
            if (crd.GetTerritoire().GetStatus() == VirtualBuildStatus.inDestruction) {
                Debug.Log("creation de monstres par Xana annulée: territoire en destruction");
                return null;
            }
            LyokoGuide lg = LyokoGuide.Create(nomMonstre, crd, nbCopies);

            DebugXana("initMonstre" + lg.nom + " " + ordre + " " + crd);// + " " + crd.GetTour().ID_de_secteur + " " + LGharcelement);
            DebugXana("ordre de" + " " + lg.nom + " " + "envoyé :" + " " + ordre.ToString());
            if (ordre == Order.attackTower) {
                lg.GiveTowerAttackOrder(tourAttackDefendTarget.ID_universel);
            }
            if (ordre == Order.defendTower) {
                lg.GiveTowerDefenseOrder(tourAttackDefendTarget.ID_universel);
            }
            if (ordre == Order.harass) {
                lg.GiveHarassOrder(LGharcelement);
            }
            if (ordre == Order.attackMaze) {
                lg.GiveCarthageOrder(Order.attackMaze);
                lg.carthagePos = CarthagePos.inMaze; //permet d'éviter les bugs de comabt
                lg.SetupVol(false);//force le non vol pour la chute sur le terrain
            }
            if (tourAttackDefendTarget == VarG.carthage.listeTours[0]) {
                lg.SetupVol(true); //force le vol pour la chute efficace
            }
            if (ordre == Order.drainSkidInGarage) {
                lg.SetupVol(true);
                lg.AI.DoFreezeInMidAir(true);
                lg.EnableAI(false);
                lg.AnnulerChute();
                lg.GiveCarthageOrder(Order.drainSkidInGarage);
                lg.carthagePos = CarthagePos.inGarageSkid;
            }
            if (ordre == Order.attackKey) {
                lg.GiveCarthageOrder(Order.attackKey);
                lg.SetupVol(false);//force le non vol pour la chute sur le terrain
            }
            if (ordre == Order.attackDomeBridge) {
                if (lg.nom == "manta") {
                    Fx_Vmap.create(FxType.mantaBirth, lg.transform.localPosition, VarG.carthage, lg.gameObject.layer);
                    lg.carthagePos = CarthagePos.inDomeVoid;
                } else {
                    lg.carthagePos = CarthagePos.onDomeBridge;
                }
                // On harcèle direct dans ce cas ci, si possible
                if (LGharcelement != "") {
                    lg.GiveHarassOrder(LGharcelement);
                } else {
                    lg.UpdateCarthageObjective(VarG.carthageParam.domeBridge);
                    lg.GiveCarthageOrder(Order.attackDomeBridge);
                }
            }
            if (ordre == Order.attackDomeVoid) {
                if (lg.nom == "manta") {
                    Fx_Vmap.create(FxType.mantaBirth, lg.transform.localPosition, VarG.carthage, lg.gameObject.layer);
                    lg.carthagePos = CarthagePos.inDomeVoid;
                } else {
                    lg.carthagePos = CarthagePos.onDomeBridge;
                }
                // On harcèle direct dans ce cas ci, si possible
                if (LGharcelement != "") {
                    lg.GiveHarassOrder(LGharcelement);
                } else {
                    if (lg.nom == "manta")
                        lg.UpdateCarthageObjective(VarG.carthageParam.domeVoid);
                    lg.GiveCarthageOrder(Order.attackDomeVoid);
                }
            }
            if (ordre == Order.attackSkidInGarage) {
                lg.GiveCarthageOrder(Order.attackSkidInGarage);
                lg.carthagePos = CarthagePos.inGarageSkid;
            }
            if (ordre == Order.attackCore) {
                if (lg.nom == "manta") {
                    Fx_Vmap.create(FxType.mantaBirth, lg.transform.localPosition, VarG.carthage, lg.gameObject.layer);
                    lg.carthagePos = CarthagePos.inDomeVoid;
                } else {
                    lg.carthagePos = CarthagePos.inCoreRoom;
                }
                lg.GiveCarthageOrder(Order.attackCore);
            }
            return lg;
        }
    }

    public void ResetClonePolyUsage() {
        Debug.Log("ResetClonePolyUsage resetted on new decyphering");
        used_clonePoly_ParDecryptage = false;
    }

    public string RandomMonster(List<string> listeMonstres) {
        if (listeMonstres.Count == 0) {
            Debug.LogError("noMonsterAvailableInRandomization!");
        }
        int hasard = UnityEngine.Random.Range(0, listeMonstres.Count);
        /*for (int a = 0; a < 100; a++) {
            Debug.Log("testHasard : "+ listeMonstres[UnityEngine.Random.Range(0, listeMonstres.Count)]);
        }*/
        return listeMonstres[hasard];
    }

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
    }

    public void RameuterMonstres(LyokoGuide a) {
        DebugXana("le monstre appelle les autres monstres présents sur le territoire");
        foreach (LyokoGuide LGMonstre in LyokoGuide.liste) {
            //on verifie que le monstre testé soit de Xana, qu'il ne soit pas celui qui appelle mais qu'il soit sur le même territoire
            if (LGMonstre.camp == Camps.xana && LGMonstre != a && LGMonstre.GetTerritoire() == a.GetTerritoire() && LGMonstre.nom != "gardien" && LGMonstre.nom != "meduse") {
                if (!LGMonstre.inBattle) {
                    if (LGMonstre.TourActiveProche() || LGMonstre.savedOrderType == Order.attackTower || LGMonstre.savedOrderType == Order.defendTower) {
                        DebugXana(LGMonstre.nom + " ne bouge pas car près d'une tour activée ou à déjà pour ordre d'en attaquer une");
                    } else {
                        LGMonstre.SetObjectif(a);
                    }
                }
            }
        }
    }

    public static void OnVirt_ManipDna(string nomScan, string lastScan) { //une seule manip ADN possible ?
        if (nomScan != "" && lastScan != "" && nomScan != lastScan && LyokoGuerrier.GetByName(nomScan).Dna_getState() == DnaModes.nothing) {
            if (UnityEngine.Random.Range(0, 11) < SavedPreferences.xanaDnaManipulation.currentValue * 2.5f) {
                if (LyokoGuerrier.codeTerreObligatoire && UnityEngine.Random.Range(0, 11) < SavedPreferences.xanaDnaManipulation.currentValue * 1.5f && nomScan == "aelita") {
                    VarG.aelita.Dna_SetState(DnaModes.bugEarthCode);
                } else {
                    if (LyokoGuerrier.GetByName(lastScan).Dna_getState() == DnaModes.nothing) {
                        //for (int a = 0; a < 100; a++) {
                        //   Debug.Log("UnityEngine.Random.Range(0,2)  " + UnityEngine.Random.Range(0, 2));
                        //}
                        if (UnityEngine.Random.Range(0, 2) == 0) {//2 est exclus
                            LyokoGuerrierUtilities.Dna_SetStateMultiple(DnaModes.confusion, LyokoGuerrier.GetByName(nomScan), LyokoGuerrier.GetByName(lastScan));
                        } else {
                            LyokoGuerrierUtilities.Dna_SetStateMultiple(DnaModes.melange, LyokoGuerrier.GetByName(nomScan), LyokoGuerrier.GetByName(lastScan));
                        }
                    }
                }
            }
        }
    }


    public void OnTowerActivationReaction(Tour t) {
        //ACTIONS spécifiques à Xana juste après l'activation d'une tour

        //on verifie si les lyokoguerriers sont virtualisés et on provoque un effet aléatoire de modification ADN, selon les paramètres de la partie
        //annulé, la modif adn n'a lieu que lors de la virtualisation (et en plus celle d'ici est peut être bugguée sur les setmultiple
        /*if (SavedPreferences.xanaDnaManipulation.currentValue > 0) {
            List<LyokoGuerrier> nbVirtLGuerrier = new List<LyokoGuerrier>();
            foreach (LyokoGuerrier LGn in LyokoGuerrier.liste) {
                if (LGn.IsVirt() && !LGn.isFront()) nbVirtLGuerrier.Add(LGn);
            }
            if (nbVirtLGuerrier.Count >= 1) {
                //on utilise l'index et le nbVirtLGuerrier pour rajouter le statut mélange à un autre lyokoguerrier si il y en a un qui tombe dessus
                uint index = 0;
                foreach (LyokoGuerrier LG in nbVirtLGuerrier) {
                    if (LG.Dna_getState() == DnaModes.nothing) {
                        int alea = Mathf.FloorToInt(UnityEngine.Random.Range(0, 11));
                        bool cancel = false;
                        // a n'activer que si on est en mode -code terre-
                        Debug.Log("<color=yellow> aela ADN xanaManip : " + alea + " on " + LG.nom + "</color>");
                        if (alea < SavedPreferences.xanaDnaManipulation.currentValue * 2f && LG.nom == "aelita" && LyokoGuerrier.codeTerreObligatoire) {
                            LG.Dna_SetState(DnaModes.bugEarthCode);
                        } else {
                            if (nbVirtLGuerrier.Count >= 2) {
                                if (alea < SavedPreferences.xanaDnaManipulation.currentValue * 2f) {
                                    //si il n'y a AUCUN lyokoguerrier avec adn mélangé, on peut faire un couple
                                    foreach (LyokoGuerrier Lgg in LyokoGuerrier.liste) {
                                        if (Lgg.Dna_getState() == DnaModes.melange || Lgg.Dna_getState() == DnaModes.confusion) {
                                            cancel = true;
                                            break;
                                        }
                                    }
                                    if (!cancel) {
                                        //on inflige melange ou confusion, mais seulement si on trouve un autre lyokoguerrier virtualisé et sans ADN changé
                                        foreach (LyokoGuerrier LGn2 in nbVirtLGuerrier) {
                                            if (LGn2 != LG && LGn2.Dna_getState() == DnaModes.nothing) {
                                                if (UnityEngine.Random.value >.5f) {
                                                    LyokoGuerrier.dna_setStateMultiple(DnaModes.confusion, LG, LGn2);
                                                } else {
                                                    LyokoGuerrier.dna_setStateMultiple(DnaModes.melange, LG, LGn2);
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
        ResponseTowerActivation(); //on force la défense directe de la tour qui vient d'être activée (sans attendre le timer d'initiative de xana)
    }

}