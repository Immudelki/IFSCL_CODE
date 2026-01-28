using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace IFSCL.Programs {
    public class FCarthageSensorsPrefab : FContentPrefab {
        public RawImage rawImage;
        public Animator animator;
        public TextMeshProUGUI zoomValue, mazeField, keyInfo;
        public Color keyNotFoundColor, keyFoundColor, mazeClosingColor;
        public GameObject rebootPanel;
        public ArrowsMapDisplay arrowsMapDisplay;
        public MousePressDetector mousePressDetector;
        public void DoCameraAction(int ID) { //onclick
            ProgramsF.GetAByOS<PrgCarthageSensors>(OSTarget.SC).OnClickBtn_DoCameraAction(ID);
        }
        public void Update() {
            if (RAZ.isRestarting)
                return;
            arrowsMapDisplay.DoUpdate(logicAction.GetCam3D().rtsCam, logicAction.GetCam3D().rtsCamMouse.enabled && mousePressDetector.mousePressed);
        }
    }
}
