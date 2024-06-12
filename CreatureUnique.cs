using System.Collections.Generic;
using IFSCL.Save;
using Sirenix.OdinInspector;
using UnityEngine;
namespace IFSCL.VirtualWorld {
    public class CreatureUnique : MonoBehaviour {
        public bool created;
        [SerializeField] public TimeUnitDateManager time_TillReuse = new TimeUnitDateManager();
        public string nom = "unknown";
        public static List<CreatureUnique> liste = new List<CreatureUnique>();
        public void Awake() {
            liste.Add(this);
        }
        public virtual void Init() {
        }
        public static void RAZ_FromRestart() {
            liste.Clear();
        }
        public virtual void OnVirt() {
            if (created) {
                MSG.AffCriticalInfo("unique creature " + nom + " already created, a duplicate spawn will cause issues");
            }
            created = true;
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
            foreach (CreatureUnique cu in liste) {
                if (index < timeUniqueCreatures.Count) {
                    cu.time_TillReuse.LoadFrom_Savegame(timeUniqueCreatures[index]);
                    DebugLogList.LogUniqueCreature(cu.nom,"SetTimeTillReuse from save",cu.gameObject);
                    index++;
                }
            }
        }
    }
}
