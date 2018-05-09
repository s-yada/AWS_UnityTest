using System;
using System.IO;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Amazon;
using Amazon.CognitoIdentity;
using Amazon.S3;


public class AWSConnector:MonoBehaviour
{
    const string IDENTITY_POOL_ID = "us-east-2:930bdf23-ed85-4772-ac72-4e546ba6ad2e";
    const string BUCKET_NAME = "test.unity";
    const string AWS_ACCOUNT = "819102951089";
    const string AUTH_ARN = "arn:aws:iam::819102951089:role/Cognito_UnityTestAuth_Role";
    const string UNAUTH_ARN = "arn:aws:iam::819102951089:role/Cognito_UnityTestUnauth_Role";

    static RegionEndpoint COGNITO_REGION = RegionEndpoint.USEast2; // Cognitリージョン
    static RegionEndpoint S3_REGION = RegionEndpoint.APNortheast1; // S3リージョン

    public GameObject TekisutoHonbun;

    CognitoAWSCredentials credentials;

    private void Start()
    {
        UnityInitializer.AttachToGameObject(this.gameObject); //AwsPrefab~のエラー対策
        AWSConfigs.HttpClient = AWSConfigs.HttpClientOption.UnityWebRequest; //Cannot override~のエラー対策

        // Amazon Cognito 認証情報プロバイダーを初期化、
        //Invalid identity pool~のエラー対策のため情報多め
        credentials = new CognitoAWSCredentials(
            AWS_ACCOUNT,　//追加
            IDENTITY_POOL_ID,
            UNAUTH_ARN,　//追加
            AUTH_ARN,　//追加
            COGNITO_REGION
        );

        //ダウンロードコルーチンの実行
        StartCoroutine(GetTextFromS3Coroutine("test.txt", true,(a,b)=> {
            if (a)
            {
                TekisutoHonbun.GetComponent<Text>().text = b;
                Debug.Log(b);
            }
        }));

    }

    
    ////////コンストラクタ内で定義すると「AwsPrefab~」のエラーが発生するので　void Start()　内へ転記////////
    //public AWSConnector()
    //{
    //    // Amazon Cognito 認証情報プロバイダーを初期化します
    //    credentials = new CognitoAWSCredentials(
    //        IDENTITY_POOL_ID,
    //        COGNITO_REGION
    //    );
    //}


    /// <summary>
    /// S3からテキストデータを取得するコルーチンです
    /// </summary>
    /// <returns>The text data coroutine.</returns>
    /// <param name="fileName">File name.</param>
    /// <param name="isForceDownload">If set to <c>true</c> is force download.</param>
    /// <param name="onFinished">On finished.</param>
    public IEnumerator GetTextFromS3Coroutine(string fileName, bool isForceDownload, Action<bool, string> onFinished)
    {
        var s3Client = new AmazonS3Client(credentials, S3_REGION);

        var cachePath = string.Format("{0}/{1}", Application.temporaryCachePath, fileName);
        if (!isForceDownload && File.Exists(cachePath))
        {
            onFinished(true, File.ReadAllText(cachePath));
            yield break;
        }

        var dirName = Path.GetDirectoryName(cachePath);
        if (!Directory.Exists(dirName))
        {
            Directory.CreateDirectory(dirName);
        }

        var isFinished = false;
        var result = false;
        var resultText = "";
        s3Client.GetObjectAsync(BUCKET_NAME, fileName, (responseObject) =>
        {
            isFinished = true;

            if (null != responseObject.Exception)
            {
                resultText = responseObject.Exception.ToString();
                return;
            }

            result = true;
            var response = responseObject.Response;
            if (null != response.ResponseStream)
            {
                using (var reader = new StreamReader(response.ResponseStream))
                {
                    resultText = reader.ReadToEnd();
                }
            }
            else
            {
                resultText = "";
            }
        });

        while (!isFinished)
        {
            yield return new WaitForSecondsRealtime(0.1f);
        }

        if (result && resultText.Length > 0)
        {
            File.WriteAllText(cachePath, resultText);
        }

        onFinished(true, resultText);
    }

}
