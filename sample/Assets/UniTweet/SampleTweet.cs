using UnityEngine;
using System.Collections;

public class SampleTweet : MonoBehaviour {

	public Rect textfieldPos;
	public Rect buttonPos;
	public Rect tokenButtonPos;

	// Use this for initialization
	void Start () {
		GetComponent<OAuth>().RequestToken();
	}

	void TestTweet(){
		Texture2D tex = new Texture2D(10,10, TextureFormat. ARGB32, false);
		for(int i = 0 ; i < tex.width ; i++){
			for(int j = 0 ; j < tex.height ; j++){
				tex.SetPixel(i, j, Color.black);
			}
		}
		tex.Apply();
		GetComponent<Twitter>().PostTweetMedia("test" , tex.EncodeToPNG());
	}

	void OnGUI(){
		GetComponent<OAuth>().PINCODE = GUI.TextField(textfieldPos, GetComponent<OAuth>().PINCODE);
		if(GUI.Button(tokenButtonPos, "REQUEST_ACCESS_TOKEN")){
			GetComponent<OAuth>().RequestAccessToken();
		}
		
		if(GUI.Button(buttonPos, "TWEET TEST")){
			TestTweet();
		}
	}

	// Update is called once per frame
	void Update () {
	
	}
}
