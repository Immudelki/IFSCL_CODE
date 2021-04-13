namespace IFSCL.Programs {
    using DG.Tweening;
    using UnityEngine;
    using VirtualWorld;
    public class PrgCarthageCoreStatus : ProgramsF {
        public FCarthageCoreStatusPrefab graph;
        public float lastSavedHP = 0;
        public ParticleSystem regenParticleSystem;
        public ParticleSystem hitParticleSystem;
        public ParticleSystem shieldDownParticleSystem;
        public Light lightHit;
        public AudioSource _audioSource;
        public bool autoOpened_Once = false;
        public VirtualCore linkedCore;
        public bool initComplete = false;
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
            ParticleSystem[] particleSystems = VarG.carthageParam.corePlane.GetComponentsInChildren<ParticleSystem>(true);
            foreach (ParticleSystem ps in particleSystems) {
                if (ps.gameObject.name.Contains("shield"))
                    shieldDownParticleSystem = ps;
                if (ps.gameObject.name.Contains("hit"))
                    hitParticleSystem = ps;
                if (ps.gameObject.name.Contains("regen"))
                    regenParticleSystem = ps;
            }
            if (regenParticleSystem == null) {
                Debug.LogError("regen particle system not found");
            }
            lightHit = VarG.carthageParam.corePlane.GetComponentInChildren<Light>();
            _audioSource = VarG.carthageParam.corePlane.GetComponentInChildren<AudioSource>(); //pas vmap3D pour le moment en terme de distances
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
        }
        public override void Raz_FromRestart() {
            ResetOpenedOnce();
            initComplete = false;
        }
        public void ResetOpenedOnce() {
            autoOpened_Once = false;
        }
        public void OnCarthageDestruction() {
            if (this.IsOpen())
                this.Fermer();
        }
        public override void UpdateTimers() {
            if (!initComplete)
                return;
            if (VarG.autoCoreAttacksScan && lastSavedHP > graph.GetTotal()) {
                // Debug.Log("lastSavedHP: "+lastSavedHP + " / graph.GetTotal(): "+graph.GetTotal());
                //même si déjà ouverte, le but est que ça ouvre uniquement si on a pas ouvert la fenêtre depuis longtemps (aka = dernier raz ou dernier bouclier tombé)
                if (!IsOpen() && !autoOpened_Once) {
                    if (linkedCore.linkedMV.IsShellConnected()) {
                        Execution(false);
                    } else {
                        CompositeSentence("coreIsUnderAttack", false);
                    }
                }
                autoOpened_Once = true; //must be after autoOpen
            }
        }
        public void StopRegenFX() {
            //regenParticleSystem.main.loop=false;
            if (regenParticleSystem != null && regenParticleSystem.gameObject.activeSelf)
                regenParticleSystem.gameObject.SetActive(false);
        }
        public void StartRegenFX() {
            if (regenParticleSystem != null && !regenParticleSystem.gameObject.activeSelf) {
                regenParticleSystem.gameObject.SetActive(true);
                regenParticleSystem.Play();
            }
        }
        public void DoDamageFX(bool animBringDownShield, bool withFX = false) {
            if (withFX) {
                hitParticleSystem.Play(); //hit
                lightHit.DOKill();
                lightHit.DOColor(Color.white, 0.3f).OnComplete(() => lightHit.DOColor(Color.black, 0.5f));
            }
            if (animBringDownShield) {
                shieldDownParticleSystem.Play(); //BringDownShieldAnim
                lightHit.DOKill();
                lightHit.DOColor(Color.white, 0.6f).OnComplete(() => lightHit.DOColor(Color.black, 1f));
                _audioSource.Play();
                graph.damageGradients.DOFade(1, 2).OnComplete(() => graph.damageGradients.DOFade(0, 3));
                graph.damageGradients.transform.DOScale(1.3f, 2).OnComplete(() => graph.damageGradients.transform.DOScale(0.5f, 3));
            }
        }
        public override void OnOpenStarted() {
            graph.moverAnimator.Play("carthageCoreCardDisappear", -1, 1);
            graph.damageGradients.DOFade(0, 0);
        }
        public override void OnOpenFinished() {
            graph.moverAnimator.Play("carthageCoreCardAppear", -1, 0);
            lastSavedHP = graph.GetTotal();
            base.OnOpenFinished();
        }
        public override void OnCloseStarted() {
            graph.moverAnimator.CrossFade("carthageCoreCardDisappear", 0.2f);
        }
        public override void Execution(bool _withErrorMessage = true, bool _direct=false) {
            if (etat == OpenCloseStatus.Closed) {
                if (VarG.scLyoko.IsShellConnected()) {
                    Ouvrir(_withErrorMessage, _direct);
                } else {
                    if (_withErrorMessage)
                        CompositeDetailled(MSG.GetAnomaly("shellNotConnected"), this, true);
                }
            } else {
                if (_withErrorMessage)
                    AffDejaOuvert();
            }
        }
    }
}