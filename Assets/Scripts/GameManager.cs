using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.IO;
//using System;

public class GameManager : MonoBehaviour 
{
	public static GameManager gm;

	public Button buttonRoll;           // dugme za rotaciju
	public Button buttonPlayAgain;      // dugme za reset i ponovnu igru
	public Button buttonQuit;           // dugme za quit
	public Button buttonHelp;           // dugme za quit
	public Button buttonCloseHelpPanel; // dugme za zatvarnje help panela
	public Text txtCredits;
	public Text txtBet;
	public Text txtWin;
	public Dropdown dropDownBet;
	public Image panelNegativeCreditsMessage;   // poruka za kredit koji bi isao u minus
	public Image panelGameOver;                 // nema vise para, game over
	public Image panelChooseBet;                // poruka da je potrebno odabrati bet pre igranja
	public Image panelHelpWinnings;             // panel za prikaz kombinacija dobitaka 

	public GameObject[] wheels; // tockovi
	private bool[] wheelDone;   // status koji oznacava da li je rotacija gotova

	public float totalCredits = 1000;  // ukupno kredita
	float totalCreditsOriginal;        // cuva ukupno kredita, radi prosledjivanja u totalCredits nakon restarta
	float totalBet = 0;
	float totalWin = 0;

	[Tooltip("Redosled simbola na tocku 1.")]
	public List<Symbols> wheel_1_SymbolOrder;
	[Tooltip("Redosled simbola na tocku 2.")]
	public List<Symbols> wheel_2_SymbolOrder;
	[Tooltip("Redosled simbola na tocku 3.")]
	public List<Symbols> wheel_3_SymbolOrder;
	[Tooltip("Redosled simbola na tocku 4.")]
	public List<Symbols> wheel_4_SymbolOrder;
	[Tooltip("Redosled simbola na tocku 5.")]
	public List<Symbols> wheel_5_SymbolOrder;

	string[] symbolRow1;  // nizovi za belezenje rezultata 
	string[] symbolRow2;  // Svaki niz predstavlja jedan red na 'slici'
	string[] symbolRow3;  // i sadrzi ime jednog simbola iz Symbols

	public AudioSource audioMusic;
	public AudioSource audioFX;
	public AudioClip audioClipClick;
	public AudioClip audioClipGameOver;
	public AudioClip audioClipWin;
	public AudioClip audioBadLuck;

	public TextAsset combinations1;
	public TextAsset combinations2;
	public TextAsset combinations3;

	private bool isGameOver = false;

	public enum Symbols
	{
		BAR,
		CHERRY,
		DIAMOND,
		LEMON,
		ORANGE,
		PLUM,
		SEVEN
	};

	private List<Symbols>[] wheelSymbols;

	void Start () 
	{

		if (gm == null)
			gm = this.GetComponent<GameManager>(); // napravi opsti GM
		
		/** UI eventi */		
		buttonRoll.onClick.AddListener( BtnRollClick );
		buttonPlayAgain.onClick.AddListener( BtnPlayAgain );
		buttonQuit.onClick.AddListener( BtnQuit );
		buttonHelp.onClick.AddListener( BtnHelp );
		buttonCloseHelpPanel.onClick.AddListener( BtnCloseHelp );
		dropDownBet.onValueChanged.AddListener(delegate {
			DropDownChanged(dropDownBet);
		});

		// lepo pozicioniraj help panel, da bude simetricno
		panelHelpWinnings.transform.position = new Vector2( Screen.width / 2, -700 );

		/** podesi statuse rotacije na FALSE, ima ih isto koliko i tockova */
		wheelDone = new bool[wheels.Length];       // napravi niz statusa
		SetWheelsDone( false );

		/** init nizova za belezenje dobijenih rezultata */
		/** koliko tockova, toliko clanova niza          */
		symbolRow1 = new string[wheels.Length];
		symbolRow2 = new string[wheels.Length];
		symbolRow3 = new string[wheels.Length];

		totalCreditsOriginal = totalCredits;      // sacuvaj originalni broj kredita

		/** aktiviraj nizove za brojanje dobitaka **/
		for( int i = 0; i < symbolRow1.Length; i++ )
		{
			symbolRow1[i] = "";
			symbolRow2[i] = "";
			symbolRow3[i] = "";
		}			

		UpdateUI();
	}
	

