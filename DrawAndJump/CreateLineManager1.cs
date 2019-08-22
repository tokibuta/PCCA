using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityStandardAssets.CrossPlatformInput;

public class CreateLineManager1 : MonoBehaviour {

	//MonoBehaviourを継承している場合に new キーワードを使うと警告が出るためインナークラスを使用
	static class ObjectPool{

		//インナークラスフィールドはLoadScene後にもデータが残っている(staticだから？)
		private static List<GameObject> objList;
		private static GameObject obj;
		private static GameObject newObj;
		private static GameObject parent;

		/// <summary>
		///	オブジェクトプールを作成する。
		/// InitPool(プールするオブジェクト, サイズ, 親オブジェクト)
		/// </summary>
		public static void InitPool(GameObject tmpObj, int initSize, GameObject tmpParent){
			objList = new List<GameObject>();
			parent = tmpParent;
			obj = tmpObj;
			for(int i = 0; i < initSize; i++){
				CreateObject();
			}
		}

		private static void CreateObject(){
			newObj = Instantiate(obj, parent.transform);
			newObj.name = newObj.name + (objList.Count + 1);
			newObj.SetActive(false);
			objList.Add(newObj);
		}

		public static GameObject GetObject(){
			for(int i = 0; i < objList.Count; i++){
				if(!objList[i].activeSelf){
					objList[i].SetActive(true);
					// // Debug.Log(objList[i]);
					return objList[i];
				}
			}

			CreateObject();
			newObj.SetActive(true);
			// // Debug.Log(newObj);
			return newObj;
		}

		public static void Recycle(){
			for(int i = 0; i < objList.Count; i++){
				if(objList[i].activeSelf){
					objList[i].GetComponent<RecycleObject>().RecycleStartNow = true;
				}
			}
		}
	}

	[SerializeField]
	private GameObject lineObject;
	[SerializeField]
	private GameObject parent;

	// private ObjectPool objectPool;
    private const int initSize = 10;

	//ゲージ関係
	[SerializeField]
	private float maxTimeLimit = 400;
	private float timeLimit;
	
	[SerializeField]
	private float timePlus = 4f;
	[SerializeField]
	private float timeMinus = 8f;

	[SerializeField]
	private GameObject gaugeObject;
	private Image gaugeImage;

	//ライン関係
	[SerializeField]
    private Material mLineMaterial;
	
	[SerializeField]
	private PhysicsMaterial2D mPhysicsMaterial;
	[SerializeField]
	private GameObject VirtualPen;

	/*******************************************
	 マルチタッチ実装により一時変数を辞書型で実装
	*******************************************/
    private Dictionary<int, Mesh> mesh = new Dictionary<int, Mesh>();
    private Dictionary<int, List<Vector3>> points = new Dictionary<int, List<Vector3>>();
    private Dictionary<int, List<Vector3>> vertices = new Dictionary<int, List<Vector3>>();
    private Dictionary<int, List<Vector2>> uvs = new Dictionary<int, List<Vector2>>();
    private Dictionary<int, List<int>> triangles = new Dictionary<int, List<int>>();
    private Dictionary<int, float> uvOffset = new Dictionary<int, float>();
    private Dictionary<int, int> index = new Dictionary<int, int>();
	
	//次にラインを適用するオブジェクト
	private Dictionary<int, GameObject> nextObject = new Dictionary<int, GameObject>();
	//nextObjectの情報をセットする変数
	private Dictionary<int, MeshFilter> nextObjectMesh = new Dictionary<int, MeshFilter>();
	private Dictionary<int, MeshRenderer> nextObjectMaterial = new Dictionary<int, MeshRenderer>();
	//nextObjectに情報をセットするフラグ
	private Dictionary<int, bool> setInfoObject = new Dictionary<int, bool>();
	//ペン衝突判定
	private Dictionary<int, bool> penCollision = new Dictionary<int, bool>();

