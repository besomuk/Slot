using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class Wheel : MonoBehaviour 
{
	[Tooltip("brzina rotacije u 'idle' rezimu")]
	public float rotationSpeed = -1.0f;         // rotacija u 'idle' rezimu
	[Tooltip("vreme glavne rotacije")]
	public float  rotationTime = 3.0f;    // vreme glavne rotacije 
	private float rotationTimeTemp;      // vreme glavne rotacije, privremena, da bi se izbeglo sabiranje u kotrljanju

	/** pozicije simbola ( ugao rotacij tocka pri kom su oni pravo ispred kamere */
	public float diamondPosition = -3.745f;
	public float cherryPosition = -17f;
	public float barPosition = -28.8f;
	public float sevenPosition = -42.3f;
	public float plumPosition = -55f;
	public float orangePosition = -67.14f;
	public float lemonPosition = -80.6f;

	private Hashtable symbolPosition;              // hash tabla radi udobnijeg snalazenja sa pozicijama simbola na tocku

	AudioSource audioFX;       // efekat zaustavljanja tocka
	AudioSource audioRoll;     // efekat kretanja
	public AudioClip clipStop;
	public AudioClip clipRoll;

	float a = 2.0f;

	float bx = -0.2f;
	float by = -0.5f;

	Renderer r;

	void Start () 
	{	
		/** dodaj audio source */
		/** FX **/
		audioFX = gameObject.AddComponent<AudioSource>();
		audioFX.playOnAwake = false;
		audioFX.clip = clipStop;

		/** zvuk okretanja **/
		audioRoll = gameObject.AddComponent<AudioSource>();
		audioRoll.playOnAwake = false;
		audioRoll.loop = true;
		audioRoll.pitch = 0.5f;
		audioRoll.volume = 0.7f;
		audioRoll.clip = clipRoll;


		/** sredi hash tablu */
		symbolPosition = new Hashtable();	
		symbolPosition.Add( GameManager.Symbols.BAR, barPosition );
		symbolPosition.Add( GameManager.Symbols.CHERRY, cherryPosition );
		symbolPosition.Add( GameManager.Symbols.DIAMOND, diamondPosition );
		symbolPosition.Add( GameManager.Symbols.LEMON, lemonPosition );
		symbolPosition.Add( GameManager.Symbols.ORANGE, orangePosition );
		symbolPosition.Add( GameManager.Symbols.PLUM, plumPosition );
		symbolPosition.Add( GameManager.Symbols.SEVEN, sevenPosition );

		rotationTimeTemp = rotationTime;
	}
		
	void Update () 
	{
		transform.Rotate( new Vector3(0,0,1) * Time.deltaTime * rotationSpeed);  // zarotiraj tocak
	}
		
	/// <summary>
	/// Zapocinje rotiranje jednog tocka. Rotacija se odvija u 2 etape.
	/// Etapa 1 - tocak obrne nekoliko krugova, sve do izabranog simbola, zaustavlja se na tom simbolu, trajanje je rotationTime
	/// Etapa 2 - "pingpong" animacija na kraju rotacije, iluzija zaustavljanja tocka
	/// Izabrani simbol dolazi iz npr. Game Managera
	/// </summary>
	/// <param name="target">naziv simbola</param>
	/// <param name="rotTime">vreme za koje ce vreme rotacije tocka biti 'promuckano'</param>
	public void StartRoll ( GameManager.Symbols target, float rotTime )
	{
		audioRoll.Play();
		audioRoll.DOPitch ( 1.9f, 3 );

		/** hocemo lepu rotaciju **/
		rotationTime = 0;                           // ponisti sabranu rotaciju
		rotationTime = rotationTimeTemp + rotTime;  // saberi original sa ulazom
		rotationSpeed = 0;                          // iskljuci rotaciju glavnog tocka

		float ang = (float)symbolPosition[target]; // ugao koji trazimo i na koji treba postaviti tocak je unapred definisan u hash tabli 

		int randomCircle = Random.Range( 2, 4 ); // random broj krugova za koje ce tocak biti okrenut 
		ang = ang + randomCircle * 360;          // na ugao treba dodati broj krugova...

		// Rotiraj, a na kraju pozovi MovementDone() zbog 'ping-ping' efekta na kraju animacije
		// Metodi MovementDone() se prosledjuje i ugao na koji treba da se vrati
		iTween.RotateTo ( gameObject, iTween.Hash (
			"z", -ang, 
			"time", rotationTime,
			"easetype", iTween.EaseType.easeInCubic,
			"oncomplete", "MovementDone",
			"oncompleteparams", ang
		));
	}

	/** Prva etapa je gotova, pozovi drugu */
	void MovementDone ( float ang )
	{
		// ova rotacija polazi i dolazi na isto mesto
		iTween.RotateTo ( gameObject, iTween.Hash (
			"z", ang, 
			"time", 0.6f,
			"easetype", iTween.EaseType.easeOutElastic,
			"oncomplete", "AllDone"
		));

		audioRoll.DOPitch ( 0.5f, 0.5f );
	}

	/** Obe etape su gotove, javi Game Manageru da si gotov */
	void AllDone ()
	{
		GameManager.gm.WheelDone( this.gameObject.name );
		audioRoll.Stop();
		audioFX.Play();

	}
		
}
