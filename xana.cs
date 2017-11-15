

    public void ResponseLwProximity() {
        DebugXana("ResponseLwProximity");
        foreach (LyokoGuerrier LG in LyokoGuerrier.liste) {
            if (LG.IsVirt() && !LG.controleXana && !LG.IsUnconscious()) {
                LyokoGuide lgHeros = LG.GetGuide();
                //if (lgHeros!=null)
                // DebugXana("check "+LG.nom+ " /recyc: " + lgHeros.recyclable + " /destr: " + lgHeros.enDestruction+ " /paralys: " + lgHeros.paralysie+" /tour: "+ lgHeros.inTour+" /chute: "+ lgHeros.enChute + " /groundDistance: "+lgHeros.GetDistanceFromGround() + " /lgHeros.carthagePos: " + lgHeros.carthagePos.ToString());
                //340fix: removed lgHeros.GetDistanceFromGround() < 3

                if (lgHeros != null && !lgHeros.recyclable && !lgHeros.enDestruction && !lgHeros.paralysie && !lgHeros.inTour && !lgHeros.enChute &&
                    lgHeros.carthagePos != CarthagePos.inArena && lgHeros.carthagePos != CarthagePos.inMaze && lgHeros.carthagePos != CarthagePos.inDomeElevatorRoom && lgHeros.carthagePos != CarthagePos.inGarageElevatorRoom) { //on utilise le getDistance pour Hopper

                    //si un LG est seul sur un territoire, la méduse est materialisée et le vise -priorité sur Aelita si jamais elle arrive sur le territoire-

                    if (IsMonsterAvailableFor(lgHeros.GetTerritoire(), Order.harass) && LyokoGuideUtilities.GetTotalLG_AttaquantDefendantTours(lgHeros.GetTerritoire(), Camps.xana) <= 0) {
                        DebugXana("on dit aux monstres dispos d'aller harceler (sachant qu'il n'y a pas d'attaque ou de défense de tour)");
                        foreach (LyokoGuide LGMonstre in LyokoGuide.liste) {
                            if (LGMonstre.nestedIN == null && !LGMonstre.enChute && !LGMonstre.recyclable && LGMonstre.camp == Camps.xana && LGMonstre.GetTerritoire() == lgHeros.GetTerritoire()
                                 && LGMonstre.savedOrderType != Order.destroySector && LGMonstre.savedOrderType != Order.defendTower && LGMonstre.savedOrderType != Order.attackTower && LGMonstre.harcelerWhichLG == "" && !LGMonstre.IsMeduseGardienOccuper()) {
                                DebugXana(LGMonstre.nom, "dispo pour aller harceler, sauf si exception carthage");
                                if (LGMonstre.GetTerritoire() == VarG.carthage) {
                                    if (LGMonstre.IsLG_accessibleInCarthage(lgHeros)) {
                                        //la fonction ci dessus prend en compte le fait que l'on puisse ne pas être sur carthage
                                        LGMonstre.GiveHarassOrder(lgHeros.nom);
                                    } else {
                                        if (lgHeros.carthagePos == CarthagePos.inMaze) {
                                            //on fait ça seulement pour quand les héros sont repérés dans les couloirs de carthage, 
                                            //pour le reste, c'est les responsePresenceVoute etc...
                                            //qui s'occupe de faire apparaitre des mponstres via defendreClef/defendreVoute...
                                            //cette condition est normalement inutile, car elle à lui dans la fonction 'CreateMonsters_pourHarceler'

                                            if (SavedPreferences.xanaRandomMonster.currentValue != 0) {
                                                //on créer des nouveaux monstres -juste pour harceler, uniquement si c'est possible
                                                CreateMonsters_pourHarceler(LG, lgHeros);
                                            }
                                        }
                                    }
                                } else {
                                    LGMonstre.GiveHarassOrder(lgHeros.nom);
                                }
                            }
                        }
                    } else {
                        DebugXana("confirmation, y'a pas de monstres dispo pour harceler", lgHeros.nom);
                        /*if (lgHeros.isInArena() && !TrapsGuides.graph.mc_ouvertureDroite.isOpen && !TrapsGuides.graph.mc_ouvertureGauche.isOpen) {
                            DebugXana("...et on en créé pas de nouveau car",LG.nom,"est dans l'arena");
                        } else {*/
                        //}
                        //note : si il y a des monstres en attaque ou en défense, on ne rajoute pas de Xana monstres pour harceler,
                        //mais on offre la possibilité que un groupe en atk/defense se splitte
                        if (LyokoGuideUtilities.GetTotalLG_AttaquantDefendantTours(lgHeros.GetTerritoire(), Camps.xana) > 0) {
                            DebugXana("il y a déjà des monstres attaquants défendant des tours -> donc on ne va créer des monstres pour harceler en prime. if LG close to tower, monster will attack or splitUp");
                            //voir fonction associée dans LyokoGuide
                        } else {
                            if (SavedPreferences.xanaRandomMonster.currentValue != 0) {
                                //on créer des nouveaux monstres -juste pour harceler, uniquement si c'est possible
                                if (waitturns_before_newMonsters == 0) {
                                    CreateMonsters_pourHarceler(LG, lgHeros);
                                    waitturns_before_newMonsters = 6; //4 trop peu apparemment
                                }
                            }
                        }
                    }
                }
            }
        }
        if (waitturns_before_newMonsters > 0)
            waitturns_before_newMonsters--;
    }
    //TODO if random monsters deactivated, you must have those bosses in other situations
    public void CreateMonsters_pourHarceler(LyokoGuerrier LG, LyokoGuide lgHeros) {
        DebugXana("pas de monstres deja dispo > creation d'un monstre pour harceler " + lgHeros.nom);
        if (!VarG.ModeMission) {
            //SEND_SPECIAL_HARCELEMENT
            int chancesMeduse = 0;
            int chancesXanafiedLw = 0;
            int chancesGardien = 0;
            int chancesClonePolymorphe = 0;

            if (SavedPreferences.xanaScyphozoaXanafie.currentValue > 0 || SavedPreferences.xanaScyphozoaSteal.currentValue > 0) {
                if (!VarG.meduseFonction.uniqueCree && !LG.GetGuide().paralysie) {
                    chancesMeduse += SavedPreferences.xanaScyphozoaXanafie.currentValue + SavedPreferences.xanaScyphozoaSteal.currentValue;
                    if (chancesMeduse > 3)
                        chancesMeduse = 3;
                    if (SavedPreferences.xanaSectorAttacks.currentValue > 0)
                        chancesMeduse++;
                    if (lgHeros.nom == "aelita" && LG.isAloneOnTerritoire()) {
                        chancesMeduse++;
                        if (lgHeros.GetTerritoire() == VarG.carthage)
                            chancesMeduse++;
                    }
                    if (!VarG.meduseFonction.CanBeUsed())
                        chancesMeduse -= 3;
                }
            }

            LyokoGuerrier xanafiedLWToUse = LyokoGuerrierUtilities.GetXanafiedLW_ToVirt();
            if (xanafiedLWToUse != null) {
                chancesXanafiedLw = 0;
                if (lgHeros.camp == Camps.jeremie && lgHeros.lgType == LgTypes.LyokoGuerrier && !lgHeros.recyclable && !lgHeros.paralysie)
                    chancesXanafiedLw++;

                if (lgHeros.nom == "aelita")
                    chancesXanafiedLw++;

                if (lgHeros.GetTerritoire() == VarG.carthage)
                    chancesXanafiedLw++;

                if (lgHeros.camp == Camps.franz && !lgHeros.recyclable)
                    chancesXanafiedLw += 3;
            }
            if (!lgHeros.paralysie && !LG.T_Inconscient.IsRunning() && !lgHeros.inTour) {
                if (lgHeros.GetTerritoire() != VarG.carthage) {
                    if (LG.isAloneOnTerritoire() && !VarG.gardienFonction.uniqueCree && SavedPreferences.xanaGuardianUse.currentValue > 0) {
                        chancesGardien += SavedPreferences.xanaGuardianUse.currentValue + 1; //on donne une chance de plus au gardien pour apparaitre (quelque soit le LG donc)
                    }
                    if (!VarG.gardienFonction.CanBeUsed()) //si on l'appeller récemment, le gardien ne sera pas appelé avant quelques temps
                        chancesGardien--;
                }
                if (LG.isAloneOnTerritoire() && !used_clonePoly_ForRandomAttack && SavedPreferences.xanaPolymorphUse.currentValue > 0) {
                    chancesClonePolymorphe += SavedPreferences.xanaPolymorphUse.currentValue;
                    if (LG.nom != "aelita")
                        chancesClonePolymorphe++;
                }
            }
            List<int> chancesVariety = new List<int>();
            chancesVariety.AddMany(chancesXanafiedLw, chancesMeduse, chancesGardien, chancesClonePolymorphe);
            //DebugXana("chancesBoss",chances);
            Territoire territoireCible = lgHeros.GetTerritoire();
            string nmTocreate = "";
            int chosenChanceValue = 0;
            for (int a = 0; a < chancesVariety.Count; a++) {
                int alea = Mathf.FloorToInt(UnityEngine.Random.Range(0, chancesVariety.Count));
                //DebugXana("aleaBoss",alea);
                if (alea < chancesVariety[a]) {
                    if (a == 0 && chancesXanafiedLw > 0) {
                        nmTocreate = xanafiedLWToUse.nom; //avec sa manta !
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

            if (nmTocreate != "") {
                //on à choisi un boss, maintenant on regarde si ça vaut le coup ou si on fait juste un monstre
                //sauf si sa chance est plus grande ou égale à 3
                int ran = UnityEngine.Random.Range(0, 11);//entre 0 & 10
                if (ran < 7 && chosenChanceValue < 3) {
                    nmTocreate = "";
                    Debug.Log("boss originaly chosen as " + nmTocreate + " but cancelled because no chance");
                } else {
                    Debug.Log("boss chosen as " + nmTocreate + " and confirmed");
                }
            }
            //CHOIX DE L'EMPLACEMENT DE VIRT
            Tour tour = null;
            List<Tour> listeToursP = new List<Tour>();
            if (territoireCible != VarG.carthage) {
                //Debug.Log("territoireCible " + territoireCible.Nom);
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
                switch (lgHeros.carthagePos) {
                    /* case CarthagePos.inTowerRoom:
                            tour = VarG.carthage.listeTours[0];
                            break;*/
                    case CarthagePos.inDomeVoid:
                        CreateMonsters(null, Order.attackDomeVoid, 0, LG.nom, CarthageAppearPointElement.Get_MonsterAppearPoint(lgHeros.HasCarthageElementProximity(CarthageElement.unique_southPole)));
                        return;
                    case CarthagePos.inGarageSkid:
                        CreateMonsters(null, Order.attackGarage, 0, LG.nom, CarthageElement.unique_garageSkid);
                        return;
                    case CarthagePos.onDomeBridge:
                        CreateMonsters(null, Order.attackDomeBridge, 0, LG.nom, CarthageElement.unique_domeBridge);
                        return;
                        //case CarthagePos.inCoreRoom:
                        //    CreateMonsters(null, Order.attackCore, 0, LG.nom, CarthageElement.unique_core);
                        //    return;
                }
            }
            if (tour == null) {
                Debug.LogWarning("Aucune tour trouvée pour y créer des monstres!");
                return;
            }
            if (nmTocreate != "") {
                Debug.Log("creation d'un boss pour harceler");
                LyokoGuide LG1 = CreateMonstersSuite(CRDManager.c(territoireCible, tour.ID_de_secteur + 1), nmTocreate, 0, Order.harass, LG.nom, null);
                if (LG1 != null && nmTocreate == "clone_polymorphe") {
                    used_clonePoly_ForRandomAttack = true;
                }
            } else {
                //dans le cas de carthage, les monstres sont créés avec les autres checks, sauf pour les persos dans les couloirs
                if (territoireCible != VarG.carthage) {
                    DebugXana("creation monstre pour harceler, après ça normalement, y'a pas d'autres monstres qui devraient être créés pour harceler, sauf si territoires différents");
                    CreateMonsters(tour, Order.harass, 0, LG.nom);
                } else {
                    //on ne créer pas les monstres du MAZE ici mais par une colision avec une specialeTile
                    /*if (lgHeros.carthagePos == CarthagePos.inMaze) {
                        //s'assurer ici qu'il n'y a pas déjà trop de monstres dans le couloir
                        DebugXana("creation monstre pour héros dans couloir de carthage");
                        CreateMonsters(tour, Order.harass, 0, LG.nom);
                    }*/
                }
            }
        }
    }
