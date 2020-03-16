using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public enum InputType
{ Move, Look, Jump, Submit }

[CreateAssetMenu(fileName = "New InputObject", menuName = "Generators/Input/InputObject")]
public class InputObject : ScriptableObject
{
    #region Scaffold
    private Dictionary<InputType, InputHandler> events;

    private void OnEnable()
    {
        events = new Dictionary<InputType, InputHandler>();
        foreach(InputType type in Enum.GetValues(typeof(InputType)))
        {
            events.Add(type, new InputHandler());
        }
    }

    private void OnDisable() => events.Clear();

    private class InputHandler
    {
        public Action<InputAction.CallbackContext> handler;
    }
    #endregion

    #region Subscriptions
    public void Listen(InputType type, Action<InputAction.CallbackContext> callback) => events[type].handler += callback;
    public void Ignore(InputType type, Action<InputAction.CallbackContext> callback) => events[type].handler -= callback;
    public void Clear(InputType type) => events[type].handler = null;
    #endregion

    #region Player Callbacks
    public void OnMove(InputAction.CallbackContext context) => events[InputType.Move].handler?.Invoke(context);
    public void OnLook(InputAction.CallbackContext context) => events[InputType.Look].handler?.Invoke(context);
    public void OnJump(InputAction.CallbackContext context) => events[InputType.Jump].handler?.Invoke(context);
    #endregion

    #region UI Callbacks
    public void OnSubmit(InputAction.CallbackContext context) => events[InputType.Submit].handler?.Invoke(context);
    #endregion
}
