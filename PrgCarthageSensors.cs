using UnityEngine;
using DG.Tweening;
namespace IFSCL.Programs {
    using VirtualWorld;
    public class PrgCarthageSensors : ProgramsF {
        public FCarthageSensorsPrefab graph;
        public int posX;
        public int posZ;
        public int zoomAdditiveValue = 0;
        public Vector3 posMatrix = Vector3.zero;
        public Camera camera3D;
        public MazeKeyManager linkedMazeManager;
        public bool lockCamButtons = true;
        public PrgCarthageSensors(params object[] args)
            : base(args) {
        }
        public override void Initialisation(FContentPrefab fenetreGameObject) {
            base.Initialisation(fenetreGameObject);
            graph = fenetreGameObject.GetComponent<FCarthageSensorsPrefab>();
            camera3D = GameScene.Instance.sensorCamLOAD;
            camera3D.transform.parent.gameObject.SetActive(false);
            //linkedMazeManager = VarG.MV_replika_carthage.mazeManager;
        }
        public void SetMazeManager(MazeKeyManager _newMazeManager) { //VarG.scLyoko.mazeManager
            linkedMazeManager = _newMazeManager;
        }
        public void OnCarthageStartDestruction() {
            this.Fermer();
        }
        public override void Execution(bool _withErrorMessage = true, bool _direct=false) {
            if (etat == OpenCloseStatus.Closed) {
                if (VarG.scLyoko.IsShellConnected()) {
                    Ouvrir(_withErrorMessage, _direct);
                } else {
                    CompositeDetailled(MSG.GetAnomaly("shellNotConnected"), this, true);
                }
            } else {
                AffDejaOuvert();
            }
        }
        public override void OnOpenStarted() {
            camera3D.transform.parent.gameObject.SetActive(true);
            graph.animator.Play("carthageSensorsSmaller", -1, 1);
            camera3D.transform.parent.GetComponent<Animator>().Play("carthageSensors3d_Loop", -1, 0);
            zoomAdditiveValue = 0;
            posX = 0;
            posZ = 0;
            UpdatePan();
            UpdateZoom();
            UpdateTexts();
            UpdateColor();
            UpdateRebootStatus();
        }
        public override void OnOpenFinished() {
            if (linkedMazeManager.statusM == MazeStatus.opened || linkedMazeManager.statusM == MazeStatus.opening) {
                OnMazeOpenEvent();
            }
            base.OnOpenFinished();
        }
        public void OnMazeOpenEvent() {
            if (!IsOpen())
                return;
            Debug.Log("OnMazeOpenEvent");
            this.audioSourceFenetre.PlayOneShot(BanqueSonore.Instance.data.carthageSensors_ToBigger);
            graph.animator.Play("carthageSensorsBigger", -1, 0);
            lockCamButtons = false;
            camera3D.transform.parent.GetComponent<Animator>().Play("carthageSensors3d_Enable", -1, 0);
        }
        public override void OnCloseStarted() {
            GetAByOS<PrgCarthageCountdown>(OSTarget.SC).Fermer();
        }
        public override void OnCloseFinished() {
            camera3D.transform.parent.gameObject.SetActive(false);
            lockCamButtons = true;
            base.OnCloseFinished();
        }
        public void UpdateRebootStatus() {
            if (IsOpen()) {
                graph.rebootPanel.gameObject.SetActive(VarG.scLyoko.isRebooting || VarG.scLyoko.waitSecondWave_ToEnd);
                graph.rawImage.gameObject.SetActive(!VarG.scLyoko.isRebooting && !VarG.scLyoko.waitSecondWave_ToEnd);
            }
        }
        public void OnClickBtn_DoCameraAction(int ID) {
            if (lockCamButtons)
                return;
            //panUp
            //panDown
            //panRight
            //panLeft
            //zoomOut
            //zoomIn
            switch (ID) {
                case 0:
                    posX += 1;
                    this.audioSourceFenetre.PlayOneShot(BanqueSonore.Instance.data.carthageSensors_Move);
                    break;
                case 1:
                    posX -= 1;
                    this.audioSourceFenetre.PlayOneShot(BanqueSonore.Instance.data.carthageSensors_Move);
                    break;
                case 2:
                    posZ -= 1;
                    this.audioSourceFenetre.PlayOneShot(BanqueSonore.Instance.data.carthageSensors_Move);
                    break;
                case 3:
                    posZ += 1;
                    this.audioSourceFenetre.PlayOneShot(BanqueSonore.Instance.data.carthageSensors_Move);
                    break;
                case 4:
                    zoomAdditiveValue -= 1;
                    this.audioSourceFenetre.PlayOneShot(BanqueSonore.Instance.data.carthageSensors_Zoom);
                    break;
                case 5:
                    zoomAdditiveValue += 1;
                    this.audioSourceFenetre.PlayOneShot(BanqueSonore.Instance.data.carthageSensors_Zoom);
                    break;
            }
            posX = Mathf.Clamp(posX, -2, 2);
            posZ = Mathf.Clamp(posZ, -2, 2);
            zoomAdditiveValue = Mathf.Clamp(zoomAdditiveValue, -2, 0);
            if (ID < 4) {
                UpdatePan();
            } else {
                UpdateZoom();
            }
        }
        private void UpdateZoom() {
            camera3D.DOKill();
            camera3D.DOFieldOfView(40 + (zoomAdditiveValue * 15), 0.2f);
            graph.zoomValue.text = 100 + (zoomAdditiveValue * -50) + "%";
        }
        private void UpdatePan() {
            posMatrix = new Vector3(posX * 125, 975, posZ * 125);
            //camera3D.gameObject.transform.DOKill();
            camera3D.DOKill();
            camera3D.gameObject.transform.DOLocalMove(posMatrix, 0.2f);
        }
        public void KeyActivated_CallAnim() {
            if (!IsOpen())
                return;
            graph.animator.Play("carthageSensorsSmaller", -1, 0);
            this.audioSourceFenetre.PlayOneShot(BanqueSonore.Instance.data.carthageSensors_Win);
            camera3D.transform.parent.GetComponent<Animator>().Play("carthageSensors3d_ResolveMaze", -1, 0);
            zoomAdditiveValue = 0;
            posX = 0;
            posZ = 0;
            UpdatePan();
            UpdateZoom();
            UpdateTexts();
            UpdateColor();
        }
        public void MazeClosing_CallAnim() {
            if (!IsOpen())
                return;
            graph.animator.Play("carthageSensorsSmaller", -1, 0);
            this.audioSourceFenetre.PlayOneShot(BanqueSonore.Instance.data.carthageSensors_Fail);
            camera3D.transform.parent.GetComponent<Animator>().Play("carthageSensors3d_ResolveMaze", -1, 0); //même anim
            zoomAdditiveValue = 0;
            lockCamButtons = true;
            posX = 0;
            posZ = 0;
            UpdatePan();
            UpdateZoom();
            UpdateTexts();
            UpdateColor();
        }
        public void UpdateColor() {
            if (!this.IsOpen())
                return;
            if (linkedMazeManager.statusM == MazeStatus.closing) {
                graph.rawImage.color = graph.mazeClosingColor;
                graph.animator.Play("carthageSensorsPulseLoop", 1, 0);
            } else {
                graph.animator.Play("carthageSensorsEmptyNoLoop", 1, 1);
                if (linkedMazeManager.statusM == MazeStatus.resolved) {
                    graph.rawImage.color = graph.keyFoundColor;
                } else {
                    graph.rawImage.color = graph.keyNotFoundColor;
                }
            }
        }
        public void UpdateTexts() {
            if (linkedMazeManager.statusM == MazeStatus.resolved) {
                graph.keyInfo.text = MSG.GetWord("found");
                graph.mazeField.text = MSG.GetName("maze") + ": " + MSG.GetWord("indispo");
            } else {
                graph.keyInfo.text = MSG.GetWord("notfound");
                string status = "";
                switch (linkedMazeManager.statusM) {
                    case MazeStatus.closed:
                        status = MSG.GetWord("closed");
                        break;
                    case MazeStatus.closing:
                        status = MSG.GetWord("closing");
                        break;
                    case MazeStatus.opened:
                        status = MSG.GetWord("opened");
                        break;
                    case MazeStatus.opening:
                        status = MSG.GetWord("opening");
                        break;
                }
                graph.mazeField.text = MSG.GetName("maze") + ": " + status;
            }
        }
        public void UpdateTextColor_FromMaze() {
            UpdateTexts();
            UpdateColor();
        }
    }
}