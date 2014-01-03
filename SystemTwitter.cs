using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Security.Cryptography;
using System;

using System.Net.Sockets;

public class SystemTwitter : MonoBehaviour {

	private const string STR_AUTHOR = "Authorization";
	private const string STR_CONTENT = "Content-Type";
	private const string STR_TCP_CONNECT_URL = "api.twitter.com";
	private const int PORT = 80;
	private const string STR_REQ_TOKEN_URL = "https://api.twitter.com/oauth/request_token?oauth_callback=oob";
	private const string STR_OAUTH_URL = "http://api.twitter.com/oauth/authorize?oauth_token={0}";
	private const string STR_ACCESS_TOKEN_URL = "https://api.twitter.com/oauth/access_token";
	private const string STR_POST_TWEET_MEDIA_URL = "http://api.twitter.com/1.1/statuses/update_with_media.json";
	private const string STR_POST_TWEET_URL = "https://api.twitter.com/1.1/statuses/update.json";
	
	public const string STR_PPREFS_USER_ID = "TwitterUserID";
    public const string STR_PPREFS_USER_NAME = "TwitterUserScreenName";
    public const string STR_PPREFS_USER_ATOKEN = "TwitterUserAccessToken";
    public const string STR_PPREFS_USER_ATOKEN_SECRET = "TwitterUserAccessTokenSecret";
	public string consumerKey;
	public string consumerSecret;
	
	private string m_AccessToken;
	private string m_AccessTokenSecret;
	private string m_UserId;
	private string m_ScreenName;
	private string request_token;
	private string request_token_secret;
	private string pincode;
	private TcpClient client;
	private NetworkStream ns;
	// シグネチャ計算に必要ないパラメータ
	private static readonly string[]    SECRET_PARAMS = new[]
	{
		"oauth_consumer_secret",
		"oauth_token_secret",
		"oauth_signature",
	};
	// OAuthに必要なヘッダパラメータ
	private static readonly string[]    OAUTH_HEADER_PARAMS = new[]
	{
		"oauth_version",
		"oauth_nonce",
		"oauth_timestamp",
		"oauth_signature_method",
		"oauth_consumer_key",
		"oauth_token",
	};
	// Use this for initialization
	void Start () {
		LoadPlayerPrefs();
		//GetRequestToken();
		pincode = "enter pincode";

	}
	
	// Update is called once per frame
	void Update () {
	
	}

	void LoadPlayerPrefs(){
		m_AccessToken = PlayerPrefs.GetString(STR_PPREFS_USER_ATOKEN);
		m_AccessTokenSecret = PlayerPrefs.GetString(STR_PPREFS_USER_ATOKEN_SECRET);
		m_UserId = PlayerPrefs.GetString(STR_PPREFS_USER_ID);
		m_ScreenName = PlayerPrefs.GetString(STR_PPREFS_USER_NAME);
	}
	public void PostTweet(string text){
		StartCoroutine(coPostTweet(text));
	}

	public void PostTweetMedia(string text, byte[] media){
		StartCoroutine(coPostTweetMedia(text, media));
	}

	IEnumerator coPostTweet(string text){
		WWWForm form = new WWWForm();
		form.AddField("status", text);

		Hashtable header = new Hashtable();
		header = form.headers;
		header[STR_AUTHOR] = makePostTweetHeader(text);
		WWW www = new WWW(STR_POST_TWEET_URL, form.data, header);
		yield return www;

		if( !string.IsNullOrEmpty(www.error) ){
			Debug.Log( string.Format("PostTweet - failed. {0}", www.error) );
		}
		else
		{   // エラーチェック
			string error = Regex.Match( www.text, @"<error>([^&]+)</error>" ).Groups[1].Value;
			if( !string.IsNullOrEmpty(error) ){
				Debug.Log( string.Format("PostTweet - failed. {0}", error) );
			}
			else{   // ツイート成功
				Debug.Log( "OnPostTweet - success." );
			}
		}

	}