	private Dictionary<int, CrossInput.Action> beforeAction = new Dictionary<int, CrossInput.Action>();
	private Dictionary<int, bool> beforeCollision = new Dictionary<int, bool>();

	//指の数だけの仮想ペン
	private Dictionary<int, GameObject> virtualPenObject = new Dictionary<int, GameObject>();
	private Dictionary<int, Transform> virtualPenTransform = new Dictionary<int, Transform>();
	private Dictionary<int, ParticleSystem> virtualPenParticle = new Dictionary<int, ParticleSystem>();
	/****
	 end
	****/

	[SerializeField]
    private float penSize = 0.05f;
    // private float penSize = 0.6f;
	
	[SerializeField]
    private float texChangeSpeed = 0.02f;
    // private float texChangeSpeed = 0.18f;

	//メッシュを一定間隔で設定するための変数
	[SerializeField]
	private float updateValue = 0.5f;
	private float updateValueSum = 0;


	//meshを作成するかフラグ
	private bool create = false;

	//ライン生成開始時のワンタイムフラグ
	private bool oneTime = true;

	private Transform penTransform;
	private Rigidbody2D penTransformRb;

	void Start () {
		
		VariableHolder.Instance.PauseLine = false;
		VariableHolder.Instance.BombCount = 0;

		//ゲージ関係の初期化
		gaugeImage = gaugeObject.GetComponent<Image>();
		gaugeImage.fillAmount = 1;
		timeLimit = maxTimeLimit;

		//オブジェクトプールを初期化
		ObjectPool.InitPool(lineObject, initSize, parent);

		//ペン関係
		penTransform = this.transform;
		penTransformRb = this.GetComponent<Rigidbody2D>();
	}
	
