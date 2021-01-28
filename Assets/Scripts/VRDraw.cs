using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Oculus.Platform;
using OVR.OpenVR;
using UnityEngine;
using UnityEngine.UIElements;

public enum HandToTrack
{
    Left,
    Right
}
public class VRDraw : MonoBehaviour
{
    [SerializeField] private HandToTrack handToTrack = HandToTrack.Left;

    [SerializeField] private GameObject objectToTrackMovement;
    
    [SerializeField] private Vector3 prevPointDistance = Vector3.zero;

    [SerializeField] private float minFingerPinchStrength = 0.5f;
    
    [SerializeField, Range(0, 1.0f)] private float minDistanceBeforeNewPoint = 0.2f;
    
    [SerializeField, Range(0, 1.0f)] private float lineDefaultWidth = 0.010f;

    private int positionCount = 0;
    
    private List<LineRenderer> lines = new List<LineRenderer>();

    private LineRenderer currentLineRenderer;

    [SerializeField] private Color defaultColor = Color.white;

    [SerializeField] private GameObject editorObjectToTrackMovement;

    [SerializeField] private bool allowEditorControls = true;

    [SerializeField] private Material defaultLineMaterial;

    private bool IsPinchingDown = false;
    
    
    #region Oculus Types

    private OVRHand ovrHand;

    private OVRSkeleton ovrSkeleton;

    private OVRBone boneToTrack;
    
    private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

    #endregion
    
    void Awake()
    {
        ovrHand = objectToTrackMovement.GetComponent<OVRHand>();
        ovrSkeleton = objectToTrackMovement.GetComponent<OVRSkeleton>();
        
        #if UNITY_EDITOR

        if (allowEditorControls)
        {
            objectToTrackMovement =
                editorObjectToTrackMovement != null ? editorObjectToTrackMovement : objectToTrackMovement;
        }
        
        #endif
        
        // Get initial bone to track
        boneToTrack = ovrSkeleton.Bones
            .SingleOrDefault(b => b.Id == OVRSkeleton.BoneId.Hand_Index1);
        
        // Add initial line renderer
        AddNewLineRenderer();
    }

    void AddNewLineRenderer()
    {
        positionCount = 0;
        GameObject go = new GameObject($"LineRenderer+{handToTrack.ToString()}_{lines.Count}");
        go.transform.parent = objectToTrackMovement.transform.parent;
        go.transform.position = objectToTrackMovement.transform.position;

        // ReSharper disable once Unity.PerformanceCriticalCodeInvocation
        LineRenderer goLineRenderer = go.AddComponent<LineRenderer>();
        goLineRenderer.startWidth = lineDefaultWidth;
        goLineRenderer.endWidth = lineDefaultWidth;
        goLineRenderer.useWorldSpace = true;
        goLineRenderer.material = defaultLineMaterial;
        goLineRenderer.positionCount = 1;
        goLineRenderer.numCapVertices = 5;

        currentLineRenderer = goLineRenderer;
        
        lines.Add(goLineRenderer);
    }
    
    // Update is called once per frame
    void Update()
    {
        if (boneToTrack == null)
        {
            boneToTrack = ovrSkeleton.Bones
                .SingleOrDefault(b => b.Id == OVRSkeleton.BoneId.Hand_Index1);

            if (boneToTrack != null) objectToTrackMovement = boneToTrack.Transform.gameObject;
        }
        
        CheckPinchState();
    }

    void CheckPinchState()
    {
        bool isIndexFingerPinching = ovrHand.GetFingerIsPinching(OVRHand.HandFinger.Index);

        float indexFingerPinchStrength = ovrHand.GetFingerPinchStrength(OVRHand.HandFinger.Index);

        if (isIndexFingerPinching && indexFingerPinchStrength >= minFingerPinchStrength)
        {
            UpdateLine();
            IsPinchingDown = false;
        }
        else
        {
            if (!IsPinchingDown)
            {
                AddNewLineRenderer();
                IsPinchingDown = true;
            }
        }
    }

    void UpdateLine()
    {

        if (Mathf.Abs(Vector3.Distance(prevPointDistance, objectToTrackMovement.transform.position)) >=
            minDistanceBeforeNewPoint)
        {
            Vector3 newPosition = objectToTrackMovement.transform.position;
            Vector3 dir = (newPosition - Camera.main.transform.position).normalized;
            prevPointDistance = newPosition;
            AddPoint(prevPointDistance, dir);
        }
    }

    void AddPoint(Vector3 position, Vector3 direction)
    {
        currentLineRenderer.SetPosition(positionCount, position);
        positionCount++;
        currentLineRenderer.positionCount = positionCount + 1;
        currentLineRenderer.SetPosition(positionCount, position);
    }

    public void UpdateLineWidth(float newValue)
    {
        currentLineRenderer.startWidth = newValue;
        currentLineRenderer.endWidth = newValue;
        lineDefaultWidth = newValue;
    }

    public void UpdateLineColor(Color color)
    {
        // in case we haven't drawn anything
        if (currentLineRenderer.positionCount == 1)
        {
            currentLineRenderer.material.color = color;
            currentLineRenderer.material.EnableKeyword("_EMISSION");
            currentLineRenderer.material.SetColor(EmissionColor, color);
        }
        
        defaultColor = color;
    }

    public void UpdateLineMinDistance(float newValue)
    {
        minDistanceBeforeNewPoint = newValue;
    }
}