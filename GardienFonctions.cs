using DG.Tweening;
using UnityEngine;
using IFSCL.Programs;
using Pathfinding;
using Sirenix.OdinInspector;
namespace IFSCL.VirtualWorld {
    public class GardienFonctions : CreatureUnique {
        public static string spawnedName = "sphereGardien";
        public LyokoGuerrier capturedLw;
        [Unity.Collections.ReadOnly, SerializeField]
        private bool isUsingSkill;
        public MainTimer T_SkillUse = new ("T_GardienSkillUse",0.01f);
        public CountdownUnit CU_chipDestabilization;
        public CloneEncounterProfile cloneEncounterProfile = new ();
        public GardienFonctions() {
            nom = Lex.gardien.ToString();
            cloneEncounterProfile.linkedCreature = this;
            cloneEncounterProfile.monsterLex = Lex.gardien;
        }
        public override void Init() {
            CU_chipDestabilization = CountdownUnit.CreateNew("surge_guardian", OnCountdownComplete);
            CU_chipDestabilization.AddDynamicDisplay(ProgramsF.GetA_SC<PrgBubbleGuardian>().graph.CU_Display);
        }
        public override bool CanSpawn() {
            return !created && !time_TillReuse.IsRUNNING() &&
                   LevelableOption.GetOp(GameOptionName.xanaGuardianUse).currentValue > 0;
        }
        public override void OnVirt(params object[] args) {
            base.OnVirt();
            CU_chipDestabilization.Reset();
        }

        public void RAZ(bool fromDevirt = false) {
            if (VarG.isQuittingApplication)
                return;
            Debug.Log("razGardien");
            CU_chipDestabilization.Stop();
            T_SkillUse.Stop();
            if (capturedLw != null && capturedLw.IsVirt()) {
                var guide = capturedLw.GetGuide();
                
                guide.DoParalyzeMidAir(false);
                Grouping.Degrouper(LyokoGuide.GetByName(Lex.gardien));
                guide.EnableRvoController(true);
                guide.RepositionAndTryEnableAI();
                guide.Mcarte.ForceResetMoveBack();
                
                capturedLw = null;
                ProgramsF.GetA_SC<PrgBubbleGuardian>().graph.UpdateLwSetup();
            }
            if (fromDevirt) {
                RazClassique();
            }
            isUsingSkill = false;
            RazMinimum();
        }
        public void UpdateTimers() {
            if (!T_SkillUse.TryDecrement())
                return;
            if (!T_SkillUse.tickTime.Equals(GameBalanceREF.GetByEnum(GameBalanceType.guardianHpRemovalTickSpeed).GetCurrentValue()))
                T_SkillUse.tickTime = GameBalanceREF.GetByEnum(GameBalanceType.guardianHpRemovalTickSpeed).GetCurrentValue();
            UseSkill();
        }
        [Button]
        public bool IsUsingItsPower() {
            return isUsingSkill;
        }
        public bool TrySetLGcontrol(LyokoGuide LG) {
            if (LG.IsLifeBubble() || !created || LG.lgType != LgTypes.LyokoGuerrier || LyokoGuerrier.GetByName(LG.nom).controle != XanaControlStatus.None)
                return false;
            if (LG.waitForUnReboot) //we wait because the creature can try this immediately after the endReboot, whereas the LG waits for the endRebootfreeze which messes things up.
                return false;
            if (IsUsingItsPower())
                return false;
            if (LG.vehicleProfile.inTransporteur) //if just entered in transporter
                return false;
            LG.vehicleProfile.GetOut_Vehicle();
            capturedLw = LyokoGuerrier.GetByName(LG.nom);
            ProgramsF.GetA_SC<PrgBubbleGuardian>().graph.UpdateLwSetup();
            isUsingSkill = true;
            LyokoGuide _monsterGuide = LyokoGuide.GetByName(Lex.gardien);
            _monsterGuide.StopMove(true);
            _monsterGuide.SetOrderPersecute("");
            LG.OnCapturedByGardien(_monsterGuide);
            if (!_monsterGuide.HasWalkablePathUnderneath(false)) {
                _monsterGuide.Remonter_surface(true, true); //in case the keeper pricked an LW that was on an aerian path
                if (ProgramsF.GetA_SC<PrgVirtualMap>().IsOpen() &&
                    ProgramsF.GetA_SC<PrgVirtualMap>().displayedTerritoire == _monsterGuide.territoire) {
                    PrgAnomaly.ExecuteDetailled(OSTarget.SC, MSG.GetAnomaly("LGteleportedBy").Replace("[LG_NAME]", capturedLw.nomTraduit), ProgramsF.GetA_SC<PrgVirtualMap>(), true);
                }
            }
            TeleportAway();
            T_SkillUse.Launch();
            TryDestabilizeChip();
            if (_monsterGuide.noVirtZone && ProgramsF.GetA_SC<PrgVirtualMap>().GetCam3D().rtsCam._followTarget == _monsterGuide.transform) {
                _monsterGuide.noVirtZone.gameObject.SetActive(true);
                _monsterGuide.noVirtZone.AnmDisplay();
            }
            return true;
        }
        [Button]
        public void DBG_TestTpToRandomTower() {
            LyokoGuide _monsterGuide = LyokoGuide.GetByName(Lex.gardien);
            _monsterGuide.teleportProfile.ToRandomTowerDirect(_monsterGuide.GetSuperc(), true);
        }