	void Update () {

		if(VariableHolder.Instance.PauseLine){
			return;
		}

		//ボムボタン押下時
		if(CrossPlatformInputManager.GetButtonUp("Fire2")){
			//ObjectPoolのリストに乗っているオブジェクトをすべてにリサイクル処理を実施
			ObjectPool.Recycle();

			//ボムをカウント
			VariableHolder.Instance.BombCount++;

			//ゲージをすべて消費
			timeLimit = 0;
		}

		//2019/03/15追記 外部からの要因によりゲージを増減させる処理
		switch(VariableHolder.Instance.GaugeCommand){
			case 0:
				//今の所何もしない
				break;

			case 1:
				//ゲージを増加
				if(timeLimit < 400){
					timeLimit += timePlus;
				}
				break;

			case -1:
				//ゲージを減少
				// Debug.Log("minus");
				timeLimit -= timeMinus;
				if(timeLimit <= 0){
					timeLimit = 0;
				}
				break;
		}

		//ゲージの増減
		gaugeImage.fillAmount = timeLimit / maxTimeLimit;

		// //追記20181128
		// var touch = CrossInput.GetAction();
		// var position = CrossInput.GetPosition();

		CrossInput.Data[] myTouches = CrossInput.GetData();
		if(myTouches != null)
		{
			// Touch[] myTouches = Input.touches;
			for(int i = 0; i < CrossInput.currentDataLength; i++)
			{

				Vector3 position = myTouches[i].GetPosition();
				position.z = -Camera.main.transform.position.z;

				// マウス位置座標をスクリーン座標からワールド座標に変換する
				if(!virtualPenTransform.ContainsKey(myTouches[i].GetFingerId()))
				{
					//エフェクト生成
					virtualPenObject[myTouches[i].GetFingerId()] = Instantiate<GameObject>(
						VirtualPen,
						VirtualPen.transform.position,
						VirtualPen.transform.rotation,
						this.transform
					);
					//Transformを設定
					virtualPenTransform[myTouches[i].GetFingerId()] = virtualPenObject[myTouches[i].GetFingerId()].transform;

					//パーティクルを設定
					virtualPenParticle[myTouches[i].GetFingerId()] = virtualPenObject[myTouches[i].GetFingerId()].GetComponent<ParticleSystem>();
				}
				virtualPenTransform[myTouches[i].GetFingerId()].position = Camera.main.ScreenToWorldPoint(position);

				//ペンレイ
				var rayTop = new Ray2D(
					new Vector3(
						virtualPenTransform[myTouches[i].GetFingerId()].position.x - 0.05f,
						virtualPenTransform[myTouches[i].GetFingerId()].position.y + 0.05f,
						0
					),
					new Vector2(0.5f, 0)
				);
				var rayBottom = new Ray2D(
					new Vector3(
						virtualPenTransform[myTouches[i].GetFingerId()].position.x - 0.05f,
						virtualPenTransform[myTouches[i].GetFingerId()].position.y - 0.05f,
						0
					),
					new Vector2(0.5f, 0)
				);
				var rayRight = new Ray2D(
					new Vector3(
						virtualPenTransform[myTouches[i].GetFingerId()].position.x - 0.05f,
						virtualPenTransform[myTouches[i].GetFingerId()].position.y - 0.05f,
						0
					),
					new Vector2(0, 0.5f)
				);
				var rayLeft = new Ray2D(
					new Vector3(
						virtualPenTransform[myTouches[i].GetFingerId()].position.x + 0.05f,
						virtualPenTransform[myTouches[i].GetFingerId()].position.y - 0.05f,
						0
					),
					new Vector2(0, 0.5f)
				);

				// Debug.DrawRay(rayTop.origin, new Vector2(0.1f, 0), Color.red);
				// Debug.DrawRay(rayBottom.origin, new Vector2(0.1f, 0), Color.green);
				// Debug.DrawRay(rayRight.origin, new Vector2(0, 0.1f), Color.yellow);
				// Debug.DrawRay(rayLeft.origin, new Vector2(0, 0.1f), Color.blue);

				RaycastHit2D hitTop = Physics2D.Raycast(rayTop.origin, new Vector2(0.5f, 0), 0.1f);
				RaycastHit2D hitBottom = Physics2D.Raycast(rayBottom.origin, new Vector2(0.5f, 0), 0.1f);
				RaycastHit2D hitRight = Physics2D.Raycast(rayRight.origin, new Vector2(0, 0.5f), 0.1f);
				RaycastHit2D hitLeft = Physics2D.Raycast(rayLeft.origin, new Vector2(0, 0.5f), 0.1f);

				if(hitTop.collider == null && hitBottom.collider == null && hitRight.collider == null && hitLeft.collider == null)
				{
					penCollision[myTouches[i].GetFingerId()] = false;
				}
				else
				{
					penCollision[myTouches[i].GetFingerId()] = true;
					virtualPenTransform[myTouches[i].GetFingerId()].position = new Vector3(0, 0, 0);
					// // Debug.Log("pne collision");
				}

				if(!penCollision[myTouches[i].GetFingerId()] && myTouches[i].GetPhase() == CrossInput.Action.Began)
				{
					//仮想ペンエフェクト開始
					if(!virtualPenParticle[myTouches[i].GetFingerId()].isPlaying){
						virtualPenParticle[myTouches[i].GetFingerId()].Play();
					}

					// Debug.Log("id: " + myTouches[i].GetFingerId() + " : Began");

					//コライダーを設定する
					setInfoObject[myTouches[i].GetFingerId()] = true;

					//ゲージを減らす
					timeLimit -= timeMinus;
					if(timeLimit <= 0){
						timeLimit = 0;
						return;
					}

					Init(myTouches[i].GetFingerId(), virtualPenTransform[myTouches[i].GetFingerId()].position);

				}
				else if(!penCollision[myTouches[i].GetFingerId()] && myTouches[i].GetPhase() == CrossInput.Action.Moved)
				{
					// Debug.Log("id: " + myTouches[i].GetFingerId() + " : Moved");

					//ペンが障害物から外に出たときにオブジェクトを作成し直すようにする
					//beganのタイミングでbeforeActionが登録されているためエラーにはならない(理論的に);
					if(beforeAction.ContainsKey(myTouches[i].GetFingerId()) && beforeCollision.ContainsKey(myTouches[i].GetFingerId()))
					{
						if(beforeAction[myTouches[i].GetFingerId()] == myTouches[i].GetPhase() && beforeCollision[myTouches[i].GetFingerId()])
						{
							//仮想ペンエフェクト開始
							if(!virtualPenParticle[myTouches[i].GetFingerId()].isPlaying){
								virtualPenParticle[myTouches[i].GetFingerId()].Play();
							}
							
							//コライダーを設定する
							setInfoObject[myTouches[i].GetFingerId()] = true;
							Init(myTouches[i].GetFingerId(), virtualPenTransform[myTouches[i].GetFingerId()].position);
						}

						//ゲージを減らす
						timeLimit -= timeMinus;
						if(timeLimit <= 0)
						{
							//仮想ペンエフェクト停止
							if(virtualPenParticle[myTouches[i].GetFingerId()].isPlaying){
								virtualPenParticle[myTouches[i].GetFingerId()].Stop();
							}
							
							timeLimit = 0;
							ChangeObject(myTouches[i].GetFingerId());
							return;
						}

						if(virtualPenTransform.TryGetValue(myTouches[i].GetFingerId(), out Transform transform)){
							CreateMesh(myTouches[i].GetFingerId(), transform.position);
						}
					}
					else
					{
						return;
					}
				}
				else if(penCollision[myTouches[i].GetFingerId()] || myTouches[i].GetPhase() == CrossInput.Action.Ended)
				{
					//仮想ペンエフェクト停止
					if(virtualPenParticle[myTouches[i].GetFingerId()].isPlaying){
						virtualPenParticle[myTouches[i].GetFingerId()].Stop();
					}

					// Debug.Log("id: " + myTouches[i].GetFingerId() + " : Ended");
					if(timeLimit < 400){
						timeLimit += timePlus;
					}
					ChangeObject(myTouches[i].GetFingerId());
				}
				else if(myTouches[i].GetPhase() == CrossInput.Action.None)
				{
					if(timeLimit < 400){
						timeLimit += timePlus;
					}
				}

				//アクション, コリジョン状況を保存(次のフレームで使用)
				beforeAction[myTouches[i].GetFingerId()] = myTouches[i].GetPhase();
				beforeCollision[myTouches[i].GetFingerId()] = penCollision.TryGetValue(myTouches[i].GetFingerId(), out bool result) ? result : true;
			}
		}
		else
		{
			if(timeLimit < 400){
				timeLimit += timePlus;
			}
		}
	}