	void Update () 
	{
		
	}

	/** Obrada eventa za Help dugme **/
	void BtnHelp()
	{
		panelHelpWinnings.transform.DOMove( new Vector2( Screen.width / 2, Screen.height / 2 ), 0.2f );
	}

	/** Obrada eventa za zatvaranje helpa **/
	void BtnCloseHelp()
	{
		panelHelpWinnings.transform.DOMove( new Vector2(  Screen.width / 2, -700 ), 0.2f );
	}

	/** Obrada eventa za Quit dumge **/
	void BtnQuit ()
	{
		Application.Quit();
	}

	/** Obrada eventa na dropdown */
	void DropDownChanged ( Dropdown change )
	{
		int val = 0;
		string input = dropDownBet.options[dropDownBet.value].text;

		if( dropDownBet.value != 0 )  // ako je izabrana prva opcija ( "Select bet"... ) preskoci celu pricu
		{
			input = input.Replace( "Bet $", "" );
			val = int.Parse( input );
			totalBet = val;
			//buttonRoll.enabled = true;
		}

		UpdateUI();
	}	

	/** Obrada eventa za dugme za restart **/
	void BtnPlayAgain()
	{
		RestartGame();
	}

	/** Obrada eventa za klik na roll dugme **/
	void BtnRollClick ()
	{		
		if( totalBet == 0 )
		{
			ShowInfoBox( panelChooseBet );
		}
		else if( ( totalCredits - totalBet ) < 0 ) // kladimo se preko novca sto imamo, prikazi poruku
		{
			ShowInfoBox( panelNegativeCreditsMessage );
		}
		else
		{
			//asMusic.Stop();
			iTween.AudioTo ( audioMusic.gameObject, iTween.Hash (
				"volume", 0, 
				"time", 1.0f
			));

			SpinTheWheels();
		}

		//UpdateUI();
	}		

	/** zarotiaj tockove **/
	void SpinTheWheels()
	{
		totalWin = 0;                 // resetuj ukupni dobitak
		totalCredits -= totalBet;     // oduzmi bet od ukupne love
		UpdateUI();                   // prikazi sve na ekranu

		SetWheelsDone( false );       // postavi sve tockove na aktivno stanje 
		buttonRoll.enabled = false;   // nema kliktanja dok radi
		dropDownBet.enabled = false;  // rotacija

		/** prodji sve tockove i zarotiraj ih */
		for( int i = 0; i < wheels.Length; i++ )
		{
			/** daj slucajan broj od 1 do 7. Ovo ce biti element koji ce se upisati u niz za redove ( symbolRow ) i prikazati na tocku */
			int rr = Random.Range( 1, 7 ); 
			//rr = 1;
			/** postavi nazive simbola u nizove za redove */
			symbolRow1[i] = GetSymbolName( rr, i, 1 );
			symbolRow2[i] = GetSymbolName( rr, i, 2 );
			symbolRow3[i] = GetSymbolName( rr, i, 3 );

			/** zarotiraj svaki tocak posebno, svakom posebno posalji simbol na kom treba da se zaustavi */
			switch( i )
			{
			case 0:
				wheels[i].GetComponent<Wheel>().StartRoll( wheel_1_SymbolOrder[rr - 1], Random.Range( 1f, 3f ) );
				break;
			case 1:
				wheels[i].GetComponent<Wheel>().StartRoll( wheel_2_SymbolOrder[rr - 1], Random.Range( 1f, 3f ) );
				break;
			case 2:
				wheels[i].GetComponent<Wheel>().StartRoll( wheel_3_SymbolOrder[rr - 1], Random.Range( 1f, 3f ) );
				break;
			case 3:
				wheels[i].GetComponent<Wheel>().StartRoll( wheel_4_SymbolOrder[rr - 1], Random.Range( 1f, 3f ) );
				break;
			case 4:
				wheels[i].GetComponent<Wheel>().StartRoll( wheel_5_SymbolOrder[rr - 1], Random.Range( 1f, 3f ) );
				break;
			}
		}		
	}

