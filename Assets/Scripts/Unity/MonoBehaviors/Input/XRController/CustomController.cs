using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;
using System;


public delegate void ClickedEvent(object sender);

public class CustomController : MonoBehaviour
{
    [SerializeField]private ActionBasedController _controller;
    
    public event ClickedEvent TriggerUnclicked;
    public event ClickedEvent MenuButtonClicked;
    public event ClickedEvent MenuButtonUnclicked;
    public event ClickedEvent Gripped;
    public event ClickedEvent Ungripped;
    public event ClickedEvent TriggerClicked;

    private void Awake(){
        _controller = GetComponent<ActionBasedController>();
    }

    private void OnEnable(){
        _controller.selectAction.action.performed += OnTriggerClicked;
        _controller.selectAction.action.canceled += OnTriggerUnclicked;

        _controller.activateAction.action.performed += OnGripped;
        _controller.activateAction.action.canceled += OnUngripped;

        _controller.uiPressAction.action.performed += OnMenuButtonClicked;
        _controller.uiPressAction.action.canceled += OnMenuButtonUnclicked;
    }

    

    private void OnDisable(){
         _controller.selectAction.action.performed -= OnTriggerClicked;
         _controller.selectAction.action.canceled -= OnTriggerUnclicked;

        _controller.activateAction.action.performed -= OnGripped;
        _controller.activateAction.action.canceled -= OnUngripped;

        _controller.uiPressAction.action.performed -= OnMenuButtonClicked;
         _controller.uiPressAction.action.canceled -= OnMenuButtonUnclicked;
    }

    private void Update(){} 

    #region Event Calls

    private void OnMenuButtonClicked(InputAction.CallbackContext obj)
    {
        Debug.Log("Menu Pressed - Custom Controller");
        if (MenuButtonClicked != null)
			MenuButtonClicked(this);
    }

    private void OnMenuButtonUnclicked(InputAction.CallbackContext obj)
    {
        Debug.Log("Menu Unpressed - Custom Controller");
        if (MenuButtonUnclicked != null)
			MenuButtonUnclicked(this);
    }

    private void OnGripped(InputAction.CallbackContext obj)
    {
        Debug.Log("Grip Pressed - Custom Controller");
        if (Gripped != null)
			Gripped(this);
    }

    private void OnUngripped(InputAction.CallbackContext obj)
    {
        Debug.Log("Grip Unpressed - Custom Controller");
        if (Ungripped != null)
			Ungripped(this);
    }
    
    private void OnTriggerClicked(InputAction.CallbackContext obj)
    {
        Debug.Log("Trigger Pressed - Custom Controller");
        if (TriggerClicked != null)
			TriggerClicked(this);
    }

    private void OnTriggerUnclicked(InputAction.CallbackContext obj)
    {
        Debug.Log("Trigger Unpressed - Custom Controller");
        if (TriggerUnclicked != null)
			TriggerUnclicked(this);
    }
    #endregion

}
