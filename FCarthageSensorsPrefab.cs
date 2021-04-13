using UnityEngine;
using UnityEngine.UI;
using TMPro;
namespace IFSCL.Programs {
    public class FCarthageSensorsPrefab : FContentPrefab {
        public RawImage rawImage;
        public Animator animator;
        public Text energyValues;
        public TextMeshProUGUI zoomValue;
        public TextMeshProUGUI mazeField;
        public TextMeshProUGUI keyInfo;
        public Color keyNotFoundColor;
        public Color keyFoundColor;
        public Color mazeClosingColor;
        public GameObject rebootPanel;
        public void DoCameraAction(int ID) { //onclick
            ProgramsF.GetAByOS<PrgCarthageSensors>(OSTarget.SC).OnClickBtn_DoCameraAction(ID);
        }
    }
}