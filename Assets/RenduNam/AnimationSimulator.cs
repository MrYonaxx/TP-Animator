using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEditor;
using UnityEditorInternal;
using UnityEditor.SceneManagement;
using UnityEditor.Callbacks;
using UnityEngine.SceneManagement;


public class ListAnimator
{
    public Animator Animator;
    public AnimationClip AnimationClip;

    public ListAnimator(Animator anim, AnimationClip animClip)
    {
        Animator = anim;
        AnimationClip = animClip;
    }
}


public class AnimationSimulator : EditorWindow
{

    List<ListAnimator> animators = new List<ListAnimator>();
    List<AnimationClip> animationClip = new List<AnimationClip>();

    //Dictionary<Animator, AnimationClip> animationC;
    //AnimSampler animSampler;

    private ReorderableList listAnimators = null;
    private ReorderableList listAnimationClip = null;


    bool inPlay = false;

    bool animationPlaying = false;

    double startTime = 0;
    double currentTime = 0;

    int sampleTime = 0;
    int maxTime = -1;


    [MenuItem("Toolbox/Animation Simulator")]
    static void InitWindow()
    {
        AnimationSimulator window = GetWindow<AnimationSimulator>();
        window.Show();
        window.titleContent = new GUIContent("Animation Simulator");

    }

    private void OnEnable()
    {
        EditorSceneManager.sceneOpened += OnSceneOpened;
        EditorApplication.playModeStateChanged += OnPlayStateChanged;

        animators = new List<ListAnimator>();
        animationClip = new List<AnimationClip>();
        FindAnimatorsScene();



        listAnimators = new ReorderableList(animators, typeof(Animator), true, true, false, false);
        listAnimators.drawHeaderCallback += DrawAnimatorsHeader;
        listAnimators.onSelectCallback += FindAnimationsClip;
        listAnimators.drawElementCallback += DrawAnimatorList;

        listAnimationClip = new ReorderableList(animationClip, typeof(AnimationClip), false, true, false, false);
        listAnimationClip.drawHeaderCallback += DrawAnimationClipHeader;
        listAnimationClip.onSelectCallback += SetAnimationClip;
        listAnimationClip.drawElementCallback += DrawAnimationClip;

    }


    private void OnHierarchyChange()
    {
        FindAnimatorsScene();
        animationClip.RemoveRange(0, animationClip.Count);
    }


    private void OnDisable()
    {
        EditorSceneManager.sceneOpened -= OnSceneOpened;
        EditorApplication.playModeStateChanged -= OnPlayStateChanged;

        listAnimators.drawHeaderCallback -= DrawAnimatorsHeader;
        listAnimators.onSelectCallback -= FindAnimationsClip;
        listAnimators.drawElementCallback -= DrawAnimatorList;

        listAnimationClip.drawHeaderCallback -= DrawAnimationClipHeader;
        listAnimationClip.onSelectCallback -= SetAnimationClip;
        listAnimationClip.drawElementCallback -= DrawAnimationClip;
    }



    public void OnPlayStateChanged(PlayModeStateChange stateChange)
    {
        if (stateChange == PlayModeStateChange.EnteredEditMode)
            inPlay = false;
        else if (stateChange == PlayModeStateChange.EnteredPlayMode)
        {
            inPlay = true;
            ResetAnimations();
        }
    }