	/** Metoda za prikaz info panela. Panel postavlja u centar i vraca ga odakle je dosao **/
	void ShowInfoBox ( Image panel )
	{
		Vector2 oldPosition = panel.transform.position;                                                                 // uzmi staru poziciju
		Sequence seq = DOTween.Sequence();                     
		seq.Append ( panel.transform.DOMove ( new Vector2 (Screen.width / 2, Screen.height / 2 ), 0.2f )); // stavi panel na sredinu ekrana
		seq.AppendInterval( 1 );                                                                                                 // sacekaj sekund
		seq.Append ( panel.transform.DOMove (oldPosition, 0.2f ));                                         // vrati panel odakle je krenuo
		seq.Play();		
	}
		

	/** RESTART Metoda **/
	void RestartGame ()
	{
		panelGameOver.transform.DOMove ( new Vector2 (-554, 350 ), 0.5f ); // vrati panel odakle je krenuo
		totalWin = 0;
		totalCredits = totalCreditsOriginal;
		totalBet = 0;

		/** UI **/
		dropDownBet.value = 0;
		dropDownBet.enabled = true;
		buttonRoll.enabled = true; 
	}

	/** Update korisnickog interfejsa */
	public void UpdateUI()
	{
		txtCredits.text = "$" + totalCredits.ToString("N");
		txtBet.text = "$" + totalBet.ToString("N");
		txtWin.text = "$" + totalWin.ToString("N");
	}

	/** podesavanje statusa rotacije tockova */
	private void SetWheelsDone ( bool status )
	{
		for( int i = 0; i < wheelDone.Length; i++ )
		{
			wheelDone[i] = status;
		}
	}

	/** Metoda u koju se javljaju Wheelsi. 
	 * Kada su svi javili da su gotovi i kkada su ceo njihov wheelDone[] niz = true,
	 * gotovo je, uradi proracun
	 * */
	public void WheelDone ( string name )
	{		
		bool allDone = true;

		/** Uzmi ime cilindra i na osnovu imena ga postavi u niz wheelDone */
		int v = int.Parse(name.Replace( "Cylinder ", "" )) - 1;
		wheelDone[v] = true;

		/** prodji kroz sve cilindre, pogledaj status, ako je bilo koji false, sacekaj jos malo */
		for( int i = 0; i < wheels.Length; i++ )
		{
			if( wheelDone[i] == false )  
			{
				allDone = false;
				break;
			}
		}

		/** ako su svi tockovi stali, izracunaj dobitak */
		if( allDone && !isGameOver )
		{
			buttonRoll.enabled = true; 
			dropDownBet.enabled = true;
			DoMath();
		}
	}
				
	/** Sta smo dobili */
	private void DoMath ()
	{
		/*
		print( "-----------------" );
		print( symbolRow1[0] + " " + symbolRow1[1] + " " + symbolRow1[2] + " " + symbolRow1[3] + " " + symbolRow1[4]);
		print( symbolRow2[0] + " " + symbolRow2[1] + " " + symbolRow2[2] + " " + symbolRow2[3] + " " + symbolRow2[4]);
		print( symbolRow3[0] + " " + symbolRow3[1] + " " + symbolRow3[2] + " " + symbolRow3[3] + " " + symbolRow3[4]);
		print( "-----------------" );		
		*/

		/***** provera dobitka *****/

		audioFX.clip = audioBadLuck; // pretpostavimo da nista nismo dobili

		CheckRow1();
		CheckRow2();
		CheckRow3();


		audioFX.Play();			

		if( totalCredits == 0 && totalWin == 0 )
		{
			GameOver();
		}
		else
		{
			totalCredits += totalWin;
		}

		UpdateUI();
	}		

