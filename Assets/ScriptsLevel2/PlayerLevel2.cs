﻿/* References */

using UnityEngine;
using System.Collections;
using UnityEngine.SocialPlatforms.Impl;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
//using UnityEditor.VersionControl;

public class PlayerLevel2 : MonoBehaviour {
	public Canvas GameOverCanvas;
	public Canvas NonBlockingPopupCanvas;

	/* Private Fields. */
	private Rigidbody2D myRigidBody;
	private GameObject bPrefab; 
	private Animator playerAnimator;


	private bool lastBoxOpened;
	// Scores
	private int hitScore;
	private int falseHitScore;
	private int lifeScore;
	private int boxesScore;
	// Movements.
	private bool flip;
	private bool isGround;
	private bool jump;
	private bool attack;
	private bool shoot;
	private bool slide;
	private bool freezePlayer;
	//private bool mapOn;


	/* Private Serialized. */
	[SerializeField]
	private int bulletForce;

	[SerializeField]
	private GameObject bulletPrefab;

	[SerializeField]
	private float playerSpeed;

	[SerializeField]
	private Transform[] groundPoints;

	[SerializeField]
	private float groundRadius;

	[SerializeField]
	private LayerMask whatIsGround;

	[SerializeField]
	private GameObject scoreText;

	[SerializeField]
	private AudioClip handgunSound;

	[SerializeField]
	private AudioClip footstep;

	[SerializeField]
	private AudioClip deadSound;

	[SerializeField]
	private AudioClip jumpSound;

	[SerializeField]
	private GameObject explodeAnimation;

	[SerializeField]
	private float jumpForce;

	//[SerializeField]
	//private Canvas mapCanvas;




	// Use this for initialization
	void Start () {
		//print(Application.loadedLevel);
		Debug.Log(SceneManager.GetActiveScene().name);
		hitScore = 0;
		falseHitScore = 0;
		lifeScore = 3;
		boxesScore = 20;
		flip = true;
		lastBoxOpened = false;
		freezePlayer = false;
		//mapOn = false;
		if(GameObject.Find ("ShowScene"))
			GameObject.Find ("ShowScene").GetComponent<Button> ().enabled = false;
		myRigidBody = GetComponent<Rigidbody2D> ();
		playerAnimator = GetComponent<Animator> ();
		//if(mapCanvas)
			//mapCanvas.enabled = false;
		if(GameOverCanvas)
			GameOverCanvas.enabled = false;

		scoreText.transform.GetChild (5).gameObject.SetActive (false);
		Vector3 msgpos;
		msgpos.x = gameObject.transform.position.x + 400f;
		msgpos.y = gameObject.transform.position.y + 200f;
		msgpos.z = gameObject.transform.position.z;
	}

	public void IncHitScore(){
		hitScore += 100;
	}

	public void DecHitScore(){
		hitScore -= 50;
	}

	public void IncFalseHitScore(){
		falseHitScore += 100;
	}

	public void DecFalseHitScore(){
		falseHitScore -= 50;
	}

	public void IncLife(){
		lifeScore++;
	}

	public void DecLife(){
		lifeScore--;
	}

	public void DecMines(){
		boxesScore--;
	}



	void Update(){
		HandleInput ();
	}


	public void ShowMap(){
		GetComponent<PauseMenu> ().isHint = true;
		//mapCanvas.enabled = true;
	}

	public void HideMap(){
		GetComponent<PauseMenuLevel2> ().isHint = false;
		//mapCanvas.enabled = false;
	}

	// Update is called once per frame
	// Fixed update helps keep updates same across platforms
	void FixedUpdate () {
		float horizontal = Input.GetAxis ("Horizontal");
		isGround = IsGrounded ();
		//Debug.Log (horizontal);
		HandleMoves(horizontal);
		FlipPlayer (horizontal);
		HandleAttacks ();
		HandleLayers ();
		UpdateScore ();
		ResetScene ();
	}
		