    public void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        FindAnimatorsScene();
        animationClip.RemoveRange(0, animationClip.Count);
    }





    // ======================================================================================
    #region AnimatorCallback

    public void DrawAnimatorsHeader(Rect rect)
    {
        EditorGUI.LabelField(rect, "Animators");
    }


    public void DrawAnimatorList(Rect rect, int index, bool isActive, bool isFocused)
    {
        Rect rectAnimator = new Rect(       rect.position,                                           new Vector2(rect.size.x * 0.5f, rect.size.y));
        Rect rectAnimationClip = new Rect(  new Vector2(rect.position.x + rect.size.x * 0.5f, rect.position.y),    new Vector2(rect.size.x * 0.5f, rect.size.y));


        EditorGUI.LabelField(rectAnimator, animators[index].Animator.name);
        if(animators[index].AnimationClip != null)
            EditorGUI.LabelField(rectAnimationClip, animators[index].AnimationClip.name);
        else
            EditorGUI.LabelField(rectAnimationClip, "---");
    }


    public void FindAnimationsClip(ReorderableList list)
    {
        EditorGUIUtility.PingObject(animators[list.index].Animator.gameObject);
        Selection.activeObject = animators[list.index].Animator.gameObject;

        // Si je clear je perd la ref
        animationClip.RemoveRange(0, animationClip.Count);

        for (int j = 0; j < animators[list.index].Animator.runtimeAnimatorController.animationClips.Length; j++)
        {
            animationClip.Add(animators[list.index].Animator.runtimeAnimatorController.animationClips[j]);
        }
    }

    #endregion
    // ======================================================================================



    // ======================================================================================
    #region Animation Callback

    public void DrawAnimationClipHeader(Rect rect)
    {
        EditorGUI.LabelField(rect, "Animation");
    }


    public void DrawAnimationClip(Rect rect, int index, bool isActive, bool isFocused)
    {
        EditorGUI.LabelField(rect, animationClip[index].name);
    }


    public void SetAnimationClip(ReorderableList list)
    {
        animators[listAnimators.index].AnimationClip = animationClip[list.index];
        CalculateMaxTime();
    }

    #endregion
    // ======================================================================================



    // ======================================================================================
    public void FindAnimatorsScene()
    {
        List<ListAnimator> animatorToRemove = new List<ListAnimator>(animators.Count);
        for (int i = 0; i < animators.Count; i++)
        {
            animatorToRemove.Add(animators[i]);
        }


        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid()) return;

        GameObject[] rootGameObjects = scene.GetRootGameObjects();
        foreach (GameObject rootGameObject in rootGameObjects)
        {
            if (!rootGameObject.activeInHierarchy) continue;

            Animator animator = rootGameObject.GetComponent<Animator>();
            if (animator != null)
            {
                if (!ContainsAnimator(animators, animator))
                {
                    animators.Add(new ListAnimator(animator, null));
                }
                else
                    RemoveAnimator(animatorToRemove, animator);
            }
        }


        for (int i = 0; i < animatorToRemove.Count; i++)
        {
            animators.Remove(animatorToRemove[i]);
        }
    }

    public bool ContainsAnimator(List<ListAnimator> listAnim, Animator anim)
    {
        for (int i = 0; i < listAnim.Count; i++)
        {
            if (listAnim[i].Animator == anim)
                return true;
        }
        return false;
    }

    public void RemoveAnimator(List<ListAnimator> listAnim, Animator anim)
    {
        for (int i = 0; i < listAnim.Count; i++)
        {
            if (listAnim[i].Animator == anim)
            {
                listAnim.RemoveAt(i);
                return;
            }
        }
    }
    // ======================================================================================






    // ======================================================================================
    private void CalculateMaxTime()
    {
        maxTime = -1;
        float time = 0f;
        for (int i = 0; i < animators.Count; i++)
        {
            if (animators[i].AnimationClip != null)
            {
                if (animators[i].AnimationClip.length > time)
                {
                    time = animators[i].AnimationClip.length;
                }
            }
        }
        maxTime = (int)(time * 60f);
    }

    private void PlayAnimations()
    {
        if(animationPlaying == false)
        {
            startTime = EditorApplication.timeSinceStartup;
        }
        animationPlaying = true;
        currentTime = EditorApplication.timeSinceStartup - startTime;

        if((int)(currentTime * 60f) > maxTime)
        {
            animationPlaying = false;
        }
        else
        {
            SetAnimations((float)currentTime);
        }

    }

    private void SetAnimations(float time)
    {
        for (int i = 0; i < animators.Count; i++)
        {
            if(animators[i].AnimationClip != null)
                animators[i].AnimationClip.SampleAnimation(animators[i].Animator.gameObject, time);
        }
    }

    public void ResetAnimations()
    {
        animationPlaying = false;
        SetAnimations(0);
    }
    // ======================================================================================















    private void Update()
    {
        if (animationPlaying)
            PlayAnimations();
    }

    private void OnGUI()
    {
        if (listAnimators != null)
        {
            if (listAnimators.count != 0)
            {
                listAnimators.DoLayoutList();
            }
        }

        if (GUILayout.Button("Reset Selection"))
        {
            for (int i = 0; i < animators.Count; i++)
            {
                animators[i].AnimationClip = null;
            }
            CalculateMaxTime();
        }

        if (inPlay == false)
        {
            GUILayout.BeginHorizontal("Buttons");
            GUI.enabled = !animationPlaying;
            if (GUILayout.Button("Play"))
            {
                PlayAnimations();
            }
            GUI.enabled = animationPlaying;
            if (GUILayout.Button("Stop"))
            {
                ResetAnimations();
            }
            GUI.enabled = true;
            GUILayout.EndHorizontal();


            //GUILayout.BeginHorizontal("Sliders");
            if (maxTime > 0)
            {
                int time = sampleTime;
                sampleTime = EditorGUILayout.IntSlider("Time", sampleTime, 0, maxTime);
                if(sampleTime != time)
                {
                    ResetAnimations();
                    SetAnimations((sampleTime / 60f));
                }

            }
        }
        //GUILayout.EndHorizontal();


        if (listAnimationClip != null)
        {
            if (listAnimationClip.count != 0)
            {
                listAnimationClip.DoLayoutList();
            }
        }
    }
}