	/** proveri red 1 **/
	private void CheckRow1()
	{
		for( int i = 0; i < 3; i++ )
		{
			CheckRowForThreeSymbolsRow1( "BAR", i );
			CheckRowForThreeSymbolsRow1( "SEVEN", i );
			CheckRowForThreeSymbolsRow1( "CHERRY", i);
			CheckRowForThreeSymbolsRow1( "PLUM", i);
			CheckRowForThreeSymbolsRow1( "DIAMOND", i);
			CheckRowForThreeSymbolsRow1( "ORANGE", i);
			CheckRowForThreeSymbolsRow1( "PLUM", i);
		}		

		/** proveri CHERRY parove **/
		int multiplier = 1;
		if( symbolRow1[0] == "CHERRY" && symbolRow1[1] == "CHERRY" )
		{
			FX.fx.AnimateTexture( 0, (Symbols)System.Enum.Parse( typeof(Symbols), symbolRow1[0] ), wheels[0] );
			FX.fx.AnimateTexture( 1, (Symbols)System.Enum.Parse( typeof(Symbols), symbolRow1[1] ), wheels[1] );
			audioFX.clip = audioClipWin;
			totalWin += totalBet * multiplier;
			audioFX.clip = audioClipWin;
		}
		if( symbolRow1[1] == "CHERRY" && symbolRow1[2] == "CHERRY" )
		{
			FX.fx.AnimateTexture( 1, (Symbols)System.Enum.Parse( typeof(Symbols), symbolRow1[1] ), wheels[1] );
			FX.fx.AnimateTexture( 2, (Symbols)System.Enum.Parse( typeof(Symbols), symbolRow1[2] ), wheels[2] );
			audioFX.clip = audioClipWin;
			totalWin += totalBet * multiplier;
			audioFX.clip = audioClipWin;
		}
		if( symbolRow1[2] == "CHERRY" && symbolRow1[3] == "CHERRY" )
		{
			FX.fx.AnimateTexture( 2, (Symbols)System.Enum.Parse( typeof(Symbols), symbolRow1[2] ), wheels[2] );
			FX.fx.AnimateTexture( 3, (Symbols)System.Enum.Parse( typeof(Symbols), symbolRow1[3] ), wheels[3] );
			audioFX.clip = audioClipWin;
			totalWin += totalBet * multiplier;
			audioFX.clip = audioClipWin;
		}
		if( symbolRow1[3] == "CHERRY" && symbolRow1[4] == "CHERRY" )
		{
			FX.fx.AnimateTexture( 3, (Symbols)System.Enum.Parse( typeof(Symbols), symbolRow1[3] ), wheels[3] );
			FX.fx.AnimateTexture( 4, (Symbols)System.Enum.Parse( typeof(Symbols), symbolRow1[4] ), wheels[4] );
			audioFX.clip = audioClipWin;
			totalWin += totalBet * multiplier;
			audioFX.clip = audioClipWin;
		}
	}

