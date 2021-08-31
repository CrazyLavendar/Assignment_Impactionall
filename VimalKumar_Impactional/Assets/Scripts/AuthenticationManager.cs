using UnityEngine;
using System.Collections.Generic;

using Amazon.Extensions.CognitoAuthentication;
using Amazon.CognitoIdentity;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using System;
using System.Threading.Tasks;
using System.Net;

//For Debugging
using UnityEngine.UI;

public class AuthenticationManager : MonoBehaviour
{
   // the AWS region of where your services live
   public static Amazon.RegionEndpoint Region = Amazon.RegionEndpoint.APSouth1;

    // Credentials From Impactional Games
   const string IdentityPool = "833ccbe9-e1a5-42fe-9cfc-f00c49cf1e8a";
   const string AppClientID = "5olhj4b8jufbf38unu4c5co8hm"; 
   const string userPoolId = "ap-south-1_tDGvOIUiG";

   private AmazonCognitoIdentityProviderClient _provider;
   private CognitoAWSCredentials _cognitoAWSCredentials;
   private static string _userid = "";
   private CognitoUser _user;

    public Text debugText;
   public async Task<bool> RefreshSession()
   {
      Debug.Log("RefreshSession");

      DateTime issued = DateTime.Now;
      UserSessionCache userSessionCache = new UserSessionCache();
      SaveDataManager.LoadJsonData(userSessionCache);

      try
      {
         CognitoUserPool userPool = new CognitoUserPool(userPoolId, AppClientID, _provider);

         // apparently the username field can be left blank for a token refresh request
         CognitoUser user = new CognitoUser("", AppClientID, userPool, _provider);
         // will fail Using DateTime.Now.AddHours(1) is a workaround for https://github.com/aws/aws-sdk-net-extensions-cognito/issues/24
         user.SessionTokens = new CognitoUserSession(
            userSessionCache.getIdToken(),
            userSessionCache.getAccessToken(),
            userSessionCache.getRefreshToken(),
            issued,
            DateTime.Now.AddDays(30)); // TODO: need to investigate further. 
                                       // It was my understanding that this should be set to when your refresh token expires...

         // Attempt refresh token call
         AuthFlowResponse authFlowResponse = await user.StartWithRefreshTokenAuthAsync(new InitiateRefreshTokenAuthRequest
         {
            AuthFlowType = AuthFlowType.REFRESH_TOKEN_AUTH
         })
         .ConfigureAwait(false);

         // Debug.Log("User Access Token after refresh: " + token);
         Debug.Log("User refresh token successfully updated!");

         // update session cache
         UserSessionCache userSessionCacheToUpdate = new UserSessionCache(
            authFlowResponse.AuthenticationResult.IdToken,
            authFlowResponse.AuthenticationResult.AccessToken,
            authFlowResponse.AuthenticationResult.RefreshToken,
            userSessionCache.getUserId());

         SaveDataManager.SaveJsonData(userSessionCacheToUpdate);

         // update credentials with the latest access token
         _cognitoAWSCredentials = user.GetCognitoAWSCredentials(IdentityPool, Region);
         _user = user;
         return true;
      }
      catch (NotAuthorizedException ne)
      {
            //debugText.text = ne.Message;
            // https://docs.aws.amazon.com/cognito/latest/developerguide/amazon-cognito-user-pools-using-tokens-with-identity-providers.html
            // refresh tokens will expire - user must login manually every x days (see user pool -> app clients -> details)
            Debug.Log("NotAuthorizedException: " + ne);
      }
      catch (WebException webEx)
      {
            //debugText.text = webEx.Message;
            // we get a web exception when we cant connect to aws - means we are offline
            Debug.Log("WebException: " + webEx);
      }
      catch (Exception ex)
      {
           // debugText.text = ex.Message;
            Debug.Log("Exception: " + ex);
      }
        
        return false;
   }
   public async Task<bool> Login(string email, string password)
   {
      CognitoUserPool userPool = new CognitoUserPool(userPoolId, AppClientID, _provider);
      CognitoUser user = new CognitoUser(email, AppClientID, userPool, _provider);

      InitiateSrpAuthRequest authRequest = new InitiateSrpAuthRequest()
      {
         Password = password
      };

      try
      {
         AuthFlowResponse authFlowResponse = await user.StartWithSrpAuthAsync(authRequest).ConfigureAwait(false);

         _userid = await GetUserIdFromProvider(authFlowResponse.AuthenticationResult.AccessToken);
         // Debug.Log("Users unique ID from cognito: " + _userid);

         UserSessionCache userSessionCache = new UserSessionCache(
            authFlowResponse.AuthenticationResult.IdToken,
            authFlowResponse.AuthenticationResult.AccessToken,
            authFlowResponse.AuthenticationResult.RefreshToken,
            _userid);

         SaveDataManager.SaveJsonData(userSessionCache);

         _cognitoAWSCredentials = user.GetCognitoAWSCredentials(IdentityPool, Region);

         _user = user;

         return true;
      }
      catch (Exception e)
      {
            //debugText.text = "Login Failed";
            Debug.Log("Login failed, exception: " + e);
            return false;
      }
   }