	public void RepositionHazel(Vector3 cameraPos, bool deadSoundPlay, string givenMsg){
		if(deadSoundPlay)
			SoundManagerLevel2.instance.PlaySingle (deadSound);
		gameObject.transform.position = cameraPos;
		string msg;
		if (givenMsg != null)
			msg = givenMsg + "\nYou have lost a life !";
		else
			msg = "Oops ! You have lost a life.\nOnly " + lifeScore + " lives remaining !";
		ShootNonBlockingPopup ( msg, 5f,"NONE");
	}


	public void ShootNonBlockingPopup(string message, float delay, string type){
		if (NonBlockingPopupCanvas) {
			NonBlockingPopupCanvas.GetComponent<NonBlockingPopupScriptLevel2> ().showPopup (
				Vector3.zero, message, "NotationsSprites/Others/hazel", delay, type);
		}
	}

	private void HandleMoves(float horizontal){
		if (freezePlayer) {
			if(!playerAnimator.GetCurrentAnimatorStateInfo (0).IsName ("death"))
				playerAnimator.SetTrigger ("death");
			return;
		}
		if (myRigidBody.velocity.y < 0) {
			playerAnimator.SetBool ("land", true);
		}

		if (!playerAnimator.GetBool ("slide") && !playerAnimator.GetNextAnimatorStateInfo (0).IsTag ("Attack")) {
			myRigidBody.velocity = new Vector2(horizontal * playerSpeed, myRigidBody.velocity.y);

		}

		playerAnimator.SetFloat ("speed", Mathf.Abs (horizontal));

		if (isGround && jump) {
			isGround = false;
			myRigidBody.AddForce (new Vector2(0, jumpForce));
			playerAnimator.SetTrigger ("jump");
			SoundManagerLevel2.instance.PlaySingle (jumpSound);
		}
	}

	private void HandleAttacks(){
		if (freezePlayer) {
			if(!playerAnimator.GetCurrentAnimatorStateInfo (0).IsName ("death"))
				playerAnimator.SetTrigger ("death");
			return;
		}
		if (attack && !playerAnimator.GetNextAnimatorStateInfo (0).IsTag ("Attack")) {
			playerAnimator.SetTrigger ("attack");
			myRigidBody.velocity = Vector2.zero;
		}
		if (shoot) {
			playerAnimator.SetTrigger ("shoot");
			SoundManagerLevel2.instance.PlaySingle (handgunSound);
			// Fire Bullets here
			ShootBullet();
		}
		if (slide && !playerAnimator.GetNextAnimatorStateInfo (0).IsName ("slide")) {
			playerAnimator.SetBool ("slide", true);
		} else if (!playerAnimator.GetNextAnimatorStateInfo (0).IsName ("slide")) {
			playerAnimator.SetBool ("slide", false);
		}
	}

	private void ShootBullet(){
		if (transform.localScale.x > 0) {
			bulletPrefab.GetComponent<SpriteRenderer> ().flipX = false;
			bPrefab = Instantiate (bulletPrefab, gameObject.transform.position, Quaternion.identity) as GameObject;
			Rigidbody2D rb = bPrefab.GetComponent<Rigidbody2D> ();
			rb.AddForce (gameObject.transform.right * bulletForce);
		}
		else {			
			bulletPrefab.GetComponent<SpriteRenderer> ().flipX = true;
			bPrefab = Instantiate (bulletPrefab, gameObject.transform.position, Quaternion.identity) as GameObject;
			Rigidbody2D rb = bPrefab.GetComponent<Rigidbody2D> ();
			rb.AddForce (-gameObject.transform.right * bulletForce);
		}
	}