	IEnumerator coPostTweetMedia(string text, byte[] media){
		WWWForm form = new WWWForm();
		form.AddField("status", text);
		form.AddBinaryData("media[]", media, "media.png", "application/octet-stream");

		Hashtable header = new Hashtable();
		header = form.headers;
		header[STR_AUTHOR] = makePostTweetMediaHeader(text);
		WWW www = new WWW(STR_POST_TWEET_MEDIA_URL, form.data, header);
		yield return www;
		
		if( !string.IsNullOrEmpty(www.error) ){
			Debug.Log( string.Format("PostTweetMedia - failed. {0}", www.error) );
		}
		else
		{   // エラーチェック
			string error = Regex.Match( www.text, @"<error>([^&]+)</error>" ).Groups[1].Value;
			if( !string.IsNullOrEmpty(error) ){
				Debug.Log( string.Format("PostTweet - failed. {0}", error) );
			}
			else{   // ツイート成功
				Debug.Log( "OnPostTweetMedia - success." );
			}
		}
		
	}

	string makePostTweetHeader(string text){
		Dictionary<string, string> hash = new Dictionary<string, string>();

		hash.Add("oauth_version", "1.0");
		hash.Add("oauth_nonce", GenerateNonce());
		hash.Add("oauth_timestamp", GenerateTimeStamp());
		hash.Add("oauth_signature_method", "HMAC-SHA1");
		hash.Add("oauth_consumer_key", consumerKey);
		hash.Add("oauth_consumer_secret", consumerSecret);

		hash.Add("oauth_token", m_AccessToken);
		hash.Add("oauth_token_secret", m_AccessTokenSecret);
		hash.Add("status", text);

		string signature = GenerateSignature(
			"POST",
			STR_POST_TWEET_URL,
			hash);

		hash.Add("oauth_signature", signature);

		// アルファベット順にソートしつつ必要なパラメータを選出
        SortedDictionary<string, string> sortedParams = new SortedDictionary<string, string>();
        foreach( KeyValuePair<string, string> param in hash )
        {
            foreach( string oauth_header_param in OAUTH_HEADER_PARAMS )
            {
                if( oauth_header_param.Contains( param.Key ) ){
                    sortedParams.Add( param.Key, param.Value );
                }
            }
        }

        // ソートされたパラメータをエスケープしてガッチャンコ
        StringBuilder headerBuilder = new StringBuilder();
        bool bFirst = true;
        foreach( var item in sortedParams )
        {
            if( bFirst ){
                bFirst = false;
                headerBuilder.AppendFormat(
                    "{0}=\"{1}\"",
                    UrlEncode(item.Key),
                    UrlEncode(item.Value));
            }
            else{   // 2番目以降の値は , 付き
                headerBuilder.AppendFormat(
                    ",{0}=\"{1}\"",
                    UrlEncode(item.Key),
                    UrlEncode(item.Value)
                );
            }
        }

        // 完成
        string ret = string.Format( "OAuth {0}", headerBuilder.ToString() );

        return ret;
	}

	string makePostTweetMediaHeader(string text){
		Dictionary<string, string> hash = new Dictionary<string, string>();

		hash.Add("oauth_version", "1.0");
		hash.Add("oauth_nonce", GenerateNonce());
		hash.Add("oauth_timestamp", GenerateTimeStamp());
		hash.Add("oauth_signature_method", "HMAC-SHA1");
		hash.Add("oauth_consumer_key", consumerKey);
		hash.Add("oauth_consumer_secret", consumerSecret);

		hash.Add("oauth_token", m_AccessToken);
		hash.Add("oauth_token_secret", m_AccessTokenSecret);
		string signature = GenerateSignature(
			"POST",
			STR_POST_TWEET_MEDIA_URL,
			hash);
		hash.Add("oauth_signature", signature);

		// アルファベット順にソートしつつ必要なパラメータを選出
        SortedDictionary<string, string> sortedParams = new SortedDictionary<string, string>();
        foreach( KeyValuePair<string, string> param in hash )
        {
            foreach( string oauth_header_param in OAUTH_HEADER_PARAMS )
            {
                if( oauth_header_param.Contains( param.Key ) ){
                    sortedParams.Add( param.Key, param.Value );
                }
            }
        }

        // ソートされたパラメータをエスケープしてガッチャンコ
        StringBuilder headerBuilder = new StringBuilder();
        bool bFirst = true;
        foreach( var item in sortedParams )
        {
            if( bFirst ){
                bFirst = false;
                headerBuilder.AppendFormat(
                    "{0}=\"{1}\"",
                    UrlEncode(item.Key),
                    UrlEncode(item.Value));
            }
            else{   // 2番目以降の値は , 付き
                headerBuilder.AppendFormat(
                    ",{0}=\"{1}\"",
                    UrlEncode(item.Key),
                    UrlEncode(item.Value)
                );
            }
        }

        // 完成
        string ret = string.Format( "OAuth {0}", headerBuilder.ToString() );

        Debug.Log(ret);

        return ret;
	}