	/** proveri red 2 **/
	private void CheckRow2()
	{
		for( int i = 0; i < 3; i++ )
		{
			CheckRowForThreeSymbolsRow2( "BAR", i );
			CheckRowForThreeSymbolsRow2( "SEVEN", i );
			CheckRowForThreeSymbolsRow2( "CHERRY", i);
			CheckRowForThreeSymbolsRow2( "PLUM", i);
			CheckRowForThreeSymbolsRow2( "DIAMOND", i);
			CheckRowForThreeSymbolsRow2( "ORANGE", i);
			CheckRowForThreeSymbolsRow2( "PLUM", i);
		}

		/** proveri CHERRY parove **/
		int multiplier = 1;
		if( symbolRow2[0] == "CHERRY" && symbolRow2[1] == "CHERRY" )
		{
			FX.fx.AnimateTexture( 0, (Symbols)System.Enum.Parse( typeof(Symbols), symbolRow2[0] ), wheels[0] );
			FX.fx.AnimateTexture( 1, (Symbols)System.Enum.Parse( typeof(Symbols), symbolRow2[1] ), wheels[1] );
			audioFX.clip = audioClipWin;
			totalWin += totalBet * multiplier;
			audioFX.clip = audioClipWin;
		}
		if( symbolRow2[1] == "CHERRY" && symbolRow2[2] == "CHERRY" )
		{
			FX.fx.AnimateTexture( 1, (Symbols)System.Enum.Parse( typeof(Symbols), symbolRow2[1] ), wheels[1] );
			FX.fx.AnimateTexture( 2, (Symbols)System.Enum.Parse( typeof(Symbols), symbolRow2[2] ), wheels[2] );
			audioFX.clip = audioClipWin;
			totalWin += totalBet * multiplier;
			audioFX.clip = audioClipWin;
		}
		if( symbolRow2[2] == "CHERRY" && symbolRow2[3] == "CHERRY" )
		{
			FX.fx.AnimateTexture( 2, (Symbols)System.Enum.Parse( typeof(Symbols), symbolRow2[2] ), wheels[2] );
			FX.fx.AnimateTexture( 3, (Symbols)System.Enum.Parse( typeof(Symbols), symbolRow2[3] ), wheels[3] );
			audioFX.clip = audioClipWin;
			totalWin += totalBet * multiplier;
			audioFX.clip = audioClipWin;
		}
		if( symbolRow2[3] == "CHERRY" && symbolRow2[4] == "CHERRY" )
		{
			FX.fx.AnimateTexture( 3, (Symbols)System.Enum.Parse( typeof(Symbols), symbolRow2[3] ), wheels[3] );
			FX.fx.AnimateTexture( 4, (Symbols)System.Enum.Parse( typeof(Symbols), symbolRow2[4] ), wheels[4] );
			audioFX.clip = audioClipWin;
			totalWin += totalBet * multiplier;
			audioFX.clip = audioClipWin;
		}
			
	}

	/** proveri red 3 **/
	private void CheckRow3()
	{
		for( int i = 0; i < 3; i++ )
		{
			CheckRowForThreeSymbolsRow3( "BAR", i );
			CheckRowForThreeSymbolsRow3( "SEVEN", i );
			CheckRowForThreeSymbolsRow3( "CHERRY", i);
			CheckRowForThreeSymbolsRow3( "PLUM", i);
			CheckRowForThreeSymbolsRow3( "DIAMOND", i);
			CheckRowForThreeSymbolsRow3( "ORANGE", i);
			CheckRowForThreeSymbolsRow3( "PLUM", i);
		}		

		/** proveri CHERRY parove **/
		int multiplier = 1;
		if( symbolRow3[0] == "CHERRY" && symbolRow3[1] == "CHERRY" )
		{
			FX.fx.AnimateTexture( 0, (Symbols)System.Enum.Parse( typeof(Symbols), symbolRow3[0] ), wheels[0] );
			FX.fx.AnimateTexture( 1, (Symbols)System.Enum.Parse( typeof(Symbols), symbolRow3[1] ), wheels[1] );
			audioFX.clip = audioClipWin;
			totalWin += totalBet * multiplier;
			audioFX.clip = audioClipWin;
		}
		if( symbolRow3[1] == "CHERRY" && symbolRow3[2] == "CHERRY" )
		{
			FX.fx.AnimateTexture( 1, (Symbols)System.Enum.Parse( typeof(Symbols), symbolRow3[1] ), wheels[1] );
			FX.fx.AnimateTexture( 2, (Symbols)System.Enum.Parse( typeof(Symbols), symbolRow3[2] ), wheels[2] );
			audioFX.clip = audioClipWin;
			totalWin += totalBet * multiplier;
			audioFX.clip = audioClipWin;
		}
		if( symbolRow3[2] == "CHERRY" && symbolRow3[3] == "CHERRY" )
		{
			FX.fx.AnimateTexture( 2, (Symbols)System.Enum.Parse( typeof(Symbols), symbolRow3[2] ), wheels[2] );
			FX.fx.AnimateTexture( 3, (Symbols)System.Enum.Parse( typeof(Symbols), symbolRow3[3] ), wheels[3] );
			audioFX.clip = audioClipWin;
			totalWin += totalBet * multiplier;
			audioFX.clip = audioClipWin;
		}
		if( symbolRow3[3] == "CHERRY" && symbolRow3[4] == "CHERRY" )
		{
			FX.fx.AnimateTexture( 3, (Symbols)System.Enum.Parse( typeof(Symbols), symbolRow3[3] ), wheels[3] );
			FX.fx.AnimateTexture( 4, (Symbols)System.Enum.Parse( typeof(Symbols), symbolRow3[4] ), wheels[4] );
			audioFX.clip = audioClipWin;
			totalWin += totalBet * multiplier;
			audioFX.clip = audioClipWin;
		}
	}


