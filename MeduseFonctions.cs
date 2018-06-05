using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum MedusePower { None, VolAdn, Xanatifier, kill, DrainSkid, DrainCore };

//le vol ADN doit être considéré comme un vol des clefs de Lyoko dans le cas d'Aelita et d'un Xana emprisonné sur lyoko

public class MeduseFonctions : CreatureUnique
{
    public MainTimer T_UsePower = new MainTimer(0.01f);
    public MedusePower modePouvoir = MedusePower.None;
    public List<int> vitessePouvoir = new List<int>();
    public bool usedOncePerMaze = false;
    public EnergieNeeder drainMeduse = new EnergieNeeder(DepensesEnergy.drain, 0);
    //public bool forceXanatification=false;
    //public bool forcevolAdnAdn=false;
    public LyokoGuerrier LGunderControl = null;
    //la meduse ne peut être détruite que lorsqu'elle utilise son pouvoir. Sur le Skidbladnir ou les lyokoguerriers
    public MeduseFonctions() {
        nom = "meduse";
        vitessePouvoir.Clear();
        vitessePouvoir.AddMany(7, 10, 15); //increased en 350
    }
    public void OnSkidDemater() {
        if (this.created && modePouvoir == MedusePower.DrainSkid && T_UsePower.IsRunning()) {
            LyokoGuide lg = LyokoGuide.GetByID(LyokoGuide.IdentifierGuide("meduse"));
            if (lg != null)
                lg.Devirtualiser();
        }
    }
    public bool CouldSpawnToDrain() {
        return !created && CanBeUsed();
    }
    public override bool CouldSpawn() {
        return !created && CanBeUsed() && (SavedPreferences.xanaScyphozoaXanafie.currentValue > 0 || SavedPreferences.xanaScyphozoaSteal.currentValue > 0);
    }
    public bool IsUsingItsPower() {
        return T_UsePower.IsRunning();
    }
    public void UpdateTimersFast() {
        if (T_UsePower.incrementReach())
            UsePower();
    }
    public void RAZ(bool depuisDevirt = false) { // Debug.Log("razMeduse");
        T_UsePower.Stop();
        if (depuisDevirt)
            razClassique();
        StopPower();
        modePouvoir = MedusePower.None;
        razMinimum();
    }
    public void SetToDrainingMode(MedusePower drainMode) {
        if (modePouvoir == MedusePower.None) {
            modePouvoir = drainMode;
            Camps.jeremie.energyManager.SendTo(drainMeduse, DepensesEnergy.drain.maintien, false, true);
            ActionsF.energieStat.Execution(false);
            if (drainMode == MedusePower.DrainCore) {
                ActionsAnomalie.ExecutionMultiple("coreIsUnderAttack");
            } else {
                ActionsAnomalie.ExecutionMultiple("skidIsUnderAttack");
            }
            //ActionsF.energieStat.graph.DisplayEnergyUse();
            T_UsePower.Start();
            Debug.Log("StartPower " + drainMode);
        } else {
            Debug.LogWarning("There's already a scyphoza mode enabled " + modePouvoir);
        }
    }
    public bool TrySetLGcontrol(LyokoGuide LG) {

        if (LG.nom == "franz" || !LyokoGuerrier.IsLyokoGuerrier(LG.nom) || LyokoGuide.IdentifierGuide("meduse") == 999 || LyokoGuerrier.GetByName(LG.nom).controleXana)
            return false;

        LGunderControl = LyokoGuerrier.GetByName(LG.nom);

        // TODO volClefs dans le cas d'Aelita ayant les Clefs et Xana enfermée sur Lyoko

        if (SavedPreferences.xanaScyphozoaXanafie.currentValue > 0 && SavedPreferences.xanaScyphozoaSteal.currentValue > 0) {
            //si le lyokoguerrier est déjà en dévirt mortelle, vu que la xanatification est possible, on choisit automatiquement celle là
            if (LyokoGuerrier.GetByName(LG.nom).HasDevirtMortelle()) {
                modePouvoir = MedusePower.Xanatifier;
            } else {
                //quelle option à le plus de chances ? on teste
                if (SavedPreferences.xanaScyphozoaSteal.currentValue == SavedPreferences.xanaScyphozoaXanafie.currentValue) {
                    //si les valeurs sont égales
                    bool _testB = UnityEngine.Random.value > 0.5f;
                    if (_testB) {
                        modePouvoir = MedusePower.VolAdn;
                    } else {
                        modePouvoir = MedusePower.Xanatifier;
                    }
                } else {
                    //si les valeurs sont inégales
                    LevelPreference highestPref;
                    LevelPreference lowestPref;
                    if (SavedPreferences.xanaScyphozoaSteal.currentValue > SavedPreferences.xanaScyphozoaXanafie.currentValue) {
                        highestPref = SavedPreferences.xanaScyphozoaSteal;
                        lowestPref = SavedPreferences.xanaScyphozoaXanafie;
                    } else {
                        highestPref = SavedPreferences.xanaScyphozoaXanafie;
                        lowestPref = SavedPreferences.xanaScyphozoaSteal;
                    }
                    int _test = Mathf.FloorToInt(UnityEngine.Random.Range(0, SavedPreferences.xanaScyphozoaSteal.currentValue + SavedPreferences.xanaScyphozoaXanafie.currentValue));
                    if (_test < lowestPref.currentValue) {
                        if (lowestPref == SavedPreferences.xanaScyphozoaSteal) {
                            modePouvoir = MedusePower.VolAdn;
                        } else {
                            modePouvoir = MedusePower.Xanatifier;
                        }
                    } else {
                        if (lowestPref == SavedPreferences.xanaScyphozoaXanafie) {
                            modePouvoir = MedusePower.Xanatifier;
                        } else {
                            modePouvoir = MedusePower.VolAdn;
                        }
                    }
                }
            }
        } else {
            if (SavedPreferences.xanaScyphozoaSteal.currentValue > 0 && !LGunderControl.HasDevirtMortelle()) {
                modePouvoir = MedusePower.VolAdn;
            } else if (SavedPreferences.xanaScyphozoaXanafie.currentValue > 0) {
                modePouvoir = MedusePower.Xanatifier;
            } else {
                //fight!
                return false;
            }
        }

        if (LG._currentVehicule) LG.Descendre_vehicule(true);
        //if (LGunderControl.T_Inconscient.isRunning){
        //	LGunderControl.Reveiller();
        //}
        //si le lyokoguerrier était inconscient, on remplace ça par de la paralysie
        LG.DoParalyze(true);
        LG.DoControledFx(true);
        LyokoGuide lg = LyokoGuide.GetByID(LyokoGuide.IdentifierGuide("meduse"));
        lg.StopMove(true);
        lg.GetComponentInChildren<NoVirtZone>(true).gameObject.SetActive(true);
        lg.GetComponentInChildren<NoVirtZone>(true).AnmDisplay(true);

        if (LGunderControl.memoire == 9999 || LGunderControl.memoire == 0)
            LGunderControl.memoire = 5000;

        ActionsF.memoireLG.Execution(false);

        //Debug.Log("aucun lyokoguerrier n'est controlable pour détruire le territoire");
        //Debug.Log(">>la méduse xanatifie donc dans le seul but d'envoyer les lyokoguerriers attaquer le territoire");

        ActionsF.memoireLG.setModeM(modePouvoir);
        ActionsF.memoireLG.setLG(LGunderControl);
        lg.SetupVol(true); //si le LG était au dessus de la mer numérique, il ne faut pas qu'il tombe!
        lg.AnnulerChute();
        Debug.Log("setLGuidecontrol_meduseFonctions_" + LG.nom + " " + LGunderControl.memoire);
        //BanqueSonore.channelMemoireLyokoG=BanqueSonore.memoireCalcul.play(0,1000);
        T_UsePower.Start();
        Debug.Log("StartPower");
        return true;
    }
    public void OnReboot() {
        StopPower();
    }
    public void UsePower() {
        switch (modePouvoir) {
            case MedusePower.DrainCore:
                if (VarG.MV_lyoko.core.GetTotal_Hp() <= 0) {
                    EndPower();
                }
                //la réduction des valeurs est gérée directement dans le ATK du lyokoGuide
                break;

            case MedusePower.DrainSkid:
                if (VarG.skidbladnir.GetTotal_Hp() <= 0) {
                    EndPower();
                    //TODO: demater ?
                }
                //la réduction des valeurs est gérée directement dans le ATK du lyokoGuide
                break;
            case MedusePower.VolAdn:
                if (LGunderControl.memoire <= 5) {
                    LGunderControl.memoire = 0;
                    LGunderControl.PerdreCodeADN();
                    EndPower();
                } else {
                    LGunderControl.memoire -= Mathf.FloorToInt(vitessePouvoir[SavedPreferences.difficultyLevel.currentValue] * Time.timeScale);
                }
                ActionsF.memoireLG.maj();
                break;
            case MedusePower.Xanatifier:
                if (LGunderControl.memoire >= 9995) {
                    LGunderControl.memoire = 9999;
                    LGunderControl.Xanatifier();
                    EndPower();
                } else {
                    LGunderControl.memoire += Mathf.FloorToInt(vitessePouvoir[SavedPreferences.difficultyLevel.currentValue] * Time.timeScale);
                }
                ActionsF.memoireLG.maj();
                break;
        }
    }
    public void OnLwRvlpWhileXanafying() {
        if (Application.isEditor) Debug.Log("on passe en xanatification définitive ET on dévirtualise");
        if (LGunderControl == null)
            return;
        LGunderControl.Xanatifier(true);
        if (LGunderControl.GetGuide())
            LGunderControl.GetGuide().PreLW_Devirt(true); //équivalent d'une devirt par chute
        StopPower();
        if (LyokoGuide.IdentifierGuide("meduse") != 999)
            LyokoGuide.GetByID(LyokoGuide.IdentifierGuide("meduse")).Devirtualiser();
    }
    public void StopPower() {
        Debug.Log("StopPower");
        //le pouvoir est stoppé, le personnage relâché et inconscient, ça se produit normalement lorsqu'une méduse est attaquée en pleine utilisation de son pouvoir
        //où lorsqu'elle est touchée par le RVLP
        if (LGunderControl != null) {
            T_UsePower.Stop();
            if (LGunderControl.IsVirt() && !LGunderControl.GetGuide().PreLW_Devirt_Started) {
                LGunderControl.GetGuide().DoParalyze(false);
                LGunderControl.GetGuide().DoControledFx(false);
                LGunderControl.GetGuide().SetupVol(false); //si le LG était au dessus de la mer numérique et avait été rendu 'volant', on le fait retomber
                LGunderControl.SetInconscient(0, 50);
            }
            LGunderControl = null;
            ActionsF.memoireLG.maj();
            /*if (ActionsF.memoireLG.etat==1) {
                //dans le cas ou la fenêtre est en ouverture, on fait juste une nouvelle verif avec la fonction etape ouverture quand la fenêtre est entièrement ouverte
                ActionsF.memoireLG.Fermer();
            }*/
            if (LyokoGuide.IdentifierGuide("meduse") != 999) {
                LyokoGuide.GetByID(LyokoGuide.IdentifierGuide("meduse")).GetComponentInChildren<NoVirtZone>(true).AnmDisplay(false);
            }
        }
        if (modePouvoir == MedusePower.DrainSkid || modePouvoir == MedusePower.DrainCore) {
            T_UsePower.Stop();
            drainMeduse.RendreEnergie(true, true);
        }
    }
    public void EndPower() {
        Debug.Log("EndPower");
        ActionsF.memoireLG.maj();
        RAZ(true);
    }
}