        public bool hasTriedTeleportA;
        [Button]
        public void TeleportAway() {
            if (VarG.gameMode == GameMode.Story) {
                if (!hasTriedTeleportA) {
                    LyokoGuide.GetByName(Lex.gardien)?.teleportProfile.ToSurfaceDirect(ChapterLyokoAction.GetGuardianTP_A(), ChapterLyokoAction.GetGuardianTPV3_A());
                    hasTriedTeleportA = true;
                } else {
                    LyokoGuide.GetByName(Lex.gardien)?.teleportProfile.ToSurfaceDirect(ChapterLyokoAction.GetGuardianTP_B(), ChapterLyokoAction.GetGuardianTPV3_B());
                    hasTriedTeleportA = false;
                }
            } else {
                LyokoGuide.GetByName(Lex.gardien)?.teleportProfile.ToRandomTowerDirect(LyokoGuide.GetByName(Lex.gardien)?.GetSuperc(), true);
            }
        }

        public void OnCountdownComplete() {
            CU_chipDestabilization.Stop();
            TryDestabilizeChip();
            TeleportAway();
        }
        public void OnEntityChipChanged() {
            if (!created)
                return;
            if (CU_chipDestabilization != null && !CU_chipDestabilization.IsRunning() && ChipCode.GetByEnum(ChipContentType.sectorEntity).IsInDestinedReader_And_Calibrated()) {
                CU_chipDestabilization.SetTimer(60 * 6);
                CU_chipDestabilization.Launch();
            }
        }
        public void TryDestabilizeChip() {
            Debug.Log("TryDestabilizeChip");
            if (!ChipCode.GetByEnum(ChipContentType.sectorEntity).IsInDestinedReader())
                return;
            ChipCode.GetByEnum(ChipContentType.sectorEntity).SetCalibration(false);
            PrgAnomaly.ExecuteComposite(AnomalyString.localImpoChip, OSTarget.SC);
        }
        public void UseSkill() {
            if (capturedLw == null || capturedLw.IsInDevirt() || capturedLw.GetGuide() == null) {
                T_SkillUse.Stop();
                if (!isUsingSkill)
                    return;
                isUsingSkill = false;
                LyokoGuide.GetByName(Lex.gardien)?.GetComponentInChildren<NoVirtZone>(true).AnmDisplay(false);
                return;
            }
            if (!isUsingSkill || capturedLw.GetGuide().PV <= 10)
                return;
            int i = (int)GameBalanceList.GetREF(GameBalanceType.guardianHpRemovalPer3Sec).GetCurrentValue();
            capturedLw.GetGuide().PV = Mathf.Clamp(capturedLw.GetGuide().PV - i, 10, 999);
        }
    }
}