   public async Task<bool> Signup(string username, string email, string password)
   {
        Debug.Log("SignUpRequest: " + username + ", " + email + ", " + password);
      SignUpRequest signUpRequest = new SignUpRequest()
      {
         ClientId = AppClientID,
         Username = email,
         Password = password
      };

      // must provide all attributes required by the User Pool that you configured
      List<AttributeType> attributes = new List<AttributeType>()
      {
         new AttributeType(){
            Name = "email", Value = email
         },
         new AttributeType(){
            Name = "preferred_username", Value = username
         }
      };
      signUpRequest.UserAttributes = attributes;

      try
      {
         SignUpResponse sighupResponse = await _provider.SignUpAsync(signUpRequest);
         Debug.Log("Sign up successful");
         debugText.text = "Enter the confirm code";
            return true;
      }
      catch (Exception e)
      {
        debugText.text = e.Message;
         Debug.Log("Sign up failed, exception: " + e);
         return false;
      }
   }

    public async Task<bool> Confirm(string username, string email, string confirmText)
    {
        ConfirmSignUpRequest confirmReq = new ConfirmSignUpRequest()
        {
            Username = username,
            ClientId = AppClientID,
            ConfirmationCode = confirmText
        };

        try
        {
            ConfirmSignUpResponse confirmResponse = await _provider.ConfirmSignUpAsync(confirmReq);
            Debug.Log("Coniration user successful");
            debugText.text = "Confirmation Success";
            return true;
        }
        catch (Exception e)
        {
           debugText.text = e.Message;
            Debug.Log("Conirmation user successful failed, exception: " + e);
            return false;
        }

    }

    public string GetUsersId()
   {
      // Debug.Log("GetUserId: [" + _userid + "]");
      if (_userid == null || _userid == "")
      {
         // load userid from cached session 
         UserSessionCache userSessionCache = new UserSessionCache();
         SaveDataManager.LoadJsonData(userSessionCache);
         _userid = userSessionCache.getUserId();
      }
      return _userid;
   }

   // we call this once after the user is authenticated, then cache it as part of the session for later retrieval 
   private async Task<string> GetUserIdFromProvider(string accessToken)
   {
      // Debug.Log("Getting user's id...");
      string subId = "";

      Task<GetUserResponse> responseTask =
         _provider.GetUserAsync(new GetUserRequest
         {
            AccessToken = accessToken
         });

      GetUserResponse responseObject = await responseTask;

      // set the user id
      foreach (var attribute in responseObject.UserAttributes)
      {
         if (attribute.Name == "sub")
         {
            subId = attribute.Value;
            break;
         }
      }

      return subId;
   }


   public async void SignOut()
   {
      await _user.GlobalSignOutAsync();

      UserSessionCache userSessionCache = new UserSessionCache("", "", "", "");
      SaveDataManager.SaveJsonData(userSessionCache);

      Debug.Log("user logged out.");
   }

 
   public CognitoAWSCredentials GetCredentials()
   {
      return _cognitoAWSCredentials;
   }

   public string GetAccessToken()
   {
      UserSessionCache userSessionCache = new UserSessionCache();
      SaveDataManager.LoadJsonData(userSessionCache);
      return userSessionCache.getAccessToken();
   }

   void Awake()
   {
      Debug.Log("AuthenticationManager: Awake");
      _provider = new AmazonCognitoIdentityProviderClient(new Amazon.Runtime.AnonymousAWSCredentials(), Region);
   }
}