	/** proveri trojke u redovima **/
	private void CheckRowForThreeSymbolsRow1 ( string symbol, int i )
	{
		int betMultiplier = 0;
		if( symbol == "BAR" )    betMultiplier = 60;
		if( symbol == "SEVEN" )  betMultiplier = 40;
		if( symbol == "CHERRY" ) betMultiplier = 20;
		else                     betMultiplier = 10;

		if( CheckThree( symbol, symbolRow1[i], symbolRow1[i + 1], symbolRow1[i + 2] ) )
		{
			FX.fx.AnimateTexture( i, (Symbols)System.Enum.Parse( typeof(Symbols), symbolRow1[i] ), wheels[i] );
			FX.fx.AnimateTexture( i + 1, (Symbols)System.Enum.Parse( typeof(Symbols), symbolRow1[i + 1] ), wheels[i + 1] );
			FX.fx.AnimateTexture( i + 2, (Symbols)System.Enum.Parse( typeof(Symbols), symbolRow1[i + 2] ), wheels[i + 2] );
			audioFX.clip = audioClipWin;
			totalWin += totalBet * betMultiplier;
			audioFX.clip = audioClipWin;
		}		
	}

	/** proveri trojke u redovima **/
	private void CheckRowForThreeSymbolsRow2 ( string symbol, int i )
	{
		int betMultiplier = 0;
		if( symbol == "BAR" )    betMultiplier = 60;
		if( symbol == "SEVEN" )  betMultiplier = 40;
		if( symbol == "CHERRY" ) betMultiplier = 20;
		else                     betMultiplier = 10;

		if( CheckThree( symbol, symbolRow2[i], symbolRow2[i + 1], symbolRow2[i + 2] ) )
		{
			FX.fx.AnimateTexture( i, (Symbols)System.Enum.Parse( typeof(Symbols), symbolRow2[i] ), wheels[i] );
			FX.fx.AnimateTexture( i + 1, (Symbols)System.Enum.Parse( typeof(Symbols), symbolRow2[i + 1] ), wheels[i + 1] );
			FX.fx.AnimateTexture( i + 2, (Symbols)System.Enum.Parse( typeof(Symbols), symbolRow2[i + 2] ), wheels[i + 2] );
			audioFX.clip = audioClipWin;
			totalWin += totalBet * betMultiplier;
			audioFX.clip = audioClipWin;
		}		
	}

	/** proveri trojke u redovima **/
	private void CheckRowForThreeSymbolsRow3 ( string symbol, int i )
	{
		int betMultiplier = 0;
		if( symbol == "BAR" )    betMultiplier = 60;
		if( symbol == "SEVEN" )  betMultiplier = 40;
		if( symbol == "CHERRY" ) betMultiplier = 20;
		else                     betMultiplier = 10;

		if( CheckThree( symbol, symbolRow3[i], symbolRow3[i + 1], symbolRow3[i + 2] ) )
		{
			FX.fx.AnimateTexture( i, (Symbols)System.Enum.Parse( typeof(Symbols), symbolRow3[i] ), wheels[i] );
			FX.fx.AnimateTexture( i + 1, (Symbols)System.Enum.Parse( typeof(Symbols), symbolRow3[i + 1] ), wheels[i + 1] );
			FX.fx.AnimateTexture( i + 2, (Symbols)System.Enum.Parse( typeof(Symbols), symbolRow3[i + 2] ), wheels[i + 2] );
			audioFX.clip = audioClipWin;
			totalWin += totalBet * betMultiplier;
			audioFX.clip = audioClipWin;
		}		
	}


