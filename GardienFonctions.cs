using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;
public class GardienFonctions : CreatureUnique {
	
	public LyokoGuerrier LGunderControl;
	private bool utilisePouvoir=false;
	
	public GardienFonctions() {
		nom = "gardien";
	}
    public override bool CouldSpawn() {
        return !created && CanBeUsed() && SavedPreferences.xanaGuardianUse.currentValue > 0;
    }
	public void RAZ(bool depuisDevirt=false){
		Debug.Log("razGardien");
		if (depuisDevirt){
			razClassique();
		}
		if (LGunderControl!=null && LGunderControl.IsVirt()){
			LGunderControl.GetGuide().DoParalyze(false);
			Grouping.Degrouper(LyokoGuide.GetByID(LyokoGuide.IdentifierGuide("gardien")));
			LGunderControl.GetGuide().EnableRvoController(true);
			LGunderControl=null;
		}
		utilisePouvoir=false;
		razMinimum();
	}
	public bool isUsingItsPower(){
		return utilisePouvoir;
	}
	public bool TrySetLGcontrol(LyokoGuide LG) {
        if (LG.nom == "franz" || !this.created || LG.lgType!=LgTypes.LyokoGuerrier || LyokoGuerrier.GetByName(LG.nom).controleXana)
            return false;
        if (isUsingItsPower())
            return false;
        if (LG._currentVehicule)
            LG.Descendre_vehicule();
        LGunderControl =LyokoGuerrier.GetByName(LG.nom);
		utilisePouvoir=true;
		LG.DoParalyze(true);
		LG.lgTransition = LgTransitions.nothing;
		LyokoGuide guideGardien = LyokoGuide.GetByID (LyokoGuide.IdentifierGuide ("gardien"));
		guideGardien.StopMove(true);
		LG.EnableRvoController(false);
        LG.DeleteObjectif();
		LG.transform.DOMove(guideGardien.transform.position,0f);
		Grouping.grouper(guideGardien, LG);
        return true;
	}
}