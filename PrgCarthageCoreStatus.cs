using DG.Tweening;
using IFSCL.VirtualWorld;
using UnityEngine;
namespace IFSCL.Programs {
    public class PrgCarthageCoreStatus : ProgramsF {
        public FCarthageCoreStatusPrefab graph;
        public float lastSavedHP;
        private GameObject regenParticleSystem;
        private ParticleSystem hitParticleSystem;
        private ParticleSystem shieldDownParticleSystem;
        public AudioSource _audioSource;
        public bool autoOpened_Once;
        private VirtualCore linkedCore;
        public bool initComplete;
        public PrgCarthageCoreStatus(params object[] args)
            : base(args) {
        }
        public override void Initialisation(FContentPrefab fenetreGameObject) {
            base.Initialisation(fenetreGameObject);
            graph = fenetreGameObject.GetComponent<FCarthageCoreStatusPrefab>();
            graph.OnInit();
        }
        public void InitAfterMV() {
            linkedCore = VarG.scLyoko.core;
            graph.linkedCore = linkedCore;
            ParticleSystem[] particleSystems = VarG.carthageParam.coreElement.GetComponentsInChildren<ParticleSystem>(true);
            foreach (ParticleSystem ps in particleSystems) {
                if (ps.gameObject.name.Contains("hit"))
                    hitParticleSystem = ps;
            }
            regenParticleSystem = VarG.carthageParam.coreElement.regenFX;
            shieldDownParticleSystem = VarG.carthageParam.shieldDownFX;
            _audioSource =
                VarG.carthageParam.shieldDownFX.GetComponent<AudioSource>(); //pas vmap3D pour le moment en terme de distances
            hitParticleSystem.Stop();
            StopRegenFX();
            shieldDownParticleSystem.Stop();
            graph.damageGradients.transform.DOScale(0.5f, 0);
            ResetOpenedOnce();
            lastSavedHP = graph.GetTotal();
            initComplete = true;
        }
        public override void Raz_FromRvlp() {
            ResetOpenedOnce();
            lastSavedHP = graph.GetTotal();
        }
        public override void Raz_FromRestart() {
            ResetOpenedOnce();
            initComplete = false;
        }
        public void ResetOpenedOnce() {
            autoOpened_Once = false;
        }
        public void OnCarthageDestruction() {
            if (IsOpen())
                CloseWin();
        }
        public override void UpdateTimers() {
            if (!initComplete)
                return;
            if (VarG.otherData.coreAttacksAutoWindowOpening && lastSavedHP > graph.GetTotal()) {
                //Debug.Log("lastSavedHP: "+lastSavedHP + " / graph.GetTotal(): "+graph.GetTotal());
                //même si déjà ouverte, le but est que ça ouvre uniquement si on a pas ouvert la fenêtre depuis longtemps (aka = dernier raz ou dernier bouclier tombé)
                if (!IsOpen() && !autoOpened_Once) {
                    if (linkedCore.linkedMV.IsShellConnected()) {
                        Execution(false);
                    }
                    // CompositeSentence("coreIsUnderAttack", false);
                }
                autoOpened_Once = true; //must be after autoOpen
            }
        }
        public void StopRegenFX() {
            //regenParticleSystem.main.loop=false;
            if (regenParticleSystem != null && regenParticleSystem.activeSelf)
                regenParticleSystem.SetActive(false);
        }
        public void StartRegenFX() {
            if (regenParticleSystem != null && !regenParticleSystem.activeSelf) {
                regenParticleSystem.SetActive(true);
            }
        }
        public void DoDamageFX(bool animBringDownShield, bool withFX = false) {
            if (withFX) {
                hitParticleSystem.Play(); //hit
            }
            if (animBringDownShield) {
                shieldDownParticleSystem.Play(); //BringDownShieldAnim
                _audioSource.Play();
                graph.damageGradients.DOFade(1, 2).OnComplete(() => graph.damageGradients.DOFade(0, 3));
                graph.damageGradients.transform.DOScale(1.3f, 2).OnComplete(() => graph.damageGradients.transform.DOScale(0.5f, 3));
            }
        }
        public override void OnOpenStarted(bool direct) {
            graph.moverAnimator.Play("cardDisappear", -1, 1);
            graph.damageGradients.DOFade(0, 0);
        }
        public override void OnOpenFinished(bool direct) {
            graph.PlayOneShot(BanqueSonore.instance.data.annexe);
            graph.moverAnimator.Play("cardAppear", -1, 0);
            
            lastSavedHP = graph.GetTotal();
            
        }
        public override void OnCloseStarted(bool direct = false) {
            graph.PlayOneShot(BanqueSonore.instance.data.annexe);
            graph.moverAnimator.CrossFade("cardDisappear", 0.2f);
        }
        public override void Execution(bool _withErrorMessage = true, bool _direct = false) {
            if (etat == OpenCloseStatus.Closed) {
                if (VarG.scLyoko.IsShellConnected()) {
                    OpenWin(_withErrorMessage, _direct);
                } else {
                    if (_withErrorMessage)
                        CompAnomalyString(AnomalyString.shellNotConnected, this);
                }
            } else {
                if (_withErrorMessage)
                    PrintAlreadyOpened();
            }
        }
    }
}