	private void HandleInput(){
		if ((SceneManager.GetActiveScene ().name == "Level1Tutorial_1") || (SceneManager.GetActiveScene ().name == "Level1Tutorial_2")){
			GameObject currHint = GameObject.Find ("HintCanvas");
			bool isHint = currHint.GetComponent<Canvas> ().enabled;
			if (isHint) {
				if (Input.GetKeyDown (KeyCode.Return))
				if (currHint)
					currHint.transform.GetChild (3).gameObject.GetComponent<Button> ().onClick.Invoke ();
			}
		}
		if (Input.GetKeyDown (KeyCode.Space)) {
			jump = true;
		}

		if (Input.GetKeyDown (KeyCode.R)) {
			ResurrectHazel ();
		}

		if (Input.GetKeyDown (KeyCode.LeftArrow) || Input.GetKeyDown (KeyCode.RightArrow)) {
			//SoundManagerLevel2.instance.RandomizeSfx (footstep);			
		}

		if (Input.GetKeyDown (KeyCode.LeftShift) || Input.GetKeyDown (KeyCode.Z)) {
			shoot = true;
		}

		if (Input.GetKeyDown (KeyCode.RightShift) || Input.GetKeyDown (KeyCode.X)) {
			shoot = true;
		}

		if (Input.GetKeyDown (KeyCode.C)) {
			slide = true;
		}

	}

	public void MurderHazel(){
		// TODO : play Dead sound
		Debug.Log ("MurderHazel Called !");
		SoundManagerLevel2.instance.PlaySingle (deadSound);
		freezePlayer = true;
		playerAnimator.SetTrigger ("death");
		//myRigidBody.isKinematic = true;

		DecLife ();
		// TODO : Show the info that press R to wake her up.
		StartCoroutine (ShowResurrectHintAfterWait(2f));
	}

	IEnumerator ShowResurrectHintAfterWait(float duration){
		yield return new WaitForSeconds(duration);   //Wait
		GameObject collider = GameObject.Find ("TutorialCollider");
		collider.SetActive (true);
		collider.GetComponent<HintScriptLevel2> ().setHint ("Press R to Resurrect", "NotationsSprites/Others/hazel");
		collider.GetComponent<HintScriptLevel2> ().showHint ();
	}

	public void ResurrectHazel(){
		//playerAnimator.ResetTrigger ("death");
		playerAnimator.SetTrigger ("resurrect");
		//myRigidBody.isKinematic = false;
		freezePlayer = false;
	}


	void AddRigidbody()
	{
		if(!gameObject.GetComponent<Rigidbody2D>())
			gameObject.AddComponent<Rigidbody2D>();;

	}
	void RemoveRigidbody()
	{
		if(gameObject.GetComponent<Rigidbody2D>())
			Destroy(gameObject.GetComponent<Rigidbody2D>());
	}



	private void FlipPlayer(float hor){
		if (freezePlayer)
			return;
		if (hor > 0 && flip == false || hor < 0 && flip == true) {
			flip = !flip;
			Vector3 myScale = transform.localScale;
			myScale.x *= -1;
			transform.localScale = myScale;			
		}
	}

	private bool IsGrounded(){
		// TODO : Add the Vode to not jump twice.
		if (true/*myRigidBody.velocity.y <= 0*/) {
			foreach (Transform point in groundPoints) {
				Collider2D[] colliders = Physics2D.OverlapCircleAll (point.position, groundRadius, whatIsGround);
				for (int i = 0; i < colliders.Length; i++) {
					if (colliders [i].gameObject != gameObject) {
						playerAnimator.ResetTrigger ("jump");
						playerAnimator.SetBool ("land", false);
						return true;
					}
				}
			}		
		}
		return false;
	}

	private void HandleLayers(){
		if (!isGround) {
			playerAnimator.SetLayerWeight (1, 1);
		} else {
			playerAnimator.SetLayerWeight (1, 0);
		}
	}

