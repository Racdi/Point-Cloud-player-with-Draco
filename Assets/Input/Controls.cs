//------------------------------------------------------------------------------
// <auto-generated>
//     This code was auto-generated by com.unity.inputsystem:InputActionCodeGenerator
//     version 1.6.1
//     from Assets/Input/Controls.inputactions
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

public partial class @Controls: IInputActionCollection2, IDisposable
{
    public InputActionAsset asset { get; }
    public @Controls()
    {
        asset = InputActionAsset.FromJson(@"{
    ""name"": ""Controls"",
    ""maps"": [
        {
            ""name"": ""XR"",
            ""id"": ""513107af-465d-463e-b58f-4b7b0eef15ff"",
            ""actions"": [
                {
                    ""name"": ""HideUI"",
                    ""type"": ""Button"",
                    ""id"": ""cb0a210b-e7f8-4292-bf10-fbabacd81149"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""ShowUI"",
                    ""type"": ""Button"",
                    ""id"": ""b8238fc5-588d-464f-aa8e-0ebc6cc53ffa"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""5c3560c8-4292-45be-b203-4f6016360352"",
                    ""path"": ""*/{PrimaryButton}"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""XRControlScheme"",
                    ""action"": ""ShowUI"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""4a4f9d3c-f894-49ec-96ec-6966d73eecb4"",
                    ""path"": ""*/{SecondaryButton}"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""XRControlScheme"",
                    ""action"": ""HideUI"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        }
    ],
    ""controlSchemes"": [
        {
            ""name"": ""XRControlScheme"",
            ""bindingGroup"": ""XRControlScheme"",
            ""devices"": [
                {
                    ""devicePath"": ""<XRController>{RightHand}"",
                    ""isOptional"": false,
                    ""isOR"": false
                },
                {
                    ""devicePath"": ""<XRController>{LeftHand}"",
                    ""isOptional"": false,
                    ""isOR"": false
                },
                {
                    ""devicePath"": ""<XRHMD>"",
                    ""isOptional"": false,
                    ""isOR"": false
                }
            ]
        }
    ]
}");
        // XR
        m_XR = asset.FindActionMap("XR", throwIfNotFound: true);
        m_XR_HideUI = m_XR.FindAction("HideUI", throwIfNotFound: true);
        m_XR_ShowUI = m_XR.FindAction("ShowUI", throwIfNotFound: true);
    }

    public void Dispose()
    {
        UnityEngine.Object.Destroy(asset);
    }

    public InputBinding? bindingMask
    {
        get => asset.bindingMask;
        set => asset.bindingMask = value;
    }

    public ReadOnlyArray<InputDevice>? devices
    {
        get => asset.devices;
        set => asset.devices = value;
    }

    public ReadOnlyArray<InputControlScheme> controlSchemes => asset.controlSchemes;

    public bool Contains(InputAction action)
    {
        return asset.Contains(action);
    }

    public IEnumerator<InputAction> GetEnumerator()
    {
        return asset.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Enable()
    {
        asset.Enable();
    }

    public void Disable()
    {
        asset.Disable();
    }

    public IEnumerable<InputBinding> bindings => asset.bindings;

    public InputAction FindAction(string actionNameOrId, bool throwIfNotFound = false)
    {
        return asset.FindAction(actionNameOrId, throwIfNotFound);
    }

    public int FindBinding(InputBinding bindingMask, out InputAction action)
    {
        return asset.FindBinding(bindingMask, out action);
    }

    // XR
    private readonly InputActionMap m_XR;
    private List<IXRActions> m_XRActionsCallbackInterfaces = new List<IXRActions>();
    private readonly InputAction m_XR_HideUI;
    private readonly InputAction m_XR_ShowUI;
    public struct XRActions
    {
        private @Controls m_Wrapper;
        public XRActions(@Controls wrapper) { m_Wrapper = wrapper; }
        public InputAction @HideUI => m_Wrapper.m_XR_HideUI;
        public InputAction @ShowUI => m_Wrapper.m_XR_ShowUI;
        public InputActionMap Get() { return m_Wrapper.m_XR; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public bool enabled => Get().enabled;
        public static implicit operator InputActionMap(XRActions set) { return set.Get(); }
        public void AddCallbacks(IXRActions instance)
        {
            if (instance == null || m_Wrapper.m_XRActionsCallbackInterfaces.Contains(instance)) return;
            m_Wrapper.m_XRActionsCallbackInterfaces.Add(instance);
            @HideUI.started += instance.OnHideUI;
            @HideUI.performed += instance.OnHideUI;
            @HideUI.canceled += instance.OnHideUI;
            @ShowUI.started += instance.OnShowUI;
            @ShowUI.performed += instance.OnShowUI;
            @ShowUI.canceled += instance.OnShowUI;
        }

        private void UnregisterCallbacks(IXRActions instance)
        {
            @HideUI.started -= instance.OnHideUI;
            @HideUI.performed -= instance.OnHideUI;
            @HideUI.canceled -= instance.OnHideUI;
            @ShowUI.started -= instance.OnShowUI;
            @ShowUI.performed -= instance.OnShowUI;
            @ShowUI.canceled -= instance.OnShowUI;
        }

        public void RemoveCallbacks(IXRActions instance)
        {
            if (m_Wrapper.m_XRActionsCallbackInterfaces.Remove(instance))
                UnregisterCallbacks(instance);
        }

        public void SetCallbacks(IXRActions instance)
        {
            foreach (var item in m_Wrapper.m_XRActionsCallbackInterfaces)
                UnregisterCallbacks(item);
            m_Wrapper.m_XRActionsCallbackInterfaces.Clear();
            AddCallbacks(instance);
        }
    }
    public XRActions @XR => new XRActions(this);
    private int m_XRControlSchemeSchemeIndex = -1;
    public InputControlScheme XRControlSchemeScheme
    {
        get
        {
            if (m_XRControlSchemeSchemeIndex == -1) m_XRControlSchemeSchemeIndex = asset.FindControlSchemeIndex("XRControlScheme");
            return asset.controlSchemes[m_XRControlSchemeSchemeIndex];
        }
    }
    public interface IXRActions
    {
        void OnHideUI(InputAction.CallbackContext context);
        void OnShowUI(InputAction.CallbackContext context);
    }
}
