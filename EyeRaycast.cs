using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

[System.Serializable]
public class AngelaInteractData{
	public string typename;
	public string[] voices;
	public float waitTime;
	public string dispname;
	public string urlPicture;
};

public class EyeRaycast : MonoBehaviour {


	Dictionary<string, string> dic;
	public GameObject angela;
	public float looktime;
	public AngelaInteractData[] data;

	private bool isCaptureScreenShot;
	private string lookparts;
	private string screenshotPath;
	private string screenshotName;
	// Use this for initialization
	void Start () {
		looktime = 0.0f;
		isCaptureScreenShot = false;
		lookparts = "";
		dic = new Dictionary<string, string>();
		dic.Add("head", "かお");
		dic.Add("bust", "おっぱい");
		#if UNITY_EDITOR
		screenshotPath = "";
		screenshotName = "screenshot.png";
		#else
		screenshotPath = Application.dataPath + "/Data/";
		screenshotName = "screenshot.png";
		#endif
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
					if(looktime > d.waitTime && isCaptureScreenShot == false){
					//if(Input.GetKeyDown(KeyCode.V) && isCaptureScreenShot == false){
						looktime = 0.0f;
						angela.GetComponent<AngelaTalk>().TalkAngry(d.voices[Random.Range(0, d.voices.Length)]);
						CaptureScreenShot();
						Debug.Log(d.dispname);
						Debug.Log(d.typename);
						lookparts = dic[d.typename];
					}
				}
			}
		}
		if(isCaptureScreenShot){
			if(File.Exists(screenshotPath + screenshotName)){
				isCaptureScreenShot = false;
				Debug.Log(Application.dataPath);
				FileStream fs = new FileStream(screenshotPath + screenshotName, FileMode.Open);
				BinaryReader br = new BinaryReader(fs);
				byte[] raw = br.ReadBytes((int)br.BaseStream.Length);

				int num = PlayerPrefs.GetInt (lookparts);
				PlayerPrefs.SetInt (lookparts, num + 1);
				string charaname = angela.name;
				GameObject.Find("SystemTwitter").GetComponent<SystemTwitter>().PostTweetMedia("私は" + charaname + "の" + lookparts + "を" + PlayerPrefs.GetInt(lookparts) + "回見つめました" + " #萌えシタン暴き", raw);
			}
		}
		angela.GetComponent<CharacterController>().enabled = true;
	}

	void CaptureScreenShot(){
		File.Delete(screenshotPath + screenshotName);
		Application.CaptureScreenshot(screenshotName);
		isCaptureScreenShot = true;
	}
}
