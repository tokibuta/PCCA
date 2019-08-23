using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RecycleObject : MonoBehaviour {

    private bool destroyFlag = false;
	//外部から強制的にリサイクルするフラグ
	private bool recycleStartNow = false;
	public bool RecycleStartNow{
		set{ recycleStartNow = value; }
	}


    private float delayTime;
    private Transform tmpTransform;
	
	void Start () {
		tmpTransform = this.transform;
	}
	
	void Update () {
		if(RecycleCheck() || recycleStartNow){

			destroyFlag = false;
			recycleStartNow = false;
			
			this.GetComponent<MeshFilter>().sharedMesh = null;
			this.GetComponent<MeshRenderer>().material = null;
			
			//ラインオブジェクトから削除する
			Destroy(this.GetComponent<Rigidbody2D>());
			Destroy(this.GetComponent<PolygonCollider2D>());

			this.transform.position = Vector3.zero;
			this.transform.rotation = new Quaternion(0, 0, 0, 0);
			
			// Debug.Log("Active false" + this.name);

			this.gameObject.SetActive(false);
		}
	}

	private bool RecycleCheck(){
		return (destroyFlag && Time.time > delayTime + 7 && tmpTransform.position.x < VariableHolder.Instance.HeroPosition.x);
	}
	
    void OnBecameInvisible()
    {
        if(this.gameObject.activeSelf){
            try{
                destroyFlag = true;
                delayTime = Time.time;
            }catch(System.Exception e){
                // // Debug.Log(e);
            }
        }
    }
}