	void OnGUI(){
		//pincode = GUI.TextField(new Rect(100, 0, 100, 50), pincode);
		//if(GUI.Button(new Rect(0, 0, 100, 100), "test")){
		//	GetAccessToken();
		//}
	}

	void GetRequestToken(){
		if(string.IsNullOrEmpty(consumerKey) || string.IsNullOrEmpty(consumerSecret)){
		}
		else{
			StartCoroutine("coRequestToken");
		}
	}

	private IEnumerator coRequestToken(){
		System.Collections.Hashtable headers = new Hashtable();
		headers[STR_AUTHOR] = makeRequestTokenHeader();

		byte[] dummy = new byte[1];
		dummy[0] = 0;
		WWW www = new WWW(STR_REQ_TOKEN_URL, dummy, headers);
		yield return www;

		if(string.IsNullOrEmpty(www.error)){
			// トークン文字列の取得
			request_token = Regex.Match( www.text, @"oauth_token=([^&]+)" ).Groups[1].Value;
			request_token_secret = Regex.Match( www.text, @"oauth_token_secret=([^&]+)" ).Groups[1].Value;
			
			// トークンが正常に取得出来ていれば成功
			if( !string.IsNullOrEmpty(request_token) &&
			   !string.IsNullOrEmpty(request_token_secret) )
			{
				string log = "OnRequestTokenCallback - succeeded";
				log += "\n    Token : " + request_token;
				log += "\n    TokenSecret : " + request_token_secret;
				
				// 認証ページをオープン
				Application.OpenURL( string.Format(STR_OAUTH_URL, request_token ));
			}
			else{   // トークン取れなかったらエラー扱い
				Debug.Log( string.Format("GetRequestToken - failed. response : {0}", www.text) );
			}
		}
		else{
			Debug.Log("error");
		}
	}

	string makeRequestTokenHeader(){
		Dictionary<string , string> param = new Dictionary<string , string>();
		//header
		param.Add("oauth_version", "1.0");
		param.Add("oauth_nonce", GenerateNonce());
		param.Add("oauth_timestamp", GenerateTimeStamp());
		param.Add("oauth_signature_method", "HMAC-SHA1");
		param.Add("oauth_consumer_key", consumerKey);
		param.Add("oauth_consumer_secret", consumerSecret);

		param.Add("oauth_callback", "oob");

		string signature = GenerateSignature("POST", STR_REQ_TOKEN_URL, param);

		param.Add("oauth_signature", signature);
		SortedDictionary<string, string> sortedParams = new SortedDictionary<string, string>();
		foreach(KeyValuePair<string, string> paramstr in  param){
			foreach(string s in OAUTH_HEADER_PARAMS){
				if(s.Contains(paramstr.Key)){
					sortedParams.Add(paramstr.Key, paramstr.Value);
				}
			}
		}

		// ソートされたパラメータをエスケープしてガッチャンコ
		StringBuilder headerBuilder = new StringBuilder();
		bool bFirst = true;
		foreach( var item in sortedParams )
		{
			if( bFirst ){
				bFirst = false;
				headerBuilder.AppendFormat(
					"{0}=\"{1}\"",
					UrlEncode(item.Key),
					UrlEncode(item.Value));
			}
			else{   // 2番目以降の値は , 付き
				headerBuilder.AppendFormat(
					",{0}=\"{1}\"",
					UrlEncode(item.Key),
					UrlEncode(item.Value)
					);
			}
		}
		
		// 完成
		string ret = string.Format( "OAuth {0}", headerBuilder.ToString() );

		return ret;
	}
	
	string GenerateNonce(){
		return new System.Random().Next(123400, int.MaxValue).ToString("X", CultureInfo.InvariantCulture);
	}
	
