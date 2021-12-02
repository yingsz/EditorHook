using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using DotNetDetour;

public class UGUIAlignWindow
{
    private static Dictionary<AlignType,Texture> alignTexture = new Dictionary<AlignType, Texture>();
    public static GUIStyle s_buttonStyle;

    static Type m_toolbarType = typeof(Editor).Assembly.GetType("UnityEditor.Toolbar");
    static Type m_guiViewType = typeof(Editor).Assembly.GetType("UnityEditor.GUIView");
    static Type m_iWindowBackendType = typeof(Editor).Assembly.GetType("UnityEditor.IWindowBackend");
    static PropertyInfo m_windowBackend = m_guiViewType.GetProperty("windowBackend",
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
    static PropertyInfo m_position = m_guiViewType.GetProperty("position",
       BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
    static PropertyInfo m_viewVisualTree = m_iWindowBackendType.GetProperty("visualTree",
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
    static FieldInfo m_imguiContainerOnGui = typeof(IMGUIContainer).GetField("m_OnGUIHandler",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
    static ScriptableObject m_currentToolbar;

    static bool UseHook = true;
    static MethodHook _hook;
    [InitializeOnLoadMethod]
    static void OnInit()
    {
        Texture leftTexture = Resources.Load<Texture>("Textures/Left");
        Texture horizontalCenterTexture = Resources.Load<Texture>("Textures/HorizontalCenter");
        Texture rightTexture = Resources.Load<Texture>("Textures/Right");
        Texture topTexture = Resources.Load<Texture>("Textures/Top");
        Texture verticalCenterTexture = Resources.Load<Texture>("Textures/VerticalCenter");
        Texture bottomTexture = Resources.Load<Texture>("Textures/Bottom");
        Texture horizontalTexture = Resources.Load<Texture>("Textures/Horizontal");
        Texture verticalTexture = Resources.Load<Texture>("Textures/Vertical");
        alignTexture.Add(AlignType.Left, leftTexture);
        alignTexture.Add(AlignType.HorizontalCenter, horizontalCenterTexture);
        alignTexture.Add(AlignType.Right, rightTexture);
        alignTexture.Add(AlignType.Top, topTexture);
        alignTexture.Add(AlignType.VerticalCenter, verticalCenterTexture);
        alignTexture.Add(AlignType.Bottom, bottomTexture);
        alignTexture.Add(AlignType.Horizontal, horizontalTexture);
        alignTexture.Add(AlignType.Vertical, verticalTexture);
        if(UseHook)
        {
           var  assembly = Assembly.Load("UnityEditor.UIServiceModule");
            var targetType = assembly.GetType("UnityEditor.UnityMainToolbar");
            var targetMethod = targetType.GetMethod("OnGUI", BindingFlags.Public | BindingFlags.Instance);
            var inJectMethod = typeof(HookToolBar).GetMethod("OnMainToolBarGUI");
            if(_hook != null)
            {
                _hook.Uninstall();
            }
            _hook = new MethodHook(targetMethod, inJectMethod, typeof(HookToolBar).GetMethod("OnGUI"));
            _hook.Install();
        }
        else
        {
            EditorApplication.update -= OnUpdate;
            EditorApplication.update += OnUpdate;
        }
        /*var assemblys = AppDomain.CurrentDomain.GetAssemblies();
        Type targetType = null;
        foreach(var assembly in assemblys)
        {
            foreach (var t in assembly.GetTypes())
            {
                if (t.FullName.Contains("UnityMainToolbar"))
                {
                    targetType = t;
                    Debug.Log(t.FullName);
                }
                    
            }
        }
        //  var targetType = typeof(Editor).Assembly.GetType("UnityEditor.UnityMainToolbar");
        var methods = targetType.GetMethods();
       */

        /*var assembly = Assembly.Load("UnityEditor.UIServiceModule");
        var targetType = assembly.GetType("UnityEditor.UnityMainToolbar");
        foreach (var methodinfo in targetType.GetMethods())
        {
            Debug.Log(methodinfo.Name);
        }

        var targetMethod = targetType.GetMethod("OnGUI", BindingFlags.Public | BindingFlags.Instance);  
        var inJectMethod = typeof(UGUIAlignWindow).GetMethod("OnMainToolBarGUI");
        ReplaceMethod(targetMethod, inJectMethod);*/

    }

    private static void ReplaceMethod(MethodInfo target, MethodInfo inject)
    {
        RuntimeHelpers.PrepareMethod(target.MethodHandle);
        RuntimeHelpers.PrepareMethod(inject.MethodHandle);
        unsafe
        {
            long* tarPtr = (long*)target.MethodHandle.Value.ToPointer();
            long* injectPtr = (long*)inject.MethodHandle.Value.ToPointer();
            tarPtr++;
            injectPtr++;

            byte* tarBytePtr = (byte*)(*tarPtr);
            byte* injectBytePtr = (byte*)(*injectPtr);

            int* tarIntPtr = (int*) (tarBytePtr + 1);
            int* injectIntPtr = (int*)(injectBytePtr + 1);

           // *tarIntPtr = (((int)injectBytePtr+5)+*injectIntPtr) -((int)tarBytePtr + 5);
        }
    }

    class HookToolBar
    {
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void OnMainToolBarGUI()
        {
            OnGUI();
            UGUIAlignWindow.OnGUI();
        }

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public void OnGUI()
        {
            Debug.Log("this is a place holder");
        }

    }

    public void OnMainToolBarGUI()
    {
        UGUIAlignWindow.OnGUI();
    }

    static void OnUpdate()
    {
        if (m_currentToolbar == null)
        {
            // Find toolbar
            var toolbars = Resources.FindObjectsOfTypeAll(m_toolbarType);
            m_currentToolbar = toolbars.Length > 0 ? (ScriptableObject)toolbars[0] : null;
            if (m_currentToolbar != null)
            {
#if UNITY_2021_1_OR_NEWER
					var root = m_currentToolbar.GetType().GetField("m_Root", BindingFlags.NonPublic | BindingFlags.Instance);
					var rawRoot = root.GetValue(m_currentToolbar);
					var mRoot = rawRoot as VisualElement;
					//RegisterCallback("ToolbarZoneLeftAlign", OnToolbarGUILeft);
					RegisterCallback("ToolbarZoneRightAlign", OnGUI);

					void RegisterCallback(string root, Action cb) {
						var toolbarZone = mRoot.Q(root);

						var parent = new VisualElement()
						{
							style = {
								flexGrow = 1,
								flexDirection = FlexDirection.Row,
							}
						};
						var container = new IMGUIContainer();
						container.onGUIHandler += () => { 
							cb?.Invoke();
						}; 
						parent.Add(container);
						toolbarZone.Add(parent);
					}
#else
#if UNITY_2020_1_OR_NEWER
                var windowBackend = m_windowBackend.GetValue(m_currentToolbar);

                // Get it's visual tree
                var visualTree = (VisualElement)m_viewVisualTree.GetValue(windowBackend, null);
#else
					// Get it's visual tree
					var visualTree = (VisualElement) m_viewVisualTree.GetValue(m_currentToolbar, null);
#endif

                // Get first child which 'happens' to be toolbar IMGUIContainer
                var container = (IMGUIContainer)visualTree[0];


                // (Re)attach handler
                var handler = (Action)m_imguiContainerOnGui.GetValue(container);
                handler -= OnGUI;
                handler += OnGUI;
                m_imguiContainerOnGui.SetValue(container, handler);

#endif
            }
        }
    }
    public static void OnGUI()
    {
        if (s_buttonStyle == null)
        {
            s_buttonStyle = new GUIStyle("Command")
            {
                fixedWidth = 34,
                fixedHeight = 22,
                fontSize = 16,
                //alignment = TextAnchor.MiddleCenter,
                imagePosition = ImagePosition.ImageAbove,
                fontStyle = FontStyle.Bold
            };
        }
        if (m_currentToolbar == null)
        {
            // Find toolbar
            var toolbars = Resources.FindObjectsOfTypeAll(m_toolbarType);
            m_currentToolbar = toolbars.Length > 0 ? (ScriptableObject)toolbars[0] : null;
        }
            var position = (Rect)(m_position.GetValue(m_currentToolbar));
        var realType = m_currentToolbar.GetType();
        int playModeControlsStart = Mathf.RoundToInt((position.width + 100) / 2);
        var pos = new Rect(playModeControlsStart, 0, 300, 0);
        GUILayout.BeginArea(new Rect(pos.x, 4, pos.width, 22));
        GUILayout.BeginHorizontal();
        // GUILayout.BeginVertical();
        for (int i = (int)AlignType.Top; i <= (int)AlignType.Vertical; i++)
        {
            if (GUILayout.Button(alignTexture[(AlignType)i], s_buttonStyle))
            {
                UGUIAlign.Align((AlignType)i);
            }
            /* if (i%3 == 0)
             {
                 GUILayout.EndVertical();
                 GUILayout.BeginVertical();
             }*/
        }
        //  GUILayout.EndVertical();
        GUILayout.EndHorizontal();
        GUILayout.EndArea();
    }
}
