using UnityEngine;
using IFSCL.Programs;
using Sirenix.OdinInspector;
namespace IFSCL.VirtualWorld {
    public class GardienFonctions : CreatureUnique {
        public static string spawnedName = "sphereGardien";
        public LyokoGuerrier capturedLw;
        [Unity.Collections.ReadOnly, SerializeField]
        private bool isUsingSkill;
        public MainTimer T_SkillUse = new MainTimer(0.01f);
        public CountdownUnit chipDestabilizationCountdown;
        public CloneEncounterProfile cloneEncounterProfile = new CloneEncounterProfile();
        public GardienFonctions() {
            nom = Lex.gardien.ToString();
            cloneEncounterProfile.linkedCreature = this;
            cloneEncounterProfile.monsterLex = Lex.gardien;
        }
        public override void Init() {
            chipDestabilizationCountdown = CountdownUnit.CreateNew("surge_guardian", ProgramsF.GetA_SC<PrgBubbleGuardian>().graph.countdownDisplay, OnCountdownComplete);
        }
        public override bool CanSpawn() {
            return !created && !time_TillReuse.IsRUNNING() &&
                   LevelableOption.GetOp(GameOptionName.xanaGuardianUse).currentValue > 0;
        }
        public override void OnVirt() {
            base.OnVirt();
            chipDestabilizationCountdown.Reset();
        }

        public void RAZ(bool fromDevirt = false) {
            if (VarG.isQuittingApplication)
                return;
            Debug.Log("razGardien");
            chipDestabilizationCountdown.Stop();
            T_SkillUse.Stop();
            if (capturedLw != null && capturedLw.IsVirt()) {
                capturedLw.GetGuide().DoParalyzeMidAir(false);
                Grouping.Degrouper(LyokoGuide.GetByName(Lex.gardien));
                capturedLw.GetGuide().EnableRvoController(true);
                capturedLw.GetGuide().RepositionAndTryEnableAI();
                capturedLw.GetGuide().Mcarte.ForceResetMoveBack();
                capturedLw = null;
                ProgramsF.GetA_SC<PrgBubbleGuardian>().graph.UpdateLwSetup();
            }
            if (fromDevirt) {
                RazClassique();
            }
            isUsingSkill = false;
            RazMinimum();
        }
        public void UpdateTimersFast() {
            if (!T_SkillUse.IncrementReach())
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
                if (ProgramsF.GetA_SC<PrgVMap>().IsOpen() &&
                    ProgramsF.GetA_SC<PrgVMap>().displayedTerritoire == _monsterGuide.territoire) {
                    PrgAnomaly.ExecuteDetailled(OSTarget.SC, MSG.GetAnomaly("LGteleportedBy").Replace("[LG_NAME]", capturedLw.nomTraduit), ProgramsF.GetA_SC<PrgVMap>(), true);
                }
            }
            TeleportAway();
            T_SkillUse.Start();
            TryDestabilizeChip();
            if (_monsterGuide.noVirtZone && ProgramsF.GetA_SC<PrgVMap>().GetCam3D().rtsCam._followTarget == _monsterGuide.transform) {
                _monsterGuide.noVirtZone.gameObject.SetActive(true);
                _monsterGuide.noVirtZone.AnmDisplay();
            }
            return true;
        }
        [Button]
        public void DBG_TestTpNearTower() {
            LyokoGuide _monsterGuide = LyokoGuide.GetByName(Lex.gardien);
            _monsterGuide.teleportProfile.ToRandomTowerDirect(_monsterGuide.GetSuperc(), true);
        }

        public bool hasTriedTeleportA;
        public void TeleportAway() {
            if (VarG.gameMode == GameMode.Story) {
                if (!hasTriedTeleportA) {
                    LyokoGuide.GetByName(Lex.gardien)?.teleportProfile.ToSurfaceDirect(ChapterAtk.GetGuardianTP_A(), ChapterAtk.GetGuardianTPV3_A());
                    hasTriedTeleportA = true;
                } else {
                    LyokoGuide.GetByName(Lex.gardien)?.teleportProfile.ToSurfaceDirect(ChapterAtk.GetGuardianTP_B(), ChapterAtk.GetGuardianTPV3_B());
                    hasTriedTeleportA = false;
                }
            } else {
                LyokoGuide.GetByName(Lex.gardien)?.teleportProfile.ToRandomTowerDirect(LyokoGuide.GetByName(Lex.gardien)?.GetSuperc(), true);
            }
        }

        public void OnCountdownComplete() {
            chipDestabilizationCountdown.Stop();
            TryDestabilizeChip();
            TeleportAway();
        }
        public void OnEntityChipChanged() {
            if (!created)
                return;
            if (chipDestabilizationCountdown != null && !chipDestabilizationCountdown.IsRunning() && ChipCode.GetByEnum(ChipContentType.sectorEntity).IsInDestinedReader_And_Calibrated()) {
                chipDestabilizationCountdown.SetTimer(60 * 6);
                chipDestabilizationCountdown.Launch();
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
