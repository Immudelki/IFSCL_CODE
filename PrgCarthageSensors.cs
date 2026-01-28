using DG.Tweening;
using IFSCL.AbstractWorld;
using IFSCL.VirtualWorld;
using RTSCam;
using UnityEngine;
using UnityEngine.Serialization;
namespace IFSCL.Programs {
    public class PrgCarthageSensors : ProgramsF {
        public FCarthageSensorsPrefab graph;
        public int posX;
        public int posZ;
        public int zoomAdditiveValue;
        public Vector3 posMatrix = Vector3.zero;
        public SensorMaze3D cam3D;
        public MazeManager linkedMazeManager;
        public bool lockCamButtons = true;
        public PrgCarthageSensors(params object[] args)
            : base(args) {
        }
        public override void Initialisation(FContentPrefab fenetreGameObject) {
            base.Initialisation(fenetreGameObject);
            graph = fenetreGameObject.GetComponent<FCarthageSensorsPrefab>();
        }
        public override Camera3D_Manager GetCam3D() {
            return cam3D;
        }
        public void LinkCamToAbstract() {
            cam3D = VarG.carthageParam.sensorMaze3D;
        }
        public void SetMazeManager(MazeManager _newMazeManager) {
            linkedMazeManager = _newMazeManager;
        }
        public void OnCarthageStartDestruction() {
            CloseWin();
        }
        public override void Execution(bool _withErrorMessage = true, bool _direct = false) {
            if (etat == OpenCloseStatus.Closed) {
                if (VarG.scLyoko.IsShellConnected()) {
                    OpenWin(_withErrorMessage, _direct);
                } else {
                    CompAnomalyString(AnomalyString.shellNotConnected);
                }
            } else {
                PrintAlreadyOpened();
            }
        }
        public override void OnOpenStarted(bool direct) {
            cam3D.Show(direct);
            cam3D.SwapToGameControl();
            graph.animator.Play("carthageSensorsSmaller", -1, 1);
            cam3D.animator.Play("carthageSensors3d_Loop", -1, 0);
            zoomAdditiveValue = 0;
            posX = 0;
            posZ = 0;
            UpdatePan();
            UpdateZoom();
            UpdateTexts();
            UpdateColor();
            UpdateRebootStatus();
        }
        public override void OnOpenFinished(bool direct) {
            if (linkedMazeManager.statusM == MazeStatus.opened_Unresolved ||
                linkedMazeManager.statusM == MazeStatus.opening_Unresolved) {
                OnMazeOpenEvent();
            }
        }
        public void OnMazeOpenEvent() {
            if (!IsOpen())
                return;
            cam3D.SwapToGameControl();
            Debug.Log("OnMazeOpenEvent");
            PlayOneShot(BanqueSonore.instance.data.carthageSensors_ToBigger);
            graph.animator.Play("carthageSensorsBigger", -1, 0);
            lockCamButtons = false;
            cam3D.animator.Play("carthageSensors3d_Enable", -1, 0);
        }
        public override void OnCloseStarted(bool direct = false) {
            GetAByOS<PrgCarthageCountdown>(OSTarget.SC).CloseWin();
        }
        public override void OnCloseFinished() {
            cam3D.Hide();
            lockCamButtons = true;
            
        }
        public void UpdateRebootStatus() {
            if (IsOpen()) {
                graph.rebootPanel.SetActive(VarG.scLyoko.isRebooting || VarG.scLyoko.waitSecondWave_ToEnd);
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
                    PlayOneShot(BanqueSonore.instance.data.carthageSensors_Move);
                    break;
                case 1:
                    posX -= 1;
                    PlayOneShot(BanqueSonore.instance.data.carthageSensors_Move);
                    break;
                case 2:
                    posZ -= 1;
                    PlayOneShot(BanqueSonore.instance.data.carthageSensors_Move);
                    break;
                case 3:
                    posZ += 1;
                    PlayOneShot(BanqueSonore.instance.data.carthageSensors_Move);
                    break;
                case 4:
                    zoomAdditiveValue -= 1;
                    PlayOneShot(BanqueSonore.instance.data.carthageSensors_Zoom);
                    break;
                case 5:
                    zoomAdditiveValue += 1;
                    PlayOneShot(BanqueSonore.instance.data.carthageSensors_Zoom);
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
            cam3D.rtCam.DOKill();
            cam3D.rtCam.DOFieldOfView(40 + (zoomAdditiveValue * 15), 0.2f);
            graph.zoomValue.text = 100 + (zoomAdditiveValue * -50) + "%";
        }
        private void UpdatePan() {
            posMatrix = new Vector3(posX * 125, 975, posZ * 125);
            cam3D.rtCam.DOKill();
            cam3D.rtCam.transform.DOLocalMove(posMatrix, 0.2f);
        }
        public void KeyActivated_CallAnim() {
            if (!IsOpen())
                return;
            cam3D.SwapToGameControl();
            graph.animator.Play("carthageSensorsSmaller", -1, 0);
            PlayOneShot(BanqueSonore.instance.data.carthageSensors_Win);
            cam3D.animator.Play("carthageSensors3d_ResolveMaze", -1, 0);
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
            cam3D.SwapToGameControl();
            graph.animator.Play("carthageSensorsSmaller", -1, 0);
            PlayOneShot(BanqueSonore.instance.data.carthageSensors_Fail);
            cam3D.animator.Play("carthageSensors3d_ResolveMaze", -1, 0); //même anim
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
            if (!IsOpen())
                return;
            if (linkedMazeManager.statusM == MazeStatus.closing_Unresolved) {
                graph.rawImage.color = graph.mazeClosingColor;
                graph.animator.Play("carthageSensorsPulseLoop", 1, 0);
            } else {
                graph.animator.Play("carthageSensorsEmptyNoLoop", 1, 1);
                if (linkedMazeManager.statusM == MazeStatus.resolved_OR_Disabled) {
                    graph.rawImage.color = graph.keyFoundColor;
                } else {
                    graph.rawImage.color = graph.keyNotFoundColor;
                }
            }
        }
        public void UpdateTexts() {
            if (linkedMazeManager.statusM == MazeStatus.resolved_OR_Disabled) {
                graph.keyInfo.text = MSG.GetWord("found");
                graph.mazeField.text = MSG.GetName("maze") + VarG.twoPoints + MSG.GetWord("indispo");
            } else {
                graph.keyInfo.text = MSG.GetWord("notfound");
                string status = "";
                switch (linkedMazeManager.statusM) {
                    case MazeStatus.closed_Unresolved:
                        status = MSG.GetWord("closed");
                        break;
                    case MazeStatus.closing_Unresolved:
                        status = MSG.GetWord("closing");
                        break;
                    case MazeStatus.opened_Unresolved:
                        status = MSG.GetWord("opened");
                        break;
                    case MazeStatus.opening_Unresolved:
                        status = MSG.GetWord("opening");
                        break;
                }
                graph.mazeField.text = MSG.GetName("maze") + VarG.twoPoints + status;
            }
        }
        public void UpdateTextColor_FromMaze() {
            UpdateTexts();
            UpdateColor();
        }
        public override void LockCamControl(bool a) {
            if (cam3D.rtsCam) { // au cas où RAZ entre temps
                cam3D.SetMouseEnable(!a);
            }
        }
    }
}