    //Mesh関係初期化と第一ポイントの設定
    private void Init(int fingerId, Vector3 point){

		create = true;

		//設定するオブジェクトを取得
		nextObject[fingerId] = ObjectPool.GetObject();
		// Debug.Log("id: " + fingerId + " : " + nextObject[fingerId]);
		// Debug.Log(nextObject.name);

		mesh[fingerId] = new Mesh();

		uvs[fingerId] = new List<Vector2>();
		vertices[fingerId] = new List<Vector3>();
		points[fingerId] = new List<Vector3>();
		triangles[fingerId] = new List<int>();

        index[fingerId] = 0;
        uvOffset[fingerId] = 0;

        uvs[fingerId].Add(new Vector2(uvOffset[fingerId], 1));
        uvs[fingerId].Add(new Vector2(uvOffset[fingerId], 0));
        
        vertices[fingerId].Add(point);
        vertices[fingerId].Add(point);
        
        points[fingerId].Add(point);

		nextObjectMesh[fingerId] = nextObject[fingerId].GetComponent<MeshFilter>();
		nextObjectMaterial[fingerId] = nextObject[fingerId].GetComponent<MeshRenderer>();
    }

    private void CreateMesh(int fingerId, Vector3 point){

		if(!create){
			return;
		}

		//仮想ポイントが必要かチェック
		//必要な場合pointsに追加
		var vec = point - points[fingerId][points[fingerId].Count - 1];

		//設定する仮想ポイントの数
		int verPointCount = 0;

		if(Mathf.Abs(vec.x) >= Mathf.Abs(vec.y))
		{	
			// verPointCount = (int)((Mathf.Abs(vec.x) + updateValueSum) / updateValue);
			verPointCount = (int)(Mathf.Abs(vec.x) / updateValue);

			if(verPointCount >= 1)
			{
				// Debug.Log("before :" +points[fingerId][points[fingerId].Count - 1]);
				// Debug.Log("point: " + point);
				// Debug.Log("vec.x : " + vec.x);
				for(int i = 1; i <= verPointCount; i++)
				{
					//xがマイナスか判定
					if(vec.x < 0 && vec.y < 0)
					{
						//Xの仮想ポイント
						var verX = points[fingerId][points[fingerId].Count - 1].x - updateValue;
						//XとupdateValueから割合を求めYを割合分移動したYの仮想ポイント
						var verY = points[fingerId][points[fingerId].Count - 1].y - (1 - ((Mathf.Abs(vec.x) - updateValue) / Mathf.Abs(vec.x))) * Mathf.Abs(vec.y);

						//pointsに追加
						points[fingerId].Add(new Vector3(verX, verY, point.z));
					}
					else if(vec.x > 0 && vec.y > 0)
					{
						//Xの仮想ポイント
						var verX = points[fingerId][points[fingerId].Count - 1].x + updateValue;
						//XとupdateValueから割合を求めYを割合分移動したYの仮想ポイント
						var verY = points[fingerId][points[fingerId].Count - 1].y + (1 - ((Mathf.Abs(vec.x) - updateValue) / Mathf.Abs(vec.x))) * Mathf.Abs(vec.y);

						//pointsに追加
						points[fingerId].Add(new Vector3(verX, verY, point.z));
					}
					else if(vec.x < 0 && vec.y > 0)
					{
						//Xの仮想ポイント
						var verX = points[fingerId][points[fingerId].Count - 1].x - updateValue;
						//XとupdateValueから割合を求めYを割合分移動したYの仮想ポイント
						var verY = points[fingerId][points[fingerId].Count - 1].y + (1 - ((Mathf.Abs(vec.x) - updateValue) / Mathf.Abs(vec.x))) * Mathf.Abs(vec.y);

						//pointsに追加
						points[fingerId].Add(new Vector3(verX, verY, point.z));
					}
					else if(vec.x > 0 && vec.y < 0)
					{
						//Xの仮想ポイント
						var verX = points[fingerId][points[fingerId].Count - 1].x + updateValue;
						//XとupdateValueから割合を求めYを割合分移動したYの仮想ポイント
						var verY = points[fingerId][points[fingerId].Count - 1].y - (1 - ((Mathf.Abs(vec.x) - updateValue) / Mathf.Abs(vec.x))) * Mathf.Abs(vec.y);

						//pointsに追加
						points[fingerId].Add(new Vector3(verX, verY, point.z));
					}
					// Debug.Log("verpoint" + i + " : " + points[fingerId][points[fingerId].Count - 1]);
				}
			}
			else
			{
				verPointCount = 1;
			}
			updateValueSum += Mathf.Abs(vec.x);
		}
		else
		{
			// verPointCount = (int)((Mathf.Abs(vec.y) + updateValueSum) / updateValue);
			verPointCount = (int)(Mathf.Abs(vec.y) / updateValue);

			if(verPointCount >= 1)
			{
				// Debug.Log("before :" +points[fingerId][points[fingerId].Count - 1]);
				// Debug.Log("point: " + point);
				// Debug.Log("vec.y : " + vec.y);
				for(int i = 1; i <= verPointCount; i++)
				{
					//xがマイナスか判定
					if(vec.y < 0 && vec.x < 0)
					{
						//Yの仮想ポイント
						var verY = points[fingerId][points[fingerId].Count - 1].y - updateValue;
						//YとupdateValueから割合を求めXを割合分移動したXの仮想ポイント
						var verX = points[fingerId][points[fingerId].Count - 1].x - (1 - ((Mathf.Abs(vec.y) - updateValue) / Mathf.Abs(vec.y))) * Mathf.Abs(vec.x);

						//pointsに追加
						points[fingerId].Add(new Vector3(verX, verY, point.z));
					}
					else if(vec.y > 0 && vec.x > 0)
					{
						//Yの仮想ポイント
						var verY = points[fingerId][points[fingerId].Count - 1].y + updateValue;
						//YとupdateValueから割合を求めXを割合分移動したXの仮想ポイント
						var verX = points[fingerId][points[fingerId].Count - 1].x + (1 - ((Mathf.Abs(vec.y) - updateValue) / Mathf.Abs(vec.y))) * Mathf.Abs(vec.x);

						//pointsに追加
						points[fingerId].Add(new Vector3(verX, verY, point.z));
					}
					else if(vec.y < 0 && vec.x > 0)
					{
						//Yの仮想ポイント
						var verY = points[fingerId][points[fingerId].Count - 1].y - updateValue;
						//YとupdateValueから割合を求めXを割合分移動したXの仮想ポイント
						var verX = points[fingerId][points[fingerId].Count - 1].x + (1 - ((Mathf.Abs(vec.y) - updateValue) / Mathf.Abs(vec.y))) * Mathf.Abs(vec.x);

						//pointsに追加
						points[fingerId].Add(new Vector3(verX, verY, point.z));
					}
					else if(vec.y > 0 && vec.x < 0)
					{
						//Yの仮想ポイント
						var verY = points[fingerId][points[fingerId].Count - 1].y + updateValue;
						//YとupdateValueから割合を求めXを割合分移動したXの仮想ポイント
						var verX = points[fingerId][points[fingerId].Count - 1].x - (1 - ((Mathf.Abs(vec.y) - updateValue) / Mathf.Abs(vec.y))) * Mathf.Abs(vec.x);

						//pointsに追加
						points[fingerId].Add(new Vector3(verX, verY, point.z));
					}
					// Debug.Log("verpoint" + i + " : " + points[fingerId][points[fingerId].Count - 1]);
				}
			}
			else
			{
				verPointCount = 1;
			}
			updateValueSum += Mathf.Abs(vec.y);
		}

		Debug.Log("point : " + verPointCount);
		Debug.Log("残り(before) : " + updateValueSum);

        //新規ポイントを設定
        points[fingerId].Add(point);
	
		if(updateValueSum >= updateValue)
		{
			for(int i = 0; i < verPointCount; i++)
			{
				//updateValue分減算
				updateValueSum -= updateValue;

				//２ポイントからベクトルを取得し、長さを1とした新しいポイントを作成
				Vector2 tmpPoint = 
					(points[fingerId][points[fingerId].Count - (1 + verPointCount - i)]
						 - points[fingerId][points[fingerId].Count - (verPointCount - i)]
					).normalized;

				//90度回転させた２ポイント(内積がゼロになるためy, xを反転しマイナスとする)を作成する。長さ1にpenSizeを掛ける
				Vector2 tmpPintMinus = new Vector2(tmpPoint.y, -tmpPoint.x) * penSize + (Vector2)points[fingerId][points[fingerId].Count - (verPointCount - i)];
				Vector2 tmpPintPlus = new Vector2(-tmpPoint.y, tmpPoint.x) * penSize + (Vector2)points[fingerId][points[fingerId].Count - (verPointCount - i)];
				
				triangles[fingerId].Add(index[fingerId]);
				triangles[fingerId].Add(index[fingerId] + 2);
				triangles[fingerId].Add(index[fingerId] + 1);
				triangles[fingerId].Add(index[fingerId] + 2);
				triangles[fingerId].Add(index[fingerId] + 3);
				triangles[fingerId].Add(index[fingerId] + 1);
				
				uvs[fingerId].Add(new Vector2(uvOffset[fingerId], 0));
				uvs[fingerId].Add(new Vector2(uvOffset[fingerId], 1));
				
				vertices[fingerId].Add(tmpPintMinus);
				vertices[fingerId].Add(tmpPintPlus);

				//meshに設定する順番は決まっている
				mesh[fingerId].vertices = vertices[fingerId].ToArray();
				mesh[fingerId].uv = uvs[fingerId].ToArray();
				mesh[fingerId].triangles = triangles[fingerId].ToArray();
				
				nextObjectMesh[fingerId].sharedMesh = mesh[fingerId];
				nextObjectMaterial[fingerId].material = mLineMaterial;
				
				index[fingerId] += 2;

				uvOffset[fingerId] += texChangeSpeed;
			}
		}

		Debug.Log("残り : " + updateValueSum);
    }

