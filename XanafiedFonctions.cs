using UnityEngine;
using System.Collections.Generic;
public class XanafiedFonctions : CreatureUnique
{
    public bool allyCalledOncePerVirt = false;
    public LyokoGuerrier linkedLG;

    public string Save() {
        string save = "[C]";
        save += "/" + GameSaveManager.ToString(allyCalledOncePerVirt);
        save += "/" + GameSaveManager.ToString(linkedLG);
        return save;
    }

    public XanafiedFonctions() {
        nom = "undefined xanafiedLyokoWarrior";
    }
    public void SetName(string a) {
        nom = a;
    }
    public override void razMinimum() {
        base.razMinimum();
        allyCalledOncePerVirt = false;
    }
    public override void OnVirt() {
        allyCalledOncePerVirt = false;
        created = true;
        VarG.xanaPocketliste.Remove(linkedLG.nom);
    }
    public void RAZ(bool depuisDevirt = false) {
        if (depuisDevirt) {
            razClassique();
        }
        razMinimum();
    }
}