	string GenerateTimeStamp(){
		// Default implementation of UNIX time of the current UTC time
		TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
		
		return Convert.ToInt64(
			ts.TotalSeconds,
			CultureInfo.CurrentCulture
			)
			.ToString( CultureInfo.CurrentCulture );
	}

	public string GenerateSignature(string reqType, string url, Dictionary<string, string> parameters){
		// 計算に必要なパラメータ用のDictionary
		Dictionary<string, string> nonSecretParams = new Dictionary<string, string>();
		
		// パラメータチェック
		foreach( KeyValuePair<string, string> param in parameters )
		{
			bool found = false;
			
			foreach( string secretParam in SECRET_PARAMS )
			{
				// シークレット見つかった？
				if( secretParam == param.Key )
				{
					found = true;
					break;
				}
			}
			// シークレット系以外のパラメータのリスト化
			if( !found ){
				nonSecretParams.Add( param.Key, param.Value );
			}
		}
		
		// 計算の元となる文字列の作成
		string base_str = string.Format(
			#if USE_CULTURE
			CultureInfo.InvariantCulture,
			#endif
			"{0}&{1}&{2}",
			reqType,
			UrlEncode( NormalizeUrl( url ) ),
			makeStringForSignature( nonSecretParams )
			);
		Debug.Log (base_str);
		// ハッシュ生成用のキー
		string key = string.Format(
			#if USE_CULTURE
			CultureInfo.InvariantCulture,
			#endif
			"{0}&{1}",
			UrlEncode( parameters["oauth_consumer_secret"] ),
			parameters.ContainsKey("oauth_token_secret") ? UrlEncode(parameters["oauth_token_secret"]) : string.Empty
			);
		Debug.Log(key);
		// ハッシュ生成
		HMACSHA1 hmacsha1 = new HMACSHA1( Encoding.UTF8.GetBytes(key) );
		
		string str_signature = Convert.ToBase64String(
			hmacsha1.ComputeHash(
			Encoding.UTF8.GetBytes( base_str )
			)
			);
		
		return str_signature;
	}

	public void GetAccessToken(){
		StartCoroutine(coGetAccessToken(pincode));
	}

	private IEnumerator coGetAccessToken(string pincode){
		Hashtable hash = new Hashtable();
		hash[STR_AUTHOR] = makeAccessTokenHeader(pincode);

		byte[] dummy = new byte[1];
		dummy[0] = 0;

		WWW www = new WWW(STR_ACCESS_TOKEN_URL, dummy, hash);
		yield return www;

		if(string.IsNullOrEmpty(www.error)){
			// トークン文字列とユーザーID,名前の取得
            m_AccessToken       = Regex.Match(www.text, @"oauth_token=([^&]+)").Groups[1].Value;
            m_AccessTokenSecret = Regex.Match(www.text, @"oauth_token_secret=([^&]+)").Groups[1].Value;
            m_UserId            = Regex.Match(www.text, @"user_id=([^&]+)").Groups[1].Value;
            m_ScreenName        = Regex.Match(www.text, @"screen_name=([^&]+)").Groups[1].Value;

            if( !string.IsNullOrEmpty(m_AccessToken) &&
                !string.IsNullOrEmpty(m_AccessTokenSecret) &&
                !string.IsNullOrEmpty(m_UserId) &&
                !string.IsNullOrEmpty(m_ScreenName) )
            {
                string log = "OnAccessTokenCallback - succeeded";
                log += "\n    UserId : " + m_UserId;
                log += "\n    ScreenName : " + m_ScreenName;
                log += "\n    Token : " + m_AccessToken;
                log += "\n    TokenSecret : " + m_AccessTokenSecret;
                Debug.Log( log );

                // PlayerPrefsに保存
                PlayerPrefs.SetString( STR_PPREFS_USER_ID, m_UserId );
                PlayerPrefs.SetString( STR_PPREFS_USER_NAME, m_ScreenName );
                PlayerPrefs.SetString( STR_PPREFS_USER_ATOKEN, m_AccessToken );
                PlayerPrefs.SetString( STR_PPREFS_USER_ATOKEN_SECRET, m_AccessTokenSecret );
            }
            else{   // トークン取れなかったらエラー扱い
                Debug.Log( string.Format("GetAccessToken - failed. response : {0}", www.text) );
            }
		}
		else{

		}
	}

