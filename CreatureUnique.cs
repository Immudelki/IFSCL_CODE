using UnityEngine;
using System.Collections.Generic;
using System;
public class CreatureUnique : MonoBehaviour{		
	public bool created=false;
    public DateTime minimumDateTime_TillReuse;
	public string nom="SANSnom";
    public int hoursBeforeReuse = 6;
	public static List<CreatureUnique>liste=new List<CreatureUnique>();
	public CreatureUnique() {
	}
    public virtual void OnVirt() {
        created = true;
    }
    public virtual bool CouldSpawn() {
        return false;
    }
    public void razClassique(){
		if (LyokoGuide.IdentifierGuide(nom)!=999)
			LyokoGuide.GetByID(LyokoGuide.IdentifierGuide(nom)).Devirtualiser();
	}
	public virtual void razMinimum(){
		created=false;
        minimumDateTime_TillReuse = VarG.fictionalTime.AddHours(hoursBeforeReuse);
	}
    public bool CanBeUsed() {
        return GetSpanTime_TillNextReuse().CompareTo(TimeSpan.Zero) <= 0;
    }
    public System.TimeSpan GetSpanTime_TillNextReuse() {
        System.TimeSpan tt = minimumDateTime_TillReuse - VarG.fictionalTime;
        return tt;
    }
	public void razCompteur(){
        minimumDateTime_TillReuse = VarG.fictionalTime;
	}
}