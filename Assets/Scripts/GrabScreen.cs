using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Experimental.Rendering;


public class GrabScreen : MonoBehaviour
{
    // Grab the camera's view when this variable is true.
    public bool grab;
    public Camera thecamera;
    public Texture2D mytexture;
    public static GrabScreen Instance;

    WaitForEndOfFrame frameEnd = new WaitForEndOfFrame();


    // The "m_Display" is the GameObject whose Texture will be set to the captured image.
    public Renderer m_Display;

    public void Start()
    {
        mytexture = new Texture2D(thecamera.pixelWidth, thecamera.pixelHeight, TextureFormat.RGB24, false);
        Instance = this;
    }

    private void Update()
    {
        //Press space to start the screen grab
    }

    private void OnEnable()
    {
        RenderPipeline.beginFrameRendering += RenderPipeline_beginFrameRendering;
    }

    private void OnDisable()
    {
        RenderPipeline.beginFrameRendering -= RenderPipeline_beginFrameRendering;
    }

    private void RenderPipeline_beginFrameRendering(Camera[] obj)
    {
        OnPostRender();
    }

    public void OnPostRender()
    {

        //Create a new texture with the width and height of the screen
        float height = 2f * thecamera.orthographicSize;
        float width = height * thecamera.aspect;
        mytexture = new Texture2D(thecamera.pixelWidth, thecamera.pixelHeight, TextureFormat.RGB24, false);
        //Read the pixels in the Rect starting at 0,0 and ending at the screen's width and height
        mytexture.ReadPixels(new Rect(0, 0, thecamera.pixelWidth, thecamera.pixelHeight), 0, 0, false);
        mytexture.Apply();
        //Check that the display field has been assigned in the Inspector
        if (m_Display != null)
            //Give your GameObject with the renderer this texture
            m_Display.material.mainTexture = mytexture;
        //Reset the grab state
    }
}