	string makeAccessTokenHeader(string pincode){
		Dictionary<string, string> header = new Dictionary<string, string>();

		header.Add("oauth_version", "1.0");
		header.Add("oauth_nonce", GenerateNonce());
		header.Add("oauth_timestamp", GenerateTimeStamp());
		header.Add("oauth_signature_method", "HMAC-SHA1");
		header.Add("oauth_consumer_key", consumerKey);
		header.Add("oauth_consumer_secret", consumerSecret);

		header.Add("oauth_token", request_token);
		header.Add("oauth_verifier", pincode);

		string signature = GenerateSignature(
			"POST",
			STR_ACCESS_TOKEN_URL,
			header);
		header.Add("oauth_signature", signature);

		SortedDictionary<string, string> sortedParams = new SortedDictionary<string, string>();
        foreach( KeyValuePair<string, string> param in header )
        {
            foreach( string oauth_header_param in OAUTH_HEADER_PARAMS )
            {
                if( oauth_header_param.Contains( param.Key ) ){
                    sortedParams.Add( param.Key, param.Value );
                }
            }
        }


        // ソートされたパラメータをエスケープしてガッチャンコ
        StringBuilder headerBuilder = new StringBuilder();
        bool bFirst = true;
        foreach( var item in sortedParams )
        {
            if( bFirst ){
                bFirst = false;
                headerBuilder.AppendFormat(
                    "{0}=\"{1}\"",
                    UrlEncode(item.Key),
                    UrlEncode(item.Value));
            }
            else{   // 2番目以降の値は , 付き
                headerBuilder.AppendFormat(
                    ",{0}=\"{1}\"",
                    UrlEncode(item.Key),
                    UrlEncode(item.Value)
                );
            }
        }

        // 完成
        string ret = string.Format( "OAuth {0}", headerBuilder.ToString() );

        return ret;

	}
	// URLを正規化して返す
	public string   NormalizeUrl( string url )
	{
		Uri uri = new Uri(url);
		
		string normalizedUrl = string.Format(
			#if USE_CULTURE
			CultureInfo.InvariantCulture,
			#endif
			"{0}://{1}",
			uri.Scheme,
			uri.Host
			);
		
		if( !( (uri.Scheme == "http" && uri.Port == 80) ||
		      (uri.Scheme == "https" && uri.Port == 443)
		      )
		   )
		{
			normalizedUrl += ":" + uri.Port;
		}
		
		normalizedUrl += uri.AbsolutePath;
		
		return normalizedUrl;
	}

	string UrlEncode(string value){
		if( string.IsNullOrEmpty(value) ){
			return string.Empty;
		}
		
		value = Uri.EscapeDataString( value );
		
		// OAuth用にアルファベットを大文字にする 例：%2F
		value = Regex.Replace(
			value,
			"(%[0-9a-f][0-9a-f])",
			c => c.Value.ToUpper()
			);
		
		// HttpUtility.UrlEncodeメソッドではエンコードされない文字のエンコード
		value = value
			.Replace("(", "%28")
				.Replace(")", "%29")
				.Replace("$", "%24")
				.Replace("!", "%21")
				.Replace("*", "%2A")
				.Replace("'", "%27");
		
		// ？
		value = value.Replace("%7E", "~");
		
		return value;
	}

	// パラメータリストを繋いでシグネチャ計算用の文字列を作る
	private string   makeStringForSignature( IEnumerable<KeyValuePair<string, string>> parameters )
	{
		StringBuilder parameterString = new StringBuilder();
		
		// アルファベット順にソート
		SortedDictionary<string, string> paramsSorted = new SortedDictionary<string, string>();
		foreach( KeyValuePair<string, string> param in parameters )
		{
			paramsSorted.Add( param.Key, param.Value );
		}
		
		// a=b&c=d&...
		foreach( var item in paramsSorted )
		{
			if( parameterString.Length > 0 ){
				parameterString.Append("&");
			}
			
			parameterString.Append(
				string.Format(
				#if USE_CULTURE
				CultureInfo.InvariantCulture,
				#endif
				"{0}={1}",
				UrlEncode( item.Key ),
				UrlEncode( item.Value )
				)
				);
		}
		
		// エスケープが必要なのでエスケープして返す
		return UrlEncode( parameterString.ToString() );
	}
}