	private void ResetScene(){
		jump = false;
		attack = false;
		shoot = false;
		slide = false;
		Destroy(GameObject.Find("Bullet(Clone)"), 0.2f);
		Destroy (GameObject.Find("ExplosionMobile(Clone)"), 2);
		Destroy (GameObject.Find("Explosion(Clone)"), 2);
		// Check if Game Over or Completed!
		if (boxesScore <= 0)
			GameCompleted ();
		if (lifeScore <= 0)
			GameOver ();
		//Show the map button only when uncovred 10 boxes.
		if (boxesScore <= 0) {
			if(GameObject.Find ("ShowScene"))
				GameObject.Find ("ShowScene").GetComponent<Button> ().enabled = true;
				//TODO : Give a popup saying You can see map now
		}
	}


	public bool IsDeltaBad(){
		return (falseHitScore > hitScore);
	}

	public void SetReviewActive(bool status){
		scoreText.transform.GetChild (5).gameObject.SetActive (status);
	}

	private void GameCompleted(){
		// Hurray Game Completed.
		lastBoxOpened = true;
	}

	private void GameOver(){
		// Alas! Game is over !. Hazel dead thrice.
		PaintGameOverMenu ();
		GameObject.Find ("Hazel").GetComponent<PauseMenuLevel2> ().isGameOver = true;
		if(GameOverCanvas)
			GameOverCanvas.enabled = true;

	}

	IEnumerator ShowGameWonAfterWait(float duration){
		yield return new WaitForSeconds(duration);
		PaintGameOverMenu ();
		GameObject.Find("Hazel").GetComponent<PauseMenuLevel2>().isHint = true;
		GameOverCanvas.GetComponentInChildren<Text>().text = "Bravo ! All Mines Uncovered !";
		GameOverCanvas.enabled = true;
	}

	public void ShootGameWon(float duration){
		StartCoroutine (ShowGameWonAfterWait (duration));
	}

	public bool IsLastBoxOpened(){
		return lastBoxOpened;
	}


	public void PaintGameOverMenu(){
		GameObject gameOverCanvas = GameObject.FindWithTag ("khatamScreen");
		Text trueScore = gameOverCanvas.transform.GetChild (4).GetComponent<Text> ();
		trueScore.text = "True Hits :: " + hitScore;

		Text falseScore = gameOverCanvas.transform.GetChild (5).GetComponent<Text> ();
		falseScore.text = "False Hits :: " + falseHitScore;


		Text feebbackBox = gameOverCanvas.transform.GetChild (6).GetComponent<Text> ();



		//Decide the message! need to change later.
		string feedbackText = "Try to focus ! Practice Makes a man perfect.";
		int delta = hitScore - falseHitScore;
		if (delta < 0) {
			// return
			feebbackBox.text = feedbackText;
			return;
		}
		
		if (delta >= 0 && delta < 100) {
			//Okay message
			feedbackText = "You are going pretty good! Try harder.";
			feebbackBox.text = feedbackText;
		} else if (delta >= 100 && delta < 500) {
			// Super message
			feedbackText = "You are awesome! Success will follow you soon.";
			feebbackBox.text = feedbackText;

		} else if (delta >= 500 && delta < 1000) {
			//Excellent!
			feedbackText = "You are Excellent! Sharp eyes you have!";
			feebbackBox.text = feedbackText;
		} else {
			//genious
			feedbackText = "Genious in Action! Hatz off to you Sir!";
			feebbackBox.text = feedbackText;
		}

	}

	private void UpdateScore(){
		//Debug.Log ("Update Score Called!");
		Text falseHitText = scoreText.transform.GetChild (0).GetComponent<Text> ();
		falseHitText.text = "False Hit Score : " + falseHitScore;
		Text lifeLeftText = scoreText.transform.GetChild (1).GetComponent<Text> ();
		lifeLeftText.text = "X " + lifeScore;
		Text boxesLeftText = scoreText.transform.GetChild (2).GetComponent<Text> ();
		boxesLeftText.text = "Mines Remaining : " + boxesScore;
		Text hitText = scoreText.transform.GetChild (3).GetComponent<Text> ();
		hitText.text = "True Hits Score : " + hitScore;
	}
}
