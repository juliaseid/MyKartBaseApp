using System;
using System.Collections;
using System.Collections.Generic;
using StrapiForUnity;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static Teak;

public class LoginOrRegisterForm : MonoBehaviour
{
    public Button LoginToggleButton;
    public Button RegisterToggleButton;
    public Button LoginSubmitButton;
    public Button RegisterSubmitButton;
    public InputField UsernameInput;
    public InputField EmailInput;
    public InputField PasswordInput;
    public Text HeaderText;
    public VerticalLayoutGroup ContainerLayout;
    public GameObject LoadingObject;
    public Toggle RememberMeToggle;
    
    
    public StrapiComponent Strapi;

    public Teak Teak;

    private bool isLoading = false;

    // Start is called before the first frame update
    void Start()
    {
        Teak.Instance;
        Teak.Instance.Trace = true;
        Teak.Instance.RegisterForNotifications();
        if (Strapi == null)
        {
            Debug.LogError("No Strapi component found. Please make sure you've got an active Strapi component assigned to the LoginOrRegisterForm");
            return;
        }

        if (Teak.Instance == null)
        {
            Debug.LogError("No Teak instance found. But at least the Teak type is working correctly!");
            return;
        }

        else 
        {
            Debug.Log("Teak instance created!");
            // Debug.Log("AppID: " + Teak.Instance.AppConfiguration.);
        }
        
        LoginToggleButton.onClick.Invoke();
        LoadingObject.SetActive(false);
        
        registerEventHandlers();
    }

    private void registerEventHandlers()
    {
        Strapi.OnAuthSuccess += handleSuccessfulAuthentication;
        Strapi.OnAuthFail += handleUnsuccessfulAuthentication;
        Strapi.OnAuthStarted += toggleLoading;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            if (LoginSubmitButton.IsActive())
            {
                LoginSubmitButton.onClick.Invoke();
            }
            else
            {
                RegisterSubmitButton.onClick.Invoke();
            }
        }
    }

    public void OnLoginToggle()
    {
        RegisterSubmitButton.gameObject.SetActive(false);
        LoginSubmitButton.gameObject.SetActive(true);
        EmailInput.transform.parent.gameObject.SetActive(false);
        RegisterToggleButton.gameObject.SetActive(true);
        LoginToggleButton.gameObject.SetActive(false);
        HeaderText.text = "Login";
        
        forceLayoutUpdate();
    }

    public void OnRegisterToggle()
    {
        RegisterSubmitButton.gameObject.SetActive(true);
        LoginSubmitButton.gameObject.SetActive(false);
        EmailInput.transform.parent.gameObject.SetActive(true);
        RegisterToggleButton.gameObject.SetActive(false);
        LoginToggleButton.gameObject.SetActive(true);
        HeaderText.text = "Register";
        
        forceLayoutUpdate();
    }
    
    private void forceLayoutUpdate()
    {
        Canvas.ForceUpdateCanvases();
        ContainerLayout.enabled = false;
        ContainerLayout.enabled = true;
    }

    public void OnLoginSubmit()
    {
        Strapi.Login(UsernameInput.text, PasswordInput.text, RememberMeToggle.isOn);
    }

    public void OnRegisterSubmit()
    {
        Strapi.Register(UsernameInput.text, EmailInput.text, PasswordInput.text, RememberMeToggle.isOn);
    }

    public void identifyTeakUser()
    {
        UserConfiguration userConfiguration = new UserConfiguration {
            Email = EmailInput.text
        };
        Console.WriteLine("Working on identifying a user!");
        Teak.Instance.IdentifyUser(UsernameInput.text, userConfiguration);
    }

    private void toggleLoading()
    {
        isLoading = !isLoading;
        LoadingObject.SetActive(isLoading);
    }

    private void handleSuccessfulAuthentication(AuthResponse authUser)
    {
        toggleLoading();
        HeaderText.text = $"Success. Welcome {authUser.user.username}";
        RegisterSubmitButton.gameObject.SetActive(false);
        LoginSubmitButton.gameObject.SetActive(false);
        UsernameInput.transform.parent.gameObject.SetActive(false);
        EmailInput.transform.parent.gameObject.SetActive(false);
        PasswordInput.transform.parent.gameObject.SetActive(false);
        RegisterToggleButton.gameObject.SetActive(false);
        LoginToggleButton.gameObject.SetActive(false);
        RememberMeToggle.gameObject.SetActive(false);
        identifyTeakUser();
    }

    private void handleUnsuccessfulAuthentication(Exception error)
    {
        toggleLoading();
        HeaderText.text = $"Authentication Error: {error.Message}";
    }
}
