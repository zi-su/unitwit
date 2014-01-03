using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

[System.Serializable]
public class AngelaInteractData{
	public string typename;
	public string[] voices;
	public float waitTime;
	public string urlPicture;
};

public class EyeRaycast : MonoBehaviour {



	public GameObject angela;
	public float looktime;
	public AngelaInteractData[] data;

	private bool isCaptureScreenShot;
	private string lookparts;
	// Use this for initialization
	void Start () {
		looktime = 0.0f;
		isCaptureScreenShot = false;
		lookparts = "";
	}
	
	// Update is called once per frame
	void Update () {
		angela.GetComponent<CharacterController>().enabled = false;
		Ray ray = new Ray(transform.position, transform.rotation * Vector3.forward);
		RaycastHit hitInfo;
		bool isHit = Physics.Raycast(ray,out hitInfo);
		looktime += Time.deltaTime;
 		AngelaInteractData[] data_ = angela.GetComponent<YomeManager>().data;
		if(isHit){
			string name = hitInfo.collider.gameObject.name;
			foreach(AngelaInteractData d in data_){
				if(d.typename == name){
					looktime += Time.deltaTime;
					//if(looktime > d.waitTime){
					if(Input.GetKeyDown(KeyCode.V) && isCaptureScreenShot == false){
						looktime = 0.0f;
						angela.GetComponent<AngelaTalk>().TalkAngry(d.voices[Random.Range(0, d.voices.Length)]);
						CaptureScreenShot();
						lookparts = d.typename;
					}
				}
			}
		}
		if(isCaptureScreenShot){
			if(File.Exists("screenshot.png")){
				isCaptureScreenShot = false;	
				FileStream fs = new FileStream("screenshot.png", FileMode.Open);
				BinaryReader br = new BinaryReader(fs);
				
				byte[] raw = br.ReadBytes((int)br.BaseStream.Length);
				
				
				
				int num = PlayerPrefs.GetInt (lookparts);
				PlayerPrefs.SetInt (lookparts, num + 1);
				string charaname = angela.name;
				float time =System.Environment.TickCount;
				GameObject.Find("SystemTwitter").GetComponent<SystemTwitter>().PostTweetMedia("私は" + charaname + "の" + lookparts + "を" + PlayerPrefs.GetInt(lookparts) + "回見つめました" + " #萌えシタン暴き", raw);
				float aftime = System.Environment.TickCount;
				float elaps = aftime - time;
				Debug.Log("read time " + elaps.ToString());
			}
		}
		angela.GetComponent<CharacterController>().enabled = true;
	}

	void CaptureScreenShot(){
		File.Delete("screenshot.png");
		Application.CaptureScreenshot("screenshot.png");
		isCaptureScreenShot = true;
	}
}
