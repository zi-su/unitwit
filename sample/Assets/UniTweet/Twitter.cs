using UnityEngine;
using System.Collections;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System;
using System.IO;
using System.Text;
public class Twitter : MonoBehaviour {

	const string TWITTER_UPDATE_MEDIA_URL = "https://api.twitter.com/1.1/statuses/update_with_media.json";
	const string TWITTER_UPDATE_URL = "https://api.twitter.com/1.1/statuses/update.json";

	OAuth oauth;
	// Use this for initialization
	void Start () {
		oauth = GetComponent<OAuth>();
	}
	
	// Update is called once per frame
	void Update () {
	}
#region PostTweet
	public void PostTweet(string text){
		StartCoroutine(CoPostTweet(text));
	}
	
	IEnumerator CoPostTweet(string text){
		WWWForm form = new WWWForm();
		form.AddField("status", text);
		Hashtable hash = new Hashtable();
		hash = form.headers;
		hash["Authorization"] = MakePostTweetHeader(text);
		
		WWW www = new WWW(TWITTER_UPDATE_URL, form.data, hash);
		yield return www;
		if(string.IsNullOrEmpty(www.error)){
			Debug.Log(www.text);
		}
		else{
			Debug.Log(www.error);
		}
	}

	string MakePostTweetHeader(string text){
		string header = "";
		string nonce = oauth.GenerateNonce();
		string timestamp = oauth.GenerateTimeStamp();
		string signature = MakeTweetSignature("POST", TWITTER_UPDATE_URL, nonce, timestamp, text);
		Debug.Log(signature);
		header = "OAuth "; 
		header += "oauth_consumer_key=\"" + oauth.CONSUMER_KEY + "\",";
		header += "oauth_nonce=\""+nonce + "\",";
		header += "oauth_signature=\""+signature + "\",";
		header += "oauth_signature_method=\"HMAC-SHA1\",";
		header += "oauth_timestamp=\"" + timestamp + "\",";
		header += "oauth_token=\""+ oauth.ACCESS_TOKEN + "\",";
		header += "oauth_verifier=\"1.0\"";
		Debug.Log(header);
		return header;
	}

	string MakeTweetSignature(string type, string url, string nonce, string timestamp, string text){
		string signature;

		signature = type + "&";
		signature += WWW.EscapeURL(url) + "&";
		signature += WWW.EscapeURL("oauth_consumer_key=" + oauth.CONSUMER_KEY
		                           + "&oauth_nonce=" + nonce
		                           + "&oauth_signature_method=HMAC-SHA1" 
		                           + "&oauth_timestamp="+timestamp
		                           + "&oauth_token="+ oauth.ACCESS_TOKEN
		                           + "&oauth_verifier=1.0"
		                           + "&status="+text);
		
		signature = Regex.Replace(signature, "(%[0-9a-f][0-9a-f])", s => s.Value.ToUpper());
		Debug.Log(signature);
		string key = string.Format("{0}&{1}",
		                           WWW.EscapeURL(oauth.CONSUMER_SECRET),WWW.EscapeURL(oauth.ACCESS_TOKEN_SECRET));
		HMACSHA1 hmacsha1 = new HMACSHA1(System.Text.Encoding.UTF8.GetBytes(key));
		string str_signature =  System.Convert.ToBase64String(
			hmacsha1.ComputeHash(
			Encoding.UTF8.GetBytes( signature )
			)
			);
		str_signature = WWW.EscapeURL(str_signature, Encoding.UTF8);
		return str_signature;
	}
	#endregion

	#region PostTweetMedia
	public void PostTweetMedia(string text, byte[] media){
		StartCoroutine(CoPostTweetMedia(text, media));
	}

	IEnumerator CoPostTweetMedia(string text, byte[] media){
		WWWForm form = new WWWForm();
		form.AddField("status", text);
		form.AddBinaryData("media[]", media, "media.png", "image/png");
		Hashtable hash = new Hashtable();
		hash = form.headers;
		hash["Authorization"] = MakePostTweetMediaHeader();
		Debug.Log(hash["Authorization"]);
		WWW www = new WWW(TWITTER_UPDATE_MEDIA_URL, form.data, hash);
		yield return www;
		if(string.IsNullOrEmpty(www.error)){
			Debug.Log("post tweet success");
		}
		else{
			Debug.Log(www.error);
		}
	}

	string MakePostTweetMediaHeader(){
		string header = "";
		string nonce = oauth.GenerateNonce();
		string timestamp = oauth.GenerateTimeStamp();
		string signature = MakeTweetMediaSignature("POST", TWITTER_UPDATE_MEDIA_URL, nonce, timestamp);
		header = "OAuth "; 
		header += "oauth_consumer_key=\"" + oauth.CONSUMER_KEY + "\",";
		header += "oauth_nonce=\""+nonce + "\",";
		header += "oauth_signature=\""+signature + "\",";
		header += "oauth_signature_method=\"HMAC-SHA1\",";
		header += "oauth_timestamp=\"" + timestamp + "\",";
		header += "oauth_token=\""+ oauth.ACCESS_TOKEN + "\",";
		header += "oauth_version=\"1.0\"";
		return header;
	}

	string MakeTweetMediaSignature(string type, string url, string nonce, string timestamp){
		string signature;
		
		signature = type + "&";
		signature += WWW.EscapeURL(url) + "&";
		signature += WWW.EscapeURL("oauth_consumer_key=" + oauth.CONSUMER_KEY
		                           + "&oauth_nonce=" + nonce
		                           + "&oauth_signature_method=HMAC-SHA1" 
		                           + "&oauth_timestamp="+timestamp
		                           + "&oauth_token="+ oauth.ACCESS_TOKEN
		                           + "&oauth_version=1.0");

		signature = Regex.Replace(signature, "(%[0-9a-f][0-9a-f])", s => s.Value.ToUpper());
		Debug.Log(signature);
		string key = string.Format("{0}&{1}",
		                           WWW.EscapeURL(oauth.CONSUMER_SECRET),WWW.EscapeURL(oauth.ACCESS_TOKEN_SECRET));
		HMACSHA1 hmacsha1 = new HMACSHA1(System.Text.Encoding.UTF8.GetBytes(key));
		string str_signature =  System.Convert.ToBase64String(
			hmacsha1.ComputeHash(
			Encoding.UTF8.GetBytes( signature )
			)
			);
		str_signature = WWW.EscapeURL(str_signature, Encoding.UTF8);
		return str_signature;
	}
	#endregion
}
