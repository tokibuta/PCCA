using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PenManager : MonoBehaviour
{

	private Transform penTransform1;
	private Rigidbody2D penTransform;

	//ペンの軌道エフェクト関係
	private ParticleSystem effect;

	// private GameObject effectObject;
	//エフェクトの生成タイミングを調整
	// private int effectCount = 0;

	//ペン衝突判定
	private bool penCollision = false;

	void Awake()
	{
		//初期化
		VariableHolder.Instance.DeleteUpdateBook();
	}
	
	void Start()
	{
		penTransform1 = this.transform;
		penTransform = this.GetComponent<Rigidbody2D>();
		effect = this.GetComponent<ParticleSystem>();

		//シーンをロードしたときに初期化する
		VariableHolder.Instance.PenInObject = true;
	}

	void Update()
	{
		return;
		var touch = CrossInput.GetAction();
		if(!penCollision && touch == CrossInput.Action.Began)
		{

			//ライン生成フラグ
			VariableHolder.Instance.CreateLineFlag = true;

			var position = CrossInput.GetPosition();
			position.z = -Camera.main.transform.position.z;
			// マウス位置座標をスクリーン座標からワールド座標に変換する
			// penTransform.position = Camera.main.ScreenToWorldPoint(position);
			penTransform1.position = Camera.main.ScreenToWorldPoint(position);
			

			//ペン座標を共有に保存
			VariableHolder.Instance.PenPos = penTransform1.position;

			if(!effect.isPlaying)
			{
				effect.Play();
			}

		}
		else if(!penCollision && touch == CrossInput.Action.Moved)
		{

			var position = CrossInput.GetPosition();
			position.z = -Camera.main.transform.position.z;
			// マウス位置座標をスクリーン座標からワールド座標に変換する
			penTransform.position = Camera.main.ScreenToWorldPoint(position);

			//ペン座標を共有に保存
			VariableHolder.Instance.PenPos = penTransform.position;

			if(!effect.isPlaying)
			{
				effect.Play();
			}

		}
		else if(penCollision || touch == CrossInput.Action.Ended)
		{

			VariableHolder.Instance.CreateLineFlag = false;

			penTransform1.position = new Vector3(-20, 0, 0);

			if(effect.isPlaying)
			{
				effect.Stop();
			}
		}
	}

	void OnCollisionEnter2D(Collision2D other)
	{
		penCollision = true;
	}

	void OnCollisionStay2D(Collision2D other)
	{
		penCollision = true;
	}

	void OnCollisionExit2D(Collision2D other)
	{
		penCollision = false;
	}

	void OnTriggerEnter2D(Collider2D other)
	{
		// // Debug.Log("set lineCreate = false : " + this.name);
		VariableHolder.Instance.PenInObject = false;
	}

	void OnTriggerExit2D(Collider2D other)
	{
		// // Debug.Log("set lineCreate = true : " + this.name);
		VariableHolder.Instance.PenInObject = true;
	}

	//パーティクルシステムのアタッチされたオブジェクトを生成する手法
	//Penオブジェクトにパーティクルシステムをアタッチして
	//Simulation Space を world にすることで解決
	// private void LineEffect(GameObject gameObj, Transform effectTransform){

	// 	//３回に１回生成する
	// 	if(effectCount == 2){

	// 		var tmp = Instantiate(gameObj, penTransform.position, penTransform.rotation);
	// 		Destroy(tmp, 1.0f);

	// 		effectCount = 0;
	// 	}else{
			
	// 		effectCount++;
	// 		return;
	// 	}
	// }
}
