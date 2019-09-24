using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System;

public enum PaperSize {
    A0,
    A1,
    A2,
    A3,
    A4,
    A5,
    B0,
    B1,
    B2,
    B3,
    B4,
    B5,
    Custom
};

public class CameraCapture : MonoBehaviour
{
	public Camera mainCamera;
    [Range(1.0f, 2000.0f)]
    public float dpi = 350.0f;
    [Range(1.0f, 16.0f)]
    public float dpiPerPpi = 1.0f;
    [HideInInspector]
    [Range(1.0f, 2000.0f)]
    public float paperWidth = 210.0f;
    [HideInInspector]
    [Range(1.0f, 2000.0f)]
    public float paperHeight = 297.0f;
    [HideInInspector]
    public bool swap = false;
    [HideInInspector]
    public PaperSize paperSize = PaperSize.A4;
    
    private bool isPlaying = false;

    private double inchPerMilli = 1.0 / 25.4;

    private int[] paperWidthArray = new int[] {
        841,
        594,
        420,
        297,
        210,
        148,
        1030,
        728,
        515,
        364,
        257,
        182
    };

    private int[] paperHeightArray = new int[] {
        1189,
        841,
        594,
        420,
        297,
        210,
        1456,
        1030,
        728,
        515,
        364,
        257
    };

    void OnGUI()
    {
        isPlaying = EditorApplication.isPlaying;
    }

    public void capture () {
		if (!isPlaying) {
			Debug.Log("プレイ中ではないのでキャプチャできません！");
			return;
		}
        double paperWidthTemp = paperWidth * inchPerMilli;
        double paperHeightTemp = paperHeight * inchPerMilli;
        if (paperSize != PaperSize.Custom) {
            paperWidthTemp = (double)paperWidthArray[(int)paperSize] * inchPerMilli;
            paperHeightTemp = (double)paperHeightArray[(int)paperSize] * inchPerMilli;
        }
        if (swap) {
            double temp = paperWidthTemp;
            paperWidthTemp = paperHeightTemp;
            paperHeightTemp = temp;
        }
        int width = (int)Math.Floor(dpi / Math.Sqrt(dpiPerPpi) * paperWidthTemp);
        int height = (int)Math.Floor(dpi / Math.Sqrt(dpiPerPpi) * paperHeightTemp);
        
        if (mainCamera.targetTexture != null ) mainCamera.targetTexture.Release();
        RenderTexture renderTex = new RenderTexture(width, height, 24);
        mainCamera.targetTexture = renderTex;
		Texture2D tex = new Texture2D(renderTex.width, renderTex.height, TextureFormat.ARGB32, false, false);
		mainCamera.Render();
        RenderTexture.active = renderTex;
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();

        byte[] bytes = tex.EncodeToPNG();
        Destroy(tex);
        RenderTexture.active = null;
        mainCamera.targetTexture = null;
        Destroy(renderTex);

        File.WriteAllBytes(Application.dataPath + "/../Assets/SavedScreen.png", bytes);
        Debug.Log("キャプチャしました！ width: " + width + " height: " + height);
	}
}

[CustomEditor(typeof(CameraCapture))]
public class ScreenShotSaverEditor : Editor {
    private bool disabled;
    public override void OnInspectorGUI () {
        base.OnInspectorGUI ();

        CameraCapture cameraCapture = target as CameraCapture;
        cameraCapture.swap = EditorGUILayout.Toggle("幅と高さを入れ替える", cameraCapture.swap);
        cameraCapture.paperSize = (PaperSize)EditorGUILayout.EnumPopup(
            "用紙サイズ", cameraCapture.paperSize);
        disabled = cameraCapture.paperSize != PaperSize.Custom;
        EditorGUI.BeginDisabledGroup(disabled);
        cameraCapture.paperWidth = EditorGUILayout.FloatField(
            "幅（mm）", cameraCapture.paperWidth);
        cameraCapture.paperHeight = EditorGUILayout.FloatField(
            "高さ（mm）", cameraCapture.paperHeight);
        EditorGUI.EndDisabledGroup();
        if (GUILayout.Button("Capture")) cameraCapture.capture();
    }

}
