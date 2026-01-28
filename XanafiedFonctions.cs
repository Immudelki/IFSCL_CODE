using IFSCL.Save;
using Sirenix.OdinInspector;
namespace IFSCL.VirtualWorld {
    public class XanafiedFonctions : CreatureUnique {
        public bool allyCalledOncePerVirt;
        public bool allyCalledOncePerTranslation;
        public LyokoGuerrier linkedLW;
        public string Save() {
            string save = "[C]";
            save += "/" + TxtFileRecorder.ToString(allyCalledOncePerVirt);
            save += "/" + TxtFileRecorder.ToString(allyCalledOncePerTranslation);
            //save += "/" + GameSaveManager.ToString(linkedLG);
            return save;
        }
        public XanafiedFonctions() {
            nom = "undefined xanafiedLyokoWarrior";
        }
        public void SetName(string a) {
            nom = a;
        }
        public override void OnVirt(params object[] arg) {
            allyCalledOncePerVirt = false;
            created = true;
            VarG.xanaPocketliste.Remove(linkedLW.character);
        }
        public override void OnTranslation(params object[] arg) {
            allyCalledOncePerTranslation = false;
        }
        public override void RazClassique() {
            base.RazClassique();
            RazMinimum();
        }
        public override void RazMinimum() {
            base.RazMinimum();
            allyCalledOncePerVirt = false;
            allyCalledOncePerTranslation = false;
        }
        [Button]
        public void SummonBlackCreature() {
            if (linkedLW.IsTranslatedOnReplika()) {
                RealWorld.TowerAttack.replikaEnemiesCreatedByTower.Add(LyokoGuide.Create(Lex.kankrelat2D, VirtOrigin.translation, 
                    CRDManager.C(VarG.replikaParam, "replikaMonsterSpot", false), 0,
                    false, false, null, ReplikaGuide.GetByLGuide(linkedLW.GetGuide())));
                RealWorld.TowerAttack.replikaEnemiesCreatedByTower.Add(LyokoGuide.Create(Lex.kankrelat2D, VirtOrigin.translation, 
                    CRDManager.C(VarG.replikaParam, "replikaMonsterSpot", false), 0,
                    false, false, null, ReplikaGuide.GetByLGuide(linkedLW.GetGuide())));
                allyCalledOncePerTranslation = true;
                return;
            }
            LyokoGuide.Create(Lex.mantaBlack, VirtOrigin.classic, CRDManager.C(linkedLW.GetGuide(), false));
            allyCalledOncePerVirt = true;
        }
        public void OnXanafie(bool isVirt, bool isDefinitif, CharacterBase character) {
            if (isVirt) {
                created = true;
                return;
            }
            if (isDefinitif && !VarG.xanaPocketliste.Contains(character))
                VarG.xanaPocketliste.Add(character);
        }
    }
}