	/// <summary>
	/// Uporedjuje source sa ulaznim stringovima i vraca true ako su svi isti
	/// </summary>
	/// <param name="source">Source.</param>
	/// <param name="s1">S1.</param>
	/// <param name="s2">S2.</param>
	/// <param name="s3">S3.</param>
	private bool CheckThree ( string source, string s1, string s2, string s3)
	{
		if( (s1 == source && s2 == source) && (s2==source && s3 == source) )
			return true;
		return false;
	}

	private void GameOver()
	{
		/** animacija panela za Game Over **/
		panelGameOver.transform.DOMove ( new Vector2 (Screen.width / 2, Screen.height / 2 ), 0.2f ); // stavi panel na sredinu ekrana

		isGameOver = true;

		buttonRoll.enabled = false;
		dropDownBet.enabled = false;

		audioFX.clip = audioClipGameOver;
		audioFX.Play();
	}

	/** Rogobatija za pronalazenja imena simbola iz kliste wheel_X_SymbolOrder na osnovu njegove pozicije koja je dobijena slucajnim brojevima */
	/** rr    - slucajni broj, tj. broj pozicije */
	/** wheel - indeks tocka                     */
	/** row   - red za koji pitamo               */
	string GetSymbolName ( int rr, int wheel, int row  )
	{
		if( row == 1 )
		{
			switch( wheel )
			{
			case 0:
				if( rr - 2 < 0 ) return wheel_1_SymbolOrder[6].ToString();
				else             return wheel_1_SymbolOrder[rr - 2].ToString();				
			case 1:
				if( rr - 2 < 0 ) return wheel_2_SymbolOrder[6].ToString();
				else             return wheel_2_SymbolOrder[rr - 2].ToString();				
			case 2:
				if( rr - 2 < 0 ) return wheel_3_SymbolOrder[6].ToString();
				else             return wheel_3_SymbolOrder[rr - 2].ToString();				
			case 3:
				if( rr - 2 < 0 ) return wheel_4_SymbolOrder[6].ToString();
				else             return wheel_4_SymbolOrder[rr - 2].ToString();				
			case 4:
				if( rr - 2 < 0 ) return wheel_5_SymbolOrder[6].ToString();
				else             return wheel_5_SymbolOrder[rr - 2].ToString();				
			}
		}

		if( row == 2 )
		{
			switch( wheel )
			{
			case 0:
				return wheel_1_SymbolOrder[rr - 1].ToString();
			case 1:
				return wheel_2_SymbolOrder[rr - 1].ToString();
			case 2:
				return wheel_3_SymbolOrder[rr - 1].ToString();
			case 3:
				return wheel_4_SymbolOrder[rr - 1].ToString();
			case 4:
				return wheel_5_SymbolOrder[rr - 1].ToString();
			}

		}

		if ( row == 3 )
		{
			switch( wheel )
			{
			case 0:
				if( rr > 6 ) return wheel_1_SymbolOrder[0].ToString();
				else         return wheel_1_SymbolOrder[rr].ToString();					
			case 1:
				if( rr > 6 ) return wheel_2_SymbolOrder[0].ToString();
				else         return wheel_2_SymbolOrder[rr].ToString();					
			case 2:
				if( rr > 6 ) return wheel_3_SymbolOrder[0].ToString();
				else         return wheel_3_SymbolOrder[rr].ToString();					
			case 3:
				if( rr > 6 ) return wheel_4_SymbolOrder[0].ToString();
				else         return wheel_4_SymbolOrder[rr].ToString();					
			case 4:
				if( rr > 6 ) return wheel_5_SymbolOrder[0].ToString();
				else         return wheel_5_SymbolOrder[rr].ToString();					
			}
		}

		return "ERROR";
	}
}

