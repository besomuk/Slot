/// <summary>
/// Klasa za pustanje 'specijalnih' efekata na polju nekog simbola.
/// </summary>
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class FX : MonoBehaviour 
{
	public static FX fx;

	public GameObject circleWin01; // partikle efekat
	public float depth = -8;       // polozaj efekta po Z osi. 

	private int columnPosition;    
	private int rowPosition;

	void Start () 
	{
		if (fx == null)
			fx = this.GetComponent<FX>();
	}

	/// <summary>
	/// Animacija teksture simbola na jednom face-u 3d modela.
	/// </summary>
	/// <param name="wheelNumber">Broj tocka na kom se radi animacija. Ovo je vazno zato sto tockovi imaju razlicit raspored simbola</param>
	/// <param name="symbol">Naziv simbola koji se animira</param>
	/// <param name="wheel">GO tocka koji se animira</param>
	public void AnimateTexture ( int wheelNumber, GameManager.Symbols symbol, GameObject wheel )
	{
		Renderer rd = wheel.GetComponent<Renderer>();
		//rd.materials[0].DOTiling( new Vector2( 2f, 2f ), 1 );
		//rd.materials[0].DOOffset( new Vector2( -0.2f, -0.5f ), 1 );

		/** name svakom tocku je raspored tekstura razlicit...zato na osnovu teksture, zakljuci koji je to broj
		 ** Animiram i offset jer u suprotnom tekstura bezi u cosak
		 ** */
		Vector2 tilingVectorStart = new Vector2( 1f, 1f );      // pocetna vrednost za skaliranje
		Vector2 offsetVectorStart = new Vector2( 0.1f, 0.0f );  // pocetna vrednost za ofset
		Vector2 tilingVector = new Vector2( 2f, 2f );           // krajnja vrednost za skaliranje
		Vector2 offsetVector = new Vector2( -0.2f, -0.5f );     // krajnja vrednost za offset

		int textureID = 0;
		float time = 0.3f;

		/** Pronadji ID, tj. redosled teksture u objektu tocka ( vidi se iz Inspektora ) **/
		if ( wheelNumber == 0 )
		{
			switch( symbol )
			{
			case GameManager.Symbols.DIAMOND: textureID = 0; break;
			case GameManager.Symbols.CHERRY: textureID = 1; break;
			case GameManager.Symbols.BAR: textureID = 2; break;
			case GameManager.Symbols.SEVEN: textureID = 3; break;
			case GameManager.Symbols.PLUM: textureID = 4; break;
			case GameManager.Symbols.ORANGE: textureID = 5; break;
			case GameManager.Symbols.LEMON: textureID = 6; break;
			}
		}

		if ( wheelNumber == 1 )
		{
			switch( symbol )
			{
			case GameManager.Symbols.PLUM: textureID = 0; break;
			case GameManager.Symbols.SEVEN: textureID = 1; break;
			case GameManager.Symbols.BAR: textureID = 2; break;
			case GameManager.Symbols.ORANGE: textureID = 3; break;
			case GameManager.Symbols.CHERRY: textureID = 4; break;
			case GameManager.Symbols.DIAMOND: textureID = 5; break;
			case GameManager.Symbols.LEMON: textureID = 6; break;
			}
		}

		if ( wheelNumber == 2 )
		{
			switch( symbol )
			{
			case GameManager.Symbols.ORANGE: textureID = 0; break;
			case GameManager.Symbols.PLUM: textureID = 1; break;
			case GameManager.Symbols.BAR: textureID = 2; break;
			case GameManager.Symbols.LEMON: textureID = 3; break;
			case GameManager.Symbols.SEVEN: textureID = 4; break;
			case GameManager.Symbols.DIAMOND: textureID = 5; break;
			case GameManager.Symbols.CHERRY: textureID = 6; break;
			}
		}

		if ( wheelNumber == 3 )
		{
			switch( symbol )
			{
			case GameManager.Symbols.DIAMOND: textureID = 0; break;
			case GameManager.Symbols.ORANGE: textureID = 1; break;
			case GameManager.Symbols.BAR: textureID = 2; break;
			case GameManager.Symbols.LEMON: textureID = 3; break;
			case GameManager.Symbols.SEVEN: textureID = 4; break;
			case GameManager.Symbols.CHERRY: textureID = 5; break;
			case GameManager.Symbols.PLUM: textureID = 6; break;
			}
		}

		if ( wheelNumber == 4 )
		{
			switch( symbol )
			{
			case GameManager.Symbols.CHERRY: textureID = 0; break;
			case GameManager.Symbols.LEMON: textureID = 1; break;
			case GameManager.Symbols.BAR: textureID = 2; break;
			case GameManager.Symbols.SEVEN: textureID = 3; break;
			case GameManager.Symbols.DIAMOND: textureID = 4; break;
			case GameManager.Symbols.ORANGE: textureID = 5; break;
			case GameManager.Symbols.PLUM: textureID = 6; break;
			}
		}

		/** uradi animaciju **/
		Sequence animateSymbol = DOTween.Sequence();
		animateSymbol.Append( rd.materials[textureID].DOTiling( tilingVector, time ));   
		animateSymbol.Join( rd.materials[textureID].DOOffset( offsetVector, time ) );
		animateSymbol.Append( rd.materials[textureID].DOTiling( tilingVectorStart, time ) );
		animateSymbol.Join( rd.materials[textureID].DOOffset( offsetVectorStart, time ) );
		animateSymbol.SetLoops( 5 );
		animateSymbol.Play();
	}

	/// <summary>
	/// Prikaz partikle efekta na nekom polju
	/// </summary>
	/// <param name="col">Kolona na kom treba pustiti efekat</param>
	/// <param name="row">Red u kom treba pustiti efekat</param>
	public void ShowFX(int col, int row)
	{
		float tempX = 0;
		float tempY = 0;
		switch( col )
		{
		case 0: tempX = -3.0f; break;
		case 1: tempX = -1.5f; break;
		case 2: tempX = 0; break;
		case 3: tempX = 1.5f; break;
		case 4: tempX = 3f; break;
		}
		switch( row )
		{
		case 1: tempY = 0.51f; break;
		case 2: tempY = -0.73f; break;
		case 3: tempY = -2f; break;
		}
		GameObject tempObject;
		tempObject = Instantiate( circleWin01, new Vector3( tempX, tempY, depth ), Quaternion.identity );
		tempObject.tag = "EffectTemp";
	}

	/// <summary>
	/// Unistava sve zaostale objekte
	/// </summary>
	/// <param name="tag">tag objekta koji treba unistiti</param>
	public void KillemAll( string tag )
	{
		GameObject[] gms = GameObject.FindGameObjectsWithTag(tag);

		foreach (GameObject gm in gms)
		{
			Destroy( gm );
		}		
	}
}
