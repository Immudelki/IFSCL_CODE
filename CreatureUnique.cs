using System.Collections.Generic;
using System.Linq;
using IFSCL.Save;
using Sirenix.OdinInspector;
using UnityEngine;
namespace IFSCL.VirtualWorld {
    public class CreatureUnique : MonoBehaviour {
        public bool created;
        [SerializeField] public TimeUnitDateManager time_TillReuse = new(RttpResponse.autoRemoveUnits);
        public string nom = "unknown";
        public static List<CreatureUnique> liste = new();
        public void Awake() {
            liste.Add(this);
        }
        public virtual void Init() {
        }
        public static void RAZ_FromRestart() {
            liste.Clear();
        }
        public virtual void OnVirt(params object[] args) {
            if (created) {
                MSG.AffCriticalInfo("unique creature " + nom + " already created, a duplicate spawn will cause issues");
            }
            created = true;
        }
        public virtual void OnTranslation(params object[] args) {
        }
        [Button]
        public virtual void RazClassique() {
            LyokoGuide.GetByName(nom)?.Devirtualiser();
        }
        [Button]
        public virtual void RazMinimum() {
            if (!created)
                return;
            created = false;
            if (GetType() == typeof(GardienFonctions)) {
                DebugLogList.LogUniqueCreature(nom,"SetTimeTillReuse: 24",this.gameObject);
                time_TillReuse.Set(24, AgendaTimeMarkerType.guardianCanBeReused);
            } else if (GetType() == typeof(PolymorphFonctions)) {
                DebugLogList.LogUniqueCreature(nom,"SetTimeTillReuse: 12",this.gameObject);
                time_TillReuse.Set(12, AgendaTimeMarkerType.polymorphCanBeReused);
            } else if (GetType() == typeof(XanafiedFonctions)) {
                DebugLogList.LogUniqueCreature(nom,"SetTimeTillReuse: 24",this.gameObject);
                time_TillReuse.Set(24, AgendaTimeMarkerType.lwXanafiedReuse, nom);
            } else if (GetType() == typeof(MeduseFonctions)) {
                DebugLogList.LogUniqueCreature(nom,"SetTimeTillReuse: 12",this.gameObject);
                time_TillReuse.Set(12, AgendaTimeMarkerType.scyphozoaCanBeReused, nom);
            } else {
                DebugLogList.LogUniqueCreature(nom,"SetTimeTillReuse: 24",this.gameObject);
                time_TillReuse.Set(24);
            }
        }
        [Button]
        public virtual bool CanSpawn() {
            return !time_TillReuse.IsRUNNING();
        }
        public void RazCompteur() {
            time_TillReuse.DESACTIVER();
        }
        public static void Load_FromSavegame(List<TimeUnitDateSubSaveData> timeUniqueCreatures) {
            if (timeUniqueCreatures == null)
                return;
            int index = 0;
            foreach (CreatureUnique cu in liste.Where(cu => index < timeUniqueCreatures.Count)) {
                cu.time_TillReuse.LoadFrom_Savegame(timeUniqueCreatures[index]);
                DebugLogList.LogUniqueCreature(cu.nom,"SetTimeTillReuse from save",cu.gameObject);
                index++;
            }
        }
        public static void DevirtRAZCheck(LyokoGuide _g) {
            if (VarG.meduseFonction.capturedLw != null && VarG.meduseFonction.capturedLw == LyokoGuerrier.GetByName(_g.nom)) {
                VarG.meduseFonction.RAZ();
            }
            
            if (_g.noVirtZone != null)
                Destroy(_g.noVirtZone.gameObject);
            
            if (_g.IsNamed(Lex.meduse)) {
                VarG.meduseFonction.RAZ();
            } else if (_g.IsNamed(Lex.gardien)) {
                VarG.gardienFonction.RAZ();
            } else if (_g.IsNamed(Lex.kolosse)) {
                VarG.kolosseFonction.RAZ();
            } else if (_g.IsNamed(Lex.kalamar)) {
                VarG.kalamarFonction.RAZ();
            } else if (_g.IsNamed(Lex.clone_polymorphe)) {
                VarG.polymorphFonction.RAZ();
            }
        }
    }
}