    private void ChangeObject(int fingerId){
		create = false;

		//同時に同じmeshを使用した複数のオブジェクトができてしまうことを防ぐ
		if(setInfoObject.TryGetValue(fingerId, out bool result) ? result : false){
			setInfoObject[fingerId] = false;
			var rb = nextObject[fingerId].gameObject.AddComponent<Rigidbody2D>();
			rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

			SetCollider(fingerId);
		}
    }
	
    //Meshに合わせてPolygonCollider2Dを設定する
	//オブジェクトをリサイクルするときはPolygonCollider2Dを削除する必要がある
    private void SetCollider(int fingerId){
		
		// Debug.Log("id: " + fingerId + " : " + nextObject[fingerId] + " SetCollider");

        //すでにPolygonCollider2Dが設定されている　又は　MeshFilterがnullの時
        if(nextObject[fingerId].GetComponent<PolygonCollider2D>() || nextObject[fingerId].GetComponent<MeshFilter>() == null){
            return;
        }

        PolygonCollider2D polygonCollider = nextObject[fingerId].gameObject.AddComponent<PolygonCollider2D>();
        polygonCollider.sharedMaterial = mPhysicsMaterial;

        polygonCollider.pathCount = 1;

        //並び替えた配列を格納
        List <Vector2> pathList = new List<Vector2>();

        bool inc = true;
        int i = 0;
        
        //外周を沿うように並び替え
        try{
            while(true){
                if(i >= vertices[fingerId].Count){
                    i--;
                    pathList.Add(vertices[fingerId][i]);
                    inc = false;
                }else if(i == 1){
                    pathList.Add(vertices[fingerId][i]);
                    break;
                }else{
                    pathList.Add(vertices[fingerId][i]);
                }
                
                if(inc){
                    i += 2;
                }else{
                    i -= 2;
                }
            }
        }catch(System.Exception){
            // // Debug.Log("描き込み禁止エリアに描き込もうとしたためオブジェクトを破棄");
        }finally{
			polygonCollider.SetPath(0, pathList.ToArray());

			//すべての工程が終了し、ラインをオブジェクト化した後に行う初期化処理
			//nextObjectをnullにする
			nextObject[fingerId] = null;

			//fingerIdを削除する
			vertices.Remove(fingerId);
			points.Remove(fingerId);
			uvs.Remove(fingerId);
			triangles.Remove(fingerId);
			mesh.Remove(fingerId);
			uvOffset.Remove(fingerId);
			index.Remove(fingerId);
			beforeAction.Remove(fingerId);
			beforeCollision.Remove(fingerId);
			setInfoObject.Remove(fingerId);
			penCollision.Remove(fingerId);
			nextObject.Remove(fingerId);
			nextObjectMaterial.Remove(fingerId);
			nextObjectMesh.Remove(fingerId);

			//パーティクル削除
			virtualPenTransform.Remove(fingerId);
			virtualPenParticle.Remove(fingerId);
			//変更予定(仮想ペンをプールするように変更して負荷を減らす)
			Destroy(virtualPenObject[fingerId], 2f);
			virtualPenObject.Remove(fingerId);
		}
    }
}
