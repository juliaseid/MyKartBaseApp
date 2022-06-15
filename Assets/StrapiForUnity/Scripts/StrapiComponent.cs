﻿﻿using System;
using System.Collections;
using System.Collections.Generic;
 using LitJson;
using StrapiForUnity;
using UnityEngine;
using Proyecto26;

public class StrapiComponent : MonoBehaviour
{
    public event Action<AuthResponse> OnAuthSuccess = delegate {  };
    public event Action<Exception> OnAuthFail = delegate {  };
    public event Action OnAuthStarted = delegate {  };
    public event Action NoStoredJWT = delegate { };
    
    [Tooltip("The root URL of your Strapi server. For example: http://localhost:1337")]
    public string BaseURL;
    
    [Tooltip("The secret key used for creating the JWT on your Strapi server. This is used to check that the stored JWT is valid. The secret can be found in your Strapi installation, at 'strapi/extensions/users-permissions/config/jwt.js'")]
    public string JWTSecret;
    
    public StrapiUser AuthenticatedUser;
    public bool IsAuthenticated = false;
    public string ErrorMessage;

    private bool rememberMe = true;

    //Singleton
    private static StrapiComponent _instance;
    public static StrapiComponent Instance { get { return _instance; } }
    
    protected virtual void Awake()
    {
        if (!BaseURL.EndsWith("/"))
        {
            BaseURL += "/";
        }
        
        //Singleton
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        } else {
            _instance = this;
            DontDestroyOnLoad(this);
        }

        OnAuthSuccess += OnAuthSuccessHandler;
    }
    
    // Start is called before the first frame update
    public void Start()
    {
        checkForSavedJWT();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void TriggerAuthSuccessEvent(AuthResponse authResponse)
    {
        OnAuthSuccess.Invoke(authResponse);
    }

    public void TriggerAuthFailEvent(Exception exception)
    {
        OnAuthFail.Invoke(exception);
    }

    public void TriggerOnAuthStartedEvent()
    {
        OnAuthStarted.Invoke();
    }

    private void checkForSavedJWT()
    {
        string jwt = PlayerPrefs.GetString("jwt","");
        if (jwt != "" && isValidJWT(jwt))
        {
            LoginWithJwt(jwt);
        }
        else
        {
            NoStoredJWT?.Invoke();
        }
    }

    private bool isValidJWT(string jwt)
    {
        if (JWTSecret=="")
        {
            Debug.Log("Couldn't validate stored JWT. You should consider setting your Strapi Component JWT secret.");
            return true;
        }
        
        try
        {
            JsonData jsonPayload = JWT.JsonWebToken.DecodeToObject(jwt, JWTSecret);
            //Debug.Log(jsonPayload);
            DateTime exp = ConvertFromUnixTimestamp(double.Parse(jsonPayload["exp"].ToString()));
            if (exp > DateTime.UtcNow.AddMinutes(1))
            {
                return true;
            }
            else // the token has expired
            {
                Debug.Log("The token has expired. Deleting.");
                PlayerPrefs.DeleteKey("jwt");
            }
        }
        catch (JWT.SignatureVerificationException)
        {
            Debug.LogError("Invalid Strapi token");
        }

        return false;
    }
    
    /// <summary>
    /// Converts a Unix timestamp into a System.DateTime
    /// </summary>
    /// <param name="timestamp">The Unix timestamp in milliseconds to convert, as a double</param>
    /// <returns>DateTime obtained through conversion</returns>
    public static DateTime ConvertFromUnixTimestamp(double timestamp)
    {
        DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);
        return origin.AddSeconds(timestamp);
    }
    
    public void Login(string identifier, string password, bool rememberMe = false)
    {
        OnAuthStarted?.Invoke();
        this.rememberMe = rememberMe;
        
        var jsonString = "{" +
                         "\"identifier\":\"" + identifier + "\"," +
                         "\"password\":\"" + password + "\"" +
                         "}";

        PostAuthRequest("auth/local", jsonString);
    }

    public void Register(string username, string email, string password, bool rememberMe = false, Dictionary<string,string> extraAttributes = null)
    {
        OnAuthStarted?.Invoke();
        this.rememberMe = rememberMe;

        var jsonString = "{" +
                         "\"username\":\"" + username + "\"," +
                         "\"email\":\"" + email + "\"," +
                         "\"password\":\"" + password + "\"";
        if (extraAttributes != null && extraAttributes.Count > 0)
        {
            foreach (var attribute in extraAttributes)
            {
                jsonString += "\""+attribute.Key+"\":\"" + attribute.Value + "\",";
            }
            jsonString = jsonString.Remove(jsonString.Length - 1); // removes the final trailing comma
        }
        jsonString += "}";
        
        PostAuthRequest("auth/local/register", jsonString);
    }

    public virtual void PostAuthRequest(string endpoint, string jsonString)
    {
        RestClient.Post<AuthResponse>(BaseURL + endpoint,jsonString).Then(authResponse =>
        {
            OnAuthSuccess?.Invoke(authResponse);
        }).Catch(err =>
        {
            OnAuthFail?.Invoke(err);
            Debug.Log($"Authentication Error: {err}");
        });
    }
    
    /// <summary>
    /// Login with an authentication token
    /// </summary>
    /// <param name="jwt"></param>
    public virtual void LoginWithJwt(string jwt)
    {
        OnAuthStarted?.Invoke();
        
        RestClient.DefaultRequestHeaders["Authorization"] = "Bearer " + jwt;
        
        RestClient.Get<StrapiUser>(BaseURL + "users/me").Then(response => {
            Debug.Log(response);
            AuthResponse authResponse = new AuthResponse()
            {
                jwt = jwt,
                user = response
            };
            OnAuthSuccess?.Invoke(authResponse);
        }).Catch(err =>
        {
            OnAuthFail?.Invoke(err);
            Debug.Log($"Authentication Error: {err}");
        });
    }

    public void OnAuthSuccessHandler(AuthResponse authResponse)
    {
        AuthenticatedUser = authResponse.user;
        IsAuthenticated = true;
        
        RestClient.DefaultRequestHeaders["Authorization"] = "Bearer " + authResponse.jwt;

        if (rememberMe && authResponse.jwt != null)
        {
            PlayerPrefs.SetString("jwt",authResponse.jwt);
        }
        Debug.Log($"Successfully authenticated. Welcome {AuthenticatedUser.username}");
    }